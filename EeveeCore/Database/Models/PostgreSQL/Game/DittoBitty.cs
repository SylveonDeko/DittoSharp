using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

[Table("EeveeCorebitties")]
public class EeveeCoreBitty
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("name")]
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("rarity")]
    [Required]
    [StringLength(20)]
    public string Rarity { get; set; } = null!;

    [Column("special")] [Required] public bool Special { get; set; }

    [Column("url_1")] [Required] public string Url1 { get; set; } = null!;

    [Column("url_2")] [Required] public string Url2 { get; set; } = null!;

    [Column("url_3")] [Required] public string Url3 { get; set; } = null!;

    [Column("url_4")] [Required] public string Url4 { get; set; } = null!;

    [Column("url_5")] [Required] public string Url5 { get; set; } = null!;
}