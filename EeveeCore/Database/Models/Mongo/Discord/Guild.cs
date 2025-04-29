using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EeveeCore.Database.Models.Mongo.Discord;

/// <summary>
///     Represents a Discord guild (server) with its configuration settings in the MongoDB database.
/// </summary>
public class Guild
{
    /// <summary>
    ///     Gets or sets the MongoDB document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord guild identifier.
    /// </summary>
    [BsonElement("id")]
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the command prefix used in this guild.
    /// </summary>
    [BsonElement("prefix")]
    public string Prefix { get; set; }

    /// <summary>
    ///     Gets or sets the list of channel IDs where bot commands are disabled.
    /// </summary>
    [BsonElement("disabled_channels")]
    public List<ulong>? DisabledChannels { get; set; }

    /// <summary>
    ///     Gets or sets the list of channel IDs where bot responses should be redirected.
    /// </summary>
    [BsonElement("redirects")]
    public List<ulong>? Redirects { get; set; }

    /// <summary>
    ///     Gets or sets the list of channel IDs where Pokémon spawns are disabled.
    /// </summary>
    [BsonElement("disabled_spawn_channels")]
    public List<ulong>? DisabledSpawnChannels { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether Pokémon spawn messages should be pinned.
    /// </summary>
    [BsonElement("pin_spawns")]
    public bool PinSpawns { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether Pokémon spawn messages should be deleted after capture.
    /// </summary>
    [BsonElement("delete_spawns")]
    public bool DeleteSpawns { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether smaller Pokémon images should be used.
    /// </summary>
    [BsonElement("small_images")]
    public bool SmallImages { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether level-up notifications should be suppressed.
    /// </summary>
    [BsonElement("silence_levels")]
    public bool SilenceLevels { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to use modal dialogs for interaction views.
    /// </summary>
    [BsonElement("modal_view")]
    public bool ModalView { get; set; }

    /// <summary>
    ///     Gets or sets the list of channel IDs where bot commands are explicitly enabled.
    /// </summary>
    [BsonElement("enabled_channels")]
    public List<ulong>? EnabledChannels { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether Pokémon spawns are enabled in all channels.
    /// </summary>
    [BsonElement("enable_spawns_all")]
    public bool EnableSpawnsAll { get; set; }

    /// <summary>
    ///     Gets or sets the spawn rate speed value for this guild.
    /// </summary>
    [BsonElement("speed")]
    public int Speed { get; set; }

    /// <summary>
    ///     Gets or sets the locale/language setting for this guild.
    /// </summary>
    [BsonElement("locale")]
    public string Locale { get; set; }
}