using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a donation record to the EeveeCore Pokémon bot.
///     Tracks donor information, amounts, and transaction details.
///     This class uses a keyless entity configuration.
/// </summary>
[Table("EeveeCore_donations")]
public class EeveeCoreDonation
{
    /// <summary>
    ///     Gets or sets the Discord user ID of the donor.
    /// </summary>
    [Column("u_id")]
    public ulong? UserId { get; set; }

    /// <summary>
    ///     Gets or sets the donation amount.
    /// </summary>
    [Column("amount")]
    public int? Amount { get; set; }

    /// <summary>
    ///     Gets or sets the transaction ID of the donation.
    ///     Used to prevent duplicate donation processing.
    /// </summary>
    [Column("txn_id")]
    public string? TransactionId { get; set; }

    /// <summary>
    ///     Gets or sets the date when the donation was made.
    /// </summary>
    [Column("date_donated")]
    public DateOnly? DateDonated { get; set; }
}