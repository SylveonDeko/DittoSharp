using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Games.Common;
using EeveeCore.Modules.Games.Models;
using EeveeCore.Modules.Games.Services;
using EeveeCore.Modules.Missions.Services;
using Serilog;

namespace EeveeCore.Modules.Games.Components;

/// <summary>
///     Handles interaction components for the word search game.
///     Processes button clicks and modal submissions for game interactions.
/// </summary>
/// <param name="wordSearchService">The service that handles word search game logic.</param>
/// <param name="missionService">The service that handles mission progress tracking.</param>
public class WordSearchInteractionModule(WordSearchService wordSearchService, MissionService missionService) 
    : EeveeCoreSlashModuleBase<WordSearchService>
{
    /// <summary>
    ///     Handles the start button interaction for word search games.
    ///     Initializes a new game and displays the game board.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("wordsearch:start")]
    public async Task HandleStartGame()
    {
        try
        {
            await DeferAsync();

            // Initialize new game
            var gameState = await wordSearchService.InitializeGameAsync(ctx.User.Id);
            
            // Store game state in cache/memory (you might want to use Redis or similar)
            // For now, we'll pass it through the embed description or store in a static dictionary
            GameStateManager.StoreGameState(gameState);

            // Generate the word search image
            var imageStream = await wordSearchService.GenerateWordSearchImageAsync(gameState, "data/backgrounds/bg1.png");
            
            // Create embed with game info
            var prompt = wordSearchService.FormatPromptMessage(gameState);
            var embed = new EmbedBuilder()
                .WithTitle("Word Search Game - Find the Pokemon!")
                .WithDescription(prompt)
                .WithColor(Color.Blue)
                .WithImageUrl($"attachment://wordsearch_{gameState.GameId}.png")
                .Build();

            // Create components for guessing
            var components = new ComponentBuilder()
                .WithButton("Make a Guess", $"wordsearch:guess:{gameState.GameId}", ButtonStyle.Primary, new Emoji("🔍"))
                .WithButton("Quit Game", $"wordsearch:quit:{gameState.GameId}", ButtonStyle.Danger, new Emoji("❌"))
                .Build();

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
                x.Attachments = new List<FileAttachment> 
                { 
                    new(imageStream, $"wordsearch_{gameState.GameId}.png") 
                };
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error starting word search game for user {UserId}", ctx.User.Id);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while starting the game. Please try again.";
                x.Embed = null;
                x.Components = null;
            });
        }
    }

    /// <summary>
    ///     Handles the guess button interaction for word search games.
    ///     Shows a modal for the user to input their guess.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("wordsearch:guess:*")]
    public async Task HandleGuessButton(string gameId)
    {
        try
        {
            var gameState = GameStateManager.GetGameState(gameId);
            if (gameState == null)
            {
                await ctx.Interaction.RespondAsync("Game session not found or has expired.", ephemeral: true);
                return;
            }

            if (gameState.UserId != ctx.User.Id)
            {
                await ctx.Interaction.RespondAsync("This is not your game!", ephemeral: true);
                return;
            }

            await ctx.Interaction.RespondWithModalAsync<WordSearchGuessModal>($"wordsearch_guess_modal:{gameId}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling guess button for game {GameId}", gameId);
            await ctx.Interaction.RespondAsync("An error occurred. Please try again.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the quit button interaction for word search games.
    ///     Ends the game and shows the final state.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("wordsearch:quit:*")]
    public async Task HandleQuitGame(string gameId)
    {
        try
        {
            await DeferAsync();

            var gameState = GameStateManager.GetGameState(gameId);
            if (gameState == null)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Game session not found or has expired.";
                    x.Embed = null;
                    x.Components = null;
                });
                return;
            }

            if (gameState.UserId != ctx.User.Id)
            {
                await ctx.Interaction.RespondAsync("This is not your game!", ephemeral: true);
                return;
            }

            // Mark game as inactive
            gameState.IsActive = false;
            GameStateManager.RemoveGameState(gameId);

            var embed = new EmbedBuilder()
                .WithTitle("Word Search Game - Quit")
                .WithDescription($"Game ended early. You found {gameState.FoundWords.Count} out of {gameState.Words.Count} words.\n\n" +
                               $"The hidden words were: {string.Join(", ", gameState.Words.Select(w => $"`{w}`"))}")
                .WithColor(Color.Orange)
                .Build();

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = embed;
                x.Components = null;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error quitting word search game {GameId}", gameId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while quitting the game.";
                x.Embed = null;
                x.Components = null;
            });
        }
    }

    /// <summary>
    ///     Handles the modal submission for word search guesses.
    /// </summary>
    /// <param name="gameId">The game ID extracted from the modal custom ID.</param>
    /// <param name="modal">The submitted guess modal.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("wordsearch_guess_modal:*")]
    public async Task HandleGuessModal(string gameId, WordSearchGuessModal modal)
    {
        try
        {
            await DeferAsync();

            var gameState = GameStateManager.GetGameState(gameId);
            if (gameState == null)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Game session not found or has expired.";
                    x.Embed = null;
                    x.Components = null;
                });
                return;
            }

            if (gameState.UserId != ctx.User.Id)
            {
                await ctx.Interaction.RespondAsync("This is not your game!", ephemeral: true);
                return;
            }

            // Process the guess
            var result = wordSearchService.HandleGuess(gameState, modal.Guess);
            
            if (result.TimedOut || result.GameComplete)
            {
                // Game is over
                GameStateManager.RemoveGameState(gameId);
                
                var finalEmbed = new EmbedBuilder()
                    .WithTitle("Word Search Game - Complete")
                    .WithDescription(result.Message + "\n\n" +
                                   $"Words found: {gameState.FoundWords.Count}/{gameState.Words.Count}\n" +
                                   $"The hidden words were: {string.Join(", ", gameState.Words.Select(w => $"`{w}`"))}")
                    .WithColor(result.GameComplete ? Color.Green : Color.Red)
                    .Build();

                // Track mission progress for word search completion
                if (result.GameComplete)
                {
                    await missionService.TriggerGameWordSearchCompletedAsync(ctx.Interaction, gameState.FoundWords.Count);
                }

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "";
                    x.Embed = finalEmbed;
                    x.Components = null;
                });
            }
            else if (result.Success)
            {
                // Correct guess, update the image
                var imageStream = await wordSearchService.GenerateWordSearchImageAsync(gameState, "data/backgrounds/bg1.png");
                var prompt = wordSearchService.FormatPromptMessage(gameState);
                
                var embed = new EmbedBuilder()
                    .WithTitle("Word Search Game - Find the Pokemon!")
                    .WithDescription($"✅ {result.Message}\n\n{prompt}")
                    .WithColor(Color.Blue)
                    .WithImageUrl($"attachment://wordsearch_{gameState.GameId}.png")
                    .Build();

                var components = new ComponentBuilder()
                    .WithButton("Make a Guess", $"wordsearch:guess:{gameState.GameId}", ButtonStyle.Primary, new Emoji("🔍"))
                    .WithButton("Quit Game", $"wordsearch:quit:{gameState.GameId}", ButtonStyle.Danger, new Emoji("❌"))
                    .Build();

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "";
                    x.Embed = embed;
                    x.Components = components;
                    x.Attachments = new List<FileAttachment> 
                    { 
                        new(imageStream, $"wordsearch_{gameState.GameId}.png") 
                    };
                });
            }
            else
            {
                // Incorrect guess, just send a message
                await ctx.Interaction.FollowupAsync(result.Message, ephemeral: true);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling guess modal for game {GameId}", gameId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while processing your guess.";
                x.Embed = null;
                x.Components = null;
            });
        }
    }
}


/// <summary>
///     Simple in-memory game state manager.
///     In production, you would want to use Redis or a proper cache.
/// </summary>
public static class GameStateManager
{
    private static readonly Dictionary<string, WordSearchGameState> GameStates = new();
    private static readonly object Lock = new();

    /// <summary>
    ///     Stores a game state in memory.
    /// </summary>
    /// <param name="gameState">The game state to store.</param>
    public static void StoreGameState(WordSearchGameState gameState)
    {
        lock (Lock)
        {
            GameStates[gameState.GameId] = gameState;
        }
    }

    /// <summary>
    ///     Retrieves a game state from memory.
    /// </summary>
    /// <param name="gameId">The game ID to retrieve.</param>
    /// <returns>The game state, or null if not found.</returns>
    public static WordSearchGameState? GetGameState(string gameId)
    {
        lock (Lock)
        {
            return GameStates.TryGetValue(gameId, out var gameState) ? gameState : null;
        }
    }

    /// <summary>
    ///     Removes a game state from memory.
    /// </summary>
    /// <param name="gameId">The game ID to remove.</param>
    public static void RemoveGameState(string gameId)
    {
        lock (Lock)
        {
            GameStates.Remove(gameId);
        }
    }

    /// <summary>
    ///     Cleans up expired game states.
    /// </summary>
    /// <param name="maxAge">The maximum age before a game state is considered expired.</param>
    public static void CleanupExpiredGames(TimeSpan maxAge)
    {
        lock (Lock)
        {
            var cutoff = DateTimeOffset.UtcNow - maxAge;
            var expiredGames = GameStates
                .Where(kvp => kvp.Value.StartTime < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var gameId in expiredGames)
            {
                GameStates.Remove(gameId);
            }
        }
    }
}