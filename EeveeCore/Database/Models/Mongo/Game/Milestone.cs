using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
///     Represents user milestone achievements and statistical tracking in the game.
/// </summary>
public class Milestone
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user identifier.
    /// </summary>
    [BsonElement("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of party duel wins achieved by the user.
    /// </summary>
    [BsonElement("duel_party_wins")]
    public int DuelPartyWins { get; set; }

    /// <summary>
    ///     Gets or sets the number of single Pokémon duel wins achieved by the user.
    /// </summary>
    [BsonElement("duel_single_wins")]
    public int DuelSingleWins { get; set; }

    /// <summary>
    ///     Gets or sets the number of NPC battle wins achieved by the user.
    /// </summary>
    [BsonElement("npc_wins")]
    public int NpcWins { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has created their first party.
    /// </summary>
    [BsonElement("first_party_create")]
    public bool FirstPartyCreate { get; set; }

    /// <summary>
    ///     Gets or sets the number of successful fishing attempts by the user.
    /// </summary>
    [BsonElement("fishing_success")]
    public int FishingSuccess { get; set; }

    /// <summary>
    ///     Gets or sets the number of perfect IV (6/6) Pokémon bred by the user.
    /// </summary>
    [BsonElement("breed_hexa")]
    public int BreedHexa { get; set; }

    /// <summary>
    ///     Gets or sets the number of near-perfect IV (5/6) Pokémon bred by the user.
    /// </summary>
    [BsonElement("breed_penta")]
    public int? BreedPenta { get; set; }

    /// <summary>
    ///     Gets or sets the total number of successful breeding attempts by the user.
    /// </summary>
    [BsonElement("breed_success")]
    public int BreedSuccess { get; set; }

    /// <summary>
    ///     Gets or sets the number of Pokémon sold on the market by the user.
    /// </summary>
    [BsonElement("market_sold")]
    public int MarketSold { get; set; }

    /// <summary>
    ///     Gets or sets the total number of Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_caught")]
    public int PokemonCaught { get; set; }

    /// <summary>
    ///     Gets or sets the number of shiny Pokémon caught by the user.
    /// </summary>
    [BsonElement("shiny_caught")]
    public int ShinyCaught { get; set; }

    /// <summary>
    ///     Gets or sets the number of shadow Pokémon caught by the user.
    /// </summary>
    [BsonElement("shadow_caught")]
    public int ShadowCaught { get; set; }

    /// <summary>
    ///     Gets or sets the number of legendary chests opened by the user.
    /// </summary>
    [BsonElement("chests_legend")]
    public int ChestsLegend { get; set; }

    /// <summary>
    ///     Gets or sets the number of mythic chests opened by the user.
    /// </summary>
    [BsonElement("chests_mythic")]
    public int ChestsMythic { get; set; }

    /// <summary>
    ///     Gets or sets the number of rare chests opened by the user.
    /// </summary>
    [BsonElement("chests_rare")]
    public int ChestsRare { get; set; }

    /// <summary>
    ///     Gets or sets the number of common chests opened by the user.
    /// </summary>
    [BsonElement("chests_common")]
    public int ChestsCommon { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeems used by the user.
    /// </summary>
    [BsonElement("redeems_used")]
    public int RedeemsUsed { get; set; }

    /// <summary>
    ///     Gets or sets the number of missions completed by the user.
    /// </summary>
    [BsonElement("missions")]
    public int Missions { get; set; }

    /// <summary>
    ///     Gets or sets the number of times the user has voted for the bot.
    /// </summary>
    [BsonElement("votes")]
    public int Votes { get; set; }

    /// <summary>
    ///     Gets or sets the total donation amount contributed by the user.
    /// </summary>
    [BsonElement("donation_amount")]
    public int? DonationAmount { get; set; }

    /// <summary>
    ///     Gets or sets the number of Normal-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_normal")]
    public int? PokemonNormal { get; set; }

    /// <summary>
    ///     Gets or sets the number of Fire-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_fire")]
    public int? PokemonFire { get; set; }

    /// <summary>
    ///     Gets or sets the number of Water-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_water")]
    public int? PokemonWater { get; set; }

    /// <summary>
    ///     Gets or sets the number of Grass-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_grass")]
    public int? PokemonGrass { get; set; }

    /// <summary>
    ///     Gets or sets the number of Electric-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_electric")]
    public int? PokemonElectric { get; set; }

    /// <summary>
    ///     Gets or sets the number of Ice-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_ice")]
    public int? PokemonIce { get; set; }

    /// <summary>
    ///     Gets or sets the number of Fighting-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_fighting")]
    public int? PokemonFighting { get; set; }

    /// <summary>
    ///     Gets or sets the number of Poison-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_poison")]
    public int? PokemonPoison { get; set; }

    /// <summary>
    ///     Gets or sets the number of Ground-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_ground")]
    public int? PokemonGround { get; set; }

    /// <summary>
    ///     Gets or sets the number of Flying-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_flying")]
    public int? PokemonFlying { get; set; }

    /// <summary>
    ///     Gets or sets the number of Psychic-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_psychic")]
    public int? PokemonPsychic { get; set; }

    /// <summary>
    ///     Gets or sets the number of Bug-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_bug")]
    public int? PokemonBug { get; set; }

    /// <summary>
    ///     Gets or sets the number of Rock-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_rock")]
    public int? PokemonRock { get; set; }

    /// <summary>
    ///     Gets or sets the number of Ghost-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_ghost")]
    public int? PokemonGhost { get; set; }

    /// <summary>
    ///     Gets or sets the number of Dark-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_dark")]
    public int? PokemonDark { get; set; }

    /// <summary>
    ///     Gets or sets the number of Dragon-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_dragon")]
    public int? PokemonDragon { get; set; }

    /// <summary>
    ///     Gets or sets the number of Steel-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_steel")]
    public int? PokemonSteel { get; set; }

    /// <summary>
    ///     Gets or sets the number of Fairy-type Pokémon caught by the user.
    /// </summary>
    [BsonElement("pokemon_fairy")]
    public int? PokemonFairy { get; set; }

    /// <summary>
    ///     Gets or sets the number of Unown event completions by the user.
    /// </summary>
    [BsonElement("unown_event")]
    public int UnownEvent { get; set; }

    /// <summary>
    ///     Gets or sets the total number of NPC duels participated in by the user.
    /// </summary>
    [BsonElement("npc_duels")]
    public int NpcDuels { get; set; }

    /// <summary>
    ///     Gets or sets the total number of duels participated in by the user.
    /// </summary>
    [BsonElement("duels_total")]
    public int DuelsTotal { get; set; }

    /// <summary>
    ///     Gets or sets the number of Pokémon released by the user.
    /// </summary>
    [BsonElement("pokemon_released")]
    public int PokemonReleased { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has completed the Pokédex.
    /// </summary>
    [BsonElement("dex_complete")]
    public bool DexComplete { get; set; }

    /// <summary>
    ///     Gets or sets the number of inverse duel wins achieved by the user.
    /// </summary>
    [BsonElement("duel_inverse_wins")]
    public int DuelInverseWins { get; set; }

    /// <summary>
    ///     Gets or sets the number of shiny Pokémon bred by the user.
    /// </summary>
    [BsonElement("shiny_bred")]
    public int ShinyBred { get; set; }

    /// <summary>
    ///     Gets or sets the number of shadow Pokémon bred by the user.
    /// </summary>
    [BsonElement("shadow_bred")]
    public int ShadowBred { get; set; }

    /// <summary>
    ///     Gets or sets the number of voucher chests opened by the user.
    /// </summary>
    [BsonElement("chests_voucher")]
    public int ChestsVoucher { get; set; }

    /// <summary>
    ///     Gets or sets the number of Pokémon purchased from the market by the user.
    /// </summary>
    [BsonElement("market_purchased")]
    public int MarketPurchased { get; set; }

    /// <summary>
    ///     Gets or sets the number of gym battles won by the user.
    /// </summary>
    [BsonElement("gym_wins")]
    public int GymWins { get; set; }

    /// <summary>
    ///     Gets or sets the number of Easter events completed by the user.
    /// </summary>
    [BsonElement("easter_completed")]
    public int EasterCompleted { get; set; }

    /// <summary>
    ///     Gets or sets the number of times the user has used the Wombo command.
    /// </summary>
    [BsonElement("wombo_used")]
    public int WomboUsed { get; set; }

    /// <summary>
    ///     Gets or sets the total IV value of all Pokémon released by the user.
    /// </summary>
    [BsonElement("pokemon_released_ivtotal")]
    public int PokemonReleasedIvTotal { get; set; }
}