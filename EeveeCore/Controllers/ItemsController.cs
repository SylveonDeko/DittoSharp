using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EeveeCore.Modules.Items.Services;
using EeveeCore.Services.Impl;
using LinqToDB;
using MongoDB.Driver;
using Serilog;

namespace EeveeCore.Controllers;

/// <summary>
///     API controller for item management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Jwt")]
public class ItemsController : ControllerBase
{
    private readonly ItemsService _itemsService;
    private readonly IMongoService _mongoService;
    private readonly LinqToDbConnectionProvider _dbProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ItemsController"/> class.
    /// </summary>
    /// <param name="itemsService">The items service.</param>
    /// <param name="mongoService">The MongoDB service.</param>
    /// <param name="dbProvider">The database provider.</param>
    public ItemsController(ItemsService itemsService, IMongoService mongoService, LinqToDbConnectionProvider dbProvider)
    {
        _itemsService = itemsService;
        _mongoService = mongoService;
        _dbProvider = dbProvider;
    }

    /// <summary>
    ///     Gets available items from the shop.
    /// </summary>
    /// <returns>List of available shop items with prices and descriptions.</returns>
    [HttpGet("shop")]
    public async Task<ActionResult> GetShopItems()
    {
        try
        {
            // Get items from MongoDB shop collection
            var shopItems = await _mongoService.Shop
                .Find(_ => true)
                .ToListAsync();

            return Ok(new { success = true, items = shopItems });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting shop items");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets items that can be equipped to Pokemon (held items).
    /// </summary>
    /// <returns>List of equippable items.</returns>
    [HttpGet("equippable")]
    public async Task<ActionResult> GetEquippableItems()
    {
        try
        {
            // Get all items from shop
            var shopItems = await _mongoService.Shop
                .Find(_ => true)
                .ToListAsync();

            // Get items that can't be equipped (active items)
            var activeItems = _itemsService.GetActiveItems();
            var berryItems = _itemsService.GetBerryItems();

            // Equippable items are shop items + berries that are NOT in active items list
            var equippableItems = shopItems
                .Where(item => !activeItems.Contains(item.Item))
                .Select(item => item.Item)
                .Concat(berryItems.Where(berry => !activeItems.Contains(berry)))
                .Concat(new[] { "glitchy-orb" }) // Special case from service logic
                .Distinct()
                .ToList();

            return Ok(new { success = true, items = equippableItems });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting equippable items");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets items that can be applied to Pokemon (evolution stones, berries, etc.).
    /// </summary>
    /// <returns>List of usable items.</returns>
    [HttpGet("usable")]
    public async Task<ActionResult> GetUsableItems()
    {
        try
        {
            var activeItems = _itemsService.GetActiveItems();
            var berries = _itemsService.GetBerryItems();
            var allUsable = _itemsService.GetUsableItems();

            await Task.CompletedTask; // Make it actually async
            return Ok(new { 
                success = true, 
                evolutionItems = activeItems, 
                berries, 
                allUsable
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting usable items");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Gets information about what items can be used on a specific Pokemon.
    /// </summary>
    /// <param name="pokemonId">The Pokemon ID to check.</param>
    /// <returns>List of applicable items for the Pokemon.</returns>
    [HttpGet("applicable/{pokemonId}")]
    public async Task<ActionResult> GetApplicableItems(ulong pokemonId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            await using var db = await _dbProvider.GetConnectionAsync();
            
            // Verify user owns this Pokemon
            var pokemon = await (from ownership in db.UserPokemonOwnerships
                               join p in db.UserPokemon on ownership.PokemonId equals p.Id
                               where ownership.UserId == userId && p.Id == pokemonId
                               select p).FirstOrDefaultAsync();

            if (pokemon == null)
                return NotFound(new { error = "Pokemon not found or not owned by user" });

            // This would be more sophisticated in a real implementation
            // You'd check the Pokemon's species, current evolution stage, etc.
            var applicableItems = new
            {
                EvolutionStones = new List<string> { "fire-stone", "water-stone", "thunder-stone", "leaf-stone" },
                Berries = new List<string> { "cheri-berry", "chesto-berry", "pecha-berry", "rawst-berry" },
                CanEquip = !string.IsNullOrEmpty(pokemon.HeldItem) && pokemon.HeldItem != "None" ? 
                    new List<string>() : 
                    new List<string> { "leftovers", "choice-band", "choice-specs", "life-orb" }
            };

            return Ok(new { success = true, pokemonName = pokemon.PokemonName, applicableItems });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting applicable items for Pokemon {PokemonId}", pokemonId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Equips an item to the user's selected Pokemon.
    /// </summary>
    /// <param name="request">The equip item request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("equip")]
    public async Task<ActionResult> EquipItem([FromBody] EquipItemRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest(new { error = "Item name is required" });

            var result = await _itemsService.Equip(userId, request.ItemName);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error equipping item for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Unequips an item from the user's selected Pokemon.
    /// </summary>
    /// <returns>Success or error message.</returns>
    [HttpPost("unequip")]
    public async Task<ActionResult> UnequipItem()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var result = await _itemsService.Unequip(userId);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error unequipping item for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Drops an item from the user's selected Pokemon.
    /// </summary>
    /// <returns>Success or error message.</returns>
    [HttpPost("drop")]
    public async Task<ActionResult> DropItem()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var result = await _itemsService.Drop(userId);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error dropping item for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Transfers an item from the selected Pokemon to another Pokemon.
    /// </summary>
    /// <param name="request">The transfer item request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("transfer")]
    public async Task<ActionResult> TransferItem([FromBody] TransferItemRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (request.TargetPokemonId == 0)
                return BadRequest(new { error = "Target Pokemon ID is required" });

            var result = await _itemsService.Transfer(userId, request.TargetPokemonId);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error transferring item for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Applies an item to the user's selected Pokemon.
    /// </summary>
    /// <param name="request">The apply item request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("apply")]
    public async Task<ActionResult> ApplyItem([FromBody] ApplyItemRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest(new { error = "Item name is required" });

            // Note: Using null for channel since this is a web API endpoint
            // The ItemsService might need to be modified to handle web-based item application
            var result = await _itemsService.Apply(userId, request.ItemName, null);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying item for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Buys an item from the shop.
    /// </summary>
    /// <param name="request">The buy item request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("buy")]
    public async Task<ActionResult> BuyItem([FromBody] BuyItemRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest(new { error = "Item name is required" });

            var result = await _itemsService.BuyItem(userId, request.ItemName);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buying item for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Buys daycare slots.
    /// </summary>
    /// <param name="request">The buy daycare request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("buy-daycare")]
    public async Task<ActionResult> BuyDaycare([FromBody] BuyDaycareRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (request.Amount <= 0)
                return BadRequest(new { error = "Amount must be greater than 0" });

            var result = await _itemsService.BuyDaycare(userId, request.Amount);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buying daycare for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Buys vitamins.
    /// </summary>
    /// <param name="request">The buy vitamins request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("buy-vitamins")]
    public async Task<ActionResult> BuyVitamins([FromBody] BuyVitaminsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.ItemName))
                return BadRequest(new { error = "Item name is required" });

            if (request.Amount <= 0)
                return BadRequest(new { error = "Amount must be greater than 0" });

            var result = await _itemsService.BuyVitamins(userId, request.ItemName, request.Amount);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buying vitamins for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Buys candy (rare candy).
    /// </summary>
    /// <param name="request">The buy candy request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("buy-candy")]
    public async Task<ActionResult> BuyCandy([FromBody] BuyCandyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (request.Amount <= 0)
                return BadRequest(new { error = "Amount must be greater than 0" });

            var result = await _itemsService.BuyCandy(userId, request.Amount);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buying candy for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Buys a chest.
    /// </summary>
    /// <param name="request">The buy chest request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("buy-chest")]
    public async Task<ActionResult> BuyChest([FromBody] BuyChestRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.ChestType))
                return BadRequest(new { error = "Chest type is required" });

            if (string.IsNullOrWhiteSpace(request.CreditsOrRedeems))
                return BadRequest(new { error = "Payment method is required" });

            var result = await _itemsService.BuyChest(userId, request.ChestType, request.CreditsOrRedeems);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buying chest for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    ///     Buys redeems.
    /// </summary>
    /// <param name="request">The buy redeems request.</param>
    /// <returns>Success or error message.</returns>
    [HttpPost("buy-redeems")]
    public async Task<ActionResult> BuyRedeems([FromBody] BuyRedeemsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return BadRequest(new { error = "Invalid user ID" });

            var result = await _itemsService.BuyRedeems(userId, request.Amount);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            
            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error buying redeems for user {UserId}", GetCurrentUserId());
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
    ///     Request model for equipping an item.
    /// </summary>
    public class EquipItemRequest
    {
        /// <summary>Gets or sets the name of the item to equip.</summary>
        public string ItemName { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Request model for transferring an item.
    /// </summary>
    public class TransferItemRequest
    {
        /// <summary>Gets or sets the target Pokemon ID.</summary>
        public ulong TargetPokemonId { get; set; }
    }

    /// <summary>
    ///     Request model for applying an item.
    /// </summary>
    public class ApplyItemRequest
    {
        /// <summary>Gets or sets the name of the item to apply.</summary>
        public string ItemName { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Request model for buying an item.
    /// </summary>
    public class BuyItemRequest
    {
        /// <summary>Gets or sets the name of the item to buy.</summary>
        public string ItemName { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Request model for buying daycare slots.
    /// </summary>
    public class BuyDaycareRequest
    {
        /// <summary>Gets or sets the amount of slots to buy.</summary>
        public int Amount { get; set; }
    }

    /// <summary>
    ///     Request model for buying vitamins.
    /// </summary>
    public class BuyVitaminsRequest
    {
        /// <summary>Gets or sets the vitamin item name.</summary>
        public string ItemName { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the amount to buy.</summary>
        public int Amount { get; set; }
    }

    /// <summary>
    ///     Request model for buying candy.
    /// </summary>
    public class BuyCandyRequest
    {
        /// <summary>Gets or sets the amount of candy to buy.</summary>
        public int Amount { get; set; }
    }

    /// <summary>
    ///     Request model for buying a chest.
    /// </summary>
    public class BuyChestRequest
    {
        /// <summary>Gets or sets the chest type.</summary>
        public string ChestType { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the payment method (credits or redeems).</summary>
        public string CreditsOrRedeems { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Request model for buying redeems.
    /// </summary>
    public class BuyRedeemsRequest
    {
        /// <summary>Gets or sets the amount of redeems to buy.</summary>
        public ulong? Amount { get; set; }
    }

    #endregion

    /// <summary>
    ///     Response model for shop items.
    /// </summary>
    public class ShopItemResponse
    {
        /// <summary>Gets or sets the item name.</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the item description.</summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the item price.</summary>
        public int Price { get; set; }
        
        /// <summary>Gets or sets the currency type (credits, redeems, etc.).</summary>
        public string Currency { get; set; } = string.Empty;
        
        /// <summary>Gets or sets the item category.</summary>
        public string Category { get; set; } = string.Empty;
    }
}