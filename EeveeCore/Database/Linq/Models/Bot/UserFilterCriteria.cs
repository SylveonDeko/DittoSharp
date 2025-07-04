using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents an individual filter criterion within a user's filter group.
///     Each criterion defines a condition that Pokemon must match to be included in the group.
/// </summary>
[Table("user_filter_criteria")]
public class UserFilterCriteria
{
    /// <summary>
    ///     Gets or sets the unique identifier for this filter criterion.
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the filter group this criterion belongs to.
    /// </summary>
    [Column("filter_group_id")]
    [NotNull]
    public int FilterGroupId { get; set; }

    /// <summary>
    ///     Gets or sets the Pokemon field name this criterion applies to.
    ///     Examples: "level", "hp_iv", "shiny", "nature", "type", "caught_at"
    /// </summary>
    [Column("field_name")]
    [NotNull]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the comparison operator for this criterion.
    ///     Examples: "equals", "not_equals", "greater_than", "less_than", "between", "contains", "in_list"
    /// </summary>
    [Column("operator")]
    [NotNull]
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the text value for string-based comparisons.
    /// </summary>
    [Column("value_text")]
    public string? ValueText { get; set; }

    /// <summary>
    ///     Gets or sets the numeric value for number-based comparisons.
    /// </summary>
    [Column("value_numeric")]
    public int? ValueNumeric { get; set; }

    /// <summary>
    ///     Gets or sets the maximum numeric value for range-based comparisons.
    /// </summary>
    [Column("value_numeric_max")]
    public int? ValueNumericMax { get; set; }

    /// <summary>
    ///     Gets or sets the boolean value for true/false comparisons.
    /// </summary>
    [Column("value_boolean")]
    public bool? ValueBoolean { get; set; }

    /// <summary>
    ///     Gets or sets the logical connector to the next criterion ("AND" or "OR").
    ///     Null for the last criterion in a group.
    /// </summary>
    [Column("logical_connector")]
    public string? LogicalConnector { get; set; } = "AND";

    /// <summary>
    ///     Gets or sets the order of this criterion within the filter group.
    /// </summary>
    [Column("criterion_order")]
    [NotNull]
    public int CriterionOrder { get; set; } = 0;
}