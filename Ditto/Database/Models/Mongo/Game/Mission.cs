using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class Mission
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }

    [BsonElement("target")]
    public int Target { get; set; }

    [BsonElement("reward")]
    public int Reward { get; set; }

    [BsonElement("active")]
    public bool Active { get; set; }

    [BsonElement("key")]
    public string Key { get; set; }

    [BsonElement("m_id")]
    public int MissionId { get; set; }

    [BsonElement("iv")]
    public int Iv { get; set; }

    [BsonElement("started_epoch")]
    public ulong StartedEpoch { get; set; }
}