using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a user's progress and ranking in the current event in the EeveeCore Pokémon bot system.
///     This class tracks event-specific experience, levels, and achievements.
/// </summary>
[Table("current_event")]
public class CurrentEvent
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this event record.
    ///     This serves as the primary key for the event record.
    /// </summary>
    [PrimaryKey]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the user's numerical ranking in the current event.
    /// </summary>
    [Column("event_ranking")]
    [NotNull]
    public int EventRanking { get; set; }

    /// <summary>
    ///     Gets or sets the title of the current event.
    /// </summary>
    [Column("event_title")]
    [NotNull]
    public string EventTitle { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the user's current event experience points.
    /// </summary>
    [Column("event_xp")]
    [NotNull]
    public int EventXp { get; set; }

    /// <summary>
    ///     Gets or sets the experience points required to reach the next event level.
    /// </summary>
    [Column("max_event_xp")]
    [NotNull]
    public int MaxEventXp { get; set; } = 100;

    /// <summary>
    ///     Gets or sets the user's current event level.
    /// </summary>
    [Column("event_level")]
    [NotNull]
    public int EventLevel { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the user's rank or achievement level in the current event.
    /// </summary>
    [Column("rank")]
    [NotNull]
    public string Rank { get; set; } = null!;
}