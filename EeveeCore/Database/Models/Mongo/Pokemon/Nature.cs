using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Pokemon;

public class Nature
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public int NatureId { get; set; }

    [BsonElement("identifier")] public string Identifier { get; set; }

    [BsonElement("decreased_stat_id")] public int DecreasedStatId { get; set; }

    [BsonElement("increased_stat_id")] public int IncreasedStatId { get; set; }

    [BsonElement("hates_flavor_id")] public int HatesFlavorId { get; set; }

    [BsonElement("likes_flavor_id")] public int LikesFlavorId { get; set; }

    [BsonElement("game_index")] public int GameIndex { get; set; }
}