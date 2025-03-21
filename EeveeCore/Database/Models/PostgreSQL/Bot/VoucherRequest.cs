using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("voucher_requests")]
public class VoucherRequest
{
    [Key] [Column("m_id")] public ulong MessageId { get; set; }

    [Column("u_id")] [Required] public ulong UserId { get; set; }

    [Column("status", TypeName = "text[]")]
    public string[]? Status { get; set; }

    [Column("artist")] public ulong? ArtistId { get; set; }
}