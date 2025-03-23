using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Start.Services;

namespace EeveeCore.Modules.Start.Components;

public class StartInteractionModule(StartService startService) : EeveeCoreSlashModuleBase<StartService>
{
    [ComponentInteraction("start:type:*")]
    public async Task HandleTypeSelection(string type)
    {
        try
        {
            List<SelectMenuOptionBuilder> options;
            switch (type)
            {
                case "grass":
                    options = startService.GetGrassStarterOptions();
                    break;
                case "water":
                    options = startService.GetWaterStarterOptions();
                    break;
                case "fire":
                    options = startService.GetFireStarterOptions();
                    break;
                default:
                    await ctx.Interaction.RespondAsync("Invalid type selection.", ephemeral: true);
                    return;
            }

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId($"start:starter:{type}")
                .WithPlaceholder($"Choose your {type} starter")
                .WithOptions(options);

            var components = new ComponentBuilder()
                .WithSelectMenu(selectMenu);

            await ctx.Interaction.RespondAsync($"Select your {type} starter:", components: components.Build(),
                ephemeral: true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [ComponentInteraction("start:starter:*")]
    public async Task HandleStarterSelection(string type, string?[] values)
    {
        await DeferAsync();
        if (values.Length == 0)
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync("No starter selected.");
            return;
        }

        string? selectedStarter = values[0];

        // Register the user if they're new
        var registrationResult = await startService.RegisterNewUser(ctx.User.Id);
        if (!registrationResult.Success)
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync(registrationResult.Message);
            return;
        }

        // Create the starter Pokemon
        var starterResult = await startService.CreateStarterPokemon(ctx.User.Id, selectedStarter);
        if (!starterResult.Success)
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync(starterResult.Message);
            return;
        }

        // Send welcome message
        var welcomeEmbed = new EmbedBuilder()
            .WithTitle("Welcome to EeveeCore")
            .WithDescription("See your owned Pokemon using `/p`\nSelect your starter with `/select 1`\n" +
                             "Visit our **[Guide here](https://eeveecore.gitbook.io/eeveecore-guide/)** for detailed instructions!\n" +
                             "**Now begin your adventure!**\n" +
                             "**Pokemon Spawns are turned off by Default. If you are setting the bot up for use in a new server " +
                             "you must enable spawns for the channels in which you want the bot to spawn in.**\n" +
                             "**Use the `/settings spawns enable` command to enable in this channel. See `/settings` for other spawn subcommands.")
            .WithColor(new Color(0xDD, 0x00, 0xDD));

        await ctx.Interaction.RespondAsync(embed: welcomeEmbed.Build());

        // Send additional info
        var infoEmbed = new EmbedBuilder()
            .WithTitle("Thank you for registering!")
            .WithDescription("Don't hesitate to [Join the Official Server](https://discord.gg/eeveecore) for upcoming Events/Tournaments\n" +
                             "We love suggestions as well! Also, If you haven't, add EeveeCore to your server or recommend EeveeCore to your friends server and spread the fun!!")
            .WithColor(new Color(0xFFB6C1))
            .AddField("How to get Redeems and Credits",
                "Get 1 Redeem and 15,000 Credits for 5 Upvote Points!\nMake sure you check out fishing for a new user bonus! Fish up mystery boxes to get battle multiplier!")
            .AddField("The most unique Pokemon experience on discord!",
                "We are the Pokemon Bot with the most accurately working Status, Weather, Setup Moves, Secondary effects and Every Pokemon **Form** working in 6v6 player vs player duels!");

        await ctx.Interaction.FollowupAsync(embed: infoEmbed.Build(), ephemeral: true);

        if (await ctx.Client.GetChannelAsync(1353493473172521020) is ITextChannel loggingChannel)
        {
            await loggingChannel.SendMessageAsync($"{ctx.User.Username} (`{ctx.User.Id}`) has started **EeveeCore** using `/start` (**{selectedStarter}**)");
        }
    }
}