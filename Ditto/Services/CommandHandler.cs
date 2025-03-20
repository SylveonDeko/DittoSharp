using System.Collections.Concurrent;
using System.Text;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Ditto.Common.Collections;
using Ditto.Database.DbContextStuff;
using Serilog;
using ExecuteResult = Discord.Commands.ExecuteResult;
using IResult = Discord.Interactions.IResult;

namespace Ditto.Services;

public class CommandHandler : INService
{
    private const int GlobalCommandsCooldown = 750;
    private readonly IDataCache _cache;
    private readonly Timer _clearUsersOnShortCooldown;
    private readonly DiscordShardedClient _client;
    private readonly NonBlocking.ConcurrentDictionary<ulong, bool> _commandParseLock = new();

    private readonly NonBlocking.ConcurrentDictionary<ulong, ConcurrentQueue<IUserMessage>> _commandParseQueue = new();
    private readonly CommandService _commands;
    private readonly DbContextProvider _db;
    private readonly GuildSettingsService _guildSettings;
    private readonly InteractionService _interactions;
    private readonly ConcurrentHashSet<ulong> _usersOnShortCooldown = [];
    public readonly IServiceProvider Services;

    public CommandHandler(
        DiscordShardedClient client,
        CommandService commands,
        DbContextProvider db,
        GuildSettingsService guildSettings,
        InteractionService interactions,
        IServiceProvider services,
        IDataCache cache,
        EventHandler eventHandler)
    {
        _client = client;
        _commands = commands;
        _db = db;
        _guildSettings = guildSettings;
        _interactions = interactions;
        Services = services;
        _cache = cache;

        _clearUsersOnShortCooldown = new Timer(_ => _usersOnShortCooldown.Clear(),
            null, GlobalCommandsCooldown, GlobalCommandsCooldown);

        eventHandler.MessageReceived += HandleMessageAsync;
        eventHandler.InteractionCreated += HandleInteractionAsync;

        _interactions.SlashCommandExecuted += HandleSlashCommand;
    }

    public event Func<IUserMessage, CommandInfo, Task> CommandExecuted = delegate { return Task.CompletedTask; };

    public event Func<CommandInfo, ITextChannel, string, IUser?, Task> CommandErrored = delegate
    {
        return Task.CompletedTask;
    };

    public event Func<IUserMessage, Task> OnMessageNoTrigger = delegate { return Task.CompletedTask; };

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

    private async Task TryRunCommandAsync(IGuild? guild, IChannel channel, IUserMessage usrMsg)
    {
        var execTime = Environment.TickCount;

        // Get prefix for this guild
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

        /*// Update stats if command was recognized
        if (command != null && guild != null)
        {
            await using var db = await _db.GetContextAsync();
            var guildConfig = await _guildSettings.GetGuildConfigAsync(guild.Id);
            if (!guildConfig.StatsOptOut)
            {
                var commandStats = new CommandStats
                {
                    ChannelId = channel.Id,
                    GuildId = guild.Id,
                    IsSlash = false,
                    NameOrId = command.Name,
                    UserId = usrMsg.Author.Id,
                    Module = command.Module.Name
                };
                await db.CommandStats.AddAsync(commandStats);
                await db.SaveChangesAsync();
            }
        }*/

        if (success)
        {
            await LogCommandExecution(usrMsg, channel as ITextChannel, command, true, execTime);
            await CommandExecuted(usrMsg, command);
            return;
        }

        if (!string.IsNullOrEmpty(error))
        {
            await LogCommandExecution(usrMsg, channel as ITextChannel, command, false, execTime, error);
            if (guild != null) await CommandErrored(command, channel as ITextChannel, error, usrMsg.Author);
        }
    }

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
            .WithColor(success ? Ditto.OkColor : Ditto.ErrorColor)
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

        /*// Log to command log channel if configured
        if (channel?.Guild != null)
        {
            var guildConfig = await _guildSettings.GetGuildConfigAsync(channel.Guild.Id);
            if (guildConfig.CommandLogChannel != 0)
            {
                try
                {
                    var logChannel = await channel.Guild.GetTextChannelAsync(guildConfig.CommandLogChannel);
                    if (logChannel != null)
                        await logChannel.SendMessageAsync(embed: embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to log command to guild log channel");
                }
            }
        }*/
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
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

    private async Task HandleSlashCommand(SlashCommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            await context.Interaction.RespondAsync(
                $"Command failed: {result.ErrorReason}",
                ephemeral: true);

            Log.Warning(
                "Slash Command Error\n" +
                $"User: {context.User} [{context.User.Id}]\n" +
                $"Guild: {context.Guild?.Name ?? "PRIVATE"} [{context.Guild?.Id}]\n" +
                $"Channel: {context.Channel.Name} [{context.Channel.Id}]\n" +
                $"Command: {commandInfo.Name}\n" +
                $"Error: {result.ErrorReason}\n" +
                $"Error Reason: {result.Error.GetValueOrDefault()}");
            return;
        }

        // Log successful command execution
        Log.Information(
            "Slash Command Executed\n" +
            $"User: {context.User} [{context.User.Id}]\n" +
            $"Guild: {context.Guild?.Name ?? "PRIVATE"} [{context.Guild?.Id}]\n" +
            $"Channel: {context.Channel.Name} [{context.Channel.Id}]\n" +
            $"Command: {commandInfo.Name}");

        /*// Update command stats if enabled
        if (context.Guild != null)
        {
            var config = await _guildSettings.GetGuildConfigAsync(context.Guild.Id);
            if (!config.StatsOptOut)
            {
                await using var dbContext = await _db.GetContextAsync();
                await dbContext.CommandStats.AddAsync(new CommandStats
                {
                    ChannelId = context.Channel.Id,
                    GuildId = context.Guild.Id,
                    IsSlash = true,
                    Module = commandInfo.Module.Name,
                    NameOrId = commandInfo.Name,
                    UserId = context.User.Id
                });
                await dbContext.SaveChangesAsync();
            }
        }*/
    }

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

    public void Dispose()
    {
        _clearUsersOnShortCooldown?.Dispose();
    }
}