using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("cal")]
public class Cal
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("week")] public int? Week { get; set; }

    [Column("monday")] public int? Monday { get; set; }

    [Column("tuesday")] public int? Tuesday { get; set; }

    [Column("wednesday")] public int? Wednesday { get; set; }

    [Column("thursday")] public int? Thursday { get; set; }

    [Column("friday")] public int? Friday { get; set; }

    [Column("saturday")] public int? Saturday { get; set; }

    [Column("sunday")] public int? Sunday { get; set; }
}