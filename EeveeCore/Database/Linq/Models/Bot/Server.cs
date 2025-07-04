using LinqToDB.Mapping;


namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a Discord server configuration for the EeveeCore Pokémon bot.
///     Stores server-specific settings and preferences.
/// </summary>
[Table(Name = "servers")]
public class Server
{
    /// <summary>
    ///     Gets or sets the Discord server ID.
    ///     This is the primary key for the table.
    /// </summary>
    [PrimaryKey]
    [Column(Name = "serverid")]
    public ulong ServerId { get; set; }

    /// <summary>
    ///     Gets or sets the command prefix for the bot on this server.
    ///     Defaults to ";" if not specified.
    /// </summary>
    [Column(Name = "prefix")]
    [NotNull]
    public string Prefix { get; set; } = ";";

    /// <summary>
    ///     Gets or sets the language code for the server.
    ///     Limited to 2 characters (e.g., "en", "es", "ja").
    /// </summary>
    [Column(Name = "language")]
    public string? Language { get; set; }

    /// <summary>
    ///     Gets or sets the array of channel IDs where bot messages are redirected.
    /// </summary>
    [Column(Name = "redirects")]
    public ulong[]? Redirects { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to automatically delete Pokémon spawn messages after capture.
    /// </summary>
    [Column(Name = "delspawns")]
    public bool? DeleteSpawns { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to pin Pokémon spawn messages.
    /// </summary>
    [Column(Name = "pinspawns")]
    public bool? PinSpawns { get; set; }

    /// <summary>
    ///     Gets or sets the array of channel IDs where the bot is completely disabled.
    /// </summary>
    [Column(Name = "disabled_channels")]
    public ulong[]? DisabledChannels { get; set; }

    /// <summary>
    ///     Gets or sets the array of channel IDs where Pokémon spawns are disabled.
    /// </summary>
    [Column(Name = "spawns_disabled")]
    public ulong[]? SpawnsDisabled { get; set; }
}