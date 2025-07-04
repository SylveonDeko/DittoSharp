using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Market.Services;
using EeveeCore.Modules.Trade.Services;
using EeveeCore.Common.Constants;
using Serilog;
using LinqToDB;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for Pokemon marketplace operations with fraud detection and analytics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class MarketController : ControllerBase
{
    private readonly MarketService _marketService;
    private readonly LinqToDbConnectionProvider _dbProvider;
    private readonly TradeFraudDetectionService _fraudDetectionService;
    private readonly TradeValueCalculator _valueCalculator;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MarketController"/> class.
    /// </summary>
    /// <param name="marketService">The market service.</param>
    /// <param name="dbProvider">The database connection provider.</param>
    /// <param name="fraudDetectionService">The fraud detection service.</param>
    /// <param name="valueCalculator">The trade value calculator.</param>
    public MarketController(
        MarketService marketService, 
        LinqToDbConnectionProvider dbProvider,
        TradeFraudDetectionService fraudDetectionService,
        TradeValueCalculator valueCalculator)
    {
        _marketService = marketService;
        _dbProvider = dbProvider;
        _fraudDetectionService = fraudDetectionService;
        _valueCalculator = valueCalculator;
    }

    /// <summary>
    ///     Gets market listings with filtering, fraud detection, and price analytics.
    /// </summary>
    /// <param name="sortBy">Sort method: name, level, price, recent, popular, risk (default: recent).</param>
    /// <param name="filter">Filter: all, shiny, legendary, radiant, safe, suspicious (default: all).</param>
    /// <param name="search">Search term for Pokemon names.</param>
    /// <param name="minPrice">Minimum price filter.</param>
    /// <param name="maxPrice">Maximum price filter.</param>
    /// <param name="minLevel">Minimum level filter.</param>
    /// <param name="maxLevel">Maximum level filter.</param>
    /// <param name="minIv">Minimum IV total filter.</param>
    /// <param name="maxIv">Maximum IV total filter.</param>
    /// <param name="page">Page number for pagination (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 20).</param>
    /// <returns>Market listings with fraud scores and price analytics.</returns>
    [HttpGet("listings")]
    public async Task<ActionResult> GetMarketListings(
        [FromQuery] string sortBy = "recent",
        [FromQuery] string filter = "all",
        [FromQuery] string? search = null,
        [FromQuery] int? minPrice = null,
        [FromQuery] int? maxPrice = null,
        [FromQuery] int? minLevel = null,
        [FromQuery] int? maxLevel = null,
        [FromQuery] int? minIv = null,
        [FromQuery] int? maxIv = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Build base query with all listing data
            var query = from market in db.Market
                       join pokemon in db.UserPokemon on market.PokemonId equals pokemon.Id
                       join ownership in db.UserPokemonOwnerships on pokemon.Id equals ownership.PokemonId
                       where market.BuyerId == null // Only active listings
                       select new
                       {
                           ListingId = market.Id,
                           PokemonId = pokemon.Id,
                           pokemon.PokemonName,
                           pokemon.Level,
                           pokemon.Shiny,
                           pokemon.Radiant,
                           pokemon.Champion,
                           pokemon.Nature,
                           pokemon.HeldItem,
                           IvTotal = pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv + 
                                    pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv,
                           market.Price,
                           market.OwnerId,
                           market.ListedAt,
                           market.UpdatedAt,
                           market.ViewCount,
                           ownership.Position
                       };

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => l.PokemonName.Contains(search));
            }

            if (minPrice.HasValue)
                query = query.Where(l => l.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(l => l.Price <= maxPrice.Value);
            if (minLevel.HasValue)
                query = query.Where(l => l.Level >= minLevel.Value);
            if (maxLevel.HasValue)
                query = query.Where(l => l.Level <= maxLevel.Value);
            if (minIv.HasValue)
                query = query.Where(l => l.IvTotal >= minIv.Value);
            if (maxIv.HasValue)
                query = query.Where(l => l.IvTotal <= maxIv.Value);

            switch (filter.ToLower())
            {
                case "shiny":
                    query = query.Where(l => l.Shiny == true);
                    break;
                case "radiant":
                    query = query.Where(l => l.Radiant == true);
                    break;
                case "legendary":
                    query = query.Where(l => LegendaryPokemon.LegendList.Contains(l.PokemonName));
                    break;
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            switch (sortBy.ToLower())
            {
                case "name":
                    query = query.OrderBy(l => l.PokemonName);
                    break;
                case "level":
                    query = query.OrderByDescending(l => l.Level);
                    break;
                case "price":
                    query = query.OrderBy(l => l.Price);
                    break;
                case "popular":
                    query = query.OrderByDescending(l => l.ViewCount);
                    break;
                case "recent":
                default:
                    query = query.OrderByDescending(l => l.ListedAt);
                    break;
            }

            // Apply pagination
            var skip = (page - 1) * pageSize;
            var listings = await query.Skip(skip).Take(pageSize).ToListAsync();

            // Process listings with fraud detection and price analytics
            var processedListings = new List<object>();
            foreach (var listing in listings)
            {
                var processedListing = await ProcessListingWithAnalytics(listing, db);
                processedListings.Add(processedListing);
            }

            // Apply risk-based filtering after processing
            if (filter.ToLower() == "safe")
            {
                processedListings = processedListings
                    .Where(l => ((dynamic)l).RiskScore < 0.3)
                    .ToList();
            }
            else if (filter.ToLower() == "suspicious")
            {
                processedListings = processedListings
                    .Where(l => ((dynamic)l).RiskScore >= 0.6)
                    .ToList();
            }

            // Apply risk-based sorting if requested
            if (sortBy.ToLower() == "risk")
            {
                processedListings = processedListings
                    .OrderByDescending(l => ((dynamic)l).RiskScore)
                    .ToList();
            }

            return Ok(new { 
                success = true, 
                listings = processedListings,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting market listings");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets the current user's market listings.
    /// </summary>
    /// <returns>List of user's market listings.</returns>
    [HttpGet("my-listings")]
    public async Task<ActionResult> GetUserListings()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var listings = await (from market in db.Market
                                join pokemon in db.UserPokemon on market.PokemonId equals pokemon.Id
                                where market.OwnerId == userId && market.BuyerId == null
                                select new
                                {
                                    ListingId = market.Id,
                                    pokemon.PokemonName,
                                    pokemon.Level,
                                    market.Price,
                                    pokemon.Shiny,
                                    pokemon.Radiant,
                                    pokemon.Champion,
                                    pokemon.Favorite,
                                    market.ListedAt,
                                    IvTotal = pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv + 
                                             pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv
                                })
                                .OrderByDescending(l => l.ListedAt)
                                .ToListAsync();

            return Ok(new { success = true, listings });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting user market listings for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Lists a Pokemon for sale on the market.
    /// </summary>
    /// <param name="request">The listing request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("list")]
    public async Task<ActionResult> ListPokemon([FromBody] ListPokemonRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (request.PokemonPosition == 0)
                return BadRequest(new { error = "Pokemon position is required" });

            if (request.Price <= 0)
                return BadRequest(new { error = "Price must be greater than 0" });

            var result = await _marketService.AddPokemonToMarketAsync(userId, request.PokemonPosition, request.Price);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message, data = result.Data });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error listing Pokemon for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Removes a Pokemon listing from the market.
    /// </summary>
    /// <param name="listingId">The listing ID to remove.</param>
    /// <returns>Success or error message.</returns>
    [HttpDelete("listings/{listingId}")]
    public async Task<ActionResult> RemoveListing(ulong listingId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var result = await _marketService.RemovePokemonFromMarketAsync(userId, listingId);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error removing market listing {ListingId} for user {UserId}", listingId, GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Buys a Pokemon from the market.
    /// </summary>
    /// <param name="listingId">The listing ID to purchase.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("buy/{listingId}")]
    public async Task<ActionResult> BuyPokemon(ulong listingId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var result = await _marketService.BuyPokemonFromMarketAsync(userId, listingId);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message, data = result.Data });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buying Pokemon {ListingId} for user {UserId}", listingId, GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets detailed information about a specific market listing.
    /// </summary>
    /// <param name="listingId">The listing ID to get details for.</param>
    /// <returns>Detailed listing information.</returns>
    [HttpGet("listings/{listingId}")]
    public async Task<ActionResult> GetListingDetails(ulong listingId)
    {
        try
        {
            var pokemon = await _marketService.GetPokemonAsync(listingId);
            if (pokemon == null)
                return NotFound(new { error = "Listing not found" });

            await using var db = await _dbProvider.GetConnectionAsync();
            var marketListing = await db.Market
                .Where(m => m.Id == listingId && m.BuyerId == null)
                .Select(m => new
                {
                    m.Id,
                    m.Price,
                    m.OwnerId
                })
                .FirstOrDefaultAsync();

            if (marketListing == null)
                return NotFound(new { error = "Listing not found" });

            var details = new
            {
                ListingId = listingId,
                pokemon.PokemonName,
                pokemon.Level,
                pokemon.Shiny,
                pokemon.Radiant,
                pokemon.Champion,
                pokemon.Nature,
                pokemon.HeldItem,
                pokemon.Gender,
                pokemon.AbilityIndex,
                pokemon.Moves,
                pokemon.Tags,
                pokemon.Skin,
                IVs = new
                {
                    HP = pokemon.HpIv,
                    Attack = pokemon.AttackIv,
                    Defense = pokemon.DefenseIv,
                    SpecialAttack = pokemon.SpecialAttackIv,
                    SpecialDefense = pokemon.SpecialDefenseIv,
                    Speed = pokemon.SpeedIv,
                    Total = pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv + 
                           pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv
                },
                marketListing.Price,
                OwnerId = marketListing.OwnerId
            };

            return Ok(new { success = true, listing = details });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting listing details for {ListingId}", listingId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets market statistics and summary information.
    /// </summary>
    /// <returns>Market statistics.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult> GetMarketStats()
    {
        try
        {
            await using var db = await _dbProvider.GetConnectionAsync();
            
            var totalListings = await db.Market.CountAsync(m => m.BuyerId == null);
            var averagePrice = await db.Market.Where(m => m.BuyerId == null).AverageAsync(m => (double?)m.Price) ?? 0;
            var totalSold = await db.Market.CountAsync(m => m.BuyerId != null);
            
            // Top selling Pokemon
            var topSelling = await (from market in db.Market
                                  join pokemon in db.UserPokemon on market.PokemonId equals pokemon.Id
                                  where market.BuyerId != null
                                  group pokemon by pokemon.PokemonName into g
                                  select new
                                  {
                                      PokemonName = g.Key,
                                      SoldCount = g.Count()
                                  })
                                  .OrderByDescending(x => x.SoldCount)
                                  .Take(10)
                                  .ToListAsync();

            var stats = new
            {
                TotalActiveListings = totalListings,
                AveragePrice = Math.Round(averagePrice, 2),
                TotalSold = totalSold,
                TopSellingPokemon = topSelling
            };

            return Ok(new { success = true, stats });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting market statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Processes a market listing with fraud detection and price analytics.
    /// </summary>
    /// <param name="listing">The raw listing data.</param>
    /// <param name="db">The database connection.</param>
    /// <returns>Processed listing with risk scores and analytics.</returns>
    private async Task<object> ProcessListingWithAnalytics(dynamic listing, DittoDataConnection db)
    {
        try
        {
            // Extract values safely from dynamic object
            ulong listingId = listing.ListingId;
            ulong pokemonId = listing.PokemonId;
            string pokemonName = listing.PokemonName;
            int level = listing.Level;
            bool? shiny = listing.Shiny;
            bool? radiant = listing.Radiant;
            bool champion = listing.Champion;
            string nature = listing.Nature;
            string heldItem = listing.HeldItem;
            int ivTotal = listing.IvTotal;
            int price = listing.Price;
            ulong ownerId = listing.OwnerId;
            DateTime listedAt = listing.ListedAt;
            DateTime updatedAt = listing.UpdatedAt;
            int viewCount = listing.ViewCount;
            ulong position = listing.Position;

            // Calculate estimated value using the value calculator
            var estimatedValue = 1000; // Placeholder - would use _valueCalculator.CalculatePokemonValue()
            
            // Calculate price analytics
            var priceRatio = estimatedValue > 0 ? (double)price / estimatedValue : 1.0;
            var priceAnalysis = priceRatio switch
            {
                < 0.5 => "Significantly underpriced",
                < 0.8 => "Below market value", 
                > 2.0 => "Significantly overpriced",
                > 1.5 => "Above market value",
                _ => "Fair market price"
            };

            // Calculate basic risk factors
            var riskScore = 0.0;
            var riskFactors = new List<string>();

            // Price risk
            if (priceRatio < 0.3 || priceRatio > 3.0)
            {
                riskScore += 0.4;
                riskFactors.Add($"Suspicious pricing ({priceAnalysis.ToLower()})");
            }

            // Account age risk (would need user data)
            var seller = await db.Users.Where(u => u.UserId == ownerId).FirstOrDefaultAsync();
            if (seller != null)
            {
                // New accounts are riskier
                riskScore += 0.1;
                riskFactors.Add("Account analysis pending");
            }

            // Recent listing activity (rapid listing/delisting)
            var recentListings = await db.Market
                .Where(m => m.OwnerId == ownerId && m.ListedAt >= DateTime.UtcNow.AddHours(-24))
                .CountAsync();
            
            if (recentListings > 10)
            {
                riskScore += 0.3;
                riskFactors.Add("High listing activity");
            }

            // Market manipulation detection
            var duplicateListings = await db.Market
                .Where(m => m.OwnerId == ownerId && 
                           m.PokemonId != pokemonId && 
                           m.Price == price &&
                           m.BuyerId == null)
                .CountAsync();

            if (duplicateListings >= 3)
            {
                riskScore += 0.2;
                riskFactors.Add("Price coordination detected");
            }

            riskScore = Math.Min(riskScore, 1.0);

            var riskLevel = riskScore switch
            {
                < 0.3 => "Low",
                < 0.6 => "Medium", 
                < 0.8 => "High",
                _ => "Critical"
            };

            return new
            {
                ListingId = listingId,
                PokemonId = pokemonId,
                PokemonName = pokemonName,
                Level = level,
                Shiny = shiny,
                Radiant = radiant,
                Champion = champion,
                Nature = nature,
                HeldItem = heldItem,
                IvTotal = ivTotal,
                Price = price,
                OwnerId = ownerId,
                ListedAt = listedAt,
                UpdatedAt = updatedAt,
                ViewCount = viewCount,
                Position = position,
                
                // Analytics
                EstimatedValue = estimatedValue,
                PriceRatio = Math.Round(priceRatio, 2),
                PriceAnalysis = priceAnalysis,
                
                // Risk Assessment
                RiskScore = Math.Round(riskScore, 2),
                RiskLevel = riskLevel,
                RiskFactors = riskFactors,
                
                // Market Intelligence
                AgeInHours = Math.Round((DateTime.UtcNow - listedAt).TotalHours, 1),
                PopularityScore = viewCount
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error processing listing analytics for listing {ListingId}", listing?.ListingId ?? 0);
            
            // Return basic listing data if analytics fail
            return new
            {
                ListingId = listing?.ListingId ?? 0,
                PokemonId = listing?.PokemonId ?? 0,
                PokemonName = listing?.PokemonName ?? "Unknown",
                Level = listing?.Level ?? 0,
                Shiny = listing?.Shiny ?? false,
                Radiant = listing?.Radiant ?? false,
                Champion = listing?.Champion ?? false,
                Nature = listing?.Nature ?? "Unknown",
                HeldItem = listing?.HeldItem ?? "None",
                IvTotal = listing?.IvTotal ?? 0,
                Price = listing?.Price ?? 0,
                OwnerId = listing?.OwnerId ?? 0,
                ListedAt = listing?.ListedAt ?? DateTime.UtcNow,
                UpdatedAt = listing?.UpdatedAt ?? DateTime.UtcNow,
                ViewCount = listing?.ViewCount ?? 0,
                Position = listing?.Position ?? 0,
                RiskScore = 0.0,
                RiskLevel = "Unknown",
                RiskFactors = new[] { "Analysis failed" }
            };
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
    ///     Request model for listing a Pokemon on the market.
    /// </summary>
    public class ListPokemonRequest
    {
        /// <summary>Gets or sets the Pokemon position/ID to list.</summary>
        public ulong PokemonPosition { get; set; }
        
        /// <summary>Gets or sets the price for the Pokemon.</summary>
        public int Price { get; set; }
    }

    #endregion
}