using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

/// <summary>
///     Represents a Patreon supporter's store reset information in the EeveeCore Pokémon bot system.
///     This class tracks when Patreon supporters can access special store refreshes or rewards.
/// </summary>
[Table("patreon_store")]
public class PatreonStore
{
    /// <summary>
    ///     Gets or sets the Discord user ID of the Patreon supporter.
    ///     This serves as the primary key for the patron store record.
    /// </summary>
    [Key]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the Patreon store reset will occur.
    ///     Patreon supporters likely have access to special items or benefits that reset periodically.
    /// </summary>
    [Column("reset")]
    [Required]
    public ulong Reset { get; set; }
}