using System.Reflection;
using System.Text;
using Ditto.Common.Logic;
using Ditto.Database.DbContextStuff;
using Ditto.Database.Models.Mongo.Pokemon;
using Ditto.Database.Models.PostgreSQL.Pokemon;
using Ditto.Modules.Spawn.Constants;
using Ditto.Services.Impl;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace Ditto.Modules.Pokemon.Services;

public class PokemonService : INService
{
    private readonly DiscordShardedClient _client;
    private readonly DbContextProvider _dbProvider;
    private readonly IMongoService _mongo;
    private readonly RedisCache _redis;

    private const string HP_DISPLAY = "`HP:`";
    private const string ATK_DISPLAY = "`ATK:`";
    private const string DEF_DISPLAY = "`DEF:`";
    private const string SPATK_DISPLAY = "`SPATK:`";
    private const string SPDEF_DISPLAY = "`SPDEF:`";
    private const string SPE_DISPLAY = "`SPEED:`";

    private readonly string[] CUSTOM_POKES =
    [
        "Onehitmonchan",
        "Xerneas-brad",
        "Lucariosouta",
        "Cubone-freki",
        "Glaceon-glaceon",
        "Scorbunny-sav",
        "Palkia-gompp",
        "Alacatzam",
        "Magearna-curtis",
        "Arceus-tatogod",
        "Enamorus-therian-forme",
        "Kubfu-rapid-strike",
        "Palkia-lord",
        "Dialga-lord",
        "Missingno"
    ];

    public PokemonService(
        DiscordShardedClient client,
        DbContextProvider dbProvider,
        IMongoService mongo,
        RedisCache redis)
    {
        _client = client;
        _dbProvider = dbProvider;
        _mongo = mongo;
        _redis = redis;
    }

    public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetUserPokemons(ulong userId)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();
        var pokeIds = dbContext.UserPokemon.Where(x => x.Owner == userId);

        if (!pokeIds.Any())
            return [];

        return await pokeIds.ToListAsyncEF();
    }

    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon?> GetPokemonById(int pokemonId)
    {
        try
        {
            await using var dbContext = await _dbProvider.GetContextAsync();
            return await dbContext.UserPokemon
                .FirstOrDefaultAsyncEF(p => p.Id == pokemonId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<string> GetKids(List<PokemonFile> rawEvos, int speciesId = -1, string prefix = null)
    {
        var result = "";
        foreach (var poke in rawEvos.Where(poke => poke.EvolvesFromSpeciesId == speciesId))
        {
            var reqs = "";
            if (speciesId != -1) reqs = await GetReqs(poke.PokemonId.Value);
            result += $"{prefix}├─{poke.Identifier} {reqs}\n";
            result += await GetKids(rawEvos, poke.PokemonId.Value, $"{prefix}│ ");
        }

        return result;
    }

    private async Task<string> GetReqs(int pokeId)
    {
        var reqs = new List<string>();

        var evoReq = await _mongo.Evolution
            .Find(e => e.EvolvedSpeciesId == pokeId)
            .FirstOrDefaultAsync();

        if (evoReq == null) return "";

        if (evoReq.TriggerItemId.HasValue)
        {
            var item = await _mongo.Items
                .Find(i => i.ItemId == evoReq.TriggerItemId)
                .FirstOrDefaultAsync();
            if (item != null)
                reqs.Add($"apply `{item.Identifier}`");
        }

        if (evoReq.HeldItemId.HasValue)
        {
            var item = await _mongo.Items
                .Find(i => i.ItemId == evoReq.HeldItemId)
                .FirstOrDefaultAsync();
            if (item != null)
                reqs.Add($"hold `{item.Identifier}`");
        }

        reqs.Add($"is `{(evoReq.GenderId == 1 ? "female" : "male")}`");

        if (evoReq.MinimumLevel.HasValue) reqs.Add($"lvl `{evoReq.MinimumLevel}`");

        if (evoReq.KnownMoveId.HasValue)
        {
            var move = await _mongo.Moves
                .Find(m => m.MoveId == evoReq.KnownMoveId)
                .FirstOrDefaultAsync();
            if (move != null)
                reqs.Add($"knows `{move.Identifier}`");
        }

        if (evoReq.MinimumHappiness.HasValue) reqs.Add($"happiness `{evoReq.MinimumHappiness}`");

        if (evoReq.RelativePhysicalStats.HasValue)
            reqs.Add(evoReq.RelativePhysicalStats switch
            {
                0 => "atk = def",
                1 => "atk > def",
                -1 => "atk < def",
                _ => ""
            });

        if (!string.IsNullOrEmpty(evoReq.Region)) reqs.Add($"region `{evoReq.Region}`");

        return reqs.Any() ? $"({string.Join(", ", reqs)})" : "";
    }

    public async Task<string> GetEvolutionLine(string pokemonName)
    {
        try
        {
            // Get base name using the same logic as Python
            var formParts = pokemonName.ToLower().Split('-');
            var formSuffix = formParts.Length > 1 ? formParts[^1] : "";
            var baseName = "";

            if (formSuffix is "blaze" or "aqua")
                baseName = "tauros-paldea";
            else if (formSuffix is "alola" or "galar" or "hisui" or "paldea") formSuffix = "";

            if (formSuffix == "zen-galar") baseName = "darmanitan-galar";

            if (string.IsNullOrEmpty(baseName))
                baseName = !string.IsNullOrEmpty(formSuffix)
                    ? pokemonName.ToLower().Replace(formSuffix, "").TrimEnd('-')
                    : pokemonName.ToLower();

            var pfile = await _mongo.PFile
                .Find(p => p.Identifier == baseName)
                .FirstOrDefaultAsync();

            if (pfile == null)
                return "";

            var rawEvos = await _mongo.PFile
                .Find(p => p.EvolutionChainId == pfile.EvolutionChainId)
                .ToListAsync();

            // Get evolution line starting from the first in the chain (no evolves_from)
            var evoLine = await GetKids(rawEvos, pfile.PokemonId.Value, "");
            return evoLine;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return "";
        }
    }


    public async Task<PokemonInfo> GetPokemonInfo(string identifier)
    {
        try
        {
            var formInfo = await _mongo.Forms
                .Find(f => f.Identifier.Equals(identifier, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefaultAsync();

            if (formInfo == null)
                return null;

            var pokemonTypes = await _mongo.PokemonTypes
                .Find(t => t.PokemonId == formInfo.PokemonId)
                .FirstOrDefaultAsync();

            if (pokemonTypes == null)
                return null;

            var typeNames = new List<string>();
            foreach (var typeId in pokemonTypes.Types)
            {
                var type = await _mongo.Types
                    .Find(t => t.TypeId == typeId)
                    .FirstOrDefaultAsync();
                typeNames.Add(type?.Identifier ?? "unknown");
            }

            var stats = await _mongo.PokemonStats
                .Find(s => s.PokemonId == formInfo.PokemonId)
                .FirstOrDefaultAsync();

            // Get abilities
            var abilities = new List<string>();
            var abilityCursor = _mongo.PokeAbilities.Find(a => a.PokemonId == formInfo.PokemonId);
            await abilityCursor.ForEachAsync(async abilityRef =>
            {
                var ability = await _mongo.Abilities.Find(a => a.AbilityId == abilityRef.AbilityId)
                    .FirstOrDefaultAsync();
                if (ability != null)
                    abilities.Add(ability.Identifier);
            });

            // Get egg groups
            var eggGroups = new List<string>();
            var eggGroupsCursor = _mongo.EggGroups.Find(e => e.SpeciesId == formInfo.PokemonId);
            await eggGroupsCursor.ForEachAsync(async eggGroupRef =>
            {
                var eggGroup = await _mongo.EggGroupsInfo.Find(e => e.Id == eggGroupRef.Id).FirstOrDefaultAsync();
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
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<string> ParseForm(string form)
    {
        form = form.ToLower();
        var formParts = form.Split("-");

        // Handle specific case for 'tauros'
        if (formParts[0] == "tauros" && formParts.Length > 2) return formParts[1] + "-" + formParts[^1];

        // Check if the form starts with any special prefix
        var specialPrefixes = new[] { "tapu", "ho", "mr", "nidoran" };
        if (specialPrefixes.Any(prefix => form.StartsWith(prefix + "-"))) return form;

        // Direct match check
        var identifierMatch = await _mongo.Forms
            .Find(f => f.Identifier == form)
            .FirstOrDefaultAsync();
        if (identifierMatch != null) return form;

        // Check for specific formed identifiers
        if (formParts.Length > 1 && IsFormed(form))
        {
            var formMatch = await _mongo.Forms
                .Find(f => f.FormIdentifier == formParts[formParts.Length - 1])
                .FirstOrDefaultAsync();
            if (formMatch != null) return form;
        }

        // General rearrangement for other cases
        if (formParts.Length == 2 || (formParts.Length == 3 && formParts[0] != "tauros"))
        {
            (formParts[0], formParts[1]) = (formParts[1], formParts[0]);
            return string.Join("-", formParts);
        }

        return form;
    }

    public async Task<string> GetEvolutionChain(int pokemonId)
    {
        var sb = new StringBuilder();
        var evolutionData = await _mongo.Evolution.Find(e => e.EvolutionId == pokemonId).ToListAsync();

        foreach (var evo in evolutionData)
        {
            sb.AppendLine($"Evolves from: {evo.EvolvedSpeciesId}");
            if (!evo.TriggerItemId.HasValue) continue;
            var item = await _mongo.Items.Find(i => i.Id == evo.Id).FirstOrDefaultAsync();
            sb.AppendLine($"Using item: {item?.Identifier ?? "Unknown"}");
            // Add other evolution conditions
        }

        return sb.ToString();
    }

    public async Task<(bool Success, string Message)> SelectPokemon(ulong userId, int pokeNumber)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();

        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        if (user == null)
            return (false, "You have not started! Use /start first.");

        if (pokeNumber <= 0)
            return (false, "Invalid pokemon number.");

        var pokemon = await dbContext.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == user.Pokemon[pokeNumber - 1]);

        if (pokemon == null)
            return (false, "That pokemon does not exist!");

        user.Selected = pokemon.Id;
        await dbContext.SaveChangesAsync();

        return (true, $"You have selected your {pokemon.PokemonName}");
    }

    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon?> GetSelectedPokemon(ulong userId)
    {
        try
        {
            await using var dbContext = await _dbProvider.GetContextAsync();

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
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon?> GetNewestPokemon(ulong userId)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();

        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        if (user?.Pokemon == null || user.Pokemon.Length == 0)
            return null;

        var newestPokeId = user.Pokemon[^1];

        return await dbContext.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == newestPokeId);
    }

    // Add these methods to PokemonService.cs

    public async Task<List<PokemonFile>> GetAllPokemon()
    {
        return await _mongo.PFile
            .Find(x => x.PokemonId > -1)
            .ToListAsync();
    }

    public async Task<Database.Models.PostgreSQL.Pokemon.Pokemon> GetPokemonByNumber(ulong userId, int number)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();
        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        if (user?.Pokemon == null || number <= 0 || number > user.Pokemon.Length)
            return null;

        return await dbContext.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == user.Pokemon[number - 1]);
    }

    public async Task RemoveUserPokemon(ulong userId, int pokemonId, bool releasePokemon = false)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();
        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        if (user == null)
            throw new Exception("User not found");

        var pokemon = await dbContext.UserPokemon
            .FirstOrDefaultAsyncEF(p => p.Id == pokemonId);

        if (pokemon == null)
            throw new Exception("Pokemon not found");

        if (pokemon.Favorite)
            throw new Exception("Cannot remove favorited Pokemon");

        // Remove from user's pokemon list
        var pokemonList = user.Pokemon.ToList();
        pokemonList.Remove(pokemonId);
        user.Pokemon = pokemonList.ToArray();

        // If this was the selected pokemon, unselect it
        if (user.Selected == pokemonId)
            user.Selected = 0;

        if (releasePokemon)
            // Actually delete the pokemon for release
            dbContext.UserPokemon.Remove(pokemon);
        else
            // For sacrifice, just unlink it
            pokemon.Owner = 0;

        await dbContext.SaveChangesAsync();
    }

    public async Task<int> GetUserSoulGauge(ulong userId)
    {
        var value = await _redis.Redis.GetDatabase().StringGetAsync($"soul_gauge:{userId}");
        return value.HasValue ? (int)value : 0;
    }

    public async Task IncrementSoulGauge(ulong userId, double increment)
    {
        var key = $"soul_gauge:{userId}";
        await _redis.Redis.GetDatabase().StringIncrementAsync(key, increment);

        // Cap at 1000
        var current = await _redis.Redis.GetDatabase().StringGetAsync(key);
        if (current.HasValue && (double)current > 1000)
            await _redis.Redis.GetDatabase().StringSetAsync(key, 1000);
    }

    public async Task<string> GetEvolutionInfo(int pokemonId)
    {
        var sb = new StringBuilder();

        // Get evolution chain info
        var baseForm = await _mongo.Forms
            .Find(f => f.PokemonId == pokemonId)
            .FirstOrDefaultAsync();

        if (baseForm == null)
            return "No evolution information available";

        var evolvedForms = await _mongo.Forms
            .Find(f => f.BaseId == baseForm.FormId)
            .ToListAsync();

        foreach (var evolution in evolvedForms)
        {
            sb.AppendLine($"├─{evolution.Identifier}");

            // Get evolution requirements
            var requirements = await _mongo.Evolution
                .Find(e => e.EvolutionId == evolution.PokemonId)
                .FirstOrDefaultAsync();

            if (requirements != null)
            {
                var reqList = new List<string>();

                if (requirements.TriggerItemId.HasValue)
                {
                    var item = await _mongo.Items
                        .Find(i => i.ItemId == requirements.TriggerItemId.Value)
                        .FirstOrDefaultAsync();
                    if (item != null)
                        reqList.Add($"use {item.Identifier}");
                }

                if (requirements.MinimumLevel.HasValue)
                    reqList.Add($"level {requirements.MinimumLevel.Value}");

                if (reqList.Any())
                    sb.AppendLine($"│ ({string.Join(", ", reqList)})");
            }
        }

        return sb.ToString();
    }

    public async Task<string> GetPokemonForms(string val)
    {
        try
        {
            var forms = new List<string>();

            // Handle special cases first
            var lowerVal = val.ToLower();
            if (lowerVal is "spewpa" or "scatterbug" or "mew")
            {
                forms = ["None"];
            }
            else if (lowerVal is "tauros-paldea")
            {
                forms = ["aqua-paldea", "blaze-paldea"];
            }
            else
            {
                // Get forms from MongoDB
                var cursor = _mongo.Forms.Find(
                    Builders<Form>.Filter.Regex(f => f.Identifier, new BsonRegularExpression($".*{val}.*", "i"))
                );

                forms = await cursor.Project(f => f.FormIdentifier)
                    .ToListAsync();

                // Filter out empty strings and specific region forms
                forms = forms.Where(f => !string.IsNullOrEmpty(f))
                    .Where(f => f is not "Galar" and not "Alola" and not "Hisui" and not "Paldea")
                    .ToList();

                if (!forms.Any()) forms = ["None"];
            }

            return string.Join("\n", forms.Select(f => f.Capitalize()));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return "None";
        }
    }

    public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetSpecialPokemon(ulong userId, string variant)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();
        var userPokemon = await GetUserPokemons(userId);

        return variant switch
        {
            "shiny" => userPokemon.Where(p => p.Shiny == true).ToList(),
            "radiant" => userPokemon.Where(p => p.Radiant == true).ToList(),
            "shadow" => userPokemon.Where(p => p.Skin == "shadow").ToList(),
            "skin" => userPokemon.Where(p => !string.IsNullOrEmpty(p.Skin)).ToList(),
            _ => userPokemon
        };
    }

    public async Task<bool> CheckUserHasAncientUnlock(ulong userId)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();
        var user = await dbContext.Users
            .FirstOrDefaultAsyncEF(u => u.UserId == userId);

        return user?.AncientUnlocked ?? false;
    }

// Add these enum extensions for variant handling
    public static class PokemonVariantExtensions
    {
        public static string GetVariantIcon(string variant)
        {
            return variant switch
            {
                "shiny" => "<:starrr:1175872035927375953>",
                "radiant" => "<a:newradhmm:1061418796021194883>",
                "shadow" => "<:shadowicon4:1077328251556470925>",
                "skin" => "<:skin23:1012754684576014416>",
                _ => "✨"
            };
        }
    }

    public async Task<(Form Form, string ImageUrl)> GetPokemonFormInfo(string pokemonName, bool shiny = false,
        bool radiant = false, string skin = null)
    {
        var identifier = await _mongo.Forms
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

            var pokemonIdentifier = await _mongo.Forms
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
        var isPlaceholder = await _mongo.RadiantPlaceholders
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


    public async Task<DetailedPokemonInfo> GetDetailedPokemonInfo(string identifier)
    {
        var formInfo = await _mongo.Forms
            .Find(f => f.Identifier.Equals(identifier, StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefaultAsync();

        if (formInfo == null)
            return null;

        var pokemonTypes = await _mongo.PokemonTypes
            .Find(t => t.PokemonId == formInfo.PokemonId)
            .FirstOrDefaultAsync();

        if (pokemonTypes == null)
            return null;

        var types = new List<string>();
        foreach (var typeId in pokemonTypes.Types)
        {
            var type = await _mongo.Types
                .Find(t => t.TypeId == typeId)
                .FirstOrDefaultAsync();
            if (type != null)
                types.Add(type.Identifier);
        }

        var eggGroupIds = (await _mongo.EggGroups
            .Find(e => e.SpeciesId == formInfo.PokemonId)
            .FirstOrDefaultAsync())?.Groups ?? [15];

        var eggGroups = new List<string>();
        foreach (var eggGroupId in eggGroupIds)
        {
            var eggGroup = await _mongo.EggGroupsInfo
                .Find(e => e.GroupId == eggGroupId)
                .FirstOrDefaultAsync();
            if (eggGroup != null)
                eggGroups.Add(eggGroup.Identifier);
        }

        var abilities = new List<string>();
        var abilityCursor = _mongo.PokeAbilities.Find(a => a.PokemonId == formInfo.PokemonId);
        await abilityCursor.ForEachAsync(async abilityRef =>
        {
            if (abilityRef.AbilityId != null)
            {
                var ability = await _mongo.Abilities
                    .Find(a => a.AbilityId == abilityRef.AbilityId)
                    .FirstOrDefaultAsync();
                if (ability != null)
                    abilities.Add(ability.Identifier);
            }
        });

        var stats = await _mongo.PokemonStats
            .Find(s => s.PokemonId == formInfo.PokemonId)
            .FirstOrDefaultAsync();

        if (stats == null)
            return null;

        return new DetailedPokemonInfo
        {
            Name = formInfo.Identifier,
            FormIdentifier = formInfo.FormIdentifier,
            Weight = formInfo.Weight / 10.0f,
            Types = types,
            EggGroups = eggGroups,
            Abilities = abilities,
            Stats = new PokemonStats
            {
                Hp = stats.Stats[0],
                Attack = stats.Stats[1],
                Defense = stats.Stats[2],
                SpecialAttack = stats.Stats[3],
                SpecialDefense = stats.Stats[4],
                Speed = stats.Stats[5]
            }
        };
    }

    public Task<CalculatedStats> CalculatePokemonStats(Database.Models.PostgreSQL.Pokemon.Pokemon pokemon,
        PokemonStats baseStats)
    {
        var natureModifier = GetNatureModifier(pokemon.Nature);

        // Calculate HP differently from other stats
        var maxHp = CalculateHpStat(baseStats.Hp, pokemon.Level, pokemon.HpIv, pokemon.HpEv);

        return Task.FromResult(new CalculatedStats
        {
            MaxHp = maxHp,
            Attack = CalculateStat(baseStats.Attack, pokemon.Level, pokemon.AttackIv, pokemon.AttackEv,
                natureModifier switch
                {
                    1.1 => 1.1,
                    0.9 => 0.9,
                    _ => 1.0
                }),
            Defense = CalculateStat(baseStats.Defense, pokemon.Level, pokemon.DefenseIv, pokemon.DefenseEv,
                natureModifier switch
                {
                    1.1 => 1.1,
                    0.9 => 0.9,
                    _ => 1.0
                }),
            SpecialAttack = CalculateStat(baseStats.SpecialAttack, pokemon.Level, pokemon.SpecialAttackIv,
                pokemon.SpecialAttackEv,
                natureModifier switch
                {
                    1.1 => 1.1,
                    0.9 => 0.9,
                    _ => 1.0
                }),
            SpecialDefense = CalculateStat(baseStats.SpecialDefense, pokemon.Level, pokemon.SpecialDefenseIv,
                pokemon.SpecialDefenseEv,
                natureModifier switch
                {
                    1.1 => 1.1,
                    0.9 => 0.9,
                    _ => 1.0
                }),
            Speed = CalculateStat(baseStats.Speed, pokemon.Level, pokemon.SpeedIv, pokemon.SpeedEv,
                natureModifier switch
                {
                    1.1 => 1.1,
                    0.9 => 0.9,
                    _ => 1.0
                })
        });
    }

    public async Task<List<DeadPokemon>> GetDeadPokemon(ulong userId)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();

        // First get all dead Pokemon that match user's Pokemon array in one query
        return await dbContext.DeadPokemon
            .AsNoTracking()
            .Where(d => dbContext.Users
                .Where(u => u.UserId == (userId == 1081889316848017539 ? 946611594488602694UL : userId))
                .SelectMany(u => u.Pokemon)
                .Contains(d.Id))
            .ToListAsyncEF();
    }

    public async Task ResurrectPokemon(List<DeadPokemon> deadPokemon)
    {
        await using var dbContext = await _dbProvider.GetContextAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // Do deletion in a single operation
            var ids = deadPokemon.Select(d => d.Id).ToList();
            await dbContext.DeadPokemon
                .Where(d => ids.Contains(d.Id))
                .ExecuteDeleteAsync();

            // Convert and add all Pokemon in batches
            var livePokemon = deadPokemon.Select(MapDeadToLivePokemon).ToList();

            foreach (var batch in livePokemon.Chunk(1000))
            {
                await dbContext.UserPokemon.AddRangeAsync(batch);
            }

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

public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetPokemonByLevel(ulong userId, int minLevel, int maxLevel)
{
    await using var dbContext = await _dbProvider.GetContextAsync();
    return await dbContext.UserPokemon
        .AsNoTracking()  // Add this since we're only reading
        .Where(p => p.Owner == userId && p.Level >= minLevel && p.Level <= maxLevel)
        .OrderByDescending(p => p.Level)
        .ToListAsyncEF();
}

public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetPokemonByIv(ulong userId, double minIvPercent, double maxIvPercent)
{
    var minIvSum = (int)(minIvPercent * 186);
    var maxIvSum = (int)(maxIvPercent * 186);

    await using var dbContext = await _dbProvider.GetContextAsync();
    return await dbContext.UserPokemon
        .AsNoTracking()
        .Where(p => p.Owner == userId)
        .Select(p => new
        {
            Pokemon = p,
            IvSum = p.HpIv + p.AttackIv + p.DefenseIv + p.SpecialAttackIv + p.SpecialDefenseIv + p.SpeedIv
        })
        .Where(x => x.IvSum >= minIvSum && x.IvSum <= maxIvSum)
        .OrderByDescending(x => x.IvSum)
        .Select(x => x.Pokemon)
        .ToListAsyncEF();
}

public async Task<int> GetPokemonIndex(ulong userId, int pokemonId)
{
    await using var dbContext = await _dbProvider.GetContextAsync();
    var user = await dbContext.Users
        .FirstOrDefaultAsyncEF(u => u.UserId == userId);

    if (user?.Pokemon == null)
        return -1;

    // Find the index of this Pokemon in the user's array
    return Array.IndexOf(user.Pokemon, pokemonId) + 1; // +1 for 1-based indexing
}

public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetPokemonByType(ulong userId, string type)
{
    // Get type info from MongoDB - do this first to fail fast if type is invalid
    var typeInfo = await _mongo.Types
        .Find(t => t.Identifier.Equals(type.ToLower()))
        .FirstOrDefaultAsync();

    if (typeInfo == null)
        return [];

    // Get Pokemon IDs and forms in parallel
    var typeTask = _mongo.PokemonTypes
        .Find(pt => pt.Types.Contains(typeInfo.TypeId))
        .Project(p => p.PokemonId)
        .ToListAsync();

    var formsTask = _mongo.Forms
        .Find(Builders<Form>.Filter.Empty)
        .Project(f => new { f.PokemonId, f.Identifier })
        .ToListAsync();

    await Task.WhenAll(typeTask, formsTask);

    var pokemonIds = await typeTask;
    var forms = await formsTask;

    var pokemonNames = forms
        .Where(f => pokemonIds.Contains(f.PokemonId))
        .Select(f => f.Identifier.Capitalize())
        .ToHashSet(); // HashSet for more efficient lookup

    // Query PostgreSQL
    await using var dbContext = await _dbProvider.GetContextAsync();
    return await dbContext.UserPokemon
        .AsNoTracking()
        .Where(p => p.Owner == userId && pokemonNames.Contains(p.PokemonName))
        .ToListAsyncEF();
}

public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetPokemonByName(ulong userId, string name)
{
    await using var dbContext = await _dbProvider.GetContextAsync();
    return await dbContext.UserPokemon
        .AsNoTracking()
        .Where(p => p.Owner == userId &&
                   EF.Functions.ILike(p.PokemonName, $"%{name}%"))
        .ToListAsyncEF();
}

public async Task<List<Database.Models.PostgreSQL.Pokemon.Pokemon>> GetFilteredPokemon(ulong userId, string filter)
{
    await using var dbContext = await _dbProvider.GetContextAsync();
    var query = dbContext.UserPokemon
        .AsNoTracking()
        .Where(p => p.Owner == userId);

    query = filter.ToLower() switch
    {
        "legendary" => query.Where(p => PokemonList.LegendList.Contains(p.PokemonName)),
        "starter" => query.Where(p => PokemonList.starterList.Contains(p.PokemonName)),
        "shiny" => query.Where(p => p.Shiny == true),
        "radiant" => query.Where(p => p.Radiant == true),
        "skin" => query.Where(p => !string.IsNullOrEmpty(p.Skin)),
        _ => query
    };

    return await query.ToListAsyncEF();
}

   public async Task<List<PokemonListEntry>> GetPokemonList(ulong userId, SortOrder order = SortOrder.Default)
{
    try
    {
        await using var db = await _dbProvider.GetContextAsync();

        // Get user and their Pokemon array
        var user = await db.Users
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Pokemon)
            .FirstOrDefaultAsync();

        if (user == null || user.Length == 0)
            return [];

        // Get Pokemon data
        var pokemonData = await db.UserPokemon
            .AsNoTracking()
            .Where(x => user.Contains(x.Id))
            .Select(p => new
            {
                p.Id,
                p.DittoId,
                p.PokemonName,
                p.Level,
                IvTotal = p.AttackIv + p.DefenseIv + p.SpecialAttackIv +
                         p.SpecialDefenseIv + p.SpeedIv + p.HpIv,
                p.Shiny,
                p.Radiant,
                p.Skin,
                p.Gender,
                p.Nickname
            })
            .ToListAsync();

        // Sort if needed (in memory since we have all data)
        var sorted = order switch
        {
            SortOrder.Iv => pokemonData.OrderByDescending(p => p.IvTotal),
            SortOrder.Level => pokemonData.OrderByDescending(p => p.Level),
            SortOrder.Name => pokemonData.OrderByDescending(p => p.PokemonName),
            _ => pokemonData.AsEnumerable()
        };

        // Return with correct numbering based on array position
        return sorted
            .Select(p => new PokemonListEntry(
                p.DittoId,
                p.PokemonName,
                Array.IndexOf(user, p.Id) + 1,
                p.Level,
                p.IvTotal / 186.0,
                p.Shiny ?? false,
                p.Radiant ?? false,
                p.Skin,
                p.Gender,
                p.Nickname))
            .ToList();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
    private static int CalculateHpStat(int baseStat, int level, int iv, int ev)
    {
        return (2 * baseStat + iv + ev / 4) * level / 100 + level + 10;
    }

    private static int CalculateStat(int baseStat, int level, int iv, int ev, double natureMultiplier)
    {
        return (int)(((2 * baseStat + iv + ev / 4) * level / 100 + 5) * natureMultiplier);
    }

    private static double GetNatureModifier(string nature)
    {
        return nature?.ToLower() switch
        {
            "adamant" => 1.1, // +Atk, -SpA
            "bold" => 1.1, // +Def, -Atk
            "brave" => 1.1, // +Atk, -Spe
            "calm" => 1.1, // +SpD, -Atk
            "careful" => 1.1, // +SpD, -SpA
            "gentle" => 1.1, // +SpD, -Def
            "hasty" => 1.1, // +Spe, -Def
            "impish" => 1.1, // +Def, -SpA
            "jolly" => 1.1, // +Spe, -SpA
            "lax" => 1.1, // +Def, -SpD
            "lonely" => 1.1, // +Atk, -Def
            "mild" => 1.1, // +SpA, -Def
            "modest" => 1.1, // +SpA, -Atk
            "naive" => 1.1, // +Spe, -SpD
            "naughty" => 1.1, // +Atk, -SpD
            "quiet" => 1.1, // +SpA, -Spe
            "rash" => 1.1, // +SpA, -SpD
            "relaxed" => 1.1, // +Def, -Spe
            "sassy" => 1.1, // +SpD, -Spe
            "timid" => 1.1, // +Spe, -Atk
            _ => 1.0 // Neutral nature
        };
    }

    public int CalculateFriendship(Database.Models.PostgreSQL.Pokemon.Pokemon pokemon)
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
        {
            if (genderId == 1 && pokemon.Gender == "-m")
                return false;
            if (genderId == 2 && pokemon.Gender == "-f")
                return false;
        }

        // Check minimum level
        if (evoReq.GetValueOrDefault("minimum_level") is int minLevel)
            if (pokemon.Level < minLevel)
                return false;

        // Check known move
        if (evoReq.GetValueOrDefault("known_move_id") is int moveId)
        {
            var move = await _mongo.Moves.Find(m => m.MoveId == moveId).FirstOrDefaultAsync();
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

            if (relativeStats == 1 && !(attack > defense))
                return false;
            if (relativeStats == -1 && !(attack < defense))
                return false;
            if (relativeStats == 0 && attack != defense)
                return false;
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

    private bool IsFormed(string pokemonName)
    {
        return pokemonName.EndsWith("-mega") || pokemonName.EndsWith("-x") || pokemonName.EndsWith("-y") ||
               pokemonName.EndsWith("-origin") || pokemonName.EndsWith("-10") || pokemonName.EndsWith("-complete") ||
               pokemonName.EndsWith("-ultra") || pokemonName.EndsWith("-crowned") ||
               pokemonName.EndsWith("-eternamax") ||
               pokemonName.EndsWith("-blade");
    }

    public async Task<(bool Success, int? NewSpeciesId, bool UsedActiveItem)> TryEvolve(
        int pokemonId,
        string activeItem = null,
        bool overrideLvl100 = false,
        IMessageChannel channel = null)
    {
        await using var db = await _dbProvider.GetContextAsync();
        var pokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == pokemonId);
        if (pokemon == null) return (false, null, false);

        var originalName = pokemon.PokemonName;
        var pokemonName = pokemon.PokemonName.ToLower();

        // Everstones block evolutions
        if (pokemon.HeldItem.ToLower() is "everstone" or "eviolite")
            return (false, null, false);

        // Eggs cannot evolve
        if (pokemonName == "egg")
            return (false, null, false);

        // Don't evolve forms
        if (IsFormed(pokemonName) || pokemonName.EndsWith("-staff") || pokemonName.EndsWith("-custom"))
            return (false, null, false);

        // Get necessary info
        var pokemonInfo = await _mongo.Forms.Find(f => f.Identifier == pokemonName).FirstOrDefaultAsync();
        if (pokemonInfo == null)
        {
            Log.Error("A poke exists that is not in the mongo forms table - {Name}", pokemonName);
            return (false, null, false);
        }

        var rawPfile = await _mongo.PFile.Find(f => f.Identifier == pokemonInfo.Identifier).FirstOrDefaultAsync();
        if (rawPfile == null)
        {
            Log.Error("A non-formed poke exists that is not in the mongo pfile table - {Name}", pokemonName);
            return (false, null, false);
        }

        // Get evolution line
        var evoline = await _mongo.PFile
            .Find(f => f.EvolutionChainId == rawPfile.EvolutionChainId)
            .ToListAsync();

        evoline.Sort((a, b) => b.IsBaby.GetValueOrDefault().CompareTo(a.IsBaby.GetValueOrDefault()));

        // Filter potential evos
        var potentialEvos = new List<Dictionary<string, object>>();
        foreach (var evo in evoline)
        {
            if (evo.EvolvesFromSpeciesId != pokemonInfo.PokemonId)
                continue;

            var val = await _mongo.Evolution.Find(e => e.EvolvedSpeciesId == evo.EvolvesFromSpeciesId)
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
            var item = await _mongo.Items.Find(i => i.Identifier == activeItem).FirstOrDefaultAsync();
            activeItemId = item?.ItemId;
            if (activeItemId == null)
                Log.Error("A poke is trying to use an active item that is not in the mongo table - {Item}", activeItem);
        }

        int? heldItemId = null;
        if (!string.IsNullOrEmpty(pokemon.HeldItem))
        {
            var item = await _mongo.Items.Find(i => i.Identifier == pokemon.HeldItem.ToLower()).FirstOrDefaultAsync();
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
        var evoTwo = await _mongo.PFile.Find(p => p.EvolutionChainId == evoId).FirstOrDefaultAsync();
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
                    .WithColor(Color.Blue); // Replace with your random color logic

                await channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send evolution message");
            }

        return (true, evoId, evoReqs.UsedActiveItem());
    }

    public async Task<bool> Devolve(ulong userId, long pokemonId)
    {
        await using var db = await _dbProvider.GetContextAsync();
        var pokemon = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == pokemonId);
        if (pokemon == null || pokemon.Radiant.GetValueOrDefault())
            return false;

        var pokeData = await _mongo.PFile.Find(p => p.Identifier == pokemon.PokemonName.ToLower())
            .FirstOrDefaultAsync();
        if (pokeData?.EvolvesFromSpeciesId == null)
            return false;

        var preEvo = await _mongo.PFile.Find(p => p.EvolvesFromSpeciesId == pokeData.EvolvesFromSpeciesId)
            .FirstOrDefaultAsync();
        if (preEvo == null)
            return false;

        var newName = preEvo.Identifier.Capitalize();
        await db.UserPokemon.Where(p => p.Id == pokemonId)
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.PokemonName, newName));

        return true;
    }

    public Dictionary<string, object> ToDictionary(object obj)
    {
        var dictionary = new Dictionary<string, object>();
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties) dictionary[property.Name] = property.GetValue(obj);

        return dictionary;
    }
}

public record PokemonListEntry(
    int botId,
    string Name,
    int Number,
    int Level,
    double IvPercent,
    bool Shiny,
    bool Radiant,
    string Skin,
    string Gender,
    string Nickname
);

public enum SortOrder
{
    Iv,
    Level,
    Ev,
    Name,
    Default
}

public class DetailedPokemonInfo
{
    public string Name { get; set; }
    public string FormIdentifier { get; set; }
    public float? Weight { get; set; }
    public List<string> Types { get; set; }
    public List<string> EggGroups { get; set; }
    public List<string> Abilities { get; set; }
    public PokemonStats Stats { get; set; }
}

public class PokemonInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> Types { get; set; }
    public List<string> Abilities { get; set; }
    public List<string> EggGroups { get; set; }
    public string FormIdentifier { get; set; }
    public float? Weight { get; set; }
    public PokemonStats Stats { get; set; }
}

public class PokemonStats
{
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }
}

public class CalculatedStats
{
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }
}