using LinqToDB.Mapping;


namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a donation record to the EeveeCore Pokémon bot.
///     Tracks donor information, amounts, and transaction details.
///     This class uses the user ID as the primary key.
/// </summary>
[Table(Name = "donations")]
public class Donation
{
    /// <summary>
    ///     Gets or sets the Discord user ID of the donor.
    ///     This is the primary key for the table.
    /// </summary>
    [PrimaryKey]
    [Column(Name = "u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the donation amount.
    /// </summary>
    [Column(Name = "amount")]
    [NotNull]
    public int Amount { get; set; }

    /// <summary>
    ///     Gets or sets the transaction ID of the donation.
    ///     Used to prevent duplicate donation processing.
    /// </summary>
    [Column(Name = "txn_id")]
    [NotNull]
    public string TransactionId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the date when the donation was made.
    /// </summary>
    [Column(Name = "date_donated")]
    public DateOnly? DateDonated { get; set; }
}