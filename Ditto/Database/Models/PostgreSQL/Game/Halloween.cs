using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Game;

[Table("halloween")]
public class Halloween
{
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    [Column("candy")] [Required] public int Candy { get; set; }

    [Column("bone")] [Required] public int Bone { get; set; }

    [Column("pumpkin")] [Required] public int Pumpkin { get; set; }

    [Column("raffle")] [Required] public int Raffle { get; set; }

    [Column("username")] public string? Username { get; set; }
}