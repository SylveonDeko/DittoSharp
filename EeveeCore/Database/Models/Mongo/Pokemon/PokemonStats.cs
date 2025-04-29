using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
///     Represents the base stat values for a specific Pokémon.
/// </summary>
public class PokemonStats
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the Pokémon identifier.
    /// </summary>
    [BsonElement("pokemon_id")]
    public int PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the list of base stat values in order (HP, Attack, Defense, Sp.Attack, Sp.Defense, Speed).
    /// </summary>
    [BsonElement("stats")]
    public List<int> Stats { get; set; }
}