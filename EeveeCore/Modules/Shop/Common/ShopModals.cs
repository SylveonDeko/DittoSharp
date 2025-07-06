using Discord.Interactions;

namespace EeveeCore.Modules.Shop.Common;

/// <summary>
///     Modal for collecting purchase confirmation from users.
/// </summary>
public class PurchaseConfirmationModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "Confirm Purchase";

    /// <summary>
    ///     Gets or sets the confirmation input from the user.
    /// </summary>
    [InputLabel("Type 'CONFIRM' to purchase")]
    [ModalTextInput("confirmation", TextInputStyle.Short, "Type CONFIRM to proceed", maxLength: 10)]
    public string Confirmation { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the quantity to purchase.
    /// </summary>
    [InputLabel("Quantity (optional)")]
    [ModalTextInput("quantity", TextInputStyle.Short, "Enter quantity (default: 1)", maxLength: 3, minLength: 0)]
    public string? Quantity { get; set; }
}

/// <summary>
///     Modal for collecting custom search filters from users.
/// </summary>
public class ShopFilterModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "Filter Shop Items";

    /// <summary>
    ///     Gets or sets the item name filter.
    /// </summary>
    [InputLabel("Item Name (optional)")]
    [ModalTextInput("item_name", TextInputStyle.Short, "Enter item name to search for", maxLength: 50, minLength: 0)]
    public string? ItemName { get; set; }

    /// <summary>
    ///     Gets or sets the price range filter.
    /// </summary>
    [InputLabel("Price Range (optional)")]
    [ModalTextInput("price_range", TextInputStyle.Short, "e.g., 1000-5000", maxLength: 20, minLength: 0)]
    public string? PriceRange { get; set; }

    /// <summary>
    ///     Gets or sets the category filter.
    /// </summary>
    [InputLabel("Category (optional)")]
    [ModalTextInput("category", TextInputStyle.Short, "e.g., Boost, Charm, Pokeball", maxLength: 30, minLength: 0)]
    public string? Category { get; set; }

    /// <summary>
    ///     Gets or sets the rarity filter.
    /// </summary>
    [InputLabel("Rarity (optional)")]
    [ModalTextInput("rarity", TextInputStyle.Short, "e.g., Common, Rare, Legendary", maxLength: 30, minLength: 0)]
    public string? Rarity { get; set; }
}

/// <summary>
///     Modal for collecting gift recipient information.
/// </summary>
public class GiftItemModal : IModal
{
    /// <summary>
    ///     Gets the title of the modal.
    /// </summary>
    public string Title => "Gift Item";

    /// <summary>
    ///     Gets or sets the recipient user mention or ID.
    /// </summary>
    [InputLabel("Recipient")]
    [ModalTextInput("recipient", TextInputStyle.Short, "Enter @user or user ID", maxLength: 50)]
    public string Recipient { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the gift message.
    /// </summary>
    [InputLabel("Gift Message (optional)")]
    [ModalTextInput("message", TextInputStyle.Paragraph, "Enter a message for the recipient", maxLength: 500, minLength: 0)]
    public string? Message { get; set; }

    /// <summary>
    ///     Gets or sets the quantity to gift.
    /// </summary>
    [InputLabel("Quantity (optional)")]
    [ModalTextInput("quantity", TextInputStyle.Short, "Enter quantity (default: 1)", maxLength: 3, minLength: 0)]
    public string? Quantity { get; set; }
}