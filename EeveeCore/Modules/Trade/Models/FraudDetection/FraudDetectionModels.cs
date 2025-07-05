using EeveeCore.Database.Linq.Models.Game;

namespace EeveeCore.Modules.Trade.Models.FraudDetection;

#region Main Analysis Results

/// <summary>
/// Represents comprehensive fraud analysis results combining all detection methods.
/// </summary>
public class ComprehensiveFraudAnalysis
{
    /// <summary>
    /// Gets or sets the unique identifier for this trade session.
    /// </summary>
    public Guid TradeSessionId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }
    
    // Individual analyses
    /// <summary>
    /// Gets or sets the basic risk analysis results.
    /// </summary>
    public TradeRiskAnalysis? BasicRiskAnalysis { get; set; }
    
    /// <summary>
    /// Gets or sets the chain trading analysis results.
    /// </summary>
    public ChainTradingAnalysis? ChainTradingAnalysis { get; set; }
    
    /// <summary>
    /// Gets or sets the burst trading analysis results.
    /// </summary>
    public BurstTradingAnalysis? BurstTradingAnalysis { get; set; }
    
    /// <summary>
    /// Gets or sets the network analysis results.
    /// </summary>
    public NetworkConnectionAnalysis? NetworkAnalysis { get; set; }
    
    /// <summary>
    /// Gets or sets the market manipulation analysis results.
    /// </summary>
    public MarketManipulationAnalysis? MarketManipulation { get; set; }
    
    /// <summary>
    /// Gets or sets the Pokemon laundering analysis results.
    /// </summary>
    public TradePokemonLaunderingAnalysis? PokemonLaundering { get; set; }
    
    // Comprehensive results
    /// <summary>
    /// Gets or sets the comprehensive risk score combining all analyses.
    /// </summary>
    public double ComprehensiveRiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the list of actionable insights from the analysis.
    /// </summary>
    public List<string> ActionableInsights { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the recommended action based on the analysis.
    /// </summary>
    public RecommendedAction RecommendedAction { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of errors encountered during analysis.
    /// </summary>
    public List<string> AnalysisErrors { get; set; } = new();
}

/// <summary>
/// Represents the result of a trade risk analysis.
/// </summary>
public class TradeRiskAnalysis
{
    /// <summary>
    /// Gets or sets the unique identifier for this trade session.
    /// </summary>
    public Guid TradeSessionId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }

    #region Risk Scores
    
    /// <summary>
    /// Gets or sets the overall risk score for this trade.
    /// </summary>
    public double OverallRiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the value imbalance risk score.
    /// </summary>
    public double ValueImbalanceScore { get; set; }
    
    /// <summary>
    /// Gets or sets the relationship risk score between traders.
    /// </summary>
    public double RelationshipRiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the behavioral risk score based on trading patterns.
    /// </summary>
    public double BehavioralRiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the account age risk score.
    /// </summary>
    public double AccountAgeRiskScore { get; set; }
    #endregion

    #region Trade Values
    
    /// <summary>
    /// Gets or sets the total value being sent in the trade.
    /// </summary>
    public decimal SenderTotalValue { get; set; }
    
    /// <summary>
    /// Gets or sets the total value being received in the trade.
    /// </summary>
    public decimal ReceiverTotalValue { get; set; }
    
    /// <summary>
    /// Gets or sets the ratio between trade values.
    /// </summary>
    public double ValueRatio { get; set; }
    
    /// <summary>
    /// Gets or sets the absolute difference between trade values.
    /// </summary>
    public decimal ValueDifference { get; set; }
    #endregion

    #region Detection Flags
    
    /// <summary>
    /// Gets or sets a value indicating whether alt account activity was flagged.
    /// </summary>
    public bool FlaggedAltAccount { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether RMT (real money trading) activity was flagged.
    /// </summary>
    public bool FlaggedRmt { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether newbie exploitation was flagged.
    /// </summary>
    public bool FlaggedNewbieExploitation { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether unusual behavior was flagged.
    /// </summary>
    public bool FlaggedUnusualBehavior { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether bot activity was flagged.
    /// </summary>
    public bool FlaggedBotActivity { get; set; }
    #endregion

    /// <summary>
    /// Gets a value indicating whether any fraud flags have been set.
    /// </summary>
    public bool HasFraudFlags => FlaggedAltAccount || FlaggedRmt || FlaggedNewbieExploitation || 
                                FlaggedUnusualBehavior || FlaggedBotActivity;

    /// <summary>
    /// Gets the risk level based on the overall risk score.
    /// </summary>
    public RiskLevel RiskLevel => OverallRiskScore switch
    {
        >= 0.8 => RiskLevel.Critical,
        >= 0.6 => RiskLevel.High,
        >= 0.4 => RiskLevel.Medium,
        >= 0.2 => RiskLevel.Low,
        _ => RiskLevel.Minimal
    };
}

/// <summary>
/// Represents the result of fraud detection analysis.
/// </summary>
public class FraudDetectionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the trade is allowed to proceed.
    /// </summary>
    public bool IsAllowed { get; set; }
    
    /// <summary>
    /// Gets or sets the overall risk score for the trade.
    /// </summary>
    public double RiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the risk level classification.
    /// </summary>
    public RiskLevel RiskLevel { get; set; }
    
    /// <summary>
    /// Gets or sets the automated action to be taken.
    /// </summary>
    public AutomatedAction AutomatedAction { get; set; }
    
    /// <summary>
    /// Gets or sets the message explaining the decision.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Gets or sets the list of fraud flags detected.
    /// </summary>
    public List<string> FraudFlags { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether the trade requires admin review.
    /// </summary>
    public bool RequiresAdminReview { get; set; }
}

/// <summary>
/// Represents the result of a fast fraud check.
/// </summary>
public class FastFraudCheckResult
{
    /// <summary>
    /// Gets or sets a value indicating whether detailed analysis is required.
    /// </summary>
    public bool RequiresDetailedAnalysis { get; set; }
    
    /// <summary>
    /// Gets or sets the basic risk analysis results.
    /// </summary>
    public TradeRiskAnalysis BasicRiskAnalysis { get; set; } = new();
}

/// <summary>
/// Represents the result of executing an automated action.
/// </summary>
public class ActionExecutionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the action allows the trade to proceed.
    /// </summary>
    public bool IsAllowed { get; set; }
    
    /// <summary>
    /// Gets or sets the message explaining the action result.
    /// </summary>
    public string? Message { get; set; }
}

#endregion

#region Chain Trading Detection

/// <summary>
/// Represents the analysis results for chain trading fraud detection.
/// </summary>
public class ChainTradingAnalysis
{
    /// <summary>
    /// Gets or sets the user ID being analyzed.
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the list of detected trade chains.
    /// </summary>
    public List<TradeChain> DetectedChains { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the maximum depth of detected chains.
    /// </summary>
    public int MaxChainDepth { get; set; }
    
    /// <summary>
    /// Gets or sets the total value that flowed through the chains.
    /// </summary>
    public decimal TotalValueFlowed { get; set; }
    
    /// <summary>
    /// Gets or sets the risk score for chain trading activity.
    /// </summary>
    public double RiskScore { get; set; }
}

/// <summary>
/// Represents a chain of trades forming a suspicious pattern.
/// </summary>
public class TradeChain
{
    /// <summary>
    /// Gets or sets the path of trades in this chain.
    /// </summary>
    public List<TradeEdge> Path { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the length of the trade chain.
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    /// Gets or sets the total value in this chain.
    /// </summary>
    public decimal TotalValue { get; set; }
    
    /// <summary>
    /// Gets or sets the concentration of value in this chain.
    /// </summary>
    public decimal ValueConcentration { get; set; }
}

/// <summary>
/// Represents a single trade connection in a trading network.
/// </summary>
public class TradeEdge
{
    /// <summary>
    /// Gets or sets the source user ID.
    /// </summary>
    public ulong From { get; set; }
    
    /// <summary>
    /// Gets or sets the destination user ID.
    /// </summary>
    public ulong To { get; set; }
    
    /// <summary>
    /// Gets or sets the value of the trade.
    /// </summary>
    public decimal Value { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp of the trade.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

#endregion

#region Burst Trading Detection

/// <summary>
/// Represents the analysis results for burst trading detection.
/// </summary>
public class BurstTradingAnalysis
{
    /// <summary>
    /// Gets or sets the user ID being analyzed.
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the list of detected trading bursts.
    /// </summary>
    public List<TradeBurst> DetectedBursts { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the total number of bursts detected.
    /// </summary>
    public int TotalBurstsDetected { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum size of a detected burst.
    /// </summary>
    public int MaxBurstSize { get; set; }
    
    /// <summary>
    /// Gets or sets the risk score for burst trading activity.
    /// </summary>
    public double RiskScore { get; set; }
}

/// <summary>
/// Represents a burst of rapid trades indicating potential automation.
/// </summary>
public class TradeBurst
{
    /// <summary>
    /// Gets or sets the start time of the burst.
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Gets or sets the end time of the burst.
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// Gets or sets the number of trades in the burst.
    /// </summary>
    public int TradeCount { get; set; }
    
    /// <summary>
    /// Gets or sets the duration of the burst in seconds.
    /// </summary>
    public double Duration { get; set; }
    
    /// <summary>
    /// Gets or sets the number of unique trading partners in the burst.
    /// </summary>
    public int UniquePartners { get; set; }
    
    /// <summary>
    /// Gets or sets the average interval between trades in the burst.
    /// </summary>
    public double AverageInterval { get; set; }
}

#endregion

#region Network Fraud Detection

/// <summary>
/// Represents the analysis results for network fraud detection.
/// </summary>
public class NetworkFraudAnalysis
{
    /// <summary>
    /// Gets or sets the seed user ID for the network analysis.
    /// </summary>
    public ulong SeedUserId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the detected fraud network.
    /// </summary>
    public FraudNetwork DetectedNetwork { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of network connections.
    /// </summary>
    public List<NetworkConnection> NetworkConnections { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the risk score for the network.
    /// </summary>
    public double RiskScore { get; set; }
}

/// <summary>
/// Represents a detected fraud network.
/// </summary>
public class FraudNetwork
{
    /// <summary>
    /// Gets or sets the core members of the fraud network.
    /// </summary>
    public List<ulong> CoreMembers { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the type of network detected.
    /// </summary>
    public NetworkType NetworkType { get; set; }
    
    /// <summary>
    /// Gets or sets the estimated size of the network.
    /// </summary>
    public int EstimatedSize { get; set; }
}

/// <summary>
/// Represents a connection between users in a fraud network.
/// </summary>
public class NetworkConnection
{
    /// <summary>
    /// Gets or sets the first user ID in the connection.
    /// </summary>
    public ulong User1Id { get; set; }
    
    /// <summary>
    /// Gets or sets the second user ID in the connection.
    /// </summary>
    public ulong User2Id { get; set; }
    
    /// <summary>
    /// Gets or sets the strength of the connection between users.
    /// </summary>
    public double ConnectionStrength { get; set; }
    
    /// <summary>
    /// Gets or sets the number of direct trades between users.
    /// </summary>
    public int DirectTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the list of shared behaviors between users.
    /// </summary>
    public List<string> SharedBehaviors { get; set; } = new();
}

/// <summary>
/// Represents network connection analysis between trading parties.
/// </summary>
public class NetworkConnectionAnalysis
{
    /// <summary>
    /// Gets or sets a value indicating whether users are in the same network.
    /// </summary>
    public bool UsersInSameNetwork { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the network.
    /// </summary>
    public int NetworkSize { get; set; }
    
    /// <summary>
    /// Gets or sets the risk score for the network connection.
    /// </summary>
    public double NetworkRiskScore { get; set; }
}

#endregion

#region Market Manipulation Detection

/// <summary>
/// Represents market manipulation detection results.
/// </summary>
public class MarketManipulationAnalysis
{
    /// <summary>
    /// Gets or sets a value indicating whether price fixing was detected.
    /// </summary>
    public bool PriceFixingDetected { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether pump and dump activity was detected.
    /// </summary>
    public bool PumpAndDumpDetected { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether wash trading was detected.
    /// </summary>
    public bool WashTradingDetected { get; set; }
    
    /// <summary>
    /// Gets or sets the price volatility metric.
    /// </summary>
    public double PriceVolatility { get; set; }
    
    /// <summary>
    /// Gets or sets the number of circular trading partners.
    /// </summary>
    public int CircularTradingPartners { get; set; }
    
    /// <summary>
    /// Gets or sets the list of suspicious prices detected.
    /// </summary>
    public List<SuspiciousPrice> SuspiciousPrices { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the risk score for market manipulation.
    /// </summary>
    public double RiskScore { get; set; }
}

/// <summary>
/// Represents a suspicious price point in market manipulation.
/// </summary>
public class SuspiciousPrice
{
    /// <summary>
    /// Gets or sets the name of the Pokemon with suspicious pricing.
    /// </summary>
    public string PokemonName { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the suspicious price.
    /// </summary>
    public ulong Price { get; set; }
    
    /// <summary>
    /// Gets or sets the number of listings at this price.
    /// </summary>
    public int ListingCount { get; set; }
    
    /// <summary>
    /// Gets or sets the list of sellers involved in the suspicious pricing.
    /// </summary>
    public List<ulong> InvolvedSellers { get; set; } = new();
}

#endregion

#region Pokemon Laundering Detection

/// <summary>
/// Represents Pokemon laundering analysis for a trade.
/// </summary>
public class TradePokemonLaunderingAnalysis
{
    /// <summary>
    /// Gets or sets the list of individual Pokemon analyses.
    /// </summary>
    public List<PokemonLaunderingAnalysis> PokemonAnalyses { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the number of high-risk Pokemon detected.
    /// </summary>
    public int HighRiskPokemonCount { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of suspicious Pokemon.
    /// </summary>
    public int TotalSuspiciousPokemon { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum risk score among all Pokemon.
    /// </summary>
    public double MaxRiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether laundering was detected.
    /// </summary>
    public bool LaunderingDetected { get; set; }
}

/// <summary>
/// Represents the analysis results for Pokemon laundering detection.
/// </summary>
public class PokemonLaunderingAnalysis
{
    /// <summary>
    /// Gets or sets the ID of the Pokemon being analyzed.
    /// </summary>
    public ulong PokemonId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the analysis was performed.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets the ownership chain for the Pokemon.
    /// </summary>
    public List<OwnershipRecord> OwnershipChain { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the total number of transfers for this Pokemon.
    /// </summary>
    public int TransferCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of rapid transfers detected.
    /// </summary>
    public int RapidTransfers { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether a circular path was detected.
    /// </summary>
    public bool CircularPath { get; set; }
    
    /// <summary>
    /// Gets or sets the estimated value of the Pokemon.
    /// </summary>
    public decimal EstimatedValue { get; set; }
    
    /// <summary>
    /// Gets or sets the risk score for this Pokemon.
    /// </summary>
    public double RiskScore { get; set; }
}

/// <summary>
/// Represents a single ownership record in a Pokemon's history.
/// </summary>
public class OwnershipRecord
{
    /// <summary>
    /// Gets or sets the user ID of the owner.
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when ownership was obtained.
    /// </summary>
    public DateTime ObtainedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the method by which ownership was obtained.
    /// </summary>
    public string Method { get; set; } = "";
}

#endregion

#region User Behavior Analysis

/// <summary>
/// Represents a user's trading behavior pattern.
/// </summary>
public class UserBehaviorPattern
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the list of preferred trading hours.
    /// </summary>
    public List<int> PreferredTradingHours { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of preferred trading days.
    /// </summary>
    public List<DayOfWeek> PreferredTradingDays { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the average time between trades in hours.
    /// </summary>
    public double AverageTimeBetweenTrades { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of trades for this user.
    /// </summary>
    public int TotalTradeCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of unique trading partners.
    /// </summary>
    public int UniquePartners { get; set; }
}

/// <summary>
/// Represents an account with similar behavior patterns.
/// </summary>
public class SimilarAccount
{
    /// <summary>
    /// Gets or sets the user ID of the similar account.
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the behavior similarity score.
    /// </summary>
    public double BehaviorSimilarity { get; set; }
    
    /// <summary>
    /// Gets or sets the list of shared behaviors.
    /// </summary>
    public List<string> SharedBehaviors { get; set; } = new();
}

/// <summary>
/// Represents a user's risk profile based on trading history.
/// </summary>
public class UserRiskProfile
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the profile was last analyzed.
    /// </summary>
    public DateTime LastAnalyzed { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of trades analyzed.
    /// </summary>
    public int TotalTradesAnalyzed { get; set; }
    
    /// <summary>
    /// Gets or sets the history of risk scores for this user.
    /// </summary>
    public List<double> RiskScoreHistory { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the average risk score for this user.
    /// </summary>
    public double AverageRiskScore { get; set; }
    
    /// <summary>
    /// Gets or sets the number of high-risk trades for this user.
    /// </summary>
    public int HighRiskTradeCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of chain trading incidents.
    /// </summary>
    public int ChainTradingIncidents { get; set; }
    
    /// <summary>
    /// Gets or sets the number of burst trading incidents.
    /// </summary>
    public int BurstTradingIncidents { get; set; }
    
    /// <summary>
    /// Gets or sets the number of market manipulation incidents.
    /// </summary>
    public int MarketManipulationIncidents { get; set; }
}

#endregion

#region Action and Decision Models

/// <summary>
/// Represents a recommended action based on fraud analysis.
/// </summary>
public class RecommendedAction
{
    /// <summary>
    /// Gets or sets the type of fraud action to take.
    /// </summary>
    public FraudAction Action { get; set; }
    
    /// <summary>
    /// Gets or sets the urgency level of the action.
    /// </summary>
    public ActionUrgency Urgency { get; set; }
    
    /// <summary>
    /// Gets or sets the reason for the recommended action.
    /// </summary>
    public string Reason { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the list of suggested steps to take.
    /// </summary>
    public List<string> SuggestedSteps { get; set; } = new();
}

/// <summary>
/// Represents a market manipulation indicator.
/// </summary>
public class MarketManipulationIndicator
{
    /// <summary>
    /// Gets or sets the Pokemon species involved in the manipulation.
    /// </summary>
    public string PokemonSpecies { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the timestamp when the manipulation was detected.
    /// </summary>
    public DateTime DetectionTime { get; set; }
    
    /// <summary>
    /// Gets or sets the suspicion level for this indicator.
    /// </summary>
    public double SuspicionLevel { get; set; }
    
    /// <summary>
    /// Gets or sets the list of users involved in the manipulation.
    /// </summary>
    public List<ulong> InvolvedUsers { get; set; } = new();
}

#endregion

#region Market Analysis Models

/// <summary>
/// Represents market activity data for fraud analysis.
/// </summary>
public class MarketActivityData
{
    /// <summary>
    /// Gets or sets the Pokemon name.
    /// </summary>
    public string PokemonName { get; set; } = "";

    /// <summary>
    /// Gets or sets the listing price.
    /// </summary>
    public int Price { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the seller.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Gets or sets the date when the item was listed.
    /// </summary>
    public DateTime DateListed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item was sold.
    /// </summary>
    public bool Sold { get; set; }
}

#endregion

#region Enums

/// <summary>
/// Represents the risk level of a trade.
/// </summary>
public enum RiskLevel
{
    /// <summary>Minimal risk - trade is likely legitimate</summary>
    Minimal = 1,
    /// <summary>Low risk - trade has minor suspicious indicators</summary>
    Low = 2,
    /// <summary>Medium risk - trade has moderate suspicious indicators</summary>
    Medium = 3,
    /// <summary>High risk - trade has significant fraud indicators</summary>
    High = 4,
    /// <summary>Critical risk - trade has severe fraud indicators</summary>
    Critical = 5
}

/// <summary>
/// Represents possible fraud actions to take.
/// </summary>
public enum FraudAction
{
    /// <summary>Allow the trade to proceed</summary>
    Allow,
    /// <summary>Monitor the trade for additional patterns</summary>
    Monitor,
    /// <summary>Flag the trade for manual review</summary>
    FlagForReview,
    /// <summary>Block the trade and investigate further</summary>
    BlockAndInvestigate
}

/// <summary>
/// Represents the urgency level of fraud actions.
/// </summary>
public enum ActionUrgency
{
    /// <summary>Low urgency - monitor or log the activity</summary>
    Low,
    /// <summary>Medium urgency - flag for review within 24 hours</summary>
    Medium,
    /// <summary>High urgency - requires immediate attention</summary>
    High,
    /// <summary>Critical urgency - immediate action required to prevent damage</summary>
    Critical
}

/// <summary>
/// Represents the type of fraud network detected.
/// </summary>
public enum NetworkType
{
    /// <summary>Loose network - occasional connections between accounts</summary>
    Loose,
    /// <summary>Tight-knit network - frequent coordinated activity</summary>
    TightKnit,
    /// <summary>Large-scale network - organized fraud operation</summary>
    LargeScale
}

/// <summary>
/// Represents the result of a return trade detection check.
/// </summary>
public class ReturnTradeCheckResult
{
    /// <summary>
    /// Gets or sets whether this trade appears to be a legitimate return trade.
    /// </summary>
    public bool IsReturnTrade { get; set; }

    /// <summary>
    /// Gets or sets the reason for the determination.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of matching Pokemon found in recent trade history.
    /// </summary>
    public int MatchingPokemonCount { get; set; }

    /// <summary>
    /// Gets or sets the number of days since the original trade(s).
    /// </summary>
    public int DaysSinceOriginalTrade { get; set; }

    /// <summary>
    /// Gets or sets the confidence level of this being a return trade (0.0 to 1.0).
    /// </summary>
    public double ConfidenceLevel { get; set; }
}


#endregion