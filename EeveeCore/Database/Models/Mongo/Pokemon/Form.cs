using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents a specific form that a Pokémon can take, with its properties and relationships.
/// </summary>
public class Form
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the base identifier for the form.
    /// </summary>
    [BsonElement("base_id")]
    public int? BaseId { get; set; }

    /// <summary>
    /// Gets or sets the string identifier for this specific form.
    /// </summary>
    [BsonElement("form_identifier")]
    public string FormIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the display order of this form relative to other forms.
    /// </summary>
    [BsonElement("form_order")]
    public int? FormOrder { get; set; }

    /// <summary>
    /// Gets or sets the numeric identifier for this form.
    /// </summary>
    [BsonElement("id")]
    public int FormId { get; set; }

    /// <summary>
    /// Gets or sets the string identifier or full name of this form.
    /// </summary>
    [BsonElement("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the version group identifier in which this form was introduced.
    /// </summary>
    [BsonElement("introduced_in_version_group_id")]
    public int? IntroducedInVersionGroupId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this form only appears in battles.
    /// </summary>
    [BsonElement("is_battle_only")]
    public int? IsBattleOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default form.
    /// </summary>
    [BsonElement("is_default")]
    public int? IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a Mega Evolution form.
    /// </summary>
    [BsonElement("is_mega")]
    public int? IsMega { get; set; }

    /// <summary>
    /// Gets or sets the display order for this form.
    /// </summary>
    [BsonElement("order")]
    public int? Order { get; set; }

    /// <summary>
    /// Gets or sets the Pokémon identifier this form belongs to.
    /// </summary>
    [BsonElement("pokemon_id")]
    public int PokemonId { get; set; }

    /// <summary>
    /// Gets or sets the weight of the Pokémon in this form.
    /// </summary>
    [BsonElement("weight")]
    public int? Weight { get; set; }
}