using Discord.Interactions;

namespace EeveeCore.Modules.Spawn.Components;

/// <summary>
///     Modal dialog used for catching Pokemon using the button interface.
///     Allows users to enter the name of the Pokemon they think is spawned.
/// </summary>
public class CatchModal : IModal
{
    /// <summary>
    ///     Input field for the Pokemon name.
    ///     User enters their guess for the Pokemon's name here.
    /// </summary>
    [InputLabel("Pokemon Name")]
    [ModalTextInput("pokemon_name", placeholder: "What do you think this pokemon is named?")]
    public string PokemonName { get; set; } = string.Empty;

    /// <summary>
    ///     The title displayed on the modal dialog.
    /// </summary>
    public string Title => "Catch!";
}