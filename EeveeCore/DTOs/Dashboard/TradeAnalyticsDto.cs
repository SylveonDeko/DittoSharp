namespace EeveeCore.DTOs.Dashboard;

/// <summary>
///     Data transfer object for trade analytics and reporting.
/// </summary>
public class TradeAnalyticsDto
{
    /// <summary>
    ///     Gets or sets the total number of trades in the period.
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded in the period.
    /// </summary>
    public decimal TotalValueTraded { get; set; }

    /// <summary>
    ///     Gets or sets the number of unique traders.
    /// </summary>
    public int UniqueTraders { get; set; }

    /// <summary>
    ///     Gets or sets the average trade value.
    /// </summary>
    public decimal AverageTradeValue { get; set; }

    /// <summary>
    ///     Gets or sets the median trade value.
    /// </summary>
    public decimal MedianTradeValue { get; set; }

    /// <summary>
    ///     Gets or sets trade volume distribution by time.
    /// </summary>
    public List<TradeVolumeTimeSeriesDto> VolumeTimeSeries { get; set; } = new();

    /// <summary>
    ///     Gets or sets the most active trading pairs.
    /// </summary>
    public List<TradingPairStatsDto> TopTradingPairs { get; set; } = new();

    /// <summary>
    ///     Gets or sets the most traded items.
    /// </summary>
    public List<ItemTradeStatsDto> TopTradedItems { get; set; } = new();

    /// <summary>
    ///     Gets or sets statistics by trade type.
    /// </summary>
    public Dictionary<string, TradeTypeStatsDto> TradeTypeStats { get; set; } = new();

    /// <summary>
    ///     Gets or sets geographic distribution of trades.
    /// </summary>
    public Dictionary<string, int> GeographicDistribution { get; set; } = new();

    /// <summary>
    ///     Gets or sets trading patterns detected.
    /// </summary>
    public List<TradingPatternDto> DetectedPatterns { get; set; } = new();

    /// <summary>
    ///     Gets or sets suspicious trading activity summary.
    /// </summary>
    public SuspiciousActivitySummaryDto SuspiciousActivity { get; set; } = new();

    /// <summary>
    ///     Gets or sets the time period this analytics covers.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    ///     Gets or sets the end date of the analytics period.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    ///     Gets or sets when this analytics was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
///     Data transfer object for trade volume over time.
/// </summary>
public class TradeVolumeTimeSeriesDto
{
    /// <summary>
    ///     Gets or sets the timestamp for this data point.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the number of trades at this time.
    /// </summary>
    public int TradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded at this time.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the number of unique traders at this time.
    /// </summary>
    public int UniqueTraders { get; set; }

    /// <summary>
    ///     Gets or sets the average trade value at this time.
    /// </summary>
    public decimal AverageTradeValue { get; set; }
}

/// <summary>
///     Data transfer object for trading pair statistics.
/// </summary>
public class TradingPairStatsDto
{
    /// <summary>
    ///     Gets or sets the first user ID in the pair.
    /// </summary>
    public ulong User1Id { get; set; }

    /// <summary>
    ///     Gets or sets the second user ID in the pair.
    /// </summary>
    public ulong User2Id { get; set; }

    /// <summary>
    ///     Gets or sets the first username if available.
    /// </summary>
    public string? User1Username { get; set; }

    /// <summary>
    ///     Gets or sets the second username if available.
    /// </summary>
    public string? User2Username { get; set; }

    /// <summary>
    ///     Gets or sets the total number of trades between this pair.
    /// </summary>
    public int TradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded between this pair.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the average trade value for this pair.
    /// </summary>
    public decimal AverageTradeValue { get; set; }

    /// <summary>
    ///     Gets or sets the last trade timestamp.
    /// </summary>
    public DateTime LastTradeTime { get; set; }

    /// <summary>
    ///     Gets or sets the risk score for this trading relationship.
    /// </summary>
    public double RiskScore { get; set; }
}

/// <summary>
///     Data transfer object for item trading statistics.
/// </summary>
public class ItemTradeStatsDto
{
    /// <summary>
    ///     Gets or sets the item name or identifier.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the number of times this item was traded.
    /// </summary>
    public int TradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the total value of trades involving this item.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the average value per trade for this item.
    /// </summary>
    public decimal AverageValue { get; set; }

    /// <summary>
    ///     Gets or sets the number of unique traders for this item.
    /// </summary>
    public int UniqueTraders { get; set; }

    /// <summary>
    ///     Gets or sets the last trade timestamp for this item.
    /// </summary>
    public DateTime LastTradeTime { get; set; }
}

/// <summary>
///     Data transfer object for trade type statistics.
/// </summary>
public class TradeTypeStatsDto
{
    /// <summary>
    ///     Gets or sets the number of trades of this type.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///     Gets or sets the total value for this trade type.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the average value for this trade type.
    /// </summary>
    public decimal AverageValue { get; set; }

    /// <summary>
    ///     Gets or sets the percentage of total trades this type represents.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    ///     Gets or sets the average risk score for this trade type.
    /// </summary>
    public double AverageRiskScore { get; set; }
}

/// <summary>
///     Data transfer object for trading patterns.
/// </summary>
public class TradingPatternDto
{
    /// <summary>
    ///     Gets or sets the pattern identifier.
    /// </summary>
    public string PatternId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the pattern name.
    /// </summary>
    public string PatternName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the pattern description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the number of instances detected.
    /// </summary>
    public int InstanceCount { get; set; }

    /// <summary>
    ///     Gets or sets the confidence level of detection.
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    ///     Gets or sets the users involved in this pattern.
    /// </summary>
    public List<ulong> InvolvedUserIds { get; set; } = new();

    /// <summary>
    ///     Gets or sets when this pattern was first detected.
    /// </summary>
    public DateTime FirstDetected { get; set; }

    /// <summary>
    ///     Gets or sets when this pattern was last observed.
    /// </summary>
    public DateTime LastObserved { get; set; }
}

/// <summary>
///     Data transfer object for suspicious activity summary.
/// </summary>
public class SuspiciousActivitySummaryDto
{
    /// <summary>
    ///     Gets or sets the number of suspicious trades detected.
    /// </summary>
    public int SuspiciousTradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the value of suspicious trades.
    /// </summary>
    public decimal SuspiciousTradeValue { get; set; }

    /// <summary>
    ///     Gets or sets the number of blocked trades.
    /// </summary>
    public int BlockedTradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the value of blocked trades.
    /// </summary>
    public decimal BlockedTradeValue { get; set; }

    /// <summary>
    ///     Gets or sets the number of flagged users.
    /// </summary>
    public int FlaggedUserCount { get; set; }

    /// <summary>
    ///     Gets or sets the most common suspicious patterns.
    /// </summary>
    public List<string> CommonSuspiciousPatterns { get; set; } = new();

    /// <summary>
    ///     Gets or sets the false positive rate.
    /// </summary>
    public double FalsePositiveRate { get; set; }

    /// <summary>
    ///     Gets or sets the detection accuracy rate.
    /// </summary>
    public double DetectionAccuracy { get; set; }
}