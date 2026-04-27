using System.Diagnostics;
using EeveeCore.Common.ModuleBehaviors;
using EeveeCore.Database.Models.Mongo.Pokemon;
using MongoDB.Driver;
using Serilog;
using PType = EeveeCore.Database.Models.Mongo.Pokemon.Type;

namespace EeveeCore.Services.Impl;

/// <inheritdoc cref="IGameDataCache" />
public class GameDataCache(IMongoService mongo) : INService, IReadyExecutor, IGameDataCache
{
    private static readonly StringComparer IdentifierComparer = StringComparer.OrdinalIgnoreCase;

    private Dictionary<long, int> _typeEffectivenessIndex = new();

    /// <inheritdoc />
    public bool IsLoaded { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, Move> MovesById { get; private set; } = new Dictionary<int, Move>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Move> MovesByIdentifier { get; private set; } =
        new Dictionary<string, Move>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, Ability> AbilitiesById { get; private set; } = new Dictionary<int, Ability>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Ability> AbilitiesByIdentifier { get; private set; } =
        new Dictionary<string, Ability>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, Form> FormsById { get; private set; } = new Dictionary<int, Form>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Form> FormsByIdentifier { get; private set; } =
        new Dictionary<string, Form>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, Nature> NaturesById { get; private set; } = new Dictionary<int, Nature>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Nature> NaturesByIdentifier { get; private set; } =
        new Dictionary<string, Nature>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, PokemonFile> PFileById { get; private set; } = new Dictionary<int, PokemonFile>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, PokemonFile> PFileByIdentifier { get; private set; } =
        new Dictionary<string, PokemonFile>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyList<PokemonFile>> PFileByEvolvesFromSpeciesId { get; private set; } =
        new Dictionary<int, IReadOnlyList<PokemonFile>>();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyList<PokemonFile>> PFileByEvolutionChainId { get; private set; } =
        new Dictionary<int, IReadOnlyList<PokemonFile>>();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, Item> ItemsById { get; private set; } = new Dictionary<int, Item>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Item> ItemsByIdentifier { get; private set; } =
        new Dictionary<string, Item>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, PType> TypesById { get; private set; } = new Dictionary<int, PType>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, PType> TypesByIdentifier { get; private set; } =
        new Dictionary<string, PType>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, StatType> StatTypesById { get; private set; } = new Dictionary<int, StatType>();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyList<PokemonAbility>> PokeAbilitiesByPokemonId { get; private set; } =
        new Dictionary<int, IReadOnlyList<PokemonAbility>>();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, PokemonTypes> PokemonTypesByPokemonId { get; private set; } =
        new Dictionary<int, PokemonTypes>();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, PokemonStats> PokemonStatsByPokemonId { get; private set; } =
        new Dictionary<int, PokemonStats>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, PokemonMoves> PokemonMovesByPokemon { get; private set; } =
        new Dictionary<string, PokemonMoves>(IdentifierComparer);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyList<Evolution>> EvolutionsByEvolvedSpeciesId { get; private set; } =
        new Dictionary<int, IReadOnlyList<Evolution>>();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, IReadOnlyList<EggGroup>> EggGroupsBySpeciesId { get; private set; } =
        new Dictionary<int, IReadOnlyList<EggGroup>>();

    /// <inheritdoc />
    public IReadOnlyDictionary<int, EggGroupInfo> EggGroupInfosById { get; private set; } =
        new Dictionary<int, EggGroupInfo>();

    /// <inheritdoc />
    public IReadOnlyList<TypeEffectiveness> TypeEffectiveness { get; private set; } = [];

    /// <inheritdoc />
    public int GetTypeEffectiveness(int damageTypeId, int targetTypeId)
    {
        var key = ((long)damageTypeId << 32) | (uint)targetTypeId;
        return _typeEffectivenessIndex.TryGetValue(key, out var factor) ? factor : 100;
    }

    /// <inheritdoc />
    public async Task OnReadyAsync()
    {
        var sw = Stopwatch.StartNew();
        Log.Information("Loading static game data into memory…");

        var movesTask = mongo.Moves.Find(FilterDefinition<Move>.Empty).ToListAsync();
        var abilitiesTask = mongo.Abilities.Find(FilterDefinition<Ability>.Empty).ToListAsync();
        var formsTask = mongo.Forms.Find(FilterDefinition<Form>.Empty).ToListAsync();
        var naturesTask = mongo.Natures.Find(FilterDefinition<Nature>.Empty).ToListAsync();
        var pfileTask = mongo.PFile.Find(FilterDefinition<PokemonFile>.Empty).ToListAsync();
        var itemsTask = mongo.Items.Find(FilterDefinition<Item>.Empty).ToListAsync();
        var typesTask = mongo.Types.Find(FilterDefinition<PType>.Empty).ToListAsync();
        var statTypesTask = mongo.StatTypes.Find(FilterDefinition<StatType>.Empty).ToListAsync();
        var pokeAbilitiesTask = mongo.PokeAbilities.Find(FilterDefinition<PokemonAbility>.Empty).ToListAsync();
        var pokemonTypesTask = mongo.PokemonTypes.Find(FilterDefinition<PokemonTypes>.Empty).ToListAsync();
        var pokemonStatsTask = mongo.PokemonStats.Find(FilterDefinition<PokemonStats>.Empty).ToListAsync();
        var pokemonMovesTask = mongo.PokemonMoves.Find(FilterDefinition<PokemonMoves>.Empty).ToListAsync();
        var evolutionsTask = mongo.Evolution.Find(FilterDefinition<Evolution>.Empty).ToListAsync();
        var eggGroupsTask = mongo.EggGroups.Find(FilterDefinition<EggGroup>.Empty).ToListAsync();
        var eggGroupInfoTask = mongo.EggGroupsInfo.Find(FilterDefinition<EggGroupInfo>.Empty).ToListAsync();
        var typeEffTask = mongo.TypeEffectiveness.Find(FilterDefinition<TypeEffectiveness>.Empty).ToListAsync();

        await Task.WhenAll(
            movesTask, abilitiesTask, formsTask, naturesTask, pfileTask, itemsTask, typesTask, statTypesTask,
            pokeAbilitiesTask, pokemonTypesTask, pokemonStatsTask, pokemonMovesTask, evolutionsTask,
            eggGroupsTask, eggGroupInfoTask, typeEffTask);

        var moves = movesTask.Result;
        MovesById = ToIntDict(moves, m => m.MoveId, "moves");
        MovesByIdentifier = ToIdentifierDict(moves, m => m.Identifier);

        var abilities = abilitiesTask.Result;
        AbilitiesById = ToIntDict(abilities, a => a.AbilityId, "abilities");
        AbilitiesByIdentifier = ToIdentifierDict(abilities, a => a.Identifier);

        var forms = formsTask.Result;
        FormsById = ToIntDict(forms, f => f.FormId, "forms");
        FormsByIdentifier = ToIdentifierDict(forms, f => f.Identifier);

        var natures = naturesTask.Result;
        NaturesById = ToIntDict(natures, n => n.NatureId, "natures");
        NaturesByIdentifier = ToIdentifierDict(natures, n => n.Identifier);

        var pfile = pfileTask.Result;
        PFileById = ToIntDict(pfile.Where(p => p.PokemonId.HasValue), p => p.PokemonId!.Value, "pfile");
        PFileByIdentifier = ToIdentifierDict(pfile, p => p.Identifier);
        PFileByEvolvesFromSpeciesId = pfile
            .Where(p => p.EvolvesFromSpeciesId.HasValue)
            .GroupBy(p => p.EvolvesFromSpeciesId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PokemonFile>)g.ToList());
        PFileByEvolutionChainId = pfile
            .Where(p => p.EvolutionChainId.HasValue)
            .GroupBy(p => p.EvolutionChainId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PokemonFile>)g.ToList());

        var items = itemsTask.Result;
        ItemsById = ToIntDict(items, i => i.ItemId, "items");
        ItemsByIdentifier = ToIdentifierDict(items, i => i.Identifier);

        var types = typesTask.Result;
        TypesById = ToIntDict(types, t => t.TypeId, "types");
        TypesByIdentifier = ToIdentifierDict(types, t => t.Identifier);

        StatTypesById = ToIntDict(statTypesTask.Result, s => s.StatId, "stat_types");

        PokeAbilitiesByPokemonId = pokeAbilitiesTask.Result
            .GroupBy(pa => pa.PokemonId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PokemonAbility>)g.OrderBy(pa => pa.Slot).ToList());

        PokemonTypesByPokemonId = pokemonTypesTask.Result
            .GroupBy(pt => pt.PokemonId)
            .ToDictionary(g => g.Key, g => g.First());

        PokemonStatsByPokemonId = pokemonStatsTask.Result
            .GroupBy(ps => ps.PokemonId)
            .ToDictionary(g => g.Key, g => g.First());

        PokemonMovesByPokemon = pokemonMovesTask.Result
            .Where(pm => !string.IsNullOrEmpty(pm.Pokemon))
            .GroupBy(pm => pm.Pokemon, IdentifierComparer)
            .ToDictionary(g => g.Key, g => g.First(), IdentifierComparer);

        EvolutionsByEvolvedSpeciesId = evolutionsTask.Result
            .GroupBy(e => e.EvolvedSpeciesId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Evolution>)g.ToList());

        EggGroupsBySpeciesId = eggGroupsTask.Result
            .GroupBy(eg => eg.SpeciesId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EggGroup>)g.ToList());

        EggGroupInfosById = ToIntDict(eggGroupInfoTask.Result, eg => eg.GroupId, "egg_groups_info");

        var typeEff = typeEffTask.Result;
        TypeEffectiveness = typeEff;
        var idx = new Dictionary<long, int>(typeEff.Count);
        foreach (var te in typeEff)
        {
            var key = ((long)te.DamageTypeId << 32) | (uint)te.TargetTypeId;
            idx[key] = te.DamageFactor;
        }

        _typeEffectivenessIndex = idx;

        IsLoaded = true;
        sw.Stop();
        Log.Information(
            "Static game data loaded in {ElapsedMs}ms — moves={Moves} abilities={Abilities} forms={Forms} pfile={PFile} items={Items} types={Types} eff={Eff}",
            sw.ElapsedMilliseconds, MovesById.Count, AbilitiesById.Count, FormsById.Count, PFileById.Count,
            ItemsById.Count, TypesById.Count, TypeEffectiveness.Count);
    }

    private static Dictionary<string, T> ToIdentifierDict<T>(IEnumerable<T> source, Func<T, string?> selector)
    {
        var dict = new Dictionary<string, T>(IdentifierComparer);
        foreach (var item in source)
        {
            var key = selector(item);
            if (string.IsNullOrEmpty(key)) continue;
            dict.TryAdd(key, item);
        }

        return dict;
    }

    private static Dictionary<int, T> ToIntDict<T>(IEnumerable<T> source, Func<T, int> selector, string label)
    {
        var dict = new Dictionary<int, T>();
        var dupes = 0;
        foreach (var item in source)
            if (!dict.TryAdd(selector(item), item))
                dupes++;
        if (dupes > 0)
            Log.Warning("{Dupes} duplicate id(s) skipped in {Label}", dupes, label);
        return dict;
    }
}
