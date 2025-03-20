using Ditto.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class Item
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public int ItemId { get; set; }

    [BsonElement("identifier")] public string? Identifier { get; set; }

    [BsonElement("category_id")] public int CategoryId { get; set; }

    [BsonElement("cost")] public int Cost { get; set; }

    [BsonElement("fling_power")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? FlingPower { get; set; }

    [BsonElement("fling_effect_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? FlingEffectId { get; set; }
}