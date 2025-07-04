using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents a Pokémon that has been removed from a user's active collection in the EeveeCore Pokémon bot system.
///     This class stores comprehensive data about inactive Pokémon, preserving their attributes for historical or recovery
///     purposes.
/// </summary>
[Table("dead_pokes")]
public class DeadPokemon
{
    /// <summary>
    ///     Gets or sets the unique identifier for this inactive Pokémon.
    /// </summary>
    [PrimaryKey]
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
    ///     Gets or sets the price at which this Pokémon was last listed on the market.
    /// </summary>
    [Column("price")]
    [NotNull]
    public int Price { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon was listed on the market when it became inactive.
    /// </summary>
    [Column("market_enlist")]
    [NotNull]
    public bool MarketEnlist { get; set; }

    #endregion

    #region Special Properties

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon was marked as a favorite by its owner.
    /// </summary>
    [Column("fav")]
    [NotNull]
    public bool Favorite { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is shiny.
    ///     Shiny Pokémon have an alternate coloration and are rare.
    /// </summary>
    [Column("shiny")]
    public bool? Shiny { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is radiant.
    ///     Radiant may be a special rarity tier beyond shiny.
    /// </summary>
    [Column("radiant")]
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
    public bool? Voucher { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    ///     Gets or sets a counter value associated with the Pokémon.
    ///     This may track interactions, battles, or other cumulative events.
    /// </summary>
    [Column("counter")]
    public int? Counter { get; set; }

    /// <summary>
    ///     Gets or sets the date and time when the Pokémon was caught or created.
    /// </summary>
    [Column("time_stamp")]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the player who originally caught or created this Pokémon.
    /// </summary>
    [Column("caught_by")]
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
    public string? Skin { get; set; }

    #endregion

    #region Flags

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon was tradable when active.
    /// </summary>
    [Column("tradable")]
    [NotNull]
    public bool Tradable { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon was eligible for breeding when active.
    /// </summary>
    [Column("breedable")]
    [NotNull]
    public bool Breedable { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon was temporary.
    ///     Temporary Pokémon may have been part of a special event or trial.
    /// </summary>
    [Column("temp")]
    [NotNull]
    public bool Temporary { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the last owner of this Pokémon.
    /// </summary>
    [Column("owner")]
    public ulong Owner { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon was owned by a player.
    ///     This may distinguish between player-owned and system-generated Pokémon.
    /// </summary>
    [Column("owned")]
    public bool? Owned { get; set; }

    #endregion
}