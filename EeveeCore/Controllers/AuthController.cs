// Controllers/AuthController.cs - CORRECTED VERSION

using EeveeCore.Common.AuthModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for user authentication and JWT token management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly BotCredentials _credentials;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="jwtTokenService">The JWT token service for authentication operations.</param>
    /// <param name="credentials">The bot credentials for Discord OAuth configuration.</param>
    public AuthController(JwtTokenService jwtTokenService, BotCredentials credentials)
    {
        _jwtTokenService = jwtTokenService;
        _credentials = credentials;
    }

    /// <summary>
    ///     Authenticates a user with Discord OAuth and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request containing Discord OAuth code.</param>
    /// <returns>JWT token and user information, or an error response.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<JwtTokenResponse>> Login([FromBody] JwtLoginRequest request)
    {
        try
        {
            Log.Information("Login attempt for Discord code");

            var result = await _jwtTokenService.AuthenticateWithDiscordAsync(
                request.DiscordCode, 
                request.RedirectUri);

            if (result == null)
            {
                Log.Warning("Login failed for Discord code");
                return BadRequest(new { error = "Authentication failed", 
                    message = "Invalid Discord code or user not found. Please ensure you have used /start in the Discord bot first." });
            }

            Log.Information("Login successful for user {UserId}", result.User.UserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during login");
            return StatusCode(500, new { error = "Internal server error", message = "An unexpected error occurred during login" });
        }
    }

    /// <summary>
    ///     Gets the current user's profile information from JWT claims.
    /// </summary>
    /// <returns>The current user's profile information.</returns>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Jwt")]
    public ActionResult<UserProfileDto> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst("UserId")?.Value;
            var username = User.FindFirst("Username")?.Value;
            var staff = User.FindFirst("Staff")?.Value;
            var isBotOwner = bool.Parse(User.FindFirst("IsBotOwner")?.Value ?? "false");
            var isAdmin = bool.Parse(User.FindFirst("IsAdmin")?.Value ?? "false");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                Log.Warning("Invalid user claims in JWT token");
                return BadRequest(new { error = "Invalid token", message = "Token contains invalid user information" });
            }

            var profile = new UserProfileDto
            {
                UserId = userId,
                Username = username,
                Staff = staff ?? "User",
                IsBotOwner = isBotOwner,
                IsAdmin = isAdmin
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting current user profile");
            return StatusCode(500, new { error = "Internal server error", message = "An unexpected error occurred while retrieving user profile" });
        }
    }

    /// <summary>
    ///     Gets the Discord OAuth URL for authentication.
    /// </summary>
    /// <param name="redirectUri">The redirect URI to use after authentication.</param>
    /// <returns>The Discord OAuth URL.</returns>
    [HttpGet("discord-url")]
    public ActionResult<object> GetDiscordAuthUrl([FromQuery] string? redirectUri = null)
    {
        try
        {
            var clientId = _credentials.DiscordClientId; // Your Discord client ID
            var scope = "identify";
            var responseType = "code";
            
            var encodedRedirectUri = string.IsNullOrEmpty(redirectUri) 
                ? "" 
                : $"&redirect_uri={Uri.EscapeDataString(redirectUri)}";

            var authUrl = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&response_type={responseType}&scope={scope}{encodedRedirectUri}";

            return Ok(new { url = authUrl });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating Discord auth URL");
            return StatusCode(500, new { error = "Internal server error", message = "An unexpected error occurred while generating auth URL" });
        }
    }
}