using System.Text;
using Ditto.Database.DbContextStuff;
using Ditto.Database.Models.Mongo.Pokemon;
using Ditto.Services.Impl;
using LinqToDB.EntityFrameworkCore;
using MongoDB.Driver;

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

    private readonly string[] CUSTOM_POKES = new[]
    {
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
    };

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

    private bool IsFormed(string name)
    {
        // Add your logic to check if a name is a formed Pokemon
        return true; // Placeholder
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

        return (true, $"You have selected your {pokemon.Name}");
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
                    Builders<Form>.Filter.Regex(f => f.Identifier, new MongoDB.Bson.BsonRegularExpression($".*{val}.*", "i"))
                );

                forms = await cursor.Project(f => f.FormIdentifier)
                    .ToListAsync();

                // Filter out empty strings and specific region forms
                forms = forms.Where(f => !string.IsNullOrEmpty(f))
                    .Where(f => f is not "Galar" and not "Alola" and not "Hisui" and not "Paldea")
                    .ToList();

                if (!forms.Any())
                {
                    forms = ["None"];
                }
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
        var parsedForm = await ParseForm(pokemonName);
        var formInfo = await _mongo.Forms
            .Find(f => f.Identifier.Equals(parsedForm, StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefaultAsync();

        if (formInfo == null)
            return (null, null);

        string imageUrl;
        if (!string.IsNullOrEmpty(skin))
            // Handle other skin types if needed
            imageUrl =
                $"https://images.mewdeko.tech/skins/{skin}/{formInfo.PokemonId}-0-.{(skin.EndsWith("gif") ? "gif" : "png")}";
        else
            imageUrl = parsedForm switch
            {
                "tauros-blaze-paldeam" => "https://images.mewdeko.tech/images/128-2-.png",
                "tauros-aqua-paldeam" => "https://images.mewdeko.tech/images/128-3-.png",
                _ => $"https://images.mewdeko.tech/images/{formInfo.PokemonId}-0-.png"
            };

        return (formInfo, imageUrl);
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