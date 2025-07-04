namespace EeveeCore.DTOs.Network;

/// <summary>
///     Data transfer object for trade network edge (relationship) information.
/// </summary>
public class TradeNetworkEdgeDto
{
    /// <summary>
    ///     Gets or sets the source user ID.
    /// </summary>
    public ulong FromUserId { get; set; }

    /// <summary>
    ///     Gets or sets the target user ID.
    /// </summary>
    public ulong ToUserId { get; set; }

    /// <summary>
    ///     Gets or sets the source username if available.
    /// </summary>
    public string? FromUsername { get; set; }

    /// <summary>
    ///     Gets or sets the target username if available.
    /// </summary>
    public string? ToUsername { get; set; }

    /// <summary>
    ///     Gets or sets the total number of trades between these users.
    /// </summary>
    public int TradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded between these users.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the value imbalance ratio between these users.
    /// </summary>
    public double ValueImbalanceRatio { get; set; }

    /// <summary>
    ///     Gets or sets the risk score for this relationship.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the risk level classification.
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the timestamp of the first trade.
    /// </summary>
    public DateTime FirstTradeTime { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp of the last trade.
    /// </summary>
    public DateTime LastTradeTime { get; set; }

    /// <summary>
    ///     Gets or sets the duration of the trading relationship in days.
    /// </summary>
    public double RelationshipDurationDays { get; set; }

    /// <summary>
    ///     Gets or sets the trading frequency (trades per day).
    /// </summary>
    public double TradingFrequency { get; set; }

    /// <summary>
    ///     Gets or sets whether this relationship is flagged as suspicious.
    /// </summary>
    public bool IsSuspicious { get; set; }

    /// <summary>
    ///     Gets or sets the edge thickness for visualization (based on trade count or value).
    /// </summary>
    public double EdgeWeight { get; set; }

    /// <summary>
    ///     Gets or sets additional flags for this relationship.
    /// </summary>
    public List<string> Flags { get; set; } = new();
}