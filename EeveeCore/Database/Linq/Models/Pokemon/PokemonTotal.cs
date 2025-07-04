using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents total count data for each Pokémon species in the EeveeCore system.
///     This class tracks how many of each Pokémon species exist across all users.
/// </summary>
[Table("poke_totals")]
public class PokemonTotal
{
    /// <summary>
    ///     Gets or sets the name of the Pokémon species.
    ///     This serves as the primary key for the total count record.
    /// </summary>
    [PrimaryKey]
    [Column("pokname")]
    public string? PokemonName { get; set; }

    /// <summary>
    ///     Gets or sets the total number of this Pokémon species that exists in the system.
    /// </summary>
    [Column("count")]
    public long? Count { get; set; }
}