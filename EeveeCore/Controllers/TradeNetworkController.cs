using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EeveeCore.DTOs.Network;
using EeveeCore.Services.Network;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for trade network analysis and visualization.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class TradeNetworkController : ControllerBase
{
    private readonly TradeNetworkGraphService _networkGraphService;
    private readonly NetworkAnalysisService _networkAnalysisService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeNetworkController"/> class.
    /// </summary>
    /// <param name="networkGraphService">The network graph service.</param>
    /// <param name="networkAnalysisService">The network analysis service.</param>
    public TradeNetworkController(
        TradeNetworkGraphService networkGraphService,
        NetworkAnalysisService networkAnalysisService)
    {
        _networkGraphService = networkGraphService;
        _networkAnalysisService = networkAnalysisService;
    }

    /// <summary>
    ///     Gets the complete trade network for visualization.
    /// </summary>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <param name="minRiskScore">Minimum risk score to include (optional)</param>
    /// <param name="maxNodes">Maximum number of nodes to return (default: 1000)</param>
    /// <returns>Network visualization data</returns>
    [HttpGet("visualization")]
    public async Task<ActionResult<NetworkVisualizationDto>> GetNetworkVisualization(
        [FromQuery] int timeWindowDays = 30,
        [FromQuery] double? minRiskScore = null,
        [FromQuery] int maxNodes = 1000)
    {
        try
        {
            Log.Information("Generating network visualization for {TimeWindow} days, minRisk={MinRisk}, maxNodes={MaxNodes}", 
                timeWindowDays, minRiskScore, maxNodes);

            var network = await _networkGraphService.GetTradeNetworkAsync(timeWindowDays);
            var clusters = await _networkAnalysisService.DetectAccountClustersAsync(timeWindowDays);
            var funnels = await _networkAnalysisService.DetectFunnelPatternsAsync(timeWindowDays);
            var circularFlows = await _networkAnalysisService.DetectCircularFlowsAsync(timeWindowDays);

            // Apply filtering
            var nodes = network.Nodes.Values.AsEnumerable();
            var edges = network.Edges.AsEnumerable();

            if (minRiskScore.HasValue)
            {
                nodes = nodes.Where(n => n.RiskScore >= minRiskScore.Value);
                edges = edges.Where(e => e.RiskScore >= minRiskScore.Value);
            }

            // Limit nodes if necessary
            if (nodes.Count() > maxNodes)
            {
                nodes = nodes.OrderByDescending(n => n.RiskScore).Take(maxNodes);
                var nodeIds = nodes.Select(n => n.UserId).ToHashSet();
                edges = edges.Where(e => nodeIds.Contains(e.FromUserId) && nodeIds.Contains(e.ToUserId));
            }

            var nodeList = nodes.ToList();
            var edgeList = edges.ToList();

            // Convert to DTOs
            var nodesDtos = nodeList.Select(n => new TradeNetworkNodeDto
            {
                UserId = n.UserId,
                Username = n.Username,
                AccountAgeDays = n.AccountAgeDays,
                TotalTrades = n.TotalTrades,
                TotalValueGiven = n.TotalValueGiven,
                TotalValueReceived = n.TotalValueReceived,
                NetValueFlow = n.NetValueFlow,
                ValueImbalanceRatio = n.ValueImbalanceRatio,
                RiskScore = n.RiskScore,
                RiskLevel = n.RiskScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                ConnectionCount = n.ConnectionCount,
                IsSuspicious = n.RiskScore > 0.6,
                Flags = n.Flags.ToList()
            }).ToList();

            var edgesDtos = edgeList.Select(e => new TradeNetworkEdgeDto
            {
                FromUserId = e.FromUserId,
                ToUserId = e.ToUserId,
                FromUsername = nodeList.FirstOrDefault(n => n.UserId == e.FromUserId)?.Username,
                ToUsername = nodeList.FirstOrDefault(n => n.UserId == e.ToUserId)?.Username,
                TradeCount = e.TradeCount,
                TotalValue = e.TotalValue,
                ValueImbalanceRatio = e.ValueImbalanceRatio,
                RiskScore = e.RiskScore,
                RiskLevel = e.RiskScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                FirstTradeTime = e.FirstTradeTime,
                LastTradeTime = e.LastTradeTime,
                RelationshipDurationDays = (e.LastTradeTime - e.FirstTradeTime).TotalDays,
                TradingFrequency = e.TradeCount / Math.Max(1, (e.LastTradeTime - e.FirstTradeTime).TotalDays),
                IsSuspicious = e.RiskScore > 0.6,
                EdgeWeight = Math.Log10((double)(e.TotalValue + 1)),
                Flags = e.Flags.ToList()
            }).ToList();

            var clusterDtos = clusters.Select(c => new NetworkClusterDto
            {
                ClusterId = c.ClusterId,
                UserIds = c.UserIds,
                Usernames = c.UserIds.Select(id => nodeList.FirstOrDefault(n => n.UserId == id)?.Username ?? "Unknown").ToList(),
                Size = c.Size,
                SuspicionScore = c.SuspicionScore,
                SuspicionLevel = c.SuspicionScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                SuspicionReasons = c.SuspicionReasons,
                InternalTradeCount = c.InternalTradeCount,
                TotalInternalValue = c.TotalInternalValue,
                InternalTradeRatio = c.InternalTradeRatio,
                AverageAccountAge = c.AverageAccountAge,
                DetectedAt = c.DetectedAt,
                TimeWindowDays = c.TimeWindowDays,
                Metadata = c.Metadata
            }).ToList();

            // Combine funnel patterns and circular flows into flow DTOs
            var flowDtos = new List<TradeFlowDto>();
            
            // Add funnel patterns as flows
            flowDtos.AddRange(funnels.Select(f => new TradeFlowDto
            {
                FlowId = f.FlowId,
                Path = f.Path,
                PathUsernames = f.Path.Select(id => nodeList.FirstOrDefault(n => n.UserId == id)?.Username ?? "Unknown").ToList(),
                PathLength = f.PathLength,
                TotalValue = f.TotalValue,
                FlowType = f.FlowType,
                SuspicionScore = f.SuspicionScore,
                SuspicionLevel = f.SuspicionLevel,
                SuspicionReasons = f.SuspicionReasons,
                FlowStartTime = f.FlowStartTime,
                FlowEndTime = f.FlowEndTime,
                FlowDuration = f.FlowDuration,
                DetectedAt = f.DetectedAt,
                IsCircular = f.IsCircular,
                FlowVelocity = f.FlowVelocity,
                Metadata = f.Metadata
            }));
            
            // Add circular flows
            flowDtos.AddRange(circularFlows.Select(f => new TradeFlowDto
            {
                FlowId = f.FlowId,
                Path = f.Path,
                PathUsernames = f.Path.Select(id => nodeList.FirstOrDefault(n => n.UserId == id)?.Username ?? "Unknown").ToList(),
                PathLength = f.PathLength,
                TotalValue = f.TotalValue,
                FlowType = f.FlowType,
                SuspicionScore = f.SuspicionScore,
                SuspicionLevel = f.SuspicionLevel,
                SuspicionReasons = f.SuspicionReasons,
                FlowStartTime = f.FlowStartTime,
                FlowEndTime = f.FlowEndTime,
                FlowDuration = f.FlowDuration,
                DetectedAt = f.DetectedAt,
                IsCircular = f.IsCircular,
                FlowVelocity = f.FlowVelocity,
                Metadata = f.Metadata
            }));

            var visualization = new NetworkVisualizationDto
            {
                Nodes = nodesDtos,
                Edges = edgesDtos,
                Clusters = clusterDtos,
                Flows = flowDtos,
                TimeWindowDays = timeWindowDays,
                GeneratedAt = DateTime.UtcNow,
                TotalUsers = nodeList.Count,
                TotalRelationships = edgeList.Count,
                Layout = new NetworkLayoutDto
                {
                    Algorithm = "ForceDirected",
                    Parameters = new Dictionary<string, object>
                    {
                        ["nodeRepulsion"] = 100,
                        ["linkDistance"] = 50,
                        ["iterations"] = 300
                    }
                },
                Filters = new NetworkFilterDto
                {
                    MinRiskScore = minRiskScore,
                    OnlySuspicious = minRiskScore >= 0.6
                },
                Statistics = new NetworkStatsDto
                {
                    Density = nodeList.Count > 1 ? (double)edgeList.Count / (nodeList.Count * (nodeList.Count - 1) / 2) : 0,
                    ConnectedComponents = 1, // Would need actual graph analysis
                    AverageClusteringCoefficient = 0.3, // Would need actual calculation
                    AveragePathLength = 2.5, // Would need actual calculation
                    TotalValueTraded = edgeList.Sum(e => e.TotalValue),
                    SuspiciousRelationshipPercentage = edgeList.Count > 0 ? edgeList.Count(e => e.IsSuspicious) * 100.0 / edgeList.Count : 0,
                    MostCentralUser = nodeList.OrderByDescending(n => n.ConnectionCount).FirstOrDefault()?.UserId,
                    AdditionalStats = new Dictionary<string, object>
                    {
                        ["maxRiskScore"] = nodeList.Any() ? nodeList.Max(n => n.RiskScore) : 0,
                        ["averageAccountAge"] = nodeList.Any() ? nodeList.Average(n => n.AccountAgeDays) : 0
                    }
                }
            };

            return Ok(visualization);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating network visualization");
            return StatusCode(500, "Internal server error while generating network visualization");
        }
    }

    /// <summary>
    ///     Gets a user-centered network view for detailed analysis.
    /// </summary>
    /// <param name="userId">The center user ID</param>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <param name="hops">Number of hops from center user (default: 2)</param>
    /// <returns>User-centered network visualization</returns>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<NetworkVisualizationDto>> GetUserCenteredNetwork(
        ulong userId,
        [FromQuery] int timeWindowDays = 30,
        [FromQuery] int hops = 2)
    {
        try
        {
            Log.Information("Generating user-centered network for user {UserId}, {Hops} hops, {TimeWindow} days", 
                userId, hops, timeWindowDays);

            var network = await _networkGraphService.GetUserNetworkAsync(userId, hops, timeWindowDays);
            
            if (!network.Nodes.ContainsKey(userId))
            {
                return NotFound($"User {userId} not found in trade network");
            }

            var clusters = await _networkAnalysisService.DetectAccountClustersAsync(timeWindowDays);
            var funnels = await _networkAnalysisService.DetectFunnelPatternsAsync(timeWindowDays);
            var circularFlows = await _networkAnalysisService.DetectCircularFlowsAsync(timeWindowDays);

            // Convert to DTOs (similar to above but user-centered)
            var nodesDtos = network.Nodes.Values.Select(n => new TradeNetworkNodeDto
            {
                UserId = n.UserId,
                Username = n.Username,
                AccountAgeDays = n.AccountAgeDays,
                TotalTrades = n.TotalTrades,
                TotalValueGiven = n.TotalValueGiven,
                TotalValueReceived = n.TotalValueReceived,
                NetValueFlow = n.NetValueFlow,
                ValueImbalanceRatio = n.ValueImbalanceRatio,
                RiskScore = n.RiskScore,
                RiskLevel = n.RiskScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                ConnectionCount = n.ConnectionCount,
                IsSuspicious = n.RiskScore > 0.6,
                Flags = n.Flags.ToList()
            }).ToList();

            var edgesDtos = network.Edges.Select(e => new TradeNetworkEdgeDto
            {
                FromUserId = e.FromUserId,
                ToUserId = e.ToUserId,
                FromUsername = network.Nodes.TryGetValue(e.FromUserId, out var fromNode) ? fromNode.Username : null,
                ToUsername = network.Nodes.TryGetValue(e.ToUserId, out var toNode) ? toNode.Username : null,
                TradeCount = e.TradeCount,
                TotalValue = e.TotalValue,
                ValueImbalanceRatio = e.ValueImbalanceRatio,
                RiskScore = e.RiskScore,
                RiskLevel = e.RiskScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                FirstTradeTime = e.FirstTradeTime,
                LastTradeTime = e.LastTradeTime,
                RelationshipDurationDays = (e.LastTradeTime - e.FirstTradeTime).TotalDays,
                TradingFrequency = e.TradeCount / Math.Max(1, (e.LastTradeTime - e.FirstTradeTime).TotalDays),
                IsSuspicious = e.RiskScore > 0.6,
                EdgeWeight = Math.Log10((double)(e.TotalValue + 1)),
                Flags = e.Flags.ToList()
            }).ToList();

            var visualization = new NetworkVisualizationDto
            {
                Nodes = nodesDtos,
                Edges = edgesDtos,
                Clusters = clusters.Select(c => new NetworkClusterDto
                {
                    ClusterId = c.ClusterId,
                    UserIds = c.UserIds,
                    Size = c.Size,
                    SuspicionScore = c.SuspicionScore,
                    DetectedAt = c.DetectedAt
                }).ToList(),
                Flows = funnels.Select(f => new TradeFlowDto
                {
                    FlowId = f.FlowId,
                    Path = f.Path,
                    PathLength = f.PathLength,
                    TotalValue = f.TotalValue,
                    SuspicionScore = f.SuspicionScore,
                    DetectedAt = f.DetectedAt
                }).Concat(circularFlows.Select(f => new TradeFlowDto
                {
                    FlowId = f.FlowId,
                    Path = f.Path,
                    PathLength = f.PathLength,
                    TotalValue = f.TotalValue,
                    SuspicionScore = f.SuspicionScore,
                    DetectedAt = f.DetectedAt
                })).ToList(),
                TimeWindowDays = timeWindowDays,
                GeneratedAt = DateTime.UtcNow,
                TotalUsers = network.Nodes.Count,
                TotalRelationships = network.Edges.Count,
                CenterUserId = userId,
                HopsFromCenter = hops,
                Layout = new NetworkLayoutDto
                {
                    Algorithm = "Radial",
                    Parameters = new Dictionary<string, object>
                    {
                        ["centerUserId"] = userId,
                        ["radius"] = 100 * hops
                    }
                }
            };

            return Ok(visualization);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating user-centered network for user {UserId}", userId);
            return StatusCode(500, "Internal server error while generating user network");
        }
    }

    /// <summary>
    ///     Gets detected clusters with optional filtering.
    /// </summary>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <param name="minSuspicionScore">Minimum suspicion score to include</param>
    /// <param name="minSize">Minimum cluster size to include</param>
    /// <returns>List of detected clusters</returns>
    [HttpGet("clusters")]
    public async Task<ActionResult<IEnumerable<NetworkClusterDto>>> GetClusters(
        [FromQuery] int timeWindowDays = 30,
        [FromQuery] double? minSuspicionScore = null,
        [FromQuery] int? minSize = null)
    {
        try
        {
            Log.Information("Fetching clusters for {TimeWindow} days, minSuspicion={MinSuspicion}, minSize={MinSize}",
                timeWindowDays, minSuspicionScore, minSize);

            var network = await _networkGraphService.GetTradeNetworkAsync(timeWindowDays);
            var clusters = await _networkAnalysisService.DetectAccountClustersAsync(timeWindowDays);

            var filteredClusters = clusters.AsEnumerable();

            if (minSuspicionScore.HasValue)
                filteredClusters = filteredClusters.Where(c => c.SuspicionScore >= minSuspicionScore.Value);

            if (minSize.HasValue)
                filteredClusters = filteredClusters.Where(c => c.Size >= minSize.Value);

            var clusterDtos = filteredClusters.Select(c => new NetworkClusterDto
            {
                ClusterId = c.ClusterId,
                UserIds = c.UserIds,
                Usernames = c.UserIds.Select(id => network.Nodes.TryGetValue(id, out var node) ? node.Username ?? "Unknown" : "Unknown").ToList(),
                Size = c.Size,
                SuspicionScore = c.SuspicionScore,
                SuspicionLevel = c.SuspicionScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                SuspicionReasons = c.SuspicionReasons,
                InternalTradeCount = c.InternalTradeCount,
                TotalInternalValue = c.TotalInternalValue,
                InternalTradeRatio = c.InternalTradeRatio,
                AverageAccountAge = c.AverageAccountAge,
                DetectedAt = c.DetectedAt,
                TimeWindowDays = c.TimeWindowDays,
                Metadata = c.Metadata
            }).OrderByDescending(c => c.SuspicionScore);

            return Ok(clusterDtos);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching clusters");
            return StatusCode(500, "Internal server error while fetching clusters");
        }
    }

    /// <summary>
    ///     Gets detected trade flows and patterns.
    /// </summary>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <param name="flowType">Filter by flow type (Linear, Circular, Funnel)</param>
    /// <param name="minSuspicionScore">Minimum suspicion score to include</param>
    /// <returns>List of detected trade flows</returns>
    [HttpGet("flows")]
    public async Task<ActionResult<IEnumerable<TradeFlowDto>>> GetTradeFlows(
        [FromQuery] int timeWindowDays = 30,
        [FromQuery] string? flowType = null,
        [FromQuery] double? minSuspicionScore = null)
    {
        try
        {
            Log.Information("Fetching trade flows for {TimeWindow} days, type={FlowType}, minSuspicion={MinSuspicion}",
                timeWindowDays, flowType, minSuspicionScore);

            var network = await _networkGraphService.GetTradeNetworkAsync(timeWindowDays);
            var funnels = await _networkAnalysisService.DetectFunnelPatternsAsync(timeWindowDays);
            var circularFlows = await _networkAnalysisService.DetectCircularFlowsAsync(timeWindowDays);
            
            // Create flow DTOs from both types
            var flowDtos = new List<TradeFlowDto>();
            
            // Add funnel patterns
            var filteredFunnels = funnels.AsEnumerable();
            if (!string.IsNullOrEmpty(flowType) && !flowType.Equals("Funnel", StringComparison.OrdinalIgnoreCase))
                filteredFunnels = [];
            if (minSuspicionScore.HasValue)
                filteredFunnels = filteredFunnels.Where(f => f.SuspicionScore >= minSuspicionScore.Value);
                
            flowDtos.AddRange(filteredFunnels.Select(f => new TradeFlowDto
            {
                FlowId = f.FlowId,
                Path = f.Path,
                PathUsernames = f.Path.Select(id => network.Nodes.TryGetValue(id, out var node) ? node.Username ?? "Unknown" : "Unknown").ToList(),
                PathLength = f.PathLength,
                TotalValue = f.TotalValue,
                FlowType = f.FlowType,
                SuspicionScore = f.SuspicionScore,
                SuspicionLevel = f.SuspicionLevel,
                SuspicionReasons = f.SuspicionReasons,
                FlowStartTime = f.FlowStartTime,
                FlowEndTime = f.FlowEndTime,
                FlowDuration = f.FlowDuration,
                DetectedAt = f.DetectedAt,
                IsCircular = f.IsCircular,
                FlowVelocity = f.FlowVelocity,
                Metadata = f.Metadata
            }));
            
            // Add circular flows
            var filteredCircular = circularFlows.AsEnumerable();
            if (!string.IsNullOrEmpty(flowType) && !flowType.Equals("Circular", StringComparison.OrdinalIgnoreCase))
                filteredCircular = [];
            if (minSuspicionScore.HasValue)
                filteredCircular = filteredCircular.Where(f => f.SuspicionScore >= minSuspicionScore.Value);
                
            flowDtos.AddRange(filteredCircular.Select(f => new TradeFlowDto
            {
                FlowId = f.FlowId,
                Path = f.Path,
                PathUsernames = f.Path.Select(id => network.Nodes.TryGetValue(id, out var node) ? node.Username ?? "Unknown" : "Unknown").ToList(),
                PathLength = f.PathLength,
                TotalValue = f.TotalValue,
                FlowType = f.FlowType,
                SuspicionScore = f.SuspicionScore,
                SuspicionLevel = f.SuspicionLevel,
                SuspicionReasons = f.SuspicionReasons,
                FlowStartTime = f.FlowStartTime,
                FlowEndTime = f.FlowEndTime,
                FlowDuration = f.FlowDuration,
                DetectedAt = f.DetectedAt,
                IsCircular = f.IsCircular,
                FlowVelocity = f.FlowVelocity,
                Metadata = f.Metadata
            }));
            
            var orderedFlows = flowDtos.OrderByDescending(f => f.SuspicionScore);

            return Ok(orderedFlows);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching trade flows");
            return StatusCode(500, "Internal server error while fetching trade flows");
        }
    }

    /// <summary>
    ///     Gets the shortest path between two users in the trade network.
    /// </summary>
    /// <param name="fromUserId">Source user ID</param>
    /// <param name="toUserId">Target user ID</param>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <returns>Shortest path information</returns>
    [HttpGet("path/{fromUserId}/{toUserId}")]
    public async Task<ActionResult> GetShortestPath(
        ulong fromUserId,
        ulong toUserId,
        [FromQuery] int timeWindowDays = 30)
    {
        try
        {
            Log.Information("Finding shortest path from {FromUser} to {ToUser} in {TimeWindow} days",
                fromUserId, toUserId, timeWindowDays);

            var network = await _networkGraphService.GetTradeNetworkAsync(timeWindowDays);
            
            // Mock implementation - would use actual graph shortest path algorithm
            var path = new List<ulong> { fromUserId, toUserId };
            var pathInfo = new
            {
                Path = path,
                PathLength = path.Count - 1,
                PathUsernames = path.Select(id => network.Nodes.TryGetValue(id, out var node) ? node.Username ?? "Unknown" : "Unknown").ToList(),
                TotalRisk = network.Edges.Where(e => 
                    (e.FromUserId == fromUserId && e.ToUserId == toUserId) ||
                    (e.FromUserId == toUserId && e.ToUserId == fromUserId))
                    .Average(e => e.RiskScore),
                Exists = true
            };

            return Ok(pathInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error finding shortest path from {FromUser} to {ToUser}", fromUserId, toUserId);
            return StatusCode(500, "Internal server error while finding path");
        }
    }
}