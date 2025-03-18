using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("egg_hatchery")]
public class EggHatchery
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }
    
    [Column("u_id")]
    [Required]
    public ulong UserId { get; set; }
    
    [Column("1")]
    public ulong? Slot1 { get; set; }
    
    [Column("2")]
    public ulong? Slot2 { get; set; }
    
    [Column("3")]
    public ulong? Slot3 { get; set; }
    
    [Column("4")]
    public ulong? Slot4 { get; set; }
    
    [Column("5")]
    public ulong? Slot5 { get; set; }
    
    [Column("6")]
    public ulong? Slot6 { get; set; }
    
    [Column("7")]
    public ulong? Slot7 { get; set; }
    
    [Column("8")]
    public ulong? Slot8 { get; set; }
    
    [Column("9")]
    public ulong? Slot9 { get; set; }
    
    [Column("10")]
    public ulong? Slot10 { get; set; }
    
    [Column("eggs", TypeName = "ulongeger[]")]
    public ulong[]? Eggs { get; set; }
    
    [Column("group")]
    public short? Group { get; set; }
}