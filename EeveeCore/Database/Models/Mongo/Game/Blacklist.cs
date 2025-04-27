using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
/// Represents blacklisted guilds and users in the MongoDB database.
/// </summary>
public class Blacklist
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the list of blacklisted guild IDs.
    /// </summary>
    [BsonElement("guilds")]
    public List<ulong> Guilds { get; set; }

    /// <summary>
    /// Gets or sets the list of blacklisted user IDs.
    /// </summary>
    [BsonElement("users")]
    public List<ulong> Users { get; set; }
}