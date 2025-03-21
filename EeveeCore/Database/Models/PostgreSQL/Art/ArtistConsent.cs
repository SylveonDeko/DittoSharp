using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Art;

[Table("artists_consent")]
public class ArtistConsent
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("artist")] [Required] public string Artist { get; set; } = null!;

    [Column("deviantart")] public string? DeviantArt { get; set; }

    [Column("instagram")] public string? Instagram { get; set; }

    [Column("twitter")] public string? Twitter { get; set; }

    [Column("discord")] public string? Discord { get; set; }

    [Column("other_links", TypeName = "text[]")]
    public string[]? OtherLinks { get; set; }

    [Column("date")] public DateTime? Date { get; set; }

    [Column("comment")] public string? Comment { get; set; }

    [Column("info")] public string? Info { get; set; }

    [Column("contact")] public string? Contact { get; set; }

    [Column("extra")] public string? Extra { get; set; }

    [Column("in_use")] public bool? InUse { get; set; }
}