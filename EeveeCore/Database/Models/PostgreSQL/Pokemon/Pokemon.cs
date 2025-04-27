using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
/// Represents an active Pokémon in the EeveeCore Pokémon bot system.
/// This class stores comprehensive data about a Pokémon including its stats, moves, and special attributes.
/// </summary>
[Table("pokes")]
public class Pokemon
{
    /// <summary>
    /// Gets or sets the unique identifier for this Pokémon.
    /// </summary>
    [Key] [Column("id")] public ulong Id { get; set; }

    /// <summary>
    /// Gets or sets the Ditto ID associated with this Pokémon, used for breeding mechanics.
    /// </summary>
    [Column("ditto_id")] public ulong DittoId { get; set; }

    #region Basic Info

    /// <summary>
    /// Gets or sets the species name of the Pokémon.
    /// </summary>
    [Column("pokname")] [Required] public string? PokemonName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user-given nickname of the Pokémon.
    /// </summary>
    [Column("poknick")] [Required] public string Nickname { get; set; } = null!;

    /// <summary>
    /// Gets or sets an alternative name or identifier for the Pokémon.
    /// </summary>
    [Column("name")] public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the gender of the Pokémon (e.g., "Male", "Female", "Genderless").
    /// </summary>
    [Column("gender")] [Required] public string Gender { get; set; } = null!;

    #endregion

    #region IVs

    /// <summary>
    /// Gets or sets the HP Individual Value (IV) of the Pokémon, which influences its maximum HP stat.
    /// </summary>
    [Column("hpiv")] [Required] public int HpIv { get; set; }

    /// <summary>
    /// Gets or sets the Attack Individual Value (IV) of the Pokémon, which influences its Attack stat.
    /// </summary>
    [Column("atkiv")] [Required] public int AttackIv { get; set; }

    /// <summary>
    /// Gets or sets the Defense Individual Value (IV) of the Pokémon, which influences its Defense stat.
    /// </summary>
    [Column("defiv")] [Required] public int DefenseIv { get; set; }

    /// <summary>
    /// Gets or sets the Special Attack Individual Value (IV) of the Pokémon, which influences its Special Attack stat.
    /// </summary>
    [Column("spatkiv")] [Required] public int SpecialAttackIv { get; set; }

    /// <summary>
    /// Gets or sets the Special Defense Individual Value (IV) of the Pokémon, which influences its Special Defense stat.
    /// </summary>
    [Column("spdefiv")] [Required] public int SpecialDefenseIv { get; set; }

    /// <summary>
    /// Gets or sets the Speed Individual Value (IV) of the Pokémon, which influences its Speed stat.
    /// </summary>
    [Column("speediv")] [Required] public int SpeedIv { get; set; }

    #endregion

    #region EVs

    /// <summary>
    /// Gets or sets the HP Effort Value (EV) of the Pokémon, which provides additional HP stat points.
    /// </summary>
    [Column("hpev")] [Required] public int HpEv { get; set; }

    /// <summary>
    /// Gets or sets the Attack Effort Value (EV) of the Pokémon, which provides additional Attack stat points.
    /// </summary>
    [Column("atkev")] [Required] public int AttackEv { get; set; }

    /// <summary>
    /// Gets or sets the Defense Effort Value (EV) of the Pokémon, which provides additional Defense stat points.
    /// </summary>
    [Column("defev")] [Required] public int DefenseEv { get; set; }

    /// <summary>
    /// Gets or sets the Special Attack Effort Value (EV) of the Pokémon, which provides additional Special Attack stat points.
    /// </summary>
    [Column("spatkev")] [Required] public int SpecialAttackEv { get; set; }

    /// <summary>
    /// Gets or sets the Special Defense Effort Value (EV) of the Pokémon, which provides additional Special Defense stat points.
    /// </summary>
    [Column("spdefev")] [Required] public int SpecialDefenseEv { get; set; }

    /// <summary>
    /// Gets or sets the Speed Effort Value (EV) of the Pokémon, which provides additional Speed stat points.
    /// </summary>
    [Column("speedev")] [Required] public int SpeedEv { get; set; }

    #endregion

    #region Battle Info

    /// <summary>
    /// Gets or sets the experience level of the Pokémon.
    /// </summary>
    [Column("pokelevel")] [Required] public int Level { get; set; }

    /// <summary>
    /// Gets or sets the array of move names known by the Pokémon.
    /// </summary>
    [Column("moves", TypeName = "text[]")]
    [Required]
    public string[] Moves { get; set; } = [];

    /// <summary>
    /// Gets or sets the name of the item held by the Pokémon.
    /// </summary>
    [Column("hitem")] [Required] public string HeldItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current experience points of the Pokémon.
    /// </summary>
    [Column("exp")] [Required] public int Experience { get; set; }

    /// <summary>
    /// Gets or sets the nature of the Pokémon, which influences stat growth.
    /// </summary>
    [Column("nature")] [Required] public string Nature { get; set; } = null!;

    /// <summary>
    /// Gets or sets the experience points required to reach the next level.
    /// </summary>
    [Column("expcap")] [Required] public int ExperienceCap { get; set; }

    /// <summary>
    /// Gets or sets the happiness level of the Pokémon, which may influence evolution or move effectiveness.
    /// </summary>
    [Column("happiness")] [Required] public int Happiness { get; set; }

    /// <summary>
    /// Gets or sets the index of the ability selected for this Pokémon.
    /// </summary>
    [Column("ability_index")] [Required] public int AbilityIndex { get; set; }

    #endregion

    #region Market Info

    /// <summary>
    /// Gets or sets the price at which this Pokémon is listed on the market.
    /// </summary>
    [Column("price")] [Required] public int Price { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon is currently listed on the market.
    /// </summary>
    [Column("market_enlist")] [Required] public bool MarketEnlist { get; set; }

    #endregion

    #region Special Properties

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon is marked as a favorite by its owner.
    /// </summary>
    [Column("fav")] [Required] public bool Favorite { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon is shiny.
    /// Shiny Pokémon have an alternate coloration and are rare.
    /// </summary>
    [Column("shiny")] public bool? Shiny { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon is radiant.
    /// Radiant may be a special rarity tier beyond shiny.
    /// </summary>
    [Column("radiant")] public bool? Radiant { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon has champion status.
    /// Champion Pokémon may have special recognition or achievements.
    /// </summary>
    [Column("champion")] [Required] public bool Champion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon was obtained using a voucher.
    /// Voucher Pokémon may have been specially created or selected.
    /// </summary>
    [Column("voucher")] public bool? Voucher { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// Gets or sets a counter value associated with the Pokémon.
    /// This may track interactions, battles, or other cumulative events.
    /// </summary>
    [Column("counter")] public int? Counter { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the Pokémon was caught or created.
    /// </summary>
    [Column("time_stamp")] public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the Discord user ID of the player who originally caught or created this Pokémon.
    /// </summary>
    [Column("caught_by")] public ulong? CaughtBy { get; set; }

    /// <summary>
    /// Gets or sets the array of tags or labels associated with this Pokémon.
    /// </summary>
    [Column("tags", TypeName = "text[]")]
    [Required]
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the skin or visual variant applied to this Pokémon.
    /// </summary>
    [Column("skin")] public string? Skin { get; set; }

    #endregion

    #region Flags

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon can be traded to other players.
    /// </summary>
    [Column("tradable")] [Required] public bool Tradable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon can be used for breeding.
    /// </summary>
    [Column("breedable")] [Required] public bool Breedable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon is temporary.
    /// Temporary Pokémon may expire or be removed under certain conditions.
    /// </summary>
    [Column("temp")] [Required] public bool Temporary { get; set; }

    /// <summary>
    /// Gets or sets the Discord user ID of the current owner of this Pokémon.
    /// </summary>
    [Column("owner")] public ulong? Owner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this Pokémon is owned by a player.
    /// This may distinguish between player-owned and system-generated Pokémon.
    /// </summary>
    [Column("owned")] public bool? Owned { get; set; }

    #endregion
}