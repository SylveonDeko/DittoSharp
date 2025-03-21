using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

public class PokemonAbility
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("pokemon_id")] public int PokemonId { get; set; }

    [BsonElement("ability_id")] public int AbilityId { get; set; }

    [BsonElement("is_hidden")] public int IsHidden { get; set; }

    [BsonElement("slot")] public int Slot { get; set; }
}