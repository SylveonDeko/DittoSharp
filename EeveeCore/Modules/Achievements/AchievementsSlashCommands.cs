using System.Text;
using Discord.Interactions;
using EeveeCore.Common.Collections;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Achievements.Common;
using EeveeCore.Modules.Achievements.Services;
using LinqToDB;
using Serilog;

namespace EeveeCore.Modules.Achievements;

/// <summary>
///     Provides Discord slash commands for viewing achievements and progress.
/// </summary>
[Group("achievements", "View your achievements and milestones!")]
public class AchievementsSlashCommands : EeveeCoreSlashModuleBase<AchievementService>
{
    /// <summary>
    ///     Shows your current achievement progress and milestones.
    /// </summary>
    /// <param name="category">Optional achievement category to filter by.</param>
    /// <param name="user">Optional user to view achievements for (defaults to yourself).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("view", "View your achievement progress and milestones")]
    public async Task ViewAchievementsCommand(
        [Summary("category", "Achievement category to view")] 
        [Choice("Battle", "battle")]
        [Choice("Breeding", "breeding")] 
        [Choice("Catching", "catching")]
        [Choice("Special", "special")]
        [Choice("Market", "market")]
        [Choice("Activities", "activities")]
        [Choice("Events", "events")]
        string? category = null,
        [Summary("user", "User to view achievements for")] IUser? user = null)
    {
        try
        {
            await DeferAsync();

            var targetUser = user ?? ctx.User;
            var userId = targetUser.Id;

            // Get achievement values and progress
            var achievementValues = await Service.GetAchievementValuesAsync(userId);
            var loyalty = await Service.GetLoyaltyAsync(userId);

            if (!achievementValues.Any())
            {
                await FollowupAsync("No achievement data found for this user.", ephemeral: true);
                return;
            }

            // Filter by category if specified
            var filteredAchievements = FilterAchievementsByCategory(achievementValues, category);

            // Create embed
            var embed = new EmbedBuilder()
                .WithTitle($"🏆 {targetUser.Username}'s Achievements")
                .WithColor(AchievementConstants.GoldTierColor)
                .WithThumbnailUrl(targetUser.GetAvatarUrl() ?? targetUser.GetDefaultAvatarUrl());

            if (category != null)
            {
                embed.WithDescription($"**Category:** {char.ToUpper(category[0]) + category[1..]}");
            }

            // Add achievement fields
            await AddAchievementFieldsAsync(embed, filteredAchievements, userId);

            // Add summary information
            var totalMilestones = await GetTotalMilestonesAsync(userId);
            var loyaltyPoints = loyalty.LoyaltyPoints;
            var dailyStreak = loyalty.DailyStreak;

            embed.AddField("📊 Summary", 
                $"Total Milestones: {totalMilestones:N0}\n" +
                $"Loyalty Points: {loyaltyPoints:N0}\n" +
                $"Daily Streak: {dailyStreak} days", 
                false);

            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in view achievements command for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while retrieving achievement data.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Shows detailed progress for a specific achievement.
    /// </summary>
    /// <param name="achievement">The achievement to view detailed progress for.</param>
    /// <param name="user">Optional user to view achievement for (defaults to yourself).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("progress", "View detailed progress for a specific achievement")]
    public async Task ViewProgressCommand(
        [Summary("achievement", "Achievement to view progress for")]
        [Autocomplete(typeof(AchievementAutocompleteHandler))]
        string achievement,
        [Summary("user", "User to view progress for")] IUser? user = null)
    {
        try
        {
            await DeferAsync();

            var targetUser = user ?? ctx.User;
            var userId = targetUser.Id;

            if (!AchievementConstants.Milestones.TryGetValue(achievement, out var milestones))
            {
                await FollowupAsync("Invalid achievement specified.", ephemeral: true);
                return;
            }

            var achievementValues = await Service.GetAchievementValuesAsync(userId);
            var loyalty = await Service.GetLoyaltyAsync(userId);

            var currentValue = achievementValues.GetValueOrDefault(achievement, 0);
            var lastMilestone = await Service.GetLastMilestoneAsync(userId, achievement);
            var displayName = AchievementConstants.AchievementDisplayNames.GetValueOrDefault(achievement, achievement);

            // Find next milestone
            var nextMilestone = milestones.FirstOrDefault(m => m > currentValue);
            var progressToNext = nextMilestone > 0 ? (double)currentValue / nextMilestone : 1.0;

            // Create progress bar
            var progressBar = CreateProgressBar(progressToNext);

            var embed = new EmbedBuilder()
                .WithTitle($"📈 {displayName} Progress")
                .WithColor(AchievementConstants.GetMilestoneColor(
                    Array.IndexOf(milestones, lastMilestone), milestones.Length))
                .WithThumbnailUrl(targetUser.GetAvatarUrl() ?? targetUser.GetDefaultAvatarUrl());

            embed.AddField("Current Progress", 
                $"**Value:** {currentValue:N0}\n" +
                $"**Last Milestone:** {lastMilestone:N0}\n" +
                (nextMilestone > 0 ? $"**Next Milestone:** {nextMilestone:N0}\n{progressBar}" : "**All milestones completed!**"), 
                false);

            // Show completed milestones
            var completedMilestones = milestones.Where(m => currentValue >= m).ToArray();
            if (completedMilestones.Any())
            {
                var milestoneText = string.Join(", ", completedMilestones.Select(m => $"`{m:N0}`"));
                embed.AddField($"✅ Completed Milestones ({completedMilestones.Length})", milestoneText, false);
            }

            // Show upcoming milestones
            var upcomingMilestones = milestones.Where(m => currentValue < m).Take(5).ToArray();
            if (upcomingMilestones.Any())
            {
                var milestoneText = string.Join(", ", upcomingMilestones.Select(m => $"`{m:N0}`"));
                embed.AddField($"🎯 Upcoming Milestones", milestoneText, false);
            }

            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in view progress command for achievement {Achievement}", achievement);
            await FollowupAsync("An error occurred while retrieving progress data.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Shows your loyalty points and tier information.
    /// </summary>
    /// <param name="user">Optional user to view loyalty for (defaults to yourself).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("loyalty", "View your loyalty points and tier benefits")]
    public async Task ViewLoyaltyCommand([Summary("user", "User to view loyalty for")] IUser? user = null)
    {
        try
        {
            await DeferAsync();

            var targetUser = user ?? ctx.User;
            var userId = targetUser.Id;

            var loyalty = await Service.GetLoyaltyAsync(userId);
            var loyaltyPoints = loyalty.LoyaltyPoints;
            var dailyStreak = loyalty.DailyStreak;

            // Calculate tier
            var tier = CalculateLoyaltyTier(loyaltyPoints);

            var embed = new EmbedBuilder()
                .WithTitle($"💎 {targetUser.Username}'s Loyalty Status")
                .WithColor(tier.TierColor)
                .WithThumbnailUrl(targetUser.GetAvatarUrl() ?? targetUser.GetDefaultAvatarUrl());

            embed.AddField("🏅 Current Tier", 
                $"**{tier.TierName}**\n" +
                $"Points: {loyaltyPoints:N0}\n" +
                $"XP Multiplier: {tier.XpMultiplier:F1}x\n" +
                $"Shop Discount: {tier.ShopDiscount}%", 
                true);

            embed.AddField("🔥 Daily Streak", 
                $"**{dailyStreak} days**\n" +
                $"Next bonus at: {GetNextStreakBonus(dailyStreak)} days", 
                true);

            // Show next tier if applicable
            var nextTier = GetNextLoyaltyTier(loyaltyPoints);
            if (nextTier != null)
            {
                var pointsNeeded = nextTier.RequiredPoints - loyaltyPoints;
                embed.AddField("🎯 Next Tier", 
                    $"**{nextTier.TierName}**\n" +
                    $"Points needed: {pointsNeeded:N0}\n" +
                    $"Bonus: {nextTier.MonthlyBonus:N0} monthly Coins", 
                    false);
            }

            embed.WithFooter("Earn loyalty points by completing achievements!");

            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in view loyalty command for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while retrieving loyalty data.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Shows leaderboard for a specific achievement.
    /// </summary>
    /// <param name="achievement">The achievement to show leaderboard for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("leaderboard", "View leaderboard for a specific achievement")]
    public async Task ViewLeaderboardCommand(
        [Summary("achievement", "Achievement to view leaderboard for")]
        [Autocomplete(typeof(AchievementAutocompleteHandler))]
        string achievement)
    {
        try
        {
            await DeferAsync();

            if (!AchievementConstants.Milestones.ContainsKey(achievement))
            {
                await FollowupAsync("Invalid achievement specified.", ephemeral: true);
                return;
            }

            // This would require a more complex query to get top users
            // For now, show a placeholder
            var embed = new EmbedBuilder()
                .WithTitle($"🏆 {AchievementConstants.AchievementDisplayNames.GetValueOrDefault(achievement, achievement)} Leaderboard")
                .WithDescription("Leaderboard functionality coming soon!")
                .WithColor(AchievementConstants.GoldTierColor);

            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in leaderboard command for achievement {Achievement}", achievement);
            await FollowupAsync("An error occurred while retrieving leaderboard data.", ephemeral: true);
        }
    }

    #region Helper Methods

    /// <summary>
    ///     Filters achievements by category.
    /// </summary>
    private static Dictionary<string, int> FilterAchievementsByCategory(Dictionary<string, int> achievements, string? category)
    {
        if (string.IsNullOrEmpty(category))
            return achievements;

        return category.ToLower() switch
        {
            "battle" => achievements.Where(a => a.Key.StartsWith("duel_") || a.Key.StartsWith("npc_") || a.Key == "gym_wins").ToDictionary(x => x.Key, x => x.Value),
            "breeding" => achievements.Where(a => a.Key.StartsWith("breed_") || a.Key.EndsWith("_bred")).ToDictionary(x => x.Key, x => x.Value),
            "catching" => achievements.Where(a => a.Key.StartsWith("pokemon_") || a.Key.EndsWith("_caught")).ToDictionary(x => x.Key, x => x.Value),
            "market" => achievements.Where(a => a.Key.StartsWith("market_") || a.Key.Contains("released")).ToDictionary(x => x.Key, x => x.Value),
            "activities" => achievements.Where(a => a.Key is "fishing_success" or "missions" or "votes" || a.Key.StartsWith("game_")).ToDictionary(x => x.Key, x => x.Value),
            "events" => achievements.Where(a => a.Key.Contains("event") || a.Key.StartsWith("chests_") || a.Key.Contains("donation")).ToDictionary(x => x.Key, x => x.Value),
            "special" => achievements.Where(a => a.Key is "dex_complete").ToDictionary(x => x.Key, x => x.Value),
            _ => achievements
        };
    }

    /// <summary>
    ///     Adds achievement fields to an embed.
    /// </summary>
    private async Task AddAchievementFieldsAsync(EmbedBuilder embed, Dictionary<string, int> achievements, ulong userId)
    {
        const int maxFieldsPerEmbed = 10;
        var addedFields = 0;

        foreach (var (achievement, value) in achievements.OrderByDescending(x => x.Value).Take(maxFieldsPerEmbed))
        {
            if (!AchievementConstants.Milestones.TryGetValue(achievement, out var milestones))
                continue;

            var displayName = AchievementConstants.AchievementDisplayNames.GetValueOrDefault(achievement, achievement);
            var lastMilestone = await Service.GetLastMilestoneAsync(userId, achievement);
            var nextMilestone = milestones.FirstOrDefault(m => m > value);

            var fieldValue = $"**Current:** {value:N0}\n";
            if (lastMilestone > 0)
                fieldValue += $"**Last Milestone:** {lastMilestone:N0}\n";
            if (nextMilestone > 0)
                fieldValue += $"**Next:** {nextMilestone:N0}";
            else
                fieldValue += "**Status:** All completed!";

            embed.AddField(displayName, fieldValue, true);
            addedFields++;
        }

        if (addedFields == 0)
        {
            embed.AddField("No Achievements", "No achievement data found for the selected category.", false);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Creates a visual progress bar.
    /// </summary>
    private static string CreateProgressBar(double progress, int length = 10)
    {
        var filled = (int)(progress * length);
        var empty = length - filled;
        return $"`[{new string('█', filled)}{new string('░', empty)}]` {progress:P1}";
    }

    /// <summary>
    ///     Calculates loyalty tier based on points.
    /// </summary>
    private static Models.LoyaltyTier CalculateLoyaltyTier(int points)
    {
        return points switch
        {
            >= 50000 => new Models.LoyaltyTier { RequiredPoints = 50000, TierName = "Diamond Elite", XpMultiplier = 2.5, ShopDiscount = 25, TierColor = AchievementConstants.DiamondTierColor, MonthlyBonus = 100000 },
            >= 25000 => new Models.LoyaltyTier { RequiredPoints = 25000, TierName = "Platinum Master", XpMultiplier = 2.0, ShopDiscount = 20, TierColor = AchievementConstants.PlatinumTierColor, MonthlyBonus = 50000 },
            >= 10000 => new Models.LoyaltyTier { RequiredPoints = 10000, TierName = "Gold Champion", XpMultiplier = 1.75, ShopDiscount = 15, TierColor = AchievementConstants.GoldTierColor, MonthlyBonus = 25000 },
            >= 5000 => new Models.LoyaltyTier { RequiredPoints = 5000, TierName = "Silver Expert", XpMultiplier = 1.5, ShopDiscount = 10, TierColor = AchievementConstants.SilverTierColor, MonthlyBonus = 10000 },
            >= 1000 => new Models.LoyaltyTier { RequiredPoints = 1000, TierName = "Bronze Trainer", XpMultiplier = 1.25, ShopDiscount = 5, TierColor = AchievementConstants.BronzeTierColor, MonthlyBonus = 5000 },
            _ => new Models.LoyaltyTier { RequiredPoints = 0, TierName = "Novice", XpMultiplier = 1.0, ShopDiscount = 0, TierColor = 0x808080, MonthlyBonus = 1000 }
        };
    }

    /// <summary>
    ///     Gets the next loyalty tier.
    /// </summary>
    private static Models.LoyaltyTier? GetNextLoyaltyTier(int currentPoints)
    {
        var tiers = new[] { 1000, 5000, 10000, 25000, 50000 };
        var nextTierPoints = tiers.FirstOrDefault(t => t > currentPoints);
        return nextTierPoints > 0 ? CalculateLoyaltyTier(nextTierPoints) : null;
    }

    /// <summary>
    ///     Gets the next streak bonus threshold.
    /// </summary>
    private static int GetNextStreakBonus(int currentStreak)
    {
        var bonuses = new[] { 3, 7, 14, 30 };
        return bonuses.FirstOrDefault(b => b > currentStreak);
    }

    /// <summary>
    ///     Gets the total number of milestones completed by a user.
    /// </summary>
    private async Task<int> GetTotalMilestonesAsync(ulong userId)
    {
        await using var db = await Service.GetDbConnectionAsync();
        return await db.MilestoneProgress
            .Where(mp => mp.UserId == userId)
            .CountAsync();
    }

    #endregion
}