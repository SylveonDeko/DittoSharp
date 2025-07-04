using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents the trading relationship between two users for fraud detection analysis.
///     This model tracks patterns and statistics of trades between specific user pairs.
/// </summary>
[Table(Name = "user_trade_relationships")]
public class UserTradeRelationship
{
    /// <summary>
    ///     Gets or sets the unique identifier for this relationship record.
    /// </summary>
    [PrimaryKey, Identity]
    [Column(Name = "id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the first user (always the lower ID for consistency).
    /// </summary>
    [Column(Name = "user1_id"), NotNull]
    public ulong User1Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the second user (always the higher ID for consistency).
    /// </summary>
    [Column(Name = "user2_id"), NotNull]
    public ulong User2Id { get; set; }

    #region Trade Statistics

    /// <summary>
    ///     Gets or sets the total number of trades between these users.
    /// </summary>
    [Column(Name = "total_trades"), NotNull]
    public int TotalTrades { get; set; }

    /// <summary>
    ///     Gets or sets the date and time of the first trade between these users.
    /// </summary>
    [Column(Name = "first_trade_timestamp"), NotNull]
    public DateTime FirstTradeTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the date and time of the most recent trade between these users.
    /// </summary>
    [Column(Name = "last_trade_timestamp"), NotNull]
    public DateTime LastTradeTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the total value of items User1 has given to User2.
    /// </summary>
    [Column(Name = "user1_total_given_value"), NotNull]
    public decimal User1TotalGivenValue { get; set; }

    /// <summary>
    ///     Gets or sets the total value of items User2 has given to User1.
    /// </summary>
    [Column(Name = "user2_total_given_value"), NotNull]
    public decimal User2TotalGivenValue { get; set; }

    /// <summary>
    ///     Gets or sets the number of trades where User1 gave significantly more value.
    /// </summary>
    [Column(Name = "user1_favoring_trades"), NotNull]
    public int User1FavoringTrades { get; set; }

    /// <summary>
    ///     Gets or sets the number of trades where User2 gave significantly more value.
    /// </summary>
    [Column(Name = "user2_favoring_trades"), NotNull]
    public int User2FavoringTrades { get; set; }

    /// <summary>
    ///     Gets or sets the number of approximately equal value trades.
    /// </summary>
    [Column(Name = "balanced_trades"), NotNull]
    public int BalancedTrades { get; set; }

    #endregion

    #region Risk Analysis

    /// <summary>
    ///     Gets or sets the calculated relationship risk score (0.0 to 1.0).
    /// </summary>
    [Column(Name = "relationship_risk_score"), NotNull]
    public double RelationshipRiskScore { get; set; }

    /// <summary>
    ///     Gets or sets the value imbalance ratio (higher giver / lower giver).
    /// </summary>
    [Column(Name = "value_imbalance_ratio"), NotNull]
    public double ValueImbalanceRatio { get; set; }

    /// <summary>
    ///     Gets or sets the trading frequency score (trades per day since first trade).
    /// </summary>
    [Column(Name = "trading_frequency_score"), NotNull]
    public double TradingFrequencyScore { get; set; }

    /// <summary>
    ///     Gets or sets whether this relationship has been flagged for potential alt account activity.
    /// </summary>
    [Column(Name = "flagged_potential_alts"), NotNull]
    public bool FlaggedPotentialAlts { get; set; }

    /// <summary>
    ///     Gets or sets whether this relationship has been flagged for potential RMT.
    /// </summary>
    [Column(Name = "flagged_potential_rmt"), NotNull]
    public bool FlaggedPotentialRmt { get; set; }

    /// <summary>
    ///     Gets or sets whether this relationship has been flagged for potential newbie exploitation.
    /// </summary>
    [Column(Name = "flagged_newbie_exploitation"), NotNull]
    public bool FlaggedNewbieExploitation { get; set; }

    #endregion

    #region Account Age Analysis

    /// <summary>
    ///     Gets or sets the difference in account creation time (in days) when first trade occurred.
    /// </summary>
    [Column(Name = "account_age_difference_days"), NotNull]
    public int AccountAgeDifferenceDays { get; set; }

    /// <summary>
    ///     Gets or sets whether both accounts were created within a suspicious timeframe.
    /// </summary>
    [Column(Name = "suspicious_creation_timing"), NotNull]
    public bool SuspiciousCreationTiming { get; set; }

    /// <summary>
    ///     Gets or sets the age of the newer account when the first trade occurred.
    /// </summary>
    [Column(Name = "newer_account_age_at_first_trade"), NotNull]
    public int NewerAccountAgeAtFirstTrade { get; set; }

    #endregion

    #region Temporal Analysis

    /// <summary>
    ///     Gets or sets the average time between trades in hours.
    /// </summary>
    [Column(Name = "average_trade_interval_hours"), NotNull]
    public double AverageTradeIntervalHours { get; set; }

    /// <summary>
    ///     Gets or sets the standard deviation of trade intervals.
    /// </summary>
    [Column(Name = "trade_interval_std_dev"), NotNull]
    public double TradeIntervalStdDev { get; set; }

    /// <summary>
    ///     Gets or sets whether trading patterns suggest automation.
    /// </summary>
    [Column(Name = "suspicious_timing_pattern"), NotNull]
    public bool SuspiciousTimingPattern { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    ///     Gets or sets the date and time when this relationship data was last updated.
    /// </summary>
    [Column(Name = "last_updated"), NotNull]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    ///     Gets or sets whether this relationship has been reviewed by an administrator.
    /// </summary>
    [Column(Name = "admin_reviewed"), NotNull]
    public bool AdminReviewed { get; set; }

    /// <summary>
    ///     Gets or sets the administrator's verdict on this relationship.
    /// </summary>
    [Column(Name = "admin_verdict"), Nullable]
    public bool? AdminVerdict { get; set; }

    /// <summary>
    ///     Gets or sets administrative notes about this relationship.
    /// </summary>
    [Column(Name = "admin_notes"), Nullable]
    public string? AdminNotes { get; set; }

    /// <summary>
    ///     Gets or sets whether this relationship is whitelisted (trusted).
    /// </summary>
    [Column(Name = "whitelisted"), NotNull]
    public bool Whitelisted { get; set; }

    #endregion
}