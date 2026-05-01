using EeveeCore.Database.Models.Mongo.Game;
using MongoDB.Driver;
using LinqToDB;
using LinqToDB.Async;

namespace EeveeCore.Services.Impl;

/// <summary>
///     Background service that handles periodic tasks like energy regeneration, mission rotation, and honey expiration.
/// </summary>
public class BackgroundTaskService : BackgroundService, INService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundTaskService> _logger;
    private Timer? _energyTimer;
    private Timer? _missionTimer;
    private Timer? _honeyTimer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BackgroundTaskService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">The logger for this service.</param>
    public BackgroundTaskService(IServiceProvider serviceProvider, ILogger<BackgroundTaskService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    ///     Starts the background tasks for energy regeneration, mission rotation, and honey expiration.
    /// </summary>
    /// <param name="stoppingToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        
        _energyTimer = new Timer(RegenerateEnergy, null, TimeSpan.Zero, TimeSpan.FromMinutes(24));
        
        _missionTimer = new Timer(CheckMissions, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        
        _honeyTimer = new Timer(CheckHoneyExpiration, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        
        _logger.LogInformation("Background tasks started");
        
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background tasks stopping...");
        }
    }

    /// <summary>
    ///     Timer callback that bumps every user's energy up by one (capped at 10) for users who are below the
    ///     cap. Errors are caught and logged so a transient DB issue cannot crash the timer.
    /// </summary>
    /// <param name="state">Timer state object (unused).</param>
    private async void RegenerateEnergy(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbProvider = scope.ServiceProvider.GetRequiredService<LinqToDbConnectionProvider>();
            await using var db = await dbProvider.GetConnectionAsync();
            
            var affectedRows = await db.Users
                .Where(u => (u.Energy ?? 0) < 10)
                .Set(u => u.Energy, u => Math.Min(10, (u.Energy ?? 0) + 1))
                .UpdateAsync();
                
            if (affectedRows > 0)
            {
                _logger.LogInformation("Regenerated energy for {Count} users", affectedRows);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating energy");
        }
    }

    /// <summary>
    ///     Timer callback that rotates the active mission set every 24 hours: deactivates the current actives,
    ///     picks two random previously-inactive missions to promote, stamps the rotation time, and clears all
    ///     <c>UserProgress</c> rows so users start the new period from zero. No-ops when fewer than two
    ///     candidates are available, or when the current rotation is younger than 24 hours.
    /// </summary>
    /// <param name="state">Timer state object (unused).</param>
    private async void CheckMissions(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mongoService = scope.ServiceProvider.GetRequiredService<IMongoService>();
            
            var missions = mongoService.Missions;
            var userProgress = mongoService.UserProgress;
            
            var totalMissions = await missions.CountDocumentsAsync(Builders<Mission>.Filter.Empty);
            if (totalMissions == 0)
            {
                _logger.LogWarning("No missions found in database, skipping mission rotation");
                return;
            }
            
            var activeMissions = await missions
                .Find(Builders<Mission>.Filter.Eq(m => m.Active, true))
                .ToListAsync();
            
            var mostRecentMission = activeMissions
                .OrderByDescending(m => m.StartedEpoch ?? m.Started ?? DateTime.MinValue)
                .FirstOrDefault();
            
            var currentTime = DateTime.UtcNow;
            
            if (mostRecentMission != null)
            {
                var startedTime = mostRecentMission.StartedEpoch ?? mostRecentMission.Started ?? DateTime.MinValue;
                if (startedTime > DateTime.MinValue && currentTime - startedTime < TimeSpan.FromHours(24))
                {
                    return;
                }
            }
            
            await missions.UpdateManyAsync(
                Builders<Mission>.Filter.Eq(m => m.Active, true),
                Builders<Mission>.Update.Set(m => m.Active, false)
            );
            
            var potentialMissions = await missions
                .Find(Builders<Mission>.Filter.Eq(m => m.Active, false))
                .ToListAsync();
            
            if (potentialMissions.Count >= 2)
            {
                var random = Random.Shared;
                var chosenMissions = potentialMissions.OrderBy(x => random.Next()).Take(2);
                
                foreach (var mission in chosenMissions)
                {
                    await missions.UpdateOneAsync(
                        Builders<Mission>.Filter.Eq(m => m.Id, mission.Id),
                        Builders<Mission>.Update
                            .Set(m => m.Active, true)
                            .Set(m => m.StartedEpoch, currentTime)
                    );
                }
                
                await userProgress.DeleteManyAsync(Builders<UserProgress>.Filter.Empty);
                
                _logger.LogInformation("Rotated missions and reset user progress. Selected {Count} new missions", chosenMissions.Count());
            }
            else if (potentialMissions.Count == 0)
            {
                _logger.LogWarning("No inactive missions available for rotation");
            }
            else
            {
                _logger.LogWarning("Only {Count} missions available, need at least 2 for rotation", potentialMissions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking missions");
        }
    }

    /// <summary>
    ///     Timer callback that prunes <c>Honey</c> rows whose unix-epoch expiry is in the past, logging the
    ///     number deleted. Errors are caught and logged.
    /// </summary>
    /// <param name="state">Timer state object (unused).</param>
    private async void CheckHoneyExpiration(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbProvider = scope.ServiceProvider.GetRequiredService<LinqToDbConnectionProvider>();
            await using var db = await dbProvider.GetConnectionAsync();
            
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            var expiredHoneys = await db.Honey
                .Where(h => h.Expires < currentTime)
                .ToListAsync();
            
            if (!expiredHoneys.Any()) return;
            
            var deletedCount = await db.Honey
                .Where(h => h.Expires < currentTime)
                .DeleteAsync();
            
            _ = Task.Run(async () =>
            {
                var discordService = scope.ServiceProvider.GetService<DiscordShardedClient>();
                if (discordService == null) return;
                
                foreach (var honey in expiredHoneys)
                {
                    try
                    {
                        var user = await discordService.GetUserAsync(honey.OwnerId, CacheMode.AllowDownload, null);
                        if (user != null)
                        {
                            var embed = new EmbedBuilder()
                                .WithTitle($"Your {honey.Type} spread has expired!")
                                .WithDescription($"Your {honey.Type} Spread in <#{honey.ChannelId}> has expired!\nSpread some more with `/spread`.")
                                .WithColor(0xFFB6C1)
                                .Build();
                            
                            await user.SendMessageAsync(embed: embed);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to notify user {UserId} about honey expiration", honey.OwnerId);
                    }
                }
            });
            
            _logger.LogInformation("Processed {Count} expired honey spreads", expiredHoneys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking honey expiration");
        }
    }

    /// <summary>
    ///     Disposes the background service and stops all timers.
    /// </summary>
    public override void Dispose()
    {
        _energyTimer?.Dispose();
        _missionTimer?.Dispose();
        _honeyTimer?.Dispose();
        base.Dispose();
    }
}