using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("patreon_store")]
public class PatreonStore
{
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    [Column("reset")] [Required] public ulong Reset { get; set; }
}