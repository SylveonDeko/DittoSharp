using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class Blacklist
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("guilds")]
    public List<ulong> Guilds { get; set; }

    [BsonElement("users")]
    public List<ulong> Users { get; set; }
}