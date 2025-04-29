using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
///     Represents the current radiant Pokémon available for a specific month in the MongoDB database.
/// </summary>
public class CurrentRadiant
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the list of radiant Pokémon names available for the current period.
    /// </summary>
    [BsonElement("rads")]
    public List<string> Radiants { get; set; }

    /// <summary>
    ///     Gets or sets the month for which these radiants are active.
    /// </summary>
    [BsonElement("month")]
    public int Month { get; set; }

    /// <summary>
    ///     Gets or sets the year for which these radiants are active.
    /// </summary>
    [BsonElement("year")]
    public int Year { get; set; }
}