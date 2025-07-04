namespace EeveeCore.Modules.Trade.Models;

/// <summary>
///     Contains information about a Pokemon that can be traded.
/// </summary>
public class TradeablePokemonInfo
{
    /// <summary>
    ///     Gets or sets the position of the Pokemon in the user's collection.
    /// </summary>
    public ulong Position { get; set; }

    /// <summary>
    ///     Gets or sets the species name of the Pokemon.
    /// </summary>
    public string PokemonName { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the user-given nickname of the Pokemon.
    /// </summary>
    public string Nickname { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the level of the Pokemon.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the Pokemon is shiny.
    /// </summary>
    public bool? Shiny { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the Pokemon is radiant.
    /// </summary>
    public bool? Radiant { get; set; }
}