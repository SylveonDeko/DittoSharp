using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("disabled_channels")]
public class DisabledChannel
{
    [Key] [Column("channel")] public ulong ChannelId { get; set; }
}