using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Parties.Services;
using LinqToDB;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for Pokemon party management (read-only operations).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class PartyController : ControllerBase
{
    private readonly PartyService _partyService;
    private readonly LinqToDbConnectionProvider _dbProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PartyController"/> class.
    /// </summary>
    /// <param name="partyService">The party service.</param>
    /// <param name="dbProvider">The database connection provider.</param>
    public PartyController(PartyService partyService, LinqToDbConnectionProvider dbProvider)
    {
        _partyService = partyService;
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Gets the user's current party configuration.
    /// </summary>
    /// <returns>Current party with Pokemon details.</returns>
    [HttpGet("current")]
    public async Task<ActionResult> GetCurrentParty()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Party)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            // Parse party array and get Pokemon details
            var partyPokemonIds = user ?? new ulong[0];
            var partyPokemon = new List<object>();

            for (var i = 0; i < partyPokemonIds.Length && i < 6; i++)
            {
                var pokemonId = partyPokemonIds[i];
                if (pokemonId == 0)
                {
                    partyPokemon.Add(new { SlotNumber = i + 1, IsEmpty = true });
                    continue;
                }

                var pokemon = await (from ownership in db.UserPokemonOwnerships
                                   join p in db.UserPokemon on ownership.PokemonId equals p.Id
                                   where ownership.UserId == userId && p.Id == pokemonId
                                   select new
                                   {
                                       p.Id,
                                       p.PokemonName,
                                       p.Level,
                                       p.Shiny,
                                       p.Radiant,
                                       p.Nature,
                                       p.HeldItem,
                                       p.Champion,
                                       Position = ownership.Position + 1,
                                       IVTotal = p.HpIv + p.AttackIv + p.DefenseIv + 
                                                p.SpecialAttackIv + p.SpecialDefenseIv + p.SpeedIv
                                   }).FirstOrDefaultAsync();

                if (pokemon != null)
                {
                    partyPokemon.Add(new
                    {
                        SlotNumber = i + 1,
                        IsEmpty = false,
                        pokemon.Id,
                        pokemon.PokemonName,
                        pokemon.Level,
                        pokemon.Shiny,
                        pokemon.Radiant,
                        pokemon.Nature,
                        pokemon.HeldItem,
                        pokemon.Champion,
                        pokemon.Position,
                        pokemon.IVTotal
                    });
                }
                else
                {
                    partyPokemon.Add(new { SlotNumber = i + 1, IsEmpty = true });
                }
            }

            // Fill remaining slots if party is less than 6
            while (partyPokemon.Count < 6)
            {
                partyPokemon.Add(new { SlotNumber = partyPokemon.Count + 1, IsEmpty = true });
            }

            return Ok(new { success = true, party = partyPokemon });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting current party for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets saved party configurations for the user.
    /// </summary>
    /// <returns>List of saved party names and information.</returns>
    [HttpGet("saved")]
    public async Task<ActionResult> GetSavedParties()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            // Note: This would require checking how saved parties are stored in the system
            // The PartyService has methods like DoesPartyExist, but we need to see how parties are persisted
            
            var savedParties = new
            {
                Message = "Saved parties can be managed through Discord commands.",
                AvailableCommands = new[]
                {
                    "/party register <name> - Save current party configuration",
                    "/party load <name> - Load a saved party",
                    "/party list - View saved parties"
                },
                Note = "Party management through web interface is not currently supported."
            };

            return Ok(new { success = true, savedParties });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting saved parties for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets party statistics and battle-readiness information.
    /// </summary>
    /// <returns>Party analysis including levels, types, and battle readiness.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult> GetPartyStats()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Party)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            var partyPokemonIds = user?.Where(id => id != 0).ToList() ?? new List<ulong>();
            
            if (!partyPokemonIds.Any())
            {
                return Ok(new { 
                    success = true, 
                    stats = new { 
                        PartySize = 0,
                        AverageLevel = 0,
                        TotalIVs = 0,
                        ShinyCount = 0,
                        ChampionCount = 0,
                        Message = "No Pokemon in party"
                    }
                });
            }

            var partyStats = await (from ownership in db.UserPokemonOwnerships
                                  join p in db.UserPokemon on ownership.PokemonId equals p.Id
                                  where ownership.UserId == userId && partyPokemonIds.Contains(p.Id)
                                  select new
                                  {
                                      p.Level,
                                      p.Shiny,
                                      p.Champion,
                                      IVTotal = p.HpIv + p.AttackIv + p.DefenseIv + 
                                               p.SpecialAttackIv + p.SpecialDefenseIv + p.SpeedIv
                                  }).ToListAsync();

            var stats = new
            {
                PartySize = partyStats.Count,
                AverageLevel = partyStats.Any() ? partyStats.Average(p => p.Level) : 0,
                TotalIVs = partyStats.Sum(p => p.IVTotal),
                AverageIVs = partyStats.Any() ? partyStats.Average(p => p.IVTotal) : 0,
                ShinyCount = partyStats.Count(p => p.Shiny == true),
                ChampionCount = partyStats.Count(p => p.Champion == true),
                HighestLevel = partyStats.Any() ? partyStats.Max(p => p.Level) : 0,
                LowestLevel = partyStats.Any() ? partyStats.Min(p => p.Level) : 0
            };

            return Ok(new { success = true, stats });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting party stats for user {UserId}", GetCurrentUserId());
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
    ///     Adds a Pokemon to a specific party slot.
    /// </summary>
    /// <param name="request">The add Pokemon request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("add")]
    public async Task<ActionResult> AddPokemonToParty([FromBody] AddPokemonToPartyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (request.Slot < 1 || request.Slot > 6)
                return BadRequest(new { error = "Slot must be between 1 and 6" });

            if (request.PokemonPosition <= 0)
                return BadRequest(new { error = "Pokemon position must be greater than 0" });

            var result = await _partyService.AddPokemonToParty(userId, request.Slot, (int)request.PokemonPosition);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error adding Pokemon to party for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Removes a Pokemon from a specific party slot.
    /// </summary>
    /// <param name="slot">The party slot to clear (1-6).</param>
    /// <returns>Success or error message.</returns>
    [HttpDelete("slot/{slot}")]
    public async Task<ActionResult> RemovePokemonFromParty(int slot)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (slot < 1 || slot > 6)
                return BadRequest(new { error = "Slot must be between 1 and 6" });

            var result = await _partyService.RemovePokemonFromParty(userId, slot);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error removing Pokemon from party slot {Slot} for user {UserId}", slot, GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Saves the current party configuration with a name.
    /// </summary>
    /// <param name="request">The save party request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("save")]
    public async Task<ActionResult> SavePartyConfiguration([FromBody] SavePartyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.PartyName))
                return BadRequest(new { error = "Party name is required" });

            var result = await _partyService.RegisterParty(userId, request.PartyName);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving party configuration for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Loads a saved party configuration.
    /// </summary>
    /// <param name="request">The load party request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("load")]
    public async Task<ActionResult> LoadPartyConfiguration([FromBody] LoadPartyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.PartyName))
                return BadRequest(new { error = "Party name is required" });

            var result = await _partyService.LoadParty(userId, request.PartyName);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading party configuration for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Deletes a saved party configuration.
    /// </summary>
    /// <param name="request">The delete party request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("delete")]
    public async Task<ActionResult> DeletePartyConfiguration([FromBody] DeletePartyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.PartyName))
                return BadRequest(new { error = "Party name is required" });

            var result = await _partyService.DeregisterParty(userId, request.PartyName);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting party configuration for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Swaps two Pokemon positions in the party.
    /// </summary>
    /// <param name="request">The swap request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("swap")]
    public async Task<ActionResult> SwapPartyPositions([FromBody] SwapPartyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (request.Slot1 < 1 || request.Slot1 > 6 || request.Slot2 < 1 || request.Slot2 > 6)
                return BadRequest(new { error = "Both slots must be between 1 and 6" });

            if (request.Slot1 == request.Slot2)
                return BadRequest(new { error = "Cannot swap a slot with itself" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { error = "User not found" });

            // Get current party
            var party = user.Party ?? new ulong[6];
            
            // Ensure party array is at least 6 elements
            if (party.Length < 6)
            {
                var newParty = new ulong[6];
                for (var i = 0; i < Math.Min(party.Length, 6); i++)
                {
                    newParty[i] = party[i];
                }
                party = newParty;
            }

            // Swap the Pokemon (adjust for 0-based indexing)
            var temp = party[request.Slot1 - 1];
            party[request.Slot1 - 1] = party[request.Slot2 - 1];
            party[request.Slot2 - 1] = temp;

            // Update in database
            await db.Users
                .Where(u => u.UserId == userId)
                .Set(u => u.Party, party)
                .UpdateAsync();

            return Ok(new { success = true, message = $"Swapped Pokemon in slots {request.Slot1} and {request.Slot2}" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error swapping party positions for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #region Request Models

    /// <summary>
    ///     Request model for adding a Pokemon to party.
    /// </summary>
    public class AddPokemonToPartyRequest
    {
        /// <summary>Gets or sets the party slot (1-6).</summary>
        public int Slot { get; set; }
        
        /// <summary>Gets or sets the Pokemon position in collection.</summary>
        public ulong PokemonPosition { get; set; }
    }

    /// <summary>
    ///     Request model for saving a party configuration.
    /// </summary>
    public class SavePartyRequest
    {
        /// <summary>Gets or sets the name for the saved party.</summary>
        public string PartyName { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Request model for loading a party configuration.
    /// </summary>
    public class LoadPartyRequest
    {
        /// <summary>Gets or sets the name of the party to load.</summary>
        public string PartyName { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Request model for deleting a party configuration.
    /// </summary>
    public class DeletePartyRequest
    {
        /// <summary>Gets or sets the name of the party to delete.</summary>
        public string PartyName { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Request model for swapping party positions.
    /// </summary>
    public class SwapPartyRequest
    {
        /// <summary>Gets or sets the first slot to swap.</summary>
        public int Slot1 { get; set; }
        
        /// <summary>Gets or sets the second slot to swap.</summary>
        public int Slot2 { get; set; }
    }

    #endregion
}