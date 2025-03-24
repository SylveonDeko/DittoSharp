using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Start.Services;

namespace EeveeCore.Modules.Start;

public class StartModule : EeveeCoreSlashModuleBase<StartService>
{
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