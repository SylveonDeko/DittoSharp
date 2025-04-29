using EeveeCore.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
///     Represents core data for a Pokémon species including its biological traits and characteristics.
/// </summary>
public class PokemonFile
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the numeric identifier for this Pokémon species.
    /// </summary>
    [BsonElement("id")]
    public int? PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the string identifier or name of the Pokémon.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    ///     Gets or sets the generation in which this Pokémon was introduced.
    /// </summary>
    [BsonElement("generation_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? GenerationId { get; set; }

    /// <summary>
    ///     Gets or sets the species identifier that this Pokémon evolves from, if any.
    /// </summary>
    [BsonElement("evolves_from_species_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? EvolvesFromSpeciesId { get; set; }

    /// <summary>
    ///     Gets or sets the evolution chain identifier this Pokémon belongs to.
    /// </summary>
    [BsonElement("evolution_chain_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? EvolutionChainId { get; set; }

    /// <summary>
    ///     Gets or sets the color identifier for this Pokémon.
    /// </summary>
    [BsonElement("color_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? ColorId { get; set; }

    /// <summary>
    ///     Gets or sets the body shape identifier for this Pokémon.
    /// </summary>
    [BsonElement("shape_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? ShapeId { get; set; }

    /// <summary>
    ///     Gets or sets the habitat identifier where this Pokémon is typically found.
    /// </summary>
    [BsonElement("habitat_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? HabitatId { get; set; }

    /// <summary>
    ///     Gets or sets the gender distribution rate for this Pokémon.
    /// </summary>
    [BsonElement("gender_rate")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? GenderRate { get; set; }

    /// <summary>
    ///     Gets or sets the base capture rate for this Pokémon.
    /// </summary>
    [BsonElement("capture_rate")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? CaptureRate { get; set; }

    /// <summary>
    ///     Gets or sets the base happiness value for this Pokémon when caught.
    /// </summary>
    [BsonElement("base_happiness")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? BaseHappiness { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon is classified as a baby.
    /// </summary>
    [BsonElement("is_baby")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? IsBaby { get; set; }

    /// <summary>
    ///     Gets or sets the number of egg cycles required to hatch this Pokémon.
    /// </summary>
    [BsonElement("hatch_counter")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? HatchCounter { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon has visual differences between genders.
    /// </summary>
    [BsonElement("has_gender_differences")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? HasGenderDifferences { get; set; }

    /// <summary>
    ///     Gets or sets the growth rate identifier for this Pokémon's experience curve.
    /// </summary>
    [BsonElement("growth_rate_id")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? GrowthRateId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon can switch between forms.
    /// </summary>
    [BsonElement("forms_switchable")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? FormsSwitchable { get; set; }

    /// <summary>
    ///     Gets or sets the display order for this Pokémon in listings.
    /// </summary>
    [BsonElement("order")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? Order { get; set; }

    /// <summary>
    ///     Gets or sets the display order for this Pokémon in Pokémon Conquest.
    /// </summary>
    [BsonElement("conquest_order")]
    [BsonSerializer(typeof(NullableIntSerializer))]
    public int? ConquestOrder { get; set; }

    /// <summary>
    ///     Gets or sets the variant identifier for regional or special forms of this Pokémon.
    /// </summary>
    [BsonElement("variant")]
    public string? Variant { get; set; }

    /// <summary>
    ///     Gets or sets the list of elemental types for this Pokémon.
    /// </summary>
    [BsonElement("types")]
    public List<string> Types { get; set; }
}