using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents a completed achievement milestone for a user.
///     This table tracks which specific milestone thresholds users have reached.
/// </summary>
[Table("milestone_progress")]
public class MilestoneProgress
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this milestone completion.
    /// </summary>
    [PrimaryKey]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the type of achievement (e.g., "pokemon_caught", "breed_hexa").
    /// </summary>
    [PrimaryKey]
    [Column("achievement_type")]
    public string AchievementType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the milestone threshold value that was reached.
    /// </summary>
    [PrimaryKey]
    [Column("milestone_value")]
    public int MilestoneValue { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this milestone was first completed.
    /// </summary>
    [Column("completed_at")]
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}