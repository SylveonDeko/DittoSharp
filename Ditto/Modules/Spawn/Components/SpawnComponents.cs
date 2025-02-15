using Discord;
using Discord.Interactions;
using Ditto.Common.Constants;
using Ditto.Database.Models.Mongo.Discord;
using Ditto.Database.Models.PostgreSQL.Bot;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Ditto.Modules.Spawn.Components;

public class CatchModal : IModal
{
    public string Title => "Catch!";

    [InputLabel("Pokemon Name")]
    [ModalTextInput("pokemon_name", placeholder: "What do you think this pokemon is named?")]
    public string PokemonName { get; set; } = string.Empty;
}