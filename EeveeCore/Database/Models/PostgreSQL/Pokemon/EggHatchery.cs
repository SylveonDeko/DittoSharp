using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
///     Represents a user's egg hatchery in the EeveeCore Pokémon bot system.
///     This class tracks the Pokémon eggs in incubation and their hatching progress.
/// </summary>
[Table("egg_hatchery")]
public class EggHatchery
{
    /// <summary>
    ///     Gets or sets the unique identifier for this hatchery record.
    /// </summary>
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the hatchery owner.
    /// </summary>
    [Column("u_id")]
    [Required]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 1 of the hatchery.
    /// </summary>
    [Column("1")]
    public ulong? Slot1 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 2 of the hatchery.
    /// </summary>
    [Column("2")]
    public ulong? Slot2 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 3 of the hatchery.
    /// </summary>
    [Column("3")]
    public ulong? Slot3 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 4 of the hatchery.
    /// </summary>
    [Column("4")]
    public ulong? Slot4 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 5 of the hatchery.
    /// </summary>
    [Column("5")]
    public ulong? Slot5 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 6 of the hatchery.
    /// </summary>
    [Column("6")]
    public ulong? Slot6 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 7 of the hatchery.
    /// </summary>
    [Column("7")]
    public ulong? Slot7 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 8 of the hatchery.
    /// </summary>
    [Column("8")]
    public ulong? Slot8 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 9 of the hatchery.
    /// </summary>
    [Column("9")]
    public ulong? Slot9 { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the egg or Pokémon in slot 10 of the hatchery.
    /// </summary>
    [Column("10")]
    public ulong? Slot10 { get; set; }

    /// <summary>
    ///     Gets or sets the array of egg IDs in this hatchery.
    ///     This may be an alternative representation to the individual slot properties.
    /// </summary>
    [Column("eggs", TypeName = "ulongeger[]")]
    public ulong[]? Eggs { get; set; }

    /// <summary>
    ///     Gets or sets the group or category identifier for this hatchery.
    ///     This may represent an organizational system or hatchery tier.
    /// </summary>
    [Column("group")]
    public short? Group { get; set; }
}