using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

public class Filter
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("arg")] public string Argument { get; set; }

    [BsonElement("value")] public string Value { get; set; }
}