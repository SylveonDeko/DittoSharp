using Discord.Interactions;

namespace EeveeCore.Modules.Vouchers.Common;

/// <summary>
///     Modal for collecting Pokemon information in voucher requests.
/// </summary>
public class VoucherPokemonModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "What Pokemon?";

    /// <summary>
    ///     Gets or sets the Pokemon name input.
    /// </summary>
    [InputLabel("What Pokemon would you like?")]
    [ModalTextInput("pokemon", TextInputStyle.Short, "e.g., Pikachu, Charizard", maxLength: 50)]
    public string Pokemon { get; set; } = null!;
}

/// <summary>
///     Modal for collecting appearance information in voucher requests.
/// </summary>
public class VoucherAppearanceModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "Pokemon Appearance";

    /// <summary>
    ///     Gets or sets the appearance description input.
    /// </summary>
    [InputLabel("How do you want the Pokemon to look?")]
    [ModalTextInput("appearance", TextInputStyle.Paragraph, "Describe the appearance, pose, style, etc.", maxLength: 500)]
    public string Appearance { get; set; } = null!;
}

/// <summary>
///     Modal for collecting payment information in voucher requests.
/// </summary>
public class VoucherPaymentModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "Payment Method";

    /// <summary>
    ///     Gets or sets the payment method input.
    /// </summary>
    [InputLabel("What is your method of payment?")]
    [ModalTextInput("payment", TextInputStyle.Paragraph, "e.g., Credits, PayPal, Commission trade", maxLength: 200)]
    public string PaymentMethod { get; set; } = null!;
}

/// <summary>
///     Modal for collecting artist preference in voucher requests.
/// </summary>
public class VoucherArtistModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "Artist Preference";

    /// <summary>
    ///     Gets or sets the artist preference input.
    /// </summary>
    [InputLabel("Do you have a specific artist in mind?")]
    [ModalTextInput("artist", TextInputStyle.Short, "Artist name or 'No preference'", maxLength: 100)]
    public ulong Artist { get; set; }
}