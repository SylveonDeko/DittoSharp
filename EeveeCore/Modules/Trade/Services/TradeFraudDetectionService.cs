using EeveeCore.Modules.Trade.Models;
using System.Text.Json;
using Serilog;
using LinqToDB;
using EeveeCore.Database.Linq.Models.Game;

namespace EeveeCore.Modules.Trade.Services;

/// <summary>
///     Main service for detecting and handling trade fraud.
///     This service coordinates risk analysis, relationship tracking, and automated responses.
/// </summary>
public class TradeFraudDetectionService : INService
{
    private readonly LinqToDbConnectionProvider _dbProvider;
    private readonly TradeRiskScorer _riskScorer;
    private readonly TradeValueCalculator _valueCalculator;
    private readonly IDataCache _cache;
    private readonly DiscordShardedClient _discordClient;

    // Risk thresholds for automated actions
    private const double LogOnlyThreshold = 0.2;
    private const double ReviewThreshold = 0.4;
    private const double BlockThreshold = 0.6;
    private const double TempBanThreshold = 0.8;
    private const double AdminAlertThreshold = 0.9;

    // Admin notification channel ID (should be configurable)
    private const ulong AdminChannelId = 1004571710323957830; // Same as trade log channel for now

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeFraudDetectionService" /> class.
    /// </summary>
    /// <param name="dbProvider">The LinqToDB connection provider.</param>
    /// <param name="riskScorer">The trade risk scoring service.</param>
    /// <param name="valueCalculator">The trade value calculator service.</param>
    /// <param name="cache">The cache service.</param>
    /// <param name="discordClient">The Discord client for notifications.</param>
    public TradeFraudDetectionService(
        LinqToDbConnectionProvider dbProvider,
        TradeRiskScorer riskScorer,
        TradeValueCalculator valueCalculator,
        IDataCache cache,
        DiscordShardedClient discordClient)
    {
        _dbProvider = dbProvider;
        _riskScorer = riskScorer;
        _valueCalculator = valueCalculator;
        _cache = cache;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Analyzes a trade for fraud and takes appropriate action.
    /// </summary>
    /// <param name="session">The trade session to analyze.</param>
    /// <returns>A result indicating whether the trade should be allowed to proceed.</returns>
    public async Task<FraudDetectionResult> AnalyzeTradeAsync(TradeSession session)
    {
        // Perform risk analysis
        var riskAnalysis = await _riskScorer.AnalyzeTradeRiskAsync(session);

        // Update user relationship data
        await UpdateUserRelationshipAsync(session, riskAnalysis);

        // Store analytics data
        await StoreAnalyticsDataAsync(session, riskAnalysis);

        // Determine automated action
        var automatedAction = DetermineAutomatedAction(riskAnalysis);

        // Create fraud detection record if risk is significant
        TradeFraudDetection? fraudDetection = null;
        if (riskAnalysis.OverallRiskScore >= LogOnlyThreshold)
        {
            fraudDetection = await CreateFraudDetectionRecordAsync(session, riskAnalysis, automatedAction);
        }

        // Execute automated action
        var actionResult = await ExecuteAutomatedActionAsync(session, riskAnalysis, automatedAction, fraudDetection);

        return new FraudDetectionResult
        {
            IsAllowed = actionResult.IsAllowed,
            RiskScore = riskAnalysis.OverallRiskScore,
            RiskLevel = riskAnalysis.RiskLevel,
            AutomatedAction = automatedAction,
            Message = actionResult.Message,
            FraudFlags = GetFraudFlags(riskAnalysis),
            RequiresAdminReview = automatedAction == AutomatedAction.FlagForReview || 
                                 automatedAction == AutomatedAction.AdminAlert
        };
    }

    /// <summary>
    ///     Updates the user relationship record with new trade data.
    /// </summary>
    /// <param name="session">The trade session.</param>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    private async Task UpdateUserRelationshipAsync(TradeSession session, TradeRiskAnalysis riskAnalysis)
    {
        // Ensure consistent user ordering
        var (user1Id, user2Id) = session.Player1Id < session.Player2Id ? 
            (session.Player1Id, session.Player2Id) : 
            (session.Player2Id, session.Player1Id);

        await using var db = await _dbProvider.GetConnectionAsync();

        var relationship = await db.UserTradeRelationships
            .FirstOrDefaultAsync(r => r.User1Id == user1Id && r.User2Id == user2Id);

        if (relationship == null)
        {
            // Create new relationship
            relationship = await CreateNewUserRelationshipAsync(user1Id, user2Id, session, riskAnalysis);
            await db.InsertAsync(relationship);
        }
        else
        {
            // Update existing relationship
            await UpdateExistingUserRelationshipAsync(relationship, session, riskAnalysis);
        }

        // Changes are saved via individual InsertAsync/UpdateAsync calls
    }

    /// <summary>
    ///     Creates a new user relationship record.
    /// </summary>
    /// <param name="user1Id">First user ID (lower ID).</param>
    /// <param name="user2Id">Second user ID (higher ID).</param>
    /// <param name="session">The trade session.</param>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <returns>New user relationship record.</returns>
    private async Task<UserTradeRelationship> CreateNewUserRelationshipAsync(
        ulong user1Id, ulong user2Id, TradeSession session, TradeRiskAnalysis riskAnalysis)
    {
        var now = DateTime.UtcNow;
        
        // Calculate account age from Discord user IDs (Discord snowflake epoch: January 1, 2015)
        var user1Age = CalculateAccountAgeFromUserId(user1Id, now);
        var user2Age = CalculateAccountAgeFromUserId(user2Id, now);

        // Determine which user gave more value
        var user1Value = session.Player1Id == user1Id ? riskAnalysis.SenderTotalValue : riskAnalysis.ReceiverTotalValue;
        var user2Value = session.Player1Id == user1Id ? riskAnalysis.ReceiverTotalValue : riskAnalysis.SenderTotalValue;

        return new UserTradeRelationship
        {
            User1Id = user1Id,
            User2Id = user2Id,
            TotalTrades = 1,
            FirstTradeTimestamp = now,
            LastTradeTimestamp = now,
            User1TotalGivenValue = user1Value,
            User2TotalGivenValue = user2Value,
            User1FavoringTrades = user1Value > user2Value * 1.5m ? 1 : 0,
            User2FavoringTrades = user2Value > user1Value * 1.5m ? 1 : 0,
            BalancedTrades = Math.Abs(user1Value - user2Value) <= Math.Max(user1Value, user2Value) * 0.2m ? 1 : 0,
            RelationshipRiskScore = riskAnalysis.RelationshipRiskScore,
            ValueImbalanceRatio = riskAnalysis.ValueRatio,
            TradingFrequencyScore = 0, // First trade
            AccountAgeDifferenceDays = (int)Math.Abs(user1Age - user2Age),
            SuspiciousCreationTiming = Math.Abs(user1Age - user2Age) < 7 && Math.Min(user1Age, user2Age) < 30,
            NewerAccountAgeAtFirstTrade = (int)Math.Min(user1Age, user2Age),
            AverageTradeIntervalHours = 0, // First trade
            TradeIntervalStdDev = 0, // First trade
            SuspiciousTimingPattern = false, // Can't determine from one trade
            LastUpdated = now,
            FlaggedPotentialAlts = riskAnalysis.FlaggedAltAccount,
            FlaggedPotentialRmt = riskAnalysis.FlaggedRmt,
            FlaggedNewbieExploitation = riskAnalysis.FlaggedNewbieExploitation
        };
    }

    /// <summary>
    ///     Updates an existing user relationship record.
    /// </summary>
    /// <param name="relationship">The existing relationship record.</param>
    /// <param name="session">The trade session.</param>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    private async Task UpdateExistingUserRelationshipAsync(
        UserTradeRelationship relationship, TradeSession session, TradeRiskAnalysis riskAnalysis)
    {
        var now = DateTime.UtcNow;

        // Update trade counts and values
        relationship.TotalTrades++;
        relationship.LastTradeTimestamp = now;

        // Determine value contributions
        var user1Value = session.Player1Id == relationship.User1Id ? 
            riskAnalysis.SenderTotalValue : riskAnalysis.ReceiverTotalValue;
        var user2Value = session.Player1Id == relationship.User1Id ? 
            riskAnalysis.ReceiverTotalValue : riskAnalysis.SenderTotalValue;

        relationship.User1TotalGivenValue += user1Value;
        relationship.User2TotalGivenValue += user2Value;

        // Update trade balance counts
        if (user1Value > user2Value * 1.5m)
        {
            relationship.User1FavoringTrades++;
        }
        else if (user2Value > user1Value * 1.5m)
        {
            relationship.User2FavoringTrades++;
        }
        else
        {
            relationship.BalancedTrades++;
        }

        // Update calculated fields
        relationship.ValueImbalanceRatio = relationship.User1TotalGivenValue == 0 || relationship.User2TotalGivenValue == 0 ?
            (double)Math.Max(relationship.User1TotalGivenValue, relationship.User2TotalGivenValue) :
            (double)(Math.Max(relationship.User1TotalGivenValue, relationship.User2TotalGivenValue) / 
                    Math.Min(relationship.User1TotalGivenValue, relationship.User2TotalGivenValue));

        // Update trading frequency
        var totalDays = (now - relationship.FirstTradeTimestamp).TotalDays;
        relationship.TradingFrequencyScore = totalDays > 0 ? relationship.TotalTrades / totalDays : 0;

        // Update timing analysis
        var timeSinceLastTrade = (now - relationship.LastTradeTimestamp).TotalHours;
        if (relationship.TotalTrades > 1)
        {
            var newAverage = ((relationship.AverageTradeIntervalHours * (relationship.TotalTrades - 2)) + timeSinceLastTrade) / 
                            (relationship.TotalTrades - 1);
            relationship.AverageTradeIntervalHours = newAverage;

            // Check for suspicious timing patterns (very regular intervals)
            if (relationship is { TotalTrades: >= 5, TradeIntervalStdDev: < 1.0, AverageTradeIntervalHours: < 24 })
            {
                relationship.SuspiciousTimingPattern = true;
            }
        }

        // Update risk flags
        relationship.RelationshipRiskScore = riskAnalysis.RelationshipRiskScore;
        relationship.FlaggedPotentialAlts = relationship.FlaggedPotentialAlts || riskAnalysis.FlaggedAltAccount;
        relationship.FlaggedPotentialRmt = relationship.FlaggedPotentialRmt || riskAnalysis.FlaggedRmt;
        relationship.FlaggedNewbieExploitation = relationship.FlaggedNewbieExploitation || riskAnalysis.FlaggedNewbieExploitation;

        relationship.LastUpdated = now;
    }

    /// <summary>
    ///     Stores analytics data for the trade.
    /// </summary>
    /// <param name="session">The trade session.</param>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    private async Task StoreAnalyticsDataAsync(TradeSession session, TradeRiskAnalysis riskAnalysis)
    {
        var now = DateTime.UtcNow;
        
        // Calculate account ages from Discord user IDs
        var senderAccountAge = CalculateAccountAgeFromUserId(session.Player1Id, now);
        var receiverAccountAge = CalculateAccountAgeFromUserId(session.Player2Id, now);
        
        // This would be called after the trade is logged to get the trade ID
        // For now, we'll store it with a placeholder trade ID that can be updated later
        var analytics = new SuspiciousTradeAnalytics
        {
            TradeId = 0, // Will be updated when trade log is created
            AnalysisTimestamp = riskAnalysis.AnalysisTimestamp,
            OverallRiskScore = riskAnalysis.OverallRiskScore,
            ValueImbalanceScore = riskAnalysis.ValueImbalanceScore,
            RelationshipRiskScore = riskAnalysis.RelationshipRiskScore,
            BehavioralRiskScore = riskAnalysis.BehavioralRiskScore,
            AccountAgeRiskScore = riskAnalysis.AccountAgeRiskScore,
            SenderTotalValue = riskAnalysis.SenderTotalValue,
            ReceiverTotalValue = riskAnalysis.ReceiverTotalValue,
            ValueRatio = riskAnalysis.ValueRatio,
            ValueDifference = riskAnalysis.ValueDifference,
            SenderAccountAgeDays = (int)senderAccountAge,
            ReceiverAccountAgeDays = (int)receiverAccountAge,
            PreviousTradesCount = 0, // Will be populated from relationship data
            PreviousTotalValue = 0, // Will be populated from relationship data
            FlaggedAltAccount = riskAnalysis.FlaggedAltAccount,
            FlaggedRmt = riskAnalysis.FlaggedRmt,
            FlaggedNewbieExploitation = riskAnalysis.FlaggedNewbieExploitation,
            FlaggedUnusualBehavior = riskAnalysis.FlaggedUnusualBehavior,
            FlaggedBotActivity = riskAnalysis.FlaggedBotActivity
        };

        // Store in cache with session ID as key for later retrieval
        var cacheKey = $"trade_analytics:{session.SessionId}";
        var database = _cache.Redis.GetDatabase();
        await database.StringSetAsync(cacheKey, JsonSerializer.Serialize(analytics), TimeSpan.FromHours(24));
    }

    /// <summary>
    ///     Determines the appropriate automated action based on risk level.
    /// </summary>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <returns>The automated action to take.</returns>
    private static AutomatedAction DetermineAutomatedAction(TradeRiskAnalysis riskAnalysis)
    {
        var riskScore = riskAnalysis.OverallRiskScore;

        return riskScore switch
        {
            >= AdminAlertThreshold => AutomatedAction.AdminAlert,
            >= TempBanThreshold => AutomatedAction.TempRestriction,
            >= BlockThreshold => AutomatedAction.BlockTrade,
            >= ReviewThreshold => AutomatedAction.FlagForReview,
            >= LogOnlyThreshold => AutomatedAction.LogOnly,
            _ => AutomatedAction.LogOnly
        };
    }

    /// <summary>
    ///     Creates a fraud detection record.
    /// </summary>
    /// <param name="session">The trade session.</param>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <param name="automatedAction">The automated action being taken.</param>
    /// <returns>The created fraud detection record.</returns>
    private async Task<TradeFraudDetection> CreateFraudDetectionRecordAsync(
        TradeSession session, TradeRiskAnalysis riskAnalysis, AutomatedAction automatedAction)
    {
        await using var db = await _dbProvider.GetConnectionAsync();

        var fraudType = DetermineFraudType(riskAnalysis);
        var triggeredRules = GetTriggeredRules(riskAnalysis);

        var detection = new TradeFraudDetection
        {
            TradeId = null, // Will be set when trade log is created
            DetectionTimestamp = DateTime.UtcNow,
            PrimaryUserId = session.Player1Id,
            SecondaryUserId = session.Player2Id,
            FraudType = fraudType,
            ConfidenceLevel = riskAnalysis.OverallRiskScore,
            RiskScore = riskAnalysis.OverallRiskScore,
            TriggeredRules = JsonSerializer.Serialize(triggeredRules),
            DetectionDetails = JsonSerializer.Serialize(new
            {
                riskAnalysis.ValueImbalanceScore,
                riskAnalysis.RelationshipRiskScore,
                riskAnalysis.BehavioralRiskScore,
                riskAnalysis.AccountAgeRiskScore,
                riskAnalysis.SenderTotalValue,
                riskAnalysis.ReceiverTotalValue,
                riskAnalysis.ValueRatio
            }),
            AutomatedAction = automatedAction,
            TradeBlocked = automatedAction == AutomatedAction.BlockTrade || 
                          automatedAction == AutomatedAction.TempRestriction,
            UsersNotified = false, // Will be set when notifications are sent
            AdminAlerted = automatedAction == AutomatedAction.AdminAlert,
            InvestigationStatus = automatedAction == AutomatedAction.AdminAlert ? 
                InvestigationStatus.Pending : InvestigationStatus.Dismissed
        };

        await db.InsertAsync(detection);

        return detection;
    }

    /// <summary>
    ///     Executes the determined automated action.
    /// </summary>
    /// <param name="session">The trade session.</param>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <param name="automatedAction">The action to execute.</param>
    /// <param name="fraudDetection">The fraud detection record.</param>
    /// <returns>The result of the action execution.</returns>
    private async Task<ActionExecutionResult> ExecuteAutomatedActionAsync(
        TradeSession session, TradeRiskAnalysis riskAnalysis, AutomatedAction automatedAction, 
        TradeFraudDetection? fraudDetection)
    {
        switch (automatedAction)
        {
            case AutomatedAction.LogOnly:
                return new ActionExecutionResult { IsAllowed = true, Message = null };

            case AutomatedAction.FlagForReview:
                return new ActionExecutionResult 
                { 
                    IsAllowed = true, 
                    Message = "This trade has been flagged for review but is allowed to proceed." 
                };

            case AutomatedAction.BlockTrade:
                return new ActionExecutionResult 
                { 
                    IsAllowed = false, 
                    Message = "This trade has been blocked due to suspicious activity. Please contact an administrator if you believe this is an error." 
                };

            case AutomatedAction.TempRestriction:
                await ApplyTempRestrictionAsync(session.Player1Id, session.Player2Id);
                return new ActionExecutionResult 
                { 
                    IsAllowed = false, 
                    Message = "This trade has been blocked and temporary trading restrictions have been applied. Please contact an administrator." 
                };

            case AutomatedAction.AdminAlert:
                await SendAdminAlertAsync(session, riskAnalysis, fraudDetection);
                return new ActionExecutionResult 
                { 
                    IsAllowed = false, 
                    Message = "This trade has been blocked and administrators have been notified. Please wait for manual review." 
                };

            default:
                return new ActionExecutionResult { IsAllowed = true, Message = null };
        }
    }

    /// <summary>
    ///     Applies temporary trading restrictions to users.
    /// </summary>
    /// <param name="user1Id">First user ID.</param>
    /// <param name="user2Id">Second user ID.</param>
    private async Task ApplyTempRestrictionAsync(ulong user1Id, ulong user2Id)
    {
        // This would integrate with your user restriction system
        // For now, we'll just cache the restrictions
        var database = _cache.Redis.GetDatabase();
        var restrictionKey1 = $"trade_restriction:{user1Id}";
        var restrictionKey2 = $"trade_restriction:{user2Id}";
        
        var expiry = TimeSpan.FromHours(24); // 24-hour restriction
        
        await database.StringSetAsync(restrictionKey1, "temp_restriction", expiry);
        await database.StringSetAsync(restrictionKey2, "temp_restriction", expiry);
    }

    /// <summary>
    ///     Sends an alert to administrators about suspicious activity.
    /// </summary>
    /// <param name="session">The trade session.</param>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <param name="fraudDetection">The fraud detection record.</param>
    private async Task SendAdminAlertAsync(TradeSession session, TradeRiskAnalysis riskAnalysis, 
        TradeFraudDetection? fraudDetection)
    {
        try
        {
            if (_discordClient.GetChannel(AdminChannelId) is not ITextChannel channel) return;

            var embed = new EmbedBuilder()
                .WithTitle("🚨 High-Risk Trade Detected")
                .WithColor(Color.Red)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .AddField("Users", $"<@{session.Player1Id}> ↔ <@{session.Player2Id}>", false)
                .AddField("Risk Score", $"{riskAnalysis.OverallRiskScore:P1}", true)
                .AddField("Risk Level", riskAnalysis.RiskLevel.ToString(), true)
                .AddField("Trade Value", $"${riskAnalysis.SenderTotalValue:N0} ↔ ${riskAnalysis.ReceiverTotalValue:N0}", true)
                .AddField("Fraud Flags", GetFraudFlagsText(riskAnalysis), false);

            if (fraudDetection != null)
            {
                embed.AddField("Detection ID", fraudDetection.Id.ToString(), true);
            }

            await channel.SendMessageAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            // Log error but don't fail the trade process
            Log.Information($"Failed to send admin alert: {ex.Message}");
        }
    }

    /// <summary>
    ///     Determines the primary fraud type based on risk analysis.
    /// </summary>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <returns>The primary fraud type detected.</returns>
    private static FraudType DetermineFraudType(TradeRiskAnalysis riskAnalysis)
    {
        if (riskAnalysis.FlaggedAltAccount) return FraudType.AltAccountTrading;
        if (riskAnalysis.FlaggedRmt) return FraudType.RealMoneyTrading;
        if (riskAnalysis.FlaggedBotActivity) return FraudType.BotAbuse;
        if (riskAnalysis.FlaggedNewbieExploitation) return FraudType.NewbieExploitation;
        if (riskAnalysis.FlaggedUnusualBehavior) return FraudType.UnusualBehavior;
        
        return FraudType.UnusualBehavior; // Default fallback
    }

    /// <summary>
    ///     Gets the list of triggered detection rules.
    /// </summary>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <returns>List of triggered rule names.</returns>
    private static List<string> GetTriggeredRules(TradeRiskAnalysis riskAnalysis)
    {
        var rules = new List<string>();

        if (riskAnalysis.ValueImbalanceScore > 0.5) rules.Add("HighValueImbalance");
        if (riskAnalysis.RelationshipRiskScore > 0.5) rules.Add("SuspiciousRelationship");
        if (riskAnalysis.BehavioralRiskScore > 0.5) rules.Add("UnusualBehavior");
        if (riskAnalysis.AccountAgeRiskScore > 0.5) rules.Add("AccountAgeRisk");
        if (riskAnalysis.FlaggedAltAccount) rules.Add("PotentialAltAccounts");
        if (riskAnalysis.FlaggedRmt) rules.Add("PotentialRMT");
        if (riskAnalysis.FlaggedNewbieExploitation) rules.Add("NewbieExploitation");
        if (riskAnalysis.FlaggedBotActivity) rules.Add("BotActivity");

        return rules;
    }

    /// <summary>
    ///     Gets fraud flags as a list of strings.
    /// </summary>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <returns>List of active fraud flags.</returns>
    private static List<string> GetFraudFlags(TradeRiskAnalysis riskAnalysis)
    {
        var flags = new List<string>();

        if (riskAnalysis.FlaggedAltAccount) flags.Add("Alt Account");
        if (riskAnalysis.FlaggedRmt) flags.Add("RMT");
        if (riskAnalysis.FlaggedNewbieExploitation) flags.Add("Newbie Exploitation");
        if (riskAnalysis.FlaggedUnusualBehavior) flags.Add("Unusual Behavior");
        if (riskAnalysis.FlaggedBotActivity) flags.Add("Bot Activity");

        return flags;
    }

    /// <summary>
    ///     Gets fraud flags as a formatted text string.
    /// </summary>
    /// <param name="riskAnalysis">The risk analysis results.</param>
    /// <returns>Formatted fraud flags text.</returns>
    private static string GetFraudFlagsText(TradeRiskAnalysis riskAnalysis)
    {
        var flags = GetFraudFlags(riskAnalysis);
        return flags.Any() ? string.Join(", ", flags) : "None";
    }

    /// <summary>
    ///     Calculates the age of a Discord account in days from its user ID.
    /// </summary>
    /// <param name="userId">The Discord user ID.</param>
    /// <param name="currentTime">The current time for calculation.</param>
    /// <returns>The account age in days.</returns>
    private static double CalculateAccountAgeFromUserId(ulong userId, DateTime currentTime)
    {
        // Discord snowflake epoch (January 1, 2015 00:00:00 UTC)
        var discordEpoch = new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Extract timestamp from Discord snowflake
        var timestamp = (userId >> 22) + 1420070400000UL; // Discord epoch in milliseconds
        var accountCreated = discordEpoch.AddMilliseconds(timestamp - 1420070400000UL);
        
        return (currentTime - accountCreated).TotalDays;
    }
}

/// <summary>
///     Represents the result of fraud detection analysis.
/// </summary>
public class FraudDetectionResult
{
    /// <summary>
    ///     Gets or sets whether the trade is allowed to proceed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    ///     Gets or sets the calculated risk score.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the risk level.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    ///     Gets or sets the automated action taken.
    /// </summary>
    public AutomatedAction AutomatedAction { get; set; }

    /// <summary>
    ///     Gets or sets any message to display to users.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///     Gets or sets the list of fraud flags detected.
    /// </summary>
    public List<string> FraudFlags { get; set; } = new();

    /// <summary>
    ///     Gets or sets whether this trade requires admin review.
    /// </summary>
    public bool RequiresAdminReview { get; set; }
}

/// <summary>
///     Represents the result of executing an automated action.
/// </summary>
internal class ActionExecutionResult
{
    /// <summary>
    ///     Gets or sets whether the trade is allowed to proceed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    ///     Gets or sets any message to display to users.
    /// </summary>
    public string? Message { get; set; }
}