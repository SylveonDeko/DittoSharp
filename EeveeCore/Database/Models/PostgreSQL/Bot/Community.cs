using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("community")]
[Keyless]
public class Community
{
    [Column("credits")] public ulong? Credits { get; set; }

    [Column("redeems")] public ulong? Redeems { get; set; }

    [Column("pokes", TypeName = "bigint[]")]
    public long[]? Pokes { get; set; }

    [Column("items", TypeName = "bigint[]")]
    public long[]? Items { get; set; }

    [Column("other")] public string? Other { get; set; }
}