namespace EeveeCore.Services.Impl;

/// <summary>
///     Implementation of API key validation for fraud detection dashboard.
/// </summary>
public class ApiKeyValidation : IApiKeyValidation
{
    private readonly BotCredentials _credentials;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApiKeyValidation" /> class.
    /// </summary>
    /// <param name="credentials">The bot credentials containing the API key.</param>
    public ApiKeyValidation(BotCredentials credentials)
    {
        _credentials = credentials;
    }

    /// <summary>
    ///     Checks if the given key is valid.
    /// </summary>
    /// <param name="userApiKey">The key to check.</param>
    /// <returns>True/False depending on if it's correct.</returns>
    public bool IsValidApiKey(string userApiKey)
    {
        return !string.IsNullOrWhiteSpace(userApiKey) && 
               userApiKey.Equals(_credentials.ApiKey, StringComparison.Ordinal);
    }
}