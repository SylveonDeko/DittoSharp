using Discord.Interactions;
using EeveeCore.Modules.Achievements.Common;

namespace EeveeCore.Common.Collections;

/// <summary>
///     Autocomplete handler for achievement names.
/// </summary>
public class AchievementAutocompleteHandler : AutocompleteHandler
{
    /// <inheritdoc />
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var userInput = autocompleteInteraction.Data.Current.Value?.ToString()?.ToLower() ?? "";

        var suggestions = AchievementConstants.AchievementDisplayNames
            .Where(a => a.Value.ToLower().Contains(userInput) || a.Key.ToLower().Contains(userInput))
            .Take(25)
            .Select(a => new AutocompleteResult(a.Value, a.Key))
            .ToList();

        return AutocompletionResult.FromSuccess(suggestions);
    }
}