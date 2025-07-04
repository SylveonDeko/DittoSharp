using LinqToDB.Mapping;

namespace EeveeCore.Database.Linq.Models.Game;

/// <summary>
///     Represents the experience points needed for each user level in the EeveeCore Pokémon bot system.
///     This reference table defines the progression and requirements for user leveling.
/// </summary>
[Table("leveling_data")]
public class LevelingData
{
    /// <summary>
    ///     Gets or sets the experience points required for the level.
    /// </summary>
    [Column("xp")]
    public int? Xp { get; set; }

    /// <summary>
    ///     Gets or sets the level number in the progression system.
    /// </summary>
    [Column("level")]
    public int? Level { get; set; }

    /// <summary>
    ///     Gets or sets the title or rank associated with the level.
    ///     Users may display these titles as achievements.
    /// </summary>
    [Column("title")]
    public string? Title { get; set; }
}