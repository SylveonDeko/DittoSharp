#nullable enable
using System.Net.Http.Headers;
using Discord.Interactions;
using Fergun.Interactive;

namespace EeveeCore.Extensions;

/// <summary>
///     Most of the extension methods for Mewdeko.
/// </summary>
public static class Extensions
{

    /// <summary>
    ///     Sends a confirmation message asynchronously.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the confirmation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task SendConfirmAsync(this IDiscordInteraction interaction, string? message)
    {
        return interaction.RespondAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build());
    }

    /// <summary>
    ///     Sends a confirmation message asynchronously with ephemeral visibility.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the confirmation.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task SendEphemeralConfirmAsync(this IDiscordInteraction interaction, string message)
    {
        return interaction.RespondAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build(),
            ephemeral: true);
    }

    /// <summary>
    ///     Sends an error message asynchronously.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the error.</param>
    /// ///
    /// <param name="config">Bot configuration.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task SendErrorAsync(this IDiscordInteraction interaction, string? message)
    {
        return interaction.RespondAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build());
    }

    /// <summary>
    ///     Sends an error message asynchronously with ephemeral visibility.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the error.</param>
    /// ///
    /// <param name="config">Bot configuration.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task SendEphemeralErrorAsync(this IDiscordInteraction interaction, string? message)
    {
        return interaction.RespondAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build(),
            ephemeral: true);
    }

    /// <summary>
    ///     Sends a confirmation follow-up message asynchronously.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the follow-up.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task<IUserMessage> SendConfirmFollowupAsync(this IDiscordInteraction interaction,
        string message)
    {
        return interaction.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build());
    }

    /// <summary>
    ///     Sends a confirmation follow-up message asynchronously with a custom component builder.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the follow-up.</param>
    /// <param name="builder">Component builder for additional interaction components.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task<IUserMessage> SendConfirmFollowupAsync(this IDiscordInteraction interaction,
        string message, ComponentBuilder builder)
    {
        return interaction.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build(),
            components: builder.Build());
    }

    /// <summary>
    ///     Sends an ephemeral follow-up confirmation message asynchronously.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the follow-up.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task<IUserMessage> SendEphemeralFollowupConfirmAsync(this IDiscordInteraction interaction,
        string message)
    {
        return interaction
            .FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build(), ephemeral: true);
    }

    /// <summary>
    ///     Sends a follow-up error message asynchronously.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the error.</param>
    /// ///
    /// <param name="config">Bot configuration.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task<IUserMessage> SendErrorFollowupAsync(this IDiscordInteraction interaction, string message)
    {
        return interaction.FollowupAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build());
    }

    /// <summary>
    ///     Sends an ephemeral follow-up error message asynchronously.
    /// </summary>
    /// <param name="interaction">Discord interaction context.</param>
    /// <param name="message">Message to include in the error.</param>
    /// <param name="config">Bot configuration.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    public static Task<IUserMessage> SendEphemeralFollowupErrorAsync(this IDiscordInteraction interaction,
        string message)
    {
        return interaction.FollowupAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build(),
            ephemeral: true);
    }


    /// <summary>
    ///     Sets the color of an embed to OK color.
    /// </summary>
    /// <param name="eb">Embed builder to set the color for.</param>
    /// <returns>Embed builder with the color set to OK color.</returns>
    public static EmbedBuilder WithOkColor(this EmbedBuilder eb)
    {
        return eb.WithColor(EeveeCore.OkColor);
    }


    /// <summary>
    ///     Sets the color of an embed to the error color.
    /// </summary>
    /// <param name="eb">Embed builder to set the color for.</param>
    /// <returns>Embed builder with the color set to the error color.</returns>
    public static EmbedBuilder WithErrorColor(this EmbedBuilder eb)
    {
        return eb.WithColor(EeveeCore.ErrorColor);
    }

    /// <summary>
    ///     Sets the color of a page builder to the OK color.
    /// </summary>
    /// <param name="eb">Page builder to set the color for.</param>
    /// <returns>Page builder with the color set to the OK color.</returns>
    public static PageBuilder WithOkColor(this PageBuilder eb)
    {
        return eb.WithColor(EeveeCore.OkColor);
    }

    /// <summary>
    ///     Sets the color of a page builder to the error color.
    /// </summary>
    /// <param name="eb">Page builder to set the color for.</param>
    /// <returns>Page builder with the color set to the error color.</returns>
    public static PageBuilder WithErrorColor(this PageBuilder eb)
    {
        return eb.WithColor(EeveeCore.ErrorColor);
    }

    /// <summary>
    ///     Adds fake headers to the HttpHeaders dictionary.
    /// </summary>
    /// <param name="dict">HttpHeaders dictionary to add headers to.</param>
    public static void AddFakeHeaders(this HttpHeaders dict)
    {
        dict.Clear();
        dict.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        dict.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
    }

    /// <summary>
    ///     Deletes a message after a specified number of seconds.
    /// </summary>
    /// <param name="msg">Message to delete.</param>
    /// <param name="seconds">Number of seconds to wait before deleting.</param>
    public static void DeleteAfter(this IUserMessage? msg, int seconds)
    {
        if (msg is null) return;

        Task.Run(async () =>
        {
            await Task.Delay(seconds * 1000).ConfigureAwait(false);
            try
            {
                await msg.DeleteAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        });
    }

    /// <summary>
    ///     Gets the top-level module of the given module.
    /// </summary>
    /// <param name="module">Module to get the top-level module for.</param>
    /// <returns>Top-level module of the given module.</returns>
    public static ModuleInfo GetTopLevelModule(this ModuleInfo module)
    {
        while (module.Parent != null) module = module.Parent;
        return module;
    }

    /// <summary>
    ///     Gets the roles associated with the specified user.
    /// </summary>
    /// <param name="user">User to get the roles for.</param>
    /// <returns>Enumerable collection of roles associated with the user.</returns>
    public static IEnumerable<IRole> GetRoles(this IGuildUser user)
    {
        return user.RoleIds.Select(r => user.Guild.GetRole(r)).Where(r => r != null);
    }
}