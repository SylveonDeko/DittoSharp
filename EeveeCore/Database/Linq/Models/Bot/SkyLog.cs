using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a log entry in the EeveeCore Pokémon bot's logging system.
///     Used to track commands executed by users for auditing and debugging purposes.
/// </summary>
[Table("skylog")]
public class SkyLog
{
    /// <summary>
    ///     Gets or sets the unique identifier for this log entry.
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the user who executed the command.
    /// </summary>
    [Column("u_id")]
    [NotNull]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the name of the command that was executed.
    /// </summary>
    [Column("command")]
    [NotNull]
    public string Command { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the arguments provided with the command.
    /// </summary>
    [Column("args")]
    [NotNull]
    public string Arguments { get; set; } = null!;

    /// <summary>
    ///     Gets or sets a URL or identifier to jump to the message where the command was executed.
    /// </summary>
    [Column("jump")]
    public string? Jump { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the command was executed.
    /// </summary>
    [Column("time")]
    [NotNull]
    public DateTime Time { get; set; }

    /// <summary>
    ///     Gets or sets an administrative note or comment about this log entry.
    /// </summary>
    [Column("note")]
    public string? Note { get; set; }
}