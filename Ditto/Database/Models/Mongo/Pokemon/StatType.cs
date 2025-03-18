using Ditto.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class StatType
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")]
    public int StatId { get; set; }

    [BsonElement("damage_class_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? DamageClassId { get; set; }

    [BsonElement("identifier")]
    public string Identifier { get; set; }

    [BsonElement("is_battle_only")]
    public int IsBattleOnly { get; set; }

    [BsonElement("game_index")]
    public int GameIndex { get; set; }
}