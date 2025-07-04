using Discord.Interactions;
using LinqToDB;
using Serilog;

namespace EeveeCore.Common.AutoCompletes;

/// <summary>
///     Provides autocomplete functionality for user's Pokemon.
///     Suggests Pokemon that the user owns and can trade.
/// </summary>
public class PokemonAutocompleteHandler : AutocompleteHandler
{
    // Cache for user Pokemon to reduce database queries (cache for 30 seconds)
    private static readonly ConcurrentDictionary<ulong, (DateTime Expiry, List<AutocompleteResult> Pokemon)> PokemonCache = new();
    private readonly LinqToDbConnectionProvider _dbProvider;
    private const int CacheTimeoutSeconds = 30;
    private const int MaxResults = 25;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PokemonAutocompleteHandler" /> class.
    /// </summary>
    /// <param name="dbProvider">The database connection provider.</param>
    public PokemonAutocompleteHandler(LinqToDbConnectionProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Generates autocomplete suggestions for Pokemon based on user's collection.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="autocompleteInteraction">The autocomplete interaction data.</param>
    /// <param name="parameter">The parameter being autocompleted.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns an
    ///     AutocompletionResult containing matching Pokemon.
    /// </returns>
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var currentValue = autocompleteInteraction.Data.Current.Value.ToString()?.ToLower() ?? string.Empty;
        var userId = context.User.Id;

        try
        {
            // Check cache first
            if (PokemonCache.TryGetValue(userId, out var cached) && DateTime.UtcNow < cached.Expiry)
            {
                var filteredResults = FilterPokemonResults(cached.Pokemon, currentValue);
                return AutocompletionResult.FromSuccess(filteredResults);
            }

            // Get user's Pokemon from database
            await using var db = await _dbProvider.GetConnectionAsync();
            
            var userPokemon = await db.UserPokemonOwnerships
                .Where(o => o.UserId == userId)
                .Join(db.UserPokemon, o => o.PokemonId, p => p.Id, (o, p) => new { o.Position, Pokemon = p })
                .Where(x => x.Position > 1 && // Cannot trade position 1
                           x.Pokemon.PokemonName != "Egg" && // Cannot trade eggs
                           !x.Pokemon.Favorite && // Cannot trade favorited
                           x.Pokemon.Tradable && // Must be tradable
                           !x.Pokemon.MarketEnlist) // Cannot trade if on market
                .OrderBy(x => x.Position)
                .Select(x => new
                {
                    x.Position,
                    x.Pokemon.PokemonName,
                    x.Pokemon.Nickname,
                    x.Pokemon.Level,
                    x.Pokemon.Shiny,
                    x.Pokemon.Radiant
                })
                .ToListAsync();

            // Create autocomplete results
            var pokemonResults = userPokemon.Select(p => 
            {
                var displayName = CreateDisplayName(p.PokemonName!, p.Nickname, p.Position, p.Level, p.Shiny, p.Radiant);
                return new AutocompleteResult(displayName, p.Position.ToString());
            }).ToList();

            // Cache the results
            var expiry = DateTime.UtcNow.AddSeconds(CacheTimeoutSeconds);
            PokemonCache[userId] = (expiry, pokemonResults);

            // Filter and return results
            var filtered = FilterPokemonResults(pokemonResults, currentValue);
            return AutocompletionResult.FromSuccess(filtered);
        }
        catch (Exception ex)
        {
            // Log the error and return empty result
            Log.Information($"Error in PokemonAutocompleteHandler: {ex.Message}");
            return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());
        }
    }

    /// <summary>
    ///     Creates a display name for the Pokemon in the autocomplete dropdown.
    /// </summary>
    /// <param name="pokemonName">The Pokemon species name.</param>
    /// <param name="nickname">The Pokemon's nickname.</param>
    /// <param name="position">The position in user's collection.</param>
    /// <param name="level">The Pokemon's level.</param>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <returns>A formatted display string for the autocomplete option.</returns>
    private static string CreateDisplayName(string pokemonName, string nickname, ulong position, int level, bool? shiny, bool? radiant)
    {
        var name = string.IsNullOrEmpty(nickname) || nickname == pokemonName ? pokemonName : $"{nickname} ({pokemonName})";
        var specialIndicators = "";
        
        if (radiant == true)
            specialIndicators += "💎";
        else if (shiny == true)
            specialIndicators += "✨";
            
        return $"{name} (#{position}) - Lvl {level}{specialIndicators}".Trim();
    }

    /// <summary>
    ///     Filters Pokemon results based on user input.
    /// </summary>
    /// <param name="allResults">All available Pokemon results.</param>
    /// <param name="filter">The filter string from user input.</param>
    /// <returns>Filtered list of autocomplete results.</returns>
    private static List<AutocompleteResult> FilterPokemonResults(List<AutocompleteResult> allResults, string filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return allResults.Take(MaxResults).ToList();
        }

        // Filter by Pokemon name, nickname, or position
        var filtered = allResults.Where(result =>
            result.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            result.Value.ToString()!.Contains(filter))
            .ToList();

        // Prioritize exact matches at the start
        var exactMatches = filtered.Where(r => 
            r.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        var otherMatches = filtered.Except(exactMatches).ToList();
        
        return exactMatches.Concat(otherMatches).Take(MaxResults).ToList();
    }

    /// <summary>
    ///     Clears the cache for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to clear cache for.</param>
    public static void ClearUserCache(ulong userId)
    {
        PokemonCache.TryRemove(userId, out _);
    }

    /// <summary>
    ///     Clears expired cache entries.
    /// </summary>
    public static void CleanExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = PokemonCache
            .Where(kvp => now >= kvp.Value.Expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            PokemonCache.TryRemove(key, out _);
        }
    }
}