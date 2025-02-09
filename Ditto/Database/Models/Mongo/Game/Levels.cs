using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class Levels
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("titles")]
    public Dictionary<string, string> Titles { get; set; }

    [BsonExtraElements]
    public IDictionary<string, int> LevelRequirements { get; set; }
}