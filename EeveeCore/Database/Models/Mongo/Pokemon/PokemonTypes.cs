using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

[BsonCollection("ptypes")]
public class PokemonTypes
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public int PokemonId { get; set; }

    [BsonElement("types")] public List<int> Types { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class BsonCollectionAttribute(string collectionName) : Attribute
{
    public string CollectionName { get; } = collectionName;
}