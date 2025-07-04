using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents shared community resources in the EeveeCore Pokémon bot system.
///     This class manages community-owned assets, rewards, and other shared resources.
/// </summary>
[Table("community")]
public class Community
{
    /// <summary>
    ///     Gets or sets the number of credits available in the community pool.
    /// </summary>
    [Column("credits")]
    public long? Credits { get; set; }

    /// <summary>
    ///     Gets or sets the number of redeems available in the community pool.
    /// </summary>
    [Column("redeems")]
    public long? Redeems { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs that are owned by the community.
    /// </summary>
    [Column("pokes")]
    public ulong[]? Pokes { get; set; }

    /// <summary>
    ///     Gets or sets the array of item IDs that are owned by the community.
    /// </summary>
    [Column("items")]
    public ulong[]? Items { get; set; }

    /// <summary>
    ///     Gets or sets a string containing other community resources not covered by the specific properties.
    ///     May store additional data in JSON or another structured format.
    /// </summary>
    [Column("other")]
    public string? Other { get; set; }
}