using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("honey")]
public class Honey
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("channel")] [Required] public ulong ChannelId { get; set; }

    [Column("owner")] [Required] public ulong OwnerId { get; set; }

    [Column("type")] public string? Type { get; set; }

    [Column("expires")] [Required] public int Expires { get; set; }
}