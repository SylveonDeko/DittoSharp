using EeveeCore.Database.Models.Mongo.Pokemon;
using EeveeCore.Services.Impl;
using MongoDB.Driver;

namespace EeveeCore.Modules.Extras.Services;

/// <summary>
///     Service implementation for the Extras module
/// </summary>
public class ExtrasService : INService
{
    private readonly IMongoService _mongoService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExtrasService" /> class.
    /// </summary>
    /// <param name="mongoService">The MongoDB service for database operations.</param>
    public ExtrasService(
        IMongoService mongoService)
    {
        _mongoService = mongoService;
    }

    /// <summary>
    ///     Gets the complete list of moves available to a specific Pokémon.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokémon to get moves for.</param>
    /// <returns>A list of move names the Pokémon can learn, or null if the Pokémon doesn't exist.</returns>
    public async Task<List<string>> GetMoves(string pokemonName)
    {
        if (pokemonName == "smeargle")
        {
            // Moves which are not coded in the bot
            var uncoded = new[]
            {
                266, 270, 476, 495, 502, 511, 597, 602, 603, 607, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632,
                633, 634, 635, 636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 650, 651, 652, 653,
                654, 655, 656, 657, 658, 671, 695, 696, 697, 698, 699, 700, 701, 702, 703, 719, 723, 724, 725, 726, 727,
                728, 811, 10001, 10002, 10003, 10004, 10005, 10006, 10007, 10008, 10009, 10010, 10011, 10012, 10013,
                10014, 10015, 10016, 10017, 10018
            };

            var allMoves = await _mongoService.Moves
                .Find(Builders<Move>.Filter.Nin(m => m.MoveId, uncoded))
                .ToListAsync();

            return allMoves.Select(m => m.Identifier).ToList();
        }

        var moves = await _mongoService.PokemonMoves.Find(p => p.Pokemon == pokemonName).FirstOrDefaultAsync();
        if (moves == null)
            return null;

        return moves.Moves.OrderBy(m => m).ToList();
    }

    /// <summary>
    ///     Determines whether a Pokémon is in a formed state.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokémon to check.</param>
    /// <returns>True if the Pokémon is in a formed state, false otherwise.</returns>
    public bool IsFormed(string pokemonName)
    {
        if (string.IsNullOrEmpty(pokemonName))
            return false;

        return pokemonName.EndsWith("-mega") ||
               pokemonName.EndsWith("-x") ||
               pokemonName.EndsWith("-y") ||
               pokemonName.EndsWith("-origin") ||
               pokemonName.EndsWith("-10") ||
               pokemonName.EndsWith("-complete") ||
               pokemonName.EndsWith("-ultra") ||
               pokemonName.EndsWith("-crowned") ||
               pokemonName.EndsWith("-eternamax") ||
               pokemonName.EndsWith("-blade");
    }

    /// <summary>
    ///     Creates a visual health bar representation for Discord messages.
    /// </summary>
    /// <param name="maxHealth">The maximum health value.</param>
    /// <param name="health">The current health value.</param>
    /// <param name="healthDashes">The number of dashes to use in the health bar.</param>
    /// <returns>A string containing Discord emoji that display as a health bar.</returns>
    public string DoHealth(int maxHealth, int health, int healthDashes = 10)
    {
        var dashConvert = maxHealth / healthDashes;
        var currentDashes = health / dashConvert;
        var remainingHealth = healthDashes - currentDashes;
        var cur = $"{Math.Round((double)health)}/{maxHealth}";

        var healthDisplay = string.Join("", Enumerable.Repeat("▰", currentDashes));
        var remainingDisplay = string.Join("", Enumerable.Repeat("▱", remainingHealth));
        var percent = (int)Math.Floor((double)health / maxHealth * 100);
        if (percent < 1)
            percent = 0;

        var result = "";
        var emojis = new Dictionary<bool, Dictionary<string, string>>
        {
            [true] = new()
            {
                ["left"] = "<:bar_e1:1059717853714055268>",
                ["center"] = "<:bar_e2:1059717860651434024>",
                ["right"] = "<:bar_e3:1059717856595558402>"
            },
            [false] = new()
            {
                ["left"] = "<:bar1:1059670478123442197>",
                ["center"] = "<:bar2:1059670474046574692>",
                ["right"] = "<:bar3:1059670475481030699>"
            }
        };

        for (var i = 0; i < healthDashes; i++)
        {
            var cur2 = emojis[i < currentDashes];
            if (i == 0)
                result += cur2["left"];
            else if (i == healthDashes - 1)
                result += cur2["right"];
            else
                result += cur2["center"];
        }

        return result;
    }

    /// <summary>
    ///     Calculates breeding success multiplier based on level.
    /// </summary>
    /// <param name="level">The level to calculate the multiplier for.</param>
    /// <returns>A formatted string representing the breeding multiplier.</returns>
    public string CalculateBreedingMultiplier(int level)
    {
        var difference = 0.02;
        return $"{Math.Round(1 + level * difference, 2)}x";
    }

    /// <summary>
    ///     Calculates IV multiplier based on level.
    /// </summary>
    /// <param name="level">The level to calculate the multiplier for.</param>
    /// <returns>A formatted string representing the IV multiplier.</returns>
    public string CalculateIvMultiplier(int level)
    {
        var difference = 0.5;
        return $"{Math.Round(level * difference, 1)}%";
    }

    /// <summary>
    ///     Checks if a mission with the specified key is active.
    /// </summary>
    /// <param name="key">The mission key to check.</param>
    /// <returns>True if there are active missions with the key, false otherwise.</returns>
    public async Task<bool> CheckActive(string key)
    {
        var activeMissions = await _mongoService.Missions
            .Find(m => m.Active == true && m.Key == key)
            .ToListAsync();

        return activeMissions.Any();
    }
}