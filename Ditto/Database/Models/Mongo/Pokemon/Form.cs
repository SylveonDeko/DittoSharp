using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class Form
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("base_id")] public int? BaseId { get; set; }

    [BsonElement("form_identifier")] public string FormIdentifier { get; set; }

    [BsonElement("form_order")] public int? FormOrder { get; set; }

    [BsonElement("id")] public int FormId { get; set; }

    [BsonElement("identifier")] public string Identifier { get; set; }

    [BsonElement("introduced_in_version_group_id")]
    public int? IntroducedInVersionGroupId { get; set; }

    [BsonElement("is_battle_only")] public int? IsBattleOnly { get; set; }

    [BsonElement("is_default")] public int? IsDefault { get; set; }

    [BsonElement("is_mega")] public int? IsMega { get; set; }

    [BsonElement("order")] public int? Order { get; set; }

    [BsonElement("pokemon_id")] public int PokemonId { get; set; }

    [BsonElement("weight")] public int? Weight { get; set; }
}