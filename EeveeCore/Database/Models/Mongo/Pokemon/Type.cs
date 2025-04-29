using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
///     Represents an elemental type in the Pokémon game.
/// </summary>
public class Type
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the numeric identifier for this type.
    /// </summary>
    [BsonElement("id")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Gets or sets the string identifier or name of the type.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    ///     Gets or sets the generation in which this type was introduced.
    /// </summary>
    [BsonElement("generation_id")]
    public int? GenerationId { get; set; }

    /// <summary>
    ///     Gets or sets the damage class identifier associated with this type.
    /// </summary>
    [BsonElement("damage_class_id")]
    public int? DamageClassId { get; set; }
}