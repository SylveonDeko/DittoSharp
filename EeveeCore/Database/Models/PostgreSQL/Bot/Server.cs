using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("servers")]
public class Server
{
    [Key] [Column("serverid")] public ulong ServerId { get; set; }

    [Column("prefix")] [Required] public string Prefix { get; set; } = ";";

    [Column("language")] [StringLength(2)] public string? Language { get; set; }

    [Column("redirects", TypeName = "bigint[]")]
    public long[]? Redirects { get; set; }

    [Column("delspawns")] public bool? DeleteSpawns { get; set; }

    [Column("pinspawns")] public bool? PinSpawns { get; set; }

    [Column("disabled_channels", TypeName = "bigint[]")]
    public long[]? DisabledChannels { get; set; }

    [Column("spawns_disabled", TypeName = "bigint[]")]
    public long[]? SpawnsDisabled { get; set; }
}