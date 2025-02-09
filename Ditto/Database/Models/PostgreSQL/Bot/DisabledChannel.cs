using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("disabled_channels")]
public class DisabledChannel
{
    [Key]
    [Column("channel")]
    public ulong ChannelId { get; set; }
}