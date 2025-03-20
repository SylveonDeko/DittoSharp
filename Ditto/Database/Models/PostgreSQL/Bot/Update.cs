using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("updates")]
public class Update
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("update")] [Required] public string Content { get; set; } = null!;

    [Column("update_date")] [Required] public DateOnly UpdateDate { get; set; }

    [Column("dev")] public string? Developer { get; set; }
}