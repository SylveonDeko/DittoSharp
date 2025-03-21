using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Discord;

public class Gym
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")] public string Name { get; set; }

    [BsonElement("emote")] public string Emote { get; set; }

    [BsonElement("img")] public string ImagePath { get; set; }
}