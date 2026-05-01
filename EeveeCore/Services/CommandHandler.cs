using System.Collections.Concurrent;
using System.Text;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using EeveeCore.Common.Collections;
using Fergun.Interactive;
using Serilog;
using ExecuteResult = Discord.Commands.ExecuteResult;
using IResult = Discord.Interactions.IResult;

namespace EeveeCore.Services;

/// <summary>
///     Handles command parsing and execution, integrating with various services to process Discord interactions and
///     messages.
/// </summary>
public class CommandHandler : INService
{
    private const int GlobalCommandsCooldown = 750;
    private readonly IDataCache _cache;
    private readonly Timer _clearUsersOnShortCooldown;
    private readonly DiscordShardedClient _client;
    private readonly NonBlocking.ConcurrentDictionary<ulong, bool> _commandParseLock = new();

    private readonly NonBlocking.ConcurrentDictionary<ulong, ConcurrentQueue<IUserMessage>> _commandParseQueue = new();
    private readonly CommandService _commands;
    private readonly GuildSettingsService _guildSettings;
    private readonly InteractionService _interactions;
    private readonly ConcurrentHashSet<ulong> _usersOnShortCooldown = [];
    private readonly InteractiveService _interactive;

    /// <summary>
    ///     Gets the IServiceProvider for the bot.
    /// </summary>
    public readonly IServiceProvider Services;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandHandler" /> class.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="commands">The service for handling commands.</param>
    /// <param name="guildSettings">The guild settings service.</param>
    /// <param name="interactions">The service for handling interactions.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <param name="cache">The data cache service.</param>
    /// <param name="eventHandler">The event handler for discord events.</param>
    /// <param name="interactive">The interactive service for handling interactive components.</param>
    public CommandHandler(
        DiscordShardedClient client,
        CommandService commands,
        GuildSettingsService guildSettings,
        InteractionService interactions,
        IServiceProvider services,
        IDataCache cache,
        EventHandler eventHandler, InteractiveService interactive)
    {
        _client = client;
        _commands = commands;
        _guildSettings = guildSettings;
        _interactions = interactions;
        Services = services;
        _cache = cache;
        _interactive = interactive;

        _clearUsersOnShortCooldown = new Timer(_ => _usersOnShortCooldown.Clear(),
            null, GlobalCommandsCooldown, GlobalCommandsCooldown);

        eventHandler.MessageReceived += HandleMessageAsync;
        eventHandler.InteractionCreated += HandleInteractionAsync;

        _interactions.SlashCommandExecuted += HandleSlashCommand;
    }

    /// <summary>
    ///     Runs when a command is executed.
    /// </summary>
    public event Func<IUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };

    /// <summary>
    ///     Runs when a command errored.
    /// </summary>
    public event Func<CommandInfo, ITextChannel, string, IUser?, Task> CommandErrored = delegate
    {
        return Task.CompletedTask;
    };

    /// <summary>
    ///     Runs the handle event found in HelpService.
    /// </summary>
    public event Func<IUserMessage, Task> OnMessageNoTrigger = delegate { return Task.CompletedTask; };

    /// <summary>
    ///     Discord message-received handler. Filters out bots/non-user messages, enqueues the message in its
    ///     channel-specific parse queue, and triggers a queue drain. Errors are caught and logged so a single
    ///     bad message cannot break the pipeline.
    /// </summary>
    /// <param name="msg">The incoming message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleMessageAsync(IMessage msg)
    {
        try
        {
            if (msg.Author.IsBot || msg is not SocketUserMessage usrMsg)
                return;

            AddCommandToParseQueue(usrMsg);
            await ExecuteCommandsInChannelAsync(usrMsg.Channel.Id);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error in CommandHandler");
            if (ex.InnerException != null)
                Log.Warning(ex.InnerException, "Inner Exception of the error in CommandHandler");
        }
    }

    /// <summary>
    ///     Appends an incoming user message to its channel's parse queue, creating the queue on first use.
    ///     Per-channel queues serialize command execution and prevent races between concurrent senders.
    /// </summary>
    /// <param name="usrMsg">The user message to enqueue.</param>
    private void AddCommandToParseQueue(IUserMessage usrMsg)
    {
        _commandParseQueue.AddOrUpdate(usrMsg.Channel.Id,
            _ => new ConcurrentQueue<IUserMessage>([usrMsg]),
            (_, queue) =>
            {
                queue.Enqueue(usrMsg);
                return queue;
            });
    }

    /// <summary>
    ///     Executes a command in the given channel in the queue.
    /// </summary>
    /// <param name="channelId">The channel ID to run a command in.</param>
    /// <returns>True if a command was executed, false otherwise.</returns>
    public async Task<bool> ExecuteCommandsInChannelAsync(ulong channelId)
    {
        if (_commandParseLock.GetValueOrDefault(channelId) ||
            _commandParseQueue.GetValueOrDefault(channelId)?.IsEmpty != false)
            return false;

        _commandParseLock[channelId] = true;
        try
        {
            while (_commandParseQueue[channelId].TryDequeue(out var msg))
                try
                {
                    var guild = (msg.Channel as IGuildChannel)?.Guild;
                    await TryRunCommandAsync(guild, msg.Channel, msg);
                }
                catch (Exception e)
                {
                    Log.Error("Error occurred in command handler: {Error}", e);
                }

            _commandParseQueue[channelId] = new ConcurrentQueue<IUserMessage>();
            return true;
        }
        finally
        {
            _commandParseLock[channelId] = false;
        }
    }

    /// <summary>
    ///     Resolves the guild's prefix, parses and executes a command from the message, and routes the
    ///     outcome to logging and the appropriate <c>CommandExecuted</c> / <c>CommandErrored</c> event.
    /// </summary>
    /// <param name="guild">The guild the message originated from, or <c>null</c> for DMs.</param>
    /// <param name="channel">The channel the message originated from.</param>
    /// <param name="usrMsg">The user message to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task TryRunCommandAsync(IGuild? guild, IChannel channel, IUserMessage usrMsg)
    {
        var execTime = Environment.TickCount;

        var prefix = await _guildSettings.GetPrefix(guild);
        if (prefix == null) return;

        var prefixLength = GetPrefixLength(usrMsg.Content, prefix);
        if (prefixLength == 0)
        {
            await OnMessageNoTrigger(usrMsg);
            return;
        }

        var messageContent = usrMsg.Content;

        var (success, error, command) = await ExecuteCommandAsync(
            new CommandContext(_client, usrMsg),
            messageContent,
            prefixLength,
            MultiMatchHandling.Best);

        execTime = Environment.TickCount - execTime;

        if (success)
        {
            await LogCommandExecution(usrMsg, channel as ITextChannel, command, true, execTime);
            await CommandExecuted(usrMsg, command!);
            return;
        }

        if (!string.IsNullOrEmpty(error))
        {
            await LogCommandExecution(usrMsg, channel as ITextChannel, command, false, execTime, error);
            if (guild != null) await CommandErrored(command!, (channel as ITextChannel)!, error, usrMsg.Author);
        }
    }

    /// <summary>
    ///     Resolves the best-matching command for the given input by running search, preconditions, and
    ///     argument parsing in turn, applies the global short-cooldown, and executes the chosen overload.
    /// </summary>
    /// <param name="context">The command context built from the message.</param>
    /// <param name="input">The raw message content.</param>
    /// <param name="argPos">The index in <paramref name="input"/> where the prefix ends and arguments begin.</param>
    /// <param name="multiMatchHandling">Strategy when multiple commands match equally well.</param>
    /// <returns>A tuple of success flag, error reason (when failed), and the matched command info (if any).</returns>
    private async Task<(bool Success, string Error, CommandInfo? Info)> ExecuteCommandAsync(
        CommandContext context,
        string input,
        int argPos,
        MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
    {
        var searchResult = _commands.Search(context, input[argPos..]);
        if (!searchResult.IsSuccess)
            return (false, searchResult.ErrorReason, null);

        var commands = searchResult.Commands;
        var preconditionResults = await Task.WhenAll(commands.Select(async match =>
            (match, await match.Command.CheckPreconditionsAsync(context, Services))));

        var successfulPreconditions = preconditionResults
            .Where(x => x.Item2.IsSuccess)
            .ToArray();

        if (successfulPreconditions.Length == 0)
        {
            var bestCandidate = preconditionResults
                .OrderByDescending(x => x.match.Command.Priority)
                .FirstOrDefault(x => !x.Item2.IsSuccess);
            return (false, bestCandidate.Item2.ErrorReason, commands[0].Command);
        }

        var parseResults = await Task.WhenAll(successfulPreconditions.Select(async x =>
        {
            var parseResult = await x.match.ParseAsync(context, searchResult, x.Item2, Services);
            return (x.match, parseResult);
        }));

        var successfulParses = parseResults
            .Where(x => x.parseResult.IsSuccess)
            .OrderByDescending(x => x.match.Command.Priority)
            .ThenByDescending(x => x.parseResult.ArgValues.Sum(y => y.Values.Sum(z => z.Score)))
            .ToArray();

        if (successfulParses.Length == 0)
        {
            var bestMatch = parseResults.FirstOrDefault(x => !x.parseResult.IsSuccess);
            return (false, bestMatch.parseResult.ErrorReason, commands[0].Command);
        }

        var cmd = successfulParses[0].match.Command;

        if (!_usersOnShortCooldown.Add(context.User.Id))
            return (false, "You are on cooldown.", cmd);

        var chosenOverload = successfulParses[0];
        var result = await chosenOverload.match.ExecuteAsync(context, chosenOverload.parseResult, Services);

        if (result is ExecuteResult execResult)
        {
            if (execResult.Exception != null &&
                execResult.Exception is not HttpException { DiscordCode: DiscordErrorCode.InsufficientPermissions })
                Log.Warning(execResult.Exception, "Command execution error");
            return (execResult.IsSuccess, execResult.ErrorReason, cmd);
        }

        return (result.IsSuccess, result.ErrorReason, cmd);
    }

    /// <summary>
    ///     Writes a structured log line and builds an audit embed for a completed (or failed) command execution.
    /// </summary>
    /// <param name="msg">The originating user message.</param>
    /// <param name="channel">The text channel where the command ran, or <c>null</c> for DMs.</param>
    /// <param name="commandInfo">The matched command, or <c>null</c> if no match was found.</param>
    /// <param name="success">Whether the command executed successfully.</param>
    /// <param name="executionTime">Execution duration in milliseconds.</param>
    /// <param name="errorMessage">Optional error reason when <paramref name="success"/> is <c>false</c>.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LogCommandExecution(
        IMessage msg,
        ITextChannel? channel,
        CommandInfo? commandInfo,
        bool success,
        int executionTime,
        string? errorMessage = null)
    {
        var logBuilder = new StringBuilder()
            .AppendLine(success ? "Command Executed" : "Command Errored")
            .AppendLine($"User: {msg.Author} [{msg.Author.Id}]")
            .AppendLine($"Server: {(channel == null ? "PRIVATE" : $"{channel.Guild.Name} [{channel.Guild.Id}]")}")
            .AppendLine($"Channel: {(channel == null ? "PRIVATE" : $"{channel.Name} [{channel.Id}]")}")
            .AppendLine($"Message: {msg.Content}")
            .AppendLine($"Time: {executionTime}ms");

        if (!success && !string.IsNullOrEmpty(errorMessage))
            logBuilder.AppendLine($"Error: {errorMessage}");

        if (success)
            Log.Information(logBuilder.ToString());
        else
            Log.Warning(logBuilder.ToString());

        var embed = new EmbedBuilder()
            .WithColor(success ? EeveeCore.OkColor : EeveeCore.ErrorColor)
            .WithTitle(success ? "Command Executed" : "Command Errored")
            .AddField("User", $"{msg.Author} [{msg.Author.Id}]")
            .AddField("Guild", channel == null ? "PRIVATE" : $"{channel.Guild.Name} [{channel.Guild.Id}]")
            .AddField("Channel", channel == null ? "PRIVATE" : $"{channel.Name} [{channel.Id}]")
            .AddField("Message", msg.Content[..Math.Min(msg.Content.Length, 1000)])
            .AddField("Time", $"{executionTime}ms");

        if (!success && !string.IsNullOrEmpty(errorMessage))
            embed.AddField("Error", errorMessage);

        if (commandInfo != null)
            embed.AddField("Command", $"{commandInfo.Module.Name} | {commandInfo.Name}");
    }

    /// <summary>
    ///     Routes an incoming Discord interaction (slash command, component, modal, etc.) into the
    ///     <see cref="InteractionService"/>. Skips interactions already managed by the interactive paginator.
    ///     On unexpected failure, replies ephemerally to application-command interactions so the user is not
    ///     left without feedback.
    /// </summary>
    /// <param name="interaction">The Discord interaction.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        if (_interactive.IsManaged(interaction))
            return;

        try
        {
            var ctx = new ShardedInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, Services);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing interaction");

            if (interaction.Type == InteractionType.ApplicationCommand)
                await interaction.RespondAsync(
                    "Sorry, something went wrong with this command.",
                    ephemeral: true);
        }
    }

    /// <summary>
    ///     Post-execution hook for slash commands: replies with the failure reason when a command was not
    ///     successful and logs both the result and any execution exception.
    /// </summary>
    /// <param name="commandInfo">The command that ran.</param>
    /// <param name="context">The interaction context.</param>
    /// <param name="result">The execution result returned by the interaction service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleSlashCommand(SlashCommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            if (!context.Interaction.HasResponded)
                await context.Interaction.RespondAsync(
                    $"Command failed: {result.ErrorReason}",
                    ephemeral: true);

            var ex = (result as Discord.Interactions.ExecuteResult?)?.Exception;
            Log.Warning(ex,
                "Slash Command Error\n" +
                $"User: {context.User} [{context.User.Id}]\n" +
                $"Guild: {context.Guild?.Name ?? "PRIVATE"} [{context.Guild?.Id}]\n" +
                $"Channel: {context.Channel.Name} [{context.Channel.Id}]\n" +
                $"Command: {commandInfo.Name}\n" +
                $"Error: {result.ErrorReason}\n" +
                $"Error Reason: {result.Error.GetValueOrDefault()}");
            return;
        }

        Log.Information(
            "Slash Command Executed\n" +
            $"User: {context.User} [{context.User.Id}]\n" +
            $"Guild: {context.Guild?.Name ?? "PRIVATE"} [{context.Guild?.Id}]\n" +
            $"Channel: {context.Channel.Name} [{context.Channel.Id}]\n" +
            $"Command: {commandInfo.Name}");
    }

    /// <summary>
    ///     Determines the number of leading characters in a message that constitute its command prefix.
    ///     Recognizes the configured text prefix as well as the bot's mention forms (<c>@Bot</c>, <c>&lt;@id&gt;</c>,
    ///     <c>&lt;@!id&gt;</c>); returns <c>0</c> when no prefix matches.
    /// </summary>
    /// <param name="content">The raw message content.</param>
    /// <param name="prefix">The guild's configured text prefix.</param>
    /// <returns>The prefix length, or <c>0</c> if the message is not prefixed.</returns>
    private int GetPrefixLength(string content, string prefix)
    {
        if (content.StartsWith(prefix, StringComparison.InvariantCulture))
            return prefix.Length;

        var mentions = new[]
        {
            _client.CurrentUser.Mention,
            $"<@{_client.CurrentUser.Id}>",
            $"<@!{_client.CurrentUser.Id}>"
        };

        return (from mention in mentions
            where content.StartsWith(mention + " ", StringComparison.InvariantCulture)
            select mention.Length + 1).FirstOrDefault();
    }
}