using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Art;

/// <summary>
///     Represents an artist's consent record for using their artwork in the LINQ2DB database model.
/// </summary>
[Table("artists_consent")]
public class ArtistConsent
{
    /// <summary>
    ///     Gets or sets the unique identifier for the consent record.
    /// </summary>
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the name of the artist who provided consent.
    /// </summary>
    [Column("artist")]
    public string Artist { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the artist's DeviantArt username or profile.
    /// </summary>
    [Column("deviantart")]
    public string? DeviantArt { get; set; }

    /// <summary>
    ///     Gets or sets the artist's Instagram username or profile.
    /// </summary>
    [Column("instagram")]
    public string? Instagram { get; set; }

    /// <summary>
    ///     Gets or sets the artist's Twitter/X username or profile.
    /// </summary>
    [Column("twitter")]
    public string? Twitter { get; set; }

    /// <summary>
    ///     Gets or sets the artist's Discord username or ID.
    /// </summary>
    [Column("discord")]
    public string? Discord { get; set; }

    /// <summary>
    ///     Gets or sets additional links provided by the artist.
    /// </summary>
    [Column("other_links")]
    public string[]? OtherLinks { get; set; }

    /// <summary>
    ///     Gets or sets the date when consent was provided.
    /// </summary>
    [Column("date")]
    public DateTime? Date { get; set; }

    /// <summary>
    ///     Gets or sets any comments provided by the artist regarding their consent.
    /// </summary>
    [Column("comment")]
    public string? Comment { get; set; }

    /// <summary>
    ///     Gets or sets additional information about the artist or their work.
    /// </summary>
    [Column("info")]
    public string? Info { get; set; }

    /// <summary>
    ///     Gets or sets the contact information provided by the artist.
    /// </summary>
    [Column("contact")]
    public string? Contact { get; set; }

    /// <summary>
    ///     Gets or sets any extra notes or information related to the consent.
    /// </summary>
    [Column("extra")]
    public string? Extra { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the artist's content is currently in use.
    /// </summary>
    [Column("in_use")]
    public bool? InUse { get; set; }
}