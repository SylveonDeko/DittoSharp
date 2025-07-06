using Discord.Interactions;

namespace EeveeCore.Modules.Games.Common;

/// <summary>
///     Modal for collecting word search guesses from users.
/// </summary>
public class WordSearchGuessModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "Enter Pokemon Name";

    /// <summary>
    ///     Gets or sets the user's guess input.
    /// </summary>
    [InputLabel("Enter your guess")]
    [ModalTextInput("guess", TextInputStyle.Short, "Enter a Pokemon Name", maxLength: 20)]
    public string Guess { get; set; } = null!;
}