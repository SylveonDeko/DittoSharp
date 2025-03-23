using EeveeCore.Common.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Discord;

public class Guild
{

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("id")] public ulong GuildId { get; set; }

    [BsonElement("prefix")] public string Prefix { get; set; }

    [BsonElement("disabled_channels")]
    [BsonSerializer(typeof(EmptyListSerializer<ulong>))]
    public List<ulong> DisabledChannels { get; set; }

    [BsonElement("redirects")]
    [BsonSerializer(typeof(EmptyListSerializer<ulong>))]
    public List<ulong> Redirects { get; set; }

    [BsonElement("disabled_spawn_channels")]
    [BsonSerializer(typeof(EmptyListSerializer<ulong>))]
    public List<ulong> DisabledSpawnChannels { get; set; }

    [BsonElement("pin_spawns")] public bool PinSpawns { get; set; }

    [BsonElement("delete_spawns")] public bool DeleteSpawns { get; set; }

    [BsonElement("small_images")] public bool SmallImages { get; set; }

    [BsonElement("silence_levels")] public bool SilenceLevels { get; set; }

    [BsonElement("modal_view")] public bool ModalView { get; set; }

    [BsonElement("enabled_channels")]
    [BsonSerializer(typeof(EmptyListSerializer<ulong>))]
    public List<ulong> EnabledChannels { get; set; }

    [BsonElement("enable_spawns_all")] public bool EnableSpawnsAll { get; set; }

    [BsonElement("speed")] public int Speed { get; set; }

    [BsonElement("locale")] public string Locale { get; set; }
}