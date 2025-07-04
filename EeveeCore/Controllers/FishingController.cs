using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Fishing.Services;
using EeveeCore.Services.Impl;
using LinqToDB;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for fishing mechanics and operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class FishingController : ControllerBase
{
    private readonly FishingService _fishingService;
    private readonly LinqToDbConnectionProvider _dbProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FishingController"/> class.
    /// </summary>
    /// <param name="fishingService">The fishing service.</param>
    /// <param name="dbProvider">The database connection provider.</param>
    public FishingController(FishingService fishingService, LinqToDbConnectionProvider dbProvider)
    {
        _fishingService = fishingService;
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Gets the user's fishing statistics and level information.
    /// </summary>
    /// <returns>User's fishing stats including level, experience, and progress.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult> GetFishingStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.FishingLevel,
                    u.FishingExp,
                    u.Inventory
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            // Calculate experience needed for next level (this logic would need to match the fishing service)
            var expForNextLevel = (user.FishingLevel + 1) * 1000; // Placeholder calculation
            var expProgress = user.FishingExp % 1000; // Placeholder calculation

            var stats = new
            {
                FishingLevel = user.FishingLevel,
                CurrentExp = user.FishingExp,
                ExpProgress = expProgress,
                ExpForNextLevel = expForNextLevel,
                ExpToNextLevel = Math.Max(0, expForNextLevel.GetValueOrDefault() - user.FishingExp.GetValueOrDefault())
            };

            return Ok(new { success = true, stats });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting fishing stats for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets available fishing rods and their requirements.
    /// </summary>
    /// <returns>List of fishing rods with level requirements and effects.</returns>
    [HttpGet("rods")]
    public async Task<ActionResult> GetFishingRods()
    {
        try
        {
            // This should come from the fishing service or MongoDB configuration
            var fishingRods = new[]
            {
                new { Name = "old-rod", RequiredLevel = 0, Description = "Basic fishing rod", ExpMultiplier = 1.0 },
                new { Name = "good-rod", RequiredLevel = 10, Description = "Improved fishing rod", ExpMultiplier = 1.2 },
                new { Name = "super-rod", RequiredLevel = 25, Description = "Advanced fishing rod", ExpMultiplier = 1.5 },
                new { Name = "master-rod", RequiredLevel = 50, Description = "Master fishing rod", ExpMultiplier = 2.0 }
            };

            await Task.CompletedTask; // Make it actually async
            return Ok(new { success = true, rods = fishingRods });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting fishing rods");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets fishing rewards by tier/category.
    /// </summary>
    /// <param name="tier">The fishing tier to get rewards for.</param>
    /// <returns>List of possible rewards for the specified tier.</returns>
    [HttpGet("rewards/{tier}")]
    public async Task<ActionResult> GetFishingRewards(string tier)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tier))
                return BadRequest(new { error = "Tier is required" });

            var rewards = await _fishingService.GetItemsByTier(tier);
            
            return Ok(new { success = true, tier, rewards });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting fishing rewards for tier {Tier}", tier);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets all available fishing tiers and their information.
    /// </summary>
    /// <returns>List of fishing tiers with descriptions.</returns>
    [HttpGet("tiers")]
    public async Task<ActionResult> GetFishingTiers()
    {
        try
        {
            // This should ideally come from configuration or the fishing service
            var tiers = new[]
            {
                new { Name = "common", Description = "Common fishing rewards", MinLevel = 0 },
                new { Name = "uncommon", Description = "Uncommon fishing rewards", MinLevel = 5 },
                new { Name = "rare", Description = "Rare fishing rewards", MinLevel = 15 },
                new { Name = "epic", Description = "Epic fishing rewards", MinLevel = 30 },
                new { Name = "legendary", Description = "Legendary fishing rewards", MinLevel = 50 }
            };

            await Task.CompletedTask; // Make it actually async
            return Ok(new { success = true, tiers });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting fishing tiers");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Calculates experience gain for a fishing attempt.
    /// </summary>
    /// <param name="request">The experience calculation request.</param>
    /// <returns>Calculated experience gain.</returns>
    [HttpPost("calculate-exp")]
    public async Task<ActionResult> CalculateExpGain([FromBody] CalculateExpRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Rod))
                return BadRequest(new { error = "Rod type is required" });

            if (request.Level <= 0)
                return BadRequest(new { error = "Level must be greater than 0" });

            var expGain = await _fishingService.CalculateExpGain(request.Rod, request.Level);
            
            return Ok(new { success = true, expGain, rod = request.Rod, level = request.Level });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating fishing exp gain");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the user's fishing inventory/catches.
    /// </summary>
    /// <returns>List of items caught through fishing.</returns>
    [HttpGet("inventory")]
    public async Task<ActionResult> GetFishingInventory()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Inventory)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            // Parse inventory and filter for fishing-related items
            var inventory = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(user))
            {
                try
                {
                    var allInventory = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(user) ?? new();
                    
                    var fishingItems = new[] { "fish", "rare-fish", "treasure", "seaweed", "pearl", "bottle" };
                    inventory = allInventory
                        .Where(kvp => fishingItems.Any(item => kvp.Key.Contains(item) || kvp.Key.Contains("fish")))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
                catch (System.Text.Json.JsonException)
                {
                    // Handle invalid JSON gracefully
                    inventory = new Dictionary<string, int>();
                }
            }

            return Ok(new { success = true, inventory });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting fishing inventory for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Handles opening multi-box rewards from fishing.
    /// </summary>
    /// <returns>Multi-box opening results.</returns>
    [HttpPost("multi-box")]
    public async Task<ActionResult> HandleMultiBox()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var result = await _fishingService.HandleMultiBox(userId);
            
            return Ok(new { success = true, result });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling multi-box for user {UserId}", GetCurrentUserId());
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

    #region Request Models

    /// <summary>
    ///     Request model for calculating experience gain.
    /// </summary>
    public class CalculateExpRequest
    {
        /// <summary>Gets or sets the fishing rod type.</summary>
        public string Rod { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the fishing level.</summary>
        public ulong Level { get; set; }
    }

    #endregion
}