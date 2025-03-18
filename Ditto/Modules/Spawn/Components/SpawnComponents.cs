using Discord.Interactions;

namespace Ditto.Modules.Spawn.Components;

public class CatchModal : IModal
{
    public string Title => "Catch!";

    [InputLabel("Pokemon Name")]
    [ModalTextInput("pokemon_name", placeholder: "What do you think this pokemon is named?")]
    public string PokemonName { get; set; } = string.Empty;
}