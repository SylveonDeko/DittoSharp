using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("gifts")]
public class Gift
{
    [Key] [Column("gift_id")] public ulong GiftId { get; set; }

    [Column("current_owner_id")]
    [Required]
    public ulong CurrentOwnerId { get; set; }

    [Column("previous_owner_id")] public ulong? PreviousOwnerId { get; set; }

    [Column("status")]
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = null!;

    [Column("transformation_level")] public int? TransformationLevel { get; set; }

    [Column("is_locked")] public bool? IsLocked { get; set; }

    [Column("lock_expiration")] public DateTime? LockExpiration { get; set; }

    [Column("gift_history", TypeName = "bigint[]")]
    public long[]? GiftHistory { get; set; }

    [Column("creation_timestamp")] public DateTime? CreationTimestamp { get; set; }

    [Column("gift_history_with_time", TypeName = "jsonb")]
    public string? GiftHistoryWithTime { get; set; } = "[]";
}