using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
///     Represents evolution data and requirements for Pokémon.
/// </summary>
public class Evolution
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the numeric identifier for this evolution.
    /// </summary>
    [BsonElement("id")]
    public int EvolutionId { get; set; }

    /// <summary>
    ///     Gets or sets the species identifier of the evolved Pokémon.
    /// </summary>
    [BsonElement("evolved_species_id")]
    public int EvolvedSpeciesId { get; set; }

    /// <summary>
    ///     Gets or sets the identifier for the evolution trigger mechanism.
    /// </summary>
    [BsonElement("evolution_trigger_id")]
    public int EvolutionTriggerId { get; set; }

    /// <summary>
    ///     Gets or sets the item identifier required to trigger the evolution, if any.
    /// </summary>
    [BsonElement("trigger_item_id")]
    public int? TriggerItemId { get; set; }

    /// <summary>
    ///     Gets or sets the minimum level required for evolution, if applicable.
    /// </summary>
    [BsonElement("minimum_level")]
    public int? MinimumLevel { get; set; }

    /// <summary>
    ///     Gets or sets the gender identifier required for evolution, if applicable.
    /// </summary>
    [BsonElement("gender_id")]
    public int GenderId { get; set; }

    /// <summary>
    ///     Gets or sets the location identifier where evolution must occur, if applicable.
    /// </summary>
    [BsonElement("location_id")]
    public int? LocationId { get; set; }

    /// <summary>
    ///     Gets or sets the item identifier that must be held for evolution, if applicable.
    /// </summary>
    [BsonElement("held_item_id")]
    public int? HeldItemId { get; set; }

    /// <summary>
    ///     Gets or sets the time of day when evolution can occur, if applicable.
    /// </summary>
    [BsonElement("time_of_day")]
    public string TimeOfDay { get; set; }

    /// <summary>
    ///     Gets or sets the move identifier that must be known for evolution, if applicable.
    /// </summary>
    [BsonElement("known_move_id")]
    public int? KnownMoveId { get; set; }

    /// <summary>
    ///     Gets or sets the move type identifier that must be known for evolution, if applicable.
    /// </summary>
    [BsonElement("known_move_type_id")]
    public int? KnownMoveTypeId { get; set; }

    /// <summary>
    ///     Gets or sets the minimum happiness value required for evolution, if applicable.
    /// </summary>
    [BsonElement("minimum_happiness")]
    public int? MinimumHappiness { get; set; }

    /// <summary>
    ///     Gets or sets the minimum beauty value required for evolution, if applicable.
    /// </summary>
    [BsonElement("minimum_beauty")]
    public int? MinimumBeauty { get; set; }

    /// <summary>
    ///     Gets or sets the minimum affection value required for evolution, if applicable.
    /// </summary>
    [BsonElement("minimum_affection")]
    public int? MinimumAffection { get; set; }

    /// <summary>
    ///     Gets or sets the relative physical stats comparison required for evolution, if applicable.
    /// </summary>
    [BsonElement("relative_physical_stats")]
    public int? RelativePhysicalStats { get; set; }

    /// <summary>
    ///     Gets or sets the party species identifier required for evolution, if applicable.
    /// </summary>
    [BsonElement("party_species_id")]
    public int? PartySpeciesId { get; set; }

    /// <summary>
    ///     Gets or sets the party type identifier required for evolution, if applicable.
    /// </summary>
    [BsonElement("party_type_id")]
    public int? PartyTypeId { get; set; }

    /// <summary>
    ///     Gets or sets the traded species identifier required for evolution, if applicable.
    /// </summary>
    [BsonElement("trade_species_id")]
    public int? TradeSpeciesId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether overworld rain is required for evolution.
    /// </summary>
    [BsonElement("needs_overworld_rain")]
    public int NeedsOverworldRain { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the device must be turned upside down for evolution.
    /// </summary>
    [BsonElement("turn_upside_down")]
    public int TurnUpsideDown { get; set; }

    /// <summary>
    ///     Gets or sets the region where the evolution can occur, if region-specific.
    /// </summary>
    [BsonElement("region")]
    public string Region { get; set; }
}