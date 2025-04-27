using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
/// Represents a placeholder for a radiant Pokémon in the MongoDB database.
/// </summary>
public class RadiantPlaceholder
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the radiant Pokémon.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; }
}