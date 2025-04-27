using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
/// Represents a user's achievements and statistics in the EeveeCore Pokémon bot system.
/// This class tracks various player accomplishments including battle performance, Pokémon collection,
/// breeding success, market activities, and other gameplay statistics.
/// </summary>
[Table("achievements")]
public class Achievement
{
    /// <summary>
    /// Gets or sets the Discord user ID associated with these achievements.
    /// This serves as the primary key for the achievement record.
    /// </summary>
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    #region Battle Stats

    /// <summary>
    /// Gets or sets the number of party battles the user has won.
    /// Party battles involve multiple Pokémon on each side.
    /// </summary>
    [Column("duel_party_wins")] public int DuelPartyWins { get; set; }

    /// <summary>
    /// Gets or sets the number of single battles the user has won.
    /// Single battles involve one Pokémon on each side.
    /// </summary>
    [Column("duel_single_wins")] public int DuelSingleWins { get; set; }

    /// <summary>
    /// Gets or sets the number of battles won against NPCs.
    /// </summary>
    [Column("npc_wins")] public int NpcWins { get; set; }

    /// <summary>
    /// Gets or sets the number of party battles the user has lost.
    /// </summary>
    [Column("duel_party_loses")] public int DuelPartyLoses { get; set; }

    /// <summary>
    /// Gets or sets the total experience points earned from duels.
    /// </summary>
    [Column("duel_total_xp")] public float DuelTotalXp { get; set; }

    /// <summary>
    /// Gets or sets the number of inverse battles the user has won.
    /// Inverse battles have reversed type effectiveness (e.g., normally ineffective moves become super effective).
    /// </summary>
    [Column("duel_inverse_wins")] public int DuelInverseWins { get; set; }

    /// <summary>
    /// Gets or sets the total number of duels against NPCs.
    /// </summary>
    [Column("npc_duels")] public int NpcDuels { get; set; }

    /// <summary>
    /// Gets or sets the total number of duels participated in.
    /// </summary>
    [Column("duels_total")] public int DuelsTotal { get; set; }

    /// <summary>
    /// Gets or sets the number of gym battles won.
    /// </summary>
    [Column("gym_wins")] public int GymWins { get; set; }

    #endregion

    #region Pokemon Stats

    /// <summary>
    /// Gets or sets the total number of Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_caught")] public int PokemonCaught { get; set; }

    /// <summary>
    /// Gets or sets the number of shiny Pokémon caught by the user.
    /// </summary>
    [Column("shiny_caught")] public int ShinyCaught { get; set; }

    /// <summary>
    /// Gets or sets the number of shadow Pokémon caught by the user.
    /// </summary>
    [Column("shadow_caught")] public int ShadowCaught { get; set; }

    /// <summary>
    /// Gets or sets the number of Pokémon released by the user.
    /// </summary>
    [Column("pokemon_released")] public int PokemonReleased { get; set; }

    /// <summary>
    /// Gets or sets the cumulative IV total of all Pokémon released by the user.
    /// This may be used for calculating certain achievements or rewards.
    /// </summary>
    [Column("pokemon_released_ivtotal")] public int PokemonReleasedIvTotal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has completed the Pokédex.
    /// </summary>
    [Column("dex_complete")] public bool? DexComplete { get; set; }

    #endregion

    #region Type Collection

    /// <summary>
    /// Gets or sets the number of Normal-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_normal")] public int PokemonNormal { get; set; }

    /// <summary>
    /// Gets or sets the number of Fire-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_fire")] public int PokemonFire { get; set; }

    /// <summary>
    /// Gets or sets the number of Water-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_water")] public int PokemonWater { get; set; }

    /// <summary>
    /// Gets or sets the number of Grass-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_grass")] public int PokemonGrass { get; set; }

    /// <summary>
    /// Gets or sets the number of Electric-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_electric")] public int PokemonElectric { get; set; }

    /// <summary>
    /// Gets or sets the number of Ice-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_ice")] public int PokemonIce { get; set; }

    /// <summary>
    /// Gets or sets the number of Fighting-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_fighting")] public int PokemonFighting { get; set; }

    /// <summary>
    /// Gets or sets the number of Poison-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_poison")] public int PokemonPoison { get; set; }

    /// <summary>
    /// Gets or sets the number of Ground-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_ground")] public int PokemonGround { get; set; }

    /// <summary>
    /// Gets or sets the number of Flying-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_flying")] public int PokemonFlying { get; set; }

    /// <summary>
    /// Gets or sets the number of Psychic-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_psychic")] public int PokemonPsychic { get; set; }

    /// <summary>
    /// Gets or sets the number of Bug-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_bug")] public int PokemonBug { get; set; }

    /// <summary>
    /// Gets or sets the number of Rock-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_rock")] public int PokemonRock { get; set; }

    /// <summary>
    /// Gets or sets the number of Ghost-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_ghost")] public int PokemonGhost { get; set; }

    /// <summary>
    /// Gets or sets the number of Dark-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_dark")] public int PokemonDark { get; set; }

    /// <summary>
    /// Gets or sets the number of Dragon-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_dragon")] public int PokemonDragon { get; set; }

    /// <summary>
    /// Gets or sets the number of Steel-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_steel")] public int PokemonSteel { get; set; }

    /// <summary>
    /// Gets or sets the number of Fairy-type Pokémon caught by the user.
    /// </summary>
    [Column("pokemon_fairy")] public int PokemonFairy { get; set; }

    #endregion

    #region Breeding Stats

    /// <summary>
    /// Gets or sets the number of successful breeding attempts by the user.
    /// </summary>
    [Column("breed_success")] public int BreedSuccess { get; set; }

    /// <summary>
    /// Gets or sets the number of perfect 6-IV (Hexa) Pokémon bred by the user.
    /// </summary>
    [Column("breed_hexa")] public int BreedHexa { get; set; }

    /// <summary>
    /// Gets or sets the number of 5-IV (Penta) Pokémon bred by the user.
    /// </summary>
    [Column("breed_penta")] public int BreedPenta { get; set; }

    /// <summary>
    /// Gets or sets the number of 4-IV (Quad) Pokémon bred by the user.
    /// </summary>
    [Column("breed_quad")] public int BreedQuad { get; set; }

    /// <summary>
    /// Gets or sets the number of Titan Pokémon bred by the user.
    /// Titan Pokémon may have special characteristics or increased size.
    /// </summary>
    [Column("breed_titan")] public int BreedTitan { get; set; }

    /// <summary>
    /// Gets or sets the number of shiny Pokémon bred by the user.
    /// </summary>
    [Column("shiny_bred")] public int ShinyBred { get; set; }

    /// <summary>
    /// Gets or sets the number of shadow Pokémon bred by the user.
    /// </summary>
    [Column("shadow_bred")] public int ShadowBred { get; set; }

    #endregion

    #region Market Stats

    /// <summary>
    /// Gets or sets the number of Pokémon sold on the market by the user.
    /// </summary>
    [Column("market_sold")] public int MarketSold { get; set; }

    /// <summary>
    /// Gets or sets the number of Pokémon purchased from the market by the user.
    /// </summary>
    [Column("market_purchased")] public int MarketPurchased { get; set; }

    #endregion

    #region Chest Stats

    /// <summary>
    /// Gets or sets the number of legendary chests opened by the user.
    /// </summary>
    [Column("chests_legend")] public int ChestsLegend { get; set; }

    /// <summary>
    /// Gets or sets the number of mythic chests opened by the user.
    /// </summary>
    [Column("chests_mythic")] public int ChestsMythic { get; set; }

    /// <summary>
    /// Gets or sets the number of rare chests opened by the user.
    /// </summary>
    [Column("chests_rare")] public int ChestsRare { get; set; }

    /// <summary>
    /// Gets or sets the number of common chests opened by the user.
    /// </summary>
    [Column("chests_common")] public int ChestsCommon { get; set; }

    /// <summary>
    /// Gets or sets the number of voucher chests opened by the user.
    /// </summary>
    [Column("chests_voucher")] public int ChestsVoucher { get; set; }

    #endregion

    #region Misc Stats

    /// <summary>
    /// Gets or sets a value indicating whether the user has created their first party.
    /// </summary>
    [Column("first_party_create")] public bool FirstPartyCreate { get; set; }

    /// <summary>
    /// Gets or sets the number of successful fishing attempts by the user.
    /// </summary>
    [Column("fishing_success")] public int FishingSuccess { get; set; }

    /// <summary>
    /// Gets or sets the number of redeems used by the user.
    /// </summary>
    [Column("redeems_used")] public int RedeemsUsed { get; set; }

    /// <summary>
    /// Gets or sets the number of missions completed by the user.
    /// </summary>
    [Column("missions")] public int Missions { get; set; }

    /// <summary>
    /// Gets or sets the number of times the user has voted for the bot.
    /// </summary>
    [Column("votes")] public int Votes { get; set; }

    /// <summary>
    /// Gets or sets the total amount donated by the user.
    /// </summary>
    [Column("donation_amount")] public int DonationAmount { get; set; }

    /// <summary>
    /// Gets or sets the user's progress in the Unown event.
    /// </summary>
    [Column("unown_event")] public int UnownEvent { get; set; }

    /// <summary>
    /// Gets or sets the number of Easter events completed by the user.
    /// </summary>
    [Column("easter_completed")] public short EasterCompleted { get; set; }

    /// <summary>
    /// Gets or sets the number of times the user has used the Wombo feature.
    /// </summary>
    [Column("wombo_used")] public int WomboUsed { get; set; }

    #endregion
}