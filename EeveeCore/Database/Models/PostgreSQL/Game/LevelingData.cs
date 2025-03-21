using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("leveling_data")]
[Keyless]
public class LevelingData
{
    [Column("xp")] public int? Xp { get; set; }

    [Column("level")] public int? Level { get; set; }

    [Column("title")] public string? Title { get; set; }
}