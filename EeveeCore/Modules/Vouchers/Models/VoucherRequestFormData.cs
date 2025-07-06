namespace EeveeCore.Modules.Vouchers.Models;

/// <summary>
///     Represents the form data collected during voucher request creation.
/// </summary>
public class VoucherRequestFormData
{
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
    ///     Gets a value indicating whether all required fields have been filled.
    /// </summary>
    public bool IsComplete => !string.IsNullOrWhiteSpace(Pokemon) &&
                             !string.IsNullOrWhiteSpace(Appearance) &&
                             !string.IsNullOrWhiteSpace(PaymentMethod);

    /// <summary>
    ///     Gets the completion percentage of the form.
    /// </summary>
    public int CompletionPercentage
    {
        get
        {
            var filledFields = 0;
            if (!string.IsNullOrWhiteSpace(Pokemon)) filledFields++;
            if (!string.IsNullOrWhiteSpace(Appearance)) filledFields++;
            if (!string.IsNullOrWhiteSpace(PaymentMethod)) filledFields++;
            if (Artist == 0) filledFields++;
            
            return (filledFields * 100) / 4; // 4 total fields
        }
    }
}