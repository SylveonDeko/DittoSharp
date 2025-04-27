using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
/// Represents an announcement in the EeveeCore Pokémon bot system.
/// Used to store and display important announcements to users.
/// </summary>
[Table("announce")]
public class Announce
{
    /// <summary>
    /// Gets or sets the unique identifier for this announcement.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the main content of the announcement.
    /// </summary>
    [Column("announce")]
    public string? AnnounceText { get; set; }

    /// <summary>
    /// Gets or sets the staff member who created the announcement.
    /// </summary>
    [Column("staff")]
    public string? Staff { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the announcement was created.
    /// </summary>
    [Column("timestamp")]
    public int? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the importance level of the announcement.
    /// Higher values typically indicate more critical announcements.
    /// </summary>
    [Column("imp_level")]
    [Required]
    public int ImportanceLevel { get; set; }

    /// <summary>
    /// Gets or sets the title of the announcement.
    /// Defaults to "Bot Announcement" if not specified.
    /// </summary>
    [Column("title")]
    [Required]
    public string Title { get; set; } = "Bot Announcement";
}