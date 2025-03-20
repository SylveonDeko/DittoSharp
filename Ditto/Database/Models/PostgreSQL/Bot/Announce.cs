using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("announce")]
public class Announce
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("announce")] public string? AnnounceText { get; set; }

    [Column("staff")] public string? Staff { get; set; }

    [Column("timestamp")] public int? Timestamp { get; set; }

    [Column("imp_level")] [Required] public int ImportanceLevel { get; set; }

    [Column("title")] [Required] public string Title { get; set; } = "Bot Announcement";
}