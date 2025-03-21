using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

[Table("botbans")]
[Keyless]
public class BotBan
{
    [Column("users", TypeName = "bigint[]")]
    [Required]
    public long[] Users { get; set; } = [];

    [Column("duelbans", TypeName = "bigint[]")]
    [Required]
    public long[] DuelBans { get; set; } = [];
}