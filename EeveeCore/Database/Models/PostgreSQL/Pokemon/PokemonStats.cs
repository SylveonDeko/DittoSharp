using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

[Table("p_stats")]
public class PokemonStats
{
    [Key]
    [Column("pokemon")]
    [StringLength(255)]
    public string Pokemon { get; set; } = null!;

    [Column("wins")] public int? Wins { get; set; }

    [Column("faints")] public int? Faints { get; set; }
}