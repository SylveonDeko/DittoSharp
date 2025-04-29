using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EeveeCore.Database.Models.PostgreSQL.Bot;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
///     Represents the ownership relationship between a user and their Pokémon.
///     This entity maintains the exact order of Pokémon in a user's collection.
/// </summary>
[Table("user_pokemon_ownership")]
[Index(nameof(UserId))]
[Index(nameof(UserId), nameof(Position), IsUnique = true)]
[Index(nameof(PokemonId))]
public class UserPokemonOwnership
{
    /// <summary>
    ///     Gets or sets the unique identifier for this ownership relationship.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the owner.
    /// </summary>
    [Column("user_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the Pokémon ID being owned.
    /// </summary>
    [Column("pokemon_id")]
    public ulong PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the position of this Pokémon in the user's collection.
    ///     This preserves the exact order from the original array.
    /// </summary>
    [Column("position")]
    public int Position { get; set; }

    /// <summary>
    ///     Gets or sets the user who owns this Pokémon.
    /// </summary>
    public User User { get; set; }
}