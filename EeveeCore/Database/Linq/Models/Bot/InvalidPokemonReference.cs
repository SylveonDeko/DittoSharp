using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a record of an invalid Pokémon reference from the user's original array.
///     Used to track Pokémon IDs that were referenced in user arrays but don't exist in the Pokémon table.
/// </summary>
[Table("invalid_pokemon_references")]
public class InvalidPokemonReference
{
    /// <summary>
    ///     Gets or sets the Discord user ID that owned the invalid reference.
    /// </summary>
    [Column("user_id")]
    [NotNull]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the invalid Pokémon reference.
    ///     This ID doesn't exist in the Pokémon table.
    /// </summary>
    [Column("pokemon_id")]
    [NotNull]
    public ulong PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the original position of the reference in the user's Pokémon array.
    /// </summary>
    [Column("original_position")]
    [NotNull]
    public int OriginalPosition { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this invalid reference was recorded.
    /// </summary>
    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }
}