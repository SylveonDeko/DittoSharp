using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("pokes")]
public class Pokemon
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("ditto_id")]
    public int DittoId { get; set; }
    
    #region Basic Info
    [Column("pokname")]
    [Required]
    public string PokemonName { get; set; } = null!;
    
    [Column("poknick")]
    [Required]
    public string Nickname { get; set; } = null!;
    
    [Column("name")]
    public string? Name { get; set; }
    
    [Column("gender")]
    [Required]
    public string Gender { get; set; } = null!;
    #endregion
    
    #region IVs
    [Column("hpiv")]
    [Required]
    public int HpIv { get; set; }
    
    [Column("atkiv")]
    [Required]
    public int AttackIv { get; set; }
    
    [Column("defiv")]
    [Required]
    public int DefenseIv { get; set; }
    
    [Column("spatkiv")]
    [Required]
    public int SpecialAttackIv { get; set; }
    
    [Column("spdefiv")]
    [Required]
    public int SpecialDefenseIv { get; set; }
    
    [Column("speediv")]
    [Required]
    public int SpeedIv { get; set; }
    #endregion
    
    #region EVs
    [Column("hpev")]
    [Required]
    public int HpEv { get; set; }
    
    [Column("atkev")]
    [Required]
    public int AttackEv { get; set; }
    
    [Column("defev")]
    [Required]
    public int DefenseEv { get; set; }
    
    [Column("spatkev")]
    [Required]
    public int SpecialAttackEv { get; set; }
    
    [Column("spdefev")]
    [Required]
    public int SpecialDefenseEv { get; set; }
    
    [Column("speedev")]
    [Required]
    public int SpeedEv { get; set; }
    #endregion
    
    #region Battle Info
    [Column("pokelevel")]
    [Required]
    public int Level { get; set; }
    
    [Column("moves", TypeName = "text[]")]
    [Required]
    public string[] Moves { get; set; } = [];
    
    [Column("hitem")]
    [Required]
    public string HeldItem { get; set; } = null!;
    
    [Column("exp")]
    [Required]
    public int Experience { get; set; }
    
    [Column("nature")]
    [Required]
    public string Nature { get; set; } = null!;
    
    [Column("expcap")]
    [Required]
    public int ExperienceCap { get; set; }
    
    [Column("happiness")]
    [Required]
    public int Happiness { get; set; }
    
    [Column("ability_index")]
    [Required]
    public int AbilityIndex { get; set; }
    #endregion
    
    #region Market Info
    [Column("price")]
    [Required]
    public int Price { get; set; }
    
    [Column("market_enlist")]
    [Required]
    public bool MarketEnlist { get; set; }
    #endregion
    
    #region Special Properties
    [Column("fav")]
    [Required]
    public bool Favorite { get; set; }
    
    [Column("shiny")]
    public bool? Shiny { get; set; }
    
    [Column("radiant")]
    public bool? Radiant { get; set; }
    
    [Column("champion")]
    [Required]
    public bool Champion { get; set; }
    
    [Column("voucher")]
    public bool? Voucher { get; set; }
    #endregion
    
    #region Metadata
    [Column("counter")]
    public int? Counter { get; set; }
    
    [Column("time_stamp")]
    public DateTime? Timestamp { get; set; }
    
    [Column("caught_by")]
    public ulong? CaughtBy { get; set; }
    
    [Column("tags", TypeName = "text[]")]
    [Required]
    public string[] Tags { get; set; } = [];
    
    [Column("skin")]
    public string? Skin { get; set; }
    #endregion
    
    #region Flags
    [Column("tradable")]
    [Required]
    public bool Tradable { get; set; } = true;
    
    [Column("breedable")]
    [Required]
    public bool Breedable { get; set; } = true;
    
    [Column("temp")]
    [Required]
    public bool Temporary { get; set; }
    
    [Column("owner")]
    public ulong Owner { get; set; }
    
    [Column("owned")]
    public bool? Owned { get; set; }
    #endregion
}