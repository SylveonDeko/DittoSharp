using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Help.Services;

namespace EeveeCore.Modules.Help;

/// <summary>
///     Slash command module for help.
/// </summary>
/// <param name="serviceProvider">Service provider used to evaluate command preconditions.</param>
/// <param name="cmds">The interaction service.</param>
[Group("help", "Show what the bot can do")]
public class HelpSlashCommand(
    IServiceProvider serviceProvider,
    InteractionService cmds)
    : EeveeCoreSlashModuleBase<HelpService>
{
    /// <summary>
    ///     Shows the home help embed with category dropdown.
    /// </summary>
    [SlashCommand("show", "Show the help menu")]
    public async Task Show()
    {
        var embed = Service.GetHelpEmbed(ctx.User);
        var components = Service.GetHelpComponents();
        await RespondAsync(embed: embed.Build(), components: components.Build()).ConfigureAwait(false);
    }

    /// <summary>
    ///     Looks up a single command by name.
    /// </summary>
    /// <param name="command">The command name (with or without slash).</param>
    [SlashCommand("search", "Look up a specific command by name")]
    public async Task SearchCommand(
        [Summary("command", "The command to look up")]
        string command)
    {
        var query = command.TrimStart('/').Trim();
        var match = cmds.SlashCommands
            .FirstOrDefault(c => string.Equals(c.Name, query, StringComparison.OrdinalIgnoreCase))
            ?? cmds.SlashCommands.FirstOrDefault(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            await EphemeralReplyErrorAsync($"No command found matching `{command}`.").ConfigureAwait(false);
            return;
        }

        var embed = Service.GetCommandHelp(match);
        await RespondAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    /// <summary>
    ///     Handles category selection from the help dropdown.
    /// </summary>
    [ComponentInteraction("helpselect:*", true)]
    public async Task OnCategorySelected(string _, string[] selected)
    {
        await DeferAsync().ConfigureAwait(false);

        var key = selected.FirstOrDefault();
        if (string.IsNullOrEmpty(key))
        {
            await ModifyOriginalResponseAsync(m =>
            {
                m.Embed = Service.GetHelpEmbed(ctx.User).Build();
                m.Components = Service.GetHelpComponents().Build();
            }).ConfigureAwait(false);
            return;
        }

        var permitted = await GetPermittedCommandNamesAsync(key).ConfigureAwait(false);
        var embed = Service.GetCategoryEmbed(key, ctx.User, permitted);

        var components = Service.GetHelpComponents();
        components.WithButton("Back to categories", "helpselect:back", ButtonStyle.Secondary);

        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = embed.Build();
            m.Components = components.Build();
        }).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns to the home help view from a category.
    /// </summary>
    [ComponentInteraction("helpselect:back", true)]
    public async Task OnBackToHome()
    {
        await DeferAsync().ConfigureAwait(false);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = Service.GetHelpEmbed(ctx.User).Build();
            m.Components = Service.GetHelpComponents().Build();
        }).ConfigureAwait(false);
    }

    /// <summary>
    ///     Resolves which slash commands in a help category the invoking user is actually allowed to run by
    ///     evaluating each command's preconditions in parallel.
    /// </summary>
    /// <param name="categoryKey">The category key whose commands should be filtered.</param>
    /// <returns>A case-insensitive set of permitted command names for the current user.</returns>
    private async Task<HashSet<string>> GetPermittedCommandNamesAsync(string categoryKey)
    {
        var entry = Service.GetCategories().FirstOrDefault(c =>
            string.Equals(c.Key, categoryKey, StringComparison.OrdinalIgnoreCase));
        var classNames = entry?.ClassNames ?? new List<string> { categoryKey };

        var commands = cmds.SlashCommands
            .Where(c => classNames.Contains(c.Module.GetTopLevelModule().Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var iCtx = new InteractionContext(ctx.Client, ctx.Interaction);
        var results = await Task.WhenAll(commands.Select(async c =>
        {
            var pre = await c.CheckPreconditionsAsync(iCtx, serviceProvider).ConfigureAwait(false);
            return (Cmd: c, Allowed: pre.IsSuccess);
        })).ConfigureAwait(false);

        return results.Where(r => r.Allowed).Select(r => r.Cmd.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
