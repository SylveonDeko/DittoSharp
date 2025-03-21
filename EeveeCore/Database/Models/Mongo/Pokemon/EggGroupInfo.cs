using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

public class EggGroupInfo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public int GroupId { get; set; }

    [BsonElement("identifier")] public string Identifier { get; set; }
}