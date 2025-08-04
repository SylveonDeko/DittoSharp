using EeveeCore.Database.Linq.Models.Pokemon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Parties.Services;
using EeveeCore.Services.Impl;
using LinqToDB;
using MongoDB.Driver;
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
    private readonly IMongoService _mongoService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PartyController"/> class.
    /// </summary>
    /// <param name="partyService">The party service.</param>
    /// <param name="dbProvider">The database connection provider.</param>
    /// <param name="mongoService">The MongoDB service for accessing Pokemon forms data.</param>
    public PartyController(PartyService partyService, LinqToDbConnectionProvider dbProvider, IMongoService mongoService)
    {
        _partyService = partyService;
        _dbProvider = dbProvider;
        _mongoService = mongoService;
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
            
            // Get current party from Parties table
            var currentParty = await db.Parties
                .Where(p => p.UserId == userId && p.IsCurrentParty)
                .FirstOrDefaultAsync();

            // If no current party exists, create an empty one
            if (currentParty == null)
            {
                currentParty = new Party
                {
                    UserId = userId,
                    Name = "Current Party",
                    IsCurrentParty = true,
                    Quick = false
                };
                await db.InsertAsync(currentParty);
            }

            var slots = new[] { currentParty.Slot1, currentParty.Slot2, currentParty.Slot3, 
                               currentParty.Slot4, currentParty.Slot5, currentParty.Slot6 };

            // Get all party Pokemon IDs that are not null/zero
            var partyPokemonIds = slots
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .ToList();

            var partyPokemon = new List<object>();

            if (partyPokemonIds.Count != 0)
            {
                // Fetch all party Pokemon data in a single query with ownership info
                var pokemonData = await (from ownership in db.UserPokemonOwnerships
                                        join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                                        where ownership.UserId == userId && partyPokemonIds.Contains(pokemon.Id)
                                        select new { Pokemon = pokemon, ownership.Position })
                    .ToListAsync();

                // Read Forms data once for all Pokemon
                var allForms = await _mongoService.Forms
                    .Find(_ => true)
                    .ToListAsync();
                var formsLookup = allForms.ToDictionary(f => f.Identifier.ToLower(), f => f);

                // Create a lookup for quick access
                var pokemonLookup = pokemonData.ToDictionary(p => p.Pokemon.Id, p => p);

                // Build party slots in correct order
                for (var i = 0; i < 6; i++)
                {
                    var pokemonId = slots[i];
                    if (!pokemonId.HasValue || pokemonId.Value == 0)
                    {
                        partyPokemon.Add(new { SlotNumber = i + 1, IsEmpty = true });
                        continue;
                    }

                    if (pokemonLookup.TryGetValue(pokemonId.Value, out var pokemonEntry))
                    {
                        var pokemon = pokemonEntry.Pokemon;

                        var pokemonName = pokemon.PokemonName.ToLower();
                        var pokemonIdForImage = 0;
                        var formId = 0;
                        var imagePath = "/images/regular/133-0-.png";

                        // Find form info
                        if (formsLookup.TryGetValue(pokemonName, out var identifier))
                        {
                            var suffix = identifier.FormIdentifier;

                            if (!string.IsNullOrEmpty(suffix) && pokemonName.EndsWith(suffix))
                            {
                                formId = (int)(identifier.FormOrder - 1)!;
                                var formName = pokemonName[..^(suffix.Length + 1)];

                                if (formsLookup.TryGetValue(formName, out var pokemonIdentifier))
                                    pokemonIdForImage = pokemonIdentifier.PokemonId;
                            }
                            else
                            {
                                pokemonIdForImage = identifier.PokemonId;
                            }

                            // Build image path
                            var pathSegments = new List<string> { "/images", "regular" };

                            if (pokemon.Radiant == true) pathSegments.Add("radiant");
                            if (pokemon.Shiny == true) pathSegments.Add("shiny");
                            if (!string.IsNullOrEmpty(pokemon.Skin) && pokemon.Skin != "None" && pokemon.Skin != "NULL")
                                pathSegments.Add(pokemon.Skin.TrimEnd('/'));

                            var fileType = "png";
                            if (!string.IsNullOrEmpty(pokemon.Skin) && pokemon.Skin.EndsWith("_gif"))
                                fileType = "gif";

                            var fileName = $"{pokemonIdForImage}-{formId}-.{fileType}";
                            pathSegments.Add(fileName);

                            imagePath = string.Join("/", pathSegments);
                        }

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
                            Position = pokemonEntry.Position + 1,
                            IVTotal = pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv + 
                                     pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv,
                            ImagePath = imagePath
                        });
                    }
                    else
                    {
                        partyPokemon.Add(new { SlotNumber = i + 1, IsEmpty = true });
                    }
                }
            }
            else
            {
                // No Pokemon in party, create 6 empty slots
                for (var i = 0; i < 6; i++)
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

            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Get all saved parties for the user with full data in single query
            var parties = await db.Parties
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            // Get all unique Pokemon IDs from all parties
            var allPokemonIds = parties
                .SelectMany(p => new[] { p.Slot1, p.Slot2, p.Slot3, p.Slot4, p.Slot5, p.Slot6 })
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            // Fetch all Pokemon names in a single query
            var pokemonLookup = allPokemonIds.Any()
                ? await db.UserPokemon
                    .Where(p => allPokemonIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.PokemonName)
                : new Dictionary<ulong, string>();

            // Build the response with Pokemon names
            var savedPartiesWithDetails = parties.Select(party =>
            {
                var slots = new[] { party.Slot1, party.Slot2, party.Slot3, 
                                   party.Slot4, party.Slot5, party.Slot6 };
                
                var pokemonNames = slots
                    .Where(slot => slot.HasValue && slot.Value > 0)
                    .Select(slot => pokemonLookup.TryGetValue(slot!.Value, out var name) ? name : null)
                    .Where(name => name != null)
                    .Cast<string>()
                    .ToList();

                return new
                {
                    party.Name,
                    party.Quick,
                    party.IsCurrentParty,
                    PokemonCount = slots.Count(slot => slot.HasValue && slot.Value > 0),
                    PokemonNames = pokemonNames
                };
            }).ToList();

            return Ok(new { success = true, savedParties = savedPartiesWithDetails });
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
            
            // Get current party from Parties table
            var currentParty = await db.Parties
                .Where(p => p.UserId == userId && p.IsCurrentParty)
                .FirstOrDefaultAsync();

            if (currentParty == null)
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

            // Get party Pokemon IDs from slots
            var partyPokemonIds = new[] { currentParty.Slot1, currentParty.Slot2, currentParty.Slot3, 
                                         currentParty.Slot4, currentParty.Slot5, currentParty.Slot6 }
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .ToList();
            
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

            var partyStats = await db.UserPokemon
                .Where(p => partyPokemonIds.Contains(p.Id))
                .Select(p => new
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
    ///     Creates a custom saved party by adding Pokemon to specific slots without affecting the current party.
    /// </summary>
    /// <param name="request">The create custom party request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("create-custom")]
    public async Task<ActionResult> CreateCustomParty([FromBody] CreateCustomPartyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.PartyName))
                return BadRequest(new { error = "Party name is required" });

            if (request.PokemonSlots == null || !request.PokemonSlots.Any())
                return BadRequest(new { error = "At least one Pokemon slot must be provided" });

            if (request.PokemonSlots.Any(slot => slot.SlotNumber < 1 || slot.SlotNumber > 6))
                return BadRequest(new { error = "All slot numbers must be between 1 and 6" });

            if (request.PokemonSlots.Any(slot => slot.PokemonPosition <= 0))
                return BadRequest(new { error = "All Pokemon positions must be greater than 0" });

            // Check for duplicate slots
            var slotNumbers = request.PokemonSlots.Select(s => s.SlotNumber).ToList();
            if (slotNumbers.Count != slotNumbers.Distinct().Count())
                return BadRequest(new { error = "Duplicate slot numbers are not allowed" });

            await using var db = await _dbProvider.GetConnectionAsync();

            // Check if party name already exists
            var existingParty = await db.Parties
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == request.PartyName);

            if (existingParty != null)
                return BadRequest(new { error = $"A party with the name '{request.PartyName}' already exists" });

            // Create empty party first
            var newParty = new Party
            {
                UserId = userId,
                Name = request.PartyName,
                IsCurrentParty = false,
                Quick = false,
                Slot1 = null,
                Slot2 = null,
                Slot3 = null,
                Slot4 = null,
                Slot5 = null,
                Slot6 = null
            };

            await db.InsertAsync(newParty);

            // Add each Pokemon to their specified slots
            var errors = new List<string>();
            var successes = new List<string>();

            foreach (var slot in request.PokemonSlots)
            {
                var result = await _partyService.AddPokemonToPartySlot(userId, slot.SlotNumber, (int)slot.PokemonPosition, request.PartyName);
                
                if (result.Success)
                {
                    successes.Add($"Slot {slot.SlotNumber}: {result.Message}");
                }
                else
                {
                    errors.Add($"Slot {slot.SlotNumber}: {result.Message}");
                }
            }

            if (errors.Any())
            {
                // If there were errors, clean up the party and return the errors
                await db.Parties.Where(p => p.UserId == userId && p.Name == request.PartyName).DeleteAsync();
                return BadRequest(new { error = "Failed to create party", details = errors });
            }

            return Ok(new { 
                success = true, 
                message = $"Successfully created custom party '{request.PartyName}' with {successes.Count} Pokemon",
                details = successes
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating custom party for user {UserId}", GetCurrentUserId());
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
            
            // Get current party from Parties table
            var currentParty = await db.Parties
                .Where(p => p.UserId == userId && p.IsCurrentParty)
                .FirstOrDefaultAsync();

            if (currentParty == null)
                return NotFound(new { error = "User party not found" });

            // Get current slot values
            var slots = new[] { currentParty.Slot1, currentParty.Slot2, currentParty.Slot3, 
                               currentParty.Slot4, currentParty.Slot5, currentParty.Slot6 };

            var pokemon1 = slots[request.Slot1 - 1];
            var pokemon2 = slots[request.Slot2 - 1];

            // Perform the swap using individual slot updates
            switch (request.Slot1)
            {
                case 1:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot1, pokemon2).UpdateAsync();
                    break;
                case 2:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot2, pokemon2).UpdateAsync();
                    break;
                case 3:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot3, pokemon2).UpdateAsync();
                    break;
                case 4:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot4, pokemon2).UpdateAsync();
                    break;
                case 5:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot5, pokemon2).UpdateAsync();
                    break;
                case 6:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot6, pokemon2).UpdateAsync();
                    break;
            }

            switch (request.Slot2)
            {
                case 1:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot1, pokemon1).UpdateAsync();
                    break;
                case 2:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot2, pokemon1).UpdateAsync();
                    break;
                case 3:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot3, pokemon1).UpdateAsync();
                    break;
                case 4:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot4, pokemon1).UpdateAsync();
                    break;
                case 5:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot5, pokemon1).UpdateAsync();
                    break;
                case 6:
                    await db.Parties.Where(p => p.PartyId == currentParty.PartyId).Set(p => p.Slot6, pokemon1).UpdateAsync();
                    break;
            }

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

    /// <summary>
    ///     Request model for creating a custom party.
    /// </summary>
    public class CreateCustomPartyRequest
    {
        /// <summary>Gets or sets the name for the custom party.</summary>
        public string PartyName { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the Pokemon slots to add to the party.</summary>
        public List<PokemonSlot> PokemonSlots { get; set; } = new();
    }

    /// <summary>
    ///     Represents a Pokemon slot assignment for custom party creation.
    /// </summary>
    public class PokemonSlot
    {
        /// <summary>Gets or sets the party slot number (1-6).</summary>
        public int SlotNumber { get; set; }
        
        /// <summary>Gets or sets the Pokemon position in the user's collection.</summary>
        public ulong PokemonPosition { get; set; }
    }

    #endregion
}