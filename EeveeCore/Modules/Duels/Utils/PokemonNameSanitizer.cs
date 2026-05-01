using System.Text.RegularExpressions;

namespace EeveeCore.Modules.Duels.Utils;

/// <summary>
/// Utility class for sanitizing Pokémon display names to remove Discord formatting and emotes
/// </summary>
public static partial class PokemonNameSanitizer
{
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

        sanitized = DiscordEmotePattern.Replace(sanitized, "");

        sanitized = MarkdownCodeBlockPattern.Replace(sanitized, "");

        sanitized = MarkdownInlineCodePattern.Replace(sanitized, "$1");

        sanitized = MarkdownBoldPattern.Replace(sanitized, "$1");
        sanitized = MarkdownItalicPattern.Replace(sanitized, "$1");
        sanitized = MarkdownUnderlinePattern.Replace(sanitized, "$1");
        sanitized = MarkdownStrikethroughPattern.Replace(sanitized, "$1");
        sanitized = MarkdownSpoilerPattern.Replace(sanitized, "$1");

        sanitized = ExcessiveWhitespacePattern.Replace(sanitized, " ");
        sanitized = sanitized.Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "Pokémon";

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
            if (sanitizedName.Length < 3 || sanitizedName == "Pokémon")
            {
                var primaryEmote = emoteNames.First();
                primaryEmote = char.ToUpper(primaryEmote[0]) + primaryEmote[1..].ToLower();
                return $"{sanitizedName} ({primaryEmote})";
            }
        }

        return sanitizedName;
    }

    /// <summary>Compiled regex matching Discord custom emote tags (e.g. <c>&lt;:name:123&gt;</c>, <c>&lt;a:name:123&gt;</c>).</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"<a?:\w+:\d+>", RegexOptions.Compiled)]
    private static partial Regex DiscordEmoteRegex();

    /// <summary>Compiled regex matching Discord triple-backtick code blocks.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Compiled)]
    private static partial Regex CodeBlockRegex();

    /// <summary>Compiled regex matching Discord inline single-backtick code spans, capturing the inner text.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
    private static partial Regex InlineCodeRegex();

    /// <summary>Compiled regex matching Discord bold markdown (<c>**text**</c>), capturing the inner text.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"\*\*([^*]+)\*\*", RegexOptions.Compiled)]
    private static partial Regex BoldRegex();

    /// <summary>Compiled regex matching Discord italic markdown (<c>*text*</c>), capturing the inner text.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"\*([^*]+)\*", RegexOptions.Compiled)]
    private static partial Regex ItalicRegex();

    /// <summary>Compiled regex matching Discord underline markdown (<c>__text__</c>), capturing the inner text.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"__([^_]+)__", RegexOptions.Compiled)]
    private static partial Regex UnderlineRegex();

    /// <summary>Compiled regex matching Discord strikethrough markdown (<c>~~text~~</c>), capturing the inner text.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"~~([^~]+)~~", RegexOptions.Compiled)]
    private static partial Regex StrikethroughRegex();

    /// <summary>Compiled regex matching Discord spoiler markdown (<c>||text||</c>), capturing the inner text.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"\|\|([^|]+)\|\|", RegexOptions.Compiled)]
    private static partial Regex SpoilerRegex();

    /// <summary>Compiled regex matching one or more consecutive whitespace characters.</summary>
    /// <returns>The compiled regex.</returns>
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}