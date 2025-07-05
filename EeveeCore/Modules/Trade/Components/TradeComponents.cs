using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Trade.Models;
using EeveeCore.Modules.Trade.Services;
using Serilog;
using TokenType = EeveeCore.Modules.Trade.Models.TokenType;

namespace EeveeCore.Modules.Trade.Components;

/// <summary>
///     Handles interaction components for the trade system.
///     Processes trade confirmation, cancellation, and other trade-related interactions.
/// </summary>
public class TradeComponents : EeveeCoreSlashModuleBase<TradeService>
{
    private readonly ITradeLockService _tradeLockService;
    private readonly TradeEvolutionService _tradeEvolutionService;
    private readonly FraudDetectionService _fraudDetectionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeComponents" /> class.
    /// </summary>
    /// <param name="tradeLockService">The trade lock service.</param>
    /// <param name="tradeEvolutionService">The trade evolution service.</param>
    /// <param name="fraudDetectionService">The comprehensive fraud detection service.</param>
    public TradeComponents(ITradeLockService tradeLockService, TradeEvolutionService tradeEvolutionService, FraudDetectionService fraudDetectionService)
    {
        _tradeLockService = tradeLockService;
        _tradeEvolutionService = tradeEvolutionService;
        _fraudDetectionService = fraudDetectionService;
    }

    /// <summary>
    ///     Handles the "Confirm Trade" button interaction.
    /// </summary>
    /// <param name="sessionId">The trade session ID to confirm.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_confirm:*")]
    public async Task HandleTradeConfirmation(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        await DeferAsync();

        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            // Session not found - likely due to bot restart, clear trade locks for both participants
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await FollowupAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await FollowupAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        if (session.Status != TradeStatus.Active && session.Status != TradeStatus.PendingConfirmation)
        {
            await FollowupAsync("This trade is no longer active.", ephemeral: true);
            return;
        }
        
        // Prevent double-click race condition
        if (session.Status == TradeStatus.Processing)
        {
            await FollowupAsync("This trade is already being processed.", ephemeral: true);
            return;
        }

        if (!session.HasItems())
        {
            await FollowupAsync("Cannot confirm an empty trade. Add items first.", ephemeral: true);
            return;
        }

        // Set user confirmation
        session.SetPlayerConfirmation(ctx.User.Id, true);
        session.Status = TradeStatus.PendingConfirmation;

        var otherPlayerId = session.GetOtherPlayer(ctx.User.Id);
        if (!otherPlayerId.HasValue)
        {
            await FollowupAsync("Error finding the other trader.", ephemeral: true);
            return;
        }

        if (session.IsBothConfirmed())
        {
            // Run fraud detection BEFORE trade execution to prevent delays
            var fraudResult = await _fraudDetectionService.AnalyzeTradeAsync(session);
            
            if (!fraudResult.IsAllowed)
            {
                // Mark session as failed due to fraud detection
                session.Status = TradeStatus.Failed;
                await Service.UpdateSessionInRedisAsync(session);
                
                // Clear trade locks
                await Service.ClearOrphanedTradeLocksAsync(session.Player1Id);
                await Service.ClearOrphanedTradeLocksAsync(session.Player2Id);
                
                var embed = new EmbedBuilder()
                    .WithTitle("❌ Trade Blocked")
                    .WithDescription(fraudResult.Message ?? "Trade blocked due to suspicious activity.")
                    .WithColor(Color.Red)
                    .Build();
                
                await FollowupAsync(embed: embed);
                return;
            }
            
            // Prevent race condition - mark as processing immediately
            session.Status = TradeStatus.Processing;
            await Service.UpdateSessionInRedisAsync(session);
            
            // Both players confirmed and fraud check passed, execute the trade
            var executeResult = await Service.ExecuteTradeAsync(sessionGuid);
            
            if (executeResult.Success)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("🎉 Trade Completed!")
                    .WithDescription("The trade has been completed successfully! Checking for trade evolutions...")
                    .WithColor(Color.Green)
                    .Build();

                await FollowupAsync(embed: embed);

                // Check for trade evolutions
                await ProcessTradeEvolutionsAsync(session);
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithTitle("❌ Trade Failed")
                    .WithDescription(executeResult.Message)
                    .WithColor(Color.Red)
                    .Build();

                await FollowupAsync(embed: embed);
            }
        }
        else
        {
            // Waiting for other player
            var embed = new EmbedBuilder()
                .WithTitle("✅ Trade Confirmed")
                .WithDescription($"You have confirmed the trade. Waiting for <@{otherPlayerId}> to confirm...")
                .WithColor(Color.Orange)
                .WithFooter("The trade will execute once both players confirm.")
                .Build();

            await FollowupAsync(embed: embed);
        }
    }

    /// <summary>
    ///     Handles the "Cancel Trade" button interaction.
    /// </summary>
    /// <param name="sessionId">The trade session ID to cancel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_cancel:*")]
    public async Task HandleTradeCancel(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        // Check if session exists
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            // Session doesn't exist, clear any orphaned locks and exit without response
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            return;
        }

        // Check if session is already cancelled or completed
        if (session.Status is TradeStatus.Cancelled or TradeStatus.Completed or TradeStatus.Failed)
        {
            // Trade is already finished, no response needed
            return;
        }

        var cancelResult = await Service.CancelTradeSessionAsync(sessionGuid, ctx.User.Id);
        
        if (cancelResult.Success)
        {
            await RespondAsync("Trade cancelled successfully! All trade locks have been removed.", ephemeral: true);
        }
        else
        {
            await RespondAsync(cancelResult.Message, ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the "Add Pokemon" button interaction.
    /// </summary>
    /// <param name="sessionId">The trade session ID to add Pokemon to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_add_pokemon:*")]
    public async Task HandleAddPokemon(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        // Check if session exists, if not clear trade locks for both participants
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await RespondAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await RespondAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        // Try to get quick Pokemon select menu first
        var quickPokemonOptions = await GetQuickPokemonSelectOptions(ctx.User.Id);
        
        if (quickPokemonOptions.Any())
        {
            var selectMenu = new SelectMenuBuilder()
                .WithCustomId($"trade_pokemon_select:{sessionId}")
                .WithPlaceholder("Choose a Pokemon to add (or use 'Enter Position' for manual input)...")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(quickPokemonOptions);

            var component = new ComponentBuilder()
                .WithSelectMenu(selectMenu)
                .WithButton("📝 Enter Position", $"trade_pokemon_manual:{sessionId}", ButtonStyle.Secondary)
                .Build();

            await RespondAsync("Select a Pokemon to add to the trade:", components: component, ephemeral: true);
        }
        else
        {
            // Fallback to modal if no tradeable Pokemon found
            await RespondWithModalAsync<TradeAddPokemonModal>($"trade_add_pokemon_modal:{sessionId}");
        }
    }

    /// <summary>
    ///     Handles the "Remove Pokemon" button interaction.
    /// </summary>
    /// <param name="sessionId">The trade session ID to remove Pokemon from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_remove_pokemon:*")]
    public async Task HandleRemovePokemon(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        // Check if session exists, if not clear trade locks for both participants
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await RespondAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await RespondAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        await RespondWithModalAsync<TradeRemovePokemonModal>($"trade_remove_pokemon_modal:{sessionId}");
    }

    /// <summary>
    ///     Handles the "Add Credits" button interaction with select menu.
    /// </summary>
    /// <param name="sessionId">The trade session ID to add credits to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_add_credits:*")]
    public async Task HandleAddCredits(string sessionId)
    {
        try
        {
            if (!Guid.TryParse(sessionId, out var sessionGuid))
            {
                await RespondAsync("Invalid trade session.", ephemeral: true);
                return;
            }

            // Check if session exists, if not clear trade lock
            var session = await Service.GetTradeSessionAsync(sessionGuid);
            if (session == null)
            {
                await _tradeLockService.RemoveTradeLockAsync(ctx.User.Id);
                await RespondAsync("Trade session not found (likely due to bot restart). Your trade lock has been cleared. Please start a new trade.", ephemeral: true);
                return;
            }

            if (!session.IsParticipant(ctx.User.Id))
            {
                await RespondAsync("You are not a participant in this trade.", ephemeral: true);
                return;
            }

            // Create select menu for credit amounts
            var creditOptions = new List<SelectMenuOptionBuilder>
            {
                new SelectMenuOptionBuilder()
                    .WithLabel("500 Credits")
                    .WithValue("500")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 500 credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("1,000 Credits")
                    .WithValue("1000")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 1K credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("2,500 Credits")
                    .WithValue("2500")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 2.5K credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("5,000 Credits")
                    .WithValue("5000")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 5K credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("10,000 Credits")
                    .WithValue("10000")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 10K credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("25,000 Credits")
                    .WithValue("25000")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 25K credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("50,000 Credits")
                    .WithValue("50000")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 50K credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("100,000 Credits")
                    .WithValue("100000")
                    .WithEmote(new Emoji("💰"))
                    .WithDescription("Add 100K credits to the trade"),
                    
                new SelectMenuOptionBuilder()
                    .WithLabel("Custom Amount")
                    .WithValue("custom")
                    .WithEmote(new Emoji("📝"))
                    .WithDescription("Enter a custom credit amount")
            };

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId($"trade_credits_select:{sessionId}")
                .WithPlaceholder("Choose a credit amount to add...")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(creditOptions);

            var component = new ComponentBuilder()
                .WithSelectMenu(selectMenu)
                .Build();

            await RespondAsync("Select the amount of credits to add to the trade:", components: component, ephemeral: true);
        }
        catch (Exception e)
        {
            Log.Information("{Exception}", e);
            throw;
        }
    }

    /// <summary>
    ///     Handles the "Add Tokens" button interaction with select menu.
    /// </summary>
    /// <param name="sessionId">The trade session ID to add tokens to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_add_tokens:*")]
    public async Task HandleAddTokens(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        // Check if session exists, if not clear trade locks for both participants
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await RespondAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await RespondAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        // Create select menu for token types
        var tokenOptions = new List<SelectMenuOptionBuilder>();
        
        foreach (var tokenType in Enum.GetValues<TokenType>())
        {
            tokenOptions.Add(new SelectMenuOptionBuilder()
                .WithLabel($"{tokenType.GetDisplayName()} Tokens")
                .WithValue(tokenType.ToString())
                .WithEmote(tokenType.GetEmoji().ToIEmote())
                .WithDescription($"Add {tokenType.GetDisplayName()} tokens to the trade"));
        }

        var selectMenu = new SelectMenuBuilder()
            .WithCustomId($"trade_token_select:{sessionId}")
            .WithPlaceholder("Choose a token type to add...")
            .WithMinValues(1)
            .WithMaxValues(1)
            .WithOptions(tokenOptions);

        var component = new ComponentBuilder()
            .WithSelectMenu(selectMenu)
            .Build();

        await RespondAsync("Select the type of tokens you want to add:", components: component, ephemeral: true);
    }

    /// <summary>
    ///     Handles token type selection from the select menu.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="values">The selected token type values.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_token_select:*")]
    public async Task HandleTokenSelection(string sessionId, string[] values)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        if (values.Length == 0 || !Enum.TryParse<TokenType>(values[0], out var selectedTokenType))
        {
            await RespondAsync("Invalid token type selected.", ephemeral: true);
            return;
        }

        // Show modal for token amount
        await RespondWithModalAsync<TradeAddTokenAmountModal>($"trade_add_token_amount:{sessionId}:{selectedTokenType}");
    }

    /// <summary>
    ///     Handles the "Remove Tokens" button interaction.
    /// </summary>
    /// <param name="sessionId">The trade session ID to remove tokens from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_remove_tokens:*")]
    public async Task HandleRemoveTokens(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        // Check if session exists, if not clear trade locks for both participants
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await RespondAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await RespondAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        await RespondWithModalAsync<TradeRemoveTokensModal>($"trade_remove_tokens_modal:{sessionId}");
    }

    /// <summary>
    ///     Handles quick credit button interactions.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="amount">The credit amount to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_quick_credits:*:*")]
    public async Task HandleQuickCredits(string sessionId, string amount)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(amount, out var credits))
        {
            await RespondAsync("Invalid credit amount.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Check if session exists, if not clear trade locks for both participants
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await FollowupAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await FollowupAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        var result = await Service.AddCreditsToTradeAsync(sessionGuid, ctx.User.Id, credits);
        
        if (result.Success)
        {
            await FollowupAsync($"Added {credits:N0} credits to the trade.", ephemeral: true);
            
            // Update the trade interface
            if (session != null)
            {
                await Service.UpdateTradeInterfaceAsync(session);
            }
        }
        else
        {
            await FollowupAsync(result.Message, ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the "View Items" button interaction.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_view_items:*")]
    public async Task HandleViewItems(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        // Check if session exists, if not clear trade locks for both participants
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await RespondAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await RespondAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        var userEntries = session.GetEntriesBy(ctx.User.Id).ToList();
        
        if (!userEntries.Any())
        {
            await RespondAsync("You haven't added any items to this trade yet.", ephemeral: true);
            return;
        }

        var itemsDescription = "**Your items in this trade:**\n";
        
        // Group items by type
        var pokemonItems = userEntries.Where(e => e.ItemType == TradeItemType.Pokemon).ToList();
        var creditsTotal = session.GetCreditsBy(ctx.User.Id);
        var tokens = session.GetTokensBy(ctx.User.Id);

        if (pokemonItems.Any())
        {
            itemsDescription += "\n🎯 **Pokemon:**\n";
            foreach (var item in pokemonItems)
            {
                itemsDescription += $"• {item.GetDisplayString()}\n";
            }
        }

        if (creditsTotal > 0)
        {
            itemsDescription += $"\n💰 **Credits:** {creditsTotal:N0}\n";
        }

        if (tokens.Any())
        {
            itemsDescription += "\n🎫 **Tokens:**\n";
            foreach (var (tokenType, count) in tokens)
            {
                itemsDescription += $"• {tokenType.GetEmoji()} {tokenType.GetDisplayName()}: {count}\n";
            }
        }

        var embed = new EmbedBuilder()
            .WithTitle("📋 Your Trade Items")
            .WithDescription(itemsDescription)
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }

    /// <summary>
    ///     Handles the "Remove Last" button interaction.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_remove_last:*")]
    public async Task HandleRemoveLast(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Check if session exists, if not clear trade locks for both participants
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await FollowupAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await FollowupAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        var userEntries = session.GetEntriesBy(ctx.User.Id).ToList();
        
        if (!userEntries.Any())
        {
            await FollowupAsync("You have no items to remove from this trade.", ephemeral: true);
            return;
        }

        // Remove the last added item
        var lastItem = userEntries.Last();
        session.RemoveEntry(lastItem.Id);

        var itemDescription = lastItem.ItemType switch
        {
            TradeItemType.Pokemon => $"Pokemon: {lastItem.GetDisplayString()}",
            TradeItemType.Credits => $"{lastItem.Credits:N0} credits",
            TradeItemType.Tokens => $"{lastItem.TokenCount} {lastItem.TokenType?.GetDisplayName()} tokens",
            _ => "Unknown item"
        };

        await FollowupAsync($"Removed {itemDescription} from your trade.", ephemeral: true);
        
        // Update the trade interface
        await Service.UpdateTradeInterfaceAsync(session);
    }

    /// <summary>
    ///     Handles credit amount selection from the select menu.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="values">The selected credit amount values.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_credits_select:*")]
    public async Task HandleCreditsSelection(string sessionId, string[] values)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        if (values.Length == 0)
        {
            await RespondAsync("Invalid credit selection.", ephemeral: true);
            return;
        }

        var selectedValue = values[0];

        if (selectedValue == "custom")
        {
            // Show modal for custom amount
            await RespondWithModalAsync<TradeAddCreditsModal>($"trade_add_credits_modal:{sessionId}");
            return;
        }

        if (!ulong.TryParse(selectedValue, out var credits))
        {
            await RespondAsync("Invalid credit amount.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        var result = await Service.AddCreditsToTradeAsync(sessionGuid, ctx.User.Id, credits);
        
        if (result.Success)
        {
            await FollowupAsync($"Added {credits:N0} credits to the trade.", ephemeral: true);
            
            // Update the trade interface
            var session = await Service.GetTradeSessionAsync(sessionGuid);
            if (session != null)
            {
                await Service.UpdateTradeInterfaceAsync(session);
            }
        }
        else
        {
            await FollowupAsync(result.Message, ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the "Custom" credit button interaction (opens modal directly).
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_custom_credits:*")]
    public async Task HandleCustomCredits(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        // Check if session exists
        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
            await RespondAsync("Trade session not found (likely due to bot restart). Trade locks have been cleared for all participants. Please start a new trade.", ephemeral: true);
            return;
        }

        if (!session.IsParticipant(ctx.User.Id))
        {
            await RespondAsync("You are not a participant in this trade.", ephemeral: true);
            return;
        }

        // Directly open the custom credits modal
        await RespondWithModalAsync<TradeAddCreditsModal>($"trade_add_credits_modal:{sessionId}");
    }

    /// <summary>
    ///     Handles Pokemon selection from the quick select menu.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="values">The selected Pokemon position values.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_pokemon_select:*")]
    public async Task HandlePokemonSelection(string sessionId, string[] values)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        if (values.Length == 0 || !int.TryParse(values[0], out var pokemonPosition))
        {
            await RespondAsync("Invalid Pokemon selection.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        var result = await Service.AddPokemonToTradeAsync(sessionGuid, ctx.User.Id, pokemonPosition);
        
        if (result.Success)
        {
            await FollowupAsync($"Added Pokemon at position {pokemonPosition} to the trade.", ephemeral: true);
            
            // Update the trade interface
            var session = await Service.GetTradeSessionAsync(sessionGuid);
            if (session != null)
            {
                await Service.UpdateTradeInterfaceAsync(session);
            }
        }
        else
        {
            await FollowupAsync(result.Message, ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the manual Pokemon entry button.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("trade_pokemon_manual:*")]
    public async Task HandlePokemonManualEntry(string sessionId)
    {
        await RespondWithModalAsync<TradeAddPokemonModal>($"trade_add_pokemon_modal:{sessionId}");
    }

    /// <summary>
    ///     Gets quick Pokemon select options for a user.
    /// </summary>
    /// <param name="userId">The user ID to get Pokemon for.</param>
    /// <returns>A list of select menu options for the user's tradeable Pokemon.</returns>
    private async Task<List<SelectMenuOptionBuilder>> GetQuickPokemonSelectOptions(ulong userId)
    {
        try
        {
            var userPokemon = await Service.GetUserTradeablePokemonAsync(userId);
            var options = new List<SelectMenuOptionBuilder>();
            
            foreach (var pokemon in userPokemon)
            {
                var displayName = CreatePokemonDisplayName(pokemon.PokemonName, pokemon.Nickname, pokemon.Position, pokemon.Level, pokemon.Shiny, pokemon.Radiant);
                var description = $"Level {pokemon.Level} • Position #{pokemon.Position}";
                
                // Add special indicators to description
                if (pokemon.Radiant == true)
                    description += " • Radiant 💎";
                else if (pokemon.Shiny == true)
                    description += " • Shiny ✨";

                options.Add(new SelectMenuOptionBuilder()
                    .WithLabel(displayName.Length > 100 ? displayName[..97] + "..." : displayName)
                    .WithValue(pokemon.Position.ToString())
                    .WithDescription(description.Length > 100 ? description[..97] + "..." : description));
            }

            return options;
        }
        catch (Exception ex)
        {
            Log.Information($"Error getting quick Pokemon options: {ex.Message}");
            return new List<SelectMenuOptionBuilder>();
        }
    }

    /// <summary>
    ///     Creates a display name for Pokemon in select menus.
    /// </summary>
    /// <param name="pokemonName">The Pokemon species name.</param>
    /// <param name="nickname">The Pokemon's nickname.</param>
    /// <param name="position">The position in user's collection.</param>
    /// <param name="level">The Pokemon's level.</param>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <returns>A formatted display string for the select menu option.</returns>
    private static string CreatePokemonDisplayName(string pokemonName, string nickname, ulong position, int level, bool? shiny, bool? radiant)
    {
        var name = string.IsNullOrEmpty(nickname) || nickname == pokemonName ? pokemonName : $"{nickname} ({pokemonName})";
        var specialIndicator = "";
        
        if (radiant == true)
            specialIndicator = " 💎";
        else if (shiny == true)
            specialIndicator = " ✨";
            
        return $"{name}{specialIndicator}";
    }

    /// <summary>
    ///     Processes trade evolutions for all Pokemon that were traded.
    /// </summary>
    /// <param name="session">The completed trade session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ProcessTradeEvolutionsAsync(TradeSession session)
    {
        try
        {
            // Check evolutions for Pokemon that went to Player 1
            foreach (var pokemonEntry in session.GetPokemonBy(session.Player2Id))
            {
                await _tradeEvolutionService.CheckAndProcessTradeEvolutionAsync(
                    pokemonEntry.PokemonId!.Value, session.Player1Id, ctx.Interaction);
            }

            // Check evolutions for Pokemon that went to Player 2
            foreach (var pokemonEntry in session.GetPokemonBy(session.Player1Id))
            {
                await _tradeEvolutionService.CheckAndProcessTradeEvolutionAsync(
                    pokemonEntry.PokemonId!.Value, session.Player2Id, ctx.Interaction);
            }
        }
        catch (Exception ex)
        {
            // Evolution errors should not break the trade completion message
            Log.Information($"Error processing trade evolutions: {ex.Message}");
        }
    }
}

#region Modal Classes

/// <summary>
///     Modal for adding Pokemon to a trade.
/// </summary>
public class TradeAddPokemonModal : IModal
{
    /// <summary>
    ///     Gets or sets the Pokemon position input.
    /// </summary>
    [InputLabel("Pokemon Position")]
    [ModalTextInput("pokemon_position", TextInputStyle.Short, "Position number of the Pokemon to add")]
    public string? PokemonPosition { get; set; }

    /// <inheritdoc />
    public string Title => "Add Pokemon to Trade";
}

/// <summary>
///     Modal for removing Pokemon from a trade.
/// </summary>
public class TradeRemovePokemonModal : IModal
{
    /// <summary>
    ///     Gets or sets the Pokemon position input.
    /// </summary>
    [InputLabel("Pokemon Position")]
    [ModalTextInput("pokemon_position", TextInputStyle.Short, "Position number of the Pokemon to remove")]
    public string? PokemonPosition { get; set; }

    /// <inheritdoc />
    public string Title => "Remove Pokemon from Trade";
}

/// <summary>
///     Modal for adding credits to a trade.
/// </summary>
public class TradeAddCreditsModal : IModal
{
    /// <summary>
    ///     Gets or sets the credits amount input.
    /// </summary>
    [InputLabel("Credits Amount")]
    [ModalTextInput("credits_amount", TextInputStyle.Short, "Number of credits to add (k = 1000, m = 1000000)")]
    public string? CreditsAmount { get; set; }

    /// <inheritdoc />
    public string Title => "Add Credits to Trade";
}

/// <summary>
///     Modal for adding tokens to a trade.
/// </summary>
public class TradeAddTokensModal : IModal
{
    /// <summary>
    ///     Gets or sets the token type input.
    /// </summary>
    [InputLabel("Token Type")]
    [ModalTextInput("token_type", TextInputStyle.Short, "Type of tokens (Fire, Water, etc.)")]
    public string? TokenType { get; set; }

    /// <summary>
    ///     Gets or sets the token count input.
    /// </summary>
    [InputLabel("Token Count")]
    [ModalTextInput("token_count", TextInputStyle.Short, "Number of tokens to add")]
    public string? TokenCount { get; set; }

    /// <inheritdoc />
    public string Title => "Add Tokens to Trade";
}

/// <summary>
///     Modal for removing tokens from a trade.
/// </summary>
public class TradeRemoveTokensModal : IModal
{
    /// <summary>
    ///     Gets or sets the token type input.
    /// </summary>
    [InputLabel("Token Type")]
    [ModalTextInput("token_type", TextInputStyle.Short, "Type of tokens to remove (Fire, Water, etc.)")]
    public string? TokenType { get; set; }

    /// <summary>
    ///     Gets or sets the token count input.
    /// </summary>
    [InputLabel("Token Count")]
    [ModalTextInput("token_count", TextInputStyle.Short, "Number of tokens to remove")]
    public string? TokenCount { get; set; }

    /// <inheritdoc />
    public string Title => "Remove Tokens from Trade";
}

/// <summary>
///     Modal for entering token amount after selecting token type.
/// </summary>
public class TradeAddTokenAmountModal : IModal
{
    /// <summary>
    ///     Gets or sets the token count input.
    /// </summary>
    [InputLabel("Token Amount")]
    [ModalTextInput("token_amount", TextInputStyle.Short, "Number of tokens to add")]
    public string? TokenAmount { get; set; }

    /// <inheritdoc />
    public string Title => "Add Tokens to Trade";
}

#endregion