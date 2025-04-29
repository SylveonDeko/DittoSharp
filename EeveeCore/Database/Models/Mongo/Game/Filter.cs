using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
///     Represents a filter configuration for game commands in the MongoDB database.
/// </summary>
public class Filter
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the filter argument or parameter name.
    /// </summary>
    [BsonElement("arg")]
    public string Argument { get; set; }

    /// <summary>
    ///     Gets or sets the filter value to apply when the argument is matched.
    /// </summary>
    [BsonElement("value")]
    public string Value { get; set; }
}