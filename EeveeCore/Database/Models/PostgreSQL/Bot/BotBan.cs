using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
///     Represents a collection of banned users in the EeveeCore Pokémon bot system.
///     Tracks users who are banned from using the bot and users who are banned from duels.
/// </summary>
[Table("botbans")]
[Keyless]
public class BotBan
{
    /// <summary>
    ///     Gets or sets the array of user IDs that are completely banned from using the bot.
    /// </summary>
    [Column("users", TypeName = "bigint[]")]
    [Required]
    public long[] Users { get; set; } = [];

    /// <summary>
    ///     Gets or sets the array of user IDs that are banned from participating in duels.
    ///     These users may still use other bot features.
    /// </summary>
    [Column("duelbans", TypeName = "bigint[]")]
    [Required]
    public long[] DuelBans { get; set; } = [];
}