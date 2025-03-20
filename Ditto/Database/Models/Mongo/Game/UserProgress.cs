using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class UserProgress
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("user_id")] public ulong UserId { get; set; }

    [BsonElement("breed")] public int Breed { get; set; }

    [BsonElement("catch")] public int Catch { get; set; }

    [BsonElement("duel_lose")] public int DuelLose { get; set; }

    [BsonElement("duel_win")] public int DuelWin { get; set; }

    [BsonElement("ev")] public int Ev { get; set; }

    [BsonElement("fish")] public int Fish { get; set; }

    [BsonElement("npc")] public int Npc { get; set; }

    [BsonElement("party")] public int Party { get; set; }

    [BsonElement("pokemon_setup")] public int PokemonSetup { get; set; }

    [BsonElement("vote")] public int Vote { get; set; }
}