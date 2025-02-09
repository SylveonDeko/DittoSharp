using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class TypeEffectiveness
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("damage_type_id")]
    public int DamageTypeId { get; set; }

    [BsonElement("target_type_id")]
    public int TargetTypeId { get; set; }

    [BsonElement("damage_factor")]
    public int DamageFactor { get; set; }
}