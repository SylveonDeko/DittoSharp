using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents an active Pokemon spawn in a Discord channel.
///     Used for tracking spawns across bot restarts and preventing race conditions.
/// </summary>
[Table(Name = "ActiveSpawns")]
public class ActiveSpawn
{
    /// <summary>
    ///     The Discord message ID of the spawn message (primary key).
    /// </summary>
    [PrimaryKey]
    [Column(Name = "MessageId"), NotNull]
    public ulong MessageId { get; set; }

    /// <summary>
    ///     The Discord channel ID where the spawn occurred.
    /// </summary>
    [Column(Name = "ChannelId"), NotNull]
    public ulong ChannelId { get; set; }

    /// <summary>
    ///     The Discord guild ID where the spawn occurred.
    /// </summary>
    [Column(Name = "GuildId"), NotNull]
    public ulong GuildId { get; set; }

    /// <summary>
    ///     The name of the spawned Pokemon.
    /// </summary>
    [Column(Name = "PokemonName"), NotNull]
    public string PokemonName { get; set; } = string.Empty;

    /// <summary>
    ///     Whether the spawned Pokemon is shiny.
    /// </summary>
    [Column(Name = "IsShiny"), NotNull]
    public bool IsShiny { get; set; }

    /// <summary>
    ///     The legendary spawn chance value used for this spawn.
    /// </summary>
    [Column(Name = "LegendaryChance"), NotNull]
    public int LegendaryChance { get; set; }

    /// <summary>
    ///     The Ultra Beast spawn chance value used for this spawn.
    /// </summary>
    [Column(Name = "UltraBeastChance"), NotNull]
    public int UltraBeastChance { get; set; }

    /// <summary>
    ///     When the spawn was created.
    /// </summary>
    [Column(Name = "CreatedAt"), NotNull]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Whether this spawn has been caught.
    /// </summary>
    [Column(Name = "IsCaught"), NotNull]
    public bool IsCaught { get; set; } = false;

    /// <summary>
    ///     The user ID who caught this Pokemon, if caught.
    /// </summary>
    [Column(Name = "CaughtByUserId"), Nullable]
    public ulong? CaughtByUserId { get; set; }

    /// <summary>
    ///     When the spawn was caught, if caught.
    /// </summary>
    [Column(Name = "CaughtAt"), Nullable]
    public DateTime? CaughtAt { get; set; }
}