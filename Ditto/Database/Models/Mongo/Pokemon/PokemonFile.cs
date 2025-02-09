using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class PokemonFile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")]
    public int? PokemonId { get; set; }

    [BsonElement("identifier")]
    public string Identifier { get; set; }

    [BsonElement("generation_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? GenerationId { get; set; }

    [BsonElement("evolves_from_species_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? EvolvesFromSpeciesId { get; set; }

    [BsonElement("evolution_chain_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? EvolutionChainId { get; set; }

    [BsonElement("color_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? ColorId { get; set; }

    [BsonElement("shape_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? ShapeId { get; set; }

    [BsonElement("habitat_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? HabitatId { get; set; }

    [BsonElement("gender_rate")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? GenderRate { get; set; }

    [BsonElement("capture_rate")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? CaptureRate { get; set; }

    [BsonElement("base_happiness")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? BaseHappiness { get; set; }

    [BsonElement("is_baby")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? IsBaby { get; set; }

    [BsonElement("hatch_counter")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? HatchCounter { get; set; }

    [BsonElement("has_gender_differences")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? HasGenderDifferences { get; set; }

    [BsonElement("growth_rate_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? GrowthRateId { get; set; }

    [BsonElement("forms_switchable")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? FormsSwitchable { get; set; }

    [BsonElement("order")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? Order { get; set; }

    [BsonElement("conquest_order")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? ConquestOrder { get; set; }

    [BsonElement("variant")]
    public string? Variant { get; set; }

    [BsonElement("types")]
    public List<string> Types { get; set; }
}