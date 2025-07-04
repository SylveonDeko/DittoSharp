namespace EeveeCore.DTOs.Network;

/// <summary>
///     Data transfer object for trade flow information (multi-hop trades).
/// </summary>
public class TradeFlowDto
{
    /// <summary>
    ///     Gets or sets the unique identifier for this flow.
    /// </summary>
    public string FlowId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the path of the flow (sequence of user IDs).
    /// </summary>
    public List<ulong> Path { get; set; } = new();

    /// <summary>
    ///     Gets or sets the usernames in the flow path if available.
    /// </summary>
    public List<string> PathUsernames { get; set; } = new();

    /// <summary>
    ///     Gets or sets the length of the flow path.
    /// </summary>
    public int PathLength { get; set; }

    /// <summary>
    ///     Gets or sets the total value in the flow.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the flow type (Linear, Circular, Funnel, etc.).
    /// </summary>
    public string FlowType { get; set; } = "Linear";

    /// <summary>
    ///     Gets or sets the suspicion score (0.0 to 1.0).
    /// </summary>
    public double SuspicionScore { get; set; }

    /// <summary>
    ///     Gets or sets the suspicion level classification.
    /// </summary>
    public string SuspicionLevel { get; set; } = "Low";

    /// <summary>
    ///     Gets or sets the reasons for suspicion.
    /// </summary>
    public List<string> SuspicionReasons { get; set; } = new();

    /// <summary>
    ///     Gets or sets the timestamp when the flow started.
    /// </summary>
    public DateTime? FlowStartTime { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the flow completed.
    /// </summary>
    public DateTime? FlowEndTime { get; set; }

    /// <summary>
    ///     Gets or sets the duration of the complete flow.
    /// </summary>
    public TimeSpan? FlowDuration { get; set; }

    /// <summary>
    ///     Gets or sets when this flow was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    ///     Gets or sets whether this is a circular flow (returns to start).
    /// </summary>
    public bool IsCircular { get; set; }

    /// <summary>
    ///     Gets or sets the velocity of the flow (value per day).
    /// </summary>
    public decimal FlowVelocity { get; set; }

    /// <summary>
    ///     Gets or sets additional metadata about the flow.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}