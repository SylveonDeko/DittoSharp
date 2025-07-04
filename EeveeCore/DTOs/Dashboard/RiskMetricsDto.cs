namespace EeveeCore.DTOs.Dashboard;

/// <summary>
///     Data transfer object for risk metrics and scoring information.
/// </summary>
public class RiskMetricsDto
{
    /// <summary>
    ///     Gets or sets the overall system risk level.
    /// </summary>
    public string OverallRiskLevel { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the average risk score across all users.
    /// </summary>
    public double AverageRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the median risk score.
    /// </summary>
    public double MedianRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the 95th percentile risk score.
    /// </summary>
    public double RiskScore95thPercentile { get; set; }

    /// <summary>
    ///     Gets or sets the distribution of users by risk level.
    /// </summary>
    public Dictionary<string, int> RiskDistribution { get; set; } = new();

    /// <summary>
    ///     Gets or sets the risk score thresholds used for classification.
    /// </summary>
    public RiskThresholdsDto Thresholds { get; set; } = new();

    /// <summary>
    ///     Gets or sets the top risk factors currently affecting the system.
    /// </summary>
    public List<RiskFactorDto> TopRiskFactors { get; set; } = new();

    /// <summary>
    ///     Gets or sets risk metrics by time period.
    /// </summary>
    public List<RiskTimeSeriesDto> RiskTimeSeries { get; set; } = new();

    /// <summary>
    ///     Gets or sets geographic risk distribution if applicable.
    /// </summary>
    public Dictionary<string, double> GeographicRiskDistribution { get; set; } = new();

    /// <summary>
    ///     Gets or sets when these metrics were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the time range these metrics cover.
    /// </summary>
    public TimeSpan TimeRange { get; set; }
}

/// <summary>
///     Data transfer object for risk threshold configuration.
/// </summary>
public class RiskThresholdsDto
{
    /// <summary>
    ///     Gets or sets the threshold for low risk classification.
    /// </summary>
    public double LowRiskThreshold { get; set; } = 0.3;

    /// <summary>
    ///     Gets or sets the threshold for medium risk classification.
    /// </summary>
    public double MediumRiskThreshold { get; set; } = 0.6;

    /// <summary>
    ///     Gets or sets the threshold for high risk classification.
    /// </summary>
    public double HighRiskThreshold { get; set; } = 0.8;

    /// <summary>
    ///     Gets or sets the threshold for automatic blocking.
    /// </summary>
    public double BlockingThreshold { get; set; } = 0.95;

    /// <summary>
    ///     Gets or sets the threshold for generating alerts.
    /// </summary>
    public double AlertThreshold { get; set; } = 0.7;
}

/// <summary>
///     Data transfer object for individual risk factors.
/// </summary>
public class RiskFactorDto
{
    /// <summary>
    ///     Gets or sets the factor name.
    /// </summary>
    public string FactorName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the factor description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the weight of this factor in scoring.
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    ///     Gets or sets the average score for this factor.
    /// </summary>
    public double AverageScore { get; set; }

    /// <summary>
    ///     Gets or sets the number of users affected by this factor.
    /// </summary>
    public int AffectedUsers { get; set; }

    /// <summary>
    ///     Gets or sets the trend direction for this factor.
    /// </summary>
    public string Trend { get; set; } = "Stable";

    /// <summary>
    ///     Gets or sets the percentage change from previous period.
    /// </summary>
    public double PercentageChange { get; set; }
}

/// <summary>
///     Data transfer object for risk metrics over time.
/// </summary>
public class RiskTimeSeriesDto
{
    /// <summary>
    ///     Gets or sets the timestamp for this data point.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the average risk score at this time.
    /// </summary>
    public double AverageRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the number of high-risk users at this time.
    /// </summary>
    public int HighRiskUserCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of alerts generated at this time.
    /// </summary>
    public int AlertCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of blocked trades at this time.
    /// </summary>
    public int BlockedTradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the total trade volume at this time.
    /// </summary>
    public decimal TradeVolume { get; set; }
}

/// <summary>
///     Data transfer object for user-specific risk breakdown.
/// </summary>
public class UserRiskBreakdownDto
{
    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the username if available.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     Gets or sets the overall risk score.
    /// </summary>
    public double OverallRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the risk level classification.
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the individual factor scores.
    /// </summary>
    public Dictionary<string, double> FactorScores { get; set; } = new();

    /// <summary>
    ///     Gets or sets the primary risk drivers for this user.
    /// </summary>
    public List<string> PrimaryRiskDrivers { get; set; } = new();

    /// <summary>
    ///     Gets or sets the risk score history for this user.
    /// </summary>
    public List<RiskTimeSeriesDto> RiskHistory { get; set; } = new();

    /// <summary>
    ///     Gets or sets when this breakdown was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; }
}