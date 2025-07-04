using LinqToDB.Mapping;


namespace EeveeCore.Database.Linq.Models.Bot;

/// <summary>
///     Represents an authentication token for the EeveeCore Pokémon bot system.
///     This class stores user authentication information and Discord user details.
/// </summary>
[Table(Name = "tokens")]
public class Token
{
    /// <summary>
    ///     Gets or sets the unique token value used for authentication.
    ///     This serves as the primary key for the token record.
    /// </summary>
    [PrimaryKey]
    [Column(Name = "token")]
    public string TokenValue { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the Discord user ID associated with this token.
    /// </summary>
    [Column(Name = "u_id")]
    [NotNull]
    public string UserId { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the Discord username of the user.
    /// </summary>
    [Column(Name = "username")]
    [NotNull]
    public string Username { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the Discord discriminator of the user.
    /// </summary>
    [Column(Name = "discriminator")]
    public string? Discriminator { get; set; }

    /// <summary>
    ///     Gets or sets the Discord avatar identifier of the user.
    /// </summary>
    [Column(Name = "avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    ///     Gets or sets the refresh token used to obtain new authentication tokens.
    /// </summary>
    [Column(Name = "refresh_token")]
    public string? RefreshToken { get; set; }
}