using EeveeCore.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents an item in the game with its properties and effects.
/// </summary>
public class Item
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier of the item.
    /// </summary>
    [BsonElement("id")]
    public int ItemId { get; set; }

    /// <summary>
    /// Gets or sets the string identifier or name of the item.
    /// </summary>
    [BsonElement("identifier")]
    public string? Identifier { get; set; }

    /// <summary>
    /// Gets or sets the category identifier this item belongs to.
    /// </summary>
    [BsonElement("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the purchase cost of the item in the game's currency.
    /// </summary>
    [BsonElement("cost")]
    public int Cost { get; set; }

    /// <summary>
    /// Gets or sets the power of this item when flung in battle, if applicable.
    /// </summary>
    [BsonElement("fling_power")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? FlingPower { get; set; }

    /// <summary>
    /// Gets or sets the effect identifier when this item is flung in battle, if applicable.
    /// </summary>
    [BsonElement("fling_effect_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? FlingEffectId { get; set; }
}