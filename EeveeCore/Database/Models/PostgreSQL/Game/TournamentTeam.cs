using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Game;

/// <summary>
///     Represents a team registered for a tournament in the EeveeCore Pokémon bot system.
///     This class tracks tournament participants and their selected Pokémon teams.
/// </summary>
[Table("tourny_teams")]
public class TournamentTeam
{
    /// <summary>
    ///     Gets or sets the unique identifier for this tournament team.
    /// </summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon names in the team.
    /// </summary>
    [Column("team", TypeName = "character varying[]")]
    [Required]
    public string[] Team { get; set; } = [];

    /// <summary>
    ///     Gets or sets the Discord user ID of the team owner.
    /// </summary>
    [Column("u_id")]
    [Required]
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the Discord user ID of the staff member who registered or approved the team.
    /// </summary>
    [Column("staff")]
    [Required]
    public ulong StaffId { get; set; }

    /// <summary>
    ///     Gets or sets the array of Pokémon IDs in the team.
    ///     These IDs correspond to the specific Pokémon instances owned by the user.
    /// </summary>
    [Column("team_ids", TypeName = "integer[]")]
    public int[]? TeamIds { get; set; }
}