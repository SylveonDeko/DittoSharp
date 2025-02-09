using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Game;

[Table("gyms")]
public class Gym
{
    [Key]
    [Column("u_id")]
    public ulong UserId { get; set; }
    
    #region Gym Status
    [Column("theblazingbowl")]
    public bool? TheBlazingBowl { get; set; }
    
    [Column("theunderwaterarena")]
    public bool? TheUnderwaterArena { get; set; }
    
    [Column("fieldofflowers")]
    public bool? FieldOfFlowers { get; set; }
    
    [Column("prismpalace")]
    public bool? PrismPalace { get; set; }
    
    [Column("thebasicbungalow")]
    public bool? TheBasicBungalow { get; set; }
    
    [Column("starterstation")]
    public bool? StarterStation { get; set; }
    
    [Column("floatingisland")]
    public bool? FloatingIsland { get; set; }
    
    [Column("stablessstadium")]
    public bool? StablessStadium { get; set; }
    
    [Column("triangulartower")]
    public bool? TriangularTower { get; set; }
    
    [Column("monotypemarket")]
    public bool? MonotypeMarket { get; set; }
    
    [Column("generationalgallery")]
    public bool? GenerationalGallery { get; set; }
    
    [Column("bansaucearena")]
    public bool? BansauceArena { get; set; }
    
    [Column("physicalfortress")]
    public bool? PhysicalFortress { get; set; }
    
    [Column("specialshack")]
    public bool? SpecialShack { get; set; }
    
    [Column("reversedimension")]
    public bool? ReverseDimension { get; set; }
    
    [Column("elite4")]
    public bool? Elite4 { get; set; }
    #endregion
    
    #region Timestamps
    [Column("theblazingbowl_ts")]
    public ulong? TheBlazingBowlTimestamp { get; set; }
    
    [Column("theunderwaterarena_ts")]
    public ulong? TheUnderwaterArenaTimestamp { get; set; }
    
    [Column("fieldofflowers_ts")]
    public ulong? FieldOfFlowersTimestamp { get; set; }
    
    [Column("prismpalace_ts")]
    public ulong? PrismPalaceTimestamp { get; set; }
    
    [Column("thebasicbungalow_ts")]
    public ulong? TheBasicBungalowTimestamp { get; set; }
    
    [Column("starterstation_ts")]
    public ulong? StarterStationTimestamp { get; set; }
    
    [Column("floatingisland_ts")]
    public ulong? FloatingIslandTimestamp { get; set; }
    
    [Column("stablessstadium_ts")]
    public ulong? StablessStadiumTimestamp { get; set; }
    
    [Column("triangulartower_ts")]
    public ulong? TriangularTowerTimestamp { get; set; }
    
    [Column("monotypemarket_ts")]
    public ulong? MonotypeMarketTimestamp { get; set; }
    
    [Column("generationalgallery_ts")]
    public ulong? GenerationalGalleryTimestamp { get; set; }
    
    [Column("bansaucearena_ts")]
    public ulong? BansauceArenaTimestamp { get; set; }
    
    [Column("physicalfortress_ts")]
    public ulong? PhysicalFortressTimestamp { get; set; }
    
    [Column("specialshack_ts")]
    public ulong? SpecialShackTimestamp { get; set; }
    
    [Column("reversedimension_ts")]
    public ulong? ReverseDimensionTimestamp { get; set; }
    
    [Column("elite4_ts")]
    public ulong? Elite4Timestamp { get; set; }
    #endregion
    
    [Column("cooldown")]
    public ulong? Cooldown { get; set; }
}