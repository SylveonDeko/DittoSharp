using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ditto.Database.Models.PostgreSQL.Bot;

[Table("tokens")]
public class Token
{
    [Key] [Column("token")] public string TokenValue { get; set; } = null!;

    [Column("u_id")] [Required] public string UserId { get; set; } = null!;

    [Column("username")] [Required] public string Username { get; set; } = null!;

    [Column("discriminator")] public string? Discriminator { get; set; }

    [Column("avatar")] public string? Avatar { get; set; }

    [Column("refresh_token")] public string? RefreshToken { get; set; }
}