using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("EeveeCore_donations")]
[Keyless]
public class EeveeCoreDonation
{
    [Column("u_id")] public ulong? UserId { get; set; }

    [Column("amount")] public int? Amount { get; set; }

    [Column("txn_id")] public string? TransactionId { get; set; }

    [Column("date_donated")] public DateOnly? DateDonated { get; set; }
}