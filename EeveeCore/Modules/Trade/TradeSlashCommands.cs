using Discord.Interactions;
using EeveeCore.Common.AutoCompletes;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Trade.Models;
using EeveeCore.Modules.Trade.Services;
using Fergun.Interactive;
using Serilog;

namespace EeveeCore.Modules.Trade;

/// <summary>
///     Provides Discord slash commands for trading functionality.
///     Handles trade initiation, management, and user interactions.
/// </summary>
/// <param name="interactivity">Service for handling interactive components like pagination.</param>
/// <param name="tradeLockService">The trade lock service.</param>
public class TradeSlashCommands(InteractiveService interactivity, ITradeLockService tradeLockService)
    : EeveeCoreSlashModuleBase<TradeService>
{
    /// <summary>
    ///     Initiates a trade with another user.
    /// </summary>
    /// <param name="user">The user to trade with.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("trade", "Begin a trade with another user!")]
    public async Task StartTrade(
        [Summary("user", "The user to begin the trade with")] IUser user)
    {
        try
        {
            if (ctx.User.Id == user.Id)
            {
                await RespondAsync("You cannot trade with yourself!", ephemeral: true);
                return;
            }

            if (user.IsBot)
            {
                await RespondAsync("You cannot trade with bots!", ephemeral: true);
                return;
            }

            await DeferAsync();

            // Check if either user is already trade locked
            if (await tradeLockService.IsUserTradeLockedAsync(ctx.User.Id))
            {
                await FollowupAsync(
                    $"{ctx.User.Username} is currently in a trade! Use `/canceltrades` if you think this is an error.",
                    ephemeral: true);
                return;
            }

            if (await tradeLockService.IsUserTradeLockedAsync(user.Id))
            {
                await FollowupAsync(
                    $"{user.Username} is currently in a trade! They can use `/canceltrades` if they think this is an error.",
                    ephemeral: true);
                return;
            }

            // Create trade confirmation embed
            var confirmEmbed = new EmbedBuilder()
                .WithTitle("🔄 Trade Request")
                .WithDescription($"{ctx.User.Mention} has requested a trade with {user.Mention}!")
                .WithColor(Color.Blue)
                .WithFooter($"Waiting for {user.Username} to accept...")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            var confirmButton = new ComponentBuilder()
                .WithButton("Accept Trade", $"trade_accept:{ctx.User.Id}", ButtonStyle.Success, new Emoji("✅"))
                .WithButton("Decline Trade", $"trade_decline:{ctx.User.Id}", ButtonStyle.Danger, new Emoji("❌"))
                .Build();

            var confirmMessage = await FollowupAsync(embed: confirmEmbed, components: confirmButton);

            // Wait for response with timeout
            var response = await interactivity.NextInteractionAsync(
                x => x is IComponentInteraction component &&
                     component.Data.CustomId.StartsWith("trade_") &&
                     component.User.Id == user.Id &&
                     component.Message.Id == confirmMessage.Id,
                timeout: TimeSpan.FromMinutes(2));

            if (response is { IsSuccess: true, Value: IComponentInteraction componentInteraction })
            {
                if (componentInteraction.Data.CustomId.StartsWith("trade_accept:"))
                {
                    await componentInteraction.DeferAsync();
                    await StartTradeSession(ctx.User.Id, user.Id, confirmMessage);
                }
                else
                {
                    await componentInteraction.RespondAsync("Trade request declined.", ephemeral: true);

                    var declineEmbed = new EmbedBuilder()
                        .WithTitle("❌ Trade Declined")
                        .WithDescription($"{user.Mention} declined the trade request.")
                        .WithColor(Color.Red)
                        .Build();

                    await confirmMessage.ModifyAsync(x =>
                    {
                        x.Embed = declineEmbed;
                        x.Components = new ComponentBuilder().Build();
                    });
                }
            }
            else
            {
                // Timeout
                var timeoutEmbed = new EmbedBuilder()
                    .WithTitle("⏰ Trade Request Expired")
                    .WithDescription($"{user.Mention} took too long to respond to the trade request.")
                    .WithColor(Color.Orange)
                    .Build();

                await confirmMessage.ModifyAsync(x =>
                {
                    x.Embed = timeoutEmbed;
                    x.Components = new ComponentBuilder().Build();
                });
            }
        }
        catch (Exception e)
        {
            Log.Information("{Exception}", e);
            throw;
        }
    }

    /// <summary>
    ///     Initiates a quick trade with a specific Pokemon.
    /// </summary>
    /// <param name="user">The user to trade with.</param>
    /// <param name="pokemon">The Pokemon to offer in the trade.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("quicktrade", "Quickly start a trade with a specific Pokemon!")]
    public async Task QuickTrade(
        [Summary("user", "The user to trade with")] IUser user,
        [Summary("pokemon", "The Pokemon you want to trade"), Autocomplete(typeof(PokemonAutocompleteHandler))] string pokemon)
    {
        if (ctx.User.Id == user.Id)
        {
            await RespondAsync("You cannot trade with yourself!", ephemeral: true);
            return;
        }

        if (user.IsBot)
        {
            await RespondAsync("You cannot trade with bots!", ephemeral: true);
            return;
        }

        if (!int.TryParse(pokemon, out var pokemonPosition))
        {
            await RespondAsync("Invalid Pokemon selection. Please try again.", ephemeral: true);
            return;
        }

        await DeferAsync();

        // Check if either user is already trade locked
        if (await tradeLockService.IsUserTradeLockedAsync(ctx.User.Id))
        {
            await FollowupAsync($"{ctx.User.Username} is currently in a trade! Use `/canceltrades` if you think this is an error.", ephemeral: true);
            return;
        }

        if (await tradeLockService.IsUserTradeLockedAsync(user.Id))
        {
            await FollowupAsync($"{user.Username} is currently in a trade! They can use `/canceltrades` if they think this is an error.", ephemeral: true);
            return;
        }

        // Create trade confirmation embed with Pokemon info
        var confirmEmbed = new EmbedBuilder()
            .WithTitle("🔄 Quick Trade Request")
            .WithDescription($"{ctx.User.Mention} wants to trade their Pokemon at position **{pokemonPosition}** with {user.Mention}!")
            .WithColor(Color.Blue)
            .WithFooter($"Waiting for {user.Username} to accept...")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        var confirmButton = new ComponentBuilder()
            .WithButton("Accept Trade", $"quicktrade_accept:{ctx.User.Id}:{pokemonPosition}", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Decline Trade", $"quicktrade_decline:{ctx.User.Id}", ButtonStyle.Danger, new Emoji("❌"))
            .Build();

        var confirmMessage = await FollowupAsync(embed: confirmEmbed, components: confirmButton);

        // Wait for response with timeout
        var response = await interactivity.NextInteractionAsync(
            x => x is IComponentInteraction component &&
                 component.Data.CustomId.StartsWith("quicktrade_") &&
                 component.User.Id == user.Id &&
                 component.Message.Id == confirmMessage.Id,
            timeout: TimeSpan.FromMinutes(2));

        if (response is { IsSuccess: true, Value: IComponentInteraction componentInteraction })
        {
            if (componentInteraction.Data.CustomId.StartsWith("quicktrade_accept:"))
            {
                await componentInteraction.DeferAsync();
                await StartQuickTradeSession(ctx.User.Id, user.Id, pokemonPosition, confirmMessage);
            }
            else
            {
                await componentInteraction.RespondAsync("Quick trade request declined.", ephemeral: true);
                
                var declineEmbed = new EmbedBuilder()
                    .WithTitle("❌ Quick Trade Declined")
                    .WithDescription($"{user.Mention} declined the quick trade request.")
                    .WithColor(Color.Red)
                    .Build();

                await confirmMessage.ModifyAsync(x =>
                {
                    x.Embed = declineEmbed;
                    x.Components = new ComponentBuilder().Build();
                });
            }
        }
        else
        {
            // Timeout
            var timeoutEmbed = new EmbedBuilder()
                .WithTitle("⏰ Quick Trade Request Expired")
                .WithDescription($"{user.Mention} took too long to respond to the quick trade request.")
                .WithColor(Color.Orange)
                .Build();

            await confirmMessage.ModifyAsync(x =>
            {
                x.Embed = timeoutEmbed;
                x.Components = new ComponentBuilder().Build();
            });
        }
    }

    /// <summary>
    ///     Starts a quick trade session with a pre-selected Pokemon.
    /// </summary>
    /// <param name="player1Id">The first player's ID.</param>
    /// <param name="player2Id">The second player's ID.</param>
    /// <param name="pokemonPosition">The position of the Pokemon to add to the trade.</param>
    /// <param name="message">The message to update with trade interface.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task StartQuickTradeSession(ulong player1Id, ulong player2Id, int pokemonPosition, IUserMessage message)
    {
        var sessionResult = await Service.CreateTradeSessionAsync(
            player1Id, player2Id, ctx.Channel.Id, ctx.Guild.Id);

        if (!sessionResult.Success)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Quick Trade Failed")
                .WithDescription(sessionResult.Message)
                .WithColor(Color.Red)
                .Build();

            await message.ModifyAsync(x =>
            {
                x.Embed = errorEmbed;
                x.Components = new ComponentBuilder().Build();
            });
            return;
        }

        var session = (TradeSession)sessionResult.Data!;

        // Lock both users
        await tradeLockService.AddTradeLockAsync(player1Id);
        await tradeLockService.AddTradeLockAsync(player2Id);

        // Add the Pokemon to the trade
        var addResult = await Service.AddPokemonToTradeAsync(session.SessionId, player1Id, pokemonPosition);
        if (!addResult.Success)
        {
            // If we can't add the Pokemon, cancel the trade
            await Service.CancelTradeSessionAsync(session.SessionId, player1Id);
            await tradeLockService.RemoveTradeLockAsync(player1Id);
            await tradeLockService.RemoveTradeLockAsync(player2Id);

            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Quick Trade Failed")
                .WithDescription($"Could not add Pokemon to trade: {addResult.Message}")
                .WithColor(Color.Red)
                .Build();

            await message.ModifyAsync(x =>
            {
                x.Embed = errorEmbed;
                x.Components = new ComponentBuilder().Build();
            });
            return;
        }

        // Create trade interface
        await UpdateTradeInterface(session, message);
    }

    /// <summary>
    ///     Starts an actual trade session after acceptance.
    /// </summary>
    /// <param name="player1Id">The first player's ID.</param>
    /// <param name="player2Id">The second player's ID.</param>
    /// <param name="message">The message to update with trade interface.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task StartTradeSession(ulong player1Id, ulong player2Id, IUserMessage message)
    {
        var sessionResult = await Service.CreateTradeSessionAsync(
            player1Id, player2Id, ctx.Channel.Id, ctx.Guild.Id);

        if (!sessionResult.Success)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Trade Failed")
                .WithDescription(sessionResult.Message)
                .WithColor(Color.Red)
                .Build();

            await message.ModifyAsync(x =>
            {
                x.Embed = errorEmbed;
                x.Components = new ComponentBuilder().Build();
            });
            return;
        }

        var session = (TradeSession)sessionResult.Data!;

        // Lock both users
        await tradeLockService.AddTradeLockAsync(player1Id);
        await tradeLockService.AddTradeLockAsync(player2Id);

        // Create trade interface
        await UpdateTradeInterface(session, message);
    }

    /// <summary>
    ///     Updates the trade interface with current trade state.
    /// </summary>
    /// <param name="session">The trade session to display.</param>
    /// <param name="message">The message to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task UpdateTradeInterface(TradeSession session, IUserMessage message)
    {
        var tradeSummary = await Service.GenerateTradeSummaryAsync(session);
        
        var embed = new EmbedBuilder()
            .WithTitle("🔄 Active Trade Session")
            .WithDescription(tradeSummary)
            .WithColor(Color.Blue)
            .WithFooter($"Session ID: {session.SessionId}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        var components = new ComponentBuilder()
            // Row 1: Add Items
            .WithButton("🎯 Add Pokemon", $"trade_add_pokemon:{session.SessionId}", ButtonStyle.Secondary, row: 0)
            .WithButton("💰 Add Credits", $"trade_add_credits:{session.SessionId}", ButtonStyle.Secondary, row: 0)
            .WithButton("🎫 Add Tokens", $"trade_add_tokens:{session.SessionId}", ButtonStyle.Secondary, row: 0)
            
            // Row 2: Quick Credits
            .WithButton("1K", $"trade_quick_credits:{session.SessionId}:1000", ButtonStyle.Primary, row: 1)
            .WithButton("5K", $"trade_quick_credits:{session.SessionId}:5000", ButtonStyle.Primary, row: 1)
            .WithButton("10K", $"trade_quick_credits:{session.SessionId}:10000", ButtonStyle.Primary, row: 1)
            .WithButton("Custom", $"trade_custom_credits:{session.SessionId}", ButtonStyle.Secondary, row: 1)
            
            // Row 3: Management
            .WithButton("📋 View Items", $"trade_view_items:{session.SessionId}", ButtonStyle.Secondary, row: 2)
            .WithButton("🗑️ Remove Last", $"trade_remove_last:{session.SessionId}", ButtonStyle.Secondary, row: 2)
            .WithButton("🔄 Remove Tokens", $"trade_remove_tokens:{session.SessionId}", ButtonStyle.Secondary, row: 2)
            
            // Row 4: Confirmation
            .WithButton("✅ Confirm Trade", $"trade_confirm:{session.SessionId}", ButtonStyle.Success, row: 3, 
                disabled: !session.HasItems())
            .WithButton("❌ Cancel Trade", $"trade_cancel:{session.SessionId}", ButtonStyle.Danger, row: 3)
            .Build();

        session.TradeMessage = message;
        
        await message.ModifyAsync(x =>
        {
            x.Embed = embed;
            x.Components = components;
        });
    }

    /// <summary>
    ///     Shows the user's current trade sessions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("mytrades", "View your current trade sessions")]
    public async Task ViewMyTrades()
    {
        await DeferAsync(ephemeral: true);

        // This would require additional tracking in TradeService
        // For now, just show if user is trade locked
        var isLocked = await tradeLockService.IsUserTradeLockedAsync(ctx.User.Id);
        
        if (isLocked)
        {
            await FollowupAsync("You are currently in an active trade session.", ephemeral: true);
        }
        else
        {
            await FollowupAsync("You are not currently in any trade sessions.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Force cancels all the user's trade sessions (for debugging/admin purposes).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("canceltrades", "Cancel all your trade sessions")]
    public async Task CancelMyTrades()
    {
        await DeferAsync(ephemeral: true);

        // Clear all trade locks for this user (more robust than single remove)
        await tradeLockService.ClearAllTradeLocksAsync(ctx.User.Id);
        
        // Also clear any orphaned session data
        await Service.ClearOrphanedTradeLocksAsync(ctx.User.Id);
        
        await FollowupAsync("All your trade sessions have been cancelled and trade locks cleared.", ephemeral: true);
    }

    /// <summary>
    ///     Shows help information about trading.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("tradehelp", "Get help with trading")]
    public async Task TradeHelp()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🔄 Trading Help")
            .WithDescription("Learn how to trade with other users!")
            .WithColor(Color.Blue)
            .AddField("**Starting a Trade**", 
                "Use `/trade @user` to request a trade with another user. They must accept before the trade begins.")
            .AddField("**Adding Items**", 
                "• **Pokemon**: Click 'Add Pokemon' and enter position numbers (e.g., `5` or `2,5,10`)\n" +
                "• **Credits**: Click 'Add Credits' and enter amount (supports `k` and `m` for thousands/millions)\n" +
                "• **Tokens**: Click 'Add Tokens' and specify type and amount")
            .AddField("**Removing Items**", 
                "• **Pokemon**: Click 'Remove Pokemon' and enter position numbers\n" +
                "• **Tokens**: Click 'Remove Tokens' and specify type and amount to remove")
            .AddField("**Completing the Trade**", 
                "Once both users have added items, click 'Confirm Trade'. Both users must confirm before the trade executes.")
            .AddField("**Trade Evolution**", 
                "Some Pokemon evolve when traded! The bot will automatically check and evolve eligible Pokemon.")
            .AddField("**Important Notes**", 
                "• You cannot trade your #1 Pokemon\n" +
                "• You cannot trade eggs or favorited Pokemon\n" +
                "• Pokemon on the market cannot be traded\n" +
                "• Trade sessions expire after 6 minutes of inactivity")
            .WithFooter("Use `/gift` commands to give items without expecting anything in return!")
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}