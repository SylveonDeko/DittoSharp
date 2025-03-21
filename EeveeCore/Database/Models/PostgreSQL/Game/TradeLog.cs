using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("trade_logs")]
public class TradeLog
{
    [Key] [Column("t_id")] public int TradeId { get; set; }

    [Column("command")] [Required] public string Command { get; set; } = null!;

    [Column("time")] public DateTime? Time { get; set; }

    #region Sender

    [Column("sender")] [Required] public ulong SenderId { get; set; }

    [Column("sender_pokes", TypeName = "bigint[]")]
    public long[]? SenderPokemon { get; set; }

    [Column("sender_credits")] public ulong? SenderCredits { get; set; }

    [Column("sender_redeems")] [Required] public ulong SenderRedeems { get; set; }

    #endregion

    #region Receiver

    [Column("receiver")] [Required] public ulong ReceiverId { get; set; }

    [Column("receiver_pokes", TypeName = "bigint[]")]
    public long[]? ReceiverPokemon { get; set; }

    [Column("receiver_credits")] public ulong? ReceiverCredits { get; set; }

    [Column("receiver_redeems")]
    [Required]
    public ulong ReceiverRedeems { get; set; }

    #endregion
}