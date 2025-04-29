using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
///     Represents a request for a voucher in the EeveeCore Pokémon bot system.
///     This class tracks user requests for vouchers, including their status and associated artist.
/// </summary>
[Table("voucher_requests")]
public class VoucherRequest
{
    /// <summary>
    ///     Gets or sets the Discord message ID associated with this voucher request.
    ///     This serves as the primary key for the request record.
    /// </summary>
    [Key]
    [Column("m_id")]
    public ulong MessageId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the user who made the request.
    /// </summary>
    [Column("u_id")]
    [Required]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the array of status flags for this voucher request.
    /// </summary>
    [Column("status", TypeName = "text[]")]
    public string[]? Status { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the artist assigned to this voucher request.
    /// </summary>
    [Column("artist")]
    public ulong? ArtistId { get; set; }
}