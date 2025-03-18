using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("skylog")]
public class SkyLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("u_id")]
    [Required]
    public ulong UserId { get; set; }
    
    [Column("command")]
    [Required]
    public string Command { get; set; } = null!;
    
    [Column("args")]
    [Required]
    public string Arguments { get; set; } = null!;
    
    [Column("jump")]
    public string? Jump { get; set; }
    
    [Column("time")]
    [Required]
    public DateTime Time { get; set; }
    
    [Column("note")]
    public string? Note { get; set; }
}