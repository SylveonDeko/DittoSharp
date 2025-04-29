using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
///     Represents user gift statistics in the MongoDB database.
/// </summary>
public class Gift
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
    [BsonElement("id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of regular gifts received or sent by the user.
    /// </summary>
    [BsonElement("gifts")]
    public int Gifts { get; set; }

    /// <summary>
    ///     Gets or sets the number of shiny Pokémon gifted or received by the user.
    /// </summary>
    [BsonElement("shinies")]
    public int Shinies { get; set; }
}