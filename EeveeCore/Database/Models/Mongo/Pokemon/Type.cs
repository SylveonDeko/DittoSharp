using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

public class Type
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public int TypeId { get; set; }

    [BsonElement("identifier")] public string Identifier { get; set; }

    [BsonElement("generation_id")] public int? GenerationId { get; set; }

    [BsonElement("damage_class_id")] public int? DamageClassId { get; set; }
}