using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

/// <summary>
///     Represents a Pokémon listing in the market system of the EeveeCore Pokémon bot.
///     This class tracks Pokémon that are for sale, including their price and ownership information.
/// </summary>
[Table("market")]
public class Market
{
    /// <summary>
    ///     Gets or sets the unique identifier for this market listing.
    /// </summary>
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon being sold in this listing.
    /// </summary>
    [Column("poke")]
    [Required]
    public int PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the seller.
    /// </summary>
    [Column("owner")]
    [Required]
    public ulong OwnerId { get; set; }

    /// <summary>
    ///     Gets or sets the price of the Pokémon in MewCoins.
    /// </summary>
    [Column("price")]
    [Required]
    public int Price { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the buyer, if the Pokémon has been sold.
    ///     This value is null when the Pokémon is still listed for sale.
    /// </summary>
    [Column("buyer")]
    public ulong? BuyerId { get; set; }
}