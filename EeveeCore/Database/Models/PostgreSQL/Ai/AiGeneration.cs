using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Ai;

[Table("ai_generations")]
public class AiGeneration
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("theme")] [Required] public string Theme { get; set; } = null!;

    [Column("generated_by")] public ulong? GeneratedBy { get; set; }

    [Column("time")] public DateTime? Time { get; set; }

    [Column("image_url")] public string? ImageUrl { get; set; }

    [Column("pokemon")] public string? Pokemon { get; set; }

    [Column("ranked")] public short? Ranked { get; set; }

    [Column("featured")] public bool? Featured { get; set; }
}