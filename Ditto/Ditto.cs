using System.Diagnostics;
using System.Reflection;
using Discord.Commands;
using Discord.Interactions;
using Ditto.Common.ModuleBehaviors;
using Ditto.Database.DbContextStuff;
using Ditto.Database.Models.Mongo.Discord;
using Serilog;
using TypeReader = Discord.Commands.TypeReader;

namespace Ditto;

public class Ditto
{
    public Ditto(IServiceProvider services)
    {
        Services = services;
        Credentials = Services.GetRequiredService<BotCredentials>();
        Cache = Services.GetRequiredService<IDataCache>();
        Client = Services.GetRequiredService<DiscordShardedClient>();
        CommandService = Services.GetRequiredService<CommandService>();
        GuildSettingsService = Services.GetRequiredService<GuildSettingsService>();
    }

    public BotCredentials Credentials { get; }
    private int ReadyCount { get; set; }
    public DiscordShardedClient Client { get; }
    private GuildSettingsService GuildSettingsService { get; }
    private CommandService CommandService { get; }
    public static Color OkColor { get; set; } = new(67, 160, 71);
    public static Color ErrorColor { get; set; } = new(229, 57, 53);
    public TaskCompletionSource<bool> Ready { get; } = new();
    private IServiceProvider Services { get; }
    private IDataCache Cache { get; }

    public event Func<Guild, Task> JoinedGuild = delegate { return Task.CompletedTask; };

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
                        && x.BaseType.GetGenericArguments().Length > 0
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
                // Ignored
            }

            Log.Information("Left server: {Name} [{Id}]", guild.Name, guild.Id);
        });
        return Task.CompletedTask;
    }

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

                await chan.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }
        });
        return Task.CompletedTask;
    }

    public async Task RunAsync()
    {
        var sw = Stopwatch.StartNew();

        await LoginAsync(Credentials.Token).ConfigureAwait(false);

        Log.Information("Loading Services...");
        try
        {
            LoadTypeReaders(typeof(Ditto).Assembly);

            var dbProvider = Services.GetRequiredService<DbContextProvider>();
            await using var context = await dbProvider.GetContextAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing services");
            throw;
        }

        sw.Stop();
        Log.Information("Connected in {Elapsed:F2}s", sw.Elapsed.TotalSeconds);

        // Initialize Commands
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

        // Register commands based on environment
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

    private Task LogCommandsService(LogMessage arg)
    {
        Log.Information(arg.ToString());
        return Task.CompletedTask;
    }

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

    private static Task Client_Log(LogMessage arg)
    {
        Log.Information(arg.ToString());
        return Task.CompletedTask;
    }
}