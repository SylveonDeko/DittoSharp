using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a collection of banned users in the EeveeCore Pokémon bot system.
///     Tracks users who are banned from using the bot and users who are banned from duels.
/// </summary>
[Table("botbans")]
public class BotBan
{
    /// <summary>
    ///     Gets or sets the array of user IDs that are completely banned from using the bot.
    /// </summary>
    [Column("users", DataType = LinqToDB.DataType.Array)]
    [NotNull]
    public ulong[] Users { get; set; } = [];

    /// <summary>
    ///     Gets or sets the array of user IDs that are banned from participating in duels.
    ///     These users may still use other bot features.
    /// </summary>
    [Column("duelbans", DataType = LinqToDB.DataType.Array)]
    [NotNull]
    public ulong[] DuelBans { get; set; } = [];
}