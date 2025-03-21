using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Art;

[Table("artists")]
public class Artist
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("artist")] public string? ArtistName { get; set; }

    [Column("pokemon")] public string? Pokemon { get; set; }

    [Column("link")] public string? Link { get; set; }

    [Column("in_use")] [Required] public bool InUse { get; set; }
}