using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents an active Pokémon in the EeveeCore Pokémon bot system.
///     This class stores comprehensive data about a Pokémon including its stats, moves, and special attributes.
/// </summary>
[Table("pokes")]
public class Pokemon
{
    /// <summary>
    ///     Gets or sets the unique identifier for this Pokémon.
    /// </summary>
    [PrimaryKey]
    [Identity]
    [Column("id")]
    public ulong Id { get; set; }

    #region Basic Info

    /// <summary>
    ///     Gets or sets the species name of the Pokémon.
    /// </summary>
    [Column("pokname")]
    [NotNull]
    public string PokemonName { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the user-given nickname of the Pokémon.
    /// </summary>
    [Column("poknick")]
    [NotNull]
    public string Nickname { get; set; } = null!;

    /// <summary>
    ///     Gets or sets an alternative name or identifier for the Pokémon.
    /// </summary>
    [Column("name")]
    [Nullable]
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets the gender of the Pokémon (e.g., "Male", "Female", "Genderless").
    /// </summary>
    [Column("gender")]
    [NotNull]
    public string Gender { get; set; } = null!;

    #endregion

    #region IVs

    /// <summary>
    ///     Gets or sets the HP Individual Value (IV) of the Pokémon, which influences its maximum HP stat.
    /// </summary>
    [Column("hpiv")]
    [NotNull]
    public int HpIv { get; set; }

    /// <summary>
    ///     Gets or sets the Attack Individual Value (IV) of the Pokémon, which influences its Attack stat.
    /// </summary>
    [Column("atkiv")]
    [NotNull]
    public int AttackIv { get; set; }

    /// <summary>
    ///     Gets or sets the Defense Individual Value (IV) of the Pokémon, which influences its Defense stat.
    /// </summary>
    [Column("defiv")]
    [NotNull]
    public int DefenseIv { get; set; }

    /// <summary>
    ///     Gets or sets the Special Attack Individual Value (IV) of the Pokémon, which influences its Special Attack stat.
    /// </summary>
    [Column("spatkiv")]
    [NotNull]
    public int SpecialAttackIv { get; set; }

    /// <summary>
    ///     Gets or sets the Special Defense Individual Value (IV) of the Pokémon, which influences its Special Defense stat.
    /// </summary>
    [Column("spdefiv")]
    [NotNull]
    public int SpecialDefenseIv { get; set; }

    /// <summary>
    ///     Gets or sets the Speed Individual Value (IV) of the Pokémon, which influences its Speed stat.
    /// </summary>
    [Column("speediv")]
    [NotNull]
    public int SpeedIv { get; set; }

    #endregion

    #region EVs

    /// <summary>
    ///     Gets or sets the HP Effort Value (EV) of the Pokémon, which provides additional HP stat points.
    /// </summary>
    [Column("hpev")]
    [NotNull]
    public int HpEv { get; set; }

    /// <summary>
    ///     Gets or sets the Attack Effort Value (EV) of the Pokémon, which provides additional Attack stat points.
    /// </summary>
    [Column("atkev")]
    [NotNull]
    public int AttackEv { get; set; }

    /// <summary>
    ///     Gets or sets the Defense Effort Value (EV) of the Pokémon, which provides additional Defense stat points.
    /// </summary>
    [Column("defev")]
    [NotNull]
    public int DefenseEv { get; set; }

    /// <summary>
    ///     Gets or sets the Special Attack Effort Value (EV) of the Pokémon, which provides additional Special Attack stat
    ///     points.
    /// </summary>
    [Column("spatkev")]
    [NotNull]
    public int SpecialAttackEv { get; set; }

    /// <summary>
    ///     Gets or sets the Special Defense Effort Value (EV) of the Pokémon, which provides additional Special Defense stat
    ///     points.
    /// </summary>
    [Column("spdefev")]
    [NotNull]
    public int SpecialDefenseEv { get; set; }

    /// <summary>
    ///     Gets or sets the Speed Effort Value (EV) of the Pokémon, which provides additional Speed stat points.
    /// </summary>
    [Column("speedev")]
    [NotNull]
    public int SpeedEv { get; set; }

    #endregion

    #region Battle Info

    /// <summary>
    ///     Gets or sets the experience level of the Pokémon.
    /// </summary>
    [Column("pokelevel")]
    [NotNull]
    public int Level { get; set; }

    /// <summary>
    ///     Gets or sets the array of move names known by the Pokémon.
    /// </summary>
    [Column("moves", DbType = "text[]")]
    [NotNull]
    public string[] Moves { get; set; } = [];

    /// <summary>
    ///     Gets or sets the name of the item held by the Pokémon.
    /// </summary>
    [Column("hitem")]
    [NotNull]
    public string HeldItem { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the current experience points of the Pokémon.
    /// </summary>
    [Column("exp")]
    [NotNull]
    public int Experience { get; set; }

    /// <summary>
    ///     Gets or sets the nature of the Pokémon, which influences stat growth.
    /// </summary>
    [Column("nature")]
    [NotNull]
    public string Nature { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the experience points required to reach the next level.
    /// </summary>
    [Column("expcap")]
    [NotNull]
    public int ExperienceCap { get; set; }

    /// <summary>
    ///     Gets or sets the happiness level of the Pokémon, which may influence evolution or move effectiveness.
    /// </summary>
    [Column("happiness")]
    [NotNull]
    public int Happiness { get; set; }

    /// <summary>
    ///     Gets or sets the index of the ability selected for this Pokémon.
    /// </summary>
    [Column("ability_index")]
    [NotNull]
    public int AbilityIndex { get; set; }

    #endregion

    #region Market Info

    /// <summary>
    ///     Gets or sets the price at which this Pokémon is listed on the market.
    /// </summary>
    [Column("price")]
    [NotNull]
    public int Price { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is currently listed on the market.
    /// </summary>
    [Column("market_enlist")]
    [NotNull]
    public bool MarketEnlist { get; set; }

    #endregion

    #region Special Properties

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is marked as a favorite by its owner.
    /// </summary>
    [Column("fav")]
    [NotNull]
    public bool Favorite { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is shiny.
    ///     Shiny Pokémon have an alternate coloration and are rare.
    /// </summary>
    [Column("shiny")]
    [Nullable]
    public bool? Shiny { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is radiant.
    ///     Radiant may be a special rarity tier beyond shiny.
    /// </summary>
    [Column("radiant")]
    [Nullable]
    public bool? Radiant { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon has champion status.
    ///     Champion Pokémon may have special recognition or achievements.
    /// </summary>
    [Column("champion")]
    [NotNull]
    public bool Champion { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon was obtained using a voucher.
    ///     Voucher Pokémon may have been specially created or selected.
    /// </summary>
    [Column("voucher")]
    [Nullable]
    public bool? Voucher { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    ///     Gets or sets a counter value associated with the Pokémon.
    ///     This may track interactions, battles, or other cumulative events.
    /// </summary>
    [Column("counter")]
    [Nullable]
    public int? Counter { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the Pokémon was caught or created.
    /// </summary>
    [Column("time_stamp")]
    [Nullable]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the player who originally caught or created this Pokémon.
    /// </summary>
    [Column("caught_by")]
    [Nullable]
    public ulong? CaughtBy { get; set; }

    /// <summary>
    ///     Gets or sets the array of tags or labels associated with this Pokémon.
    /// </summary>
    [Column("tags", DbType = "text[]")]
    [NotNull]
    public string[] Tags { get; set; } = [];

    /// <summary>
    ///     Gets or sets the skin or visual variant applied to this Pokémon.
    /// </summary>
    [Column("skin")]
    [Nullable]
    public string? Skin { get; set; }

    #endregion

    #region Flags

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon can be traded to other players.
    /// </summary>
    [Column("tradable")]
    [NotNull]
    public bool Tradable { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon can be used for breeding.
    /// </summary>
    [Column("breedable")]
    [NotNull]
    public bool Breedable { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is temporary.
    ///     Temporary Pokémon may expire or be removed under certain conditions.
    /// </summary>
    [Column("temp")]
    [NotNull]
    public bool Temporary { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the current owner of this Pokémon.
    /// </summary>
    [Column("owner")]
    [Nullable]
    public ulong? Owner { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is owned by a player.
    ///     This may distinguish between player-owned and system-generated Pokémon.
    /// </summary>
    [Column("owned")]
    [Nullable]
    public bool? Owned { get; set; }

    #endregion
}