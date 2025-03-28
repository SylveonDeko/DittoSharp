using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Discord;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("user")] public ulong UserId { get; set; }

    [BsonElement("progress")] public Dictionary<string, int> Progress { get; set; }
}