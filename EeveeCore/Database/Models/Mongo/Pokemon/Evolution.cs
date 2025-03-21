using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

public class Evolution
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public int EvolutionId { get; set; }

    [BsonElement("evolved_species_id")] public int EvolvedSpeciesId { get; set; }

    [BsonElement("evolution_trigger_id")] public int EvolutionTriggerId { get; set; }

    [BsonElement("trigger_item_id")] public int? TriggerItemId { get; set; }

    [BsonElement("minimum_level")] public int? MinimumLevel { get; set; }

    [BsonElement("gender_id")] public int GenderId { get; set; }

    [BsonElement("location_id")] public int? LocationId { get; set; }

    [BsonElement("held_item_id")] public int? HeldItemId { get; set; }

    [BsonElement("time_of_day")] public string TimeOfDay { get; set; }

    [BsonElement("known_move_id")] public int? KnownMoveId { get; set; }

    [BsonElement("known_move_type_id")] public int? KnownMoveTypeId { get; set; }

    [BsonElement("minimum_happiness")] public int? MinimumHappiness { get; set; }

    [BsonElement("minimum_beauty")] public int? MinimumBeauty { get; set; }

    [BsonElement("minimum_affection")] public int? MinimumAffection { get; set; }

    [BsonElement("relative_physical_stats")]
    public int? RelativePhysicalStats { get; set; }

    [BsonElement("party_species_id")] public int? PartySpeciesId { get; set; }

    [BsonElement("party_type_id")] public int? PartyTypeId { get; set; }

    [BsonElement("trade_species_id")] public int? TradeSpeciesId { get; set; }

    [BsonElement("needs_overworld_rain")] public int NeedsOverworldRain { get; set; }

    [BsonElement("turn_upside_down")] public int TurnUpsideDown { get; set; }

    [BsonElement("region")] public string Region { get; set; }
}