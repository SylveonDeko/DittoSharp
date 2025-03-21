using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("cheststore")]
public class ChestStore
{
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    [Column("rare")] [Required] public int Rare { get; set; }

    [Column("mythic")] [Required] public int Mythic { get; set; }

    [Column("legend")] [Required] public int Legend { get; set; }

    [Column("restock")] [Required] public string Restock { get; set; } = "0";
}