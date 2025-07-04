using LinqToDB;
using System.Text;
using System.Text.Json;
using EeveeCore.Database.Linq.Models.Bot;
using EeveeCore.Modules.Pokemon.Services;
using EeveeCore.Modules.Spawn.Constants;
using EeveeCore.Services.Helpers;
using EeveeCore.Services.Impl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for user-specific operations and data retrieval.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class UserController : ControllerBase
{
    private readonly LinqToDbConnectionProvider _dbProvider;
    private readonly DiscordShardedClient _client;
    private readonly IMongoService _mongoService;
    private readonly PokemonService _pokemonService;
    private readonly FilterGroupService _filterGroupService;
    private readonly RedisCache _redisCache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserController" /> class.
    /// </summary>
    /// <param name="dbProvider">The database connection provider.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="mongoService">The MongoDB service.</param>
    /// <param name="pokemonService">The Pokemon service.</param>
    /// <param name="filterGroupService">The filter group service.</param>
    /// <param name="redisCache">The Redis cache service.</param>
    public UserController(LinqToDbConnectionProvider dbProvider, DiscordShardedClient client, IMongoService mongoService,
        PokemonService pokemonService, FilterGroupService filterGroupService, RedisCache redisCache)
    {
        _dbProvider = dbProvider;
        _client = client;
        _mongoService = mongoService;
        _pokemonService = pokemonService;
        _filterGroupService = filterGroupService;
        _redisCache = redisCache;
    }

    /// <summary>
    ///     Gets the current user's profile and game statistics.
    /// </summary>
    /// <returns>The user's profile information including game statistics.</returns>
    [HttpGet("profile")]
    public async Task<ActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                Log.Warning("User {UserId} not found in database", userId);
                return NotFound(new { error = "User not found", message = "Please use /start in Discord first" });
            }

            // Get Pokemon count from ownership table
            var pokemonCount = await db.UserPokemonOwnerships.CountAsync(o => o.UserId == userId);

            // Get selected Pokemon details with image path
            object selectedPokemon = null;
            if (user.Selected.HasValue && user.Selected.Value > 0)
            {
                // First verify ownership, then get Pokemon details
                var selectedPokemonData = await (from ownership in db.UserPokemonOwnerships
                    join pokemon1 in db.UserPokemon on ownership.PokemonId equals pokemon1.Id
                    where ownership.UserId == userId && pokemon1.Id == user.Selected.Value
                    select pokemon1).FirstOrDefaultAsync();

                var pokemon = selectedPokemonData;

                if (pokemon != null)
                {
                    // Read all Forms data for mapping (same as in GetPokemon)
                    var allForms = await _mongoService.Forms
                        .Find(_ => true)
                        .ToListAsync();

                    var formsLookup = allForms.ToDictionary(f => f.Identifier.ToLower(), f => f);

                    var pokemonName = pokemon.PokemonName.ToLower();
                    var pokemonId = 0;
                    var formId = 0;
                    var imagePath = "/images/regular/133-0-.png"; // default fallback

                    // Find form info using same logic as GetPokemon
                    if (formsLookup.TryGetValue(pokemonName, out var identifier))
                    {
                        var suffix = identifier.FormIdentifier;

                        if (!string.IsNullOrEmpty(suffix) && pokemonName.EndsWith(suffix))
                        {
                            formId = (int)(identifier.FormOrder - 1)!;
                            var formName = pokemonName[..^(suffix.Length + 1)];

                            if (formsLookup.TryGetValue(formName, out var pokemonIdentifier))
                                pokemonId = pokemonIdentifier.PokemonId;
                        }
                        else
                        {
                            pokemonId = identifier.PokemonId;
                        }

                        // Build image path
                        var pathSegments = new List<string> { "/images", "regular" };

                        if (pokemon.Radiant == true) pathSegments.Add("radiant");
                        if (pokemon.Shiny == true) pathSegments.Add("shiny");
                        if (!string.IsNullOrEmpty(pokemon.Skin) && pokemon.Skin != "None" && pokemon.Skin != "NULL")
                            pathSegments.Add(pokemon.Skin.TrimEnd('/'));

                        // Handle file type (png vs gif)
                        var fileType = "png";
                        if (!string.IsNullOrEmpty(pokemon.Skin) && pokemon.Skin.EndsWith("_gif")) fileType = "gif";

                        var fileName = $"{pokemonId}-{formId}-.{fileType}";
                        pathSegments.Add(fileName);

                        imagePath = string.Join("/", pathSegments);
                    }

                    selectedPokemon = new
                    {
                        pokemon.PokemonName,
                        pokemon.Level,
                        pokemon.Shiny,
                        Nickname = pokemon.Nickname == "None" ? null : pokemon.Nickname,
                        pokemon.Nature,
                        pokemon.HeldItem,
                        pokemon.Radiant,
                        pokemon.Champion,
                        DexNumber = pokemonId,
                        ImagePath = imagePath
                    };
                }
            }

            var tokens = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Tokens ?? "{}");
            var totalTokens = tokens?.Values.Sum() ?? 0;

            var profile = new
            {
                UserId = user.UserId.GetValueOrDefault().ToString(),
                Username = User.FindFirst("Username")?.Value,
                AvatarUrl = (await _client.GetUserAsync(userId, CacheMode.AllowDownload, RequestOptions.Default))
                    .GetAvatarUrl(),
                user.TrainerNickname,
                Credits = user.MewCoins, // Credits are stored as MewCoins
                user.Redeems,
                user.Region,
                user.Staff,
                PokemonCount = pokemonCount,
                TotalTokens = totalTokens,
                user.VoteStreak,
                PatreonTier = user.Patreon ?? "None",
                SelectedPokemon = selectedPokemon,
                user.LastVote,
                user.MarketLimit,
                user.Hunt,
                user.Chain,
                user.EvPoints,
                user.UpvotePoints,
                user.Level,
                user.Energy,
                user.FishingLevel,
                user.FishingExp,
                MysteryTokens = user.MysteryToken,
                user.SkinTokens,
                user.VipTokens
            };

            return Ok(profile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user profile");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }


     /// <summary>
    ///     Gets the current user's Pokemon collection with advanced filtering, sorting, and search.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of Pokemon per page (default: 20, max: 100).</param>
    /// <param name="search">Optional search term to filter Pokemon names, nicknames, moves, or tags.</param>
    /// <param name="shinyOnly">If true, only return shiny Pokemon (legacy parameter, use filter=shiny instead).</param>
    /// <param name="includeStats">If true, include detailed IV/EV stats.</param>
    /// <param name="sortBy">Sort method: default, iv, level, name, recent, type, favorite, party, champion.</param>
    /// <param name="filter">Filter: all, shiny, radiant, shadow, legendary, favorite, champion, party, market.</param>
    /// <param name="gender">Gender filter: all, male, female, genderless.</param>
    /// <returns>A filtered and sorted list of the user's Pokemon with pagination and statistics.</returns>
    [HttpGet("pokemon")]
    public async Task<ActionResult> GetPokemon(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool shinyOnly = false,
        [FromQuery] bool includeStats = false,
        [FromQuery] string sortBy = "default",
        [FromQuery] string filter = "all",
        [FromQuery] string gender = "all")
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            pageSize = Math.Min(pageSize, 100);

            // Handle legacy shinyOnly parameter
            if (shinyOnly && filter == "all") filter = "shiny";

            return await GetFilteredPokemonInternal(userId, page, pageSize, search, sortBy, filter, gender, includeStats, null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user Pokemon with filters");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }


    /// <summary>
    ///     Gets detailed information about a specific Pokemon.
    /// </summary>
    /// <param name="pokemonId">The ID of the Pokemon to retrieve.</param>
    /// <returns>Detailed Pokemon information including full stats.</returns>
    [HttpGet("pokemon/{pokemonId}")]
    public async Task<ActionResult> GetPokemonDetails(ulong pokemonId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();

            // Verify user owns this Pokemon through ownership table
            var pokemonOwnership = await db.UserPokemonOwnerships
                .FirstOrDefaultAsync(o => o.UserId == userId && o.PokemonId == pokemonId);

            if (pokemonOwnership == null) return NotFound(new { error = "Pokemon not found or not owned by user" });

            // Get Pokemon details through ownership join (ownership already verified above)
            var pokemon = await (from ownership in db.UserPokemonOwnerships
                join p in db.UserPokemon on ownership.PokemonId equals p.Id
                where ownership.UserId == userId && p.Id == pokemonId
                select new
                {
                    p.Id,
                    p.PokemonName,
                    p.Nickname,
                    p.Level,
                    p.Shiny,
                    p.Radiant,
                    p.Nature,
                    p.HeldItem,
                    p.Favorite,
                    p.Champion,
                    p.Gender,
                    p.AbilityIndex,
                    Position = ownership.Position + 1,
                    CaughtAt = p.Timestamp,
                    p.CaughtBy,
                    p.Moves,
                    p.Tags,
                    p.Skin,
                    p.Tradable,
                    p.Breedable,
                    p.MarketEnlist,
                    p.Price,
                    p.Voucher,
                    p.Temporary,
                    p.Counter,
                    IVs = new
                    {
                        HP = p.HpIv,
                        Attack = p.AttackIv,
                        Defense = p.DefenseIv,
                        SpecialAttack = p.SpecialAttackIv,
                        SpecialDefense = p.SpecialDefenseIv,
                        Speed = p.SpeedIv,
                        Total = p.HpIv + p.AttackIv + p.DefenseIv + p.SpecialAttackIv + p.SpecialDefenseIv + p.SpeedIv
                    },
                    EVs = new
                    {
                        HP = p.HpEv,
                        Attack = p.AttackEv,
                        Defense = p.DefenseEv,
                        SpecialAttack = p.SpecialAttackEv,
                        SpecialDefense = p.SpecialDefenseEv,
                        Speed = p.SpeedEv,
                        Total = p.HpEv + p.AttackEv + p.DefenseEv + p.SpecialAttackEv + p.SpecialDefenseEv + p.SpeedEv
                    },
                    p.Experience,
                    p.ExperienceCap,
                    p.Happiness
                }).FirstOrDefaultAsync();

            if (pokemon == null) return NotFound(new { error = "Pokemon not found" });

            return Ok(pokemon);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting Pokemon details for ID {PokemonId}", pokemonId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets a filtered list of Pokemon using custom filter criteria.
    /// </summary>
    /// <param name="request">The filter request containing criteria.</param>
    /// <param name="page">Page number for pagination.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="search">Search term.</param>
    /// <param name="sortBy">Sort order.</param>
    /// <param name="filter">Additional filter.</param>
    /// <param name="gender">Gender filter.</param>
    /// <param name="includeStats">Whether to include detailed stats.</param>
    /// <returns>Filtered Pokemon list matching the criteria.</returns>
    [HttpPost("pokemon/filtered")]
    public async Task<ActionResult> GetFilteredPokemon(
        [FromBody] CustomFilterRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "default",
        [FromQuery] string filter = "all",
        [FromQuery] string gender = "all",
        [FromQuery] bool includeStats = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            // Generate cache key based on all parameters including cache version
            var cacheKey = await GenerateFilterCacheKeyWithVersion(userId, request?.Criteria, page, pageSize, search, sortBy, filter, gender, includeStats);
            
            // Try to get cached result
            var cachedResult = await _redisCache.GetFromCache<object>(cacheKey);
            if (cachedResult != null)
            {
                Log.Debug("Returning cached filter results for user {UserId}", userId);
                return Ok(cachedResult);
            }

            var result = await GetFilteredPokemonInternal(userId, page, pageSize, search, sortBy, filter, gender, includeStats, request?.Criteria);
            
            // Cache the result for 5 minutes (filter results change less frequently than individual Pokemon)
            if (result is OkObjectResult okResult)
            {
                await _redisCache.AddToCache(cacheKey, okResult.Value, TimeSpan.FromMinutes(5));
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting filtered Pokemon for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Internal method to get filtered Pokemon using LinqToDB advanced features for optimal performance.
    /// </summary>
    private async Task<ActionResult> GetFilteredPokemonInternal(
        ulong userId, 
        int page, 
        int pageSize, 
        string? search, 
        string sortBy, 
        string filter, 
        string gender, 
        bool includeStats,
        List<UserFilterCriteria>? customCriteria)
    {
        await using var db = await _dbProvider.GetConnectionAsync();

        // Use single query to get user data and counts efficiently
        var userDataQuery = from u in db.Users
                           where u.UserId == userId
                           select new { u.Party, u.Selected };

        var userData = await userDataQuery.FirstOrDefaultAsync();
        if (userData == null) return NotFound(new { error = "User not found" });

        // Create party lookup set
        var partyLookup = userData.Party?.Where(id => id != 0).Select(id => (ulong)id).ToHashSet() ?? new HashSet<ulong>();

        // Build base query with simple joins
        var baseQuery = from ownership in db.UserPokemonOwnerships
                       join pokemon in db.UserPokemon on ownership.PokemonId equals pokemon.Id
                       where ownership.UserId == userId
                       select new { Pokemon = pokemon, ownership.Position };

        // Get total count before filters for pagination
        var totalCount = await baseQuery.CountAsync();

        // Apply filters using LinqToDB's efficient where clauses
        var filteredQuery = baseQuery;
        
        // Apply standard filters with optimized predicates
        filteredQuery = filter switch
        {
            "shiny" => filteredQuery.Where(p => p.Pokemon.Shiny == true),
            "radiant" => filteredQuery.Where(p => p.Pokemon.Radiant == true),
            "shadow" => filteredQuery.Where(p => p.Pokemon.Skin == "shadow"),
            "legendary" => filteredQuery.Where(p => PokemonList.LegendList.Contains(p.Pokemon.PokemonName)),
            "favorite" => filteredQuery.Where(p => p.Pokemon.Favorite),
            "champion" => filteredQuery.Where(p => p.Pokemon.Champion),
            "party" => filteredQuery.Where(p => partyLookup.Contains(p.Pokemon.Id)),
            "market" => filteredQuery.Where(p => p.Pokemon.MarketEnlist),
            _ => filteredQuery
        };

        // Apply gender filter with database-optimized comparisons
        filteredQuery = gender switch
        {
            "male" => filteredQuery.Where(p => p.Pokemon.Gender == "-m"),
            "female" => filteredQuery.Where(p => p.Pokemon.Gender == "-f"),
            "genderless" => filteredQuery.Where(p => p.Pokemon.Gender == "-x"),
            _ => filteredQuery
        };

        // Apply custom criteria filters with optimized database operations
        if (customCriteria != null && customCriteria.Any())
        {
            foreach (var criterion in customCriteria.OrderBy(c => c.CriterionOrder))
            {
                switch (criterion.FieldName.ToLower())
                {
                    case "level":
                        var levelValue = criterion.ValueNumeric ?? 0;
                        filteredQuery = criterion.Operator.ToLower() switch
                        {
                            "greater_than" => filteredQuery.Where(p => p.Pokemon.Level > levelValue),
                            "less_than" => filteredQuery.Where(p => p.Pokemon.Level < levelValue),
                            "equals" => filteredQuery.Where(p => p.Pokemon.Level == levelValue),
                            "greater_equal" => filteredQuery.Where(p => p.Pokemon.Level >= levelValue),
                            "less_equal" => filteredQuery.Where(p => p.Pokemon.Level <= levelValue),
                            "between" => filteredQuery.Where(p => p.Pokemon.Level >= levelValue && p.Pokemon.Level <= (criterion.ValueNumericMax ?? levelValue)),
                            _ => filteredQuery
                        };
                        break;
                        
                    case "shiny":
                        filteredQuery = filteredQuery.Where(p => p.Pokemon.Shiny == (criterion.ValueBoolean ?? true));
                        break;
                        
                    case "radiant":
                        filteredQuery = filteredQuery.Where(p => p.Pokemon.Radiant == (criterion.ValueBoolean ?? true));
                        break;
                        
                    case "favorite":
                        filteredQuery = filteredQuery.Where(p => p.Pokemon.Favorite == (criterion.ValueBoolean ?? true));
                        break;
                        
                    case "champion":
                        filteredQuery = filteredQuery.Where(p => p.Pokemon.Champion == (criterion.ValueBoolean ?? true));
                        break;
                        
                    case "pokemon_name":
                        var nameValue = criterion.ValueText ?? "";
                        filteredQuery = criterion.Operator.ToLower() switch
                        {
                            "contains" => filteredQuery.Where(p => p.Pokemon.PokemonName.Contains(nameValue)),
                            "equals" => filteredQuery.Where(p => p.Pokemon.PokemonName == nameValue),
                            "not_contains" => filteredQuery.Where(p => !p.Pokemon.PokemonName.Contains(nameValue)),
                            _ => filteredQuery
                        };
                        break;
                        
                    case "iv_total":
                        var ivValue = criterion.ValueNumeric ?? 0;
                        var ivMaxValue = criterion.ValueNumericMax ?? ivValue;
                        filteredQuery = criterion.Operator.ToLower() switch
                        {
                            "greater_than" => filteredQuery.Where(p =>
                                (p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                                 p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv) > ivValue),
                            "less_than" => filteredQuery.Where(p =>
                                (p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                                 p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv) < ivValue),
                            "equals" => filteredQuery.Where(p =>
                                (p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                                 p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv) == ivValue),
                            "greater_equal" => filteredQuery.Where(p =>
                                (p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                                 p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv) >= ivValue),
                            "less_equal" => filteredQuery.Where(p =>
                                (p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                                 p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv) <= ivValue),
                            "between" => filteredQuery.Where(p =>
                                (p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                                 p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv) >= ivValue &&
                                (p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                                 p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv) <= ivMaxValue),
                            _ => filteredQuery
                        };
                        break;
                        
                    case "nature":
                        filteredQuery = filteredQuery.Where(p => p.Pokemon.Nature == criterion.ValueText);
                        break;
                }
            }
        }

        // Use sequential count queries to avoid connection pool exhaustion
        var filteredCount = await filteredQuery.CountAsync();
        
        // Execute stat queries sequentially to avoid overwhelming the connection pool
        var shinyCount = await filteredQuery.CountAsync(p => p.Pokemon.Shiny == true);
        var radiantCount = await filteredQuery.CountAsync(p => p.Pokemon.Radiant == true);
        var shadowCount = await filteredQuery.CountAsync(p => p.Pokemon.Skin == "shadow");
        var favoriteCount = await filteredQuery.CountAsync(p => p.Pokemon.Favorite);
        var championCount = await filteredQuery.CountAsync(p => p.Pokemon.Champion);
        var marketCount = await filteredQuery.CountAsync(p => p.Pokemon.MarketEnlist);
        var maleCount = await filteredQuery.CountAsync(p => p.Pokemon.Gender == "-m");
        var femaleCount = await filteredQuery.CountAsync(p => p.Pokemon.Gender == "-f");
        var genderlessCount = await filteredQuery.CountAsync(p => p.Pokemon.Gender == "-x");
        
        // Get distinct Pokemon names for legendary count
        var distinctPokemonNames = await filteredQuery
            .Select(p => p.Pokemon.PokemonName)
            .Distinct()
            .ToListAsync();
        var legendaryCount = distinctPokemonNames.Count(name => PokemonList.LegendList.Contains(name));

        var stats = new Dictionary<string, int>
        {
            { "TotalCount", totalCount },
            { "Total", filteredCount },
            { "Shiny", shinyCount },
            { "Radiant", radiantCount },
            { "Shadow", shadowCount },
            { "Legendary", legendaryCount },
            { "Favorite", favoriteCount },
            { "Champion", championCount },
            { "Market", marketCount },
            { "Male", maleCount },
            { "Female", femaleCount },
            { "Genderless", genderlessCount }
        };

        // Apply search filter at database level before sorting and pagination
        if (!string.IsNullOrEmpty(search))
        {
            filteredQuery = filteredQuery.Where(p => 
                p.Pokemon.PokemonName.Contains(search) || 
                (p.Pokemon.Nickname != null && p.Pokemon.Nickname.Contains(search)));
        }

        // Apply optimized sorting with database-level operations
        var sortedQuery = sortBy.ToLower() switch
        {
            "iv" => filteredQuery.OrderByDescending(p =>
                p.Pokemon.HpIv + p.Pokemon.AttackIv + p.Pokemon.DefenseIv +
                p.Pokemon.SpecialAttackIv + p.Pokemon.SpecialDefenseIv + p.Pokemon.SpeedIv),
            "level" => filteredQuery.OrderByDescending(p => p.Pokemon.Level),
            "name" => filteredQuery.OrderBy(p => p.Pokemon.PokemonName),
            "recent" => filteredQuery.OrderByDescending(p => p.Pokemon.Timestamp),
            "favorite" => filteredQuery.OrderByDescending(p => p.Pokemon.Favorite),
            "party" => filteredQuery.OrderByDescending(p => partyLookup.Contains(p.Pokemon.Id)),
            "champion" => filteredQuery.OrderByDescending(p => p.Pokemon.Champion),
            _ => filteredQuery.OrderBy(p => p.Position)
        };

        // Apply pagination with optimized Skip/Take
        var skip = (page - 1) * pageSize;
        var pokemonData = await sortedQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        // Read all Forms data once for mapping
        var allForms = await _mongoService.Forms
            .Find(_ => true)
            .ToListAsync();

        var formsLookup = allForms.ToDictionary(f => f.Identifier.ToLower(), f => f);

        // Process each Pokemon to include form data and image paths
        var processedPokemon = pokemonData.Select(item =>
        {
            var pokemonName = item.Pokemon.PokemonName.ToLower();
            var pokemonId = 0;
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
                        pokemonId = pokemonIdentifier.PokemonId;
                }
                else
                {
                    pokemonId = identifier.PokemonId;
                }

                // Build image path
                var pathSegments = new List<string> { "/images", "regular" };

                if (item.Pokemon.Radiant == true) pathSegments.Add("radiant");
                if (item.Pokemon.Shiny == true) pathSegments.Add("shiny");
                if (!string.IsNullOrEmpty(item.Pokemon.Skin) && item.Pokemon.Skin != "None" && item.Pokemon.Skin != "NULL")
                    pathSegments.Add(item.Pokemon.Skin.TrimEnd('/'));

                var fileType = "png";
                if (!string.IsNullOrEmpty(item.Pokemon.Skin) && item.Pokemon.Skin.EndsWith("_gif"))
                    fileType = "gif";

                var fileName = $"{pokemonId}-{formId}-.{fileType}";
                pathSegments.Add(fileName);

                imagePath = string.Join("/", pathSegments);
            }

            var ivTotal = item.Pokemon.HpIv + item.Pokemon.AttackIv + item.Pokemon.DefenseIv +
                         item.Pokemon.SpecialAttackIv + item.Pokemon.SpecialDefenseIv + item.Pokemon.SpeedIv;

            return new
            {
                Id = item.Pokemon.Id,
                PokemonName = item.Pokemon.PokemonName,
                Nickname = item.Pokemon.Nickname == "None" ? null : item.Pokemon.Nickname,
                item.Pokemon.Level,
                item.Pokemon.Shiny,
                item.Pokemon.Radiant,
                item.Pokemon.Nature,
                item.Pokemon.HeldItem,
                item.Pokemon.Favorite,
                item.Pokemon.Champion,
                item.Pokemon.Gender,
                item.Pokemon.AbilityIndex,
                Position = item.Position + 1,
                CaughtAt = item.Pokemon.Timestamp,
                item.Pokemon.CaughtBy,
                item.Pokemon.Moves,
                item.Pokemon.Tags,
                item.Pokemon.Skin,
                item.Pokemon.Tradable,
                item.Pokemon.Breedable,
                item.Pokemon.MarketEnlist,
                item.Pokemon.Price,
                DexNumber = pokemonId,
                FormId = formId,
                ImagePath = imagePath,
                InParty = partyLookup.Contains(item.Pokemon.Id),
                IsSelected = item.Pokemon.Id == userData.Selected,
                IvPercentage = (ivTotal / 186.0) * 100,
                Stats = includeStats
                    ? new
                    {
                        IVs = new
                        {
                            HP = item.Pokemon.HpIv,
                            Attack = item.Pokemon.AttackIv,
                            Defense = item.Pokemon.DefenseIv,
                            SpecialAttack = item.Pokemon.SpecialAttackIv,
                            SpecialDefense = item.Pokemon.SpecialDefenseIv,
                            Speed = item.Pokemon.SpeedIv
                        },
                        EVs = new
                        {
                            HP = item.Pokemon.HpEv,
                            Attack = item.Pokemon.AttackEv,
                            Defense = item.Pokemon.DefenseEv,
                            SpecialAttack = item.Pokemon.SpecialAttackEv,
                            SpecialDefense = item.Pokemon.SpecialDefenseEv,
                            Speed = item.Pokemon.SpeedEv
                        },
                        item.Pokemon.Experience,
                        item.Pokemon.ExperienceCap,
                        item.Pokemon.Happiness
                    }
                    : null
            };
        }).ToList();

        var response = new
        {
            Pokemon = processedPokemon,
            Pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = stats["Total"],
                TotalPages = (int)Math.Ceiling((double)stats["Total"] / pageSize)
            },
            Stats = stats,
            Filters = new
            {
                SortBy = sortBy,
                Filter = filter,
                Gender = gender,
                Search = search
            }
        };

        return Ok(response);
    }


    /// <summary>
    ///     Gets the current user's inventory items.
    /// </summary>
    /// <returns>The user's inventory with item counts.</returns>
    [HttpGet("inventory")]
    public async Task<ActionResult> GetInventory()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.Inventory, u.Items, u.Skins, u.HolidayInventory })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { error = "User not found" });

            var response = new
            {
                GeneralInventory = InventoryHelper.SafeDeserializeInventory(user.Inventory, "user-profile"),
                BattleItems = InventoryHelper.SafeDeserializeInventory(user.Items, "user-profile"),
                Skins = InventoryHelper.SafeDeserializeInventory(user.Skins, "user-profile"),
                HolidayItems = InventoryHelper.SafeDeserializeInventory(user.HolidayInventory, "user-profile")
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user inventory");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the current user's radiant tokens.
    /// </summary>
    /// <returns>The user's token counts by type.</returns>
    [HttpGet("tokens")]
    public async Task<ActionResult> GetTokens()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Tokens)
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { error = "User not found" });

            var tokens = JsonSerializer.Deserialize<Dictionary<string, int>>(user);
            var totalTokens = tokens?.Values.Sum() ?? 0;

            var response = new
            {
                Tokens = tokens ?? new Dictionary<string, int>(),
                TotalTokens = totalTokens
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user tokens");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the current user's recent trade history.
    /// </summary>
    /// <param name="limit">The maximum number of trades to return (default: 20, max: 100).</param>
    /// <returns>The user's recent trade history.</returns>
    [HttpGet("trades")]
    public async Task<ActionResult> GetTradeHistory([FromQuery] int limit = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            limit = Math.Min(limit, 100);

            await using var db = await _dbProvider.GetConnectionAsync();
            var trades = await db.TradeLogs
                .Where(t => t.SenderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.Time)
                .Take(limit)
                .Select(t => new
                {
                    t.TradeId,
                    t.SenderId,
                    t.ReceiverId,
                    t.SenderRedeems,
                    t.ReceiverRedeems,
                    t.SenderCredits,
                    t.ReceiverCredits,
                    t.Command,
                    t.Time,
                    IsOutgoing = t.SenderId == userId
                })
                .ToListAsync();

            return Ok(new { Trades = trades });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user trade history");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the current user's egg hatchery status.
    /// </summary>
    /// <returns>The user's egg hatchery information including active eggs and slots.</returns>
    [HttpGet("hatchery")]
    public async Task<ActionResult> GetHatcheryStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var hatcheries = await db.EggHatcheries
                .Where(h => h.UserId == userId)
                .Select(h => new
                {
                    h.Group,
                    h.Slot1,
                    h.Slot2,
                    h.Slot3,
                    h.Slot4,
                    h.Slot5,
                    h.Slot6,
                    h.Slot7,
                    h.Slot8,
                    h.Slot9,
                    h.Slot10
                })
                .ToListAsync();

            // Get egg details for occupied slots with correct field names
            var allEggIds = hatcheries
                .SelectMany(h => new[]
                    { h.Slot1, h.Slot2, h.Slot3, h.Slot4, h.Slot5, h.Slot6, h.Slot7, h.Slot8, h.Slot9, h.Slot10 })
                .Where(id => id is > 0)
                .Select(id => id!.Value)
                .ToList();

            // Get egg details through ownership join to ensure user owns them
            var eggs = await (from ownership in db.UserPokemonOwnerships
                join p in db.UserPokemon on ownership.PokemonId equals p.Id
                where ownership.UserId == userId && allEggIds.Contains(p.Id)
                select new
                {
                    p.Id,
                    p.PokemonName,
                    p.Timestamp, // Correct field name
                    p.Nickname
                }).ToListAsync();

            var eggDict = eggs.ToDictionary(e => e.Id, e => (e.Id, e.PokemonName, e.Timestamp, e.Nickname));

            var hatcheryData = hatcheries.Select(h => new
            {
                h.Group,
                Slots = new[]
                {
                    CreateSlotInfo(h.Slot1, eggDict),
                    CreateSlotInfo(h.Slot2, eggDict),
                    CreateSlotInfo(h.Slot3, eggDict),
                    CreateSlotInfo(h.Slot4, eggDict),
                    CreateSlotInfo(h.Slot5, eggDict),
                    CreateSlotInfo(h.Slot6, eggDict),
                    CreateSlotInfo(h.Slot7, eggDict),
                    CreateSlotInfo(h.Slot8, eggDict),
                    CreateSlotInfo(h.Slot9, eggDict),
                    CreateSlotInfo(h.Slot10, eggDict)
                }
            }).ToList();

            return Ok(new { Hatcheries = hatcheryData });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting hatchery status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the current user ID from the JWT claims.
    /// </summary>
    /// <returns>The user ID as a ulong, or 0 if not found or invalid.</returns>
    private ulong GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        return ulong.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    ///     Creates slot information for a hatchery slot with correct field names.
    /// </summary>
    /// <param name="eggId">The egg ID in the slot, or null if empty.</param>
    /// <param name="eggDict">Dictionary of egg information keyed by egg ID.</param>
    /// <returns>Slot information object.</returns>
    private static object CreateSlotInfo(ulong? eggId,
        Dictionary<ulong, (ulong Id, string? PokemonName, DateTime? Timestamp, string Nickname)> eggDict)
    {
        if (!eggId.HasValue || eggId.Value == 0) return new { IsEmpty = true };

        if (eggDict.TryGetValue(eggId.Value, out var egg))
            return new
            {
                IsEmpty = false,
                EggId = eggId.Value,
                egg.PokemonName,
                egg.Timestamp,
                egg.Nickname
            };

        return new { IsEmpty = true };
    }

    #region Filter Group Endpoints

    /// <summary>
    ///     Gets all filter groups for the current user.
    /// </summary>
    /// <returns>List of user's filter groups with their criteria.</returns>
    [HttpGet("pokemon/filter-groups")]
    public async Task<ActionResult> GetFilterGroups()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var filterGroups = await _filterGroupService.GetUserFilterGroups(userId);

            return Ok(new
            {
                success = true,
                data = filterGroups.Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    g.Color,
                    g.Icon,
                    g.IsFavorite,
                    g.SortOrder,
                    g.CreatedAt,
                    g.UpdatedAt,
                    Criteria = g.FilterCriteria.Select(c => new
                    {
                        c.Id,
                        c.FieldName,
                        c.Operator,
                        c.ValueText,
                        c.ValueNumeric,
                        c.ValueNumericMax,
                        c.ValueBoolean,
                        c.LogicalConnector,
                        c.CriterionOrder
                    }).ToList()
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting filter groups for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets a specific filter group by ID.
    /// </summary>
    /// <param name="groupId">The filter group ID.</param>
    /// <returns>The filter group details.</returns>
    [HttpGet("pokemon/filter-groups/{groupId}")]
    public async Task<ActionResult> GetFilterGroup(int groupId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var filterGroup = await _filterGroupService.GetFilterGroup(groupId, userId);
            if (filterGroup == null)
                return NotFound(new { error = "Filter group not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    filterGroup.Id,
                    filterGroup.Name,
                    filterGroup.Description,
                    filterGroup.Color,
                    filterGroup.Icon,
                    filterGroup.IsFavorite,
                    filterGroup.SortOrder,
                    filterGroup.CreatedAt,
                    filterGroup.UpdatedAt,
                    Criteria = filterGroup.FilterCriteria.Select(c => new
                    {
                        c.Id,
                        c.FieldName,
                        c.Operator,
                        c.ValueText,
                        c.ValueNumeric,
                        c.ValueNumericMax,
                        c.ValueBoolean,
                        c.LogicalConnector,
                        c.CriterionOrder
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting filter group {GroupId} for user {UserId}", groupId, GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Creates a new filter group.
    /// </summary>
    /// <param name="request">The filter group creation request.</param>
    /// <returns>The created filter group.</returns>
    [HttpPost("pokemon/filter-groups")]
    public async Task<ActionResult> CreateFilterGroup([FromBody] CreateFilterGroupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Name is required" });

            var criteria = request.Criteria?.Select(c => new UserFilterCriteria
            {
                FieldName = c.FieldName,
                Operator = c.Operator,
                ValueText = c.ValueText,
                ValueNumeric = c.ValueNumeric,
                ValueNumericMax = c.ValueNumericMax,
                ValueBoolean = c.ValueBoolean,
                LogicalConnector = c.LogicalConnector,
                CriterionOrder = c.CriterionOrder
            }).ToList();

            var filterGroup = await _filterGroupService.CreateFilterGroup(
                userId,
                request.Name,
                request.Description,
                request.Color,
                request.Icon,
                criteria);

            if (filterGroup == null)
                return BadRequest(new { error = "Failed to create filter group. Name may already exist." });

            return Ok(new
            {
                success = true,
                data = new
                {
                    filterGroup.Id,
                    filterGroup.Name,
                    filterGroup.Description,
                    filterGroup.Color,
                    filterGroup.Icon,
                    filterGroup.IsFavorite,
                    filterGroup.SortOrder,
                    filterGroup.CreatedAt,
                    filterGroup.UpdatedAt,
                    Criteria = filterGroup.FilterCriteria.Select(c => new
                    {
                        c.Id,
                        c.FieldName,
                        c.Operator,
                        c.ValueText,
                        c.ValueNumeric,
                        c.ValueNumericMax,
                        c.ValueBoolean,
                        c.LogicalConnector,
                        c.CriterionOrder
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating filter group for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Updates an existing filter group.
    /// </summary>
    /// <param name="groupId">The filter group ID.</param>
    /// <param name="request">The filter group update request.</param>
    /// <returns>Success status.</returns>
    [HttpPut("pokemon/filter-groups/{groupId}")]
    public async Task<ActionResult> UpdateFilterGroup(int groupId, [FromBody] UpdateFilterGroupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var criteria = request.Criteria?.Select(c => new UserFilterCriteria
            {
                FieldName = c.FieldName,
                Operator = c.Operator,
                ValueText = c.ValueText,
                ValueNumeric = c.ValueNumeric,
                ValueNumericMax = c.ValueNumericMax,
                ValueBoolean = c.ValueBoolean,
                LogicalConnector = c.LogicalConnector,
                CriterionOrder = c.CriterionOrder
            }).ToList();

            var success = await _filterGroupService.UpdateFilterGroup(
                groupId,
                userId,
                request.Name,
                request.Description,
                request.Color,
                request.Icon,
                request.IsFavorite,
                criteria);

            if (!success)
                return NotFound(new { error = "Filter group not found or update failed" });

            return Ok(new { success = true, message = "Filter group updated successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating filter group {GroupId} for user {UserId}", groupId, GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Deletes a filter group.
    /// </summary>
    /// <param name="groupId">The filter group ID.</param>
    /// <returns>Success status.</returns>
    [HttpDelete("pokemon/filter-groups/{groupId}")]
    public async Task<ActionResult> DeleteFilterGroup(int groupId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var success = await _filterGroupService.DeleteFilterGroup(groupId, userId);
            if (!success)
                return NotFound(new { error = "Filter group not found" });

            return Ok(new { success = true, message = "Filter group deleted successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting filter group {GroupId} for user {UserId}", groupId, GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Reorders user's filter groups.
    /// </summary>
    /// <param name="request">The reorder request containing new order.</param>
    /// <returns>Success status.</returns>
    [HttpPut("pokemon/filter-groups/reorder")]
    public async Task<ActionResult> ReorderFilterGroups([FromBody] ReorderFilterGroupsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (request.GroupIds == null || !request.GroupIds.Any())
                return BadRequest(new { error = "Group IDs are required" });

            var success = await _filterGroupService.ReorderFilterGroups(userId, request.GroupIds);
            if (!success)
                return BadRequest(new { error = "Invalid group IDs or reorder failed" });

            return Ok(new { success = true, message = "Filter groups reordered successfully" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reordering filter groups for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    #endregion

    #region Request Models

    /// <summary>
    ///     Request model for creating a new filter group.
    /// </summary>
    public class CreateFilterGroupRequest
    {
        /// <summary>Gets or sets the name of the filter group.</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the optional description of the filter group.</summary>
        public string? Description { get; set; }
        
        /// <summary>Gets or sets the color of the filter group.</summary>
        public string? Color { get; set; }
        
        /// <summary>Gets or sets the icon of the filter group.</summary>
        public string? Icon { get; set; }
        
        /// <summary>Gets or sets the filter criteria for the group.</summary>
        public List<FilterCriteriaRequest>? Criteria { get; set; }
    }

    /// <summary>
    ///     Request model for updating an existing filter group.
    /// </summary>
    public class UpdateFilterGroupRequest
    {
        /// <summary>Gets or sets the name of the filter group.</summary>
        public string? Name { get; set; }
        
        /// <summary>Gets or sets the description of the filter group.</summary>
        public string? Description { get; set; }
        
        /// <summary>Gets or sets the color of the filter group.</summary>
        public string? Color { get; set; }
        
        /// <summary>Gets or sets the icon of the filter group.</summary>
        public string? Icon { get; set; }
        
        /// <summary>Gets or sets whether the filter group is marked as favorite.</summary>
        public bool? IsFavorite { get; set; }
        
        /// <summary>Gets or sets the filter criteria for the group.</summary>
        public List<FilterCriteriaRequest>? Criteria { get; set; }
    }

    /// <summary>
    ///     Request model for a single filter criterion.
    /// </summary>
    public class FilterCriteriaRequest
    {
        /// <summary>Gets or sets the field name to filter on.</summary>
        public string FieldName { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the comparison operator.</summary>
        public string Operator { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the text value for comparison.</summary>
        public string? ValueText { get; set; }
        
        /// <summary>Gets or sets the numeric value for comparison.</summary>
        public int? ValueNumeric { get; set; }
        
        /// <summary>Gets or sets the maximum numeric value for range comparisons.</summary>
        public int? ValueNumericMax { get; set; }
        
        /// <summary>Gets or sets the boolean value for comparison.</summary>
        public bool? ValueBoolean { get; set; }
        
        /// <summary>Gets or sets the logical connector to the next criterion.</summary>
        public string? LogicalConnector { get; set; }
        
        /// <summary>Gets or sets the order of this criterion within the group.</summary>
        public int CriterionOrder { get; set; }
    }

    /// <summary>
    ///     Request model for reordering filter groups.
    /// </summary>
    public class ReorderFilterGroupsRequest
    {
        /// <summary>Gets or sets the list of group IDs in the new order.</summary>
        public List<int> GroupIds { get; set; } = new();
    }

    /// <summary>
    ///     Request model for applying custom filter criteria.
    /// </summary>
    public class CustomFilterRequest
    {
        /// <summary>Gets or sets the list of filter criteria to apply.</summary>
        public List<UserFilterCriteria> Criteria { get; set; } = new();
    }

    #endregion

    #region Cache Management

    /// <summary>
    ///     Generates a cache key for filtered Pokemon results based on all parameters including cache version.
    /// </summary>
    private async Task<string> GenerateFilterCacheKeyWithVersion(ulong userId, List<UserFilterCriteria>? criteria, int page, int pageSize, 
        string? search, string sortBy, string filter, string gender, bool includeStats)
    {
        // Get the current cache version for this user
        var versionKey = $"pokemon_filter_version:{userId}";
        var version = await _redisCache.GetFromCache<int>(versionKey);
        
        var keyBuilder = new StringBuilder($"pokemon_filter:v{version}:{userId}:");
        
        // Add basic parameters
        keyBuilder.Append($"p{page}_ps{pageSize}_s{sortBy}_f{filter}_g{gender}_stats{includeStats}");
        
        // Add search term if provided
        if (!string.IsNullOrEmpty(search))
        {
            keyBuilder.Append($"_search:{search.GetHashCode()}");
        }
        
        // Add criteria hash if provided
        if (criteria != null && criteria.Any())
        {
            var criteriaHash = GenerateCriteriaHash(criteria);
            keyBuilder.Append($"_criteria:{criteriaHash}");
        }
        
        return keyBuilder.ToString();
    }

    /// <summary>
    ///     Generates a hash for filter criteria to use in cache keys.
    /// </summary>
    private string GenerateCriteriaHash(List<UserFilterCriteria> criteria)
    {
        var criteriaString = string.Join("|", criteria.OrderBy(c => c.CriterionOrder).Select(c => 
            $"{c.FieldName}:{c.Operator}:{c.ValueText}:{c.ValueNumeric}:{c.ValueNumericMax}:{c.ValueBoolean}:{c.LogicalConnector}"));
        
        return criteriaString.GetHashCode().ToString();
    }

    /// <summary>
    ///     Invalidates cached filter results for a user when their Pokemon collection changes.
    /// </summary>
    private async Task InvalidateUserFilterCache(ulong userId)
    {
        try
        {
            // For now, we'll use a simpler approach and cache a "cache version" that we can increment
            // This is more efficient than pattern-based deletion and works with the current Redis setup
            var versionKey = $"pokemon_filter_version:{userId}";
            var currentVersion = await _redisCache.GetFromCache<int>(versionKey);
            await _redisCache.AddToCache(versionKey, currentVersion + 1, TimeSpan.FromDays(30));
            
            Log.Debug("Invalidated filter cache for user {UserId} by incrementing version to {Version}", userId, currentVersion + 1);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to invalidate filter cache for user {UserId}", userId);
        }
    }

    #endregion
}