using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a log entry for a gym battle in the EeveeCore Pokémon bot system.
///     This class records details about gym challenges, including participants and outcomes.
/// </summary>
[Table(Name = "gym_log")]
public class GymLog
{
    /// <summary>
    ///     Gets or sets the unique identifier for this gym log entry.
    /// </summary>
    [PrimaryKey]
    [Column(Name = "id")]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the gym that was challenged.
    /// </summary>
    [Column(Name = "gym"), NotNull]
    public string GymName { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the date and time when the gym challenge occurred.
    /// </summary>
    [Column(Name = "time"), NotNull]
    public DateTime Time { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the challenger.
    /// </summary>
    [Column(Name = "challenger"), NotNull]
    public ulong ChallengerId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the gym leader.
    ///     This could be an NPC or another player serving as a gym leader.
    /// </summary>
    [Column(Name = "leader"), NotNull]
    public ulong LeaderId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the challenger won the gym battle.
    /// </summary>
    [Column(Name = "win"), NotNull]
    public bool Win { get; set; }
}