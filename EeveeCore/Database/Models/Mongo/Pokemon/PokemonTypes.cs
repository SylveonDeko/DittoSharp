using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

/// <summary>
/// Represents the elemental types of a Pokémon stored in the MongoDB database.
/// </summary>
[BsonCollection("ptypes")]
public class PokemonTypes
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
    [BsonElement("id")]
    public int PokemonId { get; set; }

    /// <summary>
    /// Gets or sets the list of type identifiers for this Pokémon.
    /// </summary>
    [BsonElement("types")]
    public List<int> Types { get; set; }
}

/// <summary>
/// Specifies the MongoDB collection name to use for a model class.
/// </summary>
/// <param name="collectionName">The name of the MongoDB collection.</param>
[AttributeUsage(AttributeTargets.Class)]
public class BsonCollectionAttribute(string collectionName) : Attribute
{
    /// <summary>
    /// Gets the MongoDB collection name.
    /// </summary>
    public string CollectionName { get; } = collectionName;
}