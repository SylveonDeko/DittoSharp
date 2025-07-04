namespace EeveeCore.DTOs.Network;

/// <summary>
///     Data transfer object for network visualization data.
/// </summary>
public class NetworkVisualizationDto
{
    /// <summary>
    ///     Gets or sets the nodes in the network.
    /// </summary>
    public List<TradeNetworkNodeDto> Nodes { get; set; } = new();

    /// <summary>
    ///     Gets or sets the edges in the network.
    /// </summary>
    public List<TradeNetworkEdgeDto> Edges { get; set; } = new();

    /// <summary>
    ///     Gets or sets the detected clusters.
    /// </summary>
    public List<NetworkClusterDto> Clusters { get; set; } = new();

    /// <summary>
    ///     Gets or sets the detected flows.
    /// </summary>
    public List<TradeFlowDto> Flows { get; set; } = new();

    /// <summary>
    ///     Gets or sets the time window in days that this visualization represents.
    /// </summary>
    public int TimeWindowDays { get; set; }

    /// <summary>
    ///     Gets or sets when this visualization was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    ///     Gets or sets the total number of users in the network.
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    ///     Gets or sets the total number of relationships in the network.
    /// </summary>
    public int TotalRelationships { get; set; }

    /// <summary>
    ///     Gets or sets the center user ID if this is a user-centered view.
    /// </summary>
    public ulong? CenterUserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of hops from center if this is a user-centered view.
    /// </summary>
    public int? HopsFromCenter { get; set; }

    /// <summary>
    ///     Gets or sets layout parameters for visualization.
    /// </summary>
    public NetworkLayoutDto Layout { get; set; } = new();

    /// <summary>
    ///     Gets or sets filtering parameters applied to this visualization.
    /// </summary>
    public NetworkFilterDto Filters { get; set; } = new();

    /// <summary>
    ///     Gets or sets aggregate statistics for the network.
    /// </summary>
    public NetworkStatsDto Statistics { get; set; } = new();
}

/// <summary>
///     Data transfer object for network layout parameters.
/// </summary>
public class NetworkLayoutDto
{
    /// <summary>
    ///     Gets or sets the layout algorithm used (ForceDirected, Circular, Hierarchical, etc.).
    /// </summary>
    public string Algorithm { get; set; } = "ForceDirected";

    /// <summary>
    ///     Gets or sets layout-specific parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
///     Data transfer object for network filtering parameters.
/// </summary>
public class NetworkFilterDto
{
    /// <summary>
    ///     Gets or sets the minimum risk score to include.
    /// </summary>
    public double? MinRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the maximum risk score to include.
    /// </summary>
    public double? MaxRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the minimum trade count to include.
    /// </summary>
    public int? MinTradeCount { get; set; }

    /// <summary>
    ///     Gets or sets the minimum total value to include.
    /// </summary>
    public decimal? MinTotalValue { get; set; }

    /// <summary>
    ///     Gets or sets whether to include only suspicious relationships.
    /// </summary>
    public bool? OnlySuspicious { get; set; }

    /// <summary>
    ///     Gets or sets specific user IDs to include.
    /// </summary>
    public List<ulong>? IncludeUserIds { get; set; }

    /// <summary>
    ///     Gets or sets specific user IDs to exclude.
    /// </summary>
    public List<ulong>? ExcludeUserIds { get; set; }
}

/// <summary>
///     Data transfer object for network statistics.
/// </summary>
public class NetworkStatsDto
{
    /// <summary>
    ///     Gets or sets the network density (actual edges / possible edges).
    /// </summary>
    public double Density { get; set; }

    /// <summary>
    ///     Gets or sets the number of connected components.
    /// </summary>
    public int ConnectedComponents { get; set; }

    /// <summary>
    ///     Gets or sets the average clustering coefficient.
    /// </summary>
    public double AverageClusteringCoefficient { get; set; }

    /// <summary>
    ///     Gets or sets the average path length.
    /// </summary>
    public double AveragePathLength { get; set; }

    /// <summary>
    ///     Gets or sets the total value traded in the network.
    /// </summary>
    public decimal TotalValueTraded { get; set; }

    /// <summary>
    ///     Gets or sets the percentage of suspicious relationships.
    /// </summary>
    public double SuspiciousRelationshipPercentage { get; set; }

    /// <summary>
    ///     Gets or sets the most central user (highest betweenness centrality).
    /// </summary>
    public ulong? MostCentralUser { get; set; }

    /// <summary>
    ///     Gets or sets additional statistical measures.
    /// </summary>
    public Dictionary<string, object> AdditionalStats { get; set; } = new();
}