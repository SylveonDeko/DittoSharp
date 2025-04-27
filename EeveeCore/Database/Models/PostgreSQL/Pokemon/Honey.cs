using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
/// Represents a honey spot in the EeveeCore Pokémon bot system.
/// Honey spots attract wild Pokémon of specific types to designated channels for a limited time.
/// </summary>
[Table("honey")]
public class Honey
{
    /// <summary>
    /// Gets or sets the unique identifier for this honey spot.
    /// </summary>
    [Key] [Column("id")] public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Discord channel ID where this honey spot is active.
    /// </summary>
    [Column("channel")] [Required] public ulong ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the Discord user ID of the player who placed the honey.
    /// </summary>
    [Column("owner")] [Required] public ulong OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the type of Pokémon attracted to this honey spot.
    /// This may determine which species or types of Pokémon can be encountered.
    /// </summary>
    [Column("type")] public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this honey spot expires.
    /// </summary>
    [Column("expires")] [Required] public int Expires { get; set; }
}