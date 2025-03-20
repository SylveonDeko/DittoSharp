using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("ditto_donations")]
[Keyless]
public class DittoDonation
{
    [Column("u_id")] public ulong? UserId { get; set; }

    [Column("amount")] public int? Amount { get; set; }

    [Column("txn_id")] public string? TransactionId { get; set; }

    [Column("date_donated")] public DateOnly? DateDonated { get; set; }
}