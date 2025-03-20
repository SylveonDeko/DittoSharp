using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Game;

[Table("tourny_teams")]
public class TournamentTeam
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("team", TypeName = "character varying[]")]
    [Required]
    public string[] Team { get; set; } = [];

    [Column("u_id")] [Required] public ulong UserId { get; set; }

    [Column("staff")] [Required] public ulong StaffId { get; set; }

    [Column("team_ids", TypeName = "integer[]")]
    public int[]? TeamIds { get; set; }
}