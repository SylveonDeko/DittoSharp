using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EeveeCore.Database.Models.PostgreSQL.Bot;

/// <summary>
/// Represents an authentication token for the EeveeCore Pokémon bot system.
/// This class stores user authentication information and Discord user details.
/// </summary>
[Table("tokens")]
public class Token
{
    /// <summary>
    /// Gets or sets the unique token value used for authentication.
    /// This serves as the primary key for the token record.
    /// </summary>
    [Key] [Column("token")] public string TokenValue { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Discord user ID associated with this token.
    /// </summary>
    [Column("u_id")] [Required] public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Discord username of the user.
    /// </summary>
    [Column("username")] [Required] public string Username { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Discord discriminator of the user.
    /// </summary>
    [Column("discriminator")] public string? Discriminator { get; set; }

    /// <summary>
    /// Gets or sets the Discord avatar identifier of the user.
    /// </summary>
    [Column("avatar")] public string? Avatar { get; set; }

    /// <summary>
    /// Gets or sets the refresh token used to obtain new authentication tokens.
    /// </summary>
    [Column("refresh_token")] public string? RefreshToken { get; set; }
}