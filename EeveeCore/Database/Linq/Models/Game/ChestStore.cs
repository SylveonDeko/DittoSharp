using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents a user's chest inventory in the EeveeCore Pokémon bot system.
///     This class tracks different types of treasure chests that can be opened for rewards.
/// </summary>
[Table("cheststore")]
public class ChestStore
{
    /// <summary>
    ///     Gets or sets the Discord user ID associated with this chest inventory.
    ///     This serves as the primary key for the chest store record.
    /// </summary>
    [PrimaryKey]
    [Column("u_id")]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the number of rare chests the user has.
    /// </summary>
    [Column("rare")]
    [NotNull]
    public int Rare { get; set; }

    /// <summary>
    ///     Gets or sets the number of mythic chests the user has.
    /// </summary>
    [Column("mythic")]
    [NotNull]
    public int Mythic { get; set; }

    /// <summary>
    ///     Gets or sets the number of legendary chests the user has.
    /// </summary>
    [Column("legend")]
    [NotNull]
    public int Legend { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the chest store will restock.
    /// </summary>
    [Column("restock")]
    [NotNull]
    public string Restock { get; set; } = "0";
}