using Discord.Interactions;

namespace EeveeCore.Modules.Spawn.Components;

public class CatchModal : IModal
{
    [InputLabel("Pokemon Name")]
    [ModalTextInput("pokemon_name", placeholder: "What do you think this pokemon is named?")]
    public string PokemonName { get; set; } = string.Empty;

    public string Title => "Catch!";
}