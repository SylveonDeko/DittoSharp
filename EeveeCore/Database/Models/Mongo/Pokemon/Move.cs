using EeveeCore.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
///     Represents a Pokémon move with its attributes and battle effects.
/// </summary>
public class Move
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the numeric identifier for this move.
    /// </summary>
    [BsonElement("id")]
    public int MoveId { get; set; }

    /// <summary>
    ///     Gets or sets the string identifier or name of the move.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    ///     Gets or sets the generation in which this move was introduced.
    /// </summary>
    [BsonElement("generation_id")]
    public int GenerationId { get; set; }

    /// <summary>
    ///     Gets or sets the elemental type identifier of this move.
    /// </summary>
    [BsonElement("type_id")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Gets or sets the base power of this move, if applicable.
    /// </summary>
    [BsonElement("power")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? Power { get; set; }

    /// <summary>
    ///     Gets or sets the base Power Points (PP) value for this move.
    /// </summary>
    [BsonElement("pp")]
    public int PP { get; set; }

    /// <summary>
    ///     Gets or sets the accuracy percentage of this move, if applicable.
    /// </summary>
    [BsonElement("accuracy")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? Accuracy { get; set; }

    /// <summary>
    ///     Gets or sets the priority value that affects move order in battle.
    /// </summary>
    [BsonElement("priority")]
    public int Priority { get; set; }

    /// <summary>
    ///     Gets or sets the target identifier indicating what this move can target in battle.
    /// </summary>
    [BsonElement("target_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? TargetId { get; set; }

    /// <summary>
    ///     Gets or sets the damage class identifier (physical, special, or status).
    /// </summary>
    [BsonElement("damage_class_id")]
    public int DamageClassId { get; set; }

    /// <summary>
    ///     Gets or sets the effect identifier that determines the move's secondary effects.
    /// </summary>
    [BsonElement("effect_id")]
    public int EffectId { get; set; }

    /// <summary>
    ///     Gets or sets the percentage chance of the secondary effect occurring, if applicable.
    /// </summary>
    [BsonElement("effect_chance")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? EffectChance { get; set; }

    /// <summary>
    ///     Gets or sets the critical hit rate modifier for this move.
    /// </summary>
    [BsonElement("crit_rate")]
    public int CritRate { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of hits for multi-hit moves, if applicable.
    /// </summary>
    [BsonElement("max_hits")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? MaxHits { get; set; }

    /// <summary>
    ///     Gets or sets the minimum number of hits for multi-hit moves, if applicable.
    /// </summary>
    [BsonElement("min_hits")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? MinHits { get; set; }
}