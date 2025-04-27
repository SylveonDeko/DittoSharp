using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents the collection of moves that a specific Pokémon can learn.
/// </summary>
public class PokemonMoves
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier or name of the Pokémon.
    /// </summary>
    [BsonElement("pokemon")]
    public string Pokemon { get; set; }

    /// <summary>
    /// Gets or sets the list of move identifiers that this Pokémon can learn.
    /// </summary>
    [BsonElement("moves")]
    public List<string> Moves { get; set; }
}