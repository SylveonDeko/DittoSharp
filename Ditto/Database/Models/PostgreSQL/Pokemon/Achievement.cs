using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Pokemon;

[Table("achievements")]
public class Achievement
{
    [Key] [Column("u_id")] public ulong UserId { get; set; }

    #region Battle Stats

    [Column("duel_party_wins")] public int DuelPartyWins { get; set; }

    [Column("duel_single_wins")] public int DuelSingleWins { get; set; }

    [Column("npc_wins")] public int NpcWins { get; set; }

    [Column("duel_party_loses")] public int DuelPartyLoses { get; set; }

    [Column("duel_total_xp")] public float DuelTotalXp { get; set; }

    [Column("duel_inverse_wins")] public int DuelInverseWins { get; set; }

    [Column("npc_duels")] public int NpcDuels { get; set; }

    [Column("duels_total")] public int DuelsTotal { get; set; }

    [Column("gym_wins")] public int GymWins { get; set; }

    #endregion

    #region Pokemon Stats

    [Column("pokemon_caught")] public int PokemonCaught { get; set; }

    [Column("shiny_caught")] public int ShinyCaught { get; set; }

    [Column("shadow_caught")] public int ShadowCaught { get; set; }

    [Column("pokemon_released")] public int PokemonReleased { get; set; }

    [Column("pokemon_released_ivtotal")] public int PokemonReleasedIvTotal { get; set; }

    [Column("dex_complete")] public bool? DexComplete { get; set; }

    #endregion

    #region Type Collection

    [Column("pokemon_normal")] public int PokemonNormal { get; set; }

    [Column("pokemon_fire")] public int PokemonFire { get; set; }

    [Column("pokemon_water")] public int PokemonWater { get; set; }

    [Column("pokemon_grass")] public int PokemonGrass { get; set; }

    [Column("pokemon_electric")] public int PokemonElectric { get; set; }

    [Column("pokemon_ice")] public int PokemonIce { get; set; }

    [Column("pokemon_fighting")] public int PokemonFighting { get; set; }

    [Column("pokemon_poison")] public int PokemonPoison { get; set; }

    [Column("pokemon_ground")] public int PokemonGround { get; set; }

    [Column("pokemon_flying")] public int PokemonFlying { get; set; }

    [Column("pokemon_psychic")] public int PokemonPsychic { get; set; }

    [Column("pokemon_bug")] public int PokemonBug { get; set; }

    [Column("pokemon_rock")] public int PokemonRock { get; set; }

    [Column("pokemon_ghost")] public int PokemonGhost { get; set; }

    [Column("pokemon_dark")] public int PokemonDark { get; set; }

    [Column("pokemon_dragon")] public int PokemonDragon { get; set; }

    [Column("pokemon_steel")] public int PokemonSteel { get; set; }

    [Column("pokemon_fairy")] public int PokemonFairy { get; set; }

    #endregion

    #region Breeding Stats

    [Column("breed_success")] public int BreedSuccess { get; set; }

    [Column("breed_hexa")] public int BreedHexa { get; set; }

    [Column("breed_penta")] public int BreedPenta { get; set; }

    [Column("breed_quad")] public int BreedQuad { get; set; }

    [Column("breed_titan")] public int BreedTitan { get; set; }

    [Column("shiny_bred")] public int ShinyBred { get; set; }

    [Column("shadow_bred")] public int ShadowBred { get; set; }

    #endregion

    #region Market Stats

    [Column("market_sold")] public int MarketSold { get; set; }

    [Column("market_purchased")] public int MarketPurchased { get; set; }

    #endregion

    #region Chest Stats

    [Column("chests_legend")] public int ChestsLegend { get; set; }

    [Column("chests_mythic")] public int ChestsMythic { get; set; }

    [Column("chests_rare")] public int ChestsRare { get; set; }

    [Column("chests_common")] public int ChestsCommon { get; set; }

    [Column("chests_voucher")] public int ChestsVoucher { get; set; }

    #endregion

    #region Misc Stats

    [Column("first_party_create")] public bool FirstPartyCreate { get; set; }

    [Column("fishing_success")] public int FishingSuccess { get; set; }

    [Column("redeems_used")] public int RedeemsUsed { get; set; }

    [Column("missions")] public int Missions { get; set; }

    [Column("votes")] public int Votes { get; set; }

    [Column("donation_amount")] public int DonationAmount { get; set; }

    [Column("unown_event")] public int UnownEvent { get; set; }

    [Column("easter_completed")] public short EasterCompleted { get; set; }

    [Column("wombo_used")] public int WomboUsed { get; set; }

    #endregion
}