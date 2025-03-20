using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("users_inactive")]
public class InactiveUser
{
    [Column("u_id")] public ulong? UserId { get; set; }

    [Column("redeems")] public int? Redeems { get; set; }

    [Column("evpoints")] public int? EvPoints { get; set; }

    [Column("tnick")] public string? TrainerNickname { get; set; }

    [Column("upvotepoints")] public int? UpvotePoints { get; set; }

    [Column("mewcoins")] public ulong? MewCoins { get; set; }

    [Column("user_order")] public string? UserOrder { get; set; }

    [Column("pokes", TypeName = "bigint[]")]
    public long[]? Pokemon { get; set; }

    [Column("inventory", TypeName = "jsonb")]
    public string? Inventory { get; set; }

    [Column("items", TypeName = "jsonb")] public string? Items { get; set; }

    [Column("daycare")] public int? Daycare { get; set; }

    [Column("daycarelimit")] public int? DaycareLimit { get; set; }

    [Column("energy")] public int? Energy { get; set; }

    [Column("held_item")] public string? HeldItem { get; set; }

    [Column("fishing_exp")] public ulong? FishingExp { get; set; }

    [Column("fishing_level")] public int? FishingLevel { get; set; }

    [Column("energy_hour")] public int? EnergyHour { get; set; }

    [Column("fishing_level_cap")] public ulong? FishingLevelCap { get; set; }

    [Column("party", TypeName = "bigint[]")]
    public long[]? Party { get; set; }

    [Column("luck")] public int? Luck { get; set; }

    [Column("npc_relationships", TypeName = "jsonb")]
    public string? NpcRelationships { get; set; }

    [Column("selected")] public int? Selected { get; set; }

    [Column("visible")] public bool? Visible { get; set; }

    [Column("silenced")] public bool? Silenced { get; set; }

    [Column("tradelock")] public bool? TradeLock { get; set; }

    [Column("voted")] public bool? Voted { get; set; }

    [Key] [Column("id")] public int? Id { get; set; }

    [Column("restored")] public bool? Restored { get; set; }

    [Column("bike")] public bool? Bike { get; set; }

    [Column("botbanned")] public bool? BotBanned { get; set; }

    [Column("marketlimit")] public int? MarketLimit { get; set; }

    [Column("staff")] public string? Staff { get; set; }

    [Column("gym_leader")] public bool? GymLeader { get; set; }

    [Column("oxiconverted")] public bool? OxiConverted { get; set; }

    [Column("event")] public int? Event { get; set; }

    [Column("last_vote")] public int? LastVote { get; set; }

    [Column("vote_streak")] public int? VoteStreak { get; set; }

    [Column("skins", TypeName = "jsonb")] public string? Skins { get; set; }

    [Column("holidayinv", TypeName = "jsonb")]
    public string? HolidayInventory { get; set; }

    [Column("raffle")] public int? Raffle { get; set; }

    [Column("region")] public string? Region { get; set; }

    [Column("hunt")] public string? Hunt { get; set; }

    [Column("chain")] public int? Chain { get; set; }

    [Column("patreon_override")] public string? PatreonOverride { get; set; }

    [Column("note")] public string? Note { get; set; }

    [Column("skin_tokens")] public int? SkinTokens { get; set; }

    [Column("comp")] public bool? Comp { get; set; }

    [Column("show_donations")] public bool? ShowDonations { get; set; }

    [Column("mystery_token")] public int? MysteryToken { get; set; }

    [Column("event_skin_tokens")] public int? EventSkinTokens { get; set; }

    [Column("os_rep")] public int? OsRep { get; set; }

    [Column("wombo_ticket")] public int? WomboTicket { get; set; }

    [Column("active")] public bool? Active { get; set; }
}