using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a user's gym progress in the EeveeCore Pokémon bot system.
///     This class tracks which gyms the user has defeated and when, as well as cooldown periods.
/// </summary>
[Table("gyms")]
public class Gym
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this gym progress record.
    ///     This serves as the primary key for the gym record.
    /// </summary>
    [PrimaryKey]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the gym cooldown expires.
    ///     Users must wait for the cooldown to expire before challenging gyms again.
    /// </summary>
    [Column("cooldown")]
    public long? Cooldown { get; set; }

    #region Gym Status

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated The Blazing Bowl gym.
    ///     This is likely a Fire-type themed gym.
    /// </summary>
    [Column("theblazingbowl")]
    public bool? TheBlazingBowl { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated The Underwater Arena gym.
    ///     This is likely a Water-type themed gym.
    /// </summary>
    [Column("theunderwaterarena")]
    public bool? TheUnderwaterArena { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Field of Flowers gym.
    ///     This is likely a Grass or Fairy-type themed gym.
    /// </summary>
    [Column("fieldofflowers")]
    public bool? FieldOfFlowers { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Prism Palace gym.
    ///     This may be a Psychic or Fairy-type themed gym.
    /// </summary>
    [Column("prismpalace")]
    public bool? PrismPalace { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated The Basic Bungalow gym.
    ///     This is likely a Normal-type themed gym.
    /// </summary>
    [Column("thebasicbungalow")]
    public bool? TheBasicBungalow { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Starter Station gym.
    ///     This likely features starter Pokémon from various regions.
    /// </summary>
    [Column("starterstation")]
    public bool? StarterStation { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Floating Island gym.
    ///     This is likely a Flying-type themed gym.
    /// </summary>
    [Column("floatingisland")]
    public bool? FloatingIsland { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Stabless Stadium gym.
    ///     This may feature Pokémon without Same Type Attack Bonus (STAB) moves.
    /// </summary>
    [Column("stablessstadium")]
    public bool? StablessStadium { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Triangular Tower gym.
    /// </summary>
    [Column("triangulartower")]
    public bool? TriangularTower { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Monotype Market gym.
    ///     This likely features teams of a single Pokémon type.
    /// </summary>
    [Column("monotypemarket")]
    public bool? MonotypeMarket { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Generational Gallery gym.
    ///     This likely features Pokémon from specific generations.
    /// </summary>
    [Column("generationalgallery")]
    public bool? GenerationalGallery { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Bansauce Arena gym.
    ///     This may feature restricted or banned Pokémon or strategies.
    /// </summary>
    [Column("bansaucearena")]
    public bool? BansauceArena { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Physical Fortress gym.
    ///     This likely focuses on physical attacks rather than special attacks.
    /// </summary>
    [Column("physicalfortress")]
    public bool? PhysicalFortress { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Special Shack gym.
    ///     This likely focuses on special attacks rather than physical attacks.
    /// </summary>
    [Column("specialshack")]
    public bool? SpecialShack { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Reverse Dimension gym.
    ///     This may feature reversed type effectiveness or other special rules.
    /// </summary>
    [Column("reversedimension")]
    public bool? ReverseDimension { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has defeated the Elite Four.
    ///     The Elite Four typically represents the final challenge after defeating all gyms.
    /// </summary>
    [Column("elite4")]
    public bool? Elite4 { get; set; }

    #endregion

    #region Timestamps

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated The Blazing Bowl gym.
    /// </summary>
    [Column("theblazingbowl_ts")]
    public long? TheBlazingBowlTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated The Underwater Arena gym.
    /// </summary>
    [Column("theunderwaterarena_ts")]
    public long? TheUnderwaterArenaTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Field of Flowers gym.
    /// </summary>
    [Column("fieldofflowers_ts")]
    public long? FieldOfFlowersTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Prism Palace gym.
    /// </summary>
    [Column("prismpalace_ts")]
    public long? PrismPalaceTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated The Basic Bungalow gym.
    /// </summary>
    [Column("thebasicbungalow_ts")]
    public long? TheBasicBungalowTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Starter Station gym.
    /// </summary>
    [Column("starterstation_ts")]
    public long? StarterStationTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Floating Island gym.
    /// </summary>
    [Column("floatingisland_ts")]
    public long? FloatingIslandTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Stabless Stadium gym.
    /// </summary>
    [Column("stablessstadium_ts")]
    public long? StablessStadiumTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Triangular Tower gym.
    /// </summary>
    [Column("triangulartower_ts")]
    public long? TriangularTowerTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Monotype Market gym.
    /// </summary>
    [Column("monotypemarket_ts")]
    public long? MonotypeMarketTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Generational Gallery gym.
    /// </summary>
    [Column("generationalgallery_ts")]
    public long? GenerationalGalleryTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Bansauce Arena gym.
    /// </summary>
    [Column("bansaucearena_ts")]
    public long? BansauceArenaTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Physical Fortress gym.
    /// </summary>
    [Column("physicalfortress_ts")]
    public long? PhysicalFortressTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Special Shack gym.
    /// </summary>
    [Column("specialshack_ts")]
    public long? SpecialShackTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Reverse Dimension gym.
    /// </summary>
    [Column("reversedimension_ts")]
    public long? ReverseDimensionTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the user defeated the Elite Four.
    /// </summary>
    [Column("elite4_ts")]
    public long? Elite4Timestamp { get; set; }

    #endregion
}