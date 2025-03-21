using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("current_event")]
public class CurrentEvent
{
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    [Column("event_ranking")] [Required] public int EventRanking { get; set; }

    [Column("event_title")] [Required] public string EventTitle { get; set; } = null!;

    [Column("event_xp")] [Required] public int EventXp { get; set; }

    [Column("max_event_xp")] [Required] public int MaxEventXp { get; set; } = 100;

    [Column("event_level")] [Required] public int EventLevel { get; set; } = 1;

    [Column("rank")] [Required] public string Rank { get; set; } = null!;
}