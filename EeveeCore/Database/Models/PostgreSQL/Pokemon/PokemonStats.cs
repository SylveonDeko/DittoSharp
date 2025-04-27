using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Pokemon;

/// <summary>
/// Represents battle statistics for each Pokémon species in the EeveeCore system.
/// This class tracks the performance of Pokémon in battles, including wins and defeats.
/// </summary>
[Table("p_stats")]
public class PokemonStats
{
    /// <summary>
    /// Gets or sets the name of the Pokémon species.
    /// This serves as the primary key for the statistics record.
    /// </summary>
    [Key]
    [Column("pokemon")]
    [StringLength(255)]
    public string Pokemon { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of battles won by this Pokémon species.
    /// </summary>
    [Column("wins")] public int? Wins { get; set; }

    /// <summary>
    /// Gets or sets the number of times this Pokémon species has fainted in battle.
    /// </summary>
    [Column("faints")] public int? Faints { get; set; }
}