using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents the ownership relationship between a user and their Pokémon.
///     This entity maintains the exact order of Pokémon in a user's collection.
/// </summary>
[Table("user_pokemon_ownership")]
public class UserPokemonOwnership
{
    /// <summary>
    ///     Gets or sets the unique identifier for this ownership relationship.
    /// </summary>
    [PrimaryKey]
    [Identity]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the owner.
    /// </summary>
    [Column("user_id")]
    [NotNull]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the Pokémon ID being owned.
    /// </summary>
    [Column("pokemon_id")]
    [NotNull]
    public ulong PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the position of this Pokémon in the user's collection.
    ///     This preserves the exact order from the original array.
    /// </summary>
    [Column("position")]
    [NotNull]
    public ulong Position { get; set; }
}