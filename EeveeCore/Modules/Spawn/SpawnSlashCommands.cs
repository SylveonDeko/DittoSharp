using Discord.Interactions;
using EeveeCore.Common.Attributes.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Spawn.Components;
using EeveeCore.Modules.Spawn.Services;
using Serilog;

namespace EeveeCore.Modules.Spawn;

/// <summary>
///     Provides Discord slash commands for Pokémon spawn functionality.
///     Includes commands for configuring spawn settings, debugging, and handling catch interactions.
/// </summary>
[Group("spawn", "Spawn related commands")]
public class SpawnSlashCommands : EeveeCoreSlashModuleBase<SpawnService>
{
    /// <summary>
    ///     Represents the options for enabling or disabling spawns in a channel.
    /// </summary>
    public enum ChannelOption
    {
        /// <summary>Enable spawns in a channel</summary>
        Enable,

        /// <summary>Disable spawns in a channel</summary>
        Disable
    }

    /// <summary>
    ///     Represents the options for managing redirect channels.
    /// </summary>
    public enum RedirectOption
    {
        /// <summary>Add a channel to the redirect list</summary>
        Add,

        /// <summary>Remove a channel from the redirect list</summary>
        Remove
    }

    /// <summary>
    ///     Toggles spawn storm mode, which forces Pokémon to spawn regardless of chance.
    ///     Used for testing or special events.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Displays debug information about spawn settings in the current server.
    ///     Used for troubleshooting spawn issues.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Views or modifies spawn settings for the current guild.
    ///     Allows administrators to configure various spawn behavior options.
    /// </summary>
    /// <param name="setting">The setting to modify, or null to view current settings.</param>
    /// <param name="value">The new value for the setting, or null to view current value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Enables or disables Pokémon spawns in a specific channel.
    ///     Allows administrators to control which channels can have spawns.
    /// </summary>
    /// <param name="option">Whether to enable or disable spawns.</param>
    /// <param name="channel">The channel to configure, or null for the current channel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Adds or removes spawn redirect channels.
    ///     Redirect channels receive spawns that are triggered in other channels.
    /// </summary>
    /// <param name="option">Whether to add or remove the channel as a redirect.</param>
    /// <param name="channel">The channel to configure, or null for the current channel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Sets the spawn speed for the current guild.
    ///     Higher values increase the frequency of Pokémon spawns.
    /// </summary>
    /// <param name="speed">The spawn speed value (1-20).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Handles catch button interactions.
    ///     Opens a modal dialog for the user to enter the Pokémon's name.
    /// </summary>
    /// <param name="pokemonName">The name of the spawned Pokémon.</param>
    /// <param name="shiny">Whether the Pokémon is shiny.</param>
    /// <param name="legendChance">The legendary spawn chance value.</param>
    /// <param name="ubChance">The Ultra Beast spawn chance value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Handles catching Pokémon through the modal dialog.
    ///     Validates the user's guess and processes the catch if correct.
    ///     Uses Redis-based locking to prevent multiple users from catching the same Pokémon.
    /// </summary>
    /// <param name="pokemonName">The name of the spawned Pokémon.</param>
    /// <param name="shiny">Whether the Pokémon is shiny.</param>
    /// <param name="legendChance">The legendary spawn chance value.</param>
    /// <param name="ubChance">The Ultra Beast spawn chance value.</param>
    /// <param name="messageId">The ID of the spawn message.</param>
    /// <param name="modal">The modal containing the user's input.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("catch_modal:*,*,*,*,*", true)]
    public async Task HandleCatchModal(string? pokemonName, bool shiny, string legendChance, string ubChance,
        ulong messageId, CatchModal modal)
    {
        await ctx.Interaction.DeferAsync();

        // Check if pokemon is still available in the channel
        if (await ctx.Channel.GetMessageAsync(messageId) is not IUserMessage message ||
            message.Embeds.FirstOrDefault()?.Title == "Caught!")
        {
            await ctx.Interaction.FollowupAsync("This Pokémon has already been caught!", ephemeral: true);
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

        // Check if this Pokémon has already been caught using Redis
        if (await Service.IsPokemonAlreadyCaught(messageId))
        {
            await ctx.Interaction.FollowupAsync("This Pokémon has already been caught by someone else!",
                ephemeral: true);
            return;
        }

        // Try to acquire the catch lock using Redis
        if (!await Service.TryMarkPokemonAsCaught(messageId, ctx.User.Id))
        {
            await ctx.Interaction.FollowupAsync("This Pokémon has already been caught by someone else!",
                ephemeral: true);
            return;
        }

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