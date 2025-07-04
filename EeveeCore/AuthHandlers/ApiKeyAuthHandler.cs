using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EeveeCore.AuthHandlers;

/// <summary>
///     Authentication handler for API key-based authentication in the fraud detection dashboard.
/// </summary>
public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiKeyValidation _apiKeyValidation;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApiKeyAuthHandler" /> class.
    /// </summary>
    /// <param name="options">The authentication scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="apiKeyValidation">The API key validation service.</param>
    public ApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidation apiKeyValidation)
        : base(options, logger, encoder)
    {
        _apiKeyValidation = apiKeyValidation;
    }

    /// <summary>
    ///     Handles the authentication process for API key authentication.
    /// </summary>
    /// <returns>The authentication result.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiConstants.HeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key is missing"));
        }

        var providedApiKey = apiKeyHeaderValues.ToString();

        if (!_apiKeyValidation.IsValidApiKey(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "FraudDashboardUser"),
            new Claim("ApiUser", "true")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}