using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
///     Represents active boosters in the game stored in the MongoDB database.
/// </summary>
public class Booster
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier key for the booster type.
    /// </summary>
    [BsonElement("key")]
    public string Key { get; set; }

    /// <summary>
    ///     Gets or sets the list of user IDs who have active boosters of this type.
    /// </summary>
    [BsonElement("boosters")]
    public List<ulong> Boosters { get; set; }
}