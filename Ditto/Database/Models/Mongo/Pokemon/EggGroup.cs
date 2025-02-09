using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class EggGroup
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("species_id")]
    public int SpeciesId { get; set; }

    [BsonElement("egg_groups")]
    public List<int>? Groups { get; set; }
}