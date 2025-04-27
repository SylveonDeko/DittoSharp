using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents the mapping between a Pokémon and one of its possible abilities.
/// </summary>
public class PokemonAbility
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the Pokémon identifier.
    /// </summary>
    [BsonElement("pokemon_id")]
    public int PokemonId { get; set; }

    /// <summary>
    /// Gets or sets the ability identifier.
    /// </summary>
    [BsonElement("ability_id")]
    public int AbilityId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a hidden ability.
    /// </summary>
    [BsonElement("is_hidden")]
    public int IsHidden { get; set; }

    /// <summary>
    /// Gets or sets the slot position of this ability (1, 2, or hidden).
    /// </summary>
    [BsonElement("slot")]
    public int Slot { get; set; }
}