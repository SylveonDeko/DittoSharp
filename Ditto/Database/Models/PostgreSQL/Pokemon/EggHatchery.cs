using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("egg_hatchery")]
public class EggHatchery
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("u_id")]
    [Required]
    public ulong UserId { get; set; }
    
    [Column("1")]
    public int? Slot1 { get; set; }
    
    [Column("2")]
    public int? Slot2 { get; set; }
    
    [Column("3")]
    public int? Slot3 { get; set; }
    
    [Column("4")]
    public int? Slot4 { get; set; }
    
    [Column("5")]
    public int? Slot5 { get; set; }
    
    [Column("6")]
    public int? Slot6 { get; set; }
    
    [Column("7")]
    public int? Slot7 { get; set; }
    
    [Column("8")]
    public int? Slot8 { get; set; }
    
    [Column("9")]
    public int? Slot9 { get; set; }
    
    [Column("10")]
    public int? Slot10 { get; set; }
    
    [Column("eggs", TypeName = "integer[]")]
    public int[]? Eggs { get; set; }
    
    [Column("group")]
    public short? Group { get; set; }
}