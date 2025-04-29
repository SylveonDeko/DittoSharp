using StackExchange.Redis;

namespace EeveeCore.Common;

/// <summary>
///     Provides caching functionality using Redis for efficient data retrieval and storage.
/// </summary>
public interface IDataCache : IDisposable
{
    /// <summary>
    ///     Gets the Redis connection multiplexer instance.
    /// </summary>
    ConnectionMultiplexer Redis { get; }

    /// <summary>
    ///     Gets the Redis subscriber for pub/sub operations.
    /// </summary>
    ISubscriber Subscriber { get; }

    /// <summary>
    ///     Retrieves an item from the cache or adds it if not present.
    /// </summary>
    /// <typeparam name="T">The type of the item to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">A function that creates the item if it's not found in the cache.</param>
    /// <param name="expiry">Optional expiration time for the cache entry.</param>
    /// <returns>The cached or newly added item.</returns>
    Task<T> GetOrAddCachedDataAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

    /// <summary>
    ///     Attempts to add an item to the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiry">Optional expiration time for the cache entry.</param>
    /// <returns><c>true</c> if the item was added successfully; otherwise, <c>false</c>.</returns>
    Task<bool> TryAddToCache(string key, object value, TimeSpan? expiry = null);

    /// <summary>
    ///     Adds an item to the cache, overwriting any existing value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiry">Optional expiration time for the cache entry.</param>
    Task AddToCache(string key, object value, TimeSpan? expiry = null);

    /// <summary>
    ///     Removes items from the cache.
    /// </summary>
    /// <param name="keys">The keys of items to remove.</param>
    Task RemoveFromCache(params string[] keys);

    /// <summary>
    ///     Determines whether an item exists in the cache.
    /// </summary>
    /// <param name="key">The cache key to check.</param>
    /// <returns><c>true</c> if the item exists in the cache; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsInCache(string key);

    /// <summary>
    ///     Retrieves an item from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the item to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached item.</returns>
    Task<T> GetFromCache<T>(string key);

    /// <summary>
    ///     Publishes data to a Redis channel.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    /// <param name="data">The data to publish.</param>
    Task PublishAsync(string channel, object data);

    /// <summary>
    ///     Subscribes to a Redis channel.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    /// <param name="handler">The action to execute when data is received on the channel.</param>
    Task SubscribeAsync(string channel, Action<string> handler);
}