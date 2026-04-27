using EeveeCore.Database.Models.Mongo.Pokemon;
using PType = EeveeCore.Database.Models.Mongo.Pokemon.Type;

namespace EeveeCore.Services.Impl;

/// <summary>
///     Provides in-memory access to static game data loaded from MongoDB at boot.
///     Eliminates per-request Mongo queries for read-only reference data.
/// </summary>
public interface IGameDataCache
{
    /// <summary>True once <see cref="Common.ModuleBehaviors.IReadyExecutor.OnReadyAsync" /> has populated all caches.</summary>
    bool IsLoaded { get; }

    /// <summary>Moves indexed by <c>MoveId</c>.</summary>
    IReadOnlyDictionary<int, Move> MovesById { get; }

    /// <summary>Moves indexed by <c>Identifier</c> (case-insensitive).</summary>
    IReadOnlyDictionary<string, Move> MovesByIdentifier { get; }

    /// <summary>Abilities indexed by <c>AbilityId</c>.</summary>
    IReadOnlyDictionary<int, Ability> AbilitiesById { get; }

    /// <summary>Abilities indexed by <c>Identifier</c> (case-insensitive).</summary>
    IReadOnlyDictionary<string, Ability> AbilitiesByIdentifier { get; }

    /// <summary>Forms indexed by <c>FormId</c>.</summary>
    IReadOnlyDictionary<int, Form> FormsById { get; }

    /// <summary>Forms indexed by <c>Identifier</c> (case-insensitive).</summary>
    IReadOnlyDictionary<string, Form> FormsByIdentifier { get; }

    /// <summary>Natures indexed by <c>NatureId</c>.</summary>
    IReadOnlyDictionary<int, Nature> NaturesById { get; }

    /// <summary>Natures indexed by <c>Identifier</c> (case-insensitive).</summary>
    IReadOnlyDictionary<string, Nature> NaturesByIdentifier { get; }

    /// <summary>Pokemon species (PFile) indexed by <c>PokemonId</c>.</summary>
    IReadOnlyDictionary<int, PokemonFile> PFileById { get; }

    /// <summary>Pokemon species (PFile) indexed by <c>Identifier</c> (case-insensitive).</summary>
    IReadOnlyDictionary<string, PokemonFile> PFileByIdentifier { get; }

    /// <summary>Pokemon species (PFile) grouped by <c>EvolvesFromSpeciesId</c> — used to find what a species evolves into.</summary>
    IReadOnlyDictionary<int, IReadOnlyList<PokemonFile>> PFileByEvolvesFromSpeciesId { get; }

    /// <summary>Pokemon species (PFile) grouped by <c>EvolutionChainId</c>.</summary>
    IReadOnlyDictionary<int, IReadOnlyList<PokemonFile>> PFileByEvolutionChainId { get; }

    /// <summary>Items indexed by <c>ItemId</c>.</summary>
    IReadOnlyDictionary<int, Item> ItemsById { get; }

    /// <summary>Items indexed by <c>Identifier</c> (case-insensitive).</summary>
    IReadOnlyDictionary<string, Item> ItemsByIdentifier { get; }

    /// <summary>Types indexed by <c>TypeId</c>.</summary>
    IReadOnlyDictionary<int, PType> TypesById { get; }

    /// <summary>Types indexed by <c>Identifier</c> (case-insensitive).</summary>
    IReadOnlyDictionary<string, PType> TypesByIdentifier { get; }

    /// <summary>Stat types indexed by <c>StatId</c>.</summary>
    IReadOnlyDictionary<int, StatType> StatTypesById { get; }

    /// <summary>All ability slots for a given Pokemon species (PokemonId), ordered by slot.</summary>
    IReadOnlyDictionary<int, IReadOnlyList<PokemonAbility>> PokeAbilitiesByPokemonId { get; }

    /// <summary>Type slot row for a given Pokemon species (PokemonId).</summary>
    IReadOnlyDictionary<int, PokemonTypes> PokemonTypesByPokemonId { get; }

    /// <summary>Base stats for a given Pokemon species (PokemonId).</summary>
    IReadOnlyDictionary<int, PokemonStats> PokemonStatsByPokemonId { get; }

    /// <summary>Move list for a given Pokemon (key is the lowercase identifier).</summary>
    IReadOnlyDictionary<string, PokemonMoves> PokemonMovesByPokemon { get; }

    /// <summary>Evolution rules grouped by <c>EvolvedSpeciesId</c>.</summary>
    IReadOnlyDictionary<int, IReadOnlyList<Evolution>> EvolutionsByEvolvedSpeciesId { get; }

    /// <summary>Egg-group memberships grouped by <c>SpeciesId</c>.</summary>
    IReadOnlyDictionary<int, IReadOnlyList<EggGroup>> EggGroupsBySpeciesId { get; }

    /// <summary>Egg-group info indexed by <c>GroupId</c>.</summary>
    IReadOnlyDictionary<int, EggGroupInfo> EggGroupInfosById { get; }

    /// <summary>All loaded type-effectiveness rows. Use <see cref="GetTypeEffectiveness" /> for indexed lookup.</summary>
    IReadOnlyList<TypeEffectiveness> TypeEffectiveness { get; }

    /// <summary>
    ///     Returns the damage factor (e.g. 0, 50, 100, 200) for an attack of <paramref name="damageTypeId" />
    ///     against a defender of <paramref name="targetTypeId" />, or <c>100</c> if no row matches.
    /// </summary>
    /// <param name="damageTypeId">The attacking move's type id.</param>
    /// <param name="targetTypeId">The defender's type id.</param>
    int GetTypeEffectiveness(int damageTypeId, int targetTypeId);
}
