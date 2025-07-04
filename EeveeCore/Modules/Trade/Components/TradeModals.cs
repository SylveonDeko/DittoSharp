using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Trade.Models;
using EeveeCore.Modules.Trade.Services;
using TokenType = EeveeCore.Modules.Trade.Models.TokenType;

namespace EeveeCore.Modules.Trade.Components;

/// <summary>
///     Handles modal submissions for trade operations.
///     Processes user input from modals for adding/removing items from trades.
/// </summary>
public class TradeModals : EeveeCoreSlashModuleBase<TradeService>
{
    /// <summary>
    ///     Handles the submission of the add Pokemon modal.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("trade_add_pokemon_modal:*")]
    public async Task HandleAddPokemonModal(string sessionId, TradeAddPokemonModal modal)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Parse Pokemon positions (support multiple IDs separated by spaces or commas)
        var positionsInput = modal.PokemonPosition.Trim().Replace(" ", ",");
        var positions = new List<int>();

        foreach (var posStr in positionsInput.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(posStr.Trim(), out var pos) && !positions.Contains(pos))
            {
                positions.Add(pos);
            }
        }

        if (!positions.Any())
        {
            await FollowupAsync("Please enter valid Pokemon position numbers.", ephemeral: true);
            return;
        }

        var results = new List<string>();
        var errors = new List<string>();

        // Add each Pokemon
        foreach (var position in positions)
        {
            if (position == 1)
            {
                errors.Add("You cannot give away your Number 1 Pokemon");
                continue;
            }

            var result = await Service.AddPokemonToTradeAsync(sessionGuid, ctx.User.Id, position);
            if (result.Success)
            {
                results.Add($"Added Pokemon at position {position}");
            }
            else
            {
                errors.Add($"Position {position}: {result.Message}");
            }
        }

        // Build response message
        var responseMessage = "";
        if (results.Any())
        {
            responseMessage += string.Join("\n", results);
        }
        if (errors.Any())
        {
            if (results.Any()) responseMessage += "\n\n**Errors:**\n";
            responseMessage += string.Join("\n", errors);
        }

        if (string.IsNullOrEmpty(responseMessage))
        {
            responseMessage = "No Pokemon were added to the trade.";
        }

        await FollowupAsync(responseMessage, ephemeral: true);

        // Update the trade interface if any Pokemon were added successfully
        if (results.Any())
        {
            var session = await Service.GetTradeSessionAsync(sessionGuid);
            if (session != null)
            {
                await Service.UpdateTradeInterfaceAsync(session);
            }
        }
    }

    /// <summary>
    ///     Handles the submission of the remove Pokemon modal.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("trade_remove_pokemon_modal:*")]
    public async Task HandleRemovePokemonModal(string sessionId, TradeRemovePokemonModal modal)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Parse Pokemon positions
        var positionsInput = modal.PokemonPosition.Replace(" ", ",");
        var positions = new List<ulong>();

        foreach (var posStr in positionsInput.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (ulong.TryParse(posStr.Trim(), out var pos) && !positions.Contains(pos))
            {
                positions.Add(pos);
            }
        }

        if (!positions.Any())
        {
            await FollowupAsync("Please enter valid Pokemon position numbers.", ephemeral: true);
            return;
        }

        var results = new List<string>();
        var errors = new List<string>();

        // Remove each Pokemon
        foreach (var position in positions)
        {
            var result = await Service.RemovePokemonFromTradeAsync(sessionGuid, ctx.User.Id, position);
            if (result.Success)
            {
                results.Add($"Removed Pokemon at position {position}");
            }
            else
            {
                errors.Add($"Position {position}: {result.Message}");
            }
        }

        // Build response message
        var responseMessage = "";
        if (results.Any())
        {
            responseMessage += string.Join("\n", results);
        }
        if (errors.Any())
        {
            if (results.Any()) responseMessage += "\n\n**Errors:**\n";
            responseMessage += string.Join("\n", errors);
        }

        if (string.IsNullOrEmpty(responseMessage))
        {
            responseMessage = "No Pokemon were removed from the trade.";
        }

        await FollowupAsync(responseMessage, ephemeral: true);

        // Update the trade interface if any Pokemon were removed successfully
        if (results.Any())
        {
            var session = await Service.GetTradeSessionAsync(sessionGuid);
            if (session != null)
            {
                await Service.UpdateTradeInterfaceAsync(session);
            }
        }
    }

    /// <summary>
    ///     Handles the submission of the add credits modal.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("trade_add_credits_modal:*")]
    public async Task HandleAddCreditsModal(string sessionId, TradeAddCreditsModal modal)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Parse credits amount (support k and m suffixes)
        if (string.IsNullOrWhiteSpace(modal.CreditsAmount))
        {
            await FollowupAsync("Please enter a valid number of credits.", ephemeral: true);
            return;
        }

        var creditsInput = modal.CreditsAmount
            .ToLower()
            .Replace(" ", "")
            .Replace("k", "000")
            .Replace("m", "000000");

        if (!ulong.TryParse(creditsInput, out var credits))
        {
            await FollowupAsync($"Please enter a valid number of credits: {modal.CreditsAmount}", ephemeral: true);
            return;
        }

        if (credits == 0)
        {
            await FollowupAsync("You can't trade 0 credits!", ephemeral: true);
            return;
        }

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
    ///     Handles the submission of the add tokens modal.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("trade_add_tokens_modal:*")]
    public async Task HandleAddTokensModal(string sessionId, TradeAddTokensModal modal)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Parse token type
        if (!TokenTypeExtensions.TryParse(modal.TokenType, out var tokenType))
        {
            await FollowupAsync($"Invalid token type: {modal.TokenType}. Please enter a valid type like Fire, Water, etc.", ephemeral: true);
            return;
        }

        // Parse token count
        if (!int.TryParse(modal.TokenCount.Trim(), out var tokenCount) || tokenCount <= 0)
        {
            await FollowupAsync("Invalid token count, please enter a positive number.", ephemeral: true);
            return;
        }

        var result = await Service.AddTokensToTradeAsync(sessionGuid, ctx.User.Id, tokenType, tokenCount);
        
        if (result.Success)
        {
            await FollowupAsync($"Added {tokenCount} {tokenType.GetDisplayName()} tokens to the trade.", ephemeral: true);
            
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
    ///     Handles the submission of the remove tokens modal.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("trade_remove_tokens_modal:*")]
    public async Task HandleRemoveTokensModal(string sessionId, TradeRemoveTokensModal modal)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Parse token type
        if (!TokenTypeExtensions.TryParse(modal.TokenType, out var tokenType))
        {
            await FollowupAsync($"Invalid token type: {modal.TokenType}. Please enter a valid type like Fire, Water, etc.", ephemeral: true);
            return;
        }

        // Parse token count
        if (!int.TryParse(modal.TokenCount.Trim(), out var tokenCount) || tokenCount <= 0)
        {
            await FollowupAsync("Invalid token count, please enter a positive number.", ephemeral: true);
            return;
        }

        var session = await Service.GetTradeSessionAsync(sessionGuid);
        if (session == null)
        {
            await FollowupAsync("Trade session not found.", ephemeral: true);
            return;
        }

        // Find and remove token entries
        var userTokens = session.GetTokensBy(ctx.User.Id);
        if (!userTokens.TryGetValue(tokenType, out var currentAmount) || currentAmount < tokenCount)
        {
            await FollowupAsync($"You don't have {tokenCount} {tokenType.GetDisplayName()} tokens in this trade!", ephemeral: true);
            return;
        }

        // Remove token entries
        var entriesToRemove = session.GetEntriesBy(ctx.User.Id)
            .Where(e => e.ItemType == TradeItemType.Tokens && e.TokenType == tokenType)
            .ToList();

        var totalToRemove = tokenCount;
        foreach (var entry in entriesToRemove)
        {
            if (totalToRemove <= 0) break;

            if (entry.TokenCount <= totalToRemove)
            {
                totalToRemove -= entry.TokenCount;
                session.RemoveEntry(entry.Id);
            }
            else
            {
                entry.TokenCount -= totalToRemove;
                totalToRemove = 0;
            }
        }

        await FollowupAsync($"Removed {tokenCount} {tokenType.GetDisplayName()} tokens from the trade.", ephemeral: true);

        // Update the trade interface
        var updatedSession = await Service.GetTradeSessionAsync(sessionGuid);
        if (updatedSession != null)
        {
            await Service.UpdateTradeInterfaceAsync(updatedSession);
        }
    }

    /// <summary>
    ///     Handles the submission of the token amount modal (from select menu workflow).
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="tokenType">The selected token type.</param>
    /// <param name="modal">The submitted modal data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ModalInteraction("trade_add_token_amount:*:*")]
    public async Task HandleAddTokenAmountModal(string sessionId, string tokenType, TradeAddTokenAmountModal modal)
    {
        if (!Guid.TryParse(sessionId, out var sessionGuid))
        {
            await RespondAsync("Invalid trade session.", ephemeral: true);
            return;
        }

        if (!Enum.TryParse<TokenType>(tokenType, out var selectedTokenType))
        {
            await RespondAsync("Invalid token type.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        // Parse token count
        if (!int.TryParse(modal.TokenAmount?.Trim(), out var tokenCount) || tokenCount <= 0)
        {
            await FollowupAsync("Invalid token count, please enter a positive number.", ephemeral: true);
            return;
        }

        var result = await Service.AddTokensToTradeAsync(sessionGuid, ctx.User.Id, selectedTokenType, tokenCount);
        
        if (result.Success)
        {
            await FollowupAsync($"Added {tokenCount} {selectedTokenType.GetDisplayName()} tokens to the trade.", ephemeral: true);
            
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
}