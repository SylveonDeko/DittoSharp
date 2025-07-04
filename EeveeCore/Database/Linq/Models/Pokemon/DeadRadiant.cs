using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents a record of a discontinued or removed radiant Pokémon in the EeveeCore system.
///     This class tracks radiant Pokémon species that are no longer available or have been modified.
/// </summary>
[Table("dead_radiants")]
public class DeadRadiant
{
    /// <summary>
    ///     Gets or sets the unique identifier for this dead radiant record.
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the Pokémon species.
    /// </summary>
    [Column("pokemon")]
    [NotNull]
    public string Pokemon { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the number of this radiant species that have been removed or discontinued.
    /// </summary>
    [Column("dead")]
    [Nullable]
    public int? Dead { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon types associated with this radiant species.
    /// </summary>
    [Column("types", DbType = "text[]")]
    [Nullable]
    public string[]? Types { get; set; }
}