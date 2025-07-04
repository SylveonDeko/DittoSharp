namespace EeveeCore.DTOs.Dashboard;

/// <summary>
///     Data transfer object for fraud alerts and notifications.
/// </summary>
public class FraudAlertDto
{
    /// <summary>
    ///     Gets or sets the unique alert identifier.
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the alert type (RiskThreshold, SuspiciousPattern, etc.).
    /// </summary>
    public string AlertType { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the severity level.
    /// </summary>
    public string Severity { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the alert title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the detailed alert description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the primary user ID involved in the alert.
    /// </summary>
    public ulong? PrimaryUserId { get; set; }

    /// <summary>
    ///     Gets or sets the primary username if available.
    /// </summary>
    public string? PrimaryUsername { get; set; }

    /// <summary>
    ///     Gets or sets additional user IDs involved in the alert.
    /// </summary>
    public List<ulong> InvolvedUserIds { get; set; } = new();

    /// <summary>
    ///     Gets or sets additional usernames if available.
    /// </summary>
    public List<string> InvolvedUsernames { get; set; } = new();

    /// <summary>
    ///     Gets or sets the risk score that triggered this alert.
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the threshold that was exceeded.
    /// </summary>
    public double? Threshold { get; set; }

    /// <summary>
    ///     Gets or sets when this alert was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    ///     Gets or sets when this alert was first detected.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    ///     Gets or sets the current status of the alert.
    /// </summary>
    public string Status { get; set; } = "Open";

    /// <summary>
    ///     Gets or sets who is assigned to investigate this alert.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    ///     Gets or sets when this alert was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    ///     Gets or sets when this alert was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    ///     Gets or sets the resolution notes.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    ///     Gets or sets the tags associated with this alert.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    ///     Gets or sets the evidence associated with this alert.
    /// </summary>
    public List<AlertEvidenceDto> Evidence { get; set; } = new();

    /// <summary>
    ///     Gets or sets recommended actions for this alert.
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>
    ///     Gets or sets related trade IDs if applicable.
    /// </summary>
    public List<long> RelatedTradeIds { get; set; } = new();

    /// <summary>
    ///     Gets or sets related case IDs if applicable.
    /// </summary>
    public List<string> RelatedCaseIds { get; set; } = new();

    /// <summary>
    ///     Gets or sets the confidence level of this alert (0.0 to 1.0).
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    ///     Gets or sets whether this is a false positive.
    /// </summary>
    public bool? IsFalsePositive { get; set; }

    /// <summary>
    ///     Gets or sets additional metadata for the alert.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
///     Data transfer object for alert evidence.
/// </summary>
public class AlertEvidenceDto
{
    /// <summary>
    ///     Gets or sets the evidence type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the evidence description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the evidence value or data.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this evidence was collected.
    /// </summary>
    public DateTime CollectedAt { get; set; }

    /// <summary>
    ///     Gets or sets the severity of this evidence.
    /// </summary>
    public string Severity { get; set; } = "Low";
}

/// <summary>
///     Data transfer object for alert statistics.
/// </summary>
public class AlertStatsDto
{
    /// <summary>
    ///     Gets or sets the total number of alerts.
    /// </summary>
    public int TotalAlerts { get; set; }

    /// <summary>
    ///     Gets or sets the number of open alerts.
    /// </summary>
    public int OpenAlerts { get; set; }

    /// <summary>
    ///     Gets or sets the number of resolved alerts.
    /// </summary>
    public int ResolvedAlerts { get; set; }

    /// <summary>
    ///     Gets or sets the number of false positive alerts.
    /// </summary>
    public int FalsePositiveAlerts { get; set; }

    /// <summary>
    ///     Gets or sets alerts by severity level.
    /// </summary>
    public Dictionary<string, int> AlertsBySeverity { get; set; } = new();

    /// <summary>
    ///     Gets or sets alerts by type.
    /// </summary>
    public Dictionary<string, int> AlertsByType { get; set; } = new();

    /// <summary>
    ///     Gets or sets the average resolution time in hours.
    /// </summary>
    public double AverageResolutionTimeHours { get; set; }

    /// <summary>
    ///     Gets or sets the false positive rate.
    /// </summary>
    public double FalsePositiveRate { get; set; }

    /// <summary>
    ///     Gets or sets when these statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}