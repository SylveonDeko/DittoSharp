namespace EeveeCore.Modules.Vouchers.Models;

/// <summary>
///     Represents a voucher request submitted by a user.
/// </summary>
public class VoucherRequest
{
    /// <summary>
    ///     Gets or sets the unique identifier for this voucher request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the Discord user ID of the requester.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the Pokemon name being requested.
    /// </summary>
    public string Pokemon { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the description of how the Pokemon should look.
    /// </summary>
    public string Appearance { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the payment method for the request.
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the specific artist requested (if any).
    /// </summary>
    public ulong Artist { get; set; } 

    /// <summary>
    ///     Gets or sets the current status of the request.
    /// </summary>
    public List<string> Status { get; set; } = new() { "Created" };

    /// <summary>
    ///     Gets or sets the timestamp when the request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the Discord message ID associated with this request.
    /// </summary>
    public ulong? MessageId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord channel ID where the request was posted.
    /// </summary>
    public ulong? ChannelId { get; set; }

    /// <summary>
    ///     Gets or sets any additional notes or comments about the request.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}