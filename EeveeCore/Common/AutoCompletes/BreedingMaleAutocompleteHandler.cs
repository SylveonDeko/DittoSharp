using Discord.Interactions;
using Serilog;
using LinqToDB.Async;

namespace EeveeCore.Common.AutoCompletes;

/// <summary>
///     Provides autocomplete functionality for male Pokemon suitable for breeding.
///     Shows Pokemon that can be used as fathers in breeding operations.
/// </summary>
public class BreedingMaleAutocompleteHandler : AutocompleteHandler
{
    private static readonly NonBlocking.ConcurrentDictionary<ulong, (DateTime Expiry, List<AutocompleteResult> Pokemon)> BreedingMaleCache = new();
    private readonly LinqToDbConnectionProvider _dbProvider;
    private const int CacheTimeoutSeconds = 30;
    private const int MaxResults = 25;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BreedingMaleAutocompleteHandler" /> class.
    /// </summary>
    /// <param name="dbProvider">The database connection provider.</param>
    public BreedingMaleAutocompleteHandler(LinqToDbConnectionProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Generates autocomplete suggestions for male Pokemon suitable for breeding.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="autocompleteInteraction">The autocomplete interaction data.</param>
    /// <param name="parameter">The parameter being autocompleted.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns an
    ///     AutocompletionResult containing matching male Pokemon.
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
            if (BreedingMaleCache.TryGetValue(userId, out var cached) && DateTime.UtcNow < cached.Expiry)
            {
                var filteredResults = FilterBreedingResults(cached.Pokemon, currentValue);
                return AutocompletionResult.FromSuccess(filteredResults);
            }

            await using var db = await _dbProvider.GetConnectionAsync();
            
            var breedingMales = await db.UserPokemonOwnerships
                .Where(o => o.UserId == userId)
                .Join(db.UserPokemon, o => o.PokemonId, p => p.Id, (o, p) => new { o.Position, Pokemon = p })
                .Where(x => x.Pokemon.PokemonName != "Egg" &&
                           x.Pokemon.Breedable &&
                           (x.Pokemon.Gender == "-m" || x.Pokemon.PokemonName.ToLower() == "ditto"))
                .OrderBy(x => x.Position)
                .Select(x => new
                {
                    x.Position,
                    x.Pokemon.PokemonName,
                    x.Pokemon.Nickname,
                    x.Pokemon.Level,
                    x.Pokemon.Nature,
                    x.Pokemon.HeldItem,
                    x.Pokemon.Gender,
                    x.Pokemon.Shiny,
                    x.Pokemon.Radiant,
                    x.Pokemon.Skin,
                    IvTotal = x.Pokemon.HpIv + x.Pokemon.AttackIv + x.Pokemon.DefenseIv + 
                             x.Pokemon.SpecialAttackIv + x.Pokemon.SpecialDefenseIv + x.Pokemon.SpeedIv,
                    x.Pokemon.HpIv,
                    x.Pokemon.AttackIv,
                    x.Pokemon.DefenseIv,
                    x.Pokemon.SpecialAttackIv,
                    x.Pokemon.SpecialDefenseIv,
                    x.Pokemon.SpeedIv
                })
                .ToListAsync();

            var breedingResults = breedingMales.Select(p => 
            {
                var displayName = CreateBreedingDisplayName(
                    p.PokemonName!, 
                    p.Nickname, 
                    p.Position + 1,
                    p.Level, 
                    p.Nature,
                    p.IvTotal,
                    p.HeldItem,
                    p.Gender,
                    p.Shiny, 
                    p.Radiant,
                    p.Skin!);
                return new AutocompleteResult(displayName, (p.Position + 1).ToString());
            }).ToList();

            var expiry = DateTime.UtcNow.AddSeconds(CacheTimeoutSeconds);
            BreedingMaleCache[userId] = (expiry, breedingResults);

            var filtered = FilterBreedingResults(breedingResults, currentValue);
            return AutocompletionResult.FromSuccess(filtered);
        }
        catch (Exception ex)
        {
            Log.Information($"Error in BreedingMaleAutocompleteHandler: {ex.Message}");
            return AutocompletionResult.FromSuccess([]);
        }
    }

    /// <summary>
    ///     Creates a display name for the male Pokemon in the autocomplete dropdown.
    ///     Shows breeding-relevant information like IVs, nature, and held items.
    /// </summary>
    /// <param name="pokemonName">The Pokemon species name.</param>
    /// <param name="nickname">The Pokemon's nickname.</param>
    /// <param name="position">The position in user's collection (1-based).</param>
    /// <param name="level">The Pokemon's level.</param>
    /// <param name="nature">The Pokemon's nature.</param>
    /// <param name="ivTotal">The total IV value.</param>
    /// <param name="heldItem">The Pokemon's held item.</param>
    /// <param name="gender">The Pokemon's gender.</param>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <param name="skin">The Pokemon's skin.</param>
    /// <returns>A formatted display string for the autocomplete option.</returns>
    private static string CreateBreedingDisplayName(
        string pokemonName, 
        string nickname, 
        ulong position, 
        int level, 
        string nature,
        int ivTotal,
        string heldItem,
        string gender,
        bool? shiny, 
        bool? radiant,
        string skin)
    {
        var name = string.IsNullOrEmpty(nickname) || nickname == pokemonName ? pokemonName : $"{nickname} ({pokemonName})";
        var ivPercent = Math.Round(ivTotal / 186.0 * 100, 1);
        
        var specialIndicators = "";
        if (radiant == true)
            specialIndicators += "💎";
        else if (shiny == true)
            specialIndicators += "✨";
        
        var genderIcon = gender switch
        {
            "-m" => "♂️",
            "-x" => "⚫",
            _ => ""
        };
        
        var breedingInfo = "";
        
        if (!string.IsNullOrEmpty(heldItem) && heldItem.ToLower() != "none")
        {
            if (heldItem.ToLower().Contains("knot"))
                breedingInfo += " 🔗";
            else if (heldItem.ToLower() == "everstone")
                breedingInfo += " 🪨";
        }
        
        return $"{name} ({genderIcon}#{position}) - Lv{level} | {ivPercent}% | {nature}{breedingInfo}{specialIndicators}".Trim();
    }

    /// <summary>
    ///     Filters breeding male results based on user input.
    /// </summary>
    /// <param name="allResults">All available breeding male results.</param>
    /// <param name="filter">The filter string from user input.</param>
    /// <returns>Filtered list of autocomplete results.</returns>
    private static List<AutocompleteResult> FilterBreedingResults(List<AutocompleteResult> allResults, string filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return allResults.Take(MaxResults).ToList();
        }

        var filtered = allResults.Where(result =>
            result.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            result.Value.ToString()!.Contains(filter))
            .ToList();

        var dittoMatches = filtered.Where(r => 
            r.Name.Contains("ditto", StringComparison.OrdinalIgnoreCase))
            .ToList();
            
        var exactMatches = filtered.Where(r => 
            r.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase) && !dittoMatches.Contains(r))
            .ToList();
        
        var otherMatches = filtered.Except(dittoMatches).Except(exactMatches).ToList();
        
        return dittoMatches.Concat(exactMatches).Concat(otherMatches).Take(MaxResults).ToList();
    }

    /// <summary>
    ///     Clears the cache for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to clear cache for.</param>
    public static void ClearUserCache(ulong userId)
    {
        BreedingMaleCache.TryRemove(userId, out _);
    }

    /// <summary>
    ///     Clears expired cache entries.
    /// </summary>
    public static void CleanExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = BreedingMaleCache
            .Where(kvp => now >= kvp.Value.Expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            BreedingMaleCache.TryRemove(key, out _);
        }
    }
}