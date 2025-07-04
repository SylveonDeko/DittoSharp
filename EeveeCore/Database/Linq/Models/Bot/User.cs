using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Common;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a user in the EeveeCore Pokémon bot system.
///     This class serves as the primary user profile containing all user data, resources, Pokémon collection,
///     progress, settings, and game state.
/// </summary>
[Table(Name = "users")]
public class User
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this user (primary key).
    /// </summary>
    [PrimaryKey]
    [Column(Name = "u_id")]
    public ulong? UserId { get; set; }


    /// <summary>
    ///     Gets or sets an administrative note about this user.
    /// </summary>
    [Column(Name = "note")]
    public string? Note { get; set; }

    #region Resources

    /// <summary>
    ///     Gets or sets the number of redeems the user has.
    ///     Redeems allow users to obtain specific Pokémon.
    /// </summary>
    [Column(Name = "redeems")]
    public ulong? Redeems { get; set; }

    /// <summary>
    ///     Gets or sets the number of Evolution Points (EvPoints) the user has.
    ///     Evolution Points can be used to evolve Pokémon that require special conditions.
    /// </summary>
    [Column(Name = "evpoints")]
    public int? EvPoints { get; set; }

    /// <summary>
    ///     Gets or sets the number of upvote points the user has earned through supporting the bot.
    /// </summary>
    [Column(Name = "upvotepoints")]
    public int? UpvotePoints { get; set; }

    /// <summary>
    ///     Gets or sets the amount of MewCoins (in-game currency) the user possesses.
    /// </summary>
    [Column(Name = "mewcoins")]
    public ulong? MewCoins { get; set; }

    /// <summary>
    ///     Gets or sets the amount of slime resource the user has collected.
    /// </summary>
    [Column(Name = "slime")]
    public int? Slime { get; set; }

    /// <summary>
    ///     Gets or sets the amount of crystal slime resource the user has collected.
    /// </summary>
    [Column(Name = "crystal_slime")]
    public int? CrystalSlime { get; set; }

    /// <summary>
    ///     Gets or sets the number of skin tokens the user has.
    ///     Skin tokens can be used to purchase special visual variants of Pokémon.
    /// </summary>
    [Column(Name = "skin_tokens")]
    [NotNull]
    public int SkinTokens { get; set; }

    /// <summary>
    ///     Gets or sets the number of event-specific skin tokens the user has.
    ///     These can only be used for event-specific Pokémon skins.
    /// </summary>
    [Column(Name = "event_skin_tokens")]
    public int? EventSkinTokens { get; set; }

    /// <summary>
    ///     Gets or sets the number of VIP tokens the user has.
    ///     VIP tokens may provide access to special features or Pokémon.
    /// </summary>
    [Column(Name = "vip_tokens")]
    [NotNull]
    public int VipTokens { get; set; }

    /// <summary>
    ///     Gets or sets the number of mystery tokens the user has.
    ///     Mystery tokens can be used for special rewards or features.
    /// </summary>
    [Column(Name = "mystery_token")]
    public int? MysteryToken { get; set; }

    /// <summary>
    ///     Gets or sets the number of vouchers the user possesses.
    /// </summary>
    [Column(Name = "voucher")]
    public int? Voucher { get; set; }

    /// <summary>
    ///     Gets or sets the user's reputation in the OS (Operating System) community.
    /// </summary>
    [Column(Name = "os_rep")]
    [NotNull]
    public int OsRep { get; set; }

    #endregion

    #region Pokemon Management

    /// <summary>
    ///     Gets or sets the ID of the currently selected Pokémon.
    /// </summary>
    [Column(Name = "selected")]
    public ulong? Selected { get; set; }

    /// <summary>
    ///     Internal storage for party as long[] for PostgreSQL compatibility
    /// </summary>
    [Column(Name = "party", DbType = "numeric(20,0)[]")]
    internal long[] _party { get; set; } = [0, 0, 0, 0, 0, 0];

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs in the user's active party.
    ///     The party consists of up to 6 Pokémon used for battles and other activities.
    /// </summary>
    [NotColumn]
    public ulong[]? Party 
    { 
        get => _party?.Select(x => (ulong)x).ToArray() ?? [0, 0, 0, 0, 0, 0];
        set => _party = value?.Select(x => (long)x).ToArray() ?? [0, 0, 0, 0, 0, 0];
    }

    /// <summary>
    ///     Gets or sets the number of Pokémon currently in the user's daycare.
    /// </summary>
    [Column(Name = "daycare")]
    public int? Daycare { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of Pokémon the user can place in daycare.
    /// </summary>
    [Column(Name = "daycarelimit")]
    public int? DaycareLimit { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the array of female Pokémon indices in the user's collection.
    /// </summary>
    [Column(Name = "females", DbType = "integer[]")]
    public int?[]? Females { get; set; }

    #endregion

    #region User Settings

    /// <summary>
    ///     Gets or sets the trainer nickname for the user.
    /// </summary>
    [Column(Name = "tnick")]
    public string? TrainerNickname { get; set; }

    /// <summary>
    ///     Gets or sets the user's preferred ordering for Pokémon display.
    /// </summary>
    [Column(Name = "user_order")]
    public string? UserOrder { get; set; } = "kek";

    /// <summary>
    ///     Gets or sets the user's current game region.
    ///     Different regions may contain different Pokémon and features.
    /// </summary>
    [Column(Name = "region")]
    [NotNull]
    public string Region { get; set; } = "original";

    /// <summary>
    ///     Gets or sets the user's inventory as a JSON string.
    ///     Contains various items the user has collected throughout gameplay.
    /// </summary>
    [Column(Name = "inventory", DbType = "jsonb")]
    public string? Inventory { get; set; } = "{}";

    /// <summary>
    ///     Gets or sets the user's equipped items as a JSON string.
    /// </summary>
    [Column(Name = "items", DbType = "jsonb")]
    public string? Items { get; set; } = "{}";

    /// <summary>
    ///     Gets or sets the held item for the user's currently selected Pokémon.
    /// </summary>
    [Column(Name = "held_item")]
    public string? HeldItem { get; set; } = "None";

    /// <summary>
    ///     Gets or sets the user's collection of Pokémon skins as a JSON string.
    /// </summary>
    [Column(Name = "skins", DbType = "jsonb")]
    [NotNull]
    public string Skins { get; set; } = "{}";

    /// <summary>
    ///     Gets or sets the user's holiday-specific inventory as a JSON string.
    ///     Contains items from seasonal events and holiday promotions.
    /// </summary>
    [Column(Name = "holidayinv", DbType = "jsonb")]
    public string? HolidayInventory { get; set; } = "{}";

    #endregion

    #region Progress and Stats

    /// <summary>
    ///     Gets or sets the user's fishing experience points.
    /// </summary>
    [Column(Name = "fishing_exp")]
    public ulong? FishingExp { get; set; }

    /// <summary>
    ///     Gets or sets the user's fishing skill level.
    /// </summary>
    [Column(Name = "fishing_level")]
    public ulong? FishingLevel { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the maximum level cap for fishing.
    /// </summary>
    [Column(Name = "fishing_level_cap")]
    public ulong? FishingLevelCap { get; set; } = 50;

    /// <summary>
    ///     Gets or sets the user's trainer level.
    /// </summary>
    [Column(Name = "level")]
    public int? Level { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the user's current experience points toward the next level.
    /// </summary>
    [Column(Name = "current_xp")]
    public int? CurrentXp { get; set; }

    /// <summary>
    ///     Gets or sets the user's current encounter chain length.
    ///     Longer chains may increase the chances of finding rare or shiny Pokémon.
    /// </summary>
    [Column(Name = "chain")]
    [NotNull]
    public int Chain { get; set; }

    /// <summary>
    ///     Gets or sets the Pokémon the user is currently hunting.
    /// </summary>
    [Column(Name = "hunt")]
    public string? Hunt { get; set; } = "";

    #endregion

    #region Status and Flags

    /// <summary>
    ///     Gets or sets the user's current energy level.
    ///     Energy is consumed when performing various actions in the game.
    /// </summary>
    [Column(Name = "energy")]
    public int? Energy { get; set; } = 10;

    /// <summary>
    ///     Gets or sets the energy points restored per hour.
    /// </summary>
    [Column(Name = "energy_hour")]
    public int? EnergyHour { get; set; }

    /// <summary>
    ///     Gets or sets the user's luck value.
    ///     Luck may influence various random elements in the game such as encounter rates or successful captures.
    /// </summary>
    [Column(Name = "luck")]
    public int? Luck { get; set; } = 1;

    /// <summary>
    ///     Gets or sets a value indicating whether the user's profile is visible to others.
    /// </summary>
    [Column(Name = "visible")]
    public bool? Visible { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the user is silenced.
    ///     Silenced users may have limited communication abilities.
    /// </summary>
    [Column(Name = "silenced")]
    [NotNull]
    public bool Silenced { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is prevented from trading.
    /// </summary>
    [Column(Name = "tradelock")]
    public bool? TradeLock { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has voted for the bot recently.
    /// </summary>
    [Column(Name = "voted")]
    public bool? Voted { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user's data has been restored from a backup.
    /// </summary>
    [Column(Name = "restored")]
    public bool? Restored { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user owns a bike.
    ///     Bikes may provide advantages such as faster movement or access to specific areas.
    /// </summary>
    [Column(Name = "bike")]
    public bool? Bike { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is banned from using the bot.
    /// </summary>
    [Column(Name = "botbanned")]
    public bool? BotBanned { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is a Gym Leader in the game.
    /// </summary>
    [Column(Name = "gym_leader")]
    public bool? GymLeader { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user's data has been converted from a previous system.
    /// </summary>
    [Column(Name = "oxiconverted")]
    [NotNull]
    public bool OxiConverted { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has unlocked the ancient region or content.
    /// </summary>
    [Column(Name = "ancient_unlocked")]
    public bool? AncientUnlocked { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is currently active in the system.
    /// </summary>
    [Column(Name = "active")]
    [NotNull]
    public bool Active { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is participating in competitions.
    /// </summary>
    [Column(Name = "comp")]
    public bool? Comp { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to display the user's donations publicly.
    /// </summary>
    [Column(Name = "show_donations")]
    public bool? ShowDonations { get; set; }

    #endregion

    #region Trading and Market

    /// <summary>
    ///     Gets or sets the maximum number of Pokémon the user can have on the market at once.
    /// </summary>
    [Column(Name = "marketlimit")]
    [NotNull]
    public int MarketLimit { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the timestamp of the user's last vote for the bot.
    /// </summary>
    [Column(Name = "last_vote")]
    [NotNull]
    public int LastVote { get; set; }

    /// <summary>
    ///     Gets or sets the user's current vote streak.
    ///     Consecutive votes may provide increasing rewards.
    /// </summary>
    [Column(Name = "vote_streak")]
    [NotNull]
    public int VoteStreak { get; set; }

    #endregion

    #region Relationships and Progress

    /// <summary>
    ///     Gets or sets the user's relationships with NPCs in the game as a JSON string.
    ///     These relationships may influence interactions, quests, or rewards.
    /// </summary>
    [Column(Name = "npc_relationships", DbType = "jsonb")]
    public string? NpcRelationships { get; set; } = "{}";

    /// <summary>
    ///     Gets or sets the user's tutorial and starting progress as a JSON string.
    ///     Tracks completion of tutorial missions and early-game objectives.
    /// </summary>
    [Column(Name = "start_progress", DbType = "jsonb")]
    [NotNull]
    public string StartProgress { get; set; } =
        "{\"Tutorial Mission\": {\"Stage1\": false, \"Stage2\": false, \"Stage3\": false, \"Stage4\": false}}";

    #endregion

    #region Staff and Event

    /// <summary>
    ///     Gets or sets the user's staff role or permission level in the system.
    /// </summary>
    [Column(Name = "staff")]
    [NotNull]
    public string Staff { get; set; } = "User";

    /// <summary>
    ///     Gets or sets the user's participation status in the current event.
    /// </summary>
    [Column(Name = "event")]
    public int? Event { get; set; }

    /// <summary>
    ///     Gets or sets the number of raffle tickets the user has.
    /// </summary>
    [Column(Name = "raffle")]
    public int? Raffle { get; set; }

    #endregion

    #region Titles and Tokens

    /// <summary>
    ///     Gets or sets the array of titles the user has earned.
    ///     Titles are achievements or status indicators displayed with the user's name.
    /// </summary>
    [Column(Name = "titles", DbType = "text[]")]
    public string[]? Titles { get; set; } = ["Newcomer"];

    /// <summary>
    ///     Gets or sets the user's currently selected display title.
    /// </summary>
    [Column(Name = "selected_title")]
    public string? SelectedTitle { get; set; } = "";

    /// <summary>
    ///     Internal storage for type tokens as long[] for PostgreSQL compatibility
    /// </summary>
    [Column(Name = "type_tokens", DbType = "numeric(20,0)[]")]
    internal long[] _typeTokens { get; set; } = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    /// <summary>
    ///     Gets or sets the array of type tokens the user has collected.
    ///     Each index corresponds to a Pokémon type (Normal, Fire, Water, etc.).
    /// </summary>
    [NotColumn]
    public ulong[]? TypeTokens 
    { 
        get => _typeTokens?.Select(x => (ulong)x).ToArray() ?? [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        set => _typeTokens = value?.Select(x => (long)x).ToArray() ?? [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    }

    /// <summary>
    ///     Gets or sets the user's type tokens as a JSON string, mapping type names to token counts.
    /// </summary>
    [Column(Name = "tokens", DbType = "jsonb")]
    public string? Tokens { get; set; } =
        "{\"Bug\": 0, \"Ice\": 0, \"Dark\": 0, \"Fire\": 0, \"Rock\": 0, \"Fairy\": 0, \"Ghost\": 0, \"Grass\": 0, \"Steel\": 0, \"Water\": 0, \"Dragon\": 0, \"Flying\": 0, \"Ground\": 0, \"Normal\": 0, \"Poison\": 0, \"Psychic\": 0, \"Electric\": 0, \"Fighting\": 0}";

    #endregion

    #region Alt and Patreon

    /// <summary>
    ///     Internal storage for alternate account IDs as long[] for PostgreSQL compatibility
    /// </summary>
    [Column(Name = "alt", DbType = "numeric(20,0)[]")]
    internal long[]? _alt { get; set; }

    /// <summary>
    ///     Gets or sets the array of alternate account IDs associated with this user.
    /// </summary>
    [NotColumn]
    public ulong[]? Alt 
    { 
        get => _alt?.Select(x => (ulong)x).ToArray();
        set => _alt = value?.Select(x => (long)x).ToArray();
    }

    /// <summary>
    ///     Gets or sets the user's Patreon subscription tier or status.
    /// </summary>
    [Column(Name = "patreon")]
    public string? Patreon { get; set; } = "None";

    /// <summary>
    ///     Gets or sets a manual override for the user's Patreon status or tier.
    /// </summary>
    [Column(Name = "patreon_override")]
    public string? PatreonOverride { get; set; }

    #endregion
}