using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a Pokémon listing in the market system of the EeveeCore Pokémon bot.
///     This class tracks Pokémon that are for sale, including their price and ownership information.
/// </summary>
[Table(Name = "market")]
public class Market
{
    /// <summary>
    ///     Gets or sets the unique identifier for this market listing.
    /// </summary>
    [PrimaryKey]
    [Identity]
    [Column("id")]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon being sold in this listing.
    /// </summary>
    [Column(Name = "poke"), NotNull]
    public ulong PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the seller.
    /// </summary>
    [Column(Name = "owner"), NotNull]
    public ulong OwnerId { get; set; }

    /// <summary>
    ///     Gets or sets the price of the Pokémon in coins.
    /// </summary>
    [Column(Name = "price"), NotNull]
    public int Price { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the buyer, if the Pokémon has been sold.
    ///     This value is null when the Pokémon is still listed for sale,
    ///     0 when the listing was removed by the seller,
    ///     and a user ID when the Pokémon has been purchased.
    /// </summary>
    [Column(Name = "buyer"), Nullable]
    public ulong? BuyerId { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this listing was first created.
    /// </summary>
    [Column(Name = "listed_at"), NotNull]
    public DateTime ListedAt { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this listing was last updated (price changes, etc.).
    ///     This field is automatically updated by a database trigger.
    /// </summary>
    [Column(Name = "updated_at"), NotNull]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the number of times this listing has been viewed.
    ///     Used for popularity tracking and market analytics.
    /// </summary>
    [Column(Name = "view_count"), NotNull]
    public int ViewCount { get; set; }

    /// <summary>
    ///     Gets a value indicating whether this listing is currently active (available for purchase).
    /// </summary>
    [NotColumn]
    public bool IsActive => BuyerId == null;

    /// <summary>
    ///     Gets a value indicating whether this listing was removed by the seller.
    /// </summary>
    [NotColumn]
    public bool WasRemoved => BuyerId == 0;

    /// <summary>
    ///     Gets a value indicating whether this listing has been sold.
    /// </summary>
    [NotColumn]
    public bool WasSold => BuyerId.HasValue && BuyerId.Value > 0;

    /// <summary>
    ///     Gets the age of this listing in hours.
    /// </summary>
    [NotColumn]
    public double AgeInHours => (DateTime.UtcNow - ListedAt).TotalHours;

    /// <summary>
    ///     Gets the time since this listing was last updated in hours.
    /// </summary>
    [NotColumn]
    public double HoursSinceUpdate => (DateTime.UtcNow - UpdatedAt).TotalHours;
}