using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

public class PokemonMoves
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("pokemon")] public string Pokemon { get; set; }

    [BsonElement("moves")] public List<string> Moves { get; set; }
}