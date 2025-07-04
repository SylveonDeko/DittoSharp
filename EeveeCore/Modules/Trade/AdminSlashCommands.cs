using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Database.Linq.Models.Game;
using EeveeCore.Modules.Trade.Services;
using LinqToDB;

namespace EeveeCore.Modules.Trade;

/// <summary>
///     Provides admin commands for managing trade fraud detection and investigation.
/// </summary>
[Group("tradeadmin", "Admin commands for trade fraud detection")]
[RequireUserPermission(GuildPermission.Administrator)]
public class TradeAdminSlashCommands : EeveeCoreSlashModuleBase<TradeFraudDetectionService>
{
    private readonly LinqToDbConnectionProvider _context;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeAdminSlashCommands" /> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TradeAdminSlashCommands(LinqToDbConnectionProvider context)
    {
        _context = context;
    }

    /// <summary>
    ///     Shows recent high-risk trade detections.
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("recent", "Show recent high-risk trade detections")]
    public async Task ShowRecentDetections(
        [Summary("hours", "Hours to look back (default: 24)")] int hours = 24)
    {
        await DeferAsync(ephemeral: true);

        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        
        await using var db = await _context.GetConnectionAsync();

        var detections = await db.TradeFraudDetections
            .Where(d => d.DetectionTimestamp >= cutoffTime && d.RiskScore >= 0.4)
            .OrderByDescending(d => d.RiskScore)
            .Take(10)
            .Select(d => new
            {
                d.Id,
                d.PrimaryUserId,
                d.SecondaryUserId,
                d.RiskScore,
                d.FraudType,
                d.InvestigationStatus,
                d.DetectionTimestamp,
                d.TradeBlocked
            })
            .ToListAsync();

        if (!detections.Any())
        {
            await FollowupAsync($"No high-risk trade detections found in the last {hours} hours.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"🚨 High-Risk Trade Detections (Last {hours}h)")
            .WithColor(Color.Red)
            .WithTimestamp(DateTimeOffset.UtcNow);

        foreach (var detection in detections)
        {
            var status = detection.TradeBlocked ? "🚫 BLOCKED" : "⚠️ FLAGGED";
            var statusColor = detection.InvestigationStatus switch
            {
                InvestigationStatus.Pending => "🔍",
                InvestigationStatus.InProgress => "⏳",
                InvestigationStatus.Completed => "✅",
                InvestigationStatus.Dismissed => "❌",
                _ => "❓"
            };

            embed.AddField(
                $"{status} Detection #{detection.Id} {statusColor}",
                $"**Users:** <@{detection.PrimaryUserId}> ↔ <@{detection.SecondaryUserId}>\n" +
                $"**Risk:** {detection.RiskScore:P1} | **Type:** {detection.FraudType}\n" +
                $"**Time:** <t:{((DateTimeOffset)detection.DetectionTimestamp).ToUnixTimeSeconds()}:R>",
                inline: false);
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    /// <summary>
    ///     Investigates a specific fraud detection case.
    /// </summary>
    /// <param name="detectionId">The ID of the detection to investigate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("investigate", "Investigate a specific fraud detection case")]
    public async Task InvestigateDetection(
        [Summary("detection_id", "The detection ID to investigate")] int detectionId)
    {
        await DeferAsync(ephemeral: true);

        await using var db = await _context.GetConnectionAsync();

        var detection = await db.TradeFraudDetections
            .LoadWithAsTable(d => d.SuspiciousTradeAnalytics)
            .FirstOrDefaultAsync(d => d.Id == detectionId);

        if (detection == null)
        {
            await FollowupAsync($"Detection #{detectionId} not found.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"🔍 Investigation: Detection #{detectionId}")
            .WithColor(detection.TradeBlocked ? Color.Red : Color.Orange)
            .WithTimestamp(detection.DetectionTimestamp)
            .AddField("Users Involved", 
                $"**Primary:** <@{detection.PrimaryUserId}>\n" +
                $"**Secondary:** {(detection.SecondaryUserId.HasValue ? $"<@{detection.SecondaryUserId}>" : "None")}", 
                true)
            .AddField("Risk Assessment",
                $"**Score:** {detection.RiskScore:P1}\n" +
                $"**Confidence:** {detection.ConfidenceLevel:P1}\n" +
                $"**Type:** {detection.FraudType}",
                true)
            .AddField("Actions Taken",
                $"**Automated:** {detection.AutomatedAction}\n" +
                $"**Trade Blocked:** {(detection.TradeBlocked ? "Yes" : "No")}\n" +
                $"**Status:** {detection.InvestigationStatus}",
                true);

        if (detection.SuspiciousTradeAnalytics != null)
        {
            var analytics = detection.SuspiciousTradeAnalytics;
            embed.AddField("Trade Analysis",
                $"**Sender Value:** ${analytics.SenderTotalValue:N0}\n" +
                $"**Receiver Value:** ${analytics.ReceiverTotalValue:N0}\n" +
                $"**Value Ratio:** {analytics.ValueRatio:F2}x\n" +
                $"**Imbalance Score:** {analytics.ValueImbalanceScore:P1}",
                false);

            var flags = new List<string>();
            if (analytics.FlaggedAltAccount) flags.Add("Alt Account");
            if (analytics.FlaggedRmt) flags.Add("RMT");
            if (analytics.FlaggedNewbieExploitation) flags.Add("Newbie Exploitation");
            if (analytics.FlaggedUnusualBehavior) flags.Add("Unusual Behavior");
            if (analytics.FlaggedBotActivity) flags.Add("Bot Activity");

            embed.AddField("Detection Flags", 
                flags.Any() ? string.Join(", ", flags) : "None", 
                true);
        }

        if (!string.IsNullOrEmpty(detection.AdminNotes))
        {
            embed.AddField("Admin Notes", detection.AdminNotes, false);
        }

        var components = new ComponentBuilder()
            .WithButton("Mark Legitimate", $"fraud_legitimate:{detectionId}", ButtonStyle.Success)
            .WithButton("Confirm Fraud", $"fraud_confirm:{detectionId}", ButtonStyle.Danger)
            .WithButton("Needs Review", $"fraud_review:{detectionId}", ButtonStyle.Secondary)
            .Build();

        await FollowupAsync(embed: embed.Build(), components: components, ephemeral: true);
    }

    /// <summary>
    ///     Shows user trade relationship information.
    /// </summary>
    /// <param name="user1">First user.</param>
    /// <param name="user2">Second user.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("relationship", "Show trade relationship between two users")]
    public async Task ShowRelationship(
        [Summary("user1", "First user")] IUser user1,
        [Summary("user2", "Second user")] IUser user2)
    {
        await DeferAsync(ephemeral: true);

        var (lowerId, higherId) = user1.Id < user2.Id ? (user1.Id, user2.Id) : (user2.Id, user1.Id);

        await using var db = await _context.GetConnectionAsync();

        var relationship = await db.UserTradeRelationships
            .FirstOrDefaultAsync(r => r.User1Id == lowerId && r.User2Id == higherId);

        if (relationship == null)
        {
            await FollowupAsync($"No trade relationship found between {user1.Mention} and {user2.Mention}.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"📊 Trade Relationship Analysis")
            .WithDescription($"**{user1.Mention} ↔ {user2.Mention}**")
            .WithColor(relationship.RelationshipRiskScore > 0.6 ? Color.Red : 
                      relationship.RelationshipRiskScore > 0.3 ? Color.Orange : Color.Green)
            .WithTimestamp(relationship.LastUpdated)
            .AddField("Trade Statistics",
                $"**Total Trades:** {relationship.TotalTrades}\n" +
                $"**First Trade:** <t:{((DateTimeOffset)relationship.FirstTradeTimestamp).ToUnixTimeSeconds()}:d>\n" +
                $"**Last Trade:** <t:{((DateTimeOffset)relationship.LastTradeTimestamp).ToUnixTimeSeconds()}:R>\n" +
                $"**Frequency:** {relationship.TradingFrequencyScore:F2} trades/day",
                true)
            .AddField("Value Analysis",
                $"**{(lowerId == user1.Id ? user1.Username : user2.Username)} Given:** ${relationship.User1TotalGivenValue:N0}\n" +
                $"**{(lowerId == user1.Id ? user2.Username : user1.Username)} Given:** ${relationship.User2TotalGivenValue:N0}\n" +
                $"**Imbalance Ratio:** {relationship.ValueImbalanceRatio:F2}x\n" +
                $"**Balanced Trades:** {relationship.BalancedTrades}/{relationship.TotalTrades}",
                true)
            .AddField("Risk Assessment",
                $"**Risk Score:** {relationship.RelationshipRiskScore:P1}\n" +
                $"**Account Age Diff:** {relationship.AccountAgeDifferenceDays} days\n" +
                $"**Suspicious Timing:** {(relationship.SuspiciousTimingPattern ? "Yes" : "No")}\n" +
                $"**Whitelisted:** {(relationship.Whitelisted ? "Yes" : "No")}",
                true);

        var flags = new List<string>();
        if (relationship.FlaggedPotentialAlts) flags.Add("Potential Alts");
        if (relationship.FlaggedPotentialRmt) flags.Add("Potential RMT");
        if (relationship.FlaggedNewbieExploitation) flags.Add("Newbie Exploitation");

        if (flags.Any())
        {
            embed.AddField("⚠️ Flags", string.Join(", ", flags), false);
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    /// <summary>
    ///     Whitelists a user relationship to bypass fraud detection.
    /// </summary>
    /// <param name="user1">First user.</param>
    /// <param name="user2">Second user.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("whitelist", "Whitelist a user relationship to bypass fraud detection")]
    public async Task WhitelistRelationship(
        [Summary("user1", "First user")] IUser user1,
        [Summary("user2", "Second user")] IUser user2)
    {
        await DeferAsync(ephemeral: true);

        var (lowerId, higherId) = user1.Id < user2.Id ? (user1.Id, user2.Id) : (user2.Id, user1.Id);

        await using var db = await _context.GetConnectionAsync();

        var relationship = await db.UserTradeRelationships
            .FirstOrDefaultAsync(r => r.User1Id == lowerId && r.User2Id == higherId);

        if (relationship == null)
        {
            await FollowupAsync($"No trade relationship found between {user1.Mention} and {user2.Mention}. They must trade at least once first.", ephemeral: true);
            return;
        }

        await db.UserTradeRelationships
            .Where(r => r.User1Id == lowerId && r.User2Id == higherId)
            .Set(r => r.Whitelisted, true)
            .Set(r => r.AdminReviewed, true)
            .Set(r => r.AdminVerdict, true)
            .Set(r => r.AdminNotes, $"Whitelisted by {ctx.User.Username} on {DateTime.UtcNow:yyyy-MM-dd}")
            .Set(r => r.LastUpdated, DateTime.UtcNow)
            .UpdateAsync();

        await FollowupAsync($"✅ Trade relationship between {user1.Mention} and {user2.Mention} has been whitelisted. Future trades will bypass fraud detection.", ephemeral: true);
    }

    /// <summary>
    ///     Shows fraud detection statistics.
    /// </summary>
    /// <param name="days">Number of days to analyze (default: 7).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("stats", "Show fraud detection statistics")]
    public async Task ShowStats(
        [Summary("days", "Number of days to analyze (default: 7)")] int days = 7)
    {
        await DeferAsync(ephemeral: true);

        var cutoffTime = DateTime.UtcNow.AddDays(-days);

        await using var db = await _context.GetConnectionAsync();

        var stats = await db.TradeFraudDetections
            .Where(d => d.DetectionTimestamp >= cutoffTime)
            .GroupBy(d => 1)
            .Select(g => new
            {
                TotalDetections = g.Count(),
                BlockedTrades = g.Count(d => d.TradeBlocked),
                HighRisk = g.Count(d => d.RiskScore >= 0.6),
                MediumRisk = g.Count(d => d.RiskScore >= 0.4 && d.RiskScore < 0.6),
                LowRisk = g.Count(d => d.RiskScore < 0.4),
                AltAccountFlags = g.Count(d => d.FraudType == FraudType.AltAccountTrading),
                RmtFlags = g.Count(d => d.FraudType == FraudType.RealMoneyTrading),
                NewbieExploitFlags = g.Count(d => d.FraudType == FraudType.NewbieExploitation)
            })
            .FirstOrDefaultAsync();

        if (stats == null || stats.TotalDetections == 0)
        {
            await FollowupAsync($"No fraud detections found in the last {days} days.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"📈 Fraud Detection Statistics (Last {days} days)")
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("Detection Summary",
                $"**Total Detections:** {stats.TotalDetections}\n" +
                $"**Blocked Trades:** {stats.BlockedTrades}\n" +
                $"**Block Rate:** {(stats.TotalDetections > 0 ? (double)stats.BlockedTrades / stats.TotalDetections : 0):P1}",
                true)
            .AddField("Risk Distribution",
                $"**High Risk:** {stats.HighRisk}\n" +
                $"**Medium Risk:** {stats.MediumRisk}\n" +
                $"**Low Risk:** {stats.LowRisk}",
                true)
            .AddField("Fraud Types",
                $"**Alt Accounts:** {stats.AltAccountFlags}\n" +
                $"**RMT:** {stats.RmtFlags}\n" +
                $"**Newbie Exploit:** {stats.NewbieExploitFlags}",
                true);

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}