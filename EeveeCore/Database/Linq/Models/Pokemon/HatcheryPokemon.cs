using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents the list of Pokémon species available for breeding or hatching in the EeveeCore system.
///     This entity serves as a reference table for hatchery operations and does not have a primary key.
/// </summary>
[Table("hatchery_pokemon")]
public class HatcheryPokemon
{
    /// <summary>
    ///     Gets or sets the array of Pokémon species names available for the hatchery system.
    /// </summary>
    [Column("array", DbType = "text[]")]
    public string[]? PokemonArray { get; set; }
}