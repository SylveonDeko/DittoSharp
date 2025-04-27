using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
/// Represents a month record for tracking time-based game data.
/// </summary>
public class Month
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier for the month.
    /// </summary>
    [BsonElement("id")]
    public int MonthId { get; set; }

    /// <summary>
    /// Gets or sets the string representation of the month and year (e.g., "03-2025").
    /// </summary>
    [BsonElement("m-y")]
    public string MonthYear { get; set; }
}