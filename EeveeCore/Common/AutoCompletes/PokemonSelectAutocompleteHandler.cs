using Discord.Interactions;
using EeveeCore.Extensions;
using LinqToDB;
using Serilog;

namespace EeveeCore.Common.AutoCompletes;

/// <summary>
///     Provides autocomplete functionality for Pokemon selection with detailed stats.
///     Shows Pokemon name, level, IVs, nature, and special indicators.
/// </summary>
public class PokemonSelectAutocompleteHandler : AutocompleteHandler
{
    // Cache for user Pokemon to reduce database queries (cache for 30 seconds)
    private static readonly ConcurrentDictionary<ulong, (DateTime Expiry, List<AutocompleteResult> Pokemon)> PokemonCache = new();
    private readonly LinqToDbConnectionProvider _dbProvider;
    private const int CacheTimeoutSeconds = 30;
    private const int MaxResults = 25;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PokemonSelectAutocompleteHandler" /> class.
    /// </summary>
    /// <param name="dbProvider">The database connection provider.</param>
    public PokemonSelectAutocompleteHandler(LinqToDbConnectionProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Generates autocomplete suggestions for Pokemon selection with detailed stats.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="autocompleteInteraction">The autocomplete interaction data.</param>
    /// <param name="parameter">The parameter being autocompleted.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns an
    ///     AutocompletionResult containing matching Pokemon with detailed stats.
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

            // Get user's Pokemon from database with detailed stats
            await using var db = await _dbProvider.GetConnectionAsync();
            
            var userPokemon = await db.UserPokemonOwnerships
                .Where(o => o.UserId == userId)
                .Join(db.UserPokemon, o => o.PokemonId, p => p.Id, (o, p) => new { o.Position, Pokemon = p })
                .Where(x => x.Pokemon.PokemonName != "Egg") // Only exclude eggs
                .OrderBy(x => x.Position)
                .Select(x => new
                {
                    x.Position,
                    x.Pokemon.PokemonName,
                    x.Pokemon.Nickname,
                    x.Pokemon.Level,
                    x.Pokemon.Shiny,
                    x.Pokemon.Radiant,
                    x.Pokemon.Favorite,
                    x.Pokemon.Nature,
                    x.Pokemon.HpIv,
                    x.Pokemon.AttackIv,
                    x.Pokemon.DefenseIv,
                    x.Pokemon.SpecialAttackIv,
                    x.Pokemon.SpecialDefenseIv,
                    x.Pokemon.SpeedIv
                })
                .ToListAsync();

            // Create autocomplete results with detailed stats
            var pokemonResults = userPokemon.Select(p => 
            {
                var displayName = CreateDetailedDisplayName(
                    p.PokemonName!, p.Nickname, p.Position, p.Level, 
                    p.Shiny, p.Radiant, p.Favorite, p.Nature,
                    p.HpIv, p.AttackIv, p.DefenseIv, p.SpecialAttackIv, p.SpecialDefenseIv, p.SpeedIv);
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
            Log.Information($"Error in PokemonSelectAutocompleteHandler: {ex.Message}");
            return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());
        }
    }

    /// <summary>
    ///     Creates a detailed display name for the Pokemon with stats in the autocomplete dropdown.
    /// </summary>
    /// <param name="pokemonName">The Pokemon species name.</param>
    /// <param name="nickname">The Pokemon's nickname.</param>
    /// <param name="position">The position in user's collection.</param>
    /// <param name="level">The Pokemon's level.</param>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <param name="favorite">Whether the Pokemon is favorited.</param>
    /// <param name="nature">The Pokemon's nature.</param>
    /// <param name="hp">HP IV.</param>
    /// <param name="attack">Attack IV.</param>
    /// <param name="defense">Defense IV.</param>
    /// <param name="specialAttack">Special Attack IV.</param>
    /// <param name="specialDefense">Special Defense IV.</param>
    /// <param name="speed">Speed IV.</param>
    /// <returns>A formatted display string for the autocomplete option with detailed stats.</returns>
    private static string CreateDetailedDisplayName(string pokemonName, string nickname, ulong position, int level, 
        bool? shiny, bool? radiant, bool? favorite, string? nature,
        int? hp, int? attack, int? defense, int? specialAttack, int? specialDefense, int? speed)
    {
        var name = string.IsNullOrEmpty(nickname) || nickname == pokemonName ? pokemonName : $"{nickname} ({pokemonName})";
        var specialIndicators = "";
        
        if (radiant == true)
            specialIndicators += "💎";
        else if (shiny == true)
            specialIndicators += "✨";
            
        if (favorite == true)
            specialIndicators += "⭐";

        // Calculate total IV percentage
        var totalIV = (hp ?? 0) + (attack ?? 0) + (defense ?? 0) + (specialAttack ?? 0) + (specialDefense ?? 0) + (speed ?? 0);
        var ivPercentage = Math.Round((totalIV / 186.0) * 100, 1); // 186 is max total IVs (31*6)

        // Format nature
        var natureText = !string.IsNullOrEmpty(nature) ? nature.Capitalize() : "Unknown";

        // Create compact display: Name (#{pos}) Lvl{level} | {nature} | {IV%}% IV {indicators}
        return $"{name} (#{position+1}) Lvl{level} | {natureText} | {ivPercentage}%{specialIndicators}".Trim();
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

        // Filter by Pokemon name, nickname, position, nature, or IV percentage
        var filtered = allResults.Where(result =>
            result.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            result.Value.ToString()!.Contains(filter)
        ).ToList();

        // Prioritize exact matches at the start
        var exactMatches = filtered.Where(r => 
            r.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)
        ).ToList();
        
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