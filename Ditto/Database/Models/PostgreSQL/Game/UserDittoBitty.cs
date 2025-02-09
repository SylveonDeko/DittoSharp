using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Game;

[Table("user_dittobitties")]
public class UserDittoBitty
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("user_id")]
    [Required]
    public ulong UserId { get; set; }
    
    [Column("dbitty_id")]
    [Required]
    public int DittoBittyId { get; set; }
    
    #region Stats
    [Column("str")]
    [Required]
    public int Strength { get; set; }
    
    [Column("int")]
    [Required]
    public int Intelligence { get; set; }
    
    [Column("pdef")]
    [Required]
    public int PhysicalDefense { get; set; }
    
    [Column("mdef")]
    [Required]
    public int MagicalDefense { get; set; }
    
    [Column("agi")]
    [Required]
    public int Agility { get; set; }
    
    [Column("con")]
    [Required]
    public int Constitution { get; set; }
    #endregion
    
    [Column("experience")]
    [Required]
    public int Experience { get; set; }
    
    [Column("stage")]
    [Required]
    public short Stage { get; set; } = 1;
    
    [Column("current_hp")]
    [Required]
    public int CurrentHp { get; set; } = 10;
}