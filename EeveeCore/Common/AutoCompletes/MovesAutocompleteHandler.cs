using Discord.Interactions;
using EeveeCore.Services.Impl;
using LinqToDB;
using MongoDB.Driver;
using Serilog;

namespace EeveeCore.Common.AutoCompletes;

/// <summary>
///     Provides autocomplete functionality for Pokémon moves.
///     Suggests moves that the selected Pokémon can learn.
/// </summary>
public class MovesAutoCompleteHandler : AutocompleteHandler
{
    // Cache for Pokémon moves to reduce database queries
    private static readonly ConcurrentDictionary<string, List<string>> MoveCache = new();
    private readonly LinqToDbConnectionProvider _dbProvider;
    private readonly IMongoService _mongoService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MovesAutocompleteHandler" /> class.
    /// </summary>
    /// <param name="dbProvider">The database connection provider.</param>
    /// <param name="mongoService">The MongoDB service for accessing collection data.</param>
    public MovesAutoCompleteHandler(LinqToDbConnectionProvider dbProvider, IMongoService mongoService)
    {
        _dbProvider = dbProvider;
        _mongoService = mongoService;
    }

    /// <summary>
    ///     Generates autocomplete suggestions for Pokémon moves based on the selected Pokémon.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="autocompleteInteraction">The autocomplete interaction data.</param>
    /// <param name="parameter">The parameter being autocompleted.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns an
    ///     AutocompletionResult containing matching move names.
    /// </returns>
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        // Get the current input value
        var currentValue = autocompleteInteraction.Data.Current.Value.ToString()?.ToLower() ?? string.Empty;

        try
        {
            // Get the selected Pokémon for the user
            await using var db = await _dbProvider.GetConnectionAsync();
            var selectedPokemon = await db.UserPokemon
                .Where(p => p.Id == db.Users
                    .Where(u => u.UserId == context.User.Id)
                    .Select(u => u.Selected)
                    .FirstOrDefault())
                .Select(p => p.PokemonName!.ToLower())
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(selectedPokemon))
                return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());

            // Check cache or get moves from database
            if (!MoveCache.TryGetValue(selectedPokemon, out var moves))
            {
                moves = await GetMovesForPokemon(selectedPokemon);
                MoveCache[selectedPokemon] = moves;
            }

            // Filter moves based on user input
            var filteredMoves = moves
                .Where(m => string.IsNullOrEmpty(currentValue) || m.Contains(currentValue))
                .Take(25)
                .Select(m => new AutocompleteResult(m.Replace("-", " ").Capitalize(), m))
                .ToList();

            return AutocompletionResult.FromSuccess(filteredMoves);
        }
        catch (Exception ex)
        {
            // Log the error and return an empty result
            Log.Information($"Error in MovesAutocompleteHandler: {ex.Message}");
            return AutocompletionResult.FromSuccess(Array.Empty<AutocompleteResult>());
        }
    }

    /// <summary>
    ///     Gets the list of moves that a Pokémon can learn.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokémon.</param>
    /// <returns>A list of move names the Pokémon can learn.</returns>
    private async Task<List<string>> GetMovesForPokemon(string pokemonName)
    {
        // Handle special case for Smeargle which can learn almost all moves
        if (pokemonName == "smeargle")
        {
            // Moves which are not coded in the bot
            var uncoded_ids = new[]
            {
                266, 270, 476, 495, 502, 511, 597, 602, 603, 607, 622, 623, 624, 625, 626, 627, 628, 629,
                630, 631, 632, 633, 634, 635, 636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647,
                648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 671, 695, 696, 697, 698, 699, 700,
                701, 702, 703, 719, 723, 724, 725, 726, 727, 728, 811, 10001, 10002, 10003, 10004, 10005,
                10006, 10007, 10008, 10009, 10010, 10011, 10012, 10013, 10014, 10015, 10016, 10017, 10018
            };

            var all_moves = await _mongoService.Moves
                .Find(m => !uncoded_ids.Contains(m.MoveId))
                .Project(m => m.Identifier)
                .ToListAsync();

            return all_moves;
        }

        // For normal Pokémon, get their specific move list
        var pokemonMoves = await _mongoService.PokemonMoves
            .Find(pm => pm.Pokemon == pokemonName)
            .FirstOrDefaultAsync();

        if (pokemonMoves == null || pokemonMoves.Moves == null) return new List<string>();

        return pokemonMoves.Moves.Distinct().OrderBy(m => m).ToList();
    }
}