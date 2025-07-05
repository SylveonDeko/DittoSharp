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
            
            // Get user's selected Pokemon
            var selectedPokemon = await pokemonService.GetSelectedPokemonAsync(context.User.Id);
            if (selectedPokemon == null)
            {
                return AutocompletionResult.FromSuccess([
                    new AutocompleteResult("No Pokemon selected - use /pokemon select first", "none")
                ]);
            }

            var pokemonName = selectedPokemon.PokemonName.ToLower();
            var baseName = pokemonName.Split('-')[0]; // Get base name without forms

            // Get available forms for this Pokemon from MongoDB
            var forms = await mongo.Forms
                .Find(f => f.Identifier.StartsWith(baseName) && f.Identifier != baseName)
                .Limit(25)
                .ToListAsync();

            var suggestions = new List<AutocompleteResult>();

            // Add common forms based on Pokemon type
            var commonForms = GetCommonFormsForPokemon(baseName);
            
            foreach (var form in commonForms)
            {
                if (form.Contains(currentValue))
                {
                    suggestions.Add(new AutocompleteResult(form.ToTitleCase(), form));
                }
            }

            // Add forms from database
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

            // If no specific forms found and no suggestions from common forms, show a helpful message
            if (!suggestions.Any())
            {
                suggestions.Add(new AutocompleteResult("No valid forms available for this Pokemon", "none"));
            }

            // Take only first 25 suggestions and prioritize exact matches
            return AutocompletionResult.FromSuccess(suggestions
                .OrderBy(s => s.Name.ToLower().StartsWith(currentValue) ? 0 : 1)
                .ThenBy(s => s.Name)
                .Take(25));
        }
        catch
        {
            // Return error message if something goes wrong
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
            // Arceus plate forms
            "arceus" =>
            [
                "electric", "poison", "rock", "ghost", "water", "flying", "fairy",
                "psychic", "grass", "steel", "bug", "ice", "fighting", "dragon",
                "fire", "dark", "ground"
            ],
            // Deoxys forms
            "deoxys" => ["attack", "defense", "speed"],
            // Rotom appliance forms
            "rotom" => ["heat", "wash", "frost", "fan", "mow"],
            // Legendary origin/alternate forms
            "giratina" => ["origin"],
            "shaymin" => ["sky"],
            "meloetta" => ["pirouette"],
            "keldeo" => ["resolute"],
            "hoopa" => ["unbound"],
            // Forces of nature therian forms
            "thundurus" => ["therian"],
            "tornadus" => ["therian"],
            "landorus" => ["therian"],
            "enamorus" => ["therian"],
            // Fusion Pokemon (handled by separate commands, but listed for reference)
            "kyurem" => ["white", "black"],
            "necrozma" => ["dawn", "dusk", "ultra"],
            "calyrex" => ["ice-rider", "shadow-rider"],
            // Zygarde forms
            "zygarde" => ["10", "complete"],
            // Oricorio dance styles
            "oricorio" => ["pom-pom", "pau", "sensu"],
            // Urshifu styles
            "urshifu" => ["rapid-strike"],
            // Basculin variants
            "basculin" => ["blue-striped", "white-striped"],
            // Darmanitan zen mode
            "darmanitan" => ["zen"],
            // Tauros Paldea forms
            "tauros" => ["blaze-paldea", "aqua-paldea"],
            // Primal forms
            "kyogre" => ["primal"],
            "groudon" => ["primal"],
            // Origin forms for creation trio
            "dialga" => ["origin", "primal"],
            "palkia" => ["origin"],
            // Crowned forms
            "zacian" => ["crowned"],
            "zamazenta" => ["crowned"],
            // Eevee special forms (level 100 + max happiness)
            "eevee" => ["partner"],
            "pikachu" => ["partner"],
            _ => []
        };
    }
}