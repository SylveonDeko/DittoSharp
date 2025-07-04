namespace EeveeCore.Services;

/// <summary>
///     API key validation interface for fraud detection dashboard.
/// </summary>
public interface IApiKeyValidation
{
    /// <summary>
    ///     Checks if the given key is valid.
    /// </summary>
    /// <param name="userApiKey">The key to check.</param>
    /// <returns>True/False depending on if it's correct.</returns>
    public bool IsValidApiKey(string userApiKey);
}