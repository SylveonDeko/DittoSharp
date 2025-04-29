using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
///     Represents a Pokémon ability with its properties and metadata.
/// </summary>
public class Ability
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the numeric identifier of the ability.
    /// </summary>
    [BsonElement("id")]
    public int AbilityId { get; set; }

    /// <summary>
    ///     Gets or sets the string identifier or name of the ability.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    ///     Gets or sets the generation in which this ability was introduced.
    /// </summary>
    [BsonElement("generation_id")]
    public int? GenerationId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this ability appears in the main series games.
    /// </summary>
    [BsonElement("is_main_series")]
    public int? IsMainSeries { get; set; }
}