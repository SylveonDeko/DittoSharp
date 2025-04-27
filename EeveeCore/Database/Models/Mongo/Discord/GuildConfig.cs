using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Discord;

/// <summary>
/// Represents Discord guild configuration settings in the MongoDB database.
/// </summary>
public class GuildConfig
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Gets or sets the Discord guild identifier.
    /// </summary>
    [BsonElement("id")]
    public ulong GuildId { get; set; }

    /// <summary>
    /// Gets or sets the list of channel IDs where bot responses should be redirected.
    /// </summary>
    [BsonElement("redirects")]
    public List<ulong> Redirects { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether Pokémon spawn messages should be deleted after capture.
    /// </summary>
    [BsonElement("delete_spawns")]
    public bool DeleteSpawns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Pokémon spawn messages should be pinned.
    /// </summary>
    [BsonElement("pin_spawns")]
    public bool PinSpawns { get; set; }

    /// <summary>
    /// Gets or sets the list of channel IDs where Pokémon spawns are disabled.
    /// </summary>
    [BsonElement("disabled_spawn_channels")]
    public List<ulong> DisabledSpawnChannels { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether smaller Pokémon images should be used.
    /// </summary>
    [BsonElement("small_images")]
    public bool SmallImages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use modal dialogs for interaction views.
    /// </summary>
    [BsonElement("modal_view")]
    public bool ModalView { get; set; }

    /// <summary>
    /// Gets or sets the list of channel IDs where bot commands are explicitly enabled.
    /// </summary>
    [BsonElement("enabled_channels")]
    public List<ulong> EnabledChannels { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether Pokémon spawns are enabled in all channels.
    /// </summary>
    [BsonElement("enable_spawns_all")]
    public bool EnableSpawnsAll { get; set; }

    /// <summary>
    /// Gets or sets the spawn rate speed value for this guild.
    /// Default value is 10.
    /// </summary>
    [BsonElement("speed")]
    public int Speed { get; set; } = 10;

    /// <summary>
    /// Gets or sets the locale/language setting for this guild.
    /// Default value is "en" (English).
    /// </summary>
    [BsonElement("locale")]
    public string Locale { get; set; } = "en";
}