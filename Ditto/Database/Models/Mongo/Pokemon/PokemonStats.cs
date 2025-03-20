using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class PokemonStats
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("pokemon_id")] public int PokemonId { get; set; }

    [BsonElement("stats")] public List<int> Stats { get; set; }
}