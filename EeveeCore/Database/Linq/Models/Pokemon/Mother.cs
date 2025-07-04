using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents a female Pokémon in the daycare or breeding system of the EeveeCore Pokémon bot.
///     This class tracks Pokémon that are available for breeding as mothers.
/// </summary>
[Table("mothers")]
public class Mother
{
    /// <summary>
    ///     Gets or sets the unique identifier of the Pokémon serving as a mother.
    ///     This serves as the primary key for the mother record.
    /// </summary>
    [PrimaryKey]
    [Column("pokemon_id")]
    public ulong PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the Pokémon's owner.
    /// </summary>
    [Column("owner")]
    public ulong? OwnerId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the Pokémon was placed in the daycare or breeding system.
    /// </summary>
    [Column("entry_time")]
    public DateTime? EntryTime { get; set; }
}