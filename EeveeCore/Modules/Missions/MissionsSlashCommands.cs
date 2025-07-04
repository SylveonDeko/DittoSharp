using System.Text;
using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Missions.Common;
using EeveeCore.Modules.Missions.Components;
using EeveeCore.Modules.Missions.Services;
using Serilog;

namespace EeveeCore.Modules.Missions;

/// <summary>
///     Provides Discord slash commands for mission functionality.
///     Includes commands for viewing missions, progress, XP, and accessing the crystal slime shop.
/// </summary>
[Group("missions", "Mission commands!")]
public class MissionsSlashCommands : EeveeCoreSlashModuleBase<MissionService>
{
    // Store items are now loaded from MissionConstants.StoreItems

    /// <summary>
    ///     Opens the crystal slime exchange shop with a dropdown of purchasable items.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("shop", "Crystallized Slime Exchange, dropdown of options")]
    public async Task Shop()
    {
        var view = new ComponentBuilder()
            .WithSelectMenu(new StoreDropdown(MissionConstants.StoreItems, Service));

        var embed = new EmbedBuilder()
            .WithTitle("Crystal Slime Exchange")
            .WithDescription(
                "### Exchange your Crystallized Slime for Credits!\n" +
                $"- 150 {MissionConstants.CrystalSlimeEmojiId} for 100,000 credits.\n" +
                "### Friendship Stone: Increases friendship/happiness instantly!\n" +
                $"- 10 {MissionConstants.CrystalSlimeEmojiId}\n" +
                "### Shadow Essence: Increase your shadow-chain instantly!\n" +
                $"- 100 {MissionConstants.CrystalSlimeEmojiId} for random chance: +15-75 Shadow Chain\n" +
                "### Meowth Tickets: Try Your Luck!\n" +
                $"- 75 {MissionConstants.CrystalSlimeEmojiId} for 1 Ticket\n" +
                "### VIP Tokens: Get exclusive benefits!\n" +
                $"- 1000 {MissionConstants.CrystalSlimeEmojiId} for 1 VIP Token\n" +
                $"- 2500 {MissionConstants.CrystalSlimeEmojiId} for 3 VIP Tokens (x3)\n" +
                "\n**How Meowth Tickets Work:**\n" +
                "Each Meowth Ticket hides a secret amount of credits ranging from a tiny sum to a whopping 150,000.\n" +
                "- Upon buying a Meowth Ticket, you'll be presented with 9 choices, each concealing different amounts of credits.\n" +
                "- Make your first choice to uncover what's hidden beneath. But that's not all! You'll then face a tantalizing decision: keep your initial winnings or risk your first pick for potentially more!\n" +
                "**You decide! :**\n"
            )
            .WithColor(MissionConstants.StoreColor)
            .WithFooter($"When you buy a Meowth Ticket, you have {MissionConstants.LotteryTimeoutSeconds} seconds before the game ends, no refunds!")
            .Build();

        await RespondAsync("Select an item to buy:", embed: embed, components: view.Build(), ephemeral: true);
    }

    /// <summary>
    ///     Displays the user's current XP progress towards the next level.
    ///     Shows XP bar, level, crystal slime count, and user title in a generated image.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("xp", "Displays your current XP progress towards the next level")]
    public async Task Xp()
    {
        await DeferAsync();

        try
        {
            var (currentXp, level, crystalSlime) = await Service.GetUserXpInfoAsync(ctx.User.Id);
            var title = await Service.GetUserTitleAsync(ctx.User.Id);

            // Download user avatar
            byte[]? avatarBytes = null;
            try
            {
                var avatarUrl = ctx.User.GetAvatarUrl(size: 256) ?? ctx.User.GetDefaultAvatarUrl();
                using var httpClient = new HttpClient();
                avatarBytes = await httpClient.GetByteArrayAsync(avatarUrl);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to download avatar for user {UserId}", ctx.User.Id);
            }

            // Generate XP image
            var imageService = new XpImageGenerationService();
            var imageBytes = await imageService.GenerateXpImageAsync(
                ctx.User.Username,
                currentXp,
                level,
                crystalSlime,
                title,
                avatarBytes);

            // Create file attachment
            using var stream = new MemoryStream(imageBytes);
            var attachment = new FileAttachment(stream, "xp_progress.png", "XP Progress Image");

            await FollowupWithFileAsync(attachment, ephemeral: false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in XP command for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while generating your XP image.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Shows the user's progress on current active missions with progress bars.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("progress", "Shows your progress on current missions")]
    public async Task Progress()
    {
        try
        {
            var userProgress = await Service.GetUserProgressAsync(ctx.User.Id);
            if (userProgress == null)
            {
                await RespondAsync("You do not have any progress recorded for current missions.", ephemeral: true);
                return;
            }

            var activeMissions = await Service.GetActiveMissionsAsync();
            if (activeMissions.Count == 0)
            {
                await RespondAsync("No active missions are currently available.", ephemeral: true);
                return;
            }

            // Define progress bar emojis
            var fullBar = new[] { "<:bar1:1175119051568185455>", "<:bar2:1175119050112774195>", "<:bar3:1175119054084784149>" };
            var emptyBar = new[] { "<:bar1e:1175126117296902195>", "<:bar2e:1175126188742684833>", "<:bar3:1175126108459511959>" };

            var progressMessage = new StringBuilder($"**{ctx.User.Username}'s Mission Progress:**\n");

            foreach (var mission in activeMissions)
            {
                var userMissionProgress = GetProgressForMission(userProgress, mission.Key);
                var progressPercentage = Math.Min((int)((userMissionProgress / (double)mission.Target) * 10), 10);

                // Construct progress bar
                var progressBar = string.Join("", fullBar.Take(progressPercentage)) +
                                 string.Join("", emptyBar.Skip(progressPercentage));

                progressMessage.AppendLine($"- **{mission.Name}**: {progressBar} {userMissionProgress}/{mission.Target}");
            }

            await RespondAsync(progressMessage.ToString(), ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Progress command for user {UserId}", ctx.User.Id);
            await RespondAsync("An error occurred while retrieving your progress.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Lists all currently active missions with their requirements and rewards.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("list", "Lists all currently active missions")]
    public async Task List()
    {
        try
        {
            var missions = await Service.GetActiveMissionsAsync();
            if (missions.Count == 0)
            {
                await RespondAsync("Missions are currently down, please wait a few minutes and try again!", ephemeral: true);
                return;
            }

            var userProgress = await Service.GetUserProgressAsync(ctx.User.Id);
            var defaultProgress = new Database.Models.Mongo.Game.UserProgress
            {
                UserId = ctx.User.Id,
                Breed = 0,
                Catch = 0,
                DuelLose = 0,
                DuelWin = 0,
                Ev = 0,
                Fish = 0,
                Npc = 0,
                Party = 0,
                PokemonSetup = 0,
                Vote = 0
            };

            userProgress ??= defaultProgress;

            var embed = new EmbedBuilder()
                .WithTitle("Today's missions")
                .WithColor(MissionConstants.MissionCompleteColor)
                .WithFooter("Missions reset daily");

            foreach (var mission in missions)
            {
                var progress = GetProgressForMission(userProgress, mission.Key);
                const string completed = MissionConstants.MissionEmojiId;
                
                embed.AddField(
                    $"{mission.Name} ({mission.Target})",
                    $"{progress}/{mission.Target} {completed}");
            }

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in List command for user {UserId}", ctx.User.Id);
            await RespondAsync("An error occurred while retrieving missions.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Gets the progress value for a specific mission key from user progress.
    /// </summary>
    /// <param name="userProgress">The user's progress data.</param>
    /// <param name="missionKey">The mission key to get progress for.</param>
    /// <returns>The progress value for the mission.</returns>
    private static int GetProgressForMission(Database.Models.Mongo.Game.UserProgress userProgress, string missionKey)
    {
        return missionKey switch
        {
            "breed" => userProgress.Breed,
            "catch" => userProgress.Catch,
            "duel_win" => userProgress.DuelWin,
            "duel_lose" => userProgress.DuelLose,
            "ev" => userProgress.Ev,
            "fish" => userProgress.Fish,
            "npc" => userProgress.Npc,
            "party" => userProgress.Party,
            "pokemon_setup" => userProgress.PokemonSetup,
            "vote" => userProgress.Vote,
            _ => 0
        };
    }
}

