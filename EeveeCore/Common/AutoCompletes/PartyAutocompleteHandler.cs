using Discord.Interactions;
using LinqToDB;

namespace EeveeCore.Common.AutoCompletes;

/// <summary>
///     Provides autocomplete functionality for party names.
///     Suggests existing party names matching the user's input.
/// </summary>
/// <param name="_context">The database context for querying party data.</param>
public class PartyNameAutocompleteHandler(LinqToDbConnectionProvider _context) : AutocompleteHandler
{
    /// <summary>
    ///     Generates autocomplete suggestions for party names.
    ///     Filters suggestions based on user input and displays up to 25 results.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="autocompleteInteraction">The autocomplete interaction data.</param>
    /// <param name="parameter">The parameter being autocompleted.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns an
    ///     AutocompletionResult containing matching party names.
    /// </returns>
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var currentValue = autocompleteInteraction.Data.Current.Value.ToString();

        await using var db = await _context.GetConnectionAsync();

        var userParties = await db.Parties
            .Where(p => p.UserId == context.User.Id)
            .Select(p => p.Name)
            .ToListAsync();

        if (string.IsNullOrEmpty(currentValue))
            return AutocompletionResult.FromSuccess(
                userParties.Take(25).Select(p => new AutocompleteResult(p, p))
            );

        var filteredParties = userParties
            .Where(p => p.Contains(currentValue, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(p => new AutocompleteResult(p, p));

        return AutocompletionResult.FromSuccess(filteredParties);
    }
}