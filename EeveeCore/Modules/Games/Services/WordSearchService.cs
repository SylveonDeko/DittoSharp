using System.Text;
using EeveeCore.Modules.Games.Common;
using EeveeCore.Modules.Games.Models;
using EeveeCore.Services;
using EeveeCore.Services.Impl;
using MongoDB.Driver;
using SkiaSharp;

namespace EeveeCore.Modules.Games.Services;

/// <summary>
///     Service for handling word search game functionality.
///     Generates grids, places Pokemon names, and creates images using SkiaSharp.
/// </summary>
/// <param name="mongoService">The MongoDB service for accessing Pokemon data.</param>
public class WordSearchService(IMongoService mongoService) : INService
{
    private static readonly Random Random = new();

    /// <summary>
    ///     Initializes the game state for a new word search game.
    /// </summary>
    /// <param name="userId">The Discord user ID of the player.</param>
    /// <returns>A new word search game state.</returns>
    public async Task<WordSearchGameState> InitializeGameAsync(ulong userId)
    {
        // Get Pokemon names from MongoDB
        var pokemonCursor = await mongoService.PFile
            .Find(p => p.Identifier != null && p.Identifier.Length <= GameConstants.MaxWordLength && !p.Identifier.Contains("-"))
            .ToListAsync();

        var pokemonList = pokemonCursor
            .Select(p => p.Identifier?.ToLower())
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();

        var selectedWords = pokemonList.OrderBy(_ => Random.Next()).Take(GameConstants.WordSearchWordCount).ToList();
        var grid = CreateGrid();
        var wordCoordinates = new Dictionary<string, List<(int Row, int Col)>>();

        foreach (var word in selectedWords)
        {
            var (success, coords) = AddWordToGrid(word, grid);
            if (success)
                wordCoordinates[word] = coords;
        }

        FillRemainingCells(grid);

        return new WordSearchGameState
        {
            UserId = userId,
            Words = selectedWords,
            Grid = grid,
            WordCoordinates = wordCoordinates,
            FoundWords = new List<string>(),
            StartTime = DateTimeOffset.UtcNow,
            IsActive = true,
            GameId = Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    ///     Creates an empty grid filled with placeholder characters.
    /// </summary>
    /// <returns>A 2D character array representing the empty grid.</returns>
    private static char[,] CreateGrid()
    {
        var grid = new char[GameConstants.WordSearchRows, GameConstants.WordSearchColumns];
        for (var row = 0; row < GameConstants.WordSearchRows; row++)
        for (var col = 0; col < GameConstants.WordSearchColumns; col++)
            grid[row, col] = '.';
        return grid;
    }

    /// <summary>
    ///     Attempts to add a word to the grid horizontally or vertically.
    /// </summary>
    /// <param name="word">The word to add to the grid.</param>
    /// <param name="grid">The grid to add the word to.</param>
    /// <returns>A tuple indicating success and the coordinates where the word was placed.</returns>
    private static (bool Success, List<(int Row, int Col)> Coordinates) AddWordToGrid(string word, char[,] grid)
    {
        var attempts = 0;
        while (attempts < 100)
        {
            var direction = Random.Next(2) == 0 ? "horizontal" : "vertical";

            if (direction == "horizontal")
            {
                var x = Random.Next(0, GameConstants.WordSearchColumns - word.Length);
                var y = Random.Next(0, GameConstants.WordSearchRows);

                // Check if space is available
                var canPlace = true;
                for (var i = 0; i < word.Length; i++)
                {
                    if (grid[y, x + i] != '.')
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (canPlace)
                {
                    var coords = new List<(int Row, int Col)>();
                    for (var i = 0; i < word.Length; i++)
                    {
                        grid[y, x + i] = char.ToUpper(word[i]);
                        coords.Add((y, x + i));
                    }

                    return (true, coords);
                }
            }
            else // vertical
            {
                var x = Random.Next(0, GameConstants.WordSearchColumns);
                var y = Random.Next(0, GameConstants.WordSearchRows - word.Length);

                // Check if space is available
                var canPlace = true;
                for (var i = 0; i < word.Length; i++)
                {
                    if (grid[y + i, x] != '.')
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (canPlace)
                {
                    var coords = new List<(int Row, int Col)>();
                    for (var i = 0; i < word.Length; i++)
                    {
                        grid[y + i, x] = char.ToUpper(word[i]);
                        coords.Add((y + i, x));
                    }

                    return (true, coords);
                }
            }

            attempts++;
        }

        return (false, new List<(int Row, int Col)>());
    }

    /// <summary>
    ///     Fills empty cells in the grid with random letters.
    /// </summary>
    /// <param name="grid">The grid to fill with random letters.</param>
    private static void FillRemainingCells(char[,] grid)
    {
        for (var row = 0; row < GameConstants.WordSearchRows; row++)
        for (var col = 0; col < GameConstants.WordSearchColumns; col++)
            if (grid[row, col] == '.')
                grid[row, col] = (char)Random.Next('A', 'Z' + 1);
    }

    /// <summary>
    ///     Handles a user's guess for the word search game.
    /// </summary>
    /// <param name="gameState">The current game state.</param>
    /// <param name="guess">The user's guess.</param>
    /// <returns>The result of the guess attempt.</returns>
    public WordSearchGuessResult HandleGuess(WordSearchGameState gameState, string guess)
    {
        if (!gameState.IsActive)
            return new WordSearchGuessResult { Success = false, Message = "Game is not active." };

        if (DateTimeOffset.UtcNow - gameState.StartTime > TimeSpan.FromSeconds(GameConstants.WordSearchGameTimeSeconds))
        {
            gameState.IsActive = false;
            return new WordSearchGuessResult 
            { 
                Success = false, 
                Message = "Time's up!", 
                TimedOut = true 
            };
        }

        var normalizedGuess = guess.ToLower().Trim();
        var lowerWords = gameState.Words.Select(w => w.ToLower()).ToList();
        var lowerFoundWords = gameState.FoundWords.Select(w => w.ToLower()).ToList();

        if (!lowerWords.Contains(normalizedGuess))
            return new WordSearchGuessResult { Success = false, Message = "That's not one of the hidden words." };

        if (lowerFoundWords.Contains(normalizedGuess))
            return new WordSearchGuessResult { Success = false, Message = "You already found that word!" };

        // Add to found words
        gameState.FoundWords.Add(normalizedGuess);

        // Check if all words found
        var allFound = gameState.FoundWords.Count == gameState.Words.Count;
        if (allFound)
        {
            gameState.IsActive = false;
            return new WordSearchGuessResult
            {
                Success = true,
                Message = "Congratulations! You found all the words!",
                WordFound = normalizedGuess,
                GameComplete = true
            };
        }

        return new WordSearchGuessResult
        {
            Success = true,
            Message = $"Great! You found '{normalizedGuess}'. Keep looking for more!",
            WordFound = normalizedGuess,
            GameComplete = false
        };
    }

    /// <summary>
    ///     Generates a word search image using SkiaSharp.
    /// </summary>
    /// <param name="gameState">The current game state.</param>
    /// <param name="backgroundImagePath">Optional path to a background image.</param>
    /// <returns>A stream containing the generated PNG image.</returns>
    public async Task<Stream> GenerateWordSearchImageAsync(WordSearchGameState gameState, string? backgroundImagePath = null)
    {
        const int imageWidth = 600;
        const int imageHeight = 600;

        using var surface = SKSurface.Create(new SKImageInfo(imageWidth, imageHeight));
        var canvas = surface.Canvas;

        // Load and draw background if provided
        if (!string.IsNullOrEmpty(backgroundImagePath) && File.Exists(backgroundImagePath))
        {
            using var backgroundBitmap = SKBitmap.Decode(backgroundImagePath);
            if (backgroundBitmap != null)
            {
                canvas.DrawBitmap(backgroundBitmap, new SKRect(0, 0, imageWidth, imageHeight));
            }
        }
        else
        {
            // Draw a simple gradient background
            using var paint = new SKPaint();
            paint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(imageWidth, imageHeight),
                new[] { SKColors.LightBlue, SKColors.LightGreen },
                SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, imageWidth, imageHeight, paint);
        }

        // Draw the grid
        await DrawGridAsync(canvas, gameState);

        // Create image and return as stream
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        var stream = new MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    ///     Draws the word search grid on the canvas.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    /// <param name="gameState">The current game state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DrawGridAsync(SKCanvas canvas, WordSearchGameState gameState)
    {
        using var textPaint = new SKPaint();
        textPaint.Color = SKColors.White;
        textPaint.IsAntialias = true;
        textPaint.TextSize = 18;
        textPaint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        using var foundTextPaint = new SKPaint();
        foundTextPaint.Color = SKColors.Red;
        foundTextPaint.IsAntialias = true;
        foundTextPaint.TextSize = 18;
        foundTextPaint.Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        // Get coordinates of found words
        var foundCoordinates = new HashSet<(int Row, int Col)>();
        foreach (var foundWord in gameState.FoundWords)
        {
            if (gameState.WordCoordinates.TryGetValue(foundWord, out var coords))
            {
                foreach (var coord in coords)
                    foundCoordinates.Add(coord);
            }
        }

        // Draw each cell
        for (var row = 0; row < GameConstants.WordSearchRows; row++)
        {
            for (var col = 0; col < GameConstants.WordSearchColumns; col++)
            {
                var letter = gameState.Grid[row, col].ToString();
                var paint = foundCoordinates.Contains((row, col)) ? foundTextPaint : textPaint;

                var x = col * (GameConstants.WordSearchBoxSize + 4) + GameConstants.WordSearchLeftOffset;
                var y = row * (GameConstants.WordSearchBoxSize + 4) + GameConstants.WordSearchTopOffset + 18; // Add offset for text baseline

                canvas.DrawText(letter, x, y, paint);

                // Draw underline for found letters
                if (foundCoordinates.Contains((row, col)))
                {
                    using var underlinePaint = new SKPaint();
                    underlinePaint.Color = SKColors.Red;
                    underlinePaint.StrokeWidth = 2;
                    canvas.DrawLine(x, y + 2, x + textPaint.MeasureText(letter), y + 2, underlinePaint);
                }
            }
        }

        await Task.CompletedTask; // Satisfy async requirement
    }

    /// <summary>
    ///     Creates a progress bar for the game timer.
    /// </summary>
    /// <param name="startTime">The start time of the game.</param>
    /// <returns>A tuple containing the progress bar, percentage text, and time remaining.</returns>
    public (string ProgressBar, string PercentageText, int TimeRemaining) BuildProgressBar(DateTimeOffset startTime)
    {
        var timeElapsed = DateTimeOffset.UtcNow - startTime;
        var timeRemaining = Math.Max(GameConstants.WordSearchGameTimeSeconds - (int)timeElapsed.TotalSeconds, 0);
        var percentageRemaining = (double)timeRemaining / GameConstants.WordSearchGameTimeSeconds;
        var filledBlocks = (int)(percentageRemaining * GameConstants.ProgressBarBlocks);

        var progressBar = new string(GameConstants.ProgressBarFilledBlock[0], filledBlocks) + 
                         new string(GameConstants.ProgressBarUnfilledBlock[0], GameConstants.ProgressBarBlocks - filledBlocks);
        var percentageText = $"{(int)(percentageRemaining * 100)}%";

        return (progressBar, percentageText, timeRemaining);
    }

    /// <summary>
    ///     Formats the prompt message with hints for the hidden words.
    /// </summary>
    /// <param name="gameState">The current game state.</param>
    /// <returns>A formatted prompt message with hints.</returns>
    public string FormatPromptMessage(WordSearchGameState gameState)
    {
        var (progressBar, percentageText, timeRemaining) = BuildProgressBar(gameState.StartTime);
        
        var prompt = new StringBuilder();
        prompt.AppendLine($"⏰ {progressBar} {percentageText}");
        prompt.AppendLine("Enter one of the hidden Pokemon names.");
        prompt.AppendLine($"Started: <t:{gameState.StartTime.ToUnixTimeSeconds()}:R>");
        prompt.AppendLine($"Time remaining: {timeRemaining} seconds");
        prompt.AppendLine();

        foreach (var word in gameState.Words)
        {
            // Create hint by showing 2 random letters
            var indices = Enumerable.Range(0, word.Length).OrderBy(_ => Random.Next()).Take(2).ToList();
            var maskedWord = new char[word.Length];
            
            for (var i = 0; i < word.Length; i++)
                maskedWord[i] = indices.Contains(i) ? word[i] : '×';

            prompt.AppendLine($"`{new string(maskedWord)}`");
        }

        return prompt.ToString();
    }
}