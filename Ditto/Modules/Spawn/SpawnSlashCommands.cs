using Discord.Interactions;
using Ditto.Common.Attributes.Interactions;
using Ditto.Common.ModuleBases;
using Ditto.Modules.Spawn.Components;
using Ditto.Modules.Spawn.Services;
using Serilog;

namespace Ditto.Modules.Spawn;

[Group("spawn", "Spawn related commands")]
public class SpawnSlashCommands : DittoSlashModuleBase<SpawnService>
{
    public enum ChannelOption
    {
        Enable,
        Disable
    }

    public enum RedirectOption
    {
        Add,
        Remove
    }

    [SlashCommand("letsgo", "Toggle spawn storm mode")]
    [RequireContext(ContextType.Guild)]
    [RequireAdmin]
    public async Task ToggleSpawnStorm()
    {
        var isEnabled = Service.ToggleAlwaysSpawn();

        if (isEnabled)
            await RespondAsync("**Code**: *Spawn storm* `INITIATED!`");
        else
            await RespondAsync(
                $"It seems the **Spawn Storm** has ended. *You can blame {ctx.User.Id} for this crazyness*");
    }

    [SlashCommand("debug", "Debug spawn issues in the current server")]
    [RequireContext(ContextType.Guild)]
    [RequireAdmin]
    public async Task DebugSpawn()
    {
        await DeferAsync();

        try
        {
            var debugInfo = await Service.GetSpawnDebugInfo(ctx.Guild);
            await FollowupAsync(debugInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in debug spawn command");
            await FollowupAsync("An error occurred while debugging spawn settings. Please try again later.");
        }
    }

    [SlashCommand("settings", "View or modify spawn settings")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SpawnSettings(
        string? setting = null,
        string? value = null)
    {
        await DeferAsync();

        try
        {
            if (string.IsNullOrEmpty(setting))
            {
                // Display current settings
                var settings = await Service.GetGuildSettings(ctx.Guild.Id);
                await FollowupAsync(embed: settings);
                return;
            }

            // Handle setting changes
            var result = await Service.UpdateGuildSetting(ctx.Guild.Id, setting.ToLower(), value);
            await FollowupAsync(embed: result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in spawn settings command");
            await FollowupAsync("An error occurred while updating spawn settings. Please try again later.");
        }
    }

    [SlashCommand("channel", "Enable/disable spawns in a channel")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SpawnChannel(
        ChannelOption option = ChannelOption.Enable,
        ITextChannel? channel = null)
    {
        await DeferAsync();
        channel ??= ctx.Channel as ITextChannel;

        if (channel == null)
        {
            await FollowupAsync("This command can only be used in text channels.");
            return;
        }

        try
        {
            var result = await Service.UpdateChannelSetting(ctx.Guild.Id, channel.Id, option == ChannelOption.Enable);
            await FollowupAsync(embed: result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in spawn channel command");
            await FollowupAsync("An error occurred while updating channel settings. Please try again later.");
        }
    }

    [SlashCommand("redirect", "Add/remove spawn redirect channels")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SpawnRedirect(
        RedirectOption option = RedirectOption.Add,
        ITextChannel? channel = null)
    {
        await DeferAsync();
        channel ??= ctx.Channel as ITextChannel;

        if (channel == null)
        {
            await FollowupAsync("This command can only be used in text channels.");
            return;
        }

        try
        {
            var result = await Service.UpdateRedirectChannel(ctx.Guild.Id, channel.Id, option == RedirectOption.Add);
            await FollowupAsync(embed: result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in spawn redirect command");
            await FollowupAsync("An error occurred while updating redirect settings. Please try again later.");
        }
    }

    [SlashCommand("speed", "Set the spawn speed")]
    [RequireContext(ContextType.Guild)]
    [RequireAdmin]
    public async Task SpawnSpeed(
        [MinValue(1)] [MaxValue(20)] int speed = 10)
    {
        await DeferAsync();

        try
        {
            var result = await Service.UpdateSpawnSpeed(ctx.Guild.Id, speed);
            await FollowupAsync(embed: result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in spawn speed command");
            await FollowupAsync("An error occurred while updating spawn speed. Please try again later.");
        }
    }

    #region ComponentHandlers

    [ComponentInteraction("catch:*,*,*,*", true)]
    public async Task HandleCatchButton(string pokemonName, bool shiny, string legendChance, string ubChance)
    {
        var currentMessage = ctx.Interaction as IComponentInteraction;

        var message = await ctx.Channel.GetMessageAsync(currentMessage.Message.Id);
        if (message == null || message.Embeds.FirstOrDefault()?.Title == "Caught!")
        {
            await ctx.Interaction.SendEphemeralErrorAsync("This Pokemon has already been caught!");
            return;
        }

        var modal = new ModalBuilder()
            .WithTitle("Catch!")
            .WithCustomId($"catch_modal:{pokemonName},{shiny},{legendChance},{ubChance},{currentMessage.Message.Id}")
            .AddTextInput("Pokemon Name", "pokemon_name", placeholder: "What do you think this pokemon is named?")
            .Build();

        await ctx.Interaction.RespondWithModalAsync(modal);
    }

    [ModalInteraction("catch_modal:*,*,*,*,*", true)]
    public async Task HandleCatchModal(string pokemonName, bool shiny, string legendChance, string ubChance,
        ulong messageId, CatchModal modal)
    {
        await ctx.Interaction.DeferAsync();

        // Check if pokemon is still available
        if (await ctx.Channel.GetMessageAsync(messageId) is not IUserMessage message ||
            message.Embeds.FirstOrDefault()?.Title == "Caught!")
        {
            await ctx.Interaction.FollowupAsync("This Pokemon has already been caught!", ephemeral: true);
            return;
        }

        // Use the service's helper method for catch options instead of duplicating code
        var validNames = Service.GetCatchOptions(pokemonName);

        // Check guess
        if (!validNames.Contains(modal.PokemonName.ToLower().Replace(" ", "-")))
        {
            await ctx.Interaction.FollowupAsync("Incorrect name! Try again :(", ephemeral: true);
            return;
        }

        var legChance = int.Parse(legendChance);
        var ultraChance = int.Parse(ubChance);

        // Handle the catch through service
        var result = await Service.HandleCatch(
            ctx.User.Id,
            ctx.Guild.Id,
            pokemonName,
            shiny,
            legChance,
            ultraChance);

        // Send the catch message
        await ctx.Interaction.FollowupAsync(embed: result.ResponseEmbed);

        if (!result.Success) return;

        // Handle the original spawn message
        try
        {
            if (result.ShouldDeleteSpawn)
            {
                await message.DeleteAsync();
            }
            else
            {
                var embed = message.Embeds.First().ToEmbedBuilder();
                embed.Title = "Caught!";

                await message.ModifyAsync(m =>
                {
                    m.Embed = embed.Build();
                    m.Components = new ComponentBuilder().Build();
                });

                if (result.ShouldPinSpawn &&
                    ctx.Channel is ITextChannel &&
                    (await ctx.Guild.GetCurrentUserAsync()).GuildPermissions.ManageMessages)
                    await message.PinAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating spawn message {MessageId}", messageId);
        }
    }

    #endregion
}