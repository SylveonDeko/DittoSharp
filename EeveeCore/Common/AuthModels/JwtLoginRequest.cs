// AuthModels/JwtLoginRequest.cs

using System.ComponentModel.DataAnnotations;

namespace EeveeCore.Common.AuthModels;

/// <summary>
///     Represents a JWT login request containing Discord OAuth code.
/// </summary>
public class JwtLoginRequest
{
    /// <summary>
    ///     Gets or sets the Discord OAuth authorization code received from Discord.
    /// </summary>
    [Required]
    public string DiscordCode { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the redirect URI used in the OAuth flow.
    /// </summary>
    public string? RedirectUri { get; set; }
}

/// <summary>
///     Represents the response containing JWT tokens and user information.
/// </summary>
public class JwtTokenResponse
{
    /// <summary>
    ///     Gets or sets the JWT access token for API authentication.
    /// </summary>
    public string AccessToken { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the date and time when the access token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    ///     Gets or sets the user profile information.
    /// </summary>
    public UserProfileDto User { get; set; } = null!;
}

/// <summary>
///     Represents a request to refresh an expired JWT token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    ///     Gets or sets the refresh token to use for obtaining a new access token.
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = null!;
}

/// <summary>
///     Represents user profile information for API responses.
/// </summary>
public class UserProfileDto
{
    /// <summary>
    ///     Gets or sets the Discord user ID.
    /// </summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the Discord username.
    /// </summary>
    public string Username { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the Discord avatar hash.
    /// </summary>
    public string? Avatar { get; set; }
    
    /// <summary>
    ///     Gets or sets the Discord discriminator (if applicable).
    /// </summary>
    public string? Discriminator { get; set; }
    
    /// <summary>
    ///     Gets or sets a value indicating whether the user has admin privileges.
    /// </summary>
    public bool IsAdmin { get; set; }
    
    /// <summary>
    ///     Gets or sets the staff role of the user, if any.
    /// </summary>
    public string? Staff { get; set; }
    
    /// <summary>
    ///     Gets or sets a value indicating whether the user is a bot owner.
    /// </summary>
    public bool IsBotOwner { get; set; }
}

/// <summary>
///     Represents a Discord OAuth response containing user information.
/// </summary>
public class DiscordUserInfo
{
    /// <summary>
    ///     Gets or sets the Discord user ID.
    /// </summary>
    public string Id { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the Discord username.
    /// </summary>
    public string Username { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the Discord avatar hash.
    /// </summary>
    public string? Avatar { get; set; }
    
    /// <summary>
    ///     Gets or sets the Discord discriminator.
    /// </summary>
    public string? Discriminator { get; set; }
}

/// <summary>
///     Represents a Discord OAuth token response.
/// </summary>
public class DiscordTokenResponse
{
    /// <summary>
    ///     Gets or sets the Discord access token.
    /// </summary>
    public string Access_Token { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the token type (usually "Bearer").
    /// </summary>
    public string Token_Type { get; set; } = null!;
    
    /// <summary>
    ///     Gets or sets the token expiration time in seconds.
    /// </summary>
    public int Expires_In { get; set; }
    
    /// <summary>
    ///     Gets or sets the refresh token.
    /// </summary>
    public string? Refresh_Token { get; set; }
    
    /// <summary>
    ///     Gets or sets the token scope.
    /// </summary>
    public string? Scope { get; set; }
}