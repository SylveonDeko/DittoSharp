using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ditto.Database.Models.Mongo.Discord;

public class GuildConfig
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("id")] public ulong GuildId { get; set; }

    [BsonElement("redirects")] public List<ulong> Redirects { get; set; } = [];

    [BsonElement("delete_spawns")] public bool DeleteSpawns { get; set; }

    [BsonElement("pin_spawns")] public bool PinSpawns { get; set; }

    [BsonElement("disabled_spawn_channels")]
    public List<ulong> DisabledSpawnChannels { get; set; } = [];

    [BsonElement("small_images")] public bool SmallImages { get; set; }

    [BsonElement("modal_view")] public bool ModalView { get; set; }

    [BsonElement("enabled_channels")] public List<ulong> EnabledChannels { get; set; } = [];

    [BsonElement("enable_spawns_all")] public bool EnableSpawnsAll { get; set; }

    [BsonElement("speed")] public int Speed { get; set; } = 10;

    [BsonElement("locale")] public string Locale { get; set; } = "en";
}