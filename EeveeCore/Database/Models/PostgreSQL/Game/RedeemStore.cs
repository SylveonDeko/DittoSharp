using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("redeemstore")]
public class RedeemStore
{
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    [Column("bought")] [Required] public int Bought { get; set; }

    [Column("restock")] [Required] public string Restock { get; set; } = "0";
}