using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
/// Represents the list of Pokémon species available for breeding or hatching in the EeveeCore system.
/// This keyless entity serves as a reference table for hatchery operations.
/// </summary>
[Table("hatchery_pokemon")]
[Keyless]
public class HatcheryPokemon
{
    /// <summary>
    /// Gets or sets the array of Pokémon species names available for the hatchery system.
    /// </summary>
    [Column("array", TypeName = "text[]")] public string[]? PokemonArray { get; set; }
}