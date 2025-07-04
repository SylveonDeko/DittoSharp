namespace EeveeCore.Modules.Trade.Models;

/// <summary>
///     Represents the different types of Radiant tokens available in the trade system.
///     Each type corresponds to a Pokemon type and has associated emoji representations.
/// </summary>
public enum TokenType
{
    /// <summary>Dark type tokens</summary>
    Dark,
    
    /// <summary>Bug type tokens</summary>
    Bug,
    
    /// <summary>Ground type tokens</summary>
    Ground,
    
    /// <summary>Fighting type tokens</summary>
    Fighting,
    
    /// <summary>Steel type tokens</summary>
    Steel,
    
    /// <summary>Electric type tokens</summary>
    Electric,
    
    /// <summary>Grass type tokens</summary>
    Grass,
    
    /// <summary>Fairy type tokens</summary>
    Fairy,
    
    /// <summary>Water type tokens</summary>
    Water,
    
    /// <summary>Rock type tokens</summary>
    Rock,
    
    /// <summary>Flying type tokens</summary>
    Flying,
    
    /// <summary>Psychic type tokens</summary>
    Psychic,
    
    /// <summary>Normal type tokens</summary>
    Normal,
    
    /// <summary>Dragon type tokens</summary>
    Dragon,
    
    /// <summary>Fire type tokens</summary>
    Fire,
    
    /// <summary>Ghost type tokens</summary>
    Ghost,
    
    /// <summary>Ice type tokens</summary>
    Ice,
    
    /// <summary>Poison type tokens</summary>
    Poison
}

/// <summary>
///     Static helper class for token type operations and emoji mappings.
/// </summary>
public static class TokenTypeExtensions
{
    /// <summary>
    ///     Dictionary mapping token types to their Discord emoji representations.
    /// </summary>
    private static readonly Dictionary<TokenType, string> TokenEmojis = new()
    {
        { TokenType.Dark, "<:dark:1041101608504795156>" },
        { TokenType.Bug, "<:bug:1041101600384630824>" },
        { TokenType.Ground, "<:ground:1041101623147114536>" },
        { TokenType.Fighting, "<:fighting:1041101614456524800>" },
        { TokenType.Steel, "<:steel:1041101633699991623>" },
        { TokenType.Electric, "<:electric:1041101610912338011>" },
        { TokenType.Grass, "<:grass:1041101621536501792>" },
        { TokenType.Fairy, "<:fairy:1041101612728455200>" },
        { TokenType.Water, "<:water:1041101635633557576>" },
        { TokenType.Rock, "<:rock:1041101632122925207>" },
        { TokenType.Flying, "<:flying:1041101618034249868>" },
        { TokenType.Psychic, "<:psychic:1041101629920915528>" },
        { TokenType.Normal, "<:normal:1041101626645155930>" },
        { TokenType.Dragon, "<:dragon:1041101609784049664>" },
        { TokenType.Fire, "<:fire:1041101616008396953>" },
        { TokenType.Ghost, "<:ghost:1041101619422560256>" },
        { TokenType.Ice, "<:ice:1041101624749334609>" },
        { TokenType.Poison, "<:poison:1041101628259975259>" }
    };

    /// <summary>
    ///     Gets the Discord emoji representation for the specified token type.
    /// </summary>
    /// <param name="tokenType">The token type to get the emoji for.</param>
    /// <returns>The Discord emoji string for the token type.</returns>
    public static string GetEmoji(this TokenType tokenType)
    {
        return TokenEmojis.TryGetValue(tokenType, out var emoji) ? emoji : "";
    }

    /// <summary>
    ///     Gets all available token types as a collection.
    /// </summary>
    /// <returns>An enumerable of all token types.</returns>
    public static IEnumerable<TokenType> GetAllTypes()
    {
        return Enum.GetValues<TokenType>();
    }

    /// <summary>
    ///     Attempts to parse a string to a TokenType.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="tokenType">The parsed token type if successful.</param>
    /// <returns>True if parsing was successful, false otherwise.</returns>
    public static bool TryParse(string value, out TokenType tokenType)
    {
        return Enum.TryParse(value, true, out tokenType);
    }

    /// <summary>
    ///     Gets the display name for the token type (same as enum name).
    /// </summary>
    /// <param name="tokenType">The token type to get the display name for.</param>
    /// <returns>The display name of the token type.</returns>
    public static string GetDisplayName(this TokenType tokenType)
    {
        return tokenType.ToString();
    }
}