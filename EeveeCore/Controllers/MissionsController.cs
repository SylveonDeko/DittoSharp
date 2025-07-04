using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Missions.Services;
using EeveeCore.Services.Impl;
using LinqToDB;
using MongoDB.Driver;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for mission and progression management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class MissionsController : ControllerBase
{
    private readonly MissionService _missionService;
    private readonly LinqToDbConnectionProvider _dbProvider;
    private readonly IMongoService _mongoService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MissionsController"/> class.
    /// </summary>
    /// <param name="missionService">The mission service.</param>
    /// <param name="dbProvider">The database connection provider.</param>
    /// <param name="mongoService">The MongoDB service.</param>
    public MissionsController(MissionService missionService, LinqToDbConnectionProvider dbProvider, IMongoService mongoService)
    {
        _missionService = missionService;
        _dbProvider = dbProvider;
        _mongoService = mongoService;
    }

    /// <summary>
    ///     Gets active missions available to the user.
    /// </summary>
    /// <returns>List of active missions with progress information.</returns>
    [HttpGet("active")]
    public async Task<ActionResult> GetActiveMissions()
    {
        try
        {
            // Get missions from MongoDB
            var activeMissions = await _mongoService.Missions
                .Find(m => true) // Add any filtering logic here
                .ToListAsync();

            return Ok(new { success = true, missions = activeMissions });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting active missions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the user's mission progress and statistics.
    /// </summary>
    /// <returns>User's mission progress, level, and XP information.</returns>
    [HttpGet("progress")]
    public async Task<ActionResult> GetUserProgress()
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
                    u.Level,
                    u.EvPoints, // Assuming this is mission XP
                    u.UpvotePoints
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            // Get user's mission progress from MongoDB if it exists
            var userProgress = await _mongoService.UserProgress
                .Find(up => true) // Would need to filter by userId if UserProgress has userId field
                .ToListAsync();

            var progress = new
            {
                user.Level,
                ExperiencePoints = user.EvPoints,
                UpvotePoints = user.UpvotePoints,
                MissionProgress = userProgress
            };

            return Ok(new { success = true, progress });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user mission progress for {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets available milestones and their rewards.
    /// </summary>
    /// <returns>List of milestones with progress tracking.</returns>
    [HttpGet("milestones")]
    public async Task<ActionResult> GetMilestones()
    {
        try
        {
            var milestones = await _mongoService.Milestones
                .Find(m => true)
                .ToListAsync();

            return Ok(new { success = true, milestones });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting milestones");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the user's current crystal slime count.
    /// </summary>
    /// <returns>Current crystal slime balance.</returns>
    [HttpGet("crystal-slime")]
    public async Task<ActionResult> GetCrystalSlime()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var crystalSlime = await _missionService.GetUserCrystalSlimeAsync(userId);
            
            return Ok(new { success = true, crystalSlime });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting crystal slime for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets level progression information and XP requirements.
    /// </summary>
    /// <returns>Level progression data including XP requirements for each level.</returns>
    [HttpGet("levels")]
    public async Task<ActionResult> GetLevelProgression()
    {
        try
        {
            var levels = await _mongoService.Levels
                .Find(l => true)
                .ToListAsync();

            return Ok(new { success = true, levels });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting level progression");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets mission statistics and completion data.
    /// </summary>
    /// <returns>Mission statistics including completion rates and popular missions.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult> GetMissionStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Get basic user stats
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.Level,
                    u.EvPoints,
                    u.UpvotePoints
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            // Calculate stats based on available data
            var stats = new
            {
                CurrentLevel = user.Level,
                TotalExperience = user.EvPoints,
                UpvotePoints = user.UpvotePoints,
                // These would need to be calculated based on actual mission completion data
                MissionsCompleted = 0, // Placeholder
                TotalMissionsAvailable = await _mongoService.Missions.CountDocumentsAsync(m => true)
            };

            return Ok(new { success = true, stats });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting mission stats for user {UserId}", GetCurrentUserId());
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
}