using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents the egg groups that a specific Pokémon species belongs to.
/// </summary>
public class EggGroup
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the species identifier for the Pokémon.
    /// </summary>
    [BsonElement("species_id")]
    public int SpeciesId { get; set; }

    /// <summary>
    /// Gets or sets the list of egg group identifiers that this species belongs to.
    /// </summary>
    [BsonElement("egg_groups")]
    public List<int>? Groups { get; set; }
}