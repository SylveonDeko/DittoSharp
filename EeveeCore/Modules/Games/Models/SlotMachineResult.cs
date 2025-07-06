namespace EeveeCore.Modules.Games.Models;

/// <summary>
///     Represents the result of a slot machine spin.
/// </summary>
public class SlotMachineResult
{
    /// <summary>
    ///     Gets or sets the first reel symbol.
    /// </summary>
    public string Reel1 { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the second reel symbol.
    /// </summary>
    public string Reel2 { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the third reel symbol.
    /// </summary>
    public string Reel3 { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether this was a winning spin.
    /// </summary>
    public bool IsWin { get; set; }

    /// <summary>
    ///     Gets or sets the type of win (if any).
    /// </summary>
    public string WinType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the payout multiplier for this spin.
    /// </summary>
    public decimal PayoutMultiplier { get; set; }

    /// <summary>
    ///     Gets or sets the formatted result message to display.
    /// </summary>
    public string ResultMessage { get; set; } = string.Empty;
}