using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Spawn.Services;
using EeveeCore.Services.Impl;
using LinqToDB;
using MongoDB.Driver;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for Pokemon spawn information and statistics (read-only).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class SpawnController : ControllerBase
{
    private readonly SpawnService _spawnService;
    private readonly LinqToDbConnectionProvider _dbProvider;
    private readonly IMongoService _mongoService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SpawnController"/> class.
    /// </summary>
    /// <param name="spawnService">The spawn service.</param>
    /// <param name="dbProvider">The database connection provider.</param>
    /// <param name="mongoService">The MongoDB service.</param>
    public SpawnController(SpawnService spawnService, LinqToDbConnectionProvider dbProvider, IMongoService mongoService)
    {
        _spawnService = spawnService;
        _dbProvider = dbProvider;
        _mongoService = mongoService;
    }

    /// <summary>
    ///     Gets information about the spawn system and mechanics.
    /// </summary>
    /// <returns>Spawn system information and how it works.</returns>
    [HttpGet("info")]
    public async Task<ActionResult> GetSpawnInfo()
    {
        try
        {
            var spawnInfo = new
            {
                Description = "Pokemon spawn in Discord channels when users are active",
                Mechanics = new
                {
                    BaseSpawnChance = 0.03, // 3% base chance per message
                    BaseCooldown = 20, // 20 seconds between spawns in a guild
                    Factors = new[]
                    {
                        "User activity in channels",
                        "Guild-specific spawn rates",
                        "Time-based modifiers",
                        "Special events"
                    }
                },
                HowToCatch = new[]
                {
                    "Pokemon appear in Discord channels",
                    "Use /catch command or click the catch button",
                    "First person to catch gets the Pokemon",
                    "Some Pokemon may require specific methods"
                },
                Tips = new[]
                {
                    "Stay active in channels to increase spawn chances",
                    "React quickly when Pokemon spawn",
                    "Check multiple channels for spawns",
                    "Some rare Pokemon have special spawn conditions"
                }
            };

            await Task.CompletedTask; // Make it actually async
            return Ok(new { success = true, spawnInfo });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting spawn info");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the user's spawn/catch statistics.
    /// </summary>
    /// <returns>User's catching statistics and achievements.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult> GetUserSpawnStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Get user's catch statistics
            var totalCaught = await (from ownership in db.UserPokemonOwnerships
                                   join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                   where ownership.UserId == userId
                                   select pokemon).CountAsync();

            var shinyCaught = await (from ownership in db.UserPokemonOwnerships
                                   join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                   where ownership.UserId == userId && pokemon.Shiny == true
                                   select pokemon).CountAsync();

            var radiantCaught = await (from ownership in db.UserPokemonOwnerships
                                     join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                     where ownership.UserId == userId && pokemon.Radiant == true
                                     select pokemon).CountAsync();

            // Get user's chain and hunt info
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.Chain, u.Hunt })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            var stats = new
            {
                TotalPokemonCaught = totalCaught,
                ShinyCaught = shinyCaught,
                RadiantCaught = radiantCaught,
                ShinyRate = totalCaught > 0 ? (double)shinyCaught / totalCaught * 100 : 0,
                RadiantRate = totalCaught > 0 ? (double)radiantCaught / totalCaught * 100 : 0,
                CurrentChain = user.Chain,
                CurrentHunt = user.Hunt ?? "None"
            };

            return Ok(new { success = true, stats });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting spawn stats for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets information about current events affecting spawns.
    /// </summary>
    /// <returns>Current spawn events and bonuses.</returns>
    [HttpGet("events")]
    public async Task<ActionResult> GetCurrentEvents()
    {
        try
        {
            // Get current events from MongoDB
            var currentEvents = await _mongoService.CurrentRadiants
                .Find(_ => true)
                .ToListAsync();

            // This would also check for other types of events
            var events = new
            {
                RadiantEvents = currentEvents,
                Message = "Events affect spawn rates and available Pokemon",
                Note = "Check Discord announcements for current event details"
            };

            return Ok(new { success = true, events });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting current events");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets recent spawn activity (if available).
    /// </summary>
    /// <returns>Recent spawn activity information.</returns>
    [HttpGet("recent")]
    public async Task<ActionResult> GetRecentSpawns()
    {
        try
        {
            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Get recent spawns from ActiveSpawns table if it tracks this
            var recentSpawns = await db.ActiveSpawns
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .Select(s => new
                {
                    s.ChannelId,
                    s.CreatedAt,
                    s.IsCaught,
                    s.CaughtByUserId
                })
                .ToListAsync();

            return Ok(new { success = true, recentSpawns });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting recent spawns");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets spawn rates and probabilities for different Pokemon types.
    /// </summary>
    /// <returns>Spawn rate information for different Pokemon categories.</returns>
    [HttpGet("rates")]
    public async Task<ActionResult> GetSpawnRates()
    {
        try
        {
            // This would ideally come from configuration or the spawn service
            var spawnRates = new
            {
                BaseRates = new
                {
                    Common = "60%",
                    Uncommon = "25%",
                    Rare = "10%",
                    Epic = "4%",
                    Legendary = "1%"
                },
                SpecialRates = new
                {
                    Shiny = "1 in 4096 (base rate)",
                    Radiant = "Event dependent",
                    Shadow = "Event dependent"
                },
                Modifiers = new[]
                {
                    "Chain bonuses increase shiny rates",
                    "Events can boost specific Pokemon",
                    "Guild settings may affect rates",
                    "Time of day may influence spawns"
                },
                Note = "Actual rates may vary based on current events and user factors"
            };

            await Task.CompletedTask; // Make it actually async
            return Ok(new { success = true, spawnRates });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting spawn rates");
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

    // Note: Spawn catching must be done through Discord
    // This controller provides informational data about spawns for dashboard display
}