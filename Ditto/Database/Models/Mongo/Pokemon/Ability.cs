using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Pokemon;

public class Ability
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")]
    public int AbilityId { get; set; }

    [BsonElement("identifier")]
    public string Identifier { get; set; }

    [BsonElement("generation_id")]
    public int? GenerationId { get; set; }

    [BsonElement("is_main_series")]
    public int? IsMainSeries { get; set; }
}