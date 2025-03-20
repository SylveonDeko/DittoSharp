using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Game;

[Table("gym_log")]
public class GymLog
{
    [Key] [Column("id")] public ulong Id { get; set; }

    [Column("gym")] [Required] public string GymName { get; set; } = null!;

    [Column("time")] [Required] public DateTime Time { get; set; }

    [Column("challenger")] [Required] public ulong ChallengerId { get; set; }

    [Column("leader")] [Required] public ulong LeaderId { get; set; }

    [Column("win")] [Required] public bool Win { get; set; }
}