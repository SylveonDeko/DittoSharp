namespace EeveeCore.DTOs.Network;

/// <summary>
///     Data transfer object for account cluster information.
/// </summary>
public class NetworkClusterDto
{
    /// <summary>
    ///     Gets or sets the unique identifier for this cluster.
    /// </summary>
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the user IDs in the cluster.
    /// </summary>
    public List<ulong> UserIds { get; set; } = new();

    /// <summary>
    ///     Gets or sets the usernames if available.
    /// </summary>
    public List<string> Usernames { get; set; } = new();

    /// <summary>
    ///     Gets or sets the size of the cluster.
    /// </summary>
    public int Size { get; set; }

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
    ///     Gets or sets the number of internal trades within the cluster.
    /// </summary>
    public int InternalTradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded internally.
    /// </summary>
    public decimal TotalInternalValue { get; set; }

    /// <summary>
    ///     Gets or sets the ratio of internal to external trades.
    /// </summary>
    public double InternalTradeRatio { get; set; }

    /// <summary>
    ///     Gets or sets the average account age of cluster members.
    /// </summary>
    public double AverageAccountAge { get; set; }

    /// <summary>
    ///     Gets or sets when this cluster was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    ///     Gets or sets the time window used for detection.
    /// </summary>
    public int TimeWindowDays { get; set; }

    /// <summary>
    ///     Gets or sets additional metadata about the cluster.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}