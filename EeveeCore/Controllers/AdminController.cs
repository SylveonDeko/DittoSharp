// Controllers/AdminController.cs - FINAL CORRECTED VERSION
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LinqToDB;
using System.Text.Json;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for administrative operations and bot owner functions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class AdminController : ControllerBase
{
    private readonly LinqToDbConnectionProvider _dbProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AdminController"/> class.
    /// </summary>
    /// <param name="dbProvider">The database connection provider.</param>
    public AdminController(LinqToDbConnectionProvider dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Gets general bot statistics and metrics.
    /// </summary>
    /// <returns>Bot statistics including user counts, Pokemon counts, and activity metrics.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult> GetBotStats()
    {
        try
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required");
            }

            await using var db = await _dbProvider.GetConnectionAsync();

            var totalUsers = await db.Users.CountAsync();
            var activeUsers = await db.Users.CountAsync(u => u.Active);
            var bannedUsers = await db.Users.CountAsync(u => u.BotBanned == true);
            var totalPokemon = await db.UserPokemon.CountAsync();
            var shinyPokemon = await db.UserPokemon.CountAsync(p => p.Shiny == true);
            var radiantPokemon = await db.UserPokemon.CountAsync(p => p.Radiant == true);
            var championPokemon = await db.UserPokemon.CountAsync(p => p.Champion);
            var recentTrades = await db.TradeLogs.CountAsync(t => t.Time > DateTime.UtcNow.AddDays(-7));
            var totalTrades = await db.TradeLogs.CountAsync();

            // Count eggs using the ownership table and Pokemon table
            var totalEggs = await db.UserPokemonOwnerships
                .Join(db.UserPokemon,
                    o => o.PokemonId,
                    p => p.Id,
                    (o, p) => new { Ownership = o, Pokemon = p })
                .CountAsync(j => j.Pokemon.PokemonName == "Egg");

            // Use window functions for efficient top Pokemon calculation
            var topPokemon = await db.UserPokemon
                .GroupBy(p => p.PokemonName)
                .Select(g => new { 
                    Pokemon = g.Key, 
                    Count = g.Count(),
                    ShinyCount = g.Sum(p => p.Shiny == true ? 1 : 0),
                    RadiantCount = g.Sum(p => p.Radiant == true ? 1 : 0),
                    AverageLevel = g.Average(p => (double?)p.Level)
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            // User distribution by region
            var regionStats = await db.Users
                .Where(u => !string.IsNullOrEmpty(u.Region))
                .GroupBy(u => u.Region)
                .Select(g => new { Region = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            // Staff distribution
            var staffStats = await db.Users
                .Where(u => !string.IsNullOrEmpty(u.Staff) && u.Staff != "User")
                .GroupBy(u => u.Staff)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            // Market activity
            var marketListings = await db.Market.CountAsync(m => m.BuyerId == null);

            var stats = new
            {
                Users = new
                {
                    Total = totalUsers,
                    Active = activeUsers,
                    Banned = bannedUsers,
                    StaffDistribution = staffStats
                },
                Pokemon = new
                {
                    Total = totalPokemon,
                    Shiny = shinyPokemon,
                    Radiant = radiantPokemon,
                    Champion = championPokemon,
                    Eggs = totalEggs,
                    ShinyRate = totalPokemon > 0 ? (double)shinyPokemon / totalPokemon * 100 : 0,
                    RadiantRate = totalPokemon > 0 ? (double)radiantPokemon / totalPokemon * 100 : 0,
                    TopSpecies = topPokemon
                },
                Trading = new
                {
                    TotalTrades = totalTrades,
                    RecentTrades = recentTrades,
                    TradesPerDay = recentTrades / 7.0
                },
                Market = new
                {
                    ActiveListings = marketListings,
                },
                Regions = regionStats
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting bot stats");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Searches for users with various filters.
    /// </summary>
    /// <param name="search">Search term for username or Discord ID.</param>
    /// <param name="staff">Filter by staff role.</param>
    /// <param name="banned">Filter by banned status.</param>
    /// <param name="region">Filter by region.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <returns>A list of users matching the search criteria.</returns>
    [HttpGet("users/search")]
    public async Task<ActionResult> SearchUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? staff = null,
        [FromQuery] bool? banned = null,
        [FromQuery] string? region = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required");
            }

            limit = Math.Min(limit, 100);

            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Build optimized query with proper joins for Pokemon count
            var baseQuery = from u in db.Users
                           join poCount in (
                               from o in db.UserPokemonOwnerships
                               group o by o.UserId into g
                               select new { UserId = g.Key, Count = g.Count() }
                           ) on u.UserId equals poCount.UserId into pokemonCounts
                           from pc in pokemonCounts.DefaultIfEmpty()
                           select new { User = u, PokemonCount = pc != null ? pc.Count : 0 };

            // Apply filters efficiently using LinqToDB
            if (!string.IsNullOrEmpty(search))
            {
                if (ulong.TryParse(search, out var userId))
                {
                    baseQuery = baseQuery.Where(q => q.User.UserId == userId);
                }
                else
                {
                    baseQuery = baseQuery.Where(q => q.User.TrainerNickname.Contains(search));
                }
            }

            if (!string.IsNullOrEmpty(staff))
            {
                baseQuery = baseQuery.Where(q => q.User.Staff == staff);
            }

            if (banned.HasValue)
            {
                baseQuery = baseQuery.Where(q => q.User.BotBanned == banned.Value);
            }

            if (!string.IsNullOrEmpty(region))
            {
                baseQuery = baseQuery.Where(q => q.User.Region == region);
            }

            // Execute with optimized projection
            var users = await baseQuery
                .Take(limit)
                .Select(q => new
                {
                    q.User.UserId,
                    q.User.TrainerNickname,
                    q.User.MewCoins,
                    q.User.Redeems,
                    q.User.Staff,
                    q.User.BotBanned,
                    q.User.VoteStreak,
                    q.User.Patreon,
                    q.User.Region,
                    q.User.Level,
                    q.User.TradeLock,
                    PokemonCount = q.PokemonCount,
                    LastVote = q.User.LastVote
                })
                .ToListAsync();

            return Ok(new { Users = users });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error searching users");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets detailed information about a specific user.
    /// </summary>
    /// <param name="userId">The Discord user ID to get information for.</param>
    /// <returns>Detailed user information including Pokemon and trading history.</returns>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult> GetUserDetails(ulong userId)
    {
        try
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required");
            }

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var pokemonCount = await db.UserPokemonOwnerships.CountAsync(o => o.UserId == userId);
            var shinyCount = await db.UserPokemonOwnerships
                .Join(db.UserPokemon, o => o.PokemonId, p => p.Id, (o, p) => p)
                .CountAsync(p => p.Shiny == true);
            var radiantCount = await db.UserPokemonOwnerships
                .Join(db.UserPokemon, o => o.PokemonId, p => p.Id, (o, p) => p)
                .CountAsync(p => p.Radiant == true);
            
            var recentTrades = await db.TradeLogs
                .Where(t => t.SenderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.Time)
                .Take(10)
                .Select(t => new
                {
                    t.TradeId,
                    t.SenderId,
                    t.ReceiverId,
                    t.Command,
                    t.Time,
                    t.SenderRedeems,
                    t.ReceiverRedeems,
                    t.SenderCredits,
                    t.ReceiverCredits
                })
                .ToListAsync();

            // Get market activity
            var marketListings = await db.Market
                .Where(m => m.OwnerId == userId && m.BuyerId == null)
                .CountAsync();

            var tokens = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Tokens ?? "{}");
            var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Inventory ?? "{}");

            var userDetails = new
            {
                BasicInfo = new
                {
                    user.UserId,
                    user.TrainerNickname,
                    user.MewCoins,
                    user.Redeems,
                    user.Staff,
                    user.BotBanned,
                    user.Region,
                    user.Patreon,
                    user.VoteStreak,
                    user.MarketLimit,
                    user.Level,
                    user.TradeLock,
                    user.Silenced,
                    user.Active
                },
                GameProgress = new
                {
                    PokemonCount = pokemonCount,
                    ShinyCount = shinyCount,
                    RadiantCount = radiantCount,
                    TotalTokens = tokens?.Values.Sum() ?? 0,
                    Hunt = user.Hunt,
                    Chain = user.Chain,
                    SelectedPokemon = user.Selected,
                    Level = user.Level,
                    FishingLevel = user.FishingLevel,
                    FishingExp = user.FishingExp,
                    Energy = user.Energy,
                    EvPoints = user.EvPoints,
                    UpvotePoints = user.UpvotePoints
                },
                Trading = new
                {
                    TradeLock = user.TradeLock,
                    RecentTrades = recentTrades,
                    MarketListings = marketListings
                },
                Resources = new
                {
                    MysteryTokens = user.MysteryToken,
                    SkinTokens = user.SkinTokens,
                    VipTokens = user.VipTokens,
                    Vouchers = user.Voucher,
                    OsRep = user.OsRep
                },
                Tokens = tokens,
                TopInventoryItems = inventory?.OrderByDescending(kvp => kvp.Value).Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            return Ok(userDetails);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user details for {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets a user's Pokemon collection for administrative review.
    /// </summary>
    /// <param name="userId">The user ID to get Pokemon for.</param>
    /// <param name="limit">Maximum number of Pokemon to return.</param>
    /// <returns>The user's Pokemon collection with detailed information.</returns>
    [HttpGet("users/{userId}/pokemon")]
    public async Task<ActionResult> GetUserPokemon(ulong userId, [FromQuery] int limit = 20)
    {
        try
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required");
            }

            limit = Math.Min(limit, 100);

            await using var db = await _dbProvider.GetConnectionAsync();
            
            var pokemon = await (from ownership in db.UserPokemonOwnerships
                                join poke in db.UserPokemon on ownership.PokemonId equals poke.Id
                                where ownership.UserId == userId
                                orderby ownership.Position
                                select new
                                {
                                    poke.Id,
                                    poke.PokemonName,
                                    poke.Nickname,
                                    poke.Level,
                                    poke.Shiny,
                                    poke.Radiant,
                                    poke.Champion,
                                    poke.Nature,
                                    poke.HeldItem,
                                    poke.Favorite,
                                    Position = ownership.Position + 1,
                                    CaughtAt = poke.Timestamp,
                                    CaughtBy = poke.CaughtBy,
                                    poke.MarketEnlist,
                                    poke.Price,
                                    poke.Tradable,
                                    poke.Breedable,
                                    poke.Voucher,
                                    poke.Temporary,
                                    IVTotal = poke.HpIv + poke.AttackIv + poke.DefenseIv + 
                                             poke.SpecialAttackIv + poke.SpecialDefenseIv + poke.SpeedIv
                                })
                                .Take(limit)
                                .ToListAsync();

            return Ok(new { Pokemon = pokemon });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting Pokemon for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Updates a user's bot ban status.
    /// </summary>
    /// <param name="userId">The Discord user ID to update.</param>
    /// <param name="request">The ban request containing status and reason.</param>
    /// <returns>Success confirmation or error response.</returns>
    [HttpPut("users/{userId}/ban")]
    public async Task<ActionResult> UpdateUserBanStatus(ulong userId, [FromBody] UpdateBanRequest request)
    {
        try
        {
            if (!IsBotOwner())
            {
                return Forbid("Bot owner access required");
            }

            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Use bulk update for better performance - check existence and update in single query
            var updatedRows = await db.Users
                .Where(u => u.UserId == userId)
                .Set(u => u.BotBanned, request.Banned)
                .UpdateAsync();

            if (updatedRows == 0)
            {
                return NotFound(new { error = "User not found" });
            }

            Log.Warning("User {UserId} {Action} by admin {AdminId}. Reason: {Reason}", 
                userId, request.Banned ? "banned" : "unbanned", GetCurrentUserId(), request.Reason ?? "No reason provided");

            return Ok(new { message = $"User {(request.Banned ? "banned" : "unbanned")} successfully", reason = request.Reason });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating ban status for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Updates a user's staff role.
    /// </summary>
    /// <param name="userId">The Discord user ID to update.</param>
    /// <param name="request">The staff role update request.</param>
    /// <returns>Success confirmation or error response.</returns>
    [HttpPut("users/{userId}/staff")]
    public async Task<ActionResult> UpdateUserStaffRole(ulong userId, [FromBody] UpdateStaffRequest request)
    {
        try
        {
            if (!IsBotOwner())
            {
                return Forbid("Bot owner access required");
            }

            var validRoles = new[] { "User", "mod", "admin", "owner" };
            if (!validRoles.Contains(request.StaffRole))
            {
                return BadRequest(new { error = "Invalid staff role", validRoles });
            }

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var oldRole = user.Staff ?? "User";
            await db.Users
                .Where(u => u.UserId == userId)
                .Set(u => u.Staff, request.StaffRole)
                .UpdateAsync();

            Log.Information("User {UserId} staff role changed from {OldRole} to {NewRole} by admin {AdminId}", 
                userId, oldRole, request.StaffRole, GetCurrentUserId());

            return Ok(new { message = "Staff role updated successfully", oldRole, newRole = request.StaffRole });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating staff role for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets suspicious trade patterns and fraud detection alerts.
    /// </summary>
    /// <param name="days">Number of days to look back for analysis.</param>
    /// <param name="minRiskScore">Minimum risk score to include in results.</param>
    /// <returns>Suspicious trading activity and fraud detection results.</returns>
    [HttpGet("fraud/suspicious-activity")]
    public async Task<ActionResult> GetSuspiciousActivity([FromQuery] int days = 7, [FromQuery] double minRiskScore = 0.5)
    {
        try
        {
            if (!IsBotOwner())
            {
                return Forbid("Bot owner access required");
            }

            await using var db = await _dbProvider.GetConnectionAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            // Get suspicious trade analytics
            var suspiciousAnalytics = await db.SuspiciousTradeAnalytics
                .Where(a => a.AnalysisTimestamp > cutoffDate && a.OverallRiskScore >= minRiskScore)
                .OrderByDescending(a => a.OverallRiskScore)
                .Take(100)
                .Select(a => new
                {
                    a.Id,
                    a.TradeId,
                    a.AnalysisTimestamp,
                    a.OverallRiskScore,
                    a.ValueImbalanceScore,
                    a.RelationshipRiskScore,
                    a.SenderTotalValue,
                    a.ReceiverTotalValue,
                    a.FlaggedAltAccount,
                    a.FlaggedRmt,
                    a.FlaggedNewbieExploitation,
                    a.AdminReviewed,
                    a.AdminVerdict
                })
                .ToListAsync();

            // Get fraud detection cases
            var fraudCases = await db.TradeFraudDetections
                .Where(f => f.DetectionTimestamp > cutoffDate)
                .OrderByDescending(f => f.DetectionTimestamp)
                .Take(50)
                .Select(f => new
                {
                    f.Id,
                    f.DetectionTimestamp,
                    f.FraudType,
                    f.RiskScore,
                    f.TriggeredRules,
                    f.AutomatedAction,
                    f.TradeBlocked,
                    f.InvestigationStatus,
                    f.FinalVerdict
                })
                .ToListAsync();

            // Risk score distribution
            var riskDistribution = await db.SuspiciousTradeAnalytics
                .Where(a => a.AnalysisTimestamp > cutoffDate)
                .GroupBy(a => (int)(a.OverallRiskScore * 10) / 10.0) // Group by 0.1 intervals
                .Select(g => new { RiskScore = g.Key, Count = g.Count() })
                .OrderBy(x => x.RiskScore)
                .ToListAsync();

            var response = new
            {
                SuspiciousAnalytics = suspiciousAnalytics,
                FraudCases = fraudCases,
                Summary = new
                {
                    TotalSuspiciousActivities = suspiciousAnalytics.Count,
                    TotalFraudCases = fraudCases.Count,
                    HighRiskActivities = suspiciousAnalytics.Count(a => a.OverallRiskScore >= 0.8),
                    BlockedTrades = fraudCases.Count(f => f.TradeBlocked),
                    RiskDistribution = riskDistribution
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting suspicious activity");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets recent system activity logs.
    /// </summary>
    /// <param name="hours">Number of hours to look back for activity.</param>
    /// <param name="limit">Maximum number of log entries to return.</param>
    /// <returns>Recent system activity including trades, spawns, and user actions.</returns>
    [HttpGet("activity")]
    public async Task<ActionResult> GetRecentActivity([FromQuery] int hours = 24, [FromQuery] int limit = 100)
    {
        try
        {
            if (!IsAdmin())
            {
                return Forbid("Admin access required");
            }

            limit = Math.Min(limit, 500);
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            await using var db = await _dbProvider.GetConnectionAsync();

            // Recent trades
            var recentTrades = await db.TradeLogs
                .Where(t => t.Time > cutoffTime)
                .OrderByDescending(t => t.Time)
                .Take(limit / 2)
                .Select(t => new
                {
                    Type = "Trade",
                    Timestamp = t.Time,
                    t.SenderId,
                    t.ReceiverId,
                    t.Command,
                    t.SenderRedeems,
                    t.ReceiverRedeems
                })
                .ToListAsync();

            // Recent spawns (if available)
            var recentSpawns = await db.ActiveSpawns
                .Where(s => s.CreatedAt > cutoffTime)
                .OrderByDescending(s => s.CreatedAt)
                .Take(limit / 4)
                .Select(s => new
                {
                    Type = "Spawn",
                    Timestamp = s.CreatedAt,
                    s.ChannelId,
                    s.IsCaught,
                    s.CaughtByUserId
                })
                .ToListAsync();

            var allActivity = new List<object>();
            allActivity.AddRange(recentTrades);
            allActivity.AddRange(recentSpawns);

            // Sort all activity by timestamp
            var sortedActivity = allActivity
                .OrderByDescending(a => GetTimestamp(a))
                .Take(limit)
                .ToList();

            return Ok(new { Activity = sortedActivity });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting recent activity");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the current user ID from JWT claims.
    /// </summary>
    /// <returns>The current user ID as a ulong.</returns>
    private ulong GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        return ulong.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    ///     Checks if the current user is an admin.
    /// </summary>
    /// <returns>True if the user is an admin, false otherwise.</returns>
    private bool IsAdmin()
    {
        var isAdmin = bool.TryParse(User.FindFirst("IsAdmin")?.Value, out var admin) && admin;
        var isBotOwner = bool.TryParse(User.FindFirst("IsBotOwner")?.Value, out var owner) && owner;
        return isAdmin || isBotOwner;
    }

    /// <summary>
    ///     Checks if the current user is a bot owner.
    /// </summary>
    /// <returns>True if the user is a bot owner, false otherwise.</returns>
    private bool IsBotOwner()
    {
        return bool.TryParse(User.FindFirst("IsBotOwner")?.Value, out var isBotOwner) && isBotOwner;
    }

    /// <summary>
    ///     Extracts timestamp from dynamic activity objects.
    /// </summary>
    /// <param name="activity">The activity object.</param>
    /// <returns>The timestamp of the activity.</returns>
    private static DateTime GetTimestamp(object activity)
    {
        var type = activity.GetType();
        var timestampProperty = type.GetProperty("Timestamp");
        return timestampProperty?.GetValue(activity) as DateTime? ?? DateTime.MinValue;
    }
}

/// <summary>
///     Request model for updating user ban status.
/// </summary>
public class UpdateBanRequest
{
    /// <summary>
    ///     Gets or sets the new banned status.
    /// </summary>
    public bool Banned { get; set; }
    
    /// <summary>
    ///     Gets or sets the reason for the ban/unban action.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
///     Request model for updating user staff role.
/// </summary>
public class UpdateStaffRequest
{
    /// <summary>
    ///     Gets or sets the new staff role to assign.
    /// </summary>
    public string StaffRole { get; set; } = null!;
}