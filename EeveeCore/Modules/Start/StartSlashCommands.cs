using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Start.Services;

namespace EeveeCore.Modules.Start;

/// <summary>
///     Provides the initial slash command for starting a user's journey in EeveeCore.
///     Entry point for new users to begin their Pokémon adventure.
/// </summary>
public class StartModule : EeveeCoreSlashModuleBase<StartService>
{
    /// <summary>
    ///     Starts the user's journey in EeveeCore.
    ///     Displays button options for selecting a starter Pokémon type (Grass, Water, or Fire).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("start", "Start your journey in EeveeCore!")]
    public async Task StartEeveeCore()
    {
        var components = new ComponentBuilder()
            .WithButton("Grass Starter", "start:type:grass", ButtonStyle.Primary, new Emoji("🍃"))
            .WithButton("Water Starter", "start:type:water", ButtonStyle.Primary, new Emoji("💧"))
            .WithButton("Fire Starter", "start:type:fire", ButtonStyle.Primary, new Emoji("🔥"));

        await ctx.Interaction.RespondAsync("Select your starter type:", components: components.Build());
    }
}