using Discord.Interactions;
using EeveeCore.Common.Attributes.Interactions;
using EeveeCore.Common.Enums;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Database.Linq.Models.Game;
using EeveeCore.Modules.Trade.Services;
using LinqToDB;

namespace EeveeCore.Modules.Trade;

/// <summary>
///     Provides admin commands for managing trade fraud detection and investigation.
/// </summary>
[Group("tradeadmin", "Admin commands for trade fraud detection")]
public class TradeAdminSlashCommands : EeveeCoreSlashModuleBase<FraudDetectionService>
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
    [RequireStaff(StaffRank.Mod)]
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
    [RequireStaff(StaffRank.Investigator)]
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
    [RequireStaff(StaffRank.Mod)]
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
    [RequireStaff(StaffRank.Admin)]
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
    [RequireStaff(StaffRank.Mod)]
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

    /// <summary>
    ///     Removes trade ban from a user without whitelisting them.
    ///     The user will still be subject to fraud detection on future trades.
    /// </summary>
    /// <param name="user">The user to unban from trading.</param>
    /// <param name="reason">Reason for removing the trade ban.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("detradeban", "Remove trade ban from a user (keeps fraud detection active)")]
    [RequireStaff(StaffRank.Admin)]
    public async Task DetradeBan(
        [Summary("user", "The user to unban from trading")] IUser user,
        [Summary("reason", "Reason for removing the trade ban")] string reason = "Admin discretion")
    {
        await DeferAsync(ephemeral: true);

        await using var db = await _context.GetConnectionAsync();

        // Check if user is actually banned
        var userData = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == user.Id);

        if (userData == null)
        {
            await FollowupAsync($"User {user.Mention} not found in the database.", ephemeral: true);
            return;
        }

        if (userData.TradeBanned != true && userData.MarketBanned != true)
        {
            await FollowupAsync($"User {user.Mention} is not currently trade banned.", ephemeral: true);
            return;
        }

        // Store ban history for audit trail
        var banHistory = $"Previous ban: {userData.TradeBanReason ?? userData.MarketBanReason} " +
                        $"(banned on {userData.TradeBanDate ?? userData.MarketBanDate:yyyy-MM-dd})";

        // Remove trade and market bans
        var updated = await db.Users
            .Where(u => u.UserId == user.Id)
            .Set(u => u.TradeBanned, false)
            .Set(u => u.TradeBanReason, "")
            .Set(u => u.TradeBanDate, DateTime.Now)
            .Set(u => u.MarketBanned, false)
            .Set(u => u.MarketBanReason, "")
            .Set(u => u.MarketBanDate, DateTime.Now)
            .UpdateAsync();

        if (updated > 0)
        {
            // Log the unban action
            Serilog.Log.Warning("🔓 Trade ban removed by admin: User={UserId} ({Username}), Admin={AdminId} ({AdminUsername}), Reason={Reason}, PreviousBan={PreviousBan}",
                user.Id, user.Username, ctx.User.Id, ctx.User.Username, reason, banHistory);

            var embed = new EmbedBuilder()
                .WithTitle("✅ Trade Ban Removed")
                .WithColor(Color.Green)
                .WithDescription($"**User:** {user.Mention} ({user.Username}#{user.Discriminator})")
                .AddField("Action Taken", 
                    "• Trade ban removed\n" +
                    "• Market ban removed\n" +
                    "• ⚠️ **Fraud detection remains ACTIVE**\n" +
                    "• User's future trades will still be analyzed", 
                    false)
                .AddField("Admin", ctx.User.Mention, true)
                .AddField("Reason", reason, true)
                .AddField("Previous Ban Info", banHistory, false)
                .WithFooter("Note: This does NOT whitelist the user - they are still subject to fraud detection")
                .WithTimestamp(DateTimeOffset.UtcNow);

            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
        else
        {
            await FollowupAsync($"Failed to remove trade ban for {user.Mention}. Please try again.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Applies a trade ban to a user with a specified reason.
    /// </summary>
    /// <param name="user">The user to ban from trading.</param>
    /// <param name="reason">Reason for the trade ban.</param>
    /// <param name="marketBan">Also ban from market (default: true).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("tradeban", "Apply a trade ban to a user")]
    [RequireStaff(StaffRank.Admin)]
    public async Task TradeBan(
        [Summary("user", "The user to ban from trading")] IUser user,
        [Summary("reason", "Reason for the trade ban")] string reason,
        [Summary("market_ban", "Also ban from market (default: true)")] bool marketBan = true)
    {
        await DeferAsync(ephemeral: true);

        await using var db = await _context.GetConnectionAsync();

        // Check if user already exists, create if not
        var userData = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == user.Id);

        var banDate = DateTime.UtcNow;
        var updated = 0;

        if (userData == null)
        {
            // Create new user record with ban
            var newUser = new Database.Linq.Models.Bot.User
            {
                UserId = user.Id,
                TradeBanned = true,
                TradeBanReason = reason,
                TradeBanDate = banDate,
                MarketBanned = marketBan,
                MarketBanReason = marketBan ? reason : null,
                MarketBanDate = marketBan ? banDate : null
            };

            await db.InsertAsync(newUser);
            updated = 1;
        }
        else
        {
            // Update existing user
            updated = await db.Users
                .Where(u => u.UserId == user.Id)
                .Set(u => u.TradeBanned, true)
                .Set(u => u.TradeBanReason, reason)
                .Set(u => u.TradeBanDate, banDate)
                .Set(u => u.MarketBanned, marketBan)
                .Set(u => u.MarketBanReason, marketBan ? reason : userData.MarketBanReason)
                .Set(u => u.MarketBanDate, marketBan ? banDate : userData.MarketBanDate)
                .UpdateAsync();
        }

        if (updated > 0)
        {
            // Log the ban action
            Serilog.Log.Warning("🔨 Trade ban applied by admin: User={UserId} ({Username}), Admin={AdminId} ({AdminUsername}), Reason={Reason}, MarketBan={MarketBan}",
                user.Id, user.Username, ctx.User.Id, ctx.User.Username, reason, marketBan);

            var embed = new EmbedBuilder()
                .WithTitle("🔨 Trade Ban Applied")
                .WithColor(Color.Red)
                .WithDescription($"**User:** {user.Mention} ({user.Username}#{user.Discriminator})")
                .AddField("Restrictions Applied", 
                    $"• Trade ban: **Yes**\n" +
                    $"• Market ban: **{(marketBan ? "Yes" : "No")}**", 
                    true)
                .AddField("Admin", ctx.User.Mention, true)
                .AddField("Reason", reason, false)
                .WithTimestamp(DateTimeOffset.UtcNow);

            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }
        else
        {
            await FollowupAsync($"Failed to apply trade ban to {user.Mention}. Please try again.", ephemeral: true);
        }
    }
}