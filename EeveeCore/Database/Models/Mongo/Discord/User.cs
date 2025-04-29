using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Discord;

/// <summary>
///     Represents user data in the Discord context stored in the MongoDB database.
/// </summary>
public class User
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user identifier.
    /// </summary>
    [BsonElement("user")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets a dictionary of user progression values, mapping achievement names to their progress values.
    /// </summary>
    [BsonElement("progress")]
    public Dictionary<string, int> Progress { get; set; }
}