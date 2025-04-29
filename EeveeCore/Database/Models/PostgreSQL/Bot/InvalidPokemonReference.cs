using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

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
    [Required]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the invalid Pokémon reference.
    ///     This ID doesn't exist in the Pokémon table.
    /// </summary>
    [Column("pokemon_id")]
    [Required]
    public ulong PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the original position of the reference in the user's Pokémon array.
    /// </summary>
    [Column("original_position")]
    [Required]
    public int OriginalPosition { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this invalid reference was recorded.
    /// </summary>
    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; }
}