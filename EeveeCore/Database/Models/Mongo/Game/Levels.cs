using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
/// Represents level requirements and associated titles in the MongoDB database.
/// </summary>
public class Levels
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the dictionary mapping level identifiers to title strings.
    /// </summary>
    [BsonElement("titles")]
    public Dictionary<string, string> Titles { get; set; }

    /// <summary>
    /// Gets or sets additional level requirement mappings not explicitly defined in the schema.
    /// </summary>
    [BsonExtraElements]
    public IDictionary<string, int> LevelRequirements { get; set; }
}