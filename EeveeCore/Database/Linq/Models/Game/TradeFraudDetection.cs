using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a fraud detection incident for trade-related suspicious activity.
///     This model logs detected fraud attempts, admin actions, and resolution outcomes.
/// </summary>
[Table(Name = "trade_fraud_detections")]
public class TradeFraudDetection
{
    /// <summary>
    ///     Gets or sets the unique identifier for this fraud detection record.
    /// </summary>
    [PrimaryKey, Identity]
    [Column(Name = "id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the trade ID associated with this fraud detection (if applicable).
    /// </summary>
    [Column(Name = "trade_id"), Nullable]
    public int? TradeId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the fraud was detected.
    /// </summary>
    [Column(Name = "detection_timestamp"), NotNull]
    public DateTime DetectionTimestamp { get; set; }

    #region Involved Users

    /// <summary>
    ///     Gets or sets the primary user ID associated with this fraud detection.
    /// </summary>
    [Column(Name = "primary_user_id"), NotNull]
    public ulong PrimaryUserId { get; set; }

    /// <summary>
    ///     Gets or sets the secondary user ID (if fraud involves multiple users).
    /// </summary>
    [Column(Name = "secondary_user_id"), Nullable]
    public ulong? SecondaryUserId { get; set; }

    /// <summary>
    ///     Gets or sets additional user IDs involved in the fraud pattern (JSON array).
    /// </summary>
    [Column(Name = "additional_user_ids"), Nullable]
    public string? AdditionalUserIds { get; set; }

    #endregion

    #region Detection Details

    /// <summary>
    ///     Gets or sets the type of fraud detected.
    /// </summary>
    [Column(Name = "fraud_type"), NotNull]
    public FraudType FraudType { get; set; }

    /// <summary>
    ///     Gets or sets the confidence level of the detection (0.0 to 1.0).
    /// </summary>
    [Column(Name = "confidence_level"), NotNull]
    public double ConfidenceLevel { get; set; }

    /// <summary>
    ///     Gets or sets the overall risk score that triggered the detection.
    /// </summary>
    [Column(Name = "risk_score"), NotNull]
    public double RiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the specific detection rules that were triggered (JSON array).
    /// </summary>
    [Column(Name = "triggered_rules"), NotNull]
    public string TriggeredRules { get; set; } = null!;

    /// <summary>
    ///     Gets or sets detailed analysis data in JSON format.
    /// </summary>
    [Column(Name = "detection_details"), Nullable]
    public string? DetectionDetails { get; set; }

    #endregion

    #region Action Taken

    /// <summary>
    ///     Gets or sets the automated action taken by the system.
    /// </summary>
    [Column(Name = "automated_action"), NotNull]
    public AutomatedAction AutomatedAction { get; set; }

    /// <summary>
    ///     Gets or sets whether the trade was blocked by the detection system.
    /// </summary>
    [Column(Name = "trade_blocked"), NotNull]
    public bool TradeBlocked { get; set; }

    /// <summary>
    ///     Gets or sets whether users were notified about the detection.
    /// </summary>
    [Column(Name = "users_notified"), NotNull]
    public bool UsersNotified { get; set; }

    /// <summary>
    ///     Gets or sets whether administrators were alerted.
    /// </summary>
    [Column(Name = "admin_alerted"), NotNull]
    public bool AdminAlerted { get; set; }

    #endregion

    #region Investigation and Resolution

    /// <summary>
    ///     Gets or sets the current status of this fraud detection case.
    /// </summary>
    [Column(Name = "investigation_status"), NotNull]
    public InvestigationStatus InvestigationStatus { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the administrator handling this case.
    /// </summary>
    [Column(Name = "assigned_admin_id"), Nullable]
    public long? AssignedAdminId { get; set; }

    /// <summary>
    ///     Gets or sets when the investigation was started.
    /// </summary>
    [Column(Name = "investigation_started"), Nullable]
    public DateTime? InvestigationStarted { get; set; }

    /// <summary>
    ///     Gets or sets when the case was resolved.
    /// </summary>
    [Column(Name = "resolution_timestamp"), Nullable]
    public DateTime? ResolutionTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the final verdict of the investigation.
    /// </summary>
    [Column(Name = "final_verdict"), Nullable]
    public FraudVerdict? FinalVerdict { get; set; }

    /// <summary>
    ///     Gets or sets administrative notes about the investigation.
    /// </summary>
    [Column(Name = "admin_notes"), Nullable]
    public string? AdminNotes { get; set; }

    /// <summary>
    ///     Gets or sets the actions taken by administrators.
    /// </summary>
    [Column(Name = "admin_actions"), Nullable]
    public string? AdminActions { get; set; }

    #endregion

    #region False Positive Analysis

    /// <summary>
    ///     Gets or sets whether this detection was determined to be a false positive.
    /// </summary>
    [Column(Name = "false_positive"), NotNull]
    public bool FalsePositive { get; set; }

    /// <summary>
    ///     Gets or sets the reason why it was a false positive (if applicable).
    /// </summary>
    [Column(Name = "false_positive_reason"), Nullable]
    public string? FalsePositiveReason { get; set; }

    /// <summary>
    ///     Gets or sets whether the detection rules should be adjusted based on this case.
    /// </summary>
    [Column(Name = "requires_rule_adjustment"), NotNull]
    public bool RequiresRuleAdjustment { get; set; }

    #endregion

    #region Comprehensive Detection Fields

    /// <summary>
    ///     Gets or sets whether chain trading was detected.
    /// </summary>
    [Column(Name = "chain_trading_detected"), NotNull]
    public bool ChainTradingDetected { get; set; }

    /// <summary>
    ///     Gets or sets whether burst trading was detected.
    /// </summary>
    [Column(Name = "burst_trading_detected"), NotNull]
    public bool BurstTradingDetected { get; set; }

    /// <summary>
    ///     Gets or sets whether network fraud was detected.
    /// </summary>
    [Column(Name = "network_fraud_detected"), NotNull]
    public bool NetworkFraudDetected { get; set; }

    /// <summary>
    ///     Gets or sets whether market manipulation was detected.
    /// </summary>
    [Column(Name = "market_manipulation_detected"), NotNull]
    public bool MarketManipulationDetected { get; set; }

    /// <summary>
    ///     Gets or sets whether pokemon laundering was detected.
    /// </summary>
    [Column(Name = "pokemon_laundering_detected"), NotNull]
    public bool PokemonLaunderingDetected { get; set; }

    /// <summary>
    ///     Gets or sets the comprehensive risk score.
    /// </summary>
    [Column(Name = "comprehensive_risk_score"), Nullable]
    public double? ComprehensiveRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets actionable insights as JSON.
    /// </summary>
    [Column(Name = "actionable_insights"), Nullable]
    public string? ActionableInsights { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    ///     Gets or sets the ID of the associated suspicious trade analytics record (if applicable).
    /// </summary>
    [Column(Name = "suspicious_trade_analytics_id"), Nullable]
    public int? SuspiciousTradeAnalyticsId { get; set; }

    /// <summary>
    ///     Gets or sets the associated suspicious trade analytics record.
    /// </summary>
    [Association(ThisKey = nameof(SuspiciousTradeAnalyticsId), OtherKey = nameof(Models.Game.SuspiciousTradeAnalytics.Id))]
    public SuspiciousTradeAnalytics? SuspiciousTradeAnalytics { get; set; }

    #endregion
}

/// <summary>
///     Represents the type of fraud detected.
/// </summary>
public enum FraudType
{
    /// <summary>Alt account trading</summary>
    AltAccountTrading = 1,
    
    /// <summary>Real money trading</summary>
    RealMoneyTrading = 2,
    
    /// <summary>Bot or automation abuse</summary>
    BotAbuse = 3,
    
    /// <summary>Newbie exploitation</summary>
    NewbieExploitation = 4,
    
    /// <summary>Market manipulation</summary>
    MarketManipulation = 5,
    
    /// <summary>Duplication or exploit attempts</summary>
    DuplicationAttempt = 6,
    
    /// <summary>Unusual behavioral patterns</summary>
    UnusualBehavior = 7,
    
    /// <summary>Network coordination</summary>
    NetworkCoordination = 8
}

/// <summary>
///     Represents the automated action taken by the fraud detection system.
/// </summary>
public enum AutomatedAction
{
    /// <summary>No action taken, logged for monitoring</summary>
    LogOnly = 1,
    
    /// <summary>Flagged for manual review</summary>
    FlagForReview = 2,
    
    /// <summary>Trade execution blocked</summary>
    BlockTrade = 3,
    
    /// <summary>User temporarily restricted from trading</summary>
    TempRestriction = 4,
    
    /// <summary>User banned from trading</summary>
    TradeBan = 5,
    
    /// <summary>Admin immediately notified</summary>
    AdminAlert = 6
}

/// <summary>
///     Represents the status of an investigation into detected fraud.
/// </summary>
public enum InvestigationStatus
{
    /// <summary>Case is pending investigation</summary>
    Pending = 1,
    
    /// <summary>Case is under investigation</summary>
    InProgress = 2,
    
    /// <summary>Investigation is complete</summary>
    Completed = 3,
    
    /// <summary>Case was dismissed without action</summary>
    Dismissed = 4,
    
    /// <summary>Case was escalated to higher authority</summary>
    Escalated = 5
}

/// <summary>
///     Represents the final verdict of a fraud investigation.
/// </summary>
public enum FraudVerdict
{
    /// <summary>Activity was determined to be legitimate</summary>
    Legitimate = 1,
    
    /// <summary>Activity was confirmed as fraudulent</summary>
    Fraudulent = 2,
    
    /// <summary>Inconclusive evidence</summary>
    Inconclusive = 3,
    
    /// <summary>Detection was a false positive</summary>
    FalsePositive = 4,
    
    /// <summary>Warning issued but no punishment</summary>
    Warning = 5
}