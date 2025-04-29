using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
///     Represents a Discord channel where the EeveeCore Pokémon bot is disabled.
///     Used to track channels where bot commands and functionality should not be active.
/// </summary>
[Table("disabled_channels")]
public class DisabledChannel
{
    /// <summary>
    ///     Gets or sets the Discord channel ID where the bot is disabled.
    ///     This is the primary key for the table.
    /// </summary>
    [Key]
    [Column("channel")]
    public ulong ChannelId { get; set; }
}