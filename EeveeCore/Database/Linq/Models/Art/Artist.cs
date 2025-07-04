using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Art;

/// <summary>
///     Represents an artist entry in the LINQ2DB database model.
/// </summary>
[Table("artists")]
public class Artist
{
    /// <summary>
    ///     Gets or sets the unique identifier for the artist.
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the artist.
    /// </summary>
    [Column("artist")]
    public string? ArtistName { get; set; }

    /// <summary>
    ///     Gets or sets the Pokémon featured in the artist's work.
    /// </summary>
    [Column("pokemon")]
    public string? Pokemon { get; set; }

    /// <summary>
    ///     Gets or sets the URL link to the artist's work or profile.
    /// </summary>
    [Column("link")]
    public string? Link { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this artist's work is currently in use.
    /// </summary>
    [Column("in_use")]
    public bool InUse { get; set; }
}