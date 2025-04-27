using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
/// Represents a user in the EeveeCore Pokémon bot system.
/// This class serves as the primary user profile containing all user data, resources, Pokémon collection,
/// progress, settings, and game state.
/// </summary>
[Table("users")]
public class User
{
    /// <summary>
    /// Gets or sets the unique database identifier for this user record.
    /// </summary>
    [Key] [Column("id")] public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Discord user ID associated with this user.
    /// </summary>
    [Column("u_id")] public ulong? UserId { get; set; }

    /// <summary>
    /// Gets or sets an administrative note about this user.
    /// </summary>
    [Column("note")] public string? Note { get; set; }

    #region Resources

    /// <summary>
    /// Gets or sets the number of redeems the user has.
    /// Redeems allow users to obtain specific Pokémon.
    /// </summary>
    [Column("redeems")] public int? Redeems { get; set; }

    /// <summary>
    /// Gets or sets the number of Evolution Points (EvPoints) the user has.
    /// Evolution Points can be used to evolve Pokémon that require special conditions.
    /// </summary>
    [Column("evpoints")] public int? EvPoints { get; set; }

    /// <summary>
    /// Gets or sets the number of upvote points the user has earned through supporting the bot.
    /// </summary>
    [Column("upvotepoints")] public int? UpvotePoints { get; set; }

    /// <summary>
    /// Gets or sets the amount of MewCoins (in-game currency) the user possesses.
    /// </summary>
    [Column("mewcoins")] public ulong? MewCoins { get; set; }

    /// <summary>
    /// Gets or sets the amount of slime resource the user has collected.
    /// </summary>
    [Column("slime")] public int? Slime { get; set; }

    /// <summary>
    /// Gets or sets the amount of crystal slime resource the user has collected.
    /// </summary>
    [Column("crystal_slime")] public int? CrystalSlime { get; set; }

    /// <summary>
    /// Gets or sets the number of skin tokens the user has.
    /// Skin tokens can be used to purchase special visual variants of Pokémon.
    /// </summary>
    [Column("skin_tokens")] [Required] public int SkinTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of event-specific skin tokens the user has.
    /// These can only be used for event-specific Pokémon skins.
    /// </summary>
    [Column("event_skin_tokens")] public int? EventSkinTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of VIP tokens the user has.
    /// VIP tokens may provide access to special features or Pokémon.
    /// </summary>
    [Column("vip_tokens")] [Required] public int VipTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of mystery tokens the user has.
    /// Mystery tokens can be used for special rewards or features.
    /// </summary>
    [Column("mystery_token")] public int? MysteryToken { get; set; }

    /// <summary>
    /// Gets or sets the number of vouchers the user possesses.
    /// </summary>
    [Column("voucher")] public int? Voucher { get; set; }

    /// <summary>
    /// Gets or sets the user's reputation in the OS (Operating System) community.
    /// </summary>
    [Column("os_rep")] [Required] public int OsRep { get; set; }

    #endregion

    #region Pokemon Management

    /// <summary>
    /// Gets or sets the array of Pokémon IDs that the user owns.
    /// </summary>
    [Column("pokes", TypeName = "bigint[]")]
    public ulong[]? Pokemon { get; set; }

    /// <summary>
    /// Gets or sets the ID of the currently selected Pokémon.
    /// </summary>
    [Column("selected")] public ulong? Selected { get; set; }

    /// <summary>
    /// Gets or sets the array of Pokémon IDs in the user's active party.
    /// The party consists of up to 6 Pokémon used for battles and other activities.
    /// </summary>
    [Column("party", TypeName = "bigint[]")]
    public ulong[]? Party { get; set; } = [0, 0, 0, 0, 0, 0];

    /// <summary>
    /// Gets or sets the number of Pokémon currently in the user's daycare.
    /// </summary>
    [Column("daycare")] public int? Daycare { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of Pokémon the user can place in daycare.
    /// </summary>
    [Column("daycarelimit")] public int? DaycareLimit { get; set; } = 1;

    /// <summary>
    /// Gets or sets the array of female Pokémon indices in the user's collection.
    /// </summary>
    [Column("females", TypeName = "integer[]")]
    public int?[]? Females { get; set; }

    #endregion

    #region User Settings

    /// <summary>
    /// Gets or sets the trainer nickname for the user.
    /// </summary>
    [Column("tnick")] public string? TrainerNickname { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred ordering for Pokémon display.
    /// </summary>
    [Column("user_order")] public string? UserOrder { get; set; } = "kek";

    /// <summary>
    /// Gets or sets the user's current game region.
    /// Different regions may contain different Pokémon and features.
    /// </summary>
    [Column("region")] [Required] public string Region { get; set; } = "original";

    /// <summary>
    /// Gets or sets the user's inventory as a JSON string.
    /// Contains various items the user has collected throughout gameplay.
    /// </summary>
    [Column("inventory", TypeName = "jsonb")]
    public string? Inventory { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the user's equipped items as a JSON string.
    /// </summary>
    [Column("items", TypeName = "jsonb")] public string? Items { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the held item for the user's currently selected Pokémon.
    /// </summary>
    [Column("held_item")] public string? HeldItem { get; set; } = "None";

    /// <summary>
    /// Gets or sets the user's collection of Pokémon skins as a JSON string.
    /// </summary>
    [Column("skins", TypeName = "jsonb")]
    [Required]
    public string Skins { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the user's holiday-specific inventory as a JSON string.
    /// Contains items from seasonal events and holiday promotions.
    /// </summary>
    [Column("holidayinv", TypeName = "jsonb")]
    public string? HolidayInventory { get; set; } = "{}";

    #endregion

    #region Progress and Stats

    /// <summary>
    /// Gets or sets the user's fishing experience points.
    /// </summary>
    [Column("fishing_exp")] public ulong? FishingExp { get; set; }

    /// <summary>
    /// Gets or sets the user's fishing skill level.
    /// </summary>
    [Column("fishing_level")] public int? FishingLevel { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum level cap for fishing.
    /// </summary>
    [Column("fishing_level_cap")] public ulong? FishingLevelCap { get; set; } = 50;

    /// <summary>
    /// Gets or sets the user's trainer level.
    /// </summary>
    [Column("level")] public int? Level { get; set; } = 1;

    /// <summary>
    /// Gets or sets the user's current experience points toward the next level.
    /// </summary>
    [Column("current_xp")] public int? CurrentXp { get; set; }

    /// <summary>
    /// Gets or sets the user's current encounter chain length.
    /// Longer chains may increase the chances of finding rare or shiny Pokémon.
    /// </summary>
    [Column("chain")] [Required] public int Chain { get; set; }

    /// <summary>
    /// Gets or sets the Pokémon the user is currently hunting.
    /// </summary>
    [Column("hunt")] public string? Hunt { get; set; } = "";

    #endregion

    #region Status and Flags

    /// <summary>
    /// Gets or sets the user's current energy level.
    /// Energy is consumed when performing various actions in the game.
    /// </summary>
    [Column("energy")] public int? Energy { get; set; } = 10;

    /// <summary>
    /// Gets or sets the energy points restored per hour.
    /// </summary>
    [Column("energy_hour")] public int? EnergyHour { get; set; }

    /// <summary>
    /// Gets or sets the user's luck value.
    /// Luck may influence various random elements in the game such as encounter rates or successful captures.
    /// </summary>
    [Column("luck")] public int? Luck { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether the user's profile is visible to others.
    /// </summary>
    [Column("visible")] public bool? Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the user is silenced.
    /// Silenced users may have limited communication abilities.
    /// </summary>
    [Column("silenced")] [Required] public bool Silenced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is prevented from trading.
    /// </summary>
    [Column("tradelock")] public bool? TradeLock { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has voted for the bot recently.
    /// </summary>
    [Column("voted")] public bool? Voted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user's data has been restored from a backup.
    /// </summary>
    [Column("restored")] public bool? Restored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user owns a bike.
    /// Bikes may provide advantages such as faster movement or access to specific areas.
    /// </summary>
    [Column("bike")] public bool? Bike { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is banned from using the bot.
    /// </summary>
    [Column("botbanned")] public bool? BotBanned { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is a Gym Leader in the game.
    /// </summary>
    [Column("gym_leader")] public bool? GymLeader { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user's data has been converted from a previous system.
    /// </summary>
    [Column("oxiconverted")] [Required] public bool OxiConverted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has unlocked the ancient region or content.
    /// </summary>
    [Column("ancient_unlocked")] public bool? AncientUnlocked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is currently active in the system.
    /// </summary>
    [Column("active")] [Required] public bool Active { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is participating in competitions.
    /// </summary>
    [Column("comp")] public bool? Comp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display the user's donations publicly.
    /// </summary>
    [Column("show_donations")] public bool? ShowDonations { get; set; }

    #endregion

    #region Trading and Market

    /// <summary>
    /// Gets or sets the maximum number of Pokémon the user can have on the market at once.
    /// </summary>
    [Column("marketlimit")] [Required] public int MarketLimit { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timestamp of the user's last vote for the bot.
    /// </summary>
    [Column("last_vote")] [Required] public int LastVote { get; set; }

    /// <summary>
    /// Gets or sets the user's current vote streak.
    /// Consecutive votes may provide increasing rewards.
    /// </summary>
    [Column("vote_streak")] [Required] public int VoteStreak { get; set; }

    #endregion

    #region Relationships and Progress

    /// <summary>
    /// Gets or sets the user's relationships with NPCs in the game as a JSON string.
    /// These relationships may influence interactions, quests, or rewards.
    /// </summary>
    [Column("npc_relationships", TypeName = "jsonb")]
    public string? NpcRelationships { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the user's tutorial and starting progress as a JSON string.
    /// Tracks completion of tutorial missions and early-game objectives.
    /// </summary>
    [Column("start_progress", TypeName = "jsonb")]
    [Required]
    public string StartProgress { get; set; } =
        "{\"Tutorial Mission\": {\"Stage1\": false, \"Stage2\": false, \"Stage3\": false, \"Stage4\": false}}";

    #endregion

    #region Staff and Event

    /// <summary>
    /// Gets or sets the user's staff role or permission level in the system.
    /// </summary>
    [Column("staff")] [Required] public string Staff { get; set; } = "User";

    /// <summary>
    /// Gets or sets the user's participation status in the current event.
    /// </summary>
    [Column("event")] public int? Event { get; set; }

    /// <summary>
    /// Gets or sets the number of raffle tickets the user has.
    /// </summary>
    [Column("raffle")] public int? Raffle { get; set; }

    #endregion

    #region Titles and Tokens

    /// <summary>
    /// Gets or sets the array of titles the user has earned.
    /// Titles are achievements or status indicators displayed with the user's name.
    /// </summary>
    [Column("titles", TypeName = "text[]")]
    public string[]? Titles { get; set; } = ["Newcomer"];

    /// <summary>
    /// Gets or sets the user's currently selected display title.
    /// </summary>
    [Column("selected_title")] public string? SelectedTitle { get; set; } = "";

    /// <summary>
    /// Gets or sets the array of type tokens the user has collected.
    /// Each index corresponds to a Pokémon type (Normal, Fire, Water, etc.).
    /// </summary>
    [Column("type_tokens", TypeName = "integer[]")]
    public int[]? TypeTokens { get; set; } = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    /// <summary>
    /// Gets or sets the user's type tokens as a JSON string, mapping type names to token counts.
    /// </summary>
    [Column("tokens", TypeName = "jsonb")]
    public string? Tokens { get; set; } =
        "{\"Bug\": 0, \"Ice\": 0, \"Dark\": 0, \"Fire\": 0, \"Rock\": 0, \"Fairy\": 0, \"Ghost\": 0, \"Grass\": 0, \"Steel\": 0, \"Water\": 0, \"Dragon\": 0, \"Flying\": 0, \"Ground\": 0, \"Normal\": 0, \"Poison\": 0, \"Psychic\": 0, \"Electric\": 0, \"Fighting\": 0}";

    #endregion

    #region Alt and Patreon

    /// <summary>
    /// Gets or sets the array of alternate account IDs associated with this user.
    /// </summary>
    [Column("alt", TypeName = "bigint[]")] public long[]? Alt { get; set; }

    /// <summary>
    /// Gets or sets the user's Patreon subscription tier or status.
    /// </summary>
    [Column("patreon")] public string? Patreon { get; set; } = "None";

    /// <summary>
    /// Gets or sets a manual override for the user's Patreon status or tier.
    /// </summary>
    [Column("patreon_override")] public string? PatreonOverride { get; set; }

    #endregion
}