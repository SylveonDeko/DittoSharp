using System.Diagnostics;
using System.Reflection;
using Discord.Commands;
using Discord.Interactions;
using EeveeCore.Common.ModuleBehaviors;
using EeveeCore.Database.Models.Mongo.Discord;
using Serilog;
using TypeReader = Discord.Commands.TypeReader;

namespace EeveeCore;

/// <summary>
///     The main class for EeveeCore, responsible for initializing services, handling events, and managing the bot's
///     lifecycle.
/// </summary>
public class EeveeCore
{
    /// <summary>
    ///     Initializes a new instance of the EeveeCore class.
    /// </summary>
    /// <param name="services">The service provider for dependency injection.</param>
    public EeveeCore(IServiceProvider services)
    {
        Services = services;
        Credentials = Services.GetRequiredService<BotCredentials>();
        Cache = Services.GetRequiredService<IDataCache>();
        Client = Services.GetRequiredService<DiscordShardedClient>();
        CommandService = Services.GetRequiredService<CommandService>();
        GuildSettingsService = Services.GetRequiredService<GuildSettingsService>();
    }

    /// <summary>
    ///     Gets the credentials used by the bot.
    /// </summary>
    public BotCredentials Credentials { get; }

    private int ReadyCount { get; set; }

    /// <summary>
    ///     Gets the Discord client used by the bot.
    /// </summary>
    public DiscordShardedClient Client { get; }

    private GuildSettingsService GuildSettingsService { get; }
    private CommandService CommandService { get; }

    /// <summary>
    ///     Gets or sets the color used for successful operations.
    /// </summary>
    public static Color OkColor { get; set; } = new(67, 160, 71);

    /// <summary>
    ///     Gets or sets the color used for error operations.
    /// </summary>
    public static Color ErrorColor { get; set; } = new(229, 57, 53);

    /// <summary>
    ///     Gets a TaskCompletionSource that completes when the bot is ready.
    /// </summary>
    public TaskCompletionSource<bool> Ready { get; } = new();

    private IServiceProvider Services { get; }
    private IDataCache Cache { get; }

    /// <summary>
    ///     Event that occurs when the bot joins a guild.
    /// </summary>
    public event Func<Guild, Task> JoinedGuild = delegate { return Task.CompletedTask; };

    /// <summary>
    ///     Reflectively scans an assembly for non-abstract <see cref="TypeReader"/> subclasses, instantiates
    ///     each one through the DI container, and registers it with the <see cref="CommandService"/> for its
    ///     declared target type.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    private void LoadTypeReaders(Assembly assembly)
    {
        var sw = new Stopwatch();
        sw.Start();

        Type[] allTypes;
        try
        {
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Log.Warning(ex.LoaderExceptions[0], "Error getting types");
            return;
        }

        var filteredTypes = allTypes
            .Where(x => x.IsSubclassOf(typeof(TypeReader))
                        && x!.BaseType!.GetGenericArguments().Length > 0
                        && !x.IsAbstract);

        foreach (var ft in filteredTypes)
        {
            var x = (TypeReader)ActivatorUtilities.CreateInstance(Services, ft);
            var baseType = ft.BaseType;
            var typeArgs = baseType?.GetGenericArguments();
            if (typeArgs != null)
                CommandService.AddTypeReader(typeArgs[0], x);
        }

        sw.Stop();
        Log.Information("TypeReaders loaded in {ElapsedTotalSeconds}s", sw.Elapsed.TotalSeconds);
    }

    /// <summary>
    ///     Logs the sharded Discord client in, starts it, and awaits the point at which every shard has reached
    ///     the Ready state. Wires up join/leave guild handlers once startup completes.
    /// </summary>
    /// <param name="token">The Discord bot token.</param>
    /// <returns>A task that completes once all shards are ready.</returns>
    private async Task LoginAsync(string token)
    {
        Client.Log += Client_Log;
        var clientReady = new TaskCompletionSource<bool>();

        Task SetClientReady(DiscordSocketClient discordSocketClient)
        {
            ReadyCount++;
            Log.Information($"Shard {discordSocketClient.ShardId} is ready");
            Log.Information($"{ReadyCount}/{Client.Shards.Count} shards connected");
            if (ReadyCount != Client.Shards.Count)
                return Task.CompletedTask;
            _ = Task.Run(() => clientReady.TrySetResult(true));
            return Task.CompletedTask;
        }

        Log.Information("Logging in...");
        try
        {
            await Client.LoginAsync(TokenType.Bot, token.Trim()).ConfigureAwait(false);
            await Client.StartAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Login failed");
            throw;
        }

        Client.ShardReady += SetClientReady;
        await clientReady.Task.ConfigureAwait(false);
        Client.ShardReady -= SetClientReady;
        Client.JoinedGuild += Client_JoinedGuild;
        Client.LeftGuild += Client_LeftGuild;
        Log.Information("Logged in.");
        Log.Information("Logged in as:");
    }

    /// <summary>
    ///     Posts a "left guild" embed to the configured guild-joins log channel and writes a log line
    ///     when the bot is removed from a server. Errors are swallowed so logging issues never propagate.
    /// </summary>
    /// <param name="guild">The guild the bot just left.</param>
    /// <returns>A completed task; the actual work runs on a background task.</returns>
    private Task Client_LeftGuild(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var chan = await Client.Rest.GetChannelAsync(Credentials.GuildJoinsChannelId).ConfigureAwait(false);
                await ((ITextChannel)chan).SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithTitle($"Left server: {guild.Name} [{guild.Id}]")
                        .AddField("Total Guilds", Client.Guilds.Count)
                        .WithColor(ErrorColor)
                        .Build());
            }
            catch
            {
            }

            Log.Information("Left server: {Name} [{Id}]", guild.Name, guild.Id);
        });
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Downloads users for the new guild, fetches/creates its config, raises the public
    ///     <see cref="JoinedGuild"/> event, and posts a "joined guild" embed to the configured log channel.
    ///     Errors during the embed post are swallowed.
    /// </summary>
    /// <param name="guild">The guild the bot just joined.</param>
    /// <returns>A completed task; the actual work runs on a background task.</returns>
    private Task Client_JoinedGuild(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            await guild.DownloadUsersAsync().ConfigureAwait(false);
            Log.Information("Joined server: {Name} [{Id}]", guild.Name, guild.Id);

            var gc = await GuildSettingsService.GetGuildConfigAsync(guild.Id).ConfigureAwait(false);
            await JoinedGuild.Invoke(gc).ConfigureAwait(false);

            try
            {
                var chan =
                    await Client.Rest.GetChannelAsync(Credentials.GuildJoinsChannelId).ConfigureAwait(false) as
                        ITextChannel;
                var eb = new EmbedBuilder()
                    .WithTitle($"Joined {Format.Bold(guild.Name)} {guild.Id}")
                    .AddField("Members", guild.MemberCount)
                    .AddField("Text Channels", guild.TextChannels.Count)
                    .AddField("Voice Channels", guild.VoiceChannels.Count)
                    .AddField("Owner", $"Name: {guild.Owner}\nID: {guild.OwnerId}")
                    .AddField("Total Guilds", Client.Guilds.Count)
                    .WithThumbnailUrl(guild.IconUrl)
                    .WithColor(OkColor);

                await chan!.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
            }
            catch
            {
            }
        });
        return Task.CompletedTask;
    }


    /// <summary>
    ///     Runs the bot, initializing all necessary components and services.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        var sw = Stopwatch.StartNew();

        await LoginAsync(Credentials.Token).ConfigureAwait(false);

        Log.Information("Loading Services...");
        try
        {
            LoadTypeReaders(typeof(EeveeCore).Assembly);

            var migrator = Services.GetRequiredService<DatabaseMigrator>();
            var migrationSuccessful = await migrator.MigrateAsync();
            if (!migrationSuccessful)
            {
                throw new InvalidOperationException("Database migration failed");
            }

            await Task.Delay(100);

            var dbProvider = Services.GetRequiredService<LinqToDbConnectionProvider>();
            await using var context = await dbProvider.GetConnectionAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing services");
            throw;
        }

        sw.Stop();
        Log.Information("Connected in {Elapsed:F2}s", sw.Elapsed.TotalSeconds);

        var commandService = Services.GetRequiredService<CommandService>();
        commandService.Log += LogCommandsService;
        var interactionService = Services.GetRequiredService<InteractionService>();

        try
        {
            await commandService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
            await interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), Services);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error loading modules");
            throw;
        }

#if !DEBUG
        await interactionService.RegisterCommandsGloballyAsync().ConfigureAwait(false);
#else
        if (Client.Guilds.Select(x => x.Id).Contains(Credentials.DebugGuildId))
            await interactionService.RegisterCommandsToGuildAsync(Credentials.DebugGuildId);
#endif

        await ExecuteReadySubscriptions();
        Ready.TrySetResult(true);
        Log.Information("Ready.");
    }

    /// <summary>
    ///     Bridges Discord.NET's <see cref="CommandService"/> log stream into the bot's Discord log writer.
    /// </summary>
    /// <param name="arg">The log message emitted by the command service.</param>
    /// <returns>A completed task.</returns>
    private Task LogCommandsService(LogMessage arg)
    {
        WriteDiscordLog(arg);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Resolves every registered <see cref="IReadyExecutor"/> from DI and invokes their <c>OnReadyAsync</c>
    ///     hooks in parallel. Per-executor exceptions are logged so one misbehaving subscriber cannot block
    ///     the others.
    /// </summary>
    /// <returns>A task that completes when every subscriber has finished (successfully or not).</returns>
    private async Task ExecuteReadySubscriptions()
    {
        var readyExecutors = Services.GetServices<IReadyExecutor>();
        var tasks = readyExecutors.Select(async toExec =>
        {
            try
            {
                await toExec.OnReadyAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed running OnReadyAsync method on {Type} type: {Message}",
                    toExec.GetType().Name, ex.Message);
            }
        });
        await Task.WhenAll(tasks);
    }

    /// <summary>
    ///     Forwards Discord client log messages into the Serilog-backed log writer.
    /// </summary>
    /// <param name="arg">The log message emitted by the Discord client.</param>
    /// <returns>A completed task.</returns>
    private static Task Client_Log(LogMessage arg)
    {
        WriteDiscordLog(arg);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Maps a Discord <see cref="LogMessage"/> onto the matching Serilog severity level, preserving
    ///     the exception when present.
    /// </summary>
    /// <param name="arg">The log message to write.</param>
    private static void WriteDiscordLog(LogMessage arg)
    {
        var msg = arg.Message ?? arg.Exception?.Message ?? "";
        switch (arg.Severity)
        {
            case LogSeverity.Critical:
                Log.Fatal(arg.Exception, "[{Source}] {Message}", arg.Source, msg);
                break;
            case LogSeverity.Error:
                Log.Error(arg.Exception, "[{Source}] {Message}", arg.Source, msg);
                break;
            case LogSeverity.Warning:
                Log.Warning(arg.Exception, "[{Source}] {Message}", arg.Source, msg);
                break;
            case LogSeverity.Info:
                Log.Information("[{Source}] {Message}", arg.Source, msg);
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Log.Debug("[{Source}] {Message}", arg.Source, msg);
                break;
        }
    }
}