using System.Collections.Immutable;

namespace EeveeCore.Common;

public class BotCredentials : IBotCredentials
{
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
            Name = "PostgreSQL"
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
    }

    public string Token { get; }
    public ulong DebugGuildId { get; }
    public ulong GuildJoinsChannelId { get; }
    public bool IsDebug { get; }

    public string DefaultPrefix { get; }

    public DbConfig PostgresConfig { get; }
    public DbConfig MongoConfig { get; }
    public DbConfig RedisConfig { get; }
    public ImmutableArray<ulong> OwnerIds { get; set; }

    public bool IsOwner(IUser u)
    {
        return OwnerIds.Contains(u.Id);
    }
}

public interface IBotCredentials
{
    string Token { get; }
    public ImmutableArray<ulong> OwnerIds { get; set; }
    string DefaultPrefix { get; }
    ulong DebugGuildId { get; }
    ulong GuildJoinsChannelId { get; }
    bool IsDebug { get; }
    DbConfig PostgresConfig { get; }
    DbConfig MongoConfig { get; }
    DbConfig RedisConfig { get; }

    public bool IsOwner(IUser u)
    {
        return OwnerIds.Contains(u.Id);
    }
}

public class DbConfig
{
    public string ConnectionString { get; set; }
    public string Name { get; set; }
}