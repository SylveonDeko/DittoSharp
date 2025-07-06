using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
///     Represents a user's progression statistics for various game activities.
/// </summary>
public class UserProgress
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
    [BsonElement("user_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of breeding actions performed by the user.
    /// </summary>
    [BsonElement("breed")]
    public int Breed { get; set; }

    /// <summary>
    ///     Gets or sets the number of Pokémon caught by the user.
    /// </summary>
    [BsonElement("catch")]
    public int Catch { get; set; }

    /// <summary>
    ///     Gets or sets the number of duels lost by the user.
    /// </summary>
    [BsonElement("duel_lose")]
    public int DuelLose { get; set; }

    /// <summary>
    ///     Gets or sets the number of duels won by the user.
    /// </summary>
    [BsonElement("duel_win")]
    public int DuelWin { get; set; }

    /// <summary>
    ///     Gets or sets the total EV (Effort Value) points earned by the user.
    /// </summary>
    [BsonElement("ev")]
    public int Ev { get; set; }

    /// <summary>
    ///     Gets or sets the number of fishing actions performed by the user.
    /// </summary>
    [BsonElement("fish")]
    public int Fish { get; set; }

    /// <summary>
    ///     Gets or sets the number of NPC battles completed by the user.
    /// </summary>
    [BsonElement("npc")]
    public int Npc { get; set; }

    /// <summary>
    ///     Gets or sets the number of party-related actions performed by the user.
    /// </summary>
    [BsonElement("party")]
    public int Party { get; set; }

    /// <summary>
    ///     Gets or sets the number of Pokémon setup configurations made by the user.
    /// </summary>
    [BsonElement("pokemon_setup")]
    public int PokemonSetup { get; set; }

    /// <summary>
    ///     Gets or sets the number of times the user has voted for the bot.
    /// </summary>
    [BsonElement("vote")]
    public int Vote { get; set; }

    /// <summary>
    ///     Gets or sets the number of word search games completed by the user.
    /// </summary>
    [BsonElement("game_wordsearch")]
    public int GameWordSearch { get; set; }

    /// <summary>
    ///     Gets or sets the number of slot machine games played by the user.
    /// </summary>
    [BsonElement("game_slots")]
    public int GameSlots { get; set; }

    /// <summary>
    ///     Gets or sets the number of slot machine games won by the user.
    /// </summary>
    [BsonElement("game_slots_win")]
    public int GameSlotsWin { get; set; }
}