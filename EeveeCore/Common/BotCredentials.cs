using System.Collections.Immutable;

namespace EeveeCore.Common;

/// <summary>
///     Provides configuration and credentials for the bot application.
/// </summary>
public class BotCredentials : IBotCredentials
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BotCredentials" /> class.
    ///     Loads configuration from config.json and environment variables.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when required configuration values are missing.
    /// </exception>
    public BotCredentials()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", false, true)
            .AddEnvironmentVariables()
            .Build();

        Token = config[nameof(Token)]
                ?? throw new ArgumentNullException(nameof(Token), "Bot token must be provided in config");
        DefaultPrefix = config[nameof(DefaultPrefix)] ?? "";

        DebugGuildId = ulong.TryParse(config[nameof(DebugGuildId)], out var devGuild)
            ? devGuild
            : 0;

        GuildJoinsChannelId = ulong.TryParse(config[nameof(GuildJoinsChannelId)], out var joinLog)
            ? joinLog
            : 0;

        OwnerIds =
        [
            ..config.GetSection(nameof(OwnerIds)).GetChildren()
                .Select(c => ulong.Parse(c.Value))
        ];

        IsDebug = bool.Parse(config[nameof(IsDebug)] ?? "false");

        PostgresConfig = new DbConfig
        {
            ConnectionString = config["PostgresConnectionString"]
                               ?? throw new ArgumentNullException("PostgresConnectionString",
                                   "Postgres connection string must be provided"),
        };

        MongoConfig = new DbConfig
        {
            ConnectionString = config["MongoConnectionString"]
                               ?? throw new ArgumentNullException("MongoConnectionString",
                                   "MongoDB connection string must be provided"),
            Name = "MongoDB"
        };

        RedisConfig = new DbConfig
        {
            ConnectionString = config["RedisConnectionString"]
                               ?? throw new ArgumentNullException("RedisConnectionString",
                                   "Redis connection string must be provided"),
            Name = "Redis"
        };

        // Web API Configuration
        IsApiEnabled = bool.Parse(config[nameof(IsApiEnabled)] ?? "false");
        ApiPort = int.Parse(config[nameof(ApiPort)] ?? "5000");
        JwtSecret = config[nameof(JwtSecret)] ?? "DefaultJwtSecret";
        DiscordClientId = config[nameof(DiscordClientId)] ?? "DefaultDiscordClientId";
        DiscordClientSecret = config[nameof(DiscordClientSecret)] ?? "DefaultDiscordClientSecret";
    }

    /// <summary>
    ///     Gets the JWT secret key for token signing and validation.
    /// </summary>
    public string JwtSecret { get; private set; } = null!;
    
    /// <summary>
    ///     Gets the Discord client ID for OAuth authentication.
    /// </summary>
    public string DiscordClientId { get; private set; } = null!;
    
    /// <summary>
    ///     Gets the Discord client secret for OAuth authentication.
    /// </summary>
    public string DiscordClientSecret { get; private set; } = null!;
    
    /// <summary>
    ///     Gets the Discord bot token used for authentication.
    /// </summary>
    public string Token { get; }

    /// <summary>
    ///     Gets the guild ID used for testing and debugging purposes.
    /// </summary>
    public ulong DebugGuildId { get; }

    /// <summary>
    ///     Gets the channel ID where guild join notifications are sent.
    /// </summary>
    public ulong GuildJoinsChannelId { get; }

    /// <summary>
    ///     Gets a value indicating whether the bot is running in debug mode.
    /// </summary>
    public bool IsDebug { get; }

    /// <summary>
    ///     Gets the default command prefix for the bot.
    /// </summary>
    public string DefaultPrefix { get; }

    /// <summary>
    /// </summary>
    public DbConfig PostgresConfig { get; }

    /// <summary>
    ///     Gets the configuration for MongoDB database connection.
    /// </summary>
    public DbConfig MongoConfig { get; }

    /// <summary>
    ///     Gets the configuration for Redis database connection.
    /// </summary>
    public DbConfig RedisConfig { get; }

    /// <summary>
    ///     Gets a value indicating whether the web API is enabled.
    /// </summary>
    public bool IsApiEnabled { get; }

    /// <summary>
    ///     Gets the port for the web API server.
    /// </summary>
    public int ApiPort { get; }

    /// <summary>
    ///     Gets the API key for authentication.
    /// </summary>
    public string ApiKey { get; }

    /// <summary>
    ///     Gets or sets the collection of user IDs that have owner privileges.
    /// </summary>
    public ImmutableArray<ulong> OwnerIds { get; set; }

    /// <summary>
    ///     Determines whether a user has owner privileges.
    /// </summary>
    /// <param name="u">The user to check.</param>
    /// <returns>True if the user is an owner; otherwise, false.</returns>
    public bool IsOwner(IUser u)
    {
        return OwnerIds.Contains(u.Id);
    }
}

/// <summary>
///     Defines the interface for bot credentials and configuration.
/// </summary>
public interface IBotCredentials
{
    /// <summary>
    ///     Gets the Discord bot token used for authentication.
    /// </summary>
    string Token { get; }

    /// <summary>
    ///     Gets or sets the collection of user IDs that have owner privileges.
    /// </summary>
    public ImmutableArray<ulong> OwnerIds { get; set; }

    /// <summary>
    ///     Gets the default command prefix for the bot.
    /// </summary>
    string DefaultPrefix { get; }

    /// <summary>
    ///     Gets the guild ID used for testing and debugging purposes.
    /// </summary>
    ulong DebugGuildId { get; }

    /// <summary>
    ///     Gets the channel ID where guild join notifications are sent.
    /// </summary>
    ulong GuildJoinsChannelId { get; }

    /// <summary>
    ///     Gets a value indicating whether the bot is running in debug mode.
    /// </summary>
    bool IsDebug { get; }

    /// <summary>
    /// </summary>
    DbConfig PostgresConfig { get; }

    /// <summary>
    ///     Gets the configuration for MongoDB database connection.
    /// </summary>
    DbConfig MongoConfig { get; }

    /// <summary>
    ///     Gets the configuration for Redis database connection.
    /// </summary>
    DbConfig RedisConfig { get; }

    /// <summary>
    ///     Gets a value indicating whether the web API is enabled.
    /// </summary>
    bool IsApiEnabled { get; }

    /// <summary>
    ///     Gets the port for the web API server.
    /// </summary>
    int ApiPort { get; }

    /// <summary>
    ///     Gets the API key for authentication.
    /// </summary>
    string ApiKey { get; }

    /// <summary>
    ///     Determines whether a user has owner privileges.
    /// </summary>
    /// <param name="u">The user to check.</param>
    /// <returns>True if the user is an owner; otherwise, false.</returns>
    public bool IsOwner(IUser u)
    {
        return OwnerIds.Contains(u.Id);
    }
}

/// <summary>
///     Represents database connection configuration.
/// </summary>
public class DbConfig
{
    /// <summary>
    ///     Gets or sets the connection string for the database.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    ///     Gets or sets the friendly name of the database.
    /// </summary>
    public string Name { get; set; }
}