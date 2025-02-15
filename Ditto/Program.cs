using Discord.Commands;
using Discord.Interactions;
using Serilog;
using Serilog.Events;
using Ditto.Common.ModuleBehaviors;
using Ditto.Database.DbContextStuff;
using Ditto.Services.Impl;
using Fergun.Interactive;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using RunMode = Discord.Commands.RunMode;

namespace Ditto;

public class Program
{
    public static async Task Main(string[] args)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        LogSetup.SetupLogger("Ditto");
        var credentials = new BotCredentials();

        var builder = Host.CreateDefaultBuilder(args)
            .UseSerilog((context, config) =>
            {
                config
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] | {Message:lj}{NewLine}{Exception}");
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
                    .AddDbContextFactory<DittoContext>(options =>
                    options.UseNpgsql(credentials.PostgresConfig.ConnectionString))
                    .AddSingleton<IMongoClient>(new MongoClient(credentials.MongoConfig.ConnectionString))
                    .AddTransient<IMongoService, MongoService>()
                    .AddSingleton<RedisCache>()
                    .AddSingleton<IDataCache>(s => s.GetRequiredService<RedisCache>())
                    .AddSingleton<DbContextProvider>()
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
                    .AddSingleton<Ditto>()
                    .AddHostedService<DittoService>();

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

public class DittoService(Ditto ditto) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ditto.RunAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Environment.Exit(0);
        return Task.CompletedTask;
    }
}