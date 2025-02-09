using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Game;

public class ShopItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("item")]
    public string Item { get; set; }

    [BsonElement("price")]
    public int Price { get; set; }

    [BsonElement("type")]
    public int Type { get; set; }

    [BsonElement("second_type")]
    public int SecondType { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }
}