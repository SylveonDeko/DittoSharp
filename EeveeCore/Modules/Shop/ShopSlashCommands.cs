using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Shop.Common;
using EeveeCore.Modules.Shop.Models;
using EeveeCore.Modules.Shop.Services;
using Serilog;

namespace EeveeCore.Modules.Shop;

/// <summary>
///     Provides Discord slash commands for the radiant shop system.
///     Allows users to browse and purchase rare Pokemon items and radiant variants.
/// </summary>
/// <param name="shopService">Service for handling shop operations.</param>
[Group("shop", "Radiant shop system for rare Pokemon items and variants")]
public class ShopSlashCommands(ShopService shopService) 
    : EeveeCoreSlashModuleBase<ShopService>
{
    /// <summary>
    ///     Opens the radiant shop interface for browsing items.
    ///     Displays available items with filtering and pagination options.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("browse", "Browse the radiant shop for rare items and Pokemon")]
    public async Task ShopBrowseCommand()
    {
        try
        {
            await DeferAsync();

            // Check if user can access the radiant shop
            var canAccess = await shopService.CanAccessRadiantShopAsync(ctx.User.Id);
            if (!canAccess)
            {
                var accessEmbed = new EmbedBuilder()
                    .WithTitle("🔒 Radiant Shop")
                    .WithDescription($"**Access Requirements:**\n" +
                                   $"• Minimum {ShopConstants.MinimumPokemonRequired} Pokemon in collection\n\n" +
                                   "The radiant shop contains rare and powerful items for experienced trainers. " +
                                   "Keep collecting Pokemon to unlock access!")
                    .WithColor(Color.Orange)
                    .WithThumbnailUrl("https://images.mewdeko.tech/skins/radiant/25-0-.png")
                    .Build();

                var components = new ComponentBuilder()
                    .WithButton("Browse Shop", "shop:browse", ButtonStyle.Primary, 
                        new Emoji("🛍️"), disabled: true)
                    .Build();

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = accessEmbed;
                    x.Components = components;
                });
                return;
            }

            // Show shop access available
            var userCredits = await shopService.GetUserCreditsAsync(ctx.User.Id);
            
            var welcomeEmbed = new EmbedBuilder()
                .WithTitle($"🛍️ Welcome to the Unified Shop!")
                .WithDescription("Your one-stop destination for all Pokemon needs!\n\n" +
                               "**Available Categories:**\n" +
                               "• 🛒 **General Items** - Basic items, vitamins, daycare, and everyday supplies\n" +
                               "• ✨ **Radiant Shop** - Exclusive radiant Pokemon and rare collectibles\n" +
                               "• 🔮 **Crystal Slime Exchange** - Exchange crystallized slime for special rewards\n\n" +
                               "Click **Browse Shop** to start shopping!")
                .WithColor(ShopConstants.CategoryColors["Radiant"])
                .AddField("💰 Your Credits", $"{ShopConstants.Emojis.Credits} {userCredits:N0}", true)
                .AddField("🎯 Access Level", "✅ Radiant Shop Unlocked", true)
                .WithThumbnailUrl("https://images.mewdeko.tech/skins/radiant/150-0-.png") // Radiant Mewtwo
                .WithFooter("Tip: Use filters to find exactly what you're looking for!")
                .Build();

            var shopComponents = new ComponentBuilder()
                .WithButton("🛍️ Browse Shop", "shop:browse", ButtonStyle.Success, new Emoji("🛍️"))
                .Build();

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = welcomeEmbed;
                x.Components = shopComponents;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error opening shop for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while opening the shop.");
        }
    }

    /// <summary>
    ///     Shows the user's current credit balance and recent transactions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("balance", "Check your current credit balance")]
    public async Task ShopBalanceCommand()
    {
        try
        {
            await DeferAsync(ephemeral: true);

            var credits = await shopService.GetUserCreditsAsync(ctx.User.Id);
            
            var embed = new EmbedBuilder()
                .WithTitle("💰 Credit Balance")
                .WithDescription($"**Current Balance:** {ShopConstants.Emojis.Credits} {credits:N0} credits")
                .WithColor(Color.Gold);

            // Add earning tips
            embed.AddField("💡 How to Earn Credits",
                "• Complete daily missions\n" +
                "• Win Pokemon battles\n" +
                "• Participate in events\n" +
                "• Trade with other trainers\n" +
                "• Find rare Pokemon", false);

            if (credits < 1000)
            {
                embed.AddField("🎯 Getting Started",
                    "Try completing some missions or catching Pokemon to earn your first credits!", false);
            }
            else if (credits >= 100000)
            {
                embed.AddField("🏆 High Roller",
                    "You have enough credits to purchase rare items in the radiant shop!", false);
            }

            embed.WithFooter("Credits are used to purchase items in the shop");

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error checking balance for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while checking your balance.");
        }
    }

    /// <summary>
    ///     Searches for specific items in the shop by name or category.
    /// </summary>
    /// <param name="query">The search query for item name or category.</param>
    /// <param name="type">Optional Pokemon type filter.</param>
    /// <param name="maxPrice">Optional maximum price filter.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("search", "Search for specific items in the shop")]
    public async Task ShopSearchCommand(
        [Summary("query", "Item name or category to search for")] string query,
        [Summary("type", "Filter by Pokemon type")] 
        [Choice("Fire", "Fire"), Choice("Water", "Water"), Choice("Electric", "Electric"),
         Choice("Grass", "Grass"), Choice("Ice", "Ice"), Choice("Fighting", "Fighting"),
         Choice("Poison", "Poison"), Choice("Ground", "Ground"), Choice("Flying", "Flying"),
         Choice("Psychic", "Psychic"), Choice("Bug", "Bug"), Choice("Rock", "Rock"),
         Choice("Ghost", "Ghost"), Choice("Dragon", "Dragon"), Choice("Dark", "Dark"),
         Choice("Steel", "Steel"), Choice("Fairy", "Fairy"), Choice("Normal", "Normal")]
        string? type = null,
        [Summary("max-price", "Maximum price in credits")] int? maxPrice = null)
    {
        try
        {
            await DeferAsync(ephemeral: true);

            // Check shop access
            var canAccess = await shopService.CanAccessRadiantShopAsync(ctx.User.Id);
            if (!canAccess)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = $"You need at least {ShopConstants.MinimumPokemonRequired} Pokemon to access the shop.";
                });
                return;
            }

            // Create search filters
            var filters = new ShopFilters
            {
                ItemName = query,
                PokemonType = type,
                MaxPrice = maxPrice,
                ShowAvailableOnly = true
            };

            var userCredits = await shopService.GetUserCreditsAsync(ctx.User.Id);
            var items = await shopService.GetShopItemsAsync(filters, ShopSortOrder.PriceAscending, userCredits);

            var embed = new EmbedBuilder()
                .WithTitle($"🔍 Search Results for \"{query}\"")
                .WithColor(Color.Blue);

            if (!items.Any())
            {
                embed.WithDescription("No items found matching your search criteria.");
                embed.AddField("💡 Search Tips",
                    "• Try broader search terms\n" +
                    "• Check your spelling\n" +
                    "• Remove filters and try again\n" +
                    "• Browse the shop to see all available items", false);
            }
            else
            {
                embed.WithDescription($"Found {items.Count} item(s) matching your search:");

                foreach (var item in items.Take(10)) // Limit to first 10 results
                {
                    var typeEmoji = !string.IsNullOrEmpty(item.PokemonType) && 
                                  ShopConstants.TypeEmojis.TryGetValue(item.PokemonType, out var emoji) ? emoji : "";
                    var affordableIcon = item.Price <= userCredits ? "✅" : "❌";
                    var stockInfo = item.Stock == -1 ? "∞" : item.Stock.ToString();

                    embed.AddField($"{affordableIcon} {typeEmoji} {item.Name}",
                        $"{item.Description}\n" +
                        $"**Price:** {ShopConstants.Emojis.Credits} {item.Price:N0} | **Stock:** {stockInfo} | **Category:** {item.Category}",
                        false);
                }

                if (items.Count > 10)
                {
                    embed.WithFooter($"... and {items.Count - 10} more items. Use /shop browse for full results.");
                }
            }

            // Add active filters info
            var filterInfo = new List<string>();
            if (!string.IsNullOrEmpty(type)) filterInfo.Add($"Type: {type}");
            if (maxPrice.HasValue) filterInfo.Add($"Max Price: {maxPrice:N0}");
            
            if (filterInfo.Any())
            {
                embed.AddField("🔧 Active Filters", string.Join(", ", filterInfo), false);
            }

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error searching shop for user {UserId} with query {Query}", ctx.User.Id, query);
            await ErrorAsync("An error occurred while searching the shop.");
        }
    }

    /// <summary>
    ///     Shows information about the shop system and how to use it.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("info", "Learn about the radiant shop system")]
    public async Task ShopInfoCommand()
    {
        try
        {
            var embed = new EmbedBuilder()
                .WithTitle($"🛍️ Unified Shop Information")
                .WithDescription("Your comprehensive shopping destination for all Pokemon needs!")
                .WithColor(ShopConstants.CategoryColors["General"]);

            embed.AddField("🎯 **What is the Unified Shop?**",
                "The unified shop combines all purchasing systems into one convenient interface. " +
                "Browse general items, exclusive radiant collectibles, and crystal slime exchanges " +
                "all in one place with easy category switching.", false);

            embed.AddField("🔓 **Access Requirements**",
                $"• Minimum {ShopConstants.MinimumPokemonRequired} Pokemon in your collection\n" +
                "• Available credits for purchases\n" +
                "• Active trainer status", false);

            embed.AddField("📂 **Shop Categories**",
                "• **🛒 General Items** - Basic items, vitamins, daycare spaces, evolution stones, berries\n" +
                "• **✨ Radiant Shop** - Exclusive radiant Pokemon, rare boosts, and legendary collectibles\n" +
                "• **🔮 Crystal Slime Exchange** - Trade crystallized slime for credits, friendship stones, VIP tokens", false);

            embed.AddField("💰 **Payment & Credits**",
                "All purchases are made with credits earned through:\n" +
                "• Daily missions and achievements\n" +
                "• Pokemon battles and tournaments\n" +
                "• Trading with other trainers\n" +
                "• Special events and competitions", false);

            embed.AddField("🛍️ **How to Shop**",
                "1. Use `/shop browse` to open the unified shop interface\n" +
                "2. Click category tabs to switch between General, Radiant, and Crystal Slime sections\n" +
                "3. Use filters to find specific items within each category\n" +
                "4. Click purchase buttons to buy items\n" +
                "5. Confirm your purchase in the modal\n" +
                "6. Items are added to your inventory automatically", false);

            embed.AddField("🔧 **Commands**",
                "`/shop browse` - Open the shop interface\n" +
                "`/shop search <query>` - Search for specific items\n" +
                "`/shop balance` - Check your credit balance\n" +
                "`/shop info` - Show this information", false);

            embed.AddField("⚠️ **Important Notes**",
                "• All sales are final - no refunds\n" +
                "• Stock is limited for many items\n" +
                "• Prices may change based on availability\n" +
                "• Some items are seasonal or event-exclusive", false);

            embed.WithFooter("Happy shopping! 🛍️");
            embed.WithThumbnailUrl("https://images.mewdeko.tech/skins/radiant/384-0-.png"); // Radiant Rayquaza

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error showing shop info for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while retrieving shop information.");
        }
    }

    /// <summary>
    ///     Shows the current shop statistics and trending items.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("stats", "View shop statistics and trending items")]
    public async Task ShopStatsCommand()
    {
        try
        {
            await DeferAsync(ephemeral: true);

            var userCredits = await shopService.GetUserCreditsAsync(ctx.User.Id);
            var allItems = await shopService.GetShopItemsAsync();

            var embed = new EmbedBuilder()
                .WithTitle("📊 Shop Statistics")
                .WithDescription("Current shop status and popular items")
                .WithColor(Color.Purple);

            // General stats
            embed.AddField("🏪 **Shop Overview**",
                $"• Total Items: {allItems.Count}\n" +
                $"• Radiant Pokemon: {allItems.Count(i => i.IsRadiant)}\n" +
                $"• Categories: {allItems.Select(i => i.Category).Distinct().Count()}\n" +
                $"• Average Price: {ShopConstants.Emojis.Credits} {(allItems.Any() ? allItems.Average(i => i.Price) : 0):N0}", true);

            // Price ranges
            if (allItems.Any())
            {
                embed.AddField("💰 **Price Ranges**",
                    $"• Cheapest: {ShopConstants.Emojis.Credits} {allItems.Min(i => i.Price):N0}\n" +
                    $"• Most Expensive: {ShopConstants.Emojis.Credits} {allItems.Max(i => i.Price):N0}\n" +
                    $"• You can afford: {allItems.Count(i => i.Price <= userCredits)} items", true);
            }

            // Category breakdown
            var categoryStats = allItems.GroupBy(i => i.Category)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"• {g.Key}: {g.Count()} items")
                .ToList();

            if (categoryStats.Any())
            {
                embed.AddField("📂 **Top Categories**", string.Join("\n", categoryStats), true);
            }

            // Affordable items for user
            var affordableItems = allItems.Where(i => i.Price <= userCredits).Take(5).ToList();
            if (affordableItems.Any())
            {
                var affordableList = affordableItems.Select(i => 
                    $"• {i.Name} - {ShopConstants.Emojis.Credits} {i.Price:N0}").ToList();
                embed.AddField("✅ **Affordable for You**", string.Join("\n", affordableList), false);
            }

            embed.WithFooter($"Your balance: {ShopConstants.Emojis.Credits} {userCredits:N0} credits");

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error showing shop stats for user {UserId}", ctx.User.Id);
            await ErrorAsync("An error occurred while retrieving shop statistics.");
        }
    }

    /// <summary>
    ///     Provides Discord slash commands for purchasing items and services with credits.
    ///     Consolidated from the Items module for unified shopping experience.
    /// </summary>
    [Group("buy", "Purchase items and services with credits")]
    public class BuyCommands : EeveeCoreSlashModuleBase<ShopService>
    {
        /// <summary>
        ///     Buys an item from the shop.
        ///     The item may be added to inventory or equipped to the selected Pokémon depending on the item type.
        /// </summary>
        /// <param name="itemName">The name of the item to buy.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("item", "Buy an item from the shop")]
        public async Task BuyItem(string itemName)
        {
            try
            {
                var result = await Service.BuyItemAsync(ctx.User.Id, itemName);
                await RespondAsync(result.Message, ephemeral: result.Ephemeral);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error buying item {ItemName} for user {UserId}", itemName, ctx.User.Id);
                await ErrorAsync("An error occurred while purchasing the item.");
            }
        }

        /// <summary>
        ///     Buys additional daycare spaces for breeding Pokémon.
        ///     Each space costs 10,000 credits.
        /// </summary>
        /// <param name="amount">The number of daycare spaces to buy. Defaults to 1.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("daycare", "Buy additional daycare spaces")]
        public async Task BuyDaycare(int amount = 1)
        {
            try
            {
                var result = await Service.BuyDaycareAsync(ctx.User.Id, amount);
                await RespondAsync(result.Message, ephemeral: result.Ephemeral);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error buying daycare spaces for user {UserId}", ctx.User.Id);
                await ErrorAsync("An error occurred while purchasing daycare spaces.");
            }
        }

        /// <summary>
        ///     Buys vitamins to increase the EVs (Effort Values) of the user's selected Pokémon.
        ///     Each vitamin costs 100 credits and adds 10 EVs to a specific stat.
        /// </summary>
        /// <param name="vitaminName">The name of the vitamin to buy.</param>
        /// <param name="amount">The amount of the vitamin to buy.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("vitamins", "Buy vitamins for your selected pokemon")]
        public async Task BuyVitamins(
            [Choice("HP-Up", "hp-up"), Choice("Protein", "protein"), Choice("Iron", "iron"),
             Choice("Calcium", "calcium"), Choice("Zinc", "zinc"), Choice("Carbos", "carbos")]
            string vitaminName, int amount)
        {
            try
            {
                var result = await Service.BuyVitaminsAsync(ctx.User.Id, vitaminName, amount);
                await RespondAsync(result.Message, ephemeral: result.Ephemeral);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error buying vitamins for user {UserId}", ctx.User.Id);
                await ErrorAsync("An error occurred while purchasing vitamins.");
            }
        }

        /// <summary>
        ///     Buys and applies Rare Candy to level up the user's selected Pokémon.
        ///     Each candy costs 100 credits and adds one level, up to a maximum of level 100.
        /// </summary>
        /// <param name="amount">The number of Rare Candies to buy and use. Defaults to 1.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("candy", "Buy rare candy to level up your selected pokemon")]
        public async Task BuyCandy(int amount = 1)
        {
            try
            {
                var result = await Service.BuyCandyAsync(ctx.User.Id, amount);
                await RespondAsync(result.Message, ephemeral: result.Ephemeral);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error buying candy for user {UserId}", ctx.User.Id);
                await ErrorAsync("An error occurred while purchasing rare candy.");
            }
        }

        /// <summary>
        ///     Buys a radiant chest that can contain rare Pokémon or items.
        ///     Chests can be purchased with either credits or redeems and have weekly purchase limits.
        /// </summary>
        /// <param name="chestType">The type of chest to buy.</param>
        /// <param name="currency">The currency to use for purchase.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("chest", "Buy a radiant chest")]
        public async Task BuyChest(
            [Choice("Rare", "rare"), Choice("Mythic", "mythic"), Choice("Legend", "legend")]
            string chestType,
            [Choice("Credits", "credits"), Choice("Redeems", "redeems")]
            string currency)
        {
            try
            {
                // Require confirmation for chest purchases
                if (!await PromptUserConfirmAsync(
                        $"Are you sure you want to buy a {chestType} chest with {currency}?", ctx.User.Id))
                {
                    await RespondAsync("Purchase cancelled.", ephemeral: true);
                    return;
                }

                var result = await Service.BuyChestAsync(ctx.User.Id, chestType, currency);
                await RespondAsync(result.Message, ephemeral: result.Ephemeral);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error buying chest for user {UserId}", ctx.User.Id);
                await ErrorAsync("An error occurred while purchasing the chest.");
            }
        }

        /// <summary>
        ///     Buys redeems for the user, which can be used to redeem special Pokémon or items.
        ///     Redeems cost 60,000 credits each and have a weekly purchase limit of 100.
        /// </summary>
        /// <param name="amount">The number of redeems to buy, or null to show current purchase stats.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("redeems", "Buy redeems for credits")]
        public async Task BuyRedeems(int? amount = null)
        {
            try
            {
                var result = await Service.BuyRedeemsAsync(ctx.User.Id, (ulong?)amount);
                if (result.Embed != null)
                    await RespondAsync(embed: result.Embed, ephemeral: result.Ephemeral);
                else
                    await RespondAsync(result.Message, ephemeral: result.Ephemeral);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error buying redeems for user {UserId}", ctx.User.Id);
                await ErrorAsync("An error occurred while purchasing redeems.");
            }
        }
    }
}