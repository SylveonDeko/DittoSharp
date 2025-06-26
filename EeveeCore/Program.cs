using Discord.Commands;
using Discord.Interactions;
using EeveeCore.Common.ModuleBehaviors;
using EeveeCore.Database;
using EeveeCore.Database.DbContextStuff;
using EeveeCore.Services.Impl;
using Fergun.Interactive;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;
using RunMode = Discord.Commands.RunMode;

namespace EeveeCore;

using EventHandler = EventHandler;

/// <summary>
///     The main entry point class for the Mewdeko application.
/// </summary>
public class Program
{
    /// <summary>
    ///     The entry point of the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation of running the application.</returns>
    public static async Task Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        LogSetup.SetupLogger("EeveeCore");
        var credentials = new BotCredentials();

        var builder = Host.CreateDefaultBuilder(args)
            .UseSerilog((context, config) =>
            {
                config
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] | {Message:lj}{NewLine}{Exception}");
            })
            .ConfigureServices((context, services) =>
            {
                var client = new DiscordShardedClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 15,
                    LogLevel = LogSeverity.Debug,
                    ConnectionTimeout = int.MaxValue,
                    AlwaysDownloadUsers = true,
                    GatewayIntents = GatewayIntents.All,
                    LogGatewayIntentWarnings = false,
                    DefaultRetryMode = RetryMode.RetryRatelimit
                });

                services.AddSingleton(client)
                    .AddSingleton(credentials)
                    .AddDbContextFactory<EeveeCoreContext>(options =>
                        options.UseNpgsql(credentials.PostgresConfig.ConnectionString))
                    .AddSingleton<IMongoClient>(new MongoClient(credentials.MongoConfig.ConnectionString))
                    .AddTransient<IMongoService, MongoService>()
                    .AddSingleton<RedisCache>()
                    .AddSingleton<IDataCache>(s => s.GetRequiredService<RedisCache>())
                    .AddSingleton<DbContextProvider>()
                    .AddSingleton<DatabaseMigrator>()
                    .AddSingleton<EventHandler>()
                    .AddSingleton<InteractiveService>()
                    .AddSingleton(new CommandService(new CommandServiceConfig
                    {
                        CaseSensitiveCommands = false,
                        DefaultRunMode = RunMode.Async
                    }))
                    .AddSingleton(s => new InteractionService(s.GetRequiredService<DiscordShardedClient>()))
                    .AddSingleton<GuildSettingsService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<EeveeCore>()
                    .AddHostedService<EeveeCoreService>();

                services.Scan(scan => scan.FromAssemblyOf<IReadyExecutor>()
                    .AddClasses(classes => classes.AssignableToAny(
                        typeof(INService),
                        typeof(IEarlyBehavior),
                        typeof(ILateBlocker),
                        typeof(IInputTransformer),
                        typeof(ILateExecutor)))
                    .AsSelfWithInterfaces()
                    .WithSingletonLifetime());
            });

        var host = builder.Build();
        await host.RunAsync();
    }
}

/// <summary>
///     A hosted service that manages the lifecycle EeveeCore.
/// </summary>
/// <remarks>
///     This class implements <see cref="IHostedService" /> to integrate with the .NET Core hosting model.
///     It's responsible for starting and stopping the EeveeCore as part of the application's lifecycle.
/// </remarks>
public class EeveeCoreService(EeveeCore eeveeCore) : IHostedService
{
    /// <summary>
    ///     Starts EeveeCore.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> that can be used to cancel the start operation.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation of starting the bot.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await eeveeCore.RunAsync();
    }

    /// <summary>
    ///     Stops EeveeCore.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> that can be used to cancel the stop operation.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation of stopping the bot.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Environment.Exit(0);
        return Task.CompletedTask;
    }
}