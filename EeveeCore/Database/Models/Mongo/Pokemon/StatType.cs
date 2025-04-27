using EeveeCore.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents a stat type and its characteristics in the game.
/// </summary>
public class StatType
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier for this stat type.
    /// </summary>
    [BsonElement("id")]
    public int StatId { get; set; }

    /// <summary>
    /// Gets or sets the damage class identifier associated with this stat, if applicable.
    /// </summary>
    [BsonElement("damage_class_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? DamageClassId { get; set; }

    /// <summary>
    /// Gets or sets the string identifier or name of the stat type.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this stat is only used in battles.
    /// </summary>
    [BsonElement("is_battle_only")]
    public int IsBattleOnly { get; set; }

    /// <summary>
    /// Gets or sets the index of this stat type in game data.
    /// </summary>
    [BsonElement("game_index")]
    public int GameIndex { get; set; }
}