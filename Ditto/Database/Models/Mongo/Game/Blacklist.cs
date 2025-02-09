using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class Blacklist
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("guilds")]
    public List<LongIdentifier> Guilds { get; set; }

    [BsonElement("users")]
    public List<LongIdentifier> Users { get; set; }
}

public class LongIdentifier
{
    [BsonElement("low")]
    public ulong Low { get; set; }

    [BsonElement("high")]
    public ulong High { get; set; }

    [BsonElement("unsigned")]
    public bool Unsigned { get; set; }
}