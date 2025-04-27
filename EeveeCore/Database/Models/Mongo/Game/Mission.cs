using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
/// Represents a mission or quest that users can complete in the game.
/// </summary>
public class Mission
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the mission.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the detailed description of what the mission entails.
    /// </summary>
    [BsonElement("description")]
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the target value that must be reached to complete the mission.
    /// </summary>
    [BsonElement("target")]
    public int Target { get; set; }

    /// <summary>
    /// Gets or sets the reward amount granted upon mission completion.
    /// </summary>
    [BsonElement("reward")]
    public int Reward { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mission is currently active.
    /// </summary>
    [BsonElement("active")]
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets the unique key identifying the mission type.
    /// </summary>
    [BsonElement("key")]
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier for the mission.
    /// </summary>
    [BsonElement("m_id")]
    public int MissionId { get; set; }

    /// <summary>
    /// Gets or sets the IV (Individual Value) requirement or bonus associated with the mission.
    /// </summary>
    [BsonElement("iv")]
    public int Iv { get; set; }

    /// <summary>
    /// Gets or sets the Unix timestamp when the mission was started.
    /// </summary>
    [BsonElement("started_epoch")]
    public ulong StartedEpoch { get; set; }
}