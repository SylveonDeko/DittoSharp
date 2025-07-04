using Discord.Interactions;
using LinqToDB;
using Serilog;

namespace EeveeCore.Common.AutoCompletes;

/// <summary>
///     Provides autocomplete functionality for user's active market listings.
///     Shows Pokemon that the user currently has listed on the market.
/// </summary>
public class MarketListingsAutocompleteHandler : AutocompleteHandler
{
    // Cache for user market listings to reduce database queries (cache for 30 seconds)
    private static readonly ConcurrentDictionary<ulong, (DateTime Expiry, List<AutocompleteResult> Listings)> ListingsCache = new();
    private readonly LinqToDbConnectionProvider _dbProvider;
    private const int CacheTimeoutSeconds = 30;
    private const int MaxResults = 25;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MarketListingsAutocompleteHandler" /> class.
    /// </summary>
    /// <param name="dbProvider">The database connection provider.</param>
    public MarketListingsAutocompleteHandler(LinqToDbConnectionProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Generates autocomplete suggestions for market listings based on user's active listings.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="autocompleteInteraction">The autocomplete interaction data.</param>
    /// <param name="parameter">The parameter being autocompleted.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns an
    ///     AutocompletionResult containing matching market listings.
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
            if (ListingsCache.TryGetValue(userId, out var cached) && DateTime.UtcNow < cached.Expiry)
            {
                var filteredResults = FilterListingsResults(cached.Listings, currentValue);
                return AutocompletionResult.FromSuccess(filteredResults);
            }

            // Get user's active market listings from database
            await using var db = await _dbProvider.GetConnectionAsync();
            
            var userListings = await (from market in db.Market
                                    join pokemon in db.UserPokemon on market.PokemonId equals pokemon.Id
                                    where market.OwnerId == userId && market.BuyerId == null
                                    orderby market.ListedAt descending
                                    select new
                                    {
                                        market.Id,
                                        pokemon.PokemonName,
                                        pokemon.Nickname,
                                        pokemon.Level,
                                        market.Price,
                                        pokemon.Shiny,
                                        pokemon.Radiant,
                                        market.ListedAt
                                    })
                                    .ToListAsync();

            // Create autocomplete results
            var listingResults = userListings.Select(listing => 
            {
                var displayName = CreateDisplayName(listing.PokemonName!, listing.Nickname, listing.Id, listing.Level, listing.Price, listing.Shiny, listing.Radiant);
                return new AutocompleteResult(displayName, listing.Id.ToString());
            }).ToList();

            // Cache the results
            var expiry = DateTime.UtcNow.AddSeconds(CacheTimeoutSeconds);
            ListingsCache[userId] = (expiry, listingResults);

            // Filter and return results
            var filtered = FilterListingsResults(listingResults, currentValue);
            return AutocompletionResult.FromSuccess(filtered);
        }
        catch (Exception ex)
        {
            // Log the error and return empty result
            Log.Information($"Error in MarketListingsAutocompleteHandler: {ex.Message}");
            return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());
        }
    }

    /// <summary>
    ///     Creates a display name for the market listing in the autocomplete dropdown.
    /// </summary>
    /// <param name="pokemonName">The Pokemon species name.</param>
    /// <param name="nickname">The Pokemon's nickname.</param>
    /// <param name="listingId">The market listing ID.</param>
    /// <param name="level">The Pokemon's level.</param>
    /// <param name="price">The listing price.</param>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <returns>A formatted display string for the autocomplete option.</returns>
    private static string CreateDisplayName(string pokemonName, string nickname, ulong listingId, int level, int price, bool? shiny, bool? radiant)
    {
        var name = string.IsNullOrEmpty(nickname) || nickname == pokemonName ? pokemonName : $"{nickname} ({pokemonName})";
        var specialIndicators = "";
        
        if (radiant == true)
            specialIndicators += "💎";
        else if (shiny == true)
            specialIndicators += "✨";
            
        return $"{name} (#{listingId}) - Lvl {level} - {price:N0} MC{specialIndicators}".Trim();
    }

    /// <summary>
    ///     Filters market listing results based on user input.
    /// </summary>
    /// <param name="allResults">All available listing results.</param>
    /// <param name="filter">The filter string from user input.</param>
    /// <returns>Filtered list of autocomplete results.</returns>
    private static List<AutocompleteResult> FilterListingsResults(List<AutocompleteResult> allResults, string filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return allResults.Take(MaxResults).ToList();
        }

        // Filter by Pokemon name, nickname, or listing ID
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
        ListingsCache.TryRemove(userId, out _);
    }

    /// <summary>
    ///     Clears expired cache entries.
    /// </summary>
    public static void CleanExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = ListingsCache
            .Where(kvp => now >= kvp.Value.Expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            ListingsCache.TryRemove(key, out _);
        }
    }
}