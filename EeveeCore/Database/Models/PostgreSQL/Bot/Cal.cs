using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
///     Represents a calendar or schedule entry in the EeveeCore Pokémon bot system.
///     Used to track events or activities scheduled for each day of the week.
/// </summary>
[Table("cal")]
public class Cal
{
    /// <summary>
    ///     Gets or sets the unique identifier for this calendar entry.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the week number this calendar entry represents.
    /// </summary>
    [Column("week")]
    public int? Week { get; set; }

    /// <summary>
    ///     Gets or sets the event or activity identifier for Monday.
    /// </summary>
    [Column("monday")]
    public int? Monday { get; set; }

    /// <summary>
    ///     Gets or sets the event or activity identifier for Tuesday.
    /// </summary>
    [Column("tuesday")]
    public int? Tuesday { get; set; }

    /// <summary>
    ///     Gets or sets the event or activity identifier for Wednesday.
    /// </summary>
    [Column("wednesday")]
    public int? Wednesday { get; set; }

    /// <summary>
    ///     Gets or sets the event or activity identifier for Thursday.
    /// </summary>
    [Column("thursday")]
    public int? Thursday { get; set; }

    /// <summary>
    ///     Gets or sets the event or activity identifier for Friday.
    /// </summary>
    [Column("friday")]
    public int? Friday { get; set; }

    /// <summary>
    ///     Gets or sets the event or activity identifier for Saturday.
    /// </summary>
    [Column("saturday")]
    public int? Saturday { get; set; }

    /// <summary>
    ///     Gets or sets the event or activity identifier for Sunday.
    /// </summary>
    [Column("sunday")]
    public int? Sunday { get; set; }
}