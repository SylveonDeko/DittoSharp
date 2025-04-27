using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
/// Represents a Discord server configuration for the EeveeCore Pokémon bot.
/// Stores server-specific settings and preferences.
/// </summary>
[Table("servers")]
public class Server
{
    /// <summary>
    /// Gets or sets the Discord server ID.
    /// This is the primary key for the table.
    /// </summary>
    [Key]
    [Column("serverid")]
    public ulong ServerId { get; set; }

    /// <summary>
    /// Gets or sets the command prefix for the bot on this server.
    /// Defaults to ";" if not specified.
    /// </summary>
    [Column("prefix")]
    [Required]
    public string Prefix { get; set; } = ";";

    /// <summary>
    /// Gets or sets the language code for the server.
    /// Limited to 2 characters (e.g., "en", "es", "ja").
    /// </summary>
    [Column("language")]
    [StringLength(2)]
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the array of channel IDs where bot messages are redirected.
    /// </summary>
    [Column("redirects", TypeName = "bigint[]")]
    public long[]? Redirects { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically delete Pokémon spawn messages after capture.
    /// </summary>
    [Column("delspawns")]
    public bool? DeleteSpawns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to pin Pokémon spawn messages.
    /// </summary>
    [Column("pinspawns")]
    public bool? PinSpawns { get; set; }

    /// <summary>
    /// Gets or sets the array of channel IDs where the bot is completely disabled.
    /// </summary>
    [Column("disabled_channels", TypeName = "bigint[]")]
    public long[]? DisabledChannels { get; set; }

    /// <summary>
    /// Gets or sets the array of channel IDs where Pokémon spawns are disabled.
    /// </summary>
    [Column("spawns_disabled", TypeName = "bigint[]")]
    public long[]? SpawnsDisabled { get; set; }
}