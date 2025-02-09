using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class Gift
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")]
    public ulong UserId { get; set; }

    [BsonElement("gifts")]
    public int Gifts { get; set; }

    [BsonElement("shinies")]
    public int Shinies { get; set; }
}