using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents type effectiveness relationships between different elemental types.
/// </summary>
public class TypeEffectiveness
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the type dealing damage.
    /// </summary>
    [BsonElement("damage_type_id")]
    public int DamageTypeId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the type receiving damage.
    /// </summary>
    [BsonElement("target_type_id")]
    public int TargetTypeId { get; set; }

    /// <summary>
    /// Gets or sets the damage multiplier factor (as a percentage) when the damage type attacks the target type.
    /// </summary>
    /// <remarks>
    /// Common values: 0 (no effect), 50 (not very effective), 100 (normal effectiveness), 200 (super effective).
    /// </remarks>
    [BsonElement("damage_factor")]
    public int DamageFactor { get; set; }
}