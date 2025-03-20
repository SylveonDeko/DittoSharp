using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class CurrentRadiant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("rads")] public List<string> Radiants { get; set; }

    [BsonElement("month")] public int Month { get; set; }

    [BsonElement("year")] public int Year { get; set; }
}