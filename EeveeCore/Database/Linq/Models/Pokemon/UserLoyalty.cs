using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents a user's loyalty points and daily activity tracking.
///     This table manages daily streaks, loyalty points, and related bonuses.
/// </summary>
[Table("user_loyalty")]
public class UserLoyalty
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this loyalty record.
    /// </summary>
    [PrimaryKey]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the total loyalty points accumulated by the user.
    /// </summary>
    [Column("loyalty_points")]
    public int LoyaltyPoints { get; set; }

    /// <summary>
    ///     Gets or sets the current consecutive daily login streak.
    /// </summary>
    [Column("daily_streak")]
    public int DailyStreak { get; set; }

    /// <summary>
    ///     Gets or sets the date of the user's last login for streak calculation.
    /// </summary>
    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this record was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the timestamp when this record was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}