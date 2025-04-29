using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Discord;

/// <summary>
///     Represents a Pokémon gym configuration in the MongoDB database.
/// </summary>
public class Gym
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the gym.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the Discord emoji representation of the gym.
    /// </summary>
    [BsonElement("emote")]
    public string Emote { get; set; }

    /// <summary>
    ///     Gets or sets the file path to the gym's image.
    /// </summary>
    [BsonElement("img")]
    public string ImagePath { get; set; }
}