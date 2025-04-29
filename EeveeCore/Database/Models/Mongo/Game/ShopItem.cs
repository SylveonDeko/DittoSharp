using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Game;

/// <summary>
///     Represents an item available for purchase in the in-game shop.
/// </summary>
public class ShopItem
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the name or identifier of the item.
    /// </summary>
    [BsonElement("item")]
    public string? Item { get; set; }

    /// <summary>
    ///     Gets or sets the price of the item in the game's currency.
    /// </summary>
    [BsonElement("price")]
    public int Price { get; set; }

    /// <summary>
    ///     Gets or sets the primary type identifier for the item.
    /// </summary>
    [BsonElement("type")]
    public int Type { get; set; }

    /// <summary>
    ///     Gets or sets the secondary type identifier for the item.
    /// </summary>
    [BsonElement("second_type")]
    public int SecondType { get; set; }

    /// <summary>
    ///     Gets or sets the description of the item and its effects.
    /// </summary>
    [BsonElement("description")]
    public string Description { get; set; }
}