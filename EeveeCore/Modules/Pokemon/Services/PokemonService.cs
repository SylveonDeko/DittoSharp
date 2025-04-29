using System.Reflection;
using EeveeCore.Common.Logic;
using EeveeCore.Database.DbContextStuff;
using EeveeCore.Database.Linq.Models;
using EeveeCore.Database.Models.Mongo.Pokemon;
using EeveeCore.Database.Models.PostgreSQL.Pokemon;
using EeveeCore.Modules.Spawn.Constants;
using EeveeCore.Services.Impl;
using Humanizer;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace EeveeCore.Modules.Pokemon.Services;

/// <summary>
///     Service class for handling Pokemon-related operations.
/// </summary>
public class PokemonService(
    DiscordShardedClient client,
    DbContextProvider dbProvider,
    IMongoService mongo,
    RedisCache redis)
    : INService
{
    /// <summary>
    ///     Gets all Pokemon belonging to a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A list of Pokemon owned by the user.</returns>
    public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetUserPokemons(ulong userId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();
        var pokeIds = dbContext.UserPokemon.Where(x => x.Owner == userId);

        if (!pokeIds.Any())
            return [];

        return await pokeIds.ToListAsyncEF();
    }

    /// <summary>
    ///     Gets a Pokemon by its ID.
    /// </summary>
    /// <param name="pokemonId">The ID of the Pokemon.</param>
    /// <returns>The Pokemon with the specified ID, or null if not found.</returns>
    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon?> GetPokemonById(ulong pokemonId)
    {
        try
        {
            await using var dbContext = await dbProvider.GetContextAsync();
            return await dbContext.UserPokemon
                .FirstOrDefaultAsyncEF(p => p.Id == pokemonId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error retrieving Pokemon by ID {PokemonId}", pokemonId);
            throw;
        }
    }

    /// <summary>
    ///     Recursively gets the evolutionary descendants of a Pokemon.
    /// </summary>
    /// <param name="rawEvos">The list of Pokemon evolutions.</param>
    /// <param name="speciesId">The species ID to get descendants for.</param>
    /// <param name="prefix">The prefix to use for formatting.</param>
    /// <returns>A formatted string showing the evolution tree.</returns>
    private async Task<string> GetKids(List<PokemonFile> rawEvos, int speciesId, string prefix = null)
    {
        var result = "";
        foreach (var poke in rawEvos.Where(poke => poke.EvolvesFromSpeciesId == speciesId))
        {
            var reqs = await GetReqs(poke.PokemonId.Value);
            result += $"{prefix}├─{poke.Identifier} {reqs}\n";
            result += await GetKids(rawEvos, poke.PokemonId.Value, $"{prefix}│ ");
        }

        return result;
    }

    /// <summary>
    ///     Gets the evolution requirements for a Pokemon.
    /// </summary>
    /// <param name="pokeId">The ID of the Pokemon.</param>
    /// <returns>A formatted string showing the evolution requirements.</returns>
    private async Task<string> GetReqs(int pokeId)
    {
        var reqs = new List<string>();

        var evoReq = await mongo.Evolution
            .Find(e => e.EvolvedSpeciesId == pokeId)
            .FirstOrDefaultAsync();

        if (evoReq == null) return "";

        if (evoReq.TriggerItemId.HasValue)
        {
            var item = await mongo.Items
                .Find(i => i.ItemId == evoReq.TriggerItemId)
                .FirstOrDefaultAsync();
            if (item != null)
                reqs.Add($"apply `{item.Identifier}`");
        }

        if (evoReq.HeldItemId.HasValue)
        {
            var item = await mongo.Items
                .Find(i => i.ItemId == evoReq.HeldItemId)
                .FirstOrDefaultAsync();
            if (item != null)
                reqs.Add($"hold `{item.Identifier}`");
        }

        if (evoReq.GenderId > -1)
            reqs.Add($"is `{(evoReq.GenderId == 1 ? "female" : "male")}`");

        if (evoReq.MinimumLevel.HasValue)
            reqs.Add($"lvl `{evoReq.MinimumLevel}`");

        if (evoReq.KnownMoveId.HasValue)
        {
            var move = await mongo.Moves
                .Find(m => m.MoveId == evoReq.KnownMoveId)
                .FirstOrDefaultAsync();
            if (move != null)
                reqs.Add($"knows `{move.Identifier}`");
        }

        if (evoReq.MinimumHappiness.HasValue)
            reqs.Add($"happiness `{evoReq.MinimumHappiness}`");

        if (evoReq.RelativePhysicalStats.HasValue)
            reqs.Add(evoReq.RelativePhysicalStats switch
            {
                0 => "atk = def",
                1 => "atk > def",
                -1 => "atk < def",
                _ => ""
            });

        if (!string.IsNullOrEmpty(evoReq.Region))
            reqs.Add($"region `{evoReq.Region}`");

        return reqs.Any() ? $"({string.Join(", ", reqs)})" : "";
    }

    /// <summary>
    ///     Gets the evolution line for a Pokemon.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokemon.</param>
    /// <returns>A formatted string showing the evolution line.</returns>
    public async Task<string> GetEvolutionLine(string? pokemonName)
    {
        try
        {
            // Get base name using the same logic as Python
            var formParts = pokemonName.ToLower().Split('-');
            var formSuffix = formParts.Length > 1 ? formParts[^1] : "";
            var baseName = "";

            switch (formSuffix)
            {
                case "blaze" or "aqua":
                    baseName = "tauros-paldea";
                    break;
                case "alola" or "galar" or "hisui" or "paldea":
                    formSuffix = "";
                    break;
            }

            if (formSuffix == "zen-galar") baseName = "darmanitan-galar";

            if (string.IsNullOrEmpty(baseName))
                baseName = !string.IsNullOrEmpty(formSuffix)
                    ? pokemonName.ToLower().Replace(formSuffix, "").TrimEnd('-')
                    : pokemonName.ToLower();

            var pfile = await mongo.PFile
                .Find(p => p.Identifier == baseName)
                .FirstOrDefaultAsync();

            if (pfile == null)
                return "";

            var rawEvos = await mongo.PFile
                .Find(p => p.EvolutionChainId == pfile.EvolutionChainId)
                .ToListAsync();

            // Get evolution line starting from the first in the chain (no evolves_from)
            var evoLine = await GetKids(rawEvos, pfile.PokemonId.Value, "");
            return evoLine;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting evolution line for {PokemonName}", pokemonName);
            return "";
        }
    }

    /// <summary>
    ///     Gets detailed information about a Pokemon.
    /// </summary>
    /// <param name="identifier">The identifier of the Pokemon.</param>
    /// <returns>A PokemonInfo object containing details about the Pokemon.</returns>
    public async Task<PokemonInfo> GetPokemonInfo(string? identifier)
    {
        try
        {
            var formInfo = await mongo.Forms
                .Find(f => f.Identifier.Equals(identifier, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefaultAsync();

            if (formInfo == null)
                return null;

            var pokemonTypes = await mongo.PokemonTypes
                .Find(t => t.PokemonId == formInfo.PokemonId)
                .FirstOrDefaultAsync();

            if (pokemonTypes == null)
                return null;

            var typeNames = new List<string>();
            foreach (var typeId in pokemonTypes.Types)
            {
                var type = await mongo.Types
                    .Find(t => t.TypeId == typeId)
                    .FirstOrDefaultAsync();
                typeNames.Add(type?.Identifier ?? "unknown");
            }

            var stats = await mongo.PokemonStats
                .Find(s => s.PokemonId == formInfo.PokemonId)
                .FirstOrDefaultAsync();

            // Get abilities
            var abilities = new List<string>();
            var abilityCursor = mongo.PokeAbilities.Find(a => a.PokemonId == formInfo.PokemonId);
            await abilityCursor.ForEachAsync(async abilityRef =>
            {
                var ability = await mongo.Abilities.Find(a => a.AbilityId == abilityRef.AbilityId)
                    .FirstOrDefaultAsync();
                if (ability != null)
                    abilities.Add(ability.Identifier);
            });

            // Get egg groups
            var eggGroups = new List<string>();
            var eggGroupsCursor = mongo.EggGroups.Find(e => e.SpeciesId == formInfo.PokemonId);
            await eggGroupsCursor.ForEachAsync(async eggGroupRef =>
            {
                var eggGroup = await mongo.EggGroupsInfo.Find(e => e.Id == eggGroupRef.Id).FirstOrDefaultAsync();
                if (eggGroup != null)
                    eggGroups.Add(eggGroup.Identifier);
            });

            return new PokemonInfo
            {
                Id = formInfo.PokemonId,
                Name = formInfo.Identifier,
                Types = typeNames,
                FormIdentifier = formInfo.FormIdentifier,
                Weight = formInfo.Weight.HasValue ? formInfo.Weight / 10.0f : 0,
                Abilities = abilities,
                EggGroups = eggGroups,
                Stats = new PokemonStats
                {
                    Hp = stats?.Stats[0] ?? 0,
                    Attack = stats?.Stats[1] ?? 0,
                    Defense = stats?.Stats[2] ?? 0,
                    SpecialAttack = stats?.Stats[3] ?? 0,
                    SpecialDefense = stats?.Stats[4] ?? 0,
                    Speed = stats?.Stats[5] ?? 0
                }
            };
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting Pokemon info for {Identifier}", identifier);
            throw;
        }
    }

    /// <summary>
    ///     Selects a Pokemon from the user's collection.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="pokeNumber">The number of the Pokemon to select.</param>
    /// <returns>A tuple indicating success and a message.</returns>
    public async Task<(bool Success, string Message)> SelectPokemon(ulong userId, int pokeNumber)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        if (user == null)
            return (false, "You have not started! Use /start first.");

        if (pokeNumber <= 0)
            return (false, "Invalid pokemon number.");

        // Find the Pokemon in the join table using position
        var position = pokeNumber - 1;
        var ownership = await dbContext.UserPokemonOwnerships
            .Where(o => o.UserId == userId && o.Position == position)
            .FirstOrDefaultAsyncEF();

        if (ownership == null)
            return (false, "You don't have that many Pokemon.");

        var pokemon = await dbContext.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == ownership.PokemonId);

        if (pokemon == null)
            return (false, "That pokemon does not exist!");

        user.Selected = pokemon.Id;
        await dbContext.SaveChangesAsync();

        return (true, $"You have selected your {pokemon.PokemonName}");
    }

    /// <summary>
    ///     Gets the currently selected Pokemon for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The selected Pokemon, or null if none is selected.</returns>
    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon?> GetSelectedPokemon(ulong userId)
    {
        try
        {
            await using var dbContext = await dbProvider.GetContextAsync();

            var selectedId = await dbContext.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Selected)
                .FirstOrDefaultAsyncEF();

            if (selectedId == 0)
                return null;

            return await dbContext.UserPokemon
                .FirstOrDefaultAsyncEF(p => p.Id == selectedId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting selected Pokemon for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    ///     Gets the newest Pokemon owned by a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The newest Pokemon, or null if the user has no Pokemon.</returns>
    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon?> GetNewestPokemon(ulong userId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Find the ownership record with the highest position (newest Pokemon)
        var newestOwnership = await dbContext.UserPokemonOwnerships
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.Position)
            .FirstOrDefaultAsyncEF();

        if (newestOwnership == null)
            return null;

        // Get the Pokemon using the ID from the ownership record
        return await dbContext.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == newestOwnership.PokemonId);
    }

    /// <summary>
    ///     Gets a list of all Pokemon in the database.
    /// </summary>
    /// <returns>A list of all Pokemon.</returns>
    public async Task<List<PokemonFile>> GetAllPokemon()
    {
        return await mongo.PFile
            .Find(x => x.PokemonId >= 0)
            .ToListAsync();
    }

    /// <summary>
    ///     Gets a Pokemon by its number in the user's collection.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="number">The number of the Pokemon.</param>
    /// <returns>The Pokemon with the specified number, or null if not found.</returns>
    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon?> GetPokemonByNumber(ulong userId, int number)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Convert from 1-based user interface numbering to 0-based database position
        var position = number - 1;

        // Find the Pokemon ID in the ownership table using position
        var ownership = await dbContext.UserPokemonOwnerships
            .Where(o => o.UserId == userId && o.Position == position)
            .FirstOrDefaultAsyncEF();

        if (ownership == null)
            return null;

        // Get the actual Pokemon from the Pokemon table
        return (await dbContext.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == ownership.PokemonId))!;
    }

    /// <summary>
    ///     Removes a Pokemon from a user's collection.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="pokemonId">The ID of the Pokemon to remove.</param>
    /// <param name="releasePokemon">Whether to permanently delete the Pokemon.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task RemoveUserPokemon(ulong userId, ulong pokemonId, bool releasePokemon = false)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Begin transaction to ensure consistency
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var pokemon = await dbContext.UserPokemon
                .FirstOrDefaultAsyncEF(p => p.Id == pokemonId);

            if (pokemon == null)
                throw new Exception("Pokemon not found");

            if (pokemon.Favorite)
                throw new Exception("Cannot remove favorited Pokemon");

            // Find the ownership record
            var ownership = await dbContext.UserPokemonOwnerships
                .FirstOrDefaultAsyncEF(o => o.UserId == userId && o.PokemonId == pokemonId);

            if (ownership == null)
                throw new Exception("Pokemon not found in user's collection");

            // Store the position for reordering
            var position = ownership.Position;

            // Remove the ownership record
            dbContext.UserPokemonOwnerships.Remove(ownership);

            // Update all higher positions to maintain order
            await dbContext.UserPokemonOwnerships
                .Where(o => o.UserId == userId && o.Position > position)
                .ForEachAsyncEF(o => o.Position--);

            // Check if this was the selected Pokemon
            var user = await dbContext.Users
                .FirstOrDefaultAsyncEF(u => u.UserId == userId);

            if (user != null && user.Selected == pokemonId)
                user.Selected = 0;

            if (releasePokemon)
                // Actually delete the Pokemon for release
                dbContext.UserPokemon.Remove(pokemon);
            else
                // For sacrifice, just unlink it
                pokemon.Owner = 0;

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    ///     Gets the current soul gauge value for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The current soul gauge value.</returns>
    public async Task<int> GetUserSoulGauge(ulong userId)
    {
        var value = await redis.Redis.GetDatabase().StringGetAsync($"soul_gauge:{userId}");
        return value.HasValue ? (int)value : 0;
    }

    /// <summary>
    ///     Increments the soul gauge for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="increment">The amount to increment by.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task IncrementSoulGauge(ulong userId, double increment)
    {
        var key = $"soul_gauge:{userId}";
        await redis.Redis.GetDatabase().StringIncrementAsync(key, increment);

        // Cap at 1000
        var current = await redis.Redis.GetDatabase().StringGetAsync(key);
        if (current.HasValue && (double)current > 1000)
            await redis.Redis.GetDatabase().StringSetAsync(key, 1000);
    }

    /// <summary>
    ///     Gets the available forms for a Pokemon.
    /// </summary>
    /// <param name="val">The name of the Pokemon.</param>
    /// <returns>A string listing the available forms.</returns>
    public async Task<string> GetPokemonForms(string? val)
    {
        try
        {
            var forms = new List<string>();

            // Handle special cases first
            var lowerVal = val.ToLower();
            switch (lowerVal)
            {
                case "spewpa" or "scatterbug" or "mew":
                    forms = ["None"];
                    break;
                case "tauros-paldea":
                    forms = ["aqua-paldea", "blaze-paldea"];
                    break;
                default:
                {
                    // Get forms from MongoDB
                    var cursor = mongo.Forms.Find(
                        Builders<Form>.Filter.Regex(f => f.Identifier, new BsonRegularExpression($".*{val}.*", "i"))
                    );

                    forms = await cursor.Project(f => f.FormIdentifier)
                        .ToListAsync();

                    // Filter out empty strings and specific region forms
                    forms = forms.Where(f => !string.IsNullOrEmpty(f))
                        .Where(f => f is not "Galar" and not "Alola" and not "Hisui" and not "Paldea")
                        .ToList();

                    if (!forms.Any()) forms = ["None"];
                    break;
                }
            }

            return string.Join("\n", forms.Select(f => f.Capitalize()));
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting Pokemon forms for {Val}", val);
            return "None";
        }
    }

    /// <summary>
    ///     Checks if a user has unlocked ancient Pokemon.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>True if ancient Pokemon are unlocked, false otherwise.</returns>
    public async Task<bool> CheckUserHasAncientUnlock(ulong userId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();
        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        return user?.AncientUnlocked ?? false;
    }

    /// <summary>
    ///     Gets the form information and image URL for a Pokemon.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokemon.</param>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <param name="skin">The skin of the Pokemon, if any.</param>
    /// <returns>A tuple containing the form and image URL.</returns>
    public async Task<(Form Form, string ImageUrl)> GetPokemonFormInfo(string? pokemonName, bool shiny = false,
        bool radiant = false, string skin = null)
    {
        var identifier = await mongo.Forms
            .Find(f => f.Identifier.Equals(pokemonName.ToLower(), StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefaultAsync();

        if (identifier == null)
            throw new ArgumentException($"Invalid name ({pokemonName}) passed to GetPokemonFormInfo");

        var suffix = identifier.FormIdentifier;
        int pokemonId;
        var formId = 0;

        if (!string.IsNullOrEmpty(suffix) && pokemonName.EndsWith(suffix))
        {
            formId = (int)(identifier.FormOrder - 1)!;
            var formName = pokemonName[..^(suffix.Length + 1)];

            var pokemonIdentifier = await mongo.Forms
                .Find(f => f.Identifier.Equals(formName, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefaultAsync();

            if (pokemonIdentifier == null)
                throw new ArgumentException($"Invalid name ({pokemonName}) passed to GetPokemonFormInfo");

            pokemonId = pokemonIdentifier.PokemonId;
        }
        else
        {
            pokemonId = identifier.PokemonId;
        }

        var fileType = "png";
        var skinPath = "";
        if (!string.IsNullOrEmpty(skin))
        {
            if (skin.EndsWith("_gif"))
                fileType = "gif";
            skinPath = $"{skin}/";
        }

        // Assuming you have a radiant_placeholder_pokes collection
        var isPlaceholder = await mongo.RadiantPlaceholders
            .Find(p => p.Name.Equals(pokemonName, StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefaultAsync();

        var radiantPath = radiant ? "radiant/" : "";
        if (radiant && isPlaceholder != null)
            return (identifier, "placeholder.png");

        var shinyPath = shiny ? "shiny/" : "";
        var fileName = $"{radiantPath}{shinyPath}{skinPath}{pokemonId}-{formId}-.{fileType}";

        var imageUrl = string.IsNullOrEmpty(skin)
            ? $"https://images.mewdeko.tech/images/{fileName}"
            : $"https://images.mewdeko.tech/skins/{fileName}";

        return (identifier, imageUrl);
    }

    /// <summary>
    ///     Calculates the stats for a Pokemon based on its base stats, IVs, EVs, and level.
    /// </summary>
    /// <param name="pokemon">The Pokemon to calculate stats for.</param>
    /// <param name="baseStats">The base stats of the Pokemon.</param>
    /// <returns>A CalculatedStats object containing the calculated stats.</returns>
    public Task<CalculatedStats> CalculatePokemonStats(Database.Models.PostgreSQL.Pokemon.Pokemon? pokemon,
        PokemonStats baseStats)
    {
        var natureModifier = GetNatureModifier(pokemon.Nature);

        // Calculate HP differently from other stats
        var maxHp = CalculateHpStat(baseStats.Hp, pokemon.Level, pokemon.HpIv, pokemon.HpEv);

        return Task.FromResult(new CalculatedStats
        {
            MaxHp = maxHp,
            Attack = CalculateStat(baseStats.Attack, pokemon.Level, pokemon.AttackIv, pokemon.AttackEv,
                natureModifier.Item1 == "Attack" ? 1.1 :
                natureModifier.Item2 == "Attack" ? 0.9 : 1.0),
            Defense = CalculateStat(baseStats.Defense, pokemon.Level, pokemon.DefenseIv, pokemon.DefenseEv,
                natureModifier.Item1 == "Defense" ? 1.1 :
                natureModifier.Item2 == "Defense" ? 0.9 : 1.0),
            SpecialAttack = CalculateStat(baseStats.SpecialAttack, pokemon.Level, pokemon.SpecialAttackIv,
                pokemon.SpecialAttackEv,
                natureModifier.Item1 == "SpecialAttack" ? 1.1 :
                natureModifier.Item2 == "SpecialAttack" ? 0.9 : 1.0),
            SpecialDefense = CalculateStat(baseStats.SpecialDefense, pokemon.Level, pokemon.SpecialDefenseIv,
                pokemon.SpecialDefenseEv,
                natureModifier.Item1 == "SpecialDefense" ? 1.1 :
                natureModifier.Item2 == "SpecialDefense" ? 0.9 : 1.0),
            Speed = CalculateStat(baseStats.Speed, pokemon.Level, pokemon.SpeedIv, pokemon.SpeedEv,
                natureModifier.Item1 == "Speed" ? 1.1 :
                natureModifier.Item2 == "Speed" ? 0.9 : 1.0)
        });
    }

    /// <summary>
    ///     Gets all dead Pokemon for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A list of dead Pokemon for the user.</returns>
    public async Task<List<DeadPokemon>> GetDeadPokemon(ulong userId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Check for special admin case
        var actualUserId = userId == 1081889316848017539 ? 946611594488602694UL : userId;

        // Get all Pokemon IDs owned by the user from the ownership table
        var ownedPokemonIds = await dbContext.UserPokemonOwnerships
            .Where(o => o.UserId == actualUserId)
            .Select(o => o.PokemonId)
            .ToListAsyncEF();

        if (ownedPokemonIds.Count == 0)
            return new List<DeadPokemon>();

        // Get all dead Pokemon that match the user's owned Pokemon IDs
        return await dbContext.DeadPokemon
            .AsNoTracking()
            .Where(d => ownedPokemonIds.Contains(d.Id))
            .ToListAsyncEF();
    }

    /// <summary>
    ///     Checks if dead Pokemon references in a user's collection correspond to invalid references.
    ///     This helps identify if references that couldn't be migrated might actually be dead Pokemon.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <returns>A tuple containing lists of potentially recoverable and unrecoverable invalid references.</returns>
    public async Task<(List<InvalidPokemonReference> PotentialDeadPokemon, List<InvalidPokemonReference>
            UnrecoverableReferences)>
        CheckInvalidReferencesAgainstDeadPokemon(ulong userId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Get all invalid references for this user
        var invalidReferences = await dbContext.InvalidPokemonReferences
            .Where(r => r.UserId == userId)
            .ToListAsyncEF();

        if (invalidReferences.Count == 0)
            return (new List<InvalidPokemonReference>(), new List<InvalidPokemonReference>());

        // Get all dead Pokemon IDs
        var deadPokemonIds = await dbContext.DeadPokemon
            .Select(d => d.Id)
            .ToListAsyncEF();

        // Check which invalid references match dead Pokemon IDs
        var potentialDeadPokemon = invalidReferences
            .Where(r => deadPokemonIds.Contains(r.PokemonId))
            .ToList();

        var unrecoverableReferences = invalidReferences
            .Where(r => !deadPokemonIds.Contains(r.PokemonId))
            .ToList();

        return (potentialDeadPokemon, unrecoverableReferences);
    }

    /// <summary>
    ///     Recovers dead Pokemon references for a user by creating ownership entries.
    ///     This fixes cases where a user has dead Pokemon that weren't properly migrated to the ownership table.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <returns>The number of dead Pokemon references that were recovered.</returns>
    public async Task<int> RecoverDeadPokemonReferences(ulong userId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var (potentialDeadPokemon, _) = await CheckInvalidReferencesAgainstDeadPokemon(userId);

            if (potentialDeadPokemon.Count == 0)
                return 0;

            // Find the highest position for this user
            var highestPosition = await dbContext.UserPokemonOwnerships
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Position)
                .Select(o => (int?)o.Position)
                .FirstOrDefaultAsyncEF() ?? -1;

            // Create ownership entries for the dead Pokemon
            var ownerships = new List<UserPokemonOwnership>();

            foreach (var reference in potentialDeadPokemon)
            {
                highestPosition++;

                ownerships.Add(new UserPokemonOwnership
                {
                    UserId = userId,
                    PokemonId = reference.PokemonId,
                    Position = highestPosition
                });
            }

            await dbContext.UserPokemonOwnerships.AddRangeAsync(ownerships);

            // Remove the recovered references from invalid references
            var referenceIds = potentialDeadPokemon.Select(r => r.PokemonId).ToList();
            await dbContext.InvalidPokemonReferences
                .Where(r => r.UserId == userId && referenceIds.Contains(r.PokemonId))
                .ExecuteDeleteAsync();

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return potentialDeadPokemon.Count;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Log.Error(ex, "Error recovering dead Pokemon references for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    ///     Resurrects a list of dead Pokemon.
    /// </summary>
    /// <param name="deadPokemon">The list of dead Pokemon to resurrect.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task ResurrectPokemon(List<DeadPokemon> deadPokemon)
    {
        if (deadPokemon == null || !deadPokemon.Any())
            return;

        await using var dbContext = await dbProvider.GetContextAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // Get all IDs for batch deletion
            var ids = deadPokemon.Select(d => d.Id).ToList();

            // Delete from dead Pokemon table
            await dbContext.DeadPokemon
                .Where(d => ids.Contains(d.Id))
                .ExecuteDeleteAsync();

            // Convert to live Pokemon
            var livePokemon = deadPokemon.Select(MapDeadToLivePokemon).ToList();

            // Add in batches for better performance
            foreach (var batch in livePokemon.Chunk(100))
                await dbContext.UserPokemon.AddRangeAsync(batch);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            Log.Error(e, "Failed to resurrect pokemon");
            throw;
        }
    }

    /// <summary>
    ///     Maps a dead Pokemon to a live Pokemon.
    /// </summary>
    /// <param name="dead">The dead Pokemon to map.</param>
    /// <returns>A live Pokemon mapped from the dead Pokemon.</returns>
    private static Database.Models.PostgreSQL.Pokemon.Pokemon MapDeadToLivePokemon(DeadPokemon dead)
    {
        return new Database.Models.PostgreSQL.Pokemon.Pokemon
        {
            PokemonName = dead.PokemonName,
            HpIv = dead.HpIv,
            AttackIv = dead.AttackIv,
            DefenseIv = dead.DefenseIv,
            SpecialAttackIv = dead.SpecialAttackIv,
            SpecialDefenseIv = dead.SpecialDefenseIv,
            SpeedIv = dead.SpeedIv,
            HpEv = dead.HpEv,
            AttackEv = dead.AttackEv,
            DefenseEv = dead.DefenseEv,
            SpecialAttackEv = dead.SpecialAttackEv,
            SpecialDefenseEv = dead.SpecialDefenseEv,
            SpeedEv = dead.SpeedEv,
            Level = dead.Level,
            Moves = dead.Moves,
            HeldItem = dead.HeldItem,
            Experience = dead.Experience,
            Nature = dead.Nature,
            ExperienceCap = dead.ExperienceCap,
            Nickname = dead.Nickname,
            Price = dead.Price,
            MarketEnlist = dead.MarketEnlist,
            Happiness = dead.Happiness,
            Favorite = dead.Favorite,
            AbilityIndex = dead.AbilityIndex,
            Gender = dead.Gender,
            Shiny = dead.Shiny,
            Counter = dead.Counter,
            Name = dead.Name,
            Timestamp = dead.Timestamp,
            CaughtBy = dead.CaughtBy,
            Radiant = dead.Radiant,
            Tags = dead.Tags,
            Skin = dead.Skin,
            Tradable = dead.Tradable,
            Breedable = dead.Breedable,
            Champion = dead.Champion,
            Temporary = dead.Temporary,
            Voucher = dead.Voucher,
            Owner = dead.Owner,
            Owned = dead.Owned
        };
    }


    /// <summary>
    ///     Gets the index of a Pokemon in a user's collection.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="pokemonId">The ID of the Pokemon.</param>
    /// <returns>The index of the Pokemon (1-based), or -1 if not found.</returns>
    public async Task<int> GetPokemonIndex(ulong userId, ulong pokemonId)
    {
        await using var dbContext = await dbProvider.GetContextAsync();

        // Directly query the position from the join table
        var ownership = await dbContext.UserPokemonOwnerships
            .Where(o => o.UserId == userId && o.PokemonId == pokemonId)
            .FirstOrDefaultAsyncEF();

        if (ownership == null)
            return -1;

        // Return 1-based index for user interface
        return ownership.Position + 1;
    }

    /// <summary>
    ///     Gets a filtered, sorted, and paginated list of Pokemon for display.
    ///     Handles all database operations including party data retrieval and filtering.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="sortOrder">How to sort the Pokemon.</param>
    /// <param name="filter">Filter to apply (all, shiny, radiant, etc).</param>
    /// <param name="search">Optional search term.</param>
    /// <returns>A tuple with filtered Pokemon list, collection statistics, and user data for display.</returns>
    public async Task<(List<PokemonListEntry> FilteredList, Dictionary<string, int> Stats, HashSet<int> PartyPokemon,
            ulong? SelectedPokemon)>
        GetFilteredPokemonList(ulong userId, SortOrder sortOrder, string filter, string search)
    {
        try
        {
            await using var db = await dbProvider.GetContextAsync();
            var conn = db.CreateLinqToDBConnection();

            // Get user data for party and selected - using LinqToDB, not EF Core, EF Core is slow sometimes.
            var userData = await conn.GetTable<User>()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.Party, u.Selected })
                .FirstOrDefaultAsyncLinqToDB();

            if (userData == null)
            {
                var userExists = await conn.GetTable<User>()
                    .AnyAsyncLinqToDB(u => u.UserId == userId);

                if (!userExists)
                    return (new List<PokemonListEntry>(), null, new HashSet<int>(), null);
            }

            // Create lookup sets for efficient checking
            var partyLookup = userData?.Party != null
                ? new HashSet<int>(userData.Party.Where(id => id != 0))
                : new HashSet<int>();

            // Get Pokemon from the ownership table using JOIN instead of separate queries
            var query = from ownership in conn.GetTable<UserPokemonOwnership>()
                join pokemon in conn.GetTable<Database.Models.PostgreSQL.Pokemon.Pokemon>()
                    on ownership.PokemonId equals pokemon.Id
                where ownership.UserId == userId
                select new { Pokemon = pokemon, ownership.Position };

            // Apply filter
            query = filter switch
            {
                "shiny" => query.Where(p => p.Pokemon.Shiny == true),
                "radiant" => query.Where(p => p.Pokemon.Radiant == true),
                "shadow" => query.Where(p => p.Pokemon.Skin == "shadow"),
                "legendary" => query.Where(p => PokemonList.LegendList.Contains(p.Pokemon.PokemonName)),
                "favorite" => query.Where(p => p.Pokemon.Favorite),
                "champion" => query.Where(p => p.Pokemon.Champion),
                "party" => query.Where(p => partyLookup.Contains((int)p.Pokemon.Id)),
                "market" => query.Where(p => p.Pokemon.MarketEnlist),
                _ => query
            };

            // Get collection stats before applying sort
            var stats = await CalculateCollectionStatistics(userId, conn);

            // Apply sorting
            query = sortOrder switch
            {
                SortOrder.Iv => query.OrderByDescending(p =>
                    p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                    p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv),
                SortOrder.Level => query.OrderByDescending(p => p.Pokemon.Level),
                SortOrder.Name => query.OrderBy(p => p.Pokemon.PokemonName),
                SortOrder.Recent => query.OrderByDescending(p => p.Pokemon.Timestamp),
                SortOrder.Favorite => query.OrderByDescending(p => p.Pokemon.Favorite),
                SortOrder.Party => query.OrderByDescending(p => partyLookup.Contains((int)p.Pokemon.Id)),
                SortOrder.Champion => query.OrderByDescending(p => p.Pokemon.Champion),
                _ => query.OrderBy(p => p.Position) // Default sort by position (index in collection)
            };

            // Create the projection to get all needed fields
            var projectedQuery = query.Select(p => new
            {
                p.Pokemon.Id,
                p.Pokemon.PokemonName,
                p.Pokemon.Nickname,
                p.Pokemon.Level,
                IvTotal = p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                          p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv,
                p.Pokemon.Shiny,
                p.Pokemon.Radiant,
                p.Pokemon.Skin,
                p.Pokemon.Gender,
                p.Pokemon.Timestamp,
                p.Pokemon.Favorite,
                p.Pokemon.Champion,
                p.Pokemon.MarketEnlist,
                p.Pokemon.HeldItem,
                p.Pokemon.Moves,
                p.Pokemon.Tags,
                p.Pokemon.Tradable,
                p.Pokemon.Breedable,
                Position = p.Position + 1 // Convert to 1-based indexing for display
            });

            // Execute the query and get the results
            var pokemonData = await projectedQuery.ToListAsyncLinqToDB();

            if (!string.IsNullOrEmpty(search))
            {
                pokemonData = pokemonData.Where(p =>
                    p.PokemonName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (p.Nickname != null && p.Nickname.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Tags != null && p.Tags.Any(t => t != null && t.Contains(search, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Moves != null && p.Moves.Any(m => m != null && m.Contains(search, StringComparison.OrdinalIgnoreCase)))
                ).ToList();
            }

            // If we need to sort by type, we need to process the results further
            if (sortOrder == SortOrder.Type)
            {
                var typedPokemon = new List<(PokemonListEntry entry, string primaryType)>();

                foreach (var p in pokemonData)
                {
                    var entry = new PokemonListEntry(
                        p.Id,
                        p.PokemonName,
                        p.Position,
                        p.Level,
                        p.IvTotal / 186.0,
                        p.Shiny ?? false,
                        p.Radiant ?? false,
                        p.Skin,
                        p.Gender,
                        p.Nickname,
                        p.Favorite,
                        p.Champion,
                        p.MarketEnlist,
                        p.HeldItem,
                        p.Moves ?? [],
                        p.Tags ?? [],
                        p.Tradable,
                        p.Breedable,
                        p.Timestamp
                    );

                    var primaryType = await GetPrimaryType(p.PokemonName);
                    typedPokemon.Add((entry, primaryType));
                }

                var sortedList = typedPokemon
                    .OrderBy(tp => tp.primaryType)
                    .ThenBy(tp => tp.entry.Name)
                    .Select(tp => tp.entry)
                    .ToList();

                return (sortedList, stats, partyLookup, userData?.Selected);
            }

            // Convert to PokemonListEntry objects
            var result = pokemonData.Select(p => new PokemonListEntry(
                p.Id,
                p.PokemonName,
                p.Position,
                p.Level,
                p.IvTotal / 186.0,
                p.Shiny ?? false,
                p.Radiant ?? false,
                p.Skin,
                p.Gender,
                p.Nickname,
                p.Favorite,
                p.Champion,
                p.MarketEnlist,
                p.HeldItem,
                p.Moves ?? [],
                p.Tags ?? [],
                p.Tradable,
                p.Breedable,
                p.Timestamp
            )).ToList();

            return (result, stats, partyLookup, userData?.Selected);
        }
        catch (Exception e)
        {
            Log.Error(e,
                "Error getting filtered Pokemon list for user {UserId} with sort {SortOrder}, filter {Filter}, search {Search}",
                userId, sortOrder, filter, search);
            throw;
        }
    }

    /// <summary>
    ///     Calculates statistics about a user's Pokemon collection.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="conn">The LinqToDB connection.</param>
    /// <returns>A dictionary containing count statistics for different Pokemon categories.</returns>
    private async Task<Dictionary<string, int>> CalculateCollectionStatistics(ulong userId, DataConnection conn)
    {
        // Get all ownership records for this user
        var ownerships = conn.GetTable<UserPokemonOwnership>()
            .Where(o => o.UserId == userId);

        // Join with Pokemon table to get details needed for stats
        var pokemonQuery = from o in ownerships
            join p in conn.GetTable<Database.Models.PostgreSQL.Pokemon.Pokemon>()
                on o.PokemonId equals p.Id
            select p;

        // Get total count
        var total = await pokemonQuery.CountAsyncLinqToDB();

        // Get counts for different categories
        var shinyCount = await pokemonQuery.CountAsyncLinqToDB(p => p.Shiny == true);
        var radiantCount = await pokemonQuery.CountAsyncLinqToDB(p => p.Radiant == true);
        var shadowCount = await pokemonQuery.CountAsyncLinqToDB(p => p.Skin == "shadow");
        var legendaryCount = await pokemonQuery.CountAsyncLinqToDB(p => PokemonList.LegendList.Contains(p.PokemonName));
        var favoriteCount = await pokemonQuery.CountAsyncLinqToDB(p => p.Favorite);
        var championCount = await pokemonQuery.CountAsyncLinqToDB(p => p.Champion);
        var marketCount = await pokemonQuery.CountAsyncLinqToDB(p => p.MarketEnlist);

        return new Dictionary<string, int>
        {
            { "Total", total },
            { "Shiny", shinyCount },
            { "Radiant", radiantCount },
            { "Shadow", shadowCount },
            { "Legendary", legendaryCount },
            { "Favorite", favoriteCount },
            { "Champion", championCount },
            { "Market", marketCount }
        };
    }

    /// <summary>
    ///     Gets the total count of Pokemon owned by a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The total count of Pokemon owned by the user.</returns>
    public async Task<int> GetUserPokemonCount(ulong userId)
    {
        try
        {
            await using var db = await dbProvider.GetContextAsync();
            var conn = db.CreateLinqToDBConnection();

            return await conn.GetTable<UserPokemonOwnership>()
                .CountAsyncLinqToDB(o => o.UserId == userId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting Pokemon count for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    ///     Gets the types of a Pokemon.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokemon.</param>
    /// <returns>A list of the Pokemon's types.</returns>
    public async Task<List<string>> GetPokemonTypes(string pokemonName)
    {
        try
        {
            var pokemonInfo = await GetPokemonInfo(pokemonName);
            return pokemonInfo?.Types ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    ///     Gets the primary type of a Pokemon for sorting and filtering purposes.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokemon.</param>
    /// <returns>The primary type of the Pokemon, or "unknown" if not found.</returns>
    public async Task<string> GetPrimaryType(string pokemonName)
    {
        try
        {
            // Get the Pokemon form
            var formInfo = await mongo.Forms
                .Find(f => f.Identifier.Equals(pokemonName.ToLower(), StringComparison.OrdinalIgnoreCase))
                .FirstOrDefaultAsync();

            if (formInfo == null)
                return "unknown";

            // Get the Pokemon's types
            var pokemonTypes = await mongo.PokemonTypes
                .Find(t => t.PokemonId == formInfo.PokemonId)
                .FirstOrDefaultAsync();

            if (pokemonTypes?.Types == null || pokemonTypes.Types.Count == 0)
                return "unknown";

            // Get the name of the primary type (first in the list)
            var primaryTypeId = pokemonTypes.Types[0];
            var primaryType = await mongo.Types
                .Find(t => t.TypeId == primaryTypeId)
                .FirstOrDefaultAsync();

            return primaryType?.Identifier ?? "unknown";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting primary type for {PokemonName}", pokemonName);
            return "unknown";
        }
    }

    /// <summary>
    ///     Calculates the HP stat for a Pokemon.
    /// </summary>
    /// <param name="baseStat">The base HP stat.</param>
    /// <param name="level">The level of the Pokemon.</param>
    /// <param name="iv">The HP IV.</param>
    /// <param name="ev">The HP EV.</param>
    /// <returns>The calculated HP stat.</returns>
    private static int CalculateHpStat(int baseStat, int level, int iv, int ev)
    {
        return (2 * baseStat + iv + ev / 4) * level / 100 + level + 10;
    }

    /// <summary>
    ///     Calculates a non-HP stat for a Pokemon.
    /// </summary>
    /// <param name="baseStat">The base stat value.</param>
    /// <param name="level">The level of the Pokemon.</param>
    /// <param name="iv">The IV for the stat.</param>
    /// <param name="ev">The EV for the stat.</param>
    /// <param name="natureMultiplier">The nature multiplier for the stat.</param>
    /// <returns>The calculated stat value.</returns>
    private static int CalculateStat(int baseStat, int level, int iv, int ev, double natureMultiplier)
    {
        return (int)(((2 * baseStat + iv + ev / 4) * level / 100 + 5) * natureMultiplier);
    }

    /// <summary>
    ///     Gets the stat modifiers for a Pokemon's nature.
    /// </summary>
    /// <param name="nature">The nature of the Pokemon.</param>
    /// <returns>A tuple containing the increased stat and decreased stat.</returns>
    private static (string, string) GetNatureModifier(string nature)
    {
        return nature?.ToLower() switch
        {
            "hardy" => ("None", "None"),
            "lonely" => ("Attack", "Defense"),
            "brave" => ("Attack", "Speed"),
            "adamant" => ("Attack", "SpecialAttack"),
            "naughty" => ("Attack", "SpecialDefense"),
            "bold" => ("Defense", "Attack"),
            "docile" => ("None", "None"),
            "relaxed" => ("Defense", "Speed"),
            "impish" => ("Defense", "SpecialAttack"),
            "lax" => ("Defense", "SpecialDefense"),
            "timid" => ("Speed", "Attack"),
            "hasty" => ("Speed", "Defense"),
            "serious" => ("None", "None"),
            "jolly" => ("Speed", "SpecialAttack"),
            "naive" => ("Speed", "SpecialDefense"),
            "modest" => ("SpecialAttack", "Attack"),
            "mild" => ("SpecialAttack", "Defense"),
            "quiet" => ("SpecialAttack", "Speed"),
            "bashful" => ("None", "None"),
            "rash" => ("SpecialAttack", "SpecialDefense"),
            "calm" => ("SpecialDefense", "Attack"),
            "gentle" => ("SpecialDefense", "Defense"),
            "sassy" => ("SpecialDefense", "Speed"),
            "careful" => ("SpecialDefense", "SpecialAttack"),
            "quirky" => ("None", "None"),
            _ => ("None", "None") // Default for unknown natures
        };
    }

    /// <summary>
    ///     Calculates the friendship level for a Pokemon.
    /// </summary>
    /// <param name="pokemon">The Pokemon to calculate friendship for.</param>
    /// <returns>The calculated friendship value.</returns>
    public int CalculateFriendship(Database.Models.PostgreSQL.Pokemon.Pokemon? pokemon)
    {
        // Base friendship starts at 70
        var friendship = 70;

        // Add friendship based on level
        friendship += pokemon.Level / 2;

        // Add friendship for high IVs (above 25)
        if (pokemon.HpIv > 25) friendship += 5;
        if (pokemon.AttackIv > 25) friendship += 5;
        if (pokemon.DefenseIv > 25) friendship += 5;
        if (pokemon.SpecialAttackIv > 25) friendship += 5;
        if (pokemon.SpecialDefenseIv > 25) friendship += 5;
        if (pokemon.SpeedIv > 25) friendship += 5;

        // Cap at 255
        return Math.Min(friendship, 255);
    }

    /// <summary>
    ///     Checks if a Pokemon name represents a form.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokemon.</param>
    /// <returns>True if the name represents a form, false otherwise.</returns>
    private bool IsFormed(string pokemonName)
    {
        return pokemonName.EndsWith("-mega") || pokemonName.EndsWith("-x") || pokemonName.EndsWith("-y") ||
               pokemonName.EndsWith("-origin") || pokemonName.EndsWith("-10") || pokemonName.EndsWith("-complete") ||
               pokemonName.EndsWith("-ultra") || pokemonName.EndsWith("-crowned") ||
               pokemonName.EndsWith("-eternamax") ||
               pokemonName.EndsWith("-blade");
    }

    /// <summary>
    ///     Converts an object to a dictionary of property names and values.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <returns>A dictionary representation of the object.</returns>
    public Dictionary<string, object> ToDictionary(object obj)
    {
        var dictionary = new Dictionary<string, object>();
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
            dictionary[property.Name] = property.GetValue(obj);

        return dictionary;
    }

    /// <summary>
    ///     Attempts to evolve a Pokemon based on various evolution requirements.
    /// </summary>
    /// <param name="pokemonId">The ID of the Pokemon to evolve.</param>
    /// <param name="activeItem">The active item being used, if any.</param>
    /// <param name="overrideLvl100">Whether to override the level 100 evolution restriction.</param>
    /// <param name="channel">The message channel to send evolution notifications to, if any.</param>
    /// <returns>A tuple containing success status, new species ID if successful, and whether an active item was used.</returns>
    public async Task<(bool Success, int? NewSpeciesId, bool UsedActiveItem)> TryEvolve(
        ulong pokemonId,
        string activeItem = null,
        bool overrideLvl100 = false,
        IMessageChannel channel = null)
    {
        await using var db = await dbProvider.GetContextAsync();
        var pokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == pokemonId);
        if (pokemon == null) return (false, null, false);

        var originalName = pokemon.PokemonName;
        var pokemonName = pokemon.PokemonName.ToLower();

        // Everstones block evolutions
        if (pokemon.HeldItem?.ToLower() is "everstone" or "eviolite")
            return (false, null, false);

        // Eggs cannot evolve
        if (pokemonName == "egg")
            return (false, null, false);

        // Don't evolve forms
        if (IsFormed(pokemonName) || pokemonName.EndsWith("-staff") || pokemonName.EndsWith("-custom"))
            return (false, null, false);

        // Get necessary info
        var pokemonInfo = await mongo.Forms.Find(f => f.Identifier == pokemonName).FirstOrDefaultAsync();
        if (pokemonInfo == null)
        {
            Log.Error("A poke exists that is not in the mongo forms table - {Name}", pokemonName);
            return (false, null, false);
        }

        var rawPfile = await mongo.PFile.Find(f => f.Identifier == pokemonInfo.Identifier).FirstOrDefaultAsync();
        if (rawPfile == null)
        {
            Log.Error("A non-formed poke exists that is not in the mongo pfile table - {Name}", pokemonName);
            return (false, null, false);
        }

        // Get evolution line
        var evoline = await mongo.PFile
            .Find(f => f.EvolutionChainId == rawPfile.EvolutionChainId)
            .ToListAsync();

        evoline.Sort((a, b) => b.IsBaby.GetValueOrDefault().CompareTo(a.IsBaby.GetValueOrDefault()));

        // Filter potential evos
        var potentialEvos = new List<Dictionary<string, object>>();
        foreach (var evo in evoline)
        {
            if (evo.EvolvesFromSpeciesId != pokemonInfo.PokemonId)
                continue;

            var val = await mongo.Evolution.Find(e => e.EvolvedSpeciesId == evo.EvolvesFromSpeciesId)
                .FirstOrDefaultAsync();
            if (val == null)
                Log.Error("An evofile does not exist for a poke - {Name}", evo.Identifier);
            else if (val.EvolvedSpeciesId == 0)
                return (false, null, false);
            else
                potentialEvos.Add(ToDictionary(val));
        }

        if (!potentialEvos.Any())
            return (false, null, false);

        // Prep items
        int? activeItemId = null;
        if (activeItem != null)
        {
            var item = await mongo.Items.Find(i => i.Identifier == activeItem).FirstOrDefaultAsync();
            activeItemId = item?.ItemId;
            if (activeItemId == null)
                Log.Error("A poke is trying to use an active item that is not in the mongo table - {Item}", activeItem);
        }

        int? heldItemId = null;
        if (!string.IsNullOrEmpty(pokemon.HeldItem))
        {
            var item = await mongo.Items.Find(i => i.Identifier == pokemon.HeldItem.ToLower()).FirstOrDefaultAsync();
            heldItemId = item?.ItemId;
        }

        // Get owner's region
        var owner = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == pokemon.Owner);
        if (owner?.Region == null)
            return (false, null, false);

        // Filter valid evos
        var validEvos = new List<Dictionary<string, object>>();
        foreach (var evoReq in potentialEvos)
            if (await CheckEvoReqs(pokemon, heldItemId, activeItemId, owner.Region, evoReq, overrideLvl100))
                validEvos.Add(evoReq);

        if (!validEvos.Any())
            return (false, null, false);

        var (evoId, evoReqs) = PickEvo(validEvos);
        var evoTwo = await mongo.PFile.Find(p => p.PokemonId == evoId).FirstOrDefaultAsync();
        if (evoTwo == null)
            return (false, null, false);

        var evoName = evoTwo.Identifier.Capitalize();

        // Update the Pokemon
        await db.UserPokemon.Where(p => p.Id == pokemonId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.PokemonName, evoName));

        if (channel != null)
            try
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Congratulations!!!")
                    .WithDescription($"Your {originalName} has evolved into {evoName}!")
                    .WithColor(Color.Blue);

                await channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send evolution message");
            }

        return (true, evoId, evoReqs.UsedActiveItem());
    }

    /// <summary>
    ///     Devolves a Pokemon to its previous evolution stage.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the Pokemon.</param>
    /// <param name="pokemonId">The ID of the Pokemon to devolve.</param>
    /// <returns>True if the Pokemon was successfully devolved, false otherwise.</returns>
    public async Task<bool> Devolve(ulong userId, ulong pokemonId)
    {
        await using var db = await dbProvider.GetContextAsync();
        var pokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == pokemonId);
        if (pokemon == null || pokemon.Radiant.GetValueOrDefault())
            return false;

        var pokeData = await mongo.PFile.Find(p => p.Identifier == pokemon.PokemonName.ToLower())
            .FirstOrDefaultAsync();
        if (pokeData?.EvolvesFromSpeciesId == null)
            return false;

        var preEvo = await mongo.PFile.Find(p => p.PokemonId == pokeData.EvolvesFromSpeciesId)
            .FirstOrDefaultAsync();
        if (preEvo == null)
            return false;

        var newName = preEvo.Identifier.Capitalize();
        await db.UserPokemon.Where(p => p.Id == pokemonId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.PokemonName, newName));

        return true;
    }

    /// <summary>
    ///     Checks if a Pokemon meets the requirements for evolution.
    /// </summary>
    /// <param name="pokemon">The Pokemon to check.</param>
    /// <param name="heldItemId">The ID of the item the Pokemon is holding, if any.</param>
    /// <param name="activeItemId">The ID of the active item being used, if any.</param>
    /// <param name="region">The region the Pokemon is in.</param>
    /// <param name="evoReq">The evolution requirements to check against.</param>
    /// <param name="overrideLvl100">Whether to override the level 100 evolution restriction.</param>
    /// <returns>True if the Pokemon meets the evolution requirements, false otherwise.</returns>
    private async Task<bool> CheckEvoReqs(
        Database.Models.PostgreSQL.Pokemon.Pokemon pokemon,
        int? heldItemId,
        int? activeItemId,
        string region,
        Dictionary<string, object> evoReq,
        bool overrideLvl100)
    {
        var reqFlags = EvoReqs.FromRaw(evoReq);

        // They used an active item but this evo doesn't use an active item, don't use it.
        if (activeItemId.HasValue && !reqFlags.UsedActiveItem())
            return false;

        // If a pokemon is level 100, ONLY evolve via an override or active item.
        if (pokemon.Level >= 100 && !(overrideLvl100 || activeItemId.HasValue))
            return false;

        // Check trigger item
        if (evoReq.GetValueOrDefault("trigger_item_id") is int triggerItemId)
            if (triggerItemId != activeItemId)
                return false;

        // Check held item
        if (evoReq.GetValueOrDefault("held_item_id") is int requiredHeldItemId)
            if (requiredHeldItemId != heldItemId)
                return false;

        // Check gender
        if (evoReq.GetValueOrDefault("gender_id") is int genderId)
            switch (genderId)
            {
                case 1 when pokemon.Gender == "-m":
                case 2 when pokemon.Gender == "-f":
                    return false;
            }

        // Check minimum level
        if (evoReq.GetValueOrDefault("minimum_level") is int minLevel)
            if (pokemon.Level < minLevel)
                return false;

        // Check known move
        if (evoReq.GetValueOrDefault("known_move_id") is int moveId)
        {
            var move = await mongo.Moves.Find(m => m.MoveId == moveId).FirstOrDefaultAsync();
            if (move == null || !pokemon.Moves.Contains(move.Identifier))
                return false;
        }

        // Check happiness
        if (evoReq.GetValueOrDefault("minimum_happiness") is int minHappiness)
            if (pokemon.Happiness < minHappiness)
                return false;

        // Check relative physical stats
        if (evoReq.GetValueOrDefault("relative_physical_stats") is int relativeStats)
        {
            // WARNING: Currently only used by Tyrogue which has identical base stats for atk and def
            // If used on other Pokemon, base stats need to be considered
            var attack = pokemon.AttackIv + pokemon.AttackEv;
            var defense = pokemon.DefenseIv + pokemon.DefenseEv;

            switch (relativeStats)
            {
                case 1 when !(attack > defense):
                case -1 when !(attack < defense):
                case 0 when attack != defense:
                    return false;
            }
        }

        // Check region
        if (evoReq.GetValueOrDefault("region") is string requiredRegion)
        {
            if (requiredRegion != region)
                return false;
            // Temp blocker since previously radiants could never evolve to regional forms
            if (pokemon.Radiant.GetValueOrDefault())
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Gets the ability name for a Pokemon based on its ability index.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokemon.</param>
    /// <param name="abilityIndex">The ability index of the Pokemon.</param>
    /// <returns>The name of the ability.</returns>
    public async Task<string> GetAbilityName(string pokemonName, int abilityIndex)
    {
        try
        {
            // Get the Pokemon form
            var form = await mongo.Forms
                .Find(f => f.Identifier.Equals(pokemonName.ToLower(), StringComparison.OrdinalIgnoreCase))
                .FirstOrDefaultAsync();

            if (form == null)
                return "Unknown";

            // Get abilities for this Pokemon
            var abilities = await mongo.PokeAbilities
                .Find(a => a.PokemonId == form.PokemonId)
                .ToListAsync();

            // Find the ability ID based on the index
            // Index 0 = primary ability, 1 = secondary ability, 2 = hidden ability
            int? abilityId = null;
            if (abilities.Count > abilityIndex && abilityIndex >= 0) abilityId = abilities[abilityIndex].AbilityId;

            if (!abilityId.HasValue)
                return "Unknown";

            // Get the ability name
            var ability = await mongo.Abilities
                .Find(a => a.AbilityId == abilityId.Value)
                .FirstOrDefaultAsync();

            return ability?.Identifier.Titleize() ?? "Unknown";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting ability name for {PokemonName} with index {AbilityIndex}", pokemonName,
                abilityIndex);
            return "Unknown";
        }
    }

    /// <summary>
    ///     Gets the trainer name for a user ID.
    /// </summary>
    /// <param name="userId">The ID of the user/trainer.</param>
    /// <returns>The name of the trainer.</returns>
    public async Task<string> GetTrainerName(ulong userId)
    {
        try
        {
            await using var dbContext = await dbProvider.GetContextAsync();
            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsyncEF(u => u.UserId == userId);

            if (user == null)
                return $"Unknown Trainer ({userId})";

            // Return trainer nickname if set, otherwise just return "Trainer" with ID
            return !string.IsNullOrEmpty(user.TrainerNickname)
                ? user.TrainerNickname
                : $"Trainer #{userId}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting trainer name for user {UserId}", userId);
            return $"Unknown Trainer ({userId})";
        }
    }

    /// <summary>
    ///     Picks the best evolution from a list of valid evolution options.
    /// </summary>
    /// <param name="validEvos">The list of valid evolution options.</param>
    /// <returns>A tuple containing the ID of the best evolution and its requirements.</returns>
    private (int Id, EvoReqs Reqs) PickEvo(List<Dictionary<string, object>> validEvos)
    {
        var bestScore = -1.0;
        var bestId = -1;
        EvoReqs bestReqs = null;

        foreach (var evo in validEvos)
        {
            var reqs = EvoReqs.FromRaw(evo);
            if (reqs > bestScore)
            {
                bestScore = reqs.Score;
                bestId = (int)evo["evolved_species_id"];
                bestReqs = reqs;
            }
        }

        return (bestId, bestReqs);
    }
}

/// <summary>
///     Represents a Pokemon in a list view.
/// </summary>
/// <param name="BotId">The bot ID of the Pokemon.</param>
/// <param name="Name">The name of the Pokemon.</param>
/// <param name="Number">The number of the Pokemon in the user's collection.</param>
/// <param name="Level">The level of the Pokemon.</param>
/// <param name="IvPercent">The IV percentage of the Pokemon.</param>
/// <param name="Shiny">Whether the Pokemon is shiny.</param>
/// <param name="Radiant">Whether the Pokemon is radiant.</param>
/// <param name="Skin">The skin of the Pokemon, if any.</param>
/// <param name="Gender">The gender of the Pokemon.</param>
/// <param name="Nickname">The nickname of the Pokemon, if any.</param>
public record PokemonListEntry(
    ulong BotId,
    string Name,
    int Number,
    int Level,
    double IvPercent,
    bool Shiny,
    bool Radiant,
    string Skin,
    string Gender,
    string Nickname,
    bool Favorite,
    bool Champion,
    bool MarketEnlist,
    string HeldItem,
    string[] Moves,
    string[] Tags,
    bool Tradable,
    bool Breedable,
    DateTime? Timestamp
);

/// <summary>
///     Specifies the sort order for Pokemon lists.
/// </summary>
public enum SortOrder
{
    /// <summary>Sort by IV percentage.</summary>
    Iv,

    /// <summary>Sort by level.</summary>
    Level,

    /// <summary>Sort by EV total.</summary>
    Ev,

    /// <summary>Sort by name.</summary>
    Name,

    /// <summary>Sort by recent acquisition (timestamp).</summary>
    Recent,

    /// <summary>Sort by primary type.</summary>
    Type,

    /// <summary>Sort by favorite status first.</summary>
    Favorite,

    /// <summary>Sort by party membership first.</summary>
    Party,

    /// <summary>Sort by champion status first.</summary>
    Champion,

    /// <summary>Default sort order (by acquisition).</summary>
    Default
}

/// <summary>
///     Contains information about a Pokemon's stats, types, and abilities.
/// </summary>
public class PokemonInfo
{
    /// <summary>Gets or sets the ID of the Pokemon.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the name of the Pokemon.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the types of the Pokemon.</summary>
    public List<string> Types { get; set; }

    /// <summary>Gets or sets the abilities of the Pokemon.</summary>
    public List<string> Abilities { get; set; }

    /// <summary>Gets or sets the egg groups of the Pokemon.</summary>
    public List<string> EggGroups { get; set; }

    /// <summary>Gets or sets the form identifier of the Pokemon.</summary>
    public string FormIdentifier { get; set; }

    /// <summary>Gets or sets the weight of the Pokemon in kg.</summary>
    public float? Weight { get; set; }

    /// <summary>Gets or sets the base stats of the Pokemon.</summary>
    public PokemonStats Stats { get; set; }
}

/// <summary>
///     Contains the base stat values for a Pokemon.
/// </summary>
public class PokemonStats
{
    /// <summary>Gets or sets the HP stat.</summary>
    public int Hp { get; set; }

    /// <summary>Gets or sets the Attack stat.</summary>
    public int Attack { get; set; }

    /// <summary>Gets or sets the Defense stat.</summary>
    public int Defense { get; set; }

    /// <summary>Gets or sets the Special Attack stat.</summary>
    public int SpecialAttack { get; set; }

    /// <summary>Gets or sets the Special Defense stat.</summary>
    public int SpecialDefense { get; set; }

    /// <summary>Gets or sets the Speed stat.</summary>
    public int Speed { get; set; }
}

/// <summary>
///     Contains the calculated stat values for a Pokemon.
/// </summary>
public class CalculatedStats
{
    /// <summary>Gets or sets the maximum HP.</summary>
    public int MaxHp { get; set; }

    /// <summary>Gets or sets the Attack stat.</summary>
    public int Attack { get; set; }

    /// <summary>Gets or sets the Defense stat.</summary>
    public int Defense { get; set; }

    /// <summary>Gets or sets the Special Attack stat.</summary>
    public int SpecialAttack { get; set; }

    /// <summary>Gets or sets the Special Defense stat.</summary>
    public int SpecialDefense { get; set; }

    /// <summary>Gets or sets the Speed stat.</summary>
    public int Speed { get; set; }
}