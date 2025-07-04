using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents an active user in the EeveeCore Pokémon bot system.
///     This class stores comprehensive user data including inventory, Pokémon, progress, and various game-related
///     statistics.
/// </summary>
[Table("users_active")]
public class ActiveUser
{
    /// <summary>
    ///     Gets or sets the unique identifier for this user record in the database.
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int? Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the active user.
    /// </summary>
    [Column("u_id")]
    public ulong? UserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeems the user has.
    ///     Redeems can be used to obtain specific Pokémon.
    /// </summary>
    [Column("redeems")]
    public int? Redeems { get; set; }

    /// <summary>
    ///     Gets or sets the number of Evolution Points (EVP) the user has.
    ///     Evolution Points can be used to evolve Pokémon that require special conditions.
    /// </summary>
    [Column("evpoints")]
    public int? EvPoints { get; set; }

    /// <summary>
    ///     Gets or sets the user's preferred trainer nickname in the game.
    /// </summary>
    [Column("tnick")]
    public string? TrainerNickname { get; set; }

    /// <summary>
    ///     Gets or sets the number of upvote points the user has earned.
    ///     These points are typically earned through supporting the bot on listing sites.
    /// </summary>
    [Column("upvotepoints")]
    public int? UpvotePoints { get; set; }

    /// <summary>
    ///     Gets or sets the number of MewCoins (in-game currency) the user possesses.
    ///     MewCoins are used for various in-game purchases and transactions.
    /// </summary>
    [Column("mewcoins")]
    public long? MewCoins { get; set; }

    /// <summary>
    ///     Gets or sets the user's preferred order for displaying Pokémon.
    /// </summary>
    [Column("user_order")]
    public string? UserOrder { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs that the user owns.
    ///     Each ID corresponds to a specific Pokémon in the user's collection.
    /// </summary>
    [Column("pokes")]
    public ulong[]? Pokemon { get; set; }

    /// <summary>
    ///     Gets or sets the user's inventory as a JSON string.
    ///     Contains various items the user has collected throughout gameplay.
    /// </summary>
    [Column("inventory")]
    public string? Inventory { get; set; }

    /// <summary>
    ///     Gets or sets the user's items as a JSON string.
    ///     This may represent specialized or equipped items separate from the general inventory.
    /// </summary>
    [Column("items")]
    public string? Items { get; set; }

    /// <summary>
    ///     Gets or sets the number of Pokémon currently in the user's daycare.
    /// </summary>
    [Column("daycare")]
    public int? Daycare { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of Pokémon the user can place in daycare.
    /// </summary>
    [Column("daycarelimit")]
    public int? DaycareLimit { get; set; }

    /// <summary>
    ///     Gets or sets the user's current energy level.
    ///     Energy is consumed when performing various actions in the game.
    /// </summary>
    [Column("energy")]
    public int? Energy { get; set; }

    /// <summary>
    ///     Gets or sets the held item for the user's currently selected Pokémon.
    /// </summary>
    [Column("held_item")]
    public string? HeldItem { get; set; }

    /// <summary>
    ///     Gets or sets the user's fishing experience points.
    ///     Fishing is an activity that can yield Pokémon and items.
    /// </summary>
    [Column("fishing_exp")]
    public long? FishingExp { get; set; }

    /// <summary>
    ///     Gets or sets the user's fishing skill level.
    ///     Higher levels may provide better rewards or increased success rates.
    /// </summary>
    [Column("fishing_level")]
    public int? FishingLevel { get; set; }

    /// <summary>
    ///     Gets or sets the energy points restored per hour.
    /// </summary>
    [Column("energy_hour")]
    public int? EnergyHour { get; set; }

    /// <summary>
    ///     Gets or sets the maximum level cap for fishing.
    /// </summary>
    [Column("fishing_level_cap")]
    public long? FishingLevelCap { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs in the user's active party.
    ///     The party typically consists of Pokémon used for battles and other activities.
    /// </summary>
    [Column("party")]
    public ulong[]? Party { get; set; }

    /// <summary>
    ///     Gets or sets the user's luck value.
    ///     Luck may influence various random elements in the game such as encounter rates or successful captures.
    /// </summary>
    [Column("luck")]
    public int? Luck { get; set; }

    /// <summary>
    ///     Gets or sets the user's relationships with NPCs in the game as a JSON string.
    ///     These relationships may influence interactions, quests, or rewards.
    /// </summary>
    [Column("npc_relationships")]
    public string? NpcRelationships { get; set; }

    /// <summary>
    ///     Gets or sets the index of the currently selected Pokémon in the user's collection.
    /// </summary>
    [Column("selected")]
    public int? Selected { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user's profile is visible to others.
    /// </summary>
    [Column("visible")]
    public bool? Visible { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is silenced.
    ///     Silenced users may have limited communication abilities.
    /// </summary>
    [Column("silenced")]
    public bool? Silenced { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is prevented from trading.
    /// </summary>
    [Column("tradelock")]
    public bool? TradeLock { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has voted for the bot recently.
    /// </summary>
    [Column("voted")]
    public bool? Voted { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user's data has been restored from a backup.
    /// </summary>
    [Column("restored")]
    public bool? Restored { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user owns a bike.
    ///     Bikes may provide advantages such as faster movement or access to specific areas.
    /// </summary>
    [Column("bike")]
    public bool? Bike { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is banned from using the bot.
    /// </summary>
    [Column("botbanned")]
    public bool? BotBanned { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of Pokémon the user can have on the market at once.
    /// </summary>
    [Column("marketlimit")]
    public int? MarketLimit { get; set; }

    /// <summary>
    ///     Gets or sets the staff role or permissions of the user, if any.
    /// </summary>
    [Column("staff")]
    public string? Staff { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is a Gym Leader in the game.
    /// </summary>
    [Column("gym_leader")]
    public bool? GymLeader { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user's data has been converted from a previous system.
    /// </summary>
    [Column("oxiconverted")]
    public bool? OxiConverted { get; set; }

    /// <summary>
    ///     Gets or sets the user's participation status in the current event.
    /// </summary>
    [Column("event")]
    public int? Event { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp of the user's last vote for the bot.
    /// </summary>
    [Column("last_vote")]
    public int? LastVote { get; set; }

    /// <summary>
    ///     Gets or sets the user's current vote streak.
    ///     Consecutive votes may provide increasing rewards.
    /// </summary>
    [Column("vote_streak")]
    public int? VoteStreak { get; set; }

    /// <summary>
    ///     Gets or sets the user's collection of Pokémon skins as a JSON string.
    /// </summary>
    [Column("skins")]
    public string? Skins { get; set; }

    /// <summary>
    ///     Gets or sets the user's holiday or event-specific inventory as a JSON string.
    /// </summary>
    [Column("holidayinv")]
    public string? HolidayInventory { get; set; }

    /// <summary>
    ///     Gets or sets the number of raffle tickets the user has.
    /// </summary>
    [Column("raffle")]
    public int? Raffle { get; set; }

    /// <summary>
    ///     Gets or sets the user's current game region.
    ///     Different regions may contain different Pokémon and features.
    /// </summary>
    [Column("region")]
    public string? Region { get; set; }

    /// <summary>
    ///     Gets or sets the Pokémon the user is currently hunting.
    ///     Hunting may provide increased chances of encountering a specific Pokémon.
    /// </summary>
    [Column("hunt")]
    public string? Hunt { get; set; }

    /// <summary>
    ///     Gets or sets the user's current encounter chain length.
    ///     Longer chains may increase the chances of finding rare or shiny Pokémon.
    /// </summary>
    [Column("chain")]
    public int? Chain { get; set; }

    /// <summary>
    ///     Gets or sets a manual override for the user's Patreon status or tier.
    /// </summary>
    [Column("patreon_override")]
    public string? PatreonOverride { get; set; }

    /// <summary>
    ///     Gets or sets an administrative note about the user.
    /// </summary>
    [Column("note")]
    public string? Note { get; set; }

    /// <summary>
    ///     Gets or sets the number of skin tokens the user has.
    ///     Skin tokens can be used to purchase Pokémon skins.
    /// </summary>
    [Column("skin_tokens")]
    public int? SkinTokens { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is participating in competitions.
    /// </summary>
    [Column("comp")]
    public bool? Comp { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to display the user's donations publicly.
    /// </summary>
    [Column("show_donations")]
    public bool? ShowDonations { get; set; }

    /// <summary>
    ///     Gets or sets the number of mystery tokens the user has.
    ///     Mystery tokens may be used for special rewards or features.
    /// </summary>
    [Column("mystery_token")]
    public int? MysteryToken { get; set; }

    /// <summary>
    ///     Gets or sets the number of event-specific skin tokens the user has.
    ///     These tokens may be limited to event-specific skins.
    /// </summary>
    [Column("event_skin_tokens")]
    public int? EventSkinTokens { get; set; }

    /// <summary>
    ///     Gets or sets the user's reputation in the OS (Operating System) community.
    /// </summary>
    [Column("os_rep")]
    public int? OsRep { get; set; }

    /// <summary>
    ///     Gets or sets the number of Wombo tickets the user has.
    ///     These may be used for special features or rewards.
    /// </summary>
    [Column("wombo_ticket")]
    public int? WomboTicket { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user is currently active in the system.
    /// </summary>
    [Column("active")]
    public bool? Active { get; set; }
}