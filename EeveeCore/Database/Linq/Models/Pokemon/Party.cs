using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Pokemon;

/// <summary>
///     Represents a customized team of Pokémon in the EeveeCore Pokémon bot system.
///     This class allows users to create and save different party configurations for various purposes.
/// </summary>
[Table("partys")]
public class Party
{
    /// <summary>
    ///     Gets or sets the unique identifier for this party configuration.
    /// </summary>
    [PrimaryKey]
    [Column("p_id")]
    public int PartyId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the party owner.
    /// </summary>
    [Column("u_id")]
    [NotNull]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon in slot 1 of the party.
    /// </summary>
    [Column("slot1")]
    public ulong? Slot1 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon in slot 2 of the party.
    /// </summary>
    [Column("slot2")]
    public ulong? Slot2 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon in slot 3 of the party.
    /// </summary>
    [Column("slot3")]
    public ulong? Slot3 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon in slot 4 of the party.
    /// </summary>
    [Column("slot4")]
    public ulong? Slot4 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon in slot 5 of the party.
    /// </summary>
    [Column("slot5")]
    public ulong? Slot5 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon in slot 6 of the party.
    /// </summary>
    [Column("slot6")]
    public ulong? Slot6 { get; set; }

    /// <summary>
    ///     Gets or sets the custom name given to this party configuration.
    /// </summary>
    [Column("name")]
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this is a quick-access party.
    ///     Quick parties may be more easily accessible in battles or other gameplay.
    /// </summary>
    [Column("quick")]
    [NotNull]
    public bool Quick { get; set; }
}