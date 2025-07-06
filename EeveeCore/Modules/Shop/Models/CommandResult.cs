using Discord;

namespace EeveeCore.Modules.Shop.Models;

/// <summary>
///     Represents the result of a command operation, providing message content and optional embed.
/// </summary>
public class CommandResult
{
    /// <summary>
    ///     Gets or sets the message content to display to the user.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets an optional embed to display with the message.
    /// </summary>
    public Embed? Embed { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this response should be ephemeral (only visible to the user).
    /// </summary>
    public bool Ephemeral { get; set; } = false;

    /// <summary>
    ///     Creates a successful command result with a message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <param name="ephemeral">Whether the message should be ephemeral.</param>
    /// <returns>A successful CommandResult.</returns>
    public static CommandResult Success(string message, bool ephemeral = false)
    {
        return new CommandResult { Message = message, Ephemeral = ephemeral };
    }

    /// <summary>
    ///     Creates an error command result with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="ephemeral">Whether the message should be ephemeral.</param>
    /// <returns>An error CommandResult.</returns>
    public static CommandResult Error(string message, bool ephemeral = true)
    {
        return new CommandResult { Message = message, Ephemeral = ephemeral };
    }

    /// <summary>
    ///     Creates a command result with an embed.
    /// </summary>
    /// <param name="embed">The embed to display.</param>
    /// <param name="ephemeral">Whether the message should be ephemeral.</param>
    /// <returns>A CommandResult with embed.</returns>
    public static CommandResult WithEmbed(Embed embed, bool ephemeral = false)
    {
        return new CommandResult { Embed = embed, Ephemeral = ephemeral };
    }
}