using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("users")]
public class User
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("u_id")] public ulong? UserId { get; set; }

    [Column("note")] public string? Note { get; set; }

    #region Resources

    [Column("redeems")] public int? Redeems { get; set; }

    [Column("evpoints")] public int? EvPoints { get; set; }

    [Column("upvotepoints")] public int? UpvotePoints { get; set; }

    [Column("mewcoins")] public ulong? MewCoins { get; set; }

    [Column("slime")] public int? Slime { get; set; }

    [Column("crystal_slime")] public int? CrystalSlime { get; set; }

    [Column("skin_tokens")] [Required] public int SkinTokens { get; set; }

    [Column("event_skin_tokens")] public int? EventSkinTokens { get; set; }

    [Column("vip_tokens")] [Required] public int VipTokens { get; set; }

    [Column("mystery_token")] public int? MysteryToken { get; set; }

    [Column("voucher")] public int? Voucher { get; set; }

    [Column("os_rep")] [Required] public int OsRep { get; set; }

    #endregion

    #region Pokemon Management

    [Column("pokes", TypeName = "bigint[]")]
    public ulong[]? Pokemon { get; set; }

    [Column("selected")] public ulong? Selected { get; set; }

    [Column("party", TypeName = "bigint[]")]
    public ulong[]? Party { get; set; } = [0, 0, 0, 0, 0, 0];

    [Column("daycare")] public int? Daycare { get; set; }

    [Column("daycarelimit")] public int? DaycareLimit { get; set; } = 1;

    [Column("females", TypeName = "integer[]")]
    public int?[]? Females { get; set; }

    #endregion

    #region User Settings

    [Column("tnick")] public string? TrainerNickname { get; set; }

    [Column("user_order")] public string? UserOrder { get; set; } = "kek";

    [Column("region")] [Required] public string Region { get; set; } = "original";

    [Column("inventory", TypeName = "jsonb")]
    public string? Inventory { get; set; } = "{}";

    [Column("items", TypeName = "jsonb")] public string? Items { get; set; } = "{}";

    [Column("held_item")] public string? HeldItem { get; set; } = "None";

    [Column("skins", TypeName = "jsonb")]
    [Required]
    public string Skins { get; set; } = "{}";

    [Column("holidayinv", TypeName = "jsonb")]
    public string? HolidayInventory { get; set; } = "{}";

    #endregion

    #region Progress and Stats

    [Column("fishing_exp")] public ulong? FishingExp { get; set; }

    [Column("fishing_level")] public int? FishingLevel { get; set; } = 1;

    [Column("fishing_level_cap")] public ulong? FishingLevelCap { get; set; } = 50;

    [Column("level")] public int? Level { get; set; } = 1;

    [Column("current_xp")] public int? CurrentXp { get; set; }

    [Column("chain")] [Required] public int Chain { get; set; }

    [Column("hunt")] public string? Hunt { get; set; } = "";

    #endregion

    #region Status and Flags

    [Column("energy")] public int? Energy { get; set; } = 10;

    [Column("energy_hour")] public int? EnergyHour { get; set; }

    [Column("luck")] public int? Luck { get; set; } = 1;

    [Column("visible")] public bool? Visible { get; set; } = true;

    [Column("silenced")] [Required] public bool Silenced { get; set; }

    [Column("tradelock")] public bool? TradeLock { get; set; }

    [Column("voted")] public bool? Voted { get; set; }

    [Column("restored")] public bool? Restored { get; set; }

    [Column("bike")] public bool? Bike { get; set; }

    [Column("botbanned")] public bool? BotBanned { get; set; }

    [Column("gym_leader")] public bool? GymLeader { get; set; }

    [Column("oxiconverted")] [Required] public bool OxiConverted { get; set; }

    [Column("ancient_unlocked")] public bool? AncientUnlocked { get; set; }

    [Column("active")] [Required] public bool Active { get; set; }

    [Column("comp")] public bool? Comp { get; set; }

    [Column("show_donations")] public bool? ShowDonations { get; set; }

    #endregion

    #region Trading and Market

    [Column("marketlimit")] [Required] public int MarketLimit { get; set; } = 3;

    [Column("last_vote")] [Required] public int LastVote { get; set; }

    [Column("vote_streak")] [Required] public int VoteStreak { get; set; }

    #endregion

    #region Relationships and Progress

    [Column("npc_relationships", TypeName = "jsonb")]
    public string? NpcRelationships { get; set; } = "{}";

    [Column("start_progress", TypeName = "jsonb")]
    [Required]
    public string StartProgress { get; set; } =
        "{\"Tutorial Mission\": {\"Stage1\": false, \"Stage2\": false, \"Stage3\": false, \"Stage4\": false}}";

    #endregion

    #region Staff and Event

    [Column("staff")] [Required] public string Staff { get; set; } = "User";

    [Column("event")] public int? Event { get; set; }

    [Column("raffle")] public int? Raffle { get; set; }

    #endregion

    #region Titles and Tokens

    [Column("titles", TypeName = "text[]")]
    public string[]? Titles { get; set; } = ["Newcomer"];

    [Column("selected_title")] public string? SelectedTitle { get; set; } = "";

    [Column("type_tokens", TypeName = "integer[]")]
    public int[]? TypeTokens { get; set; } = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    [Column("tokens", TypeName = "jsonb")]
    public string? Tokens { get; set; } =
        "{\"Bug\": 0, \"Ice\": 0, \"Dark\": 0, \"Fire\": 0, \"Rock\": 0, \"Fairy\": 0, \"Ghost\": 0, \"Grass\": 0, \"Steel\": 0, \"Water\": 0, \"Dragon\": 0, \"Flying\": 0, \"Ground\": 0, \"Normal\": 0, \"Poison\": 0, \"Psychic\": 0, \"Electric\": 0, \"Fighting\": 0}";

    #endregion

    #region Alt and Patreon

    [Column("alt", TypeName = "bigint[]")] public long[]? Alt { get; set; }

    [Column("patreon")] public string? Patreon { get; set; } = "None";

    [Column("patreon_override")] public string? PatreonOverride { get; set; }

    #endregion
}