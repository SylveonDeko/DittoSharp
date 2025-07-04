using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a log entry for a trade between users in the EeveeCore Pokémon bot system.
///     This class records the details of trading transactions, including the items exchanged.
/// </summary>
[Table("trade_logs")]
public class TradeLog
{
    /// <summary>
    ///     Gets or sets the unique identifier for this trade.
    /// </summary>
    [PrimaryKey]
    [Column("t_id")]
    public int TradeId { get; set; }

    /// <summary>
    ///     Gets or sets the command used to initiate the trade.
    /// </summary>
    [Column("command")]
    [NotNull]
    public string Command { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the date and time when the trade occurred.
    /// </summary>
    [Column("time")]
    public DateTime? Time { get; set; }

    #region Sender

    /// <summary>
    ///     Gets or sets the Discord user ID of the user who initiated the trade.
    /// </summary>
    [Column("sender")]
    [NotNull]
    public ulong SenderId { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs that the sender traded away.
    /// </summary>
    [Column("sender_pokes")]
    public ulong[]? SenderPokemon { get; set; }

    /// <summary>
    ///     Gets or sets the amount of MewCoins that the sender included in the trade.
    /// </summary>
    [Column("sender_credits")]
    public long? SenderCredits { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeem tokens that the sender included in the trade.
    /// </summary>
    [Column("sender_redeems")]
    [NotNull]
    public ulong SenderRedeems { get; set; }

    #endregion

    #region Receiver

    /// <summary>
    ///     Gets or sets the Discord user ID of the user who received the trade.
    /// </summary>
    [Column("receiver")]
    [NotNull]
    public ulong ReceiverId { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs that the receiver traded away.
    /// </summary>
    [Column("receiver_pokes")]
    public ulong[]? ReceiverPokemon { get; set; }

    /// <summary>
    ///     Gets or sets the amount of MewCoins that the receiver included in the trade.
    /// </summary>
    [Column("receiver_credits")]
    public long? ReceiverCredits { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeem tokens that the receiver included in the trade.
    /// </summary>
    [Column("receiver_redeems")]
    [NotNull]
    public ulong ReceiverRedeems { get; set; }

    #endregion
}