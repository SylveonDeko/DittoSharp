namespace EeveeCore.DTOs.Dashboard;

/// <summary>
///     Data transfer object for the main dashboard summary information.
/// </summary>
public class DashboardSummaryDto
{
    /// <summary>
    ///     Gets or sets the total number of active users in the system.
    /// </summary>
    public int TotalActiveUsers { get; set; }

    /// <summary>
    ///     Gets or sets the total number of trades processed.
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded across all users.
    /// </summary>
    public decimal TotalValueTraded { get; set; }

    /// <summary>
    ///     Gets or sets the current number of open fraud alerts.
    /// </summary>
    public int OpenAlerts { get; set; }

    /// <summary>
    ///     Gets or sets the number of high-risk relationships detected.
    /// </summary>
    public int HighRiskRelationships { get; set; }

    /// <summary>
    ///     Gets or sets the number of suspicious clusters identified.
    /// </summary>
    public int SuspiciousClusters { get; set; }

    /// <summary>
    ///     Gets or sets the number of blocked accounts.
    /// </summary>
    public int BlockedAccounts { get; set; }

    /// <summary>
    ///     Gets or sets the fraud prevention rate (percentage).
    /// </summary>
    public double FraudPreventionRate { get; set; }

    /// <summary>
    ///     Gets or sets statistics for the last 24 hours.
    /// </summary>
    public DashboardPeriodStatsDto Last24Hours { get; set; } = new();

    /// <summary>
    ///     Gets or sets statistics for the last 7 days.
    /// </summary>
    public DashboardPeriodStatsDto Last7Days { get; set; } = new();

    /// <summary>
    ///     Gets or sets statistics for the last 30 days.
    /// </summary>
    public DashboardPeriodStatsDto Last30Days { get; set; } = new();

    /// <summary>
    ///     Gets or sets the most recent alerts.
    /// </summary>
    public List<FraudAlertDto> RecentAlerts { get; set; } = new();

    /// <summary>
    ///     Gets or sets the top risk users by score.
    /// </summary>
    public List<TopRiskUserDto> TopRiskUsers { get; set; } = new();

    /// <summary>
    ///     Gets or sets trending risk patterns.
    /// </summary>
    public List<RiskTrendDto> RiskTrends { get; set; } = new();

    /// <summary>
    ///     Gets or sets when this summary was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    ///     Gets or sets the data freshness timestamp.
    /// </summary>
    public DateTime DataAsOf { get; set; }
}

/// <summary>
///     Data transfer object for period-specific statistics.
/// </summary>
public class DashboardPeriodStatsDto
{
    /// <summary>
    ///     Gets or sets the number of new trades in this period.
    /// </summary>
    public int NewTrades { get; set; }

    /// <summary>
    ///     Gets or sets the value traded in this period.
    /// </summary>
    public decimal ValueTraded { get; set; }

    /// <summary>
    ///     Gets or sets the number of new alerts generated.
    /// </summary>
    public int NewAlerts { get; set; }

    /// <summary>
    ///     Gets or sets the number of new fraud cases opened.
    /// </summary>
    public int NewFraudCases { get; set; }

    /// <summary>
    ///     Gets or sets the number of blocked trades.
    /// </summary>
    public int BlockedTrades { get; set; }

    /// <summary>
    ///     Gets or sets the average risk score for trades in this period.
    /// </summary>
    public double AverageRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the percentage change from the previous period.
    /// </summary>
    public double PercentageChange { get; set; }
}

/// <summary>
///     Data transfer object for top risk users.
/// </summary>
public class TopRiskUserDto
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
    ///     Gets or sets the risk score.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the risk level classification.
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the total trades by this user.
    /// </summary>
    public int TotalTrades { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded by this user.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the primary risk reasons.
    /// </summary>
    public List<string> PrimaryRiskReasons { get; set; } = new();

    /// <summary>
    ///     Gets or sets the last activity timestamp.
    /// </summary>
    public DateTime LastActivity { get; set; }
}

/// <summary>
///     Data transfer object for risk trend information.
/// </summary>
public class RiskTrendDto
{
    /// <summary>
    ///     Gets or sets the trend identifier.
    /// </summary>
    public string TrendId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the trend description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the trend category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the severity level.
    /// </summary>
    public string Severity { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the number of instances detected.
    /// </summary>
    public int InstanceCount { get; set; }

    /// <summary>
    ///     Gets or sets the percentage change from previous period.
    /// </summary>
    public double PercentageChange { get; set; }

    /// <summary>
    ///     Gets or sets when this trend was first detected.
    /// </summary>
    public DateTime FirstDetected { get; set; }

    /// <summary>
    ///     Gets or sets when this trend was last observed.
    /// </summary>
    public DateTime LastObserved { get; set; }
}