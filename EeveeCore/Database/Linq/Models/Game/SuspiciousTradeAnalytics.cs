using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents analytics data for detecting suspicious trading patterns.
///     This model stores computed risk metrics and analysis results for trade fraud detection.
/// </summary>
[Table(Name = "suspicious_trade_analytics")]
public class SuspiciousTradeAnalytics
{
    /// <summary>
    ///     Gets or sets the unique identifier for this analytics record.
    /// </summary>
    [PrimaryKey, Identity]
    [Column(Name = "id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the trade ID this analytics record is associated with.
    /// </summary>
    [Column(Name = "trade_id"), NotNull]
    public int TradeId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when this analysis was performed.
    /// </summary>
    [Column(Name = "analysis_timestamp"), NotNull]
    public DateTime AnalysisTimestamp { get; set; }

    #region Risk Scores

    /// <summary>
    ///     Gets or sets the overall risk score for this trade (0.0 to 1.0).
    /// </summary>
    [Column(Name = "overall_risk_score"), NotNull]
    public double OverallRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the value imbalance score indicating how one-sided the trade is.
    /// </summary>
    [Column(Name = "value_imbalance_score"), NotNull]
    public double ValueImbalanceScore { get; set; }

    /// <summary>
    ///     Gets or sets the relationship risk score based on trading partner analysis.
    /// </summary>
    [Column(Name = "relationship_risk_score"), NotNull]
    public double RelationshipRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the behavioral risk score based on timing and pattern analysis.
    /// </summary>
    [Column(Name = "behavioral_risk_score"), NotNull]
    public double BehavioralRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the account age risk score based on new account exploitation patterns.
    /// </summary>
    [Column(Name = "account_age_risk_score"), NotNull]
    public double AccountAgeRiskScore { get; set; }

    #endregion

    #region Trade Value Analysis

    /// <summary>
    ///     Gets or sets the estimated total value of items given by the sender.
    /// </summary>
    [Column(Name = "sender_total_value"), NotNull]
    public decimal SenderTotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the estimated total value of items given by the receiver.
    /// </summary>
    [Column(Name = "receiver_total_value"), NotNull]
    public decimal ReceiverTotalValue { get; set; }

    /// <summary>
    ///     Gets or sets the calculated value ratio (higher value side / lower value side).
    /// </summary>
    [Column(Name = "value_ratio"), NotNull]
    public double ValueRatio { get; set; }

    /// <summary>
    ///     Gets or sets the absolute value difference between the two sides.
    /// </summary>
    [Column(Name = "value_difference"), NotNull]
    public decimal ValueDifference { get; set; }

    #endregion

    #region Account Analysis

    /// <summary>
    ///     Gets or sets the age of the sender's account in days when the trade occurred.
    /// </summary>
    [Column(Name = "sender_account_age_days"), NotNull]
    public int SenderAccountAgeDays { get; set; }

    /// <summary>
    ///     Gets or sets the age of the receiver's account in days when the trade occurred.
    /// </summary>
    [Column(Name = "receiver_account_age_days"), NotNull]
    public int ReceiverAccountAgeDays { get; set; }

    /// <summary>
    ///     Gets or sets the number of previous trades between these two users.
    /// </summary>
    [Column(Name = "previous_trades_count"), NotNull]
    public int PreviousTradesCount { get; set; }

    /// <summary>
    ///     Gets or sets the total value previously exchanged between these users.
    /// </summary>
    [Column(Name = "previous_total_value"), NotNull]
    public decimal PreviousTotalValue { get; set; }

    #endregion

    #region Detection Flags

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential alt account activity.
    /// </summary>
    [Column(Name = "flagged_alt_account"), NotNull]
    public bool FlaggedAltAccount { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential RMT (real money trading).
    /// </summary>
    [Column(Name = "flagged_rmt"), NotNull]
    public bool FlaggedRmt { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential newbie exploitation.
    /// </summary>
    [Column(Name = "flagged_newbie_exploitation"), NotNull]
    public bool FlaggedNewbieExploitation { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for unusual behavioral patterns.
    /// </summary>
    [Column(Name = "flagged_unusual_behavior"), NotNull]
    public bool FlaggedUnusualBehavior { get; set; }

    /// <summary>
    ///     Gets or sets whether this trade was flagged for potential bot activity.
    /// </summary>
    [Column(Name = "flagged_bot_activity"), NotNull]
    public bool FlaggedBotActivity { get; set; }

    #endregion

    #region Additional Metadata

    /// <summary>
    ///     Gets or sets additional analysis notes in JSON format.
    /// </summary>
    [Column(Name = "analysis_notes"), Nullable]
    public string? AnalysisNotes { get; set; }

    /// <summary>
    ///     Gets or sets whether this analysis was reviewed by an administrator.
    /// </summary>
    [Column(Name = "admin_reviewed"), NotNull]
    public bool AdminReviewed { get; set; }

    /// <summary>
    ///     Gets or sets the admin's verdict if reviewed (true = legitimate, false = fraudulent, null = pending).
    /// </summary>
    [Column(Name = "admin_verdict"), Nullable]
    public bool? AdminVerdict { get; set; }

    /// <summary>
    ///     Gets or sets admin review notes.
    /// </summary>
    [Column(Name = "admin_notes"), Nullable]
    public string? AdminNotes { get; set; }

    /// <summary>
    ///     Gets or sets when the admin review was completed.
    /// </summary>
    [Column(Name = "admin_review_timestamp"), Nullable]
    public DateTime? AdminReviewTimestamp { get; set; }

    #endregion
}