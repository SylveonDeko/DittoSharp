using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("donations")]
public class Donation
{
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    [Column("amount")] [Required] public int Amount { get; set; }

    [Column("txn_id")] [Required] public string TransactionId { get; set; } = null!;

    [Column("date_donated")] public DateOnly? DateDonated { get; set; }
}