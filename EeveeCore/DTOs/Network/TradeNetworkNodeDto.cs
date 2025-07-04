namespace EeveeCore.DTOs.Network;

/// <summary>
///     Data transfer object for trade network node information.
/// </summary>
public class TradeNetworkNodeDto
{
    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord username if available.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Gets or sets the account age in days.
    /// </summary>
    public double AccountAgeDays { get; set; }

    /// <summary>
    ///     Gets or sets the total number of trades this user has participated in.
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    ///     Gets or sets the total value this user has given in trades.
    /// </summary>
    public decimal TotalValueGiven { get; set; }

    /// <summary>
    ///     Gets or sets the total value this user has received in trades.
    /// </summary>
    public decimal TotalValueReceived { get; set; }

    /// <summary>
    ///     Gets or sets the net value flow for this user (received - given).
    /// </summary>
    public decimal NetValueFlow { get; set; }

    /// <summary>
    ///     Gets or sets the value imbalance ratio (higher value / lower value).
    /// </summary>
    public double ValueImbalanceRatio { get; set; }

    /// <summary>
    ///     Gets or sets the highest risk score associated with this user.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the risk level classification.
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the number of connections this node has.
    /// </summary>
    public int ConnectionCount { get; set; }

    /// <summary>
    ///     Gets or sets whether this node is flagged as suspicious.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    ///     Gets or sets additional flags for this node.
    /// </summary>
    public List<string> Flags { get; set; } = new();
}