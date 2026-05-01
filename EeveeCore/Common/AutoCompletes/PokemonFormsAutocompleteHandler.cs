using Discord.Interactions;
using EeveeCore.Modules.Pokemon.Services;
using EeveeCore.Services.Impl;
using MongoDB.Driver;

namespace EeveeCore.Common.AutoCompletes;

/// <summary>
///     Autocomplete handler for Pokemon forms based on the user's selected Pokemon.
///     Provides form suggestions that are valid for the currently selected Pokemon.
/// </summary>
/// <param name="mongo">MongoDB service for accessing Pokemon forms data.</param>
/// <param name="pokemonService">Service for getting user's selected Pokemon.</param>
public class PokemonFormsAutocompleteHandler(IMongoService mongo, PokemonService pokemonService) 
    : AutocompleteHandler
{
    /// <summary>
    ///     Generates autocomplete suggestions for Pokemon forms.
    ///     Returns forms that are available for the user's currently selected Pokemon.
    /// </summary>
    /// <returns>A task containing autocomplete results.</returns>
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, 
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        try
        {
            var currentValue = autocompleteInteraction.Data.Current.Value?.ToString()?.ToLower() ?? "";
            
            var selectedPokemon = await pokemonService.GetSelectedPokemonAsync(context.User.Id);
            if (selectedPokemon == null)
            {
                return AutocompletionResult.FromSuccess([
                    new AutocompleteResult("No Pokemon selected - use /pokemon select first", "none")
                ]);
            }

            var pokemonName = selectedPokemon.PokemonName.ToLower();
            var baseName = pokemonName.Split('-')[0];

            var forms = await mongo.Forms
                .Find(f => f.Identifier.StartsWith(baseName) && f.Identifier != baseName)
                .Limit(25)
                .ToListAsync();

            var suggestions = new List<AutocompleteResult>();

            var commonForms = GetCommonFormsForPokemon(baseName);
            
            foreach (var form in commonForms)
            {
                if (form.Contains(currentValue))
                {
                    suggestions.Add(new AutocompleteResult(form.ToTitleCase(), form));
                }
            }

            foreach (var form in forms)
            {
                if (!string.IsNullOrEmpty(form.FormIdentifier) && form.FormIdentifier.Contains(currentValue))
                {
                    var displayName = form.FormIdentifier.ToTitleCase();
                    if (!suggestions.Any(s => s.Value.ToString() == form.FormIdentifier))
                    {
                        suggestions.Add(new AutocompleteResult(displayName, form.FormIdentifier));
                    }
                }
            }

            if (!suggestions.Any())
            {
                suggestions.Add(new AutocompleteResult("No valid forms available for this Pokemon", "none"));
            }

            return AutocompletionResult.FromSuccess(suggestions
                .OrderBy(s => s.Name.ToLower().StartsWith(currentValue) ? 0 : 1)
                .ThenBy(s => s.Name)
                .Take(25));
        }
        catch
        {
            return AutocompletionResult.FromSuccess([
                new AutocompleteResult("Error loading forms", "error")
            ]);
        }
    }

    /// <summary>
    ///     Gets common forms for specific Pokemon based on the original Python implementation.
    /// </summary>
    /// <param name="pokemonName">The base Pokemon name.</param>
    /// <returns>List of common form names.</returns>
    private static List<string> GetCommonFormsForPokemon(string pokemonName)
    {
        return pokemonName switch
        {
            "arceus" =>
            [
                "electric", "poison", "rock", "ghost", "water", "flying", "fairy",
                "psychic", "grass", "steel", "bug", "ice", "fighting", "dragon",
                "fire", "dark", "ground"
            ],
            "deoxys" => ["attack", "defense", "speed"],
            "rotom" => ["heat", "wash", "frost", "fan", "mow"],
            "giratina" => ["origin"],
            "shaymin" => ["sky"],
            "meloetta" => ["pirouette"],
            "keldeo" => ["resolute"],
            "hoopa" => ["unbound"],
            "thundurus" => ["therian"],
            "tornadus" => ["therian"],
            "landorus" => ["therian"],
            "enamorus" => ["therian"],
            "kyurem" => ["white", "black"],
            "necrozma" => ["dawn", "dusk", "ultra"],
            "calyrex" => ["ice-rider", "shadow-rider"],
            "zygarde" => ["10", "complete"],
            "oricorio" => ["pom-pom", "pau", "sensu"],
            "urshifu" => ["rapid-strike"],
            "basculin" => ["blue-striped", "white-striped"],
            "darmanitan" => ["zen"],
            "tauros" => ["blaze-paldea", "aqua-paldea"],
            "kyogre" => ["primal"],
            "groudon" => ["primal"],
            "dialga" => ["origin", "primal"],
            "palkia" => ["origin"],
            "zacian" => ["crowned"],
            "zamazenta" => ["crowned"],
            "eevee" => ["partner"],
            "pikachu" => ["partner"],
            _ => []
        };
    }
}