namespace EeveeCore.Services.Impl;

/// <summary>
///     Service for managing trade locks using Redis to prevent concurrent trade operations.
/// </summary>
public class TradeLockService : ITradeLockService, INService
{
    private readonly IDataCache _cache;
    private const string TradeLockKey = "tradelock";

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeLockService" /> class.
    /// </summary>
    /// <param name="cache">The Redis cache service.</param>
    public TradeLockService(IDataCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<bool> IsUserTradeLockedAsync(ulong userId)
    {
        var database = _cache.Redis.GetDatabase();
        var tradeLockedUsers = await database.ListRangeAsync(TradeLockKey);
        
        return tradeLockedUsers.Any(value => 
            value.HasValue && 
            ulong.TryParse((string?)value, out var id) &&
            id == userId);
    }

    /// <inheritdoc />
    public async Task AddTradeLockAsync(ulong userId)
    {
        var database = _cache.Redis.GetDatabase();
        await database.ListLeftPushAsync(TradeLockKey, userId.ToString());
    }

    /// <inheritdoc />
    public async Task RemoveTradeLockAsync(ulong userId)
    {
        var database = _cache.Redis.GetDatabase();
        await database.ListRemoveAsync(TradeLockKey, userId.ToString());
    }

    /// <inheritdoc />
    public async Task<bool> ExecuteWithTradeLockAsync(IUser user, Func<Task> action)
    {
        if (await IsUserTradeLockedAsync(user.Id))
            return false;

        try
        {
            await AddTradeLockAsync(user.Id);
            
            await action();
            
            return true;
        }
        finally
        {
            await RemoveTradeLockAsync(user.Id);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExecuteWithTradeLockAsync(IUser user1, IUser user2, Func<Task> action)
    {
        if (await IsUserTradeLockedAsync(user1.Id) || await IsUserTradeLockedAsync(user2.Id))
            return false;

        try
        {
            await AddTradeLockAsync(user1.Id);
            await AddTradeLockAsync(user2.Id);
            
            await action();
            
            return true;
        }
        finally
        {
            await RemoveTradeLockAsync(user1.Id);
            await RemoveTradeLockAsync(user2.Id);
        }
    }

    /// <inheritdoc />
    public async Task ClearAllTradeLocksAsync(ulong userId)
    {
        var database = _cache.Redis.GetDatabase();
        
        await database.ListRemoveAsync(TradeLockKey, userId.ToString(), -1);
    }
}