using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents a Pokémon nature that affects stat growth and flavor preferences.
/// </summary>
public class Nature
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier for this nature.
    /// </summary>
    [BsonElement("id")]
    public int NatureId { get; set; }

    /// <summary>
    /// Gets or sets the string identifier or name of the nature.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the stat identifier that is decreased by this nature.
    /// </summary>
    [BsonElement("decreased_stat_id")]
    public int DecreasedStatId { get; set; }

    /// <summary>
    /// Gets or sets the stat identifier that is increased by this nature.
    /// </summary>
    [BsonElement("increased_stat_id")]
    public int IncreasedStatId { get; set; }

    /// <summary>
    /// Gets or sets the flavor identifier that this nature dislikes.
    /// </summary>
    [BsonElement("hates_flavor_id")]
    public int HatesFlavorId { get; set; }

    /// <summary>
    /// Gets or sets the flavor identifier that this nature likes.
    /// </summary>
    [BsonElement("likes_flavor_id")]
    public int LikesFlavorId { get; set; }

    /// <summary>
    /// Gets or sets the index of this nature in game data.
    /// </summary>
    [BsonElement("game_index")]
    public int GameIndex { get; set; }
}