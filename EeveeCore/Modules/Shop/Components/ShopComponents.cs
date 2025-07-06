using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Shop.Common;
using EeveeCore.Modules.Shop.Models;
using EeveeCore.Modules.Shop.Services;
using Serilog;

namespace EeveeCore.Modules.Shop.Components;

/// <summary>
///     Handles interaction components for the radiant shop system.
///     Processes button clicks, select menu interactions, and modal submissions for shop operations.
/// </summary>
/// <param name="shopService">The service that handles shop operations.</param>
public class ShopInteractionModule(ShopService shopService) 
    : EeveeCoreSlashModuleBase<ShopService>
{
    /// <summary>
    ///     Handles the browse shop button interaction.
    ///     Initializes a new shop session and displays the shop interface.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:browse")]
    public async Task HandleBrowseShop()
    {
        try
        {
            await DeferAsync();

            // Check if user can access the radiant shop
            var canAccess = await shopService.CanAccessRadiantShopAsync(ctx.User.Id);
            if (!canAccess)
            {
                var accessDeniedContainer = new ContainerBuilder()
                    .WithComponents(new List<IMessageComponentBuilder>
                    {
                        new TextDisplayBuilder()
                            .WithContent($"# ❌ Radiant Shop Access Denied\n" +
                                       $"You need at least {ShopConstants.MinimumPokemonRequired} Pokemon to access the radiant shop.\n\n" +
                                       "Keep collecting Pokemon and try again!")
                    })
                    .WithAccentColor(Color.Red);

                var accessDeniedComponents = new ComponentBuilderV2()
                    .AddComponent(accessDeniedContainer);

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "";
                    x.Embed = null;
                    x.Components = accessDeniedComponents.Build();
                    x.Flags = MessageFlags.ComponentsV2;
                });
                return;
            }

            // Create new shop session
            var sessionId = Guid.NewGuid().ToString();
            var session = new ShopSession
            {
                SessionId = sessionId,
                UserId = ctx.User.Id
            };
            ShopSessionManager.StoreSession(session);

            await UpdateShopDisplayAsync(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error browsing shop for user {UserId}", ctx.User.Id);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while opening the shop.";
                x.Embed = null;
                x.Components = null;
            });
        }
    }

    /// <summary>
    ///     Handles navigation between shop pages.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <param name="direction">The navigation direction (next/prev).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:navigate:*:*")]
    public async Task HandleNavigation(string sessionId, string direction)
    {
        try
        {
            await DeferAsync();

            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Shop session not found or expired. Please start a new shop session.";
                    x.Embed = null;
                    x.Components = null;
                });
                return;
            }

            // Update page number
            if (direction == "next")
            {
                session.CurrentPage++;
            }
            else if (direction == "prev" && session.CurrentPage > 0)
            {
                session.CurrentPage--;
            }

            session.LastAccessed = DateTimeOffset.UtcNow;
            ShopSessionManager.StoreSession(session);

            await UpdateShopDisplayAsync(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error navigating shop for session {SessionId}", sessionId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while navigating the shop.";
                x.Embed = null;
                x.Components = null;
            });
        }
    }

    /// <summary>
    ///     Handles the filter button interaction.
    ///     Shows a modal for users to input their filter preferences.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:filter:*")]
    public async Task HandleFilter(string sessionId)
    {
        try
        {
            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.RespondAsync("Shop session not found or expired.", ephemeral: true);
                return;
            }

            await ctx.Interaction.RespondWithModalAsync<ShopFilterModal>($"shop_filter_modal:{sessionId}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling filter for session {SessionId}", sessionId);
            await ctx.Interaction.RespondAsync("An error occurred while opening the filter dialog.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the type filter select menu interaction.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <param name="selectedTypes">The selected Pokemon types.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:type_filter:*")]
    public async Task HandleTypeFilter(string sessionId, string[] selectedTypes)
    {
        try
        {
            await DeferAsync();

            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Shop session not found or expired.";
                });
                return;
            }

            // Update filter
            session.Filters.PokemonType = selectedTypes.FirstOrDefault();
            session.CurrentPage = 0; // Reset to first page
            session.LastAccessed = DateTimeOffset.UtcNow;
            ShopSessionManager.StoreSession(session);

            await UpdateShopDisplayAsync(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling type filter for session {SessionId}", sessionId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while applying the type filter.";
            });
        }
    }

    /// <summary>
    ///     Handles the purchase select menu interaction.
    ///     Shows a confirmation modal for the selected item.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <param name="selectedValues">The selected values from the select menu.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:purchase_select:*")]
    public async Task HandlePurchaseSelect(string sessionId, string[] selectedValues)
    {
        try
        {
            if (selectedValues.Length == 0 || string.IsNullOrEmpty(selectedValues[0]))
            {
                await ctx.Interaction.RespondAsync("No item selected.", ephemeral: true);
                return;
            }

            // Extract item ID from the value format "shop_purchase:itemId"
            var selectedValue = selectedValues[0];
            if (!selectedValue.StartsWith("shop_purchase:"))
            {
                await ctx.Interaction.RespondAsync("Invalid selection.", ephemeral: true);
                return;
            }

            var itemId = selectedValue.Substring("shop_purchase:".Length);
            await HandlePurchase(sessionId, itemId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling purchase select for session {SessionId}", sessionId);
            await ctx.Interaction.RespondAsync("An error occurred while processing your selection.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the purchase logic for the selected item.
    ///     Shows a confirmation modal for the selected item.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <param name="itemId">The ID of the item to purchase.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandlePurchase(string sessionId, string itemId)
    {
        try
        {
            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.RespondAsync("Shop session not found or expired.", ephemeral: true);
                return;
            }

            var item = await shopService.GetShopItemAsync(itemId);
            if (item == null)
            {
                await ctx.Interaction.RespondAsync("Item not found in shop.", ephemeral: true);
                return;
            }

            // Store selected item in session
            session.SelectedItemId = itemId;
            ShopSessionManager.StoreSession(session);

            await ctx.Interaction.RespondWithModalAsync<PurchaseConfirmationModal>($"shop_purchase_modal:{sessionId}:{itemId}");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling purchase for item {ItemId}", itemId);
            await ctx.Interaction.RespondAsync("An error occurred while initiating the purchase.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the refresh shop button interaction.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:refresh:*")]
    public async Task HandleRefresh(string sessionId)
    {
        try
        {
            await DeferAsync();

            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Shop session not found or expired.";
                });
                return;
            }

            session.LastAccessed = DateTimeOffset.UtcNow;
            ShopSessionManager.StoreSession(session);

            await UpdateShopDisplayAsync(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error refreshing shop for session {SessionId}", sessionId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while refreshing the shop.";
            });
        }
    }

    /// <summary>
    ///     Handles category tab selection.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <param name="category">The selected category.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:category:*:*")]
    public async Task HandleCategorySelection(string sessionId, string category)
    {
        try
        {
            await DeferAsync();

            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Shop session not found or expired.";
                });
                return;
            }

            // Update category and reset filters/pagination
            session.CurrentCategory = category;
            session.Filters.ClearFilters();
            session.CurrentPage = 0;
            session.LastAccessed = DateTimeOffset.UtcNow;
            ShopSessionManager.StoreSession(session);

            await UpdateShopDisplayAsync(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling category selection for session {SessionId}", sessionId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while switching categories.";
            });
        }
    }

    /// <summary>
    ///     Handles the clear filters button interaction.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("shop:clear_filters:*")]
    public async Task HandleClearFilters(string sessionId)
    {
        try
        {
            await DeferAsync();

            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Shop session not found or expired.";
                });
                return;
            }

            // Clear all filters
            session.Filters.ClearFilters();
            session.CurrentPage = 0;
            session.LastAccessed = DateTimeOffset.UtcNow;
            ShopSessionManager.StoreSession(session);

            await UpdateShopDisplayAsync(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error clearing filters for session {SessionId}", sessionId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while clearing filters.";
            });
        }
    }

    /// <summary>
    ///     Handles modal submissions for shop filters.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <param name="modal">The submitted filter modal.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("shop_filter_modal:*")]
    public async Task HandleFilterModal(string sessionId, ShopFilterModal modal)
    {
        try
        {
            await DeferAsync();

            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Shop session not found or expired.";
                });
                return;
            }

            // Apply filters from modal
            if (!string.IsNullOrWhiteSpace(modal.ItemName))
            {
                session.Filters.ItemName = modal.ItemName;
            }

            if (!string.IsNullOrWhiteSpace(modal.Category))
            {
                session.Filters.Category = modal.Category;
            }

            if (!string.IsNullOrWhiteSpace(modal.Rarity))
            {
                session.Filters.Rarity = modal.Rarity;
            }

            if (!string.IsNullOrWhiteSpace(modal.PriceRange))
            {
                var parts = modal.PriceRange.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
                {
                    session.Filters.MinPrice = min;
                    session.Filters.MaxPrice = max;
                }
            }

            session.CurrentPage = 0;
            session.LastAccessed = DateTimeOffset.UtcNow;
            ShopSessionManager.StoreSession(session);

            await UpdateShopDisplayAsync(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling filter modal for session {SessionId}", sessionId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while applying filters.";
            });
        }
    }

    /// <summary>
    ///     Handles modal submissions for purchase confirmations.
    /// </summary>
    /// <param name="sessionId">The shop session ID.</param>
    /// <param name="itemId">The item ID to purchase.</param>
    /// <param name="modal">The submitted purchase modal.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("shop_purchase_modal:*:*")]
    public async Task HandlePurchaseModal(string sessionId, string itemId, PurchaseConfirmationModal modal)
    {
        try
        {
            await DeferAsync();

            var session = ShopSessionManager.GetSession(sessionId);
            if (session == null || session.UserId != ctx.User.Id)
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Shop session not found or expired.";
                });
                return;
            }

            // Validate confirmation
            if (!modal.Confirmation.Equals("CONFIRM", StringComparison.OrdinalIgnoreCase))
            {
                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "Purchase cancelled. You must type 'CONFIRM' to proceed with the purchase.";
                });
                return;
            }

            // Parse quantity
            var quantity = 1;
            if (!string.IsNullOrWhiteSpace(modal.Quantity) && int.TryParse(modal.Quantity, out var parsedQuantity))
            {
                quantity = Math.Max(1, parsedQuantity);
            }

            // Process the purchase
            var result = await shopService.PurchaseItemAsync(ctx.User.Id, itemId, quantity);

            if (result.Success)
            {
                var successEmbed = new EmbedBuilder()
                    .WithTitle("✅ Purchase Successful!")
                    .WithDescription(result.Message)
                    .WithColor(Color.Green)
                    .AddField("Item", result.Item?.Name ?? "Unknown", true)
                    .AddField("Quantity", quantity.ToString(), true)
                    .AddField("Total Cost", $"{ShopConstants.Emojis.Credits} {result.TotalCost:N0}", true)
                    .AddField("Remaining Credits", $"{ShopConstants.Emojis.Credits} {result.RemainingCredits:N0}", true)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .Build();

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "";
                    x.Embed = successEmbed;
                    x.Components = null;
                });
            }
            else
            {
                var errorEmbed = new EmbedBuilder()
                    .WithTitle("❌ Purchase Failed")
                    .WithDescription(result.Message)
                    .WithColor(Color.Red)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .Build();

                await ctx.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "";
                    x.Embed = errorEmbed;
                    x.Components = null;
                });
            }

            // Update shop display
            session.LastAccessed = DateTimeOffset.UtcNow;
            ShopSessionManager.StoreSession(session);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error handling purchase modal for item {ItemId}", itemId);
            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "An error occurred while processing your purchase.";
            });
        }
    }

    /// <summary>
    ///     Updates the shop display with current session state.
    /// </summary>
    /// <param name="session">The shop session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task UpdateShopDisplayAsync(ShopSession session)
    {
        try
        {
            var userCredits = await shopService.GetUserCreditsAsync(session.UserId);
            var items = await shopService.GetShopItemsByCategoryAsync(session.CurrentCategory, session.Filters, session.SortOrder, userCredits);

            // Calculate pagination
            var totalPages = (int)Math.Ceiling((double)items.Count / ShopConstants.ItemsPerPage);
            var currentPageItems = items
                .Skip(session.CurrentPage * ShopConstants.ItemsPerPage)
                .Take(ShopConstants.ItemsPerPage)
                .ToList();

            // Create Components V2 display
            var containerComponents = CreateShopContainer(session, currentPageItems, totalPages, userCredits);

            await ctx.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = null;
                x.Components = containerComponents;
                x.Flags = MessageFlags.ComponentsV2;
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error updating shop display for session {SessionId}", session.SessionId);
            throw;
        }
    }

    /// <summary>
    ///     Creates the shop Components V2 container display.
    /// </summary>
    /// <param name="session">The shop session.</param>
    /// <param name="items">The items to display.</param>
    /// <param name="totalPages">The total number of pages.</param>
    /// <param name="userCredits">The user's credit balance.</param>
    /// <returns>The created message components.</returns>
    private static MessageComponent CreateShopContainer(ShopSession session, List<ShopItem> items, int totalPages, int userCredits)
    {
        var categoryInfo = ShopConstants.CategoryInfo[session.CurrentCategory];
        var categoryColor = ShopConstants.CategoryColors[session.CurrentCategory];
        
        var containerComponents = new List<IMessageComponentBuilder>();

        // Add shop title and description
        containerComponents.Add(new TextDisplayBuilder()
            .WithContent($"# {categoryInfo.Emoji} {categoryInfo.DisplayName}\n{categoryInfo.Description}"));

        // Add category tab buttons at the top
        var categoryRow = new ActionRowBuilder();
        foreach (var category in ShopConstants.ShopCategories)
        {
            var catInfo = ShopConstants.CategoryInfo[category];
            var isActive = session.CurrentCategory == category;
            var buttonStyle = isActive ? ButtonStyle.Success : ButtonStyle.Secondary;
            
            categoryRow.WithButton($"{catInfo.Emoji} {catInfo.DisplayName}", 
                $"shop:category:{session.SessionId}:{category}", 
                buttonStyle, disabled: isActive);
        }
        containerComponents.Add(categoryRow);

        // Add separator after category buttons
        containerComponents.Add(new SeparatorBuilder());

        // Add user info section
        var userInfoText = $"💰 **Your Credits:** {ShopConstants.Emojis.Credits} {userCredits:N0} | " +
                           $"📄 **Page:** {session.CurrentPage + 1}/{Math.Max(1, totalPages)} | ";
        
        containerComponents.Add(new TextDisplayBuilder().WithContent(userInfoText));

        // Add active filters info if any
        if (session.Filters.HasActiveFilters)
        {
            var filterInfo = new List<string>();
            if (!string.IsNullOrEmpty(session.Filters.PokemonType))
                filterInfo.Add($"Type: {session.Filters.PokemonType}");
            if (!string.IsNullOrEmpty(session.Filters.Category))
                filterInfo.Add($"Category: {session.Filters.Category}");
            if (session.Filters.MinPrice.HasValue || session.Filters.MaxPrice.HasValue)
                filterInfo.Add($"Price: {session.Filters.MinPrice ?? 0}-{session.Filters.MaxPrice ?? int.MaxValue}");

            containerComponents.Add(new TextDisplayBuilder()
                .WithContent($"🔍 **Active Filters:** {string.Join(", ", filterInfo)}"));
        }

        // Add separator before items
        containerComponents.Add(new SeparatorBuilder());

        // Add items (combine all items into one text display to reduce component count)
        if (items.Any())
        {
            var itemsText = new List<string>();
            
            foreach (var item in items)
            {
                var typeEmoji = !string.IsNullOrEmpty(item.PokemonType) && 
                              ShopConstants.TypeEmojis.TryGetValue(item.PokemonType, out var emoji) ? emoji : "";
                var stockInfo = item.Stock == -1 ? "∞" : item.Stock.ToString();
                var affordableIcon = item.Price <= userCredits ? "✅" : "❌";

                var itemText = $"**{affordableIcon} {typeEmoji} {item.Name}**\n" +
                              $"{item.Description}\n" +
                              $"**Price:** {ShopConstants.Emojis.Credits} {item.Price:N0} | **Stock:** {stockInfo} | **Rarity:** {item.Rarity}";
                
                itemsText.Add(itemText);
            }

            containerComponents.Add(new TextDisplayBuilder()
                .WithContent(string.Join("\n\n", itemsText)));

            // Add purchase select menu for all items
            if (items.Count > 0)
            {
                var purchaseOptions = items.Select(item => 
                {
                    var affordable = item.Price <= userCredits;
                    var affordableIcon = affordable ? "✅" : "❌";
                    var typeEmoji = !string.IsNullOrEmpty(item.PokemonType) && 
                                  ShopConstants.TypeEmojis.TryGetValue(item.PokemonType, out var emoji) ? emoji : "";
                    
                    var optionBuilder = new SelectMenuOptionBuilder()
                        .WithLabel($"{affordableIcon} {item.Name}")
                        .WithValue($"shop_purchase:{item.Id}")
                        .WithDescription($"{ShopConstants.Emojis.Credits} {item.Price:N0} | Stock: {(item.Stock == -1 ? "∞" : item.Stock.ToString())}");
                    
                    // Only add emoji if it's not empty and is a valid Unicode emoji
                    if (!string.IsNullOrEmpty(typeEmoji))
                    {
                        try
                        {
                            optionBuilder.WithEmote(new Emoji(typeEmoji));
                        }
                        catch
                        {
                            // If emoji is invalid, skip adding it
                        }
                    }
                    
                    return optionBuilder;
                }).ToList();

                var purchaseSelect = new SelectMenuBuilder()
                    .WithCustomId($"shop:purchase_select:{session.SessionId}")
                    .WithPlaceholder("💰 Select an item to purchase...")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .WithOptions(purchaseOptions);

                var purchaseSelectRow = new ActionRowBuilder().WithSelectMenu(purchaseSelect);
                containerComponents.Add(purchaseSelectRow);
            }
        }
        else
        {
            containerComponents.Add(new TextDisplayBuilder()
                .WithContent("**No Items Found**\nNo items match your current filters. Try adjusting your search criteria."));
        }

        // Add separator before controls
        containerComponents.Add(new SeparatorBuilder());

        // Add navigation and control buttons
        var hasNextPage = session.CurrentPage < totalPages - 1;
        var hasPrevPage = session.CurrentPage > 0;

        var navigationRow = new ActionRowBuilder()
            .WithButton("◀️ Previous", $"shop:navigate:{session.SessionId}:prev", 
                ButtonStyle.Secondary, disabled: !hasPrevPage)
            .WithButton("🔍 Filter", $"shop:filter:{session.SessionId}", ButtonStyle.Primary)
            .WithButton("🔄 Refresh", $"shop:refresh:{session.SessionId}", ButtonStyle.Secondary)
            .WithButton("▶️ Next", $"shop:navigate:{session.SessionId}:next", 
                ButtonStyle.Secondary, disabled: !hasNextPage);

        containerComponents.Add(navigationRow);

        // Add type filter dropdown if applicable
        if (ShopConstants.PokemonTypes.Length > 0)
        {
            var typeOptions = ShopConstants.PokemonTypes.Take(20).Select(type =>
                new SelectMenuOptionBuilder(type, type, $"Filter by {type} type",
                    ShopConstants.TypeEmojis.TryGetValue(type, out var emoji) ? emoji.ToIEmote() : null)
            ).ToList();

            var typeSelect = new SelectMenuBuilder()
                .WithCustomId($"shop:type_filter:{session.SessionId}")
                .WithPlaceholder("Filter by Pokemon Type...")
                .WithMinValues(0)
                .WithMaxValues(1)
                .WithOptions(typeOptions);

            var typeSelectRow = new ActionRowBuilder().WithSelectMenu(typeSelect);
            containerComponents.Add(typeSelectRow);
        }

        // Add clear filters button if needed
        if (session.Filters.HasActiveFilters)
        {
            var clearFiltersRow = new ActionRowBuilder()
                .WithButton("❌ Clear Filters", $"shop:clear_filters:{session.SessionId}", ButtonStyle.Danger);
            containerComponents.Add(clearFiltersRow);
        }

        // Create the main container with all components
        var mainContainer = new ContainerBuilder()
            .WithComponents(containerComponents)
            .WithAccentColor(categoryColor);

        var componentsV2 = new ComponentBuilderV2()
            .AddComponent(mainContainer);

        return componentsV2.Build();
    }

}

/// <summary>
///     Simple in-memory shop session manager.
///     In production, you would want to use Redis or a proper cache.
/// </summary>
public static class ShopSessionManager
{
    private static readonly Dictionary<string, ShopSession> Sessions = new();
    private static readonly object Lock = new();

    /// <summary>
    ///     Stores a shop session in memory.
    /// </summary>
    /// <param name="session">The session to store.</param>
    public static void StoreSession(ShopSession session)
    {
        lock (Lock)
        {
            Sessions[session.SessionId] = session;
        }
    }

    /// <summary>
    ///     Retrieves a shop session from memory.
    /// </summary>
    /// <param name="sessionId">The session ID to retrieve.</param>
    /// <returns>The session if found, null otherwise.</returns>
    public static ShopSession? GetSession(string sessionId)
    {
        lock (Lock)
        {
            return Sessions.TryGetValue(sessionId, out var session) ? session : null;
        }
    }

    /// <summary>
    ///     Removes a shop session from memory.
    /// </summary>
    /// <param name="sessionId">The session ID to remove.</param>
    public static void RemoveSession(string sessionId)
    {
        lock (Lock)
        {
            Sessions.Remove(sessionId);
        }
    }

    /// <summary>
    ///     Cleans up expired shop sessions.
    /// </summary>
    /// <param name="maxAge">The maximum age before a session is considered expired.</param>
    public static void CleanupExpiredSessions(TimeSpan maxAge)
    {
        lock (Lock)
        {
            var cutoff = DateTimeOffset.UtcNow - maxAge;
            var expiredSessions = Sessions
                .Where(kvp => kvp.Value.LastAccessed < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in expiredSessions)
            {
                Sessions.Remove(sessionId);
            }
        }
    }
}