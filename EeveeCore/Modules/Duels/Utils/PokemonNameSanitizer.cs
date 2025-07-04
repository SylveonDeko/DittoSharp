using System.Text.RegularExpressions;

namespace EeveeCore.Modules.Duels.Utils;

/// <summary>
/// Utility class for sanitizing Pokémon display names to remove Discord formatting and emotes
/// </summary>
public static partial class PokemonNameSanitizer
{
    // Regex patterns for different Discord formatting elements
    private static readonly Regex DiscordEmotePattern = DiscordEmoteRegex();
    private static readonly Regex MarkdownCodeBlockPattern = CodeBlockRegex();
    private static readonly Regex MarkdownInlineCodePattern = InlineCodeRegex();
    private static readonly Regex MarkdownBoldPattern = BoldRegex();
    private static readonly Regex MarkdownItalicPattern = ItalicRegex();
    private static readonly Regex MarkdownUnderlinePattern = UnderlineRegex();
    private static readonly Regex MarkdownStrikethroughPattern = StrikethroughRegex();
    private static readonly Regex MarkdownSpoilerPattern = SpoilerRegex();
    private static readonly Regex ExcessiveWhitespacePattern = WhitespaceRegex();

    /// <summary>
    /// Sanitizes a Pokémon name by removing Discord emotes, markdown formatting, and cleaning up whitespace
    /// </summary>
    /// <param name="name">The original Pokémon name that may contain Discord formatting</param>
    /// <param name="maxLength">Maximum length for the sanitized name (default: 25)</param>
    /// <returns>A clean name suitable for display in battle images</returns>
    public static string SanitizeDisplayName(string name, int maxLength = 25)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Unknown";

        var sanitized = name;

        // Remove Discord emotes (both static and animated) - no capture group, remove entirely
        sanitized = DiscordEmotePattern.Replace(sanitized, "");

        // Remove markdown code blocks - no capture group, remove entirely
        sanitized = MarkdownCodeBlockPattern.Replace(sanitized, "");

        // Remove inline code and extract content - has capture group, keep content
        sanitized = MarkdownInlineCodePattern.Replace(sanitized, "$1");

        // Remove other markdown formatting but keep the content - all have capture groups
        sanitized = MarkdownBoldPattern.Replace(sanitized, "$1");
        sanitized = MarkdownItalicPattern.Replace(sanitized, "$1");
        sanitized = MarkdownUnderlinePattern.Replace(sanitized, "$1");
        sanitized = MarkdownStrikethroughPattern.Replace(sanitized, "$1");
        sanitized = MarkdownSpoilerPattern.Replace(sanitized, "$1");

        // Clean up excessive whitespace
        sanitized = ExcessiveWhitespacePattern.Replace(sanitized, " ");
        sanitized = sanitized.Trim();

        // Handle empty result
        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "Pokémon";

        // Truncate if too long and add ellipsis
        if (sanitized.Length > maxLength)
            sanitized = sanitized[..(maxLength - 3)] + "...";

        return sanitized;
    }

    /// <summary>
    /// Extracts emote names from a string for potential use in rendering
    /// </summary>
    /// <param name="text">Text containing Discord emotes</param>
    /// <returns>List of emote names found in the text</returns>
    private static List<string> ExtractEmoteNames(string text)
    {
        var emoteNames = new List<string>();
        
        if (string.IsNullOrWhiteSpace(text))
            return emoteNames;

        var matches = DiscordEmotePattern.Matches(text);
        foreach (Match match in matches)
        {
            // Extract emote name from <:name:id> or <a:name:id>
            var emoteContent = match.Value.Trim('<', '>');
            var parts = emoteContent.Split(':');
            if (parts.Length < 2) continue;
            var emoteName = parts[1];
            if (!string.IsNullOrWhiteSpace(emoteName))
                emoteNames.Add(emoteName);
        }

        return emoteNames;
    }

    /// <summary>
    /// Gets a clean version of the name while preserving some context about emotes used
    /// </summary>
    /// <param name="name">The original name</param>
    /// <param name="includeEmoteHint">Whether to include a hint about emotes being present</param>
    /// <returns>Sanitized name with optional emote context</returns>
    public static string GetDisplayNameWithContext(string name, bool includeEmoteHint = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Unknown";

        var emoteNames = ExtractEmoteNames(name);
        var sanitizedName = SanitizeDisplayName(name);

        if (includeEmoteHint && emoteNames.Any())
        {
            // If we found emotes and the sanitized name is very short or generic,
            // try to include context
            if (sanitizedName.Length < 3 || sanitizedName == "Pokémon")
            {
                var primaryEmote = emoteNames.First();
                // Capitalize first letter for better display
                primaryEmote = char.ToUpper(primaryEmote[0]) + primaryEmote[1..].ToLower();
                return $"{sanitizedName} ({primaryEmote})";
            }
        }

        return sanitizedName;
    }

    // Generated regex patterns with descriptive names
    [GeneratedRegex(@"<a?:\w+:\d+>", RegexOptions.Compiled)]
    private static partial Regex DiscordEmoteRegex();
    
    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Compiled)]
    private static partial Regex CodeBlockRegex();
    
    [GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
    private static partial Regex InlineCodeRegex();
    
    [GeneratedRegex(@"\*\*([^*]+)\*\*", RegexOptions.Compiled)]
    private static partial Regex BoldRegex();
    
    [GeneratedRegex(@"\*([^*]+)\*", RegexOptions.Compiled)]
    private static partial Regex ItalicRegex();
    
    [GeneratedRegex(@"__([^_]+)__", RegexOptions.Compiled)]
    private static partial Regex UnderlineRegex();
    
    [GeneratedRegex(@"~~([^~]+)~~", RegexOptions.Compiled)]
    private static partial Regex StrikethroughRegex();
    
    [GeneratedRegex(@"\|\|([^|]+)\|\|", RegexOptions.Compiled)]
    private static partial Regex SpoilerRegex();
    
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}