using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents a user-defined filter group for Pokemon collections.
///     Filter groups are dynamic collections that automatically show Pokemon matching custom criteria.
/// </summary>
[Table("user_filter_groups")]
public class UserFilterGroup
{
    /// <summary>
    ///     Gets or sets the unique identifier for this filter group.
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID that owns this filter group.
    /// </summary>
    [Column("user_id")]
    [NotNull]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the display name of this filter group.
    /// </summary>
    [Column("name")]
    [NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets an optional description of what this filter group shows.
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the display color for this filter group (hex color code).
    /// </summary>
    [Column("color")]
    public string? Color { get; set; } = "#3B82F6";

    /// <summary>
    ///     Gets or sets the emoji or icon name for this filter group.
    /// </summary>
    [Column("icon")]
    public string? Icon { get; set; } = "📁";

    /// <summary>
    ///     Gets or sets the display order for this filter group in the user's list.
    /// </summary>
    [Column("sort_order")]
    [NotNull]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    ///     Gets or sets whether this filter group is marked as favorite.
    /// </summary>
    [Column("is_favorite")]
    [NotNull]
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    ///     Gets or sets whether this filter group is currently active/enabled.
    /// </summary>
    [Column("is_active")]
    [NotNull]
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Gets or sets when this filter group was created.
    /// </summary>
    [Column("created_at")]
    [NotNull]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets when this filter group was last updated.
    /// </summary>
    [Column("updated_at")]
    [NotNull]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Navigation property for the filter criteria associated with this group.
    /// </summary>
    [Association(ThisKey = "Id", OtherKey = "FilterGroupId")]
    public virtual List<UserFilterCriteria> FilterCriteria { get; set; } = new();
}