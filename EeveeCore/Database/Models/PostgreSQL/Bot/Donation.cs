using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
/// Represents a donation record to the EeveeCore Pokémon bot.
/// Tracks donor information, amounts, and transaction details.
/// This class uses the user ID as the primary key.
/// </summary>
[Table("donations")]
public class Donation
{
    /// <summary>
    /// Gets or sets the Discord user ID of the donor.
    /// This is the primary key for the table.
    /// </summary>
    [Key]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    /// Gets or sets the donation amount.
    /// </summary>
    [Column("amount")]
    [Required]
    public int Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID of the donation.
    /// Used to prevent duplicate donation processing.
    /// </summary>
    [Column("txn_id")]
    [Required]
    public string TransactionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date when the donation was made.
    /// </summary>
    [Column("date_donated")]
    public DateOnly? DateDonated { get; set; }
}