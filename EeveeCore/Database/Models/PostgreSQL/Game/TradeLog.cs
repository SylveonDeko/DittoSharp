using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

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
    [Key]
    [Column("t_id")]
    public int TradeId { get; set; }

    /// <summary>
    ///     Gets or sets the command used to initiate the trade.
    /// </summary>
    [Column("command")]
    [Required]
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
    [Required]
    public ulong SenderId { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs that the sender traded away.
    /// </summary>
    [Column("sender_pokes", TypeName = "bigint[]")]
    public long[]? SenderPokemon { get; set; }

    /// <summary>
    ///     Gets or sets the amount of MewCoins that the sender included in the trade.
    /// </summary>
    [Column("sender_credits")]
    public ulong? SenderCredits { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeem tokens that the sender included in the trade.
    /// </summary>
    [Column("sender_redeems")]
    [Required]
    public ulong SenderRedeems { get; set; }

    #endregion

    #region Receiver

    /// <summary>
    ///     Gets or sets the Discord user ID of the user who received the trade.
    /// </summary>
    [Column("receiver")]
    [Required]
    public ulong ReceiverId { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs that the receiver traded away.
    /// </summary>
    [Column("receiver_pokes", TypeName = "bigint[]")]
    public long[]? ReceiverPokemon { get; set; }

    /// <summary>
    ///     Gets or sets the amount of MewCoins that the receiver included in the trade.
    /// </summary>
    [Column("receiver_credits")]
    public ulong? ReceiverCredits { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeem tokens that the receiver included in the trade.
    /// </summary>
    [Column("receiver_redeems")]
    [Required]
    public ulong ReceiverRedeems { get; set; }

    #endregion
}