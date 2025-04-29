using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
///     Represents a user's collection of Pokémon eggs in the EeveeCore system.
///     This class tracks various species of eggs the user has collected.
/// </summary>
[Table("eggs")]
public class Eggs
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this egg collection.
    ///     This serves as the primary key for the egg collection record.
    /// </summary>
    [Key]
    [Column("u_id")]
    public long UserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of Bidoof eggs the user has.
    /// </summary>
    [Column("bidoof")]
    public int Bidoof { get; set; }

    /// <summary>
    ///     Gets or sets the number of Caterpie eggs the user has.
    /// </summary>
    [Column("caterpie")]
    public int Caterpie { get; set; }

    /// <summary>
    ///     Gets or sets the number of Pidgey eggs the user has.
    /// </summary>
    [Column("pidgey")]
    public int Pidgey { get; set; }

    /// <summary>
    ///     Gets or sets the number of Magikarp eggs the user has.
    /// </summary>
    [Column("magikarp")]
    public int Magikarp { get; set; }

    /// <summary>
    ///     Gets or sets the number of Spinarak eggs the user has.
    /// </summary>
    [Column("spinarak")]
    public int Spinarak { get; set; }

    /// <summary>
    ///     Gets or sets the number of Tentacruel eggs the user has.
    /// </summary>
    [Column("tentacruel")]
    public int Tentacruel { get; set; }

    /// <summary>
    ///     Gets or sets the number of Togepi eggs the user has.
    /// </summary>
    [Column("togepi")]
    public int Togepi { get; set; }

    /// <summary>
    ///     Gets or sets the number of Bellsprout eggs the user has.
    /// </summary>
    [Column("bellsprout")]
    public int Bellsprout { get; set; }

    /// <summary>
    ///     Gets or sets the number of Chansey eggs the user has.
    /// </summary>
    [Column("chansey")]
    public int Chansey { get; set; }

    /// <summary>
    ///     Gets or sets the number of Omastar eggs the user has.
    /// </summary>
    [Column("omastar")]
    public int Omastar { get; set; }

    /// <summary>
    ///     Gets or sets the number of Cubone eggs the user has.
    /// </summary>
    [Column("cubone")]
    public int Cubone { get; set; }

    /// <summary>
    ///     Gets or sets the number of Farfetch'd eggs the user has.
    /// </summary>
    [Column("farfetchd")]
    public int Farfetchd { get; set; }

    /// <summary>
    ///     Gets or sets the number of Porygon eggs the user has.
    /// </summary>
    [Column("porygon")]
    public int Porygon { get; set; }

    /// <summary>
    ///     Gets or sets the number of Ralts eggs the user has.
    /// </summary>
    [Column("ralts")]
    public int Ralts { get; set; }

    /// <summary>
    ///     Gets or sets the number of Dratini eggs the user has.
    /// </summary>
    [Column("dratini")]
    public int Dratini { get; set; }

    /// <summary>
    ///     Gets or sets the number of Larvitar eggs the user has.
    /// </summary>
    [Column("larvitar")]
    public int Larvitar { get; set; }

    /// <summary>
    ///     Gets or sets the number of Bagon eggs the user has.
    /// </summary>
    [Column("bagon")]
    public int Bagon { get; set; }

    /// <summary>
    ///     Gets or sets the number of Gible eggs the user has.
    /// </summary>
    [Column("gible")]
    public int Gible { get; set; }

    /// <summary>
    ///     Gets or sets the number of Kyogre eggs the user has.
    /// </summary>
    [Column("kyogre")]
    public int Kyogre { get; set; }

    /// <summary>
    ///     Gets or sets the number of Dialga eggs the user has.
    /// </summary>
    [Column("dialga")]
    public int Dialga { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the user has obtained a radiant Pokémon from eggs.
    /// </summary>
    [Column("got_radiant")]
    public bool GotRadiant { get; set; }
}