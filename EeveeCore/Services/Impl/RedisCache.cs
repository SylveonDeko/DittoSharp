﻿using System.Text.Json;
using StackExchange.Redis;

namespace EeveeCore.Services.Impl;

public class RedisCache : IDataCache
{
    private readonly IDatabase _db;

    public RedisCache(BotCredentials creds)
    {
        Redis = ConnectionMultiplexer.Connect(creds.RedisConfig.ConnectionString);
        _db = Redis.GetDatabase();
    }

    public ISubscriber Subscriber => Redis.GetSubscriber();
    public ConnectionMultiplexer Redis { get; }

    public async Task<T> GetOrAddCachedDataAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        var data = await _db.StringGetAsync(key);
        if (data.HasValue)
            return JsonSerializer.Deserialize<T>(data);

        var value = await factory();
        await _db.StringSetAsync(key,
            JsonSerializer.Serialize(value),
            expiry ?? TimeSpan.FromHours(1));

        return value;
    }

    public async Task<bool> TryAddToCache(string key, object value, TimeSpan? expiry = null)
    {
        return await _db.StringSetAsync(key,
            JsonSerializer.Serialize(value),
            expiry ?? TimeSpan.FromHours(1),
            When.NotExists);
    }

    public async Task AddToCache(string key, object value, TimeSpan? expiry = null)
    {
        await _db.StringSetAsync(key,
            JsonSerializer.Serialize(value),
            expiry ?? TimeSpan.FromHours(1));
    }

    public async Task RemoveFromCache(params string[] keys)
    {
        await _db.KeyDeleteAsync(keys.Select(x => new RedisKey(x)).ToArray());
    }

    public async Task<bool> ExistsInCache(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task<T> GetFromCache<T>(string key)
    {
        var data = await _db.StringGetAsync(key);
        return data.HasValue ? JsonSerializer.Deserialize<T>(data) : default;
    }

    public async Task PublishAsync(string channel, object data)
    {
        await Subscriber.PublishAsync(channel, JsonSerializer.Serialize(data));
    }

    public async Task SubscribeAsync(string channel, Action<string> handler)
    {
        await Subscriber.SubscribeAsync(channel, (_, message) =>
        {
            if (message.HasValue)
                handler(message);
        });
    }

    public void Dispose()
    {
        Redis?.Dispose();
    }
}