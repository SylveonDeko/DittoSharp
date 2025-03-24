using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Help.Services;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace EeveeCore.Modules.Help;

/// <summary>
///     Slash command module for help commands.
/// </summary>
/// <param name="interactivity">The service for embed pagination</param>
/// <param name="serviceProvider">Service provider</param>
/// <param name="cmds">The command service</param>
/// <param name="ch">The command handler (yes they are different now shut up)</param>
/// <param name="guildSettings">The service to retrieve guildconfigs</param>
[Group("help", "Help Commands, what else is there to say?")]
public class HelpSlashCommand(
    InteractiveService interactivity,
    IServiceProvider serviceProvider,
    InteractionService cmds,
    CommandHandler ch,
    GuildSettingsService guildSettings)
    : EeveeCoreSlashModuleBase<HelpService>
{
    private static readonly ConcurrentDictionary<ulong, ulong> HelpMessages = new();

    /// <summary>
    ///     Shows all modules as well as additional information.
    /// </summary>
    [SlashCommand("help", "Shows help on how to use the bot")]
    public async Task Modules()
    {
        var embed = await Service.GetHelpEmbed(false, ctx.Guild, ctx.Channel, ctx.User);
        await RespondAsync(embed: embed.Build(), components: Service.GetHelpComponents(ctx.Guild, ctx.User).Build())
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Handles select menus for the help menu.
    /// </summary>
    /// <param name="unused">Literally unused</param>
    /// <param name="selected">The selected module</param>
    [ComponentInteraction("helpselect:*", true)]
    public async Task HelpSlash(string unused, string[] selected)
    {
        var currentmsg = new EeveeCoreMessage()
        {
            Content = "help", Author = ctx.User, Channel = ctx.Channel
        };

        if (HelpMessages.TryGetValue(ctx.Channel.Id, out var msgId))
        {
            try
            {
                await ctx.Channel.DeleteMessageAsync(msgId);
                HelpMessages.TryRemove(ctx.Channel.Id, out _);
            }

            catch
            {
                // ignored
            }
        }

        var module = selected.FirstOrDefault();
        module = module?.Trim().ToUpperInvariant().Replace(" ", "");
        if (string.IsNullOrWhiteSpace(module))
        {
            await Modules().ConfigureAwait(false);
            return;
        }

        var prefix = await guildSettings.GetPrefix(ctx.Guild);


        var commandInfos = cmds.SlashCommands
            .Where(c => c.Module.GetTopLevelModule().Name.ToUpperInvariant()
                            .StartsWith(module, StringComparison.InvariantCulture))
            .Distinct()
            .ToList();

        if (!commandInfos.Any())
        {
            await ReplyErrorAsync("Module not found.").ConfigureAwait(false);
            return;
        }

        // Check preconditions
        var preconditionTasks = commandInfos.Select(async x =>
        {
            var pre = await x.CheckPreconditionsAsync(new InteractionContext(ctx.Client, ctx.Interaction), serviceProvider);
            return (Cmd: x, Succ: pre.IsSuccess);
        });
        var preconditionResults = await Task.WhenAll(preconditionTasks).ConfigureAwait(false);
        var succ = new HashSet<SlashCommandInfo>(preconditionResults.Where(x => x.Succ).Select(x => x.Cmd));

        // Group and sort commands, ensuring no duplicates
        var seenCommands = new HashSet<string>();
        var cmdsWithGroup = commandInfos
            .GroupBy(c => c.Module.Name.Replace("Commands", "", StringComparison.InvariantCulture))
            .Select(g => new
            {
                ModuleName = g.Key,
                Commands = g.Where(c => seenCommands.Add(c.Name))
                    .OrderBy(c => c.Name)
                    .ToList()
            })
            .Where(g => g.Commands.Any())
            .OrderBy(g => g.ModuleName)
            .ToList();

        var pageSize = 24;
        var totalCommands = cmdsWithGroup.Sum(g => g.Commands.Count);
        var totalPages = (int)Math.Ceiling(totalCommands / (double)pageSize);

        var paginator = new LazyPaginatorBuilder()
            .AddUser(ctx.User)
            .WithPageFactory(PageFactory)
            .WithFooter(PaginatorFooter.PageNumber | PaginatorFooter.Users)
            .WithMaxPageIndex(totalPages - 1)
            .WithDefaultEmotes()
            .WithActionOnCancellation(ActionOnStop.DeleteMessage)
            .Build();

        await interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(60)).ConfigureAwait(false);

        Task<PageBuilder> PageFactory(int page)
        {
            var pageBuilder = new PageBuilder().WithOkColor();
            var commandsOnPage = new List<string>();
            var currentModule = "";
            var commandCount = 0;

            foreach (var group in cmdsWithGroup)
            {
                foreach (var cmd in group.Commands)
                {
                    if (commandCount >= page * pageSize && commandCount < (page + 1) * pageSize)
                    {
                        if (currentModule != group.ModuleName)
                        {
                            if (commandsOnPage.Any())
                                pageBuilder.AddField(currentModule,
                                    $"```css\n{string.Join("\n", commandsOnPage)}\n```");
                            commandsOnPage.Clear();
                            currentModule = group.ModuleName;
                        }

                        var cmdString =
                            $"{(succ.Contains(cmd) ? "✅" : "❌")}" +
                            $"/{cmd.Name}";
                        commandsOnPage.Add(cmdString);
                    }

                    commandCount++;
                    if (commandCount >= (page + 1) * pageSize) break;
                }

                if (commandCount >= (page + 1) * pageSize) break;
            }

            if (commandsOnPage.Any())
                pageBuilder.AddField(currentModule, $"```css\n{string.Join("\n", commandsOnPage)}\n```");

            pageBuilder.WithDescription("\u2705: You can use this command." +
                                        "\n \u274c: You cannot use this command." +
                                        "\n {0}: If you need any help don't hesitate to join [The Support Server](https://discord.gg/mewdeko)" +
                                        "\nDo `/help commandname` to see info on that command");

            return Task.FromResult(pageBuilder);
        }
    }

    /// <summary>
    ///     ALlows you to search for a command using the autocompleter. Can also show help for the command thats chosen from
    ///     autocomplete.
    /// </summary>
    /// <param name="command">The command to search for or to get help for</param>
    [SlashCommand("search", "get information on a specific command")]
    public async Task SearchCommand
    (
        [Summary("command", "the command to get information about")]
        string command
    )
    {
        var com = cmds.SlashCommands.FirstOrDefault(x => x.Name.Contains(command));
        if (com == null)
        {
            await Modules().ConfigureAwait(false);
            return;
        }

        var embed = await Service.GetCommandHelp(com, ctx.Guild, (ctx.User as IGuildUser)!);
        await RespondAsync(embed: embed.Build()).ConfigureAwait(false);
    }

    /// <summary>
    ///     Toggles module descriptions in help.
    /// </summary>
    /// <param name="sDesc">Bool thats parsed to either true or false to show the descriptions</param>
    /// <param name="sId">The server id the button is ran in</param>
    [ComponentInteraction("toggle-descriptions:*,*", true)]
    public async Task ToggleHelpDescriptions(string sDesc, string sId)
    {
        if (ctx.User.Id.ToString() != sId) return;

        await DeferAsync().ConfigureAwait(false);
        var description = bool.TryParse(sDesc, out var desc) && desc;
        var message = (ctx.Interaction as SocketMessageComponent)?.Message;
        var embed = await Service.GetHelpEmbed(description, ctx.Guild, ctx.Channel, ctx.User);

        await message.ModifyAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = Service.GetHelpComponents(ctx.Guild, ctx.User, !description).Build();
        }).ConfigureAwait(false);
    }
}