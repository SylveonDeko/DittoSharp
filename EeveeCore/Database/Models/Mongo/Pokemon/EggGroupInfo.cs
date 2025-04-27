using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents metadata about a specific egg group.
/// </summary>
public class EggGroupInfo
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier of the egg group.
    /// </summary>
    [BsonElement("id")]
    public int GroupId { get; set; }

    /// <summary>
    /// Gets or sets the string identifier or name of the egg group.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }
}