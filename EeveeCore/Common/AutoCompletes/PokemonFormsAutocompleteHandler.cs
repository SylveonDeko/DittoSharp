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
                return AutocompletionResult.FromSuccess(new[]
                {
                    new AutocompleteResult("No Pokemon selected - use /pokemon select first", "none")
                });
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
            return AutocompletionResult.FromSuccess(new[]
            {
                new AutocompleteResult("Error loading forms", "error")
            });
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
            "arceus" => new List<string> 
            { 
                "electric", "poison", "rock", "ghost", "water", "flying", "fairy", 
                "psychic", "grass", "steel", "bug", "ice", "fighting", "dragon", 
                "fire", "dark", "ground" 
            },
            // Deoxys forms
            "deoxys" => new List<string> { "attack", "defense", "speed" },
            // Rotom appliance forms
            "rotom" => new List<string> { "heat", "wash", "frost", "fan", "mow" },
            // Legendary origin/alternate forms
            "giratina" => new List<string> { "origin" },
            "shaymin" => new List<string> { "sky" },
            "meloetta" => new List<string> { "pirouette" },
            "keldeo" => new List<string> { "resolute" },
            "hoopa" => new List<string> { "unbound" },
            // Forces of nature therian forms
            "thundurus" => new List<string> { "therian" },
            "tornadus" => new List<string> { "therian" },
            "landorus" => new List<string> { "therian" },
            "enamorus" => new List<string> { "therian" },
            // Fusion Pokemon (handled by separate commands, but listed for reference)
            "kyurem" => new List<string> { "white", "black" },
            "necrozma" => new List<string> { "dawn", "dusk", "ultra" },
            "calyrex" => new List<string> { "ice-rider", "shadow-rider" },
            // Zygarde forms
            "zygarde" => new List<string> { "10", "complete" },
            // Oricorio dance styles
            "oricorio" => new List<string> { "pom-pom", "pau", "sensu" },
            // Urshifu styles
            "urshifu" => new List<string> { "rapid-strike" },
            // Basculin variants
            "basculin" => new List<string> { "blue-striped", "white-striped" },
            // Darmanitan zen mode
            "darmanitan" => new List<string> { "zen" },
            // Tauros Paldea forms
            "tauros" => new List<string> { "blaze-paldea", "aqua-paldea" },
            // Primal forms
            "kyogre" => new List<string> { "primal" },
            "groudon" => new List<string> { "primal" },
            // Origin forms for creation trio
            "dialga" => new List<string> { "origin", "primal" },
            "palkia" => new List<string> { "origin" },
            // Crowned forms
            "zacian" => new List<string> { "crowned" },
            "zamazenta" => new List<string> { "crowned" },
            // Eevee special forms (level 100 + max happiness)
            "eevee" => new List<string> { "partner" },
            "pikachu" => new List<string> { "partner" },
            _ => new List<string>()
        };
    }
}