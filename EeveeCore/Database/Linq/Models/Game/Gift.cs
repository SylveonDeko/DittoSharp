using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a gift in the EeveeCore Pokémon bot system.
///     This class tracks special items or Pokémon that can be gifted between users,
///     including their ownership history and status.
/// </summary>
[Table("gifts")]
public class Gift
{
    /// <summary>
    ///     Gets or sets the unique identifier for this gift.
    /// </summary>
    [PrimaryKey]
    [Column("gift_id")]
    public ulong GiftId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the current owner of the gift.
    /// </summary>
    [Column("current_owner_id")]
    [NotNull]
    public ulong CurrentOwnerId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the previous owner of the gift.
    /// </summary>
    [Column("previous_owner_id")]
    public long? PreviousOwnerId { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the gift (e.g., "Unopened", "Opened", "Transformed").
    /// </summary>
    [Column("status")]
    [NotNull]
    public string Status { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the transformation level of the gift, if applicable.
    ///     Gifts may transform or evolve after certain conditions are met.
    /// </summary>
    [Column("transformation_level")]
    public int? TransformationLevel { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the gift is locked from being traded or gifted further.
    /// </summary>
    [Column("is_locked")]
    public bool? IsLocked { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the lock on the gift expires.
    /// </summary>
    [Column("lock_expiration")]
    public DateTime? LockExpiration { get; set; }

    /// <summary>
    ///     Gets or sets the array of previous owner IDs, representing the gift's ownership history.
    /// </summary>
    [Column("gift_history")]
    public ulong[]? GiftHistory { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the gift was created.
    /// </summary>
    [Column("creation_timestamp")]
    public DateTime? CreationTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the detailed gift history with timestamps as a JSON string.
    ///     This provides a chronological record of the gift's transfers between users.
    /// </summary>
    [Column("gift_history_with_time")]
    public string? GiftHistoryWithTime { get; set; } = "[]";
}