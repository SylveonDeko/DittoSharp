﻿using System.Text.Json.Serialization;
using Discord.Commands;
using Discord.Interactions;
using EeveeCore.AuthHandlers;
using EeveeCore.Common.ModuleBehaviors;
using EeveeCore.Services.Impl;
using Fergun.Interactive;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Serilog;
using RunMode = Discord.Commands.RunMode;

namespace EeveeCore;

using EventHandler = EventHandler;

/// <summary>
///     The main entry point class for the Mewdeko application.
/// </summary>
public class Program
{
    /// <summary>
    ///     Static constructor to configure MongoDB serialization settings.
    /// </summary>
    static Program()
    {
        // Configure MongoDB to ignore unknown elements globally for all classes
        // This prevents deserialization errors when MongoDB documents contain fields
        // that don't exist in the C# models (common during schema evolution)
        BsonClassMap.RegisterClassMap<Database.Models.Mongo.Discord.Guild>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        
        // Set global convention to ignore extra elements for all MongoDB models
        var conventionPack = new MongoDB.Bson.Serialization.Conventions.ConventionPack
        {
            new MongoDB.Bson.Serialization.Conventions.IgnoreExtraElementsConvention(true)
        };
        MongoDB.Bson.Serialization.Conventions.ConventionRegistry.Register(
            "IgnoreExtraElements", 
            conventionPack, 
            t => t.Namespace?.StartsWith("EeveeCore.Database.Models.Mongo") == true);
    }
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

        if (credentials.IsApiEnabled)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Clear default logging providers and configure Serilog
            builder.Logging.ClearProviders();
            
            ConfigureServices(builder.Services, credentials);

            builder.WebHost.UseUrls($"http://localhost:{credentials.ApiPort}");

            // Register JWT token service
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddHttpClient<JwtTokenService>();

            // Configure authentication schemes
            builder.Services.AddTransient<IApiKeyValidation, ApiKeyValidation>();
            var auth = builder.Services.AddAuthentication();

            // Add existing API Key authentication
            auth.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

            // Add new JWT authentication
            auth.AddScheme<AuthenticationSchemeOptions, JwtAuthHandler>("Jwt", null);

            // Configure authorization policies
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("ApiKeyPolicy",
                    policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes("ApiKey"))
                .AddPolicy("JwtPolicy",
                    policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes("Jwt"))
                .AddPolicy("AdminPolicy",
                    policy => policy.RequireAuthenticatedUser()
                        .AddAuthenticationSchemes("Jwt")
                        .RequireAssertion(context =>
                        {
                            var isAdmin = bool.TryParse(context.User.FindFirst("IsAdmin")?.Value, out var admin) &&
                                          admin;
                            var isBotOwner =
                                bool.TryParse(context.User.FindFirst("IsBotOwner")?.Value, out var owner) && owner;
                            return isAdmin || isBotOwner;
                        }))
                .AddPolicy("BotOwnerPolicy",
                    policy => policy.RequireAuthenticatedUser()
                        .AddAuthenticationSchemes("Jwt")
                        .RequireAssertion(context =>
                        {
                            return bool.TryParse(context.User.FindFirst("IsBotOwner")?.Value, out var isBotOwner) &&
                                   isBotOwner;
                        }));

            builder.Services.AddAuthorization();

            // Add controllers with JSON options
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                })
                .ConfigureApiBehaviorOptions(options => { options.SuppressModelStateInvalidFilter = true; });

            // Update Swagger configuration to support both authentication schemes
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(x =>
            {
                // API Key authentication
                x.AddSecurityDefinition("ApiKeyHeader", new OpenApiSecurityScheme
                {
                    Name = "X-API-Key",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Description = "Authorization by X-API-Key inside request's header"
                });

                // JWT Bearer authentication
                x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
                });

                // Add security requirements for both schemes
                x.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKeyHeader"
                            }
                        },
                        []
                    }
                });

                x.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        []
                    }
                });
            });

            // Update CORS policy to include auth endpoints
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FraudDashboardPolicy", policy =>
                {
                    policy
                        .WithOrigins($"http://localhost:{credentials.ApiPort}",
                            $"https://localhost:{credentials.ApiPort}",
                            "http://localhost:3000", // Add your Svelte dev server
                            "http://localhost:5173") // Add Vite dev server default port
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            var app = builder.Build();

            app.UseCors("FraudDashboardPolicy");
            app.UseSerilogRequestLogging();

            if (builder.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EeveeCore API V1");
                    c.RoutePrefix = "swagger";
                });
            }

            // Add authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            Log.Information("EeveeCore API listening on http://localhost:{Port}", credentials.ApiPort);
            Log.Information("Swagger UI available at http://localhost:{Port}/swagger", credentials.ApiPort);
            await app.RunAsync();
        }
        else
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.ClearProviders())
                .ConfigureServices((context, services) => { ConfigureServices(services, credentials); });

            var host = builder.Build();
            Log.Information("API is disabled. Starting bot only.");
            await host.RunAsync();
        }
    }

    /// <summary>
    ///     Configures the shared services for the application (both bot and API).
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="credentials">The bot credentials.</param>
    private static void ConfigureServices(IServiceCollection services, BotCredentials credentials)
    {
        // Add Serilog to service collection
        services.AddSerilog(LogSetup.SetupLogger("EeveeCore"));
        
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
            .AddSingleton<LinqToDbConnectionProvider>(provider => 
                new LinqToDbConnectionProvider(credentials.PostgresConfig.ConnectionString))
            .AddSingleton<IMongoClient>(new MongoClient(credentials.MongoConfig.ConnectionString))
            .AddTransient<IMongoService, MongoService>()
            .AddSingleton<RedisCache>()
            .AddSingleton<IDataCache>(s => s.GetRequiredService<RedisCache>())
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
            .AddHostedService<EeveeCoreService>()
            .AddHostedService<BackgroundTaskService>();

        services.Scan(scan => scan.FromAssemblyOf<IReadyExecutor>()
            .AddClasses(classes => classes.AssignableToAny(
                typeof(INService),
                typeof(IEarlyBehavior),
                typeof(ILateBlocker),
                typeof(IInputTransformer),
                typeof(ILateExecutor)))
            .AsSelfWithInterfaces()
            .WithSingletonLifetime());
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