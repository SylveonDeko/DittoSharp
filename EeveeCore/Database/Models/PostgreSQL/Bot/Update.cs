using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
/// Represents a system update or changelog entry for the EeveeCore Pokémon bot.
/// This class tracks changes, additions, and improvements made to the system over time.
/// </summary>
[Table("updates")]
public class Update
{
    /// <summary>
    /// Gets or sets the unique identifier for this update record.
    /// </summary>
    [Key] [Column("id")] public int Id { get; set; }

    /// <summary>
    /// Gets or sets the content or description of the update.
    /// </summary>
    [Column("update")] [Required] public string Content { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date when the update was released.
    /// </summary>
    [Column("update_date")] [Required] public DateOnly UpdateDate { get; set; }

    /// <summary>
    /// Gets or sets the name of the developer responsible for the update.
    /// </summary>
    [Column("dev")] public string? Developer { get; set; }
}