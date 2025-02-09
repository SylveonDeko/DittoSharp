using StackExchange.Redis;

namespace Ditto.Common;

public interface IDataCache : IDisposable
{
    ConnectionMultiplexer Redis { get; }
    ISubscriber Subscriber { get; }
    
    Task<T> GetOrAddCachedDataAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
    Task<bool> TryAddToCache(string key, object value, TimeSpan? expiry = null);
    Task AddToCache(string key, object value, TimeSpan? expiry = null);
    Task RemoveFromCache(params string[] keys);
    Task<bool> ExistsInCache(string key);
    Task<T> GetFromCache<T>(string key);
    Task PublishAsync(string channel, object data);
    Task SubscribeAsync(string channel, Action<string> handler);
}