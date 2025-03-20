using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Ai;

[Table("ai_themes")]
public class AiTheme
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("creator")] public ulong? Creator { get; set; }

    [Column("featured")] public bool? Featured { get; set; }

    [Column("name")] [Required] public string Name { get; set; } = null!;

    [Column("used")] public ulong? Used { get; set; }

    [Column("prompt")] public string? Prompt { get; set; }

    [Column("negative_prompt")] public string? NegativePrompt { get; set; }

    [Column("text_cfg")] public short? TextCfg { get; set; }

    [Column("weight")] public float? Weight { get; set; }

    [Column("steps")] public int? Steps { get; set; }

    [Column("style")] public int? Style { get; set; }

    [Column("display_img")] public string? DisplayImg { get; set; }
}