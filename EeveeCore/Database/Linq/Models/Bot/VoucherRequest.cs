using LinqToDB.Mapping;


namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a request for a voucher in the EeveeCore Pokémon bot system.
///     This class tracks user requests for vouchers, including their status and associated artist.
/// </summary>
[Table(Name = "voucher_requests")]
public class VoucherRequest
{
    /// <summary>
    ///     Gets or sets the Discord message ID associated with this voucher request.
    ///     This serves as the primary key for the request record.
    /// </summary>
    [PrimaryKey]
    [Column(Name = "m_id")]
    public ulong MessageId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the user who made the request.
    /// </summary>
    [Column(Name = "u_id")]
    [NotNull]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the array of status flags for this voucher request.
    /// </summary>
    [Column(Name = "status")]
    public string[]? Status { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the artist assigned to this voucher request.
    /// </summary>
    [Column(Name = "artist")]
    public ulong? ArtistId { get; set; }
}