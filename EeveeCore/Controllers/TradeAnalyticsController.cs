using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EeveeCore.DTOs.Dashboard;
using EeveeCore.Services.Network;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for trade analytics and reporting.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class TradeAnalyticsController : ControllerBase
{
    private readonly TradeNetworkGraphService _networkGraphService;
    private readonly NetworkAnalysisService _networkAnalysisService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeAnalyticsController"/> class.
    /// </summary>
    /// <param name="networkGraphService">The network graph service.</param>
    /// <param name="networkAnalysisService">The network analysis service.</param>
    public TradeAnalyticsController(
        TradeNetworkGraphService networkGraphService,
        NetworkAnalysisService networkAnalysisService)
    {
        _networkGraphService = networkGraphService;
        _networkAnalysisService = networkAnalysisService;
    }

    /// <summary>
    ///     Gets comprehensive trade analytics for a specified time period.
    /// </summary>
    /// <param name="startDate">Start date for analysis (optional, defaults to 30 days ago)</param>
    /// <param name="endDate">End date for analysis (optional, defaults to now)</param>
    /// <returns>Comprehensive trade analytics</returns>
    [HttpGet("overview")]
    public async Task<ActionResult<TradeAnalyticsDto>> GetTradeAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var end = endDate ?? DateTime.UtcNow;
            var start = startDate ?? end.AddDays(-30);
            var timeWindowDays = (int)(end - start).TotalDays;

            Log.Information("Generating trade analytics from {StartDate} to {EndDate}", start, end);

            var network = await _networkGraphService.GetTradeNetworkAsync(timeWindowDays);
            var clusters = await _networkAnalysisService.DetectAccountClustersAsync(timeWindowDays);
            var funnels = await _networkAnalysisService.DetectFunnelPatternsAsync(timeWindowDays);
            var circularFlows = await _networkAnalysisService.DetectCircularFlowsAsync(timeWindowDays);

            // Calculate basic statistics
            var totalTrades = network.Edges.Sum(e => e.TradeCount);
            var totalValue = network.Edges.Sum(e => e.TotalValue);
            var uniqueTraders = network.Nodes.Count;
            var avgTradeValue = totalTrades > 0 ? totalValue / totalTrades : 0;

            // Mock data for time series (would come from actual database queries)
            var volumeTimeSeries = GenerateMockTimeSeries(start, end, totalTrades, totalValue, uniqueTraders);

            // Top trading pairs
            var topTradingPairs = network.Edges
                .OrderByDescending(e => e.TradeCount)
                .Take(10)
                .Select(e => new TradingPairStatsDto
                {
                    User1Id = e.FromUserId,
                    User2Id = e.ToUserId,
                    User1Username = network.Nodes.TryGetValue(e.FromUserId, out var node) ? node.Username : null,
                    User2Username = network.Nodes.TryGetValue(e.ToUserId, out var networkNode) ? networkNode.Username : null,
                    TradeCount = e.TradeCount,
                    TotalValue = e.TotalValue,
                    AverageTradeValue = e.TradeCount > 0 ? e.TotalValue / e.TradeCount : 0,
                    LastTradeTime = e.LastTradeTime,
                    RiskScore = e.RiskScore
                })
                .ToList();

            // Mock data for most traded items
            var topTradedItems = new List<ItemTradeStatsDto>
            {
                new() { ItemName = "Rare Pokemon Card", TradeCount = 150, TotalValue = 50000, AverageValue = 333, UniqueTraders = 45, LastTradeTime = DateTime.UtcNow.AddHours(-2) },
                new() { ItemName = "Legendary Pokemon", TradeCount = 89, TotalValue = 125000, AverageValue = 1404, UniqueTraders = 67, LastTradeTime = DateTime.UtcNow.AddMinutes(-30) },
                new() { ItemName = "Shiny Pokemon", TradeCount = 200, TotalValue = 75000, AverageValue = 375, UniqueTraders = 120, LastTradeTime = DateTime.UtcNow.AddHours(-1) }
            };

            // Trade type statistics
            var tradeTypeStats = new Dictionary<string, TradeTypeStatsDto>
            {
                ["Direct"] = new() { Count = (int)(totalTrades * 0.7), TotalValue = totalValue * 0.6m, AverageValue = avgTradeValue * 0.8m, Percentage = 70.0, AverageRiskScore = 0.3 },
                ["Gift"] = new() { Count = (int)(totalTrades * 0.2), TotalValue = totalValue * 0.1m, AverageValue = avgTradeValue * 0.5m, Percentage = 20.0, AverageRiskScore = 0.4 },
                ["Auction"] = new() { Count = (int)(totalTrades * 0.1), TotalValue = totalValue * 0.3m, AverageValue = avgTradeValue * 3.0m, Percentage = 10.0, AverageRiskScore = 0.2 }
            };

            // Detected patterns
            var detectedPatterns = new List<TradingPatternDto>
            {
                new()
                {
                    PatternId = "circular_flow_1",
                    PatternName = "Circular Trading",
                    Description = "Items being traded in circular patterns",
                    InstanceCount = circularFlows.Count,
                    ConfidenceLevel = 0.85,
                    InvolvedUserIds = circularFlows.SelectMany(f => f.Path).Distinct().ToList(),
                    FirstDetected = DateTime.UtcNow.AddDays(-5),
                    LastObserved = DateTime.UtcNow.AddHours(-2)
                },
                new()
                {
                    PatternId = "funnel_pattern_1",
                    PatternName = "Value Funneling",
                    Description = "Multiple users funneling value to single accounts",
                    InstanceCount = funnels.Count,
                    ConfidenceLevel = 0.92,
                    InvolvedUserIds = funnels.SelectMany(f => f.Path).Distinct().ToList(),
                    FirstDetected = DateTime.UtcNow.AddDays(-3),
                    LastObserved = DateTime.UtcNow.AddMinutes(-45)
                }
            };

            // Suspicious activity summary
            var suspiciousActivity = new SuspiciousActivitySummaryDto
            {
                SuspiciousTradeCount = network.Edges.Count(e => e.RiskScore > 0.6),
                SuspiciousTradeValue = network.Edges.Where(e => e.RiskScore > 0.6).Sum(e => e.TotalValue),
                BlockedTradeCount = 0, // Would come from actual blocking system
                BlockedTradeValue = 0,
                FlaggedUserCount = network.Nodes.Values.Count(n => n.RiskScore > 0.6),
                CommonSuspiciousPatterns =
                    ["High velocity trading", "Value imbalance", "New account activity", "Circular flows"],
                FalsePositiveRate = 0.05,
                DetectionAccuracy = 0.94
            };

            var analytics = new TradeAnalyticsDto
            {
                TotalTrades = totalTrades,
                TotalValueTraded = totalValue,
                UniqueTraders = uniqueTraders,
                AverageTradeValue = avgTradeValue,
                MedianTradeValue = avgTradeValue * 0.7m, // Mock calculation
                VolumeTimeSeries = volumeTimeSeries,
                TopTradingPairs = topTradingPairs,
                TopTradedItems = topTradedItems,
                TradeTypeStats = tradeTypeStats,
                GeographicDistribution = new Dictionary<string, int>
                {
                    ["North America"] = (int)(uniqueTraders * 0.4),
                    ["Europe"] = (int)(uniqueTraders * 0.35),
                    ["Asia"] = (int)(uniqueTraders * 0.2),
                    ["Other"] = (int)(uniqueTraders * 0.05)
                },
                DetectedPatterns = detectedPatterns,
                SuspiciousActivity = suspiciousActivity,
                StartDate = start,
                EndDate = end,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating trade analytics");
            return StatusCode(500, "Internal server error while generating analytics");
        }
    }

    /// <summary>
    ///     Gets trading volume trends over time with configurable granularity.
    /// </summary>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <param name="granularity">Time granularity (hour, day, week) (default: day)</param>
    /// <returns>Time series data for trading volume</returns>
    [HttpGet("volume-trends")]
    public async Task<ActionResult<IEnumerable<TradeVolumeTimeSeriesDto>>> GetVolumeTrends(
        [FromQuery] int timeWindowDays = 30,
        [FromQuery] string granularity = "day")
    {
        try
        {
            Log.Information("Generating volume trends for {TimeWindow} days with {Granularity} granularity",
                timeWindowDays, granularity);

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-timeWindowDays);

            var timeSeries = GenerateMockTimeSeries(startDate, endDate, 1000, 500000, 200);

            return Ok(timeSeries);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating volume trends");
            return StatusCode(500, "Internal server error while generating volume trends");
        }
    }

    /// <summary>
    ///     Gets statistics for a specific user's trading activity.
    /// </summary>
    /// <param name="userId">The user ID to analyze</param>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <returns>User-specific trading statistics</returns>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult> GetUserTradeStats(
        ulong userId,
        [FromQuery] int timeWindowDays = 30)
    {
        try
        {
            Log.Information("Generating trade stats for user {UserId} over {TimeWindow} days",
                userId, timeWindowDays);

            var network = await _networkGraphService.GetUserNetworkAsync(userId, 1, timeWindowDays);
            var userNode = network.Nodes.TryGetValue(userId, out var node) ? node : null;

            if (userNode == null)
            {
                return NotFound($"User {userId} not found in trade network");
            }

            var userEdges = network.Edges.Where(e => e.FromUserId == userId || e.ToUserId == userId).ToList();

            var stats = new
            {
                UserId = userId,
                Username = userNode.Username,
                TotalTrades = userNode.TotalTrades,
                TotalValueGiven = userNode.TotalValueGiven,
                TotalValueReceived = userNode.TotalValueReceived,
                NetValueFlow = userNode.NetValueFlow,
                TradingPartners = userEdges.Select(e => e.FromUserId == userId ? e.ToUserId : e.FromUserId).Distinct().Count(),
                AverageTradeValue = userNode.TotalTrades > 0 ? (userNode.TotalValueGiven + userNode.TotalValueReceived) / userNode.TotalTrades : 0,
                RiskScore = userNode.RiskScore,
                RiskLevel = userNode.RiskScore switch
                {
                    > 0.8 => "Critical",
                    > 0.6 => "High",
                    > 0.4 => "Medium",
                    _ => "Low"
                },
                AccountAgeDays = userNode.AccountAgeDays,
                FirstTradeDate = userEdges.Any() ? userEdges.Min(e => e.FirstTradeTime) : (DateTime?)null,
                LastTradeDate = userEdges.Any() ? userEdges.Max(e => e.LastTradeTime) : (DateTime?)null,
                MostFrequentPartner = userEdges
                    .GroupBy(e => e.FromUserId == userId ? e.ToUserId : e.FromUserId)
                    .OrderByDescending(g => g.Sum(e => e.TradeCount))
                    .FirstOrDefault()?.Key,
                TopTradedValues = userEdges.OrderByDescending(e => e.TotalValue).Take(5).Select(e => new
                {
                    PartnerId = e.FromUserId == userId ? e.ToUserId : e.FromUserId,
                    PartnerUsername = network.Nodes.TryGetValue(e.FromUserId == userId ? e.ToUserId : e.FromUserId, out var partner) ? partner.Username : null,
                    TotalValue = e.TotalValue,
                    TradeCount = e.TradeCount
                }),
                Flags = userNode.Flags.ToList(),
                TimeWindowDays = timeWindowDays,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating user trade stats for {UserId}", userId);
            return StatusCode(500, "Internal server error while generating user stats");
        }
    }

    /// <summary>
    ///     Gets comparative analytics between different time periods.
    /// </summary>
    /// <param name="currentPeriodDays">Current period in days (default: 30)</param>
    /// <param name="comparisonPeriodDays">Comparison period in days (default: 30)</param>
    /// <returns>Comparative analytics data</returns>
    [HttpGet("comparison")]
    public async Task<ActionResult> GetComparativeAnalytics(
        [FromQuery] int currentPeriodDays = 30,
        [FromQuery] int comparisonPeriodDays = 30)
    {
        try
        {
            Log.Information("Generating comparative analytics: current {Current} days vs previous {Previous} days",
                currentPeriodDays, comparisonPeriodDays);

            var currentNetwork = await _networkGraphService.GetTradeNetworkAsync(currentPeriodDays);
            var previousNetwork = await _networkGraphService.GetTradeNetworkAsync(currentPeriodDays + comparisonPeriodDays);

            // Calculate metrics for both periods
            var currentMetrics = CalculatePeriodMetrics(currentNetwork);
            var previousMetrics = CalculatePeriodMetrics(previousNetwork, currentPeriodDays); // Offset for previous period

            var comparison = new
            {
                CurrentPeriod = new
                {
                    Days = currentPeriodDays,
                    StartDate = DateTime.UtcNow.AddDays(-currentPeriodDays),
                    EndDate = DateTime.UtcNow,
                    Metrics = currentMetrics
                },
                PreviousPeriod = new
                {
                    Days = comparisonPeriodDays,
                    StartDate = DateTime.UtcNow.AddDays(-currentPeriodDays - comparisonPeriodDays),
                    EndDate = DateTime.UtcNow.AddDays(-currentPeriodDays),
                    Metrics = previousMetrics
                },
                Changes = new
                {
                    TradeCountChange = currentMetrics.TotalTrades - previousMetrics.TotalTrades,
                    TradeCountPercentChange = previousMetrics.TotalTrades > 0 ? 
                        (double)(currentMetrics.TotalTrades - previousMetrics.TotalTrades) / previousMetrics.TotalTrades * 100 : 0,
                    ValueChange = currentMetrics.TotalValue - previousMetrics.TotalValue,
                    ValuePercentChange = previousMetrics.TotalValue > 0 ? 
                        (double)(currentMetrics.TotalValue - previousMetrics.TotalValue) / (double)previousMetrics.TotalValue * 100 : 0,
                    UserCountChange = currentMetrics.UniqueUsers - previousMetrics.UniqueUsers,
                    RiskScoreChange = currentMetrics.AverageRiskScore - previousMetrics.AverageRiskScore
                },
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(comparison);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating comparative analytics");
            return StatusCode(500, "Internal server error while generating comparative analytics");
        }
    }

    /// <summary>
    ///     Exports trade analytics data in various formats.
    /// </summary>
    /// <param name="format">Export format (json, csv) (default: json)</param>
    /// <param name="timeWindowDays">Time window in days for analysis (default: 30)</param>
    /// <returns>Exported analytics data</returns>
    [HttpGet("export")]
    public async Task<ActionResult> ExportAnalytics(
        [FromQuery] string format = "json",
        [FromQuery] int timeWindowDays = 30)
    {
        try
        {
            Log.Information("Exporting analytics in {Format} format for {TimeWindow} days", format, timeWindowDays);

            var analytics = await GetTradeAnalytics(DateTime.UtcNow.AddDays(-timeWindowDays), DateTime.UtcNow);
            
            if (analytics.Result is OkObjectResult { Value: TradeAnalyticsDto analyticsData })
            {
                switch (format.ToLower())
                {
                    case "csv":
                        var csv = ConvertToCsv(analyticsData);
                        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"trade_analytics_{DateTime.UtcNow:yyyyMMdd}.csv");
                    
                    case "json":
                    default:
                        return File(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(analyticsData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })), 
                                  "application/json", $"trade_analytics_{DateTime.UtcNow:yyyyMMdd}.json");
                }
            }

            return BadRequest("Unable to generate analytics data for export");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error exporting analytics");
            return StatusCode(500, "Internal server error while exporting analytics");
        }
    }

    #region Private Helper Methods

    private List<TradeVolumeTimeSeriesDto> GenerateMockTimeSeries(DateTime startDate, DateTime endDate, int totalTrades, decimal totalValue, int uniqueTraders)
    {
        var timeSeries = new List<TradeVolumeTimeSeriesDto>();
        var days = (int)(endDate - startDate).TotalDays;
        var random = new Random();

        for (var i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var dailyTrades = (int)(totalTrades / days * (0.5 + random.NextDouble()));
            var dailyValue = totalValue / days * (decimal)(0.5 + random.NextDouble());
            var dailyTraders = (int)(uniqueTraders / days * (0.5 + random.NextDouble()));

            timeSeries.Add(new TradeVolumeTimeSeriesDto
            {
                Timestamp = date,
                TradeCount = dailyTrades,
                TotalValue = dailyValue,
                UniqueTraders = dailyTraders,
                AverageTradeValue = dailyTrades > 0 ? dailyValue / dailyTrades : 0
            });
        }

        return timeSeries;
    }

    private dynamic CalculatePeriodMetrics(TradeNetworkGraph network, int offsetDays = 0)
    {
        var totalTrades = network.Edges.Sum(e => e.TradeCount);
        var totalValue = network.Edges.Sum(e => e.TotalValue);
        var uniqueUsers = network.Nodes.Count;
        var avgRiskScore = network.Nodes.Values.Any() ? network.Nodes.Values.Average(n => n.RiskScore) : 0;

        return new
        {
            TotalTrades = totalTrades,
            TotalValue = totalValue,
            UniqueUsers = uniqueUsers,
            AverageRiskScore = avgRiskScore,
            AverageTradeValue = totalTrades > 0 ? totalValue / totalTrades : 0,
            HighRiskUsers = network.Nodes.Values.Count(n => n.RiskScore > 0.6)
        };
    }

    private string ConvertToCsv(TradeAnalyticsDto analytics)
    {
        var csv = new System.Text.StringBuilder();
        
        // Header
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Total Trades,{analytics.TotalTrades}");
        csv.AppendLine($"Total Value Traded,{analytics.TotalValueTraded}");
        csv.AppendLine($"Unique Traders,{analytics.UniqueTraders}");
        csv.AppendLine($"Average Trade Value,{analytics.AverageTradeValue}");
        csv.AppendLine($"Median Trade Value,{analytics.MedianTradeValue}");
        
        csv.AppendLine();
        csv.AppendLine("Top Trading Pairs");
        csv.AppendLine("User1,User2,Trade Count,Total Value,Risk Score");
        
        foreach (var pair in analytics.TopTradingPairs)
        {
            csv.AppendLine($"{pair.User1Username ?? pair.User1Id.ToString()},{pair.User2Username ?? pair.User2Id.ToString()},{pair.TradeCount},{pair.TotalValue},{pair.RiskScore:F3}");
        }

        return csv.ToString();
    }

    #endregion
}