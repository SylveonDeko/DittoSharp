using EeveeCore.Database.Linq.Models.Game;
using EeveeCore.Modules.Trade.Models;
using LinqToDB;

namespace EeveeCore.Modules.Trade.Services;

/// <summary>
///     Service for calculating risk scores for trades to detect potential fraud.
///     This service analyzes various factors to determine the likelihood of fraudulent activity.
/// </summary>
public class TradeRiskScorer : INService
{
    private readonly LinqToDbConnectionProvider _context;
    private readonly TradeValueCalculator _valueCalculator;
    private readonly IDataCache _cache;

    // Risk scoring thresholds and weights
    private const double ValueImbalanceWeight = 0.35;
    private const double RelationshipWeight = 0.25;
    private const double BehavioralWeight = 0.20;
    private const double AccountAgeWeight = 0.20;

    // Account age thresholds (in days)
    private const int NewAccountThreshold = 7;
    private const int SuspiciousAccountThreshold = 30;

    // Trading frequency thresholds
    private const double HighFrequencyThreshold = 10.0; // trades per day
    private const double SuspiciousFrequencyThreshold = 20.0; // trades per day

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeRiskScorer" /> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="valueCalculator">The trade value calculator service.</param>
    /// <param name="cache">The cache service.</param>
    public TradeRiskScorer(LinqToDbConnectionProvider context, TradeValueCalculator valueCalculator, IDataCache cache)
    {
        _context = context;
        _valueCalculator = valueCalculator;
        _cache = cache;
    }

    /// <summary>
    ///     Calculates a comprehensive risk score for a trade.
    /// </summary>
    /// <param name="session">The trade session to analyze.</param>
    /// <returns>A risk analysis result containing all risk scores and flags.</returns>
    public async Task<TradeRiskAnalysis> AnalyzeTradeRiskAsync(TradeSession session)
    {
        var analysis = new TradeRiskAnalysis
        {
            TradeSessionId = session.SessionId,
            AnalysisTimestamp = DateTime.UtcNow
        };

        // Calculate trade values
        var senderValue = await _valueCalculator.CalculateUserTradeValueAsync(session, session.Player1Id);
        var receiverValue = await _valueCalculator.CalculateUserTradeValueAsync(session, session.Player2Id);

        analysis.SenderTotalValue = senderValue;
        analysis.ReceiverTotalValue = receiverValue;
        analysis.ValueDifference = Math.Abs(senderValue - receiverValue);
        analysis.ValueRatio = senderValue == 0 || receiverValue == 0 ? 
            (double)Math.Max(senderValue, receiverValue) : 
            (double)(Math.Max(senderValue, receiverValue) / Math.Min(senderValue, receiverValue));

        // Calculate individual risk scores
        analysis.ValueImbalanceScore = await CalculateValueImbalanceRiskAsync(senderValue, receiverValue, session.Player1Id, session.Player2Id);
        analysis.RelationshipRiskScore = await CalculateRelationshipRiskAsync(session.Player1Id, session.Player2Id);
        analysis.BehavioralRiskScore = await CalculateBehavioralRiskAsync(session);
        analysis.AccountAgeRiskScore = await CalculateAccountAgeRiskAsync(session.Player1Id, session.Player2Id);

        // Calculate overall risk score
        analysis.OverallRiskScore = CalculateOverallRiskScore(analysis);

        // Set detection flags
        await SetDetectionFlagsAsync(analysis, session);

        return analysis;
    }

    /// <summary>
    ///     Calculates the value imbalance risk score.
    /// </summary>
    /// <param name="senderValue">Total value given by sender.</param>
    /// <param name="receiverValue">Total value given by receiver.</param>
    /// <param name="user1Id">First user ID for relationship context.</param>
    /// <param name="user2Id">Second user ID for relationship context.</param>
    /// <returns>Risk score from 0.0 to 1.0.</returns>
    private async Task<double> CalculateValueImbalanceRiskAsync(decimal senderValue, decimal receiverValue, ulong user1Id, ulong user2Id)
    {
        var baseScore = TradeValueCalculator.CalculateValueImbalanceScore(senderValue, receiverValue);

        // Check if this is a gift (one-sided trade) and adjust based on relationship
        if (senderValue == 0 || receiverValue == 0)
        {
            var relationship = await GetOrCreateUserRelationshipAsync(user1Id, user2Id);
            
            // Reduce gift risk for established relationships
            if (relationship != null)
            {
                // Users with multiple trades together are less suspicious for gifts
                if (relationship.TotalTrades >= 5)
                {
                    baseScore *= 0.6; // 40% reduction for established relationship
                }
                else if (relationship.TotalTrades >= 2)
                {
                    baseScore *= 0.8; // 20% reduction for some history
                }
                
                // Further reduce for balanced trading history
                if (relationship.BalancedTrades > relationship.TotalTrades * 0.5)
                {
                    baseScore *= 0.7; // Users who usually trade fairly are less suspicious
                }
            }
        }

        // Increase risk for extremely high-value one-sided trades
        var maxValue = Math.Max(senderValue, receiverValue);
        if (maxValue > 1000000) // 1M+ value trades
        {
            baseScore = Math.Min(1.0, baseScore * 1.5);
        }
        else if (maxValue > 100000) // 100K+ value trades
        {
            baseScore = Math.Min(1.0, baseScore * 1.2);
        }

        return baseScore;
    }

    /// <summary>
    ///     Calculates the relationship risk score between two users.
    /// </summary>
    /// <param name="user1Id">First user ID.</param>
    /// <param name="user2Id">Second user ID.</param>
    /// <returns>Risk score from 0.0 to 1.0.</returns>
    private async Task<double> CalculateRelationshipRiskAsync(ulong user1Id, ulong user2Id)
    {
        var relationship = await GetOrCreateUserRelationshipAsync(user1Id, user2Id);
        if (relationship == null)
        {
            return 0.0; // First trade between users
        }

        // Whitelisted relationships have minimal risk (for legitimate friends, competitive players, etc.)
        if (relationship.Whitelisted)
        {
            return 0.05; // Very low risk for admin-approved relationships
        }

        var riskScore = 0.0;

        // High trading frequency risk
        if (relationship.TradingFrequencyScore > HighFrequencyThreshold)
        {
            riskScore += 0.3;
        }
        if (relationship.TradingFrequencyScore > SuspiciousFrequencyThreshold)
        {
            riskScore += 0.3;
        }

        // Value imbalance pattern risk
        if (relationship.ValueImbalanceRatio > 5.0)
        {
            riskScore += 0.4;
        }
        else if (relationship.ValueImbalanceRatio > 2.0)
        {
            riskScore += 0.2;
        }

        // Suspicious creation timing
        if (relationship.SuspiciousCreationTiming)
        {
            riskScore += 0.3;
        }

        // Timing pattern analysis
        if (relationship.SuspiciousTimingPattern)
        {
            riskScore += 0.2;
        }

        return Math.Min(1.0, riskScore);
    }

    /// <summary>
    ///     Calculates the behavioral risk score for a trade.
    /// </summary>
    /// <param name="session">The trade session to analyze.</param>
    /// <returns>Risk score from 0.0 to 1.0.</returns>
    private async Task<double> CalculateBehavioralRiskAsync(TradeSession session)
    {
        var riskScore = 0.0;

        // Quick acceptance risk (if we track confirmation times)
        var confirmationTime = session.LastUpdated - session.CreatedAt;
        if (confirmationTime.TotalSeconds < 30) // Confirmed in under 30 seconds
        {
            riskScore += 0.3;
        }

        // Recent trading pattern analysis
        var recentTrades = await GetRecentTradeCountAsync(session.Player1Id, session.Player2Id, TimeSpan.FromHours(24));
        if (recentTrades > 10) // More than 10 trades in 24 hours
        {
            riskScore += 0.4;
        }
        else if (recentTrades > 5) // More than 5 trades in 24 hours
        {
            riskScore += 0.2;
        }

        // Empty trade risk (no items from one side)
        if (!session.GetEntriesBy(session.Player1Id).Any() || !session.GetEntriesBy(session.Player2Id).Any())
        {
            riskScore += 0.3;
        }

        return Math.Min(1.0, riskScore);
    }

    /// <summary>
    ///     Calculates the account age risk score.
    /// </summary>
    /// <param name="user1Id">First user ID.</param>
    /// <param name="user2Id">Second user ID.</param>
    /// <returns>Risk score from 0.0 to 1.0.</returns>
    private async Task<double> CalculateAccountAgeRiskAsync(ulong user1Id, ulong user2Id)
    {
        var now = DateTime.UtcNow;
        
        // Calculate account ages from Discord user IDs (Discord snowflake contains creation timestamp)
        var user1AgeDays = CalculateAccountAgeFromUserId(user1Id, now);
        var user2AgeDays = CalculateAccountAgeFromUserId(user2Id, now);

        var riskScore = 0.0;

        // New account risk
        if (user1AgeDays < NewAccountThreshold || user2AgeDays < NewAccountThreshold)
        {
            riskScore += 0.5;
        }
        else if (user1AgeDays < SuspiciousAccountThreshold || user2AgeDays < SuspiciousAccountThreshold)
        {
            riskScore += 0.3;
        }

        // Similar creation time risk (potential alts)
        var ageDifference = Math.Abs(user1AgeDays - user2AgeDays);
        if (ageDifference < 1) // Created within 1 day
        {
            riskScore += 0.4;
        }
        else if (ageDifference < 7) // Created within 1 week
        {
            riskScore += 0.2;
        }

        // Experienced user trading with new user (potential exploitation)
        var minAge = Math.Min(user1AgeDays, user2AgeDays);
        var maxAge = Math.Max(user1AgeDays, user2AgeDays);
        if (minAge < NewAccountThreshold && maxAge > 90) // New user + experienced user
        {
            riskScore += 0.3;
        }

        return Math.Min(1.0, riskScore);
    }

    /// <summary>
    ///     Calculates the overall risk score from component scores.
    /// </summary>
    /// <param name="analysis">The risk analysis containing component scores.</param>
    /// <returns>Overall risk score from 0.0 to 1.0.</returns>
    private static double CalculateOverallRiskScore(TradeRiskAnalysis analysis)
    {
        return (analysis.ValueImbalanceScore * ValueImbalanceWeight) +
               (analysis.RelationshipRiskScore * RelationshipWeight) +
               (analysis.BehavioralRiskScore * BehavioralWeight) +
               (analysis.AccountAgeRiskScore * AccountAgeWeight);
    }

    /// <summary>
    ///     Sets detection flags based on risk analysis.
    /// </summary>
    /// <param name="analysis">The risk analysis to update.</param>
    /// <param name="session">The trade session being analyzed.</param>
    private async Task SetDetectionFlagsAsync(TradeRiskAnalysis analysis, TradeSession session)
    {
        // Alt account detection
        analysis.FlaggedAltAccount = analysis is { AccountAgeRiskScore: > 0.6, RelationshipRiskScore: > 0.5 };

        // RMT detection (high value + imbalanced + new relationship)
        var maxValue = Math.Max(analysis.SenderTotalValue, analysis.ReceiverTotalValue);
        analysis.FlaggedRmt = maxValue > 500000 && analysis is { ValueImbalanceScore: > 0.7, RelationshipRiskScore: < 0.3 };

        // Newbie exploitation detection
        analysis.FlaggedNewbieExploitation = analysis is { AccountAgeRiskScore: > 0.5, ValueImbalanceScore: > 0.6 };

        // Unusual behavior detection
        analysis.FlaggedUnusualBehavior = analysis.BehavioralRiskScore > 0.6;

        // Bot activity detection (high frequency + perfect timing patterns)
        var relationship = await GetOrCreateUserRelationshipAsync(session.Player1Id, session.Player2Id);
        analysis.FlaggedBotActivity = relationship?.SuspiciousTimingPattern == true && analysis.BehavioralRiskScore > 0.5;
    }

    /// <summary>
    ///     Gets or creates a user trade relationship record.
    /// </summary>
    /// <param name="user1Id">First user ID.</param>
    /// <param name="user2Id">Second user ID.</param>
    /// <returns>The user relationship record or null if this is their first interaction.</returns>
    private async Task<UserTradeRelationship?> GetOrCreateUserRelationshipAsync(ulong user1Id, ulong user2Id)
    {
        // Ensure consistent ordering
        var (lowerId, higherId) = user1Id < user2Id ? (user1Id, user2Id) : (user2Id, user1Id);

        await using var db = await _context.GetConnectionAsync();
        return await db.UserTradeRelationships
            .FirstOrDefaultAsync(r => r.User1Id == lowerId && r.User2Id == higherId);
    }

    /// <summary>
    ///     Gets the count of recent trades between two users.
    /// </summary>
    /// <param name="user1Id">First user ID.</param>
    /// <param name="user2Id">Second user ID.</param>
    /// <param name="timeSpan">The time period to check.</param>
    /// <returns>Number of trades in the specified time period.</returns>
    private async Task<int> GetRecentTradeCountAsync(ulong user1Id, ulong user2Id, TimeSpan timeSpan)
    {
        var cutoffTime = DateTime.UtcNow - timeSpan;

        await using var db = await _context.GetConnectionAsync();

        return await db.TradeLogs
            .Where(t => t.Time >= cutoffTime &&
                       ((t.SenderId == user1Id && t.ReceiverId == user2Id) ||
                        (t.SenderId == user2Id && t.ReceiverId == user1Id)))
            .CountAsync();
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
///     Represents the result of a trade risk analysis.
/// </summary>
public class TradeRiskAnalysis
{
    /// <summary>
    ///     Gets or sets the trade session ID being analyzed.
    /// </summary>
    public Guid TradeSessionId { get; set; }

    /// <summary>
    ///     Gets or sets when this analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }

    #region Risk Scores

    /// <summary>
    ///     Gets or sets the overall risk score (0.0 to 1.0).
    /// </summary>
    public double OverallRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the value imbalance risk score.
    /// </summary>
    public double ValueImbalanceScore { get; set; }

    /// <summary>
    ///     Gets or sets the relationship risk score.
    /// </summary>
    public double RelationshipRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the behavioral risk score.
    /// </summary>
    public double BehavioralRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the account age risk score.
    /// </summary>
    public double AccountAgeRiskScore { get; set; }

    #endregion

    #region Trade Values

    /// <summary>
    ///     Gets or sets the total value of items given by the sender.
    /// </summary>
    public decimal SenderTotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the total value of items given by the receiver.
    /// </summary>
    public decimal ReceiverTotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the value ratio (higher / lower).
    /// </summary>
    public double ValueRatio { get; set; }

    /// <summary>
    ///     Gets or sets the absolute value difference.
    /// </summary>
    public decimal ValueDifference { get; set; }

    #endregion

    #region Detection Flags

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential alt account activity.
    /// </summary>
    public bool FlaggedAltAccount { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential RMT.
    /// </summary>
    public bool FlaggedRmt { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential newbie exploitation.
    /// </summary>
    public bool FlaggedNewbieExploitation { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for unusual behavior.
    /// </summary>
    public bool FlaggedUnusualBehavior { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential bot activity.
    /// </summary>
    public bool FlaggedBotActivity { get; set; }

    #endregion

    /// <summary>
    ///     Gets whether any fraud flags are set.
    /// </summary>
    public bool HasFraudFlags => FlaggedAltAccount || FlaggedRmt || FlaggedNewbieExploitation || 
                                FlaggedUnusualBehavior || FlaggedBotActivity;

    /// <summary>
    ///     Gets the risk level based on the overall score.
    /// </summary>
    public RiskLevel RiskLevel => OverallRiskScore switch
    {
        >= 0.8 => RiskLevel.Critical,
        >= 0.6 => RiskLevel.High,
        >= 0.4 => RiskLevel.Medium,
        >= 0.2 => RiskLevel.Low,
        _ => RiskLevel.Minimal
    };
}

/// <summary>
///     Represents the risk level of a trade.
/// </summary>
public enum RiskLevel
{
    /// <summary>Minimal risk</summary>
    Minimal = 1,
    
    /// <summary>Low risk</summary>
    Low = 2,
    
    /// <summary>Medium risk</summary>
    Medium = 3,
    
    /// <summary>High risk</summary>
    High = 4,
    
    /// <summary>Critical risk</summary>
    Critical = 5
}