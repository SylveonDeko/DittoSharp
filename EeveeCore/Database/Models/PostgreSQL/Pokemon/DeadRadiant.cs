using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

[Table("dead_radiants")]
public class DeadRadiant
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("pokemon")] [Required] public string Pokemon { get; set; } = null!;

    [Column("dead")] public int? Dead { get; set; }

    [Column("types", TypeName = "text[]")] public string[]? Types { get; set; }
}