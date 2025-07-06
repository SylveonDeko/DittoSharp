namespace EeveeCore.Modules.Games.Models;

/// <summary>
///     Represents the state of a word search game.
/// </summary>
public class WordSearchGameState
{
    /// <summary>
    ///     Gets or sets the Discord user ID of the player.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the list of hidden words in the game.
    /// </summary>
    public List<string> Words { get; set; } = new();

    /// <summary>
    ///     Gets or sets the 2D character grid containing the word search puzzle.
    /// </summary>
    public char[,] Grid { get; set; } = new char[0, 0];

    /// <summary>
    ///     Gets or sets the mapping of words to their coordinates in the grid.
    /// </summary>
    public Dictionary<string, List<(int Row, int Col)>> WordCoordinates { get; set; } = new();

    /// <summary>
    ///     Gets or sets the list of words that have been found by the player.
    /// </summary>
    public List<string> FoundWords { get; set; } = new();

    /// <summary>
    ///     Gets or sets the start time of the game.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the game is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier for this game session.
    /// </summary>
    public string GameId { get; set; } = string.Empty;
}