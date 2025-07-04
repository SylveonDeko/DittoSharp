// AuthHandlers/JwtAuthHandler.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LinqToDB;
using Serilog;

namespace EeveeCore.AuthHandlers;

/// <summary>
///     Authentication handler for JWT token validation in API requests.
/// </summary>
public class JwtAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly LinqToDbConnectionProvider _dbConnectionProvider;
    private readonly BotCredentials _credentials;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JwtAuthHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication scheme options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="dbConnectionProvider">The database connection provider.</param>
    /// <param name="credentials">The bot credentials containing JWT secret.</param>
    public JwtAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory, UrlEncoder encoder,
        LinqToDbConnectionProvider dbConnectionProvider, BotCredentials credentials)
        : base(options, loggerFactory, encoder)
    {
        _dbConnectionProvider = dbConnectionProvider;
        _credentials = credentials;
    }

    /// <summary>
    ///     Handles the authentication of JWT tokens from the Authorization header.
    /// </summary>
    /// <returns>The authentication result indicating success or failure.</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
        {
            return AuthenticateResult.Fail("Missing or invalid Authorization header");
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_credentials.JwtSecret);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            
            var userId = principal.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return AuthenticateResult.Fail("Invalid token: missing user ID");
            }

            // Verify user still exists and is not banned
            await using var db = await _dbConnectionProvider.GetConnectionAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == ulong.Parse(userId));
            if (user == null || user.BotBanned == true)
            {
                return AuthenticateResult.Fail("User not found or banned");
            }

            // Add additional claims
            var claims = new List<Claim>(principal.Claims)
            {
                new("Staff", user.Staff),
                new("IsBotOwner", (_credentials.OwnerIds.Contains(user.UserId.GetValueOrDefault())).ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (SecurityTokenExpiredException)
        {
            return AuthenticateResult.Fail("Token has expired");
        }
        catch (SecurityTokenException ex)
        {
            return AuthenticateResult.Fail($"Invalid token: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating JWT token");
            return AuthenticateResult.Fail("Token validation failed");
        }
    }
}