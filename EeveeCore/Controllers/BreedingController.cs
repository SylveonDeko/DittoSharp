using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Breeding.Services;
using LinqToDB.Async;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for Pokemon breeding operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class BreedingController : ControllerBase
{
    private readonly BreedingService _breedingService;
    private readonly LinqToDbConnectionProvider _dbProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BreedingController"/> class.
    /// </summary>
    /// <param name="breedingService">The breeding service.</param>
    /// <param name="dbProvider">The database connection provider.</param>
    public BreedingController(BreedingService breedingService, LinqToDbConnectionProvider dbProvider)
    {
        _breedingService = breedingService;
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Gets the user's breeding queue (female Pokemon available for breeding).
    /// </summary>
    /// <returns>List of female Pokemon available for breeding.</returns>
    [HttpGet("queue")]
    public async Task<ActionResult> GetBreedingQueue()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var firstFemale = await _breedingService.FetchFirstFemaleAsync(userId);
            
            await using var db = await _dbProvider.GetConnectionAsync();
            
            var breedingQueue = new List<object>();
            
            if (firstFemale.HasValue)
            {
                var femalePokemon = await (from ownership in db.UserPokemonOwnerships
                                         join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                         where ownership.UserId == userId && pokemon.Id == firstFemale.Value
                                         select new
                                         {
                                             pokemon.Id,
                                             pokemon.PokemonName,
                                             pokemon.Level,
                                             pokemon.Shiny,
                                             pokemon.Radiant,
                                             pokemon.Nature,
                                             Position = ownership.Position + 1
                                         }).FirstOrDefaultAsync();

                if (femalePokemon != null)
                    breedingQueue.Add(femalePokemon);
            }

            return Ok(new { success = true, queue = breedingQueue });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting breeding queue for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets breeding statistics and history for the user.
    /// </summary>
    /// <returns>Breeding statistics including success rates and recent activity.</returns>
    [HttpGet("history")]
    public async Task<ActionResult> GetBreedingHistory()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            
            var totalEggs = await (from ownership in db.UserPokemonOwnerships
                                 join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                 where ownership.UserId == userId && pokemon.PokemonName == "Egg"
                                 select pokemon).CountAsync();

            var currentEggs = await (from ownership in db.UserPokemonOwnerships
                                   join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                   where ownership.UserId == userId && pokemon.PokemonName == "Egg"
                                   select new
                                   {
                                       pokemon.Id,
                                       pokemon.PokemonName,
                                       pokemon.Counter,
                                       CaughtAt = pokemon.Timestamp,
                                       Position = ownership.Position + 1
                                   }).ToListAsync();

            var history = new
            {
                TotalEggsProduced = totalEggs,
                CurrentEggs = currentEggs,
            };

            return Ok(new { success = true, history });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting breeding history for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets breeding compatibility information for Pokemon species.
    /// </summary>
    /// <returns>Information about Pokemon egg groups and breeding compatibility.</returns>
    [HttpGet("compatibility")]
    public async Task<ActionResult> GetBreedingCompatibility()
    {
        try
        {
            await using var db = await _dbProvider.GetConnectionAsync();
            
            
            var info = new
            {
                Message = "Breeding is performed through Discord commands.",
                AvailableCommands = new[]
                {
                    "/breed - Attempt to breed two Pokemon",
                    "/daycare - Manage breeding queue",
                    "/hatch - Hatch eggs"
                },
                Requirements = new[]
                {
                    "Pokemon must be compatible egg groups",
                    "Must have one male and one female",
                    "Both Pokemon must be level 15 or higher"
                }
            };

            return Ok(new { success = true, info });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting breeding compatibility info");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }


    /// <summary>
    ///     Gets breeding statistics and information.
    /// </summary>
    /// <returns>Breeding statistics.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult> GetBreedingStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            
            var totalEggs = await (from ownership in db.UserPokemonOwnerships
                                 join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                 where ownership.UserId == userId && pokemon.PokemonName == "Egg"
                                 select pokemon).CountAsync();

            var stats = new
            {
                TotalEggs = totalEggs,
            };

            return Ok(new { success = true, stats });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting breeding stats for user {UserId}", GetCurrentUserId());
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