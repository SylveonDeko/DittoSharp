using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class Milestone
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("u_id")]
    public ulong UserId { get; set; }

    [BsonElement("duel_party_wins")]
    public int DuelPartyWins { get; set; }

    [BsonElement("duel_single_wins")]
    public int DuelSingleWins { get; set; }

    [BsonElement("npc_wins")]
    public int NpcWins { get; set; }

    [BsonElement("first_party_create")]
    public bool FirstPartyCreate { get; set; }

    [BsonElement("fishing_success")]
    public int FishingSuccess { get; set; }

    [BsonElement("breed_hexa")]
    public int BreedHexa { get; set; }

    [BsonElement("breed_penta")]
    public int? BreedPenta { get; set; }

    [BsonElement("breed_success")]
    public int BreedSuccess { get; set; }

    [BsonElement("market_sold")]
    public int MarketSold { get; set; }

    [BsonElement("pokemon_caught")]
    public int PokemonCaught { get; set; }

    [BsonElement("shiny_caught")]
    public int ShinyCaught { get; set; }

    [BsonElement("shadow_caught")]
    public int ShadowCaught { get; set; }

    [BsonElement("chests_legend")]
    public int ChestsLegend { get; set; }

    [BsonElement("chests_mythic")]
    public int ChestsMythic { get; set; }

    [BsonElement("chests_rare")]
    public int ChestsRare { get; set; }

    [BsonElement("chests_common")]
    public int ChestsCommon { get; set; }

    [BsonElement("redeems_used")]
    public int RedeemsUsed { get; set; }

    [BsonElement("missions")]
    public int Missions { get; set; }

    [BsonElement("votes")]
    public int Votes { get; set; }

    [BsonElement("donation_amount")]
    public int? DonationAmount { get; set; }

    // Pokemon type counts
    [BsonElement("pokemon_normal")]
    public int? PokemonNormal { get; set; }

    [BsonElement("pokemon_fire")]
    public int? PokemonFire { get; set; }

    [BsonElement("pokemon_water")]
    public int? PokemonWater { get; set; }

    [BsonElement("pokemon_grass")]
    public int? PokemonGrass { get; set; }

    [BsonElement("pokemon_electric")]
    public int? PokemonElectric { get; set; }

    [BsonElement("pokemon_ice")]
    public int? PokemonIce { get; set; }

    [BsonElement("pokemon_fighting")]
    public int? PokemonFighting { get; set; }

    [BsonElement("pokemon_poison")]
    public int? PokemonPoison { get; set; }

    [BsonElement("pokemon_ground")]
    public int? PokemonGround { get; set; }

    [BsonElement("pokemon_flying")]
    public int? PokemonFlying { get; set; }

    [BsonElement("pokemon_psychic")]
    public int? PokemonPsychic { get; set; }

    [BsonElement("pokemon_bug")]
    public int? PokemonBug { get; set; }

    [BsonElement("pokemon_rock")]
    public int? PokemonRock { get; set; }

    [BsonElement("pokemon_ghost")]
    public int? PokemonGhost { get; set; }

    [BsonElement("pokemon_dark")]
    public int? PokemonDark { get; set; }

    [BsonElement("pokemon_dragon")]
    public int? PokemonDragon { get; set; }

    [BsonElement("pokemon_steel")]
    public int? PokemonSteel { get; set; }

    [BsonElement("pokemon_fairy")]
    public int? PokemonFairy { get; set; }

    [BsonElement("unown_event")]
    public int UnownEvent { get; set; }

    [BsonElement("npc_duels")]
    public int NpcDuels { get; set; }

    [BsonElement("duels_total")]
    public int DuelsTotal { get; set; }

    [BsonElement("pokemon_released")]
    public int PokemonReleased { get; set; }

    [BsonElement("dex_complete")]
    public bool DexComplete { get; set; }

    [BsonElement("duel_inverse_wins")]
    public int DuelInverseWins { get; set; }

    [BsonElement("shiny_bred")]
    public int ShinyBred { get; set; }

    [BsonElement("shadow_bred")]
    public int ShadowBred { get; set; }

    [BsonElement("chests_voucher")]
    public int ChestsVoucher { get; set; }

    [BsonElement("market_purchased")]
    public int MarketPurchased { get; set; }

    [BsonElement("gym_wins")]
    public int GymWins { get; set; }

    [BsonElement("easter_completed")]
    public int EasterCompleted { get; set; }

    [BsonElement("wombo_used")]
    public int WomboUsed { get; set; }

    [BsonElement("pokemon_released_ivtotal")]
    public int PokemonReleasedIvTotal { get; set; }
}