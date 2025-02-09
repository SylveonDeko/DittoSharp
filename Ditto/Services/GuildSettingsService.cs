using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Discord;
using Ditto.Common;
using Ditto.Database.Models.Mongo.Discord;
using Ditto.Database.Models.Mongo.Game;
using Ditto.Services.Impl;
using MongoDB.Driver;
using Serilog;

namespace Ditto.Services;

public class GuildSettingsService : IAsyncDisposable
{
    private readonly IMongoService _mongo;
    private readonly BotCredentials _creds;
    private readonly IDataCache _cache;
    private readonly ConcurrentDictionary<ulong, GuildConfigChanged> _changeTracker;
    private readonly Channel<(ulong GuildId, Guild Config)> _updateChannel;

    private const string PrefixCacheKey = "prefix:{0}";
    private const string ConfigCacheKey = "guild_config:{0}";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public GuildSettingsService(
        IMongoService mongo,
        BotCredentials creds,
        IDataCache cache)
    {
        _mongo = mongo;
        _creds = creds;
        _cache = cache;
        _changeTracker = new ConcurrentDictionary<ulong, GuildConfigChanged>();
        _updateChannel = Channel.CreateUnbounded<(ulong, Guild)>(
            new UnboundedChannelOptions { SingleReader = true });

        _ = ProcessConfigUpdatesAsync();
    }

    public async Task<string> GetPrefix(IGuild? guild)
    {
        if (guild is null)
            return _creds.DefaultPrefix;

        var cacheKey = string.Format(PrefixCacheKey, guild.Id);
        return await _cache.GetOrAddCachedDataAsync(cacheKey,
            async () => {
                var config = await GetGuildConfigAsync(guild.Id);
                return string.IsNullOrWhiteSpace(config.Prefix)
                    ? _creds.DefaultPrefix
                    : config.Prefix;
            },
            CacheDuration);
    }

    public async Task<string> SetPrefix(IGuild guild, string prefix)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(prefix);

        var config = await GetGuildConfigAsync(guild.Id);
        config.Prefix = prefix;

        _changeTracker.AddOrUpdate(guild.Id,
            _ => new GuildConfigChanged { LastModified = DateTime.UtcNow },
            (_, existing) =>
            {
                existing.LastModified = DateTime.UtcNow;
                return existing;
            });

        await _updateChannel.Writer.WriteAsync((guild.Id, config));
        await _cache.RemoveFromCache(string.Format(PrefixCacheKey, guild.Id));

        return prefix;
    }

    public async Task<Guild> GetGuildConfigAsync(
        ulong guildId,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string filePath = "")
    {
        var cacheKey = string.Format(ConfigCacheKey, guildId);
        try
        {
            return await _cache.GetOrAddCachedDataAsync(
                cacheKey,
                async () =>
                {
                    var config = await _mongo.Guilds
                        .Find(x => x.GuildId == guildId)
                        .FirstOrDefaultAsync();

                    return config ?? await CreateGuildConfigAsync(guildId);
                },
                CacheDuration);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed getting guild config for {GuildId} from {CallerName} in {FilePath}",
                guildId, callerName, filePath);
            throw;
        }
    }

    private async Task<Guild> CreateGuildConfigAsync(ulong guildId)
    {
        var config = new Guild
        {
            GuildId = guildId,
            Prefix = _creds.DefaultPrefix,
            DeleteSpawns = false,
            SmallImages = false,
            SilenceLevels = false,
            PinSpawns = false,
            DisabledChannels = [],
            EnabledChannels = [],
            DisabledSpawnChannels = [],
            EnableSpawnsAll = false,
            ModalView = false,
            Speed = 10,
            Locale = "en"
        };

        await _mongo.Guilds.InsertOneAsync(config);
        return config;
    }

    public async Task UpdateGuildConfigAsync(
        ulong guildId,
        Guild config,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string filePath = "")
    {
        try
        {
            _changeTracker.AddOrUpdate(guildId,
                _ => new GuildConfigChanged { LastModified = DateTime.UtcNow },
                (_, existing) =>
                {
                    existing.LastModified = DateTime.UtcNow;
                    return existing;
                });

            await _updateChannel.Writer.WriteAsync((guildId, config));

            var cacheKey = string.Format(ConfigCacheKey, guildId);
            await _cache.AddToCache(cacheKey, config, CacheDuration);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating guild config for {GuildId} from {CallerName} in {FilePath}",
                guildId, callerName, filePath);
            throw;
        }
    }

    private async Task ProcessConfigUpdatesAsync()
    {
        var batch = new List<(ulong GuildId, Guild Config)>();

        while (await _updateChannel.Reader.WaitToReadAsync())
        {
            while (batch.Count < 100 && _updateChannel.Reader.TryRead(out var update))
            {
                batch.Add(update);
            }

            if (batch.Count > 0)
            {
                await SaveConfigBatchAsync(batch);
                batch.Clear();
            }
        }
    }

    private async Task SaveConfigBatchAsync(
        IEnumerable<(ulong GuildId, Guild Config)> updates)
    {
        try
        {
            var bulkOps = new List<WriteModel<Guild>>();

            foreach (var (_, config) in updates)
            {
                var filter = Builders<Guild>.Filter
                    .Eq(x => x.GuildId, config.GuildId);

                var update = Builders<Guild>.Update
                    .Set(x => x.Prefix, config.Prefix)
                    .Set(x => x.DeleteSpawns, config.DeleteSpawns)
                    .Set(x => x.SmallImages, config.SmallImages)
                    .Set(x => x.SilenceLevels, config.SilenceLevels)
                    .Set(x => x.PinSpawns, config.PinSpawns)
                    .Set(x => x.DisabledChannels, config.DisabledChannels)
                    .Set(x => x.EnabledChannels, config.EnabledChannels)
                    .Set(x => x.DisabledSpawnChannels, config.DisabledSpawnChannels)
                    .Set(x => x.EnableSpawnsAll, config.EnableSpawnsAll)
                    .Set(x => x.ModalView, config.ModalView)
                    .Set(x => x.Speed, config.Speed)
                    .Set(x => x.Locale, config.Locale);

                bulkOps.Add(new UpdateOneModel<Guild>(filter, update)
                {
                    IsUpsert = true
                });
            }

            if (bulkOps.Any())
            {
                await _mongo.Guilds.BulkWriteAsync(bulkOps);
            }

            foreach (var (guildId, config) in updates)
            {
                var cacheKey = string.Format(ConfigCacheKey, guildId);
                await _cache.AddToCache(cacheKey, config, CacheDuration);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving guild configs batch");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _updateChannel.Writer.Complete();
        await SaveConfigBatchAsync(
            _changeTracker.Select(x => (x.Key, GetGuildConfigAsync(x.Key).GetAwaiter().GetResult())));
        GC.SuppressFinalize(this);
    }

    private class GuildConfigChanged
    {
        public DateTime LastModified { get; set; }
    }
}