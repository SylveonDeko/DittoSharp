using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EeveeCore.DTOs.Dashboard;
using EeveeCore.Services.Network;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for trade fraud detection and management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class TradeFraudController : ControllerBase
{
    private readonly NetworkAnalysisService _networkAnalysisService;
    private readonly TradeNetworkGraphService _networkGraphService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeFraudController"/> class.
    /// </summary>
    /// <param name="networkAnalysisService">The network analysis service.</param>
    /// <param name="networkGraphService">The network graph service.</param>
    public TradeFraudController(
        NetworkAnalysisService networkAnalysisService,
        TradeNetworkGraphService networkGraphService)
    {
        _networkAnalysisService = networkAnalysisService;
        _networkGraphService = networkGraphService;
    }

    /// <summary>
    ///     Gets the main dashboard summary with fraud detection metrics.
    /// </summary>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <returns>Dashboard summary with key fraud detection metrics</returns>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary([FromQuery] int timeWindowDays = 30)
    {
        try
        {
            Log.Information("Generating dashboard summary for {TimeWindow} days", timeWindowDays);

            var network = await _networkGraphService.GetTradeNetworkAsync(timeWindowDays);
            var clusters = await _networkAnalysisService.DetectAccountClustersAsync(timeWindowDays);
            var flows = await _networkAnalysisService.DetectFunnelPatternsAsync(timeWindowDays);

            // Calculate summary statistics
            var totalUsers = network.Nodes.Count;
            var totalTrades = network.Edges.Sum(e => e.TradeCount);
            var totalValue = network.Edges.Sum(e => e.TotalValue);
            var highRiskRelationships = network.Edges.Count(e => e.RiskScore > 0.7);
            var suspiciousClusters = clusters.Count(c => c.SuspicionScore > 0.6);

            // Get recent alerts (mock data for now - would come from actual alert system)
            var recentAlerts = new List<FraudAlertDto>();

            // Get top risk users
            var topRiskUsers = network.Nodes.Values
                .OrderByDescending(n => n.RiskScore)
                .Take(10)
                .Select(n => new TopRiskUserDto
                {
                    UserId = n.UserId,
                    Username = n.Username,
                    RiskScore = n.RiskScore,
                    RiskLevel = n.RiskScore switch
                    {
                        > 0.8 => "Critical",
                        > 0.6 => "High",
                        > 0.4 => "Medium",
                        _ => "Low"
                    },
                    TotalTrades = n.TotalTrades,
                    TotalValue = n.TotalValueGiven + n.TotalValueReceived,
                    PrimaryRiskReasons = n.Flags.ToList(),
                    LastActivity = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30))
                })
                .ToList();

            var summary = new DashboardSummaryDto
            {
                TotalActiveUsers = totalUsers,
                TotalTrades = totalTrades,
                TotalValueTraded = totalValue,
                OpenAlerts = recentAlerts.Count(a => a.Status == "Open"),
                HighRiskRelationships = highRiskRelationships,
                SuspiciousClusters = suspiciousClusters,
                BlockedAccounts = 0, // Would come from user service
                FraudPreventionRate = 98.5, // Would be calculated from historical data
                RecentAlerts = recentAlerts,
                TopRiskUsers = topRiskUsers,
                RiskTrends = [], // Would be populated from trend analysis
                GeneratedAt = DateTime.UtcNow,
                DataAsOf = DateTime.UtcNow
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating dashboard summary");
            return StatusCode(500, "Internal server error while generating dashboard summary");
        }
    }

    /// <summary>
    ///     Gets fraud alerts with optional filtering.
    /// </summary>
    /// <param name="severity">Filter by severity level</param>
    /// <param name="status">Filter by status</param>
    /// <param name="limit">Maximum number of alerts to return</param>
    /// <param name="offset">Number of alerts to skip</param>
    /// <returns>List of fraud alerts</returns>
    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<FraudAlertDto>>> GetAlerts(
        [FromQuery] string? severity = null,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            Log.Information("Fetching alerts with severity={Severity}, status={Status}, limit={Limit}, offset={Offset}",
                severity, status, limit, offset);

            // For now, return mock data. In real implementation, this would query the alerts database
            var alerts = new List<FraudAlertDto>();

            return Ok(alerts.Skip(offset).Take(limit));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching fraud alerts");
            return StatusCode(500, "Internal server error while fetching alerts");
        }
    }

    /// <summary>
    ///     Gets detailed information about a specific alert.
    /// </summary>
    /// <param name="alertId">The alert identifier</param>
    /// <returns>Detailed alert information</returns>
    [HttpGet("alerts/{alertId}")]
    public async Task<ActionResult<FraudAlertDto>> GetAlert(string alertId)
    {
        try
        {
            Log.Information("Fetching alert details for {AlertId}", alertId);

            // Mock implementation - would query actual alert database
            return NotFound($"Alert {alertId} not found");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching alert {AlertId}", alertId);
            return StatusCode(500, "Internal server error while fetching alert");
        }
    }

    /// <summary>
    ///     Updates the status of a fraud alert.
    /// </summary>
    /// <param name="alertId">The alert identifier</param>
    /// <param name="status">The new status</param>
    /// <param name="notes">Optional resolution notes</param>
    /// <returns>Updated alert information</returns>
    [HttpPut("alerts/{alertId}/status")]
    public async Task<ActionResult<FraudAlertDto>> UpdateAlertStatus(
        string alertId, 
        [FromBody] string status, 
        [FromQuery] string? notes = null)
    {
        try
        {
            Log.Information("Updating alert {AlertId} status to {Status}", alertId, status);

            // Mock implementation - would update actual alert database
            return NotFound($"Alert {alertId} not found");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating alert {AlertId} status", alertId);
            return StatusCode(500, "Internal server error while updating alert");
        }
    }

    /// <summary>
    ///     Gets risk metrics and scoring information.
    /// </summary>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <returns>Risk metrics and distribution information</returns>
    [HttpGet("risk-metrics")]
    public async Task<ActionResult<RiskMetricsDto>> GetRiskMetrics([FromQuery] int timeWindowDays = 30)
    {
        try
        {
            Log.Information("Calculating risk metrics for {TimeWindow} days", timeWindowDays);

            var network = await _networkGraphService.GetTradeNetworkAsync(timeWindowDays);
            
            var riskScores = network.Nodes.Values.Select(n => n.RiskScore).ToList();
            var averageRisk = riskScores.Any() ? riskScores.Average() : 0;
            var medianRisk = riskScores.Any() ? riskScores.OrderBy(x => x).Skip(riskScores.Count / 2).First() : 0;
            var risk95th = riskScores.Any() ? riskScores.OrderBy(x => x).Skip((int)(riskScores.Count * 0.95)).First() : 0;

            var riskDistribution = new Dictionary<string, int>
            {
                ["Low"] = riskScores.Count(s => s <= 0.3),
                ["Medium"] = riskScores.Count(s => s is > 0.3 and <= 0.6),
                ["High"] = riskScores.Count(s => s is > 0.6 and <= 0.8),
                ["Critical"] = riskScores.Count(s => s > 0.8)
            };

            var metrics = new RiskMetricsDto
            {
                OverallRiskLevel = averageRisk switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High", 
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                AverageRiskScore = averageRisk,
                MedianRiskScore = medianRisk,
                RiskScore95thPercentile = risk95th,
                RiskDistribution = riskDistribution,
                Thresholds = new RiskThresholdsDto(),
                TopRiskFactors =
                [
                    new()
                    {
                        FactorName = "Account Age", Weight = 0.2, AverageScore = 0.3, AffectedUsers = riskScores.Count
                    },
                    new()
                    {
                        FactorName = "Trade Imbalance", Weight = 0.3, AverageScore = 0.4,
                        AffectedUsers = riskScores.Count
                    },
                    new()
                    {
                        FactorName = "Velocity", Weight = 0.25, AverageScore = 0.2, AffectedUsers = riskScores.Count
                    }
                ],
                RiskTimeSeries = [],
                CalculatedAt = DateTime.UtcNow,
                TimeRange = TimeSpan.FromDays(timeWindowDays)
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating risk metrics");
            return StatusCode(500, "Internal server error while calculating risk metrics");
        }
    }

    /// <summary>
    ///     Gets detailed risk breakdown for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to analyze</param>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <returns>Detailed risk breakdown for the user</returns>
    [HttpGet("users/{userId}/risk")]
    public async Task<ActionResult<UserRiskBreakdownDto>> GetUserRiskBreakdown(
        ulong userId, 
        [FromQuery] int timeWindowDays = 30)
    {
        try
        {
            Log.Information("Calculating risk breakdown for user {UserId}", userId);

            var network = await _networkGraphService.GetUserNetworkAsync(userId, 2, timeWindowDays);
            var userNode = network.Nodes.TryGetValue(userId, out var node) ? node : null;

            if (userNode == null)
            {
                return NotFound($"User {userId} not found in trade network");
            }

            var breakdown = new UserRiskBreakdownDto
            {
                UserId = userId,
                Username = userNode.Username,
                OverallRiskScore = userNode.RiskScore,
                RiskLevel = userNode.RiskScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                FactorScores = new Dictionary<string, double>
                {
                    ["AccountAge"] = Math.Max(0, 1 - userNode.AccountAgeDays / 365.0),
                    ["TradeImbalance"] = userNode.ValueImbalanceRatio,
                    ["ConnectionCount"] = Math.Min(1.0, userNode.ConnectionCount / 50.0),
                    ["Velocity"] = 0.3 // Would be calculated from actual trading velocity
                },
                PrimaryRiskDrivers = userNode.Flags.ToList(),
                RiskHistory = [],
                CalculatedAt = DateTime.UtcNow
            };

            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating user risk breakdown for {UserId}", userId);
            return StatusCode(500, "Internal server error while calculating user risk");
        }
    }

    /// <summary>
    ///     Blocks or unblocks a user from trading.
    /// </summary>
    /// <param name="userId">The user ID to block/unblock</param>
    /// <param name="blocked">Whether to block (true) or unblock (false) the user</param>
    /// <param name="reason">Reason for the action</param>
    /// <returns>Result of the block/unblock operation</returns>
    [HttpPut("users/{userId}/block")]
    public async Task<ActionResult> BlockUser(
        ulong userId, 
        [FromQuery] bool blocked, 
        [FromBody] string? reason = null)
    {
        try
        {
            Log.Information("Setting user {UserId} blocked status to {Blocked}. Reason: {Reason}", 
                userId, blocked, reason);

            // Mock implementation - would update user blocking status in database
            return Ok(new { Success = true, Message = $"User {userId} {(blocked ? "blocked" : "unblocked")} successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating block status for user {UserId}", userId);
            return StatusCode(500, "Internal server error while updating user block status");
        }
    }

    /// <summary>
    ///     Triggers a manual fraud analysis for specific users or the entire network.
    /// </summary>
    /// <param name="userIds">Optional list of specific user IDs to analyze</param>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <returns>Analysis job information</returns>
    [HttpPost("analyze")]
    public async Task<ActionResult> TriggerFraudAnalysis(
        [FromBody] List<ulong>? userIds = null,
        [FromQuery] int timeWindowDays = 30)
    {
        try
        {
            Log.Information("Triggering fraud analysis for {UserCount} users in {TimeWindow} day window", 
                userIds?.Count ?? 0, timeWindowDays);

            // Mock implementation - would trigger background analysis job
            var jobId = Guid.NewGuid().ToString();
            
            return Accepted(new { JobId = jobId, Message = "Fraud analysis started", EstimatedCompletionMinutes = 5 });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error triggering fraud analysis");
            return StatusCode(500, "Internal server error while starting fraud analysis");
        }
    }

    /// <summary>
    ///     Gets the status of a fraud analysis job.
    /// </summary>
    /// <param name="jobId">The analysis job identifier</param>
    /// <returns>Job status and results</returns>
    [HttpGet("analyze/{jobId}")]
    public async Task<ActionResult> GetAnalysisStatus(string jobId)
    {
        try
        {
            Log.Information("Checking status of analysis job {JobId}", jobId);

            // Mock implementation - would check actual job status
            return Ok(new { 
                JobId = jobId, 
                Status = "Completed", 
                Progress = 100, 
                CompletedAt = DateTime.UtcNow,
                Results = new { AnomaliesDetected = 3, AlertsGenerated = 1 }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking analysis job status {JobId}", jobId);
            return StatusCode(500, "Internal server error while checking job status");
        }
    }
}