using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Games.Services;
using EeveeCore.Modules.Missions.Services;
using Serilog;

namespace EeveeCore.Modules.Games;

/// <summary>
///     Provides Discord slash commands for game functionality.
///     Includes commands for word search, slot machine, and other mini-games.
/// </summary>
/// <param name="wordSearchService">Service for handling word search games.</param>
/// <param name="slotMachineService">Service for handling slot machine games.</param>
/// <param name="missionService">Service for handling mission progress tracking.</param>
[Group("game", "Mini-game commands to earn credits and have fun!")]
public class GamesSlashCommands(WordSearchService wordSearchService, SlotMachineService slotMachineService, MissionService missionService)
    : EeveeCoreSlashModuleBase<WordSearchService>
{
    /// <summary>
    ///     Starts a word search game where players find hidden Pokemon names.
    ///     Players have a limited time to find 8 hidden Pokemon names in a grid of letters.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("wordsearch", "Start a word search puzzle with hidden Pokemon names")]
    public async Task WordSearchCommand()
    {
        try
        {
            await DeferAsync();

            var embed = new EmbedBuilder()
                .WithTitle("Word Search Game")
                .WithDescription("🔍 **Find the Hidden Pokemon!**\n\n" +
                               "There are 8 hidden Pokemon names in a grid of letters.\n" +
                               "You have 200 seconds to find them all!\n\n" +
                               "Click **Start Game** to begin your search!")
                .WithColor(Color.Blue)
                .WithThumbnailUrl("https://images.mewdeko.tech/skins/normal/25-0-.png") // Pikachu thumbnail
                .Build();

            var components = new ComponentBuilder()
                .WithButton("Start Game", "wordsearch:start", ButtonStyle.Success, new Emoji("🎮"))
                .Build();

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error creating word search game for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while setting up the word search game.");
        }
    }

    /// <summary>
    ///     Plays a slot machine game with Pokemon-themed symbols.
    ///     Players can win different amounts based on the combination of symbols.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("slots", "Play the Pokemon slot machine")]
    public async Task SlotsCommand()
    {
        try
        {
            await DeferAsync();

            // Perform the spin
            var result = slotMachineService.Spin();

            // Fire mission event for slot machine play
            await missionService.TriggerGameSlotsPlayedAsync(ctx.Interaction, result.IsWin);

            // Create embed with result
            var embedBuilder = new EmbedBuilder()
                .WithTitle("🎰 Pokemon Slot Machine 🎰")
                .WithDescription(result.ResultMessage)
                .WithColor(result.IsWin ? Color.Gold : Color.LightGrey);

            if (result.IsWin)
            {
                embedBuilder.AddField("🎉 Congratulations!", 
                    $"Win Type: {result.WinType}\n" +
                    $"Payout Multiplier: {result.PayoutMultiplier}x", 
                    false);
            }
            else
            {
                embedBuilder.WithFooter("Better luck next time! Try again for a chance to win big!");
            }

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embedBuilder.Build();
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error running slot machine for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while spinning the slot machine.");
        }
    }

    /// <summary>
    ///     Shows information about the slot machine including win probabilities and payouts.
    ///     Displays the different symbol combinations and their respective rewards.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("slot-info", "View slot machine win probabilities and payouts")]
    public async Task SlotInfoCommand()
    {
        try
        {
            var probabilities = slotMachineService.GetWinProbabilities();

            var embed = new EmbedBuilder()
                .WithTitle("🎰 Slot Machine Information")
                .WithDescription("**Win Combinations and Payouts:**")
                .WithColor(Color.Purple);

            // Add payout information
            embed.AddField("💰 **Jackpot Wins**",
                "🔥 Triple Seven: 1000x (Jackpot!)\n" +
                "⭐ Triple Star: 500x\n" +
                "🪙 Triple Coin: 250x", true);

            embed.AddField("🎯 **Good Wins**",
                "🍒 Triple Cherry: 100x\n" +
                "⚡ Triple Pikachu: 75x\n" +
                "🎁 Triple Present: 50x\n" +
                "🟣 Triple Purple: 25x", true);

            embed.AddField("✨ **Special Wins**",
                "🍀 Lucky Seven: 10x (Any seven)\n" +
                "🎲 Triple Match: 5x (Any three)", true);

            // Add some probabilities
            var probabilityText = string.Join("\n", 
                probabilities.Take(5).Select(p => $"{p.Key}: {p.Value:F3}%"));
            
            embed.AddField("📊 **Win Probabilities** (Top 5)", probabilityText, false);

            embed.WithFooter("Good luck spinning! 🎰");

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error showing slot info for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while retrieving slot machine information.");
        }
    }
}

/// <summary>
///     Provides additional game-related slash commands that don't require services.
///     These are simpler commands that provide information or basic functionality.
/// </summary>
public class SimpleGamesSlashCommands : EeveeCoreSlashCommandModule
{
    /// <summary>
    ///     Shows a list of available games and their descriptions.
    ///     Provides an overview of all mini-games available in the bot.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("game-list", "Show all available mini-games")]
    public async Task GameListCommand()
    {
        try
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎮 Available Mini-Games")
                .WithDescription("Here are all the fun games you can play to earn rewards!")
                .WithColor(Color.Blue);

            embed.AddField("🔍 **Word Search**", 
                "Find hidden Pokemon names in a letter grid!\n" +
                "Time limit: 200 seconds\n" +
                "Command: `/game wordsearch`", false);

            embed.AddField("🎰 **Slot Machine**", 
                "Spin the Pokemon-themed slot machine for big wins!\n" +
                "Various symbols with different payouts\n" +
                "Command: `/game slots`", false);

            embed.AddField("📊 **Coming Soon**", 
                "• Pokemon Trivia Quiz\n" +
                "• Memory Card Game\n" +
                "• Battle Simulator\n" +
                "• And more!", false);

            embed.WithFooter("Have fun and good luck! 🍀");

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error showing game list for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while retrieving the game list.");
        }
    }

    /// <summary>
    ///     Shows general help information about the games system.
    ///     Provides tips and guidance for new players.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("game-help", "Get help with the games system")]
    public async Task GameHelpCommand()
    {
        try
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎮 Games Help")
                .WithDescription("Welcome to the mini-games system! Here's how to get started:")
                .WithColor(Color.Green);

            embed.AddField("🎯 **How to Play**",
                "• Use `/game list` to see all available games\n" +
                "• Each game has different rules and rewards\n" +
                "• Some games are single-player, others multiplayer\n" +
                "• Read each game's description before playing!", false);

            embed.AddField("🏆 **Rewards**",
                "• Earn credits for winning games\n" +
                "• Get special items from certain games\n" +
                "• Unlock achievements for game milestones\n" +
                "• Compete on leaderboards!", false);

            embed.AddField("⚡ **Tips**",
                "• Practice makes perfect!\n" +
                "• Some games have time limits\n" +
                "• Pay attention to game instructions\n" +
                "• Don't be afraid to try different strategies", false);

            embed.AddField("🆘 **Need More Help?**",
                "• Join our support server\n" +
                "• Ask questions in community channels\n" +
                "• Check out the full guide online\n" +
                "• Contact staff for technical issues", false);

            embed.WithFooter("Have fun gaming! 🎮");

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error showing game help for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while retrieving game help.");
        }
    }
}