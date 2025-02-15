using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class Booster
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("key")]
    public string Key { get; set; }

    [BsonElement("boosters")]
    public List<ulong> Boosters { get; set; }
}