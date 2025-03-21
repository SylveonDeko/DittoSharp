using EeveeCore.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

public class Move
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public int MoveId { get; set; }

    [BsonElement("identifier")] public string Identifier { get; set; }

    [BsonElement("generation_id")] public int GenerationId { get; set; }

    [BsonElement("type_id")] public int TypeId { get; set; }

    [BsonElement("power")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? Power { get; set; }

    [BsonElement("pp")] public int PP { get; set; }

    [BsonElement("accuracy")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? Accuracy { get; set; }

    [BsonElement("priority")] public int Priority { get; set; }

    [BsonElement("target_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? TargetId { get; set; }

    [BsonElement("damage_class_id")] public int DamageClassId { get; set; }

    [BsonElement("effect_id")] public int EffectId { get; set; }

    [BsonElement("effect_chance")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? EffectChance { get; set; }

    [BsonElement("crit_rate")] public int CritRate { get; set; }

    [BsonElement("max_hits")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? MaxHits { get; set; }

    [BsonElement("min_hits")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? MinHits { get; set; }
}