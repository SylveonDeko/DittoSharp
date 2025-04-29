using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

/// <summary>
///     Represents a log entry for a gym battle in the EeveeCore Pokémon bot system.
///     This class records details about gym challenges, including participants and outcomes.
/// </summary>
[Table("gym_log")]
public class GymLog
{
    /// <summary>
    ///     Gets or sets the unique identifier for this gym log entry.
    /// </summary>
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the gym that was challenged.
    /// </summary>
    [Column("gym")]
    [Required]
    public string GymName { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the date and time when the gym challenge occurred.
    /// </summary>
    [Column("time")]
    [Required]
    public DateTime Time { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the challenger.
    /// </summary>
    [Column("challenger")]
    [Required]
    public ulong ChallengerId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the gym leader.
    ///     This could be an NPC or another player serving as a gym leader.
    /// </summary>
    [Column("leader")]
    [Required]
    public ulong LeaderId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the challenger won the gym battle.
    /// </summary>
    [Column("win")]
    [Required]
    public bool Win { get; set; }
}