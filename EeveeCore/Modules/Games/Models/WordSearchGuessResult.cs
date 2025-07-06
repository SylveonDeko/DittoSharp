namespace EeveeCore.Modules.Games.Models;

/// <summary>
///     Represents the result of a word search guess.
/// </summary>
public class WordSearchGuessResult
{
    /// <summary>
    ///     Gets or sets a value indicating whether the guess was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the message to display to the user.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the word that was found, if any.
    /// </summary>
    public string? WordFound { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the game is complete.
    /// </summary>
    public bool GameComplete { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the game has timed out.
    /// </summary>
    public bool TimedOut { get; set; }
}