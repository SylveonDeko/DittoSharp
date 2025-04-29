namespace EeveeCore.Common.Constants;

/// <summary>
///     Provides global constants and utility methods related to Pokémon.
/// </summary>
public static class PokemonConstants
{
    /// <summary>
    ///     Gets a comprehensive list of all notable Pokémon, including pseudo-legendaries,
    ///     starters, legendary Pokémon, and Ultra Beasts.
    /// </summary>
    public static readonly List<string?> TotalList = PseudoAndStarters.PseudoList
        .Concat(PseudoAndStarters.StarterList)
        .Concat(LegendaryPokemon.LegendList)
        .Concat(LegendaryPokemon.UltraBeasts)
        .ToList();
}