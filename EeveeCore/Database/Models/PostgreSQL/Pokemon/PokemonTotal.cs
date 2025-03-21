using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

[Table("poke_totals")]
public class PokemonTotal
{
    [Key] [Column("pokname")] public string? PokemonName { get; set; }

    [Column("count")] public ulong? Count { get; set; }
}