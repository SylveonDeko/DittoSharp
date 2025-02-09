using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("hatchery_pokemon")]
[Keyless]
public class HatcheryPokemon
{
    [Column("array", TypeName = "text[]")]
    public string[]? PokemonArray { get; set; }
}