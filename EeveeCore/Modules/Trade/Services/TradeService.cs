using System.Text;
using System.Text.Json;
using EeveeCore.Modules.Trade.Models;
using Serilog;
using TokenType = EeveeCore.Modules.Trade.Models.TokenType;
using LinqToDB;
using LinqToDB.Async;
using UserPokemonOwnership = EeveeCore.Database.Linq.Models.Pokemon.UserPokemonOwnership;

namespace EeveeCore.Modules.Trade.Services;

/// <summary>
///     Service for managing trade sessions between users.
///     Handles session creation, item management, validation, and trade execution.
/// </summary>
public class TradeService : INService
{
    private readonly LinqToDbConnectionProvider _context;
    private readonly IDataCache _cache;
    private readonly DiscordShardedClient _discordClient;
    private readonly ITradeLockService _tradeLockService;
    private readonly TradeEvolutionService _evolutionService;
    
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, TradeSession> _activeSessions = new();
    
    private const string TradeSessionPrefix = "trade_session:";
    private const int SessionTimeoutMinutes = 6;
    private const ulong LogChannelId = 1004571710323957830;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeService" /> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cache">The Redis cache service.</param>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="tradeLockService">The trade lock service.</param>
    /// <param name="evolutionService">The trade evolution service.</param>
    public TradeService(LinqToDbConnectionProvider context, IDataCache cache, DiscordShardedClient discordClient, ITradeLockService tradeLockService, TradeEvolutionService evolutionService)
    {
        _context = context;
        _cache = cache;
        _discordClient = discordClient;
        _tradeLockService = tradeLockService;
        _evolutionService = evolutionService;
    }

    /// <summary>
    ///     Creates a new trade session between two users.
    /// </summary>
    /// <param name="player1Id">The first player's Discord user ID.</param>
    /// <param name="player2Id">The second player's Discord user ID.</param>
    /// <param name="channelId">The channel where the trade is taking place.</param>
    /// <param name="guildId">The guild where the trade is taking place.</param>
    /// <returns>A TradeResult containing the created session or error information.</returns>
    public async Task<TradeResult> CreateTradeSessionAsync(ulong player1Id, ulong player2Id, ulong channelId, ulong guildId)
    {
        var validationResult = await ValidateTradeParticipantsAsync(player1Id, player2Id);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        if (await IsUserInTradeAsync(player1Id) || await IsUserInTradeAsync(player2Id))
        {
            return TradeResult.Failure("One of the users is already in an active trade session.");
        }

        var session = new TradeSession
        {
            Player1Id = player1Id,
            Player2Id = player2Id,
            ChannelId = channelId,
            GuildId = guildId,
            Status = TradeStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes)
        };

        _activeSessions[session.SessionId] = session;
        await StoreSessionInRedisAsync(session);

        return TradeResult.FromSuccess("Trade session created successfully.", session);
    }

    /// <summary>
    ///     Adds a Pokemon to a trade session.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="userId">The user adding the Pokemon.</param>
    /// <param name="pokemonPosition">The position of the Pokemon in the user's collection.</param>
    /// <returns>A TradeResult indicating success or failure.</returns>
    public async Task<TradeResult> AddPokemonToTradeAsync(Guid sessionId, ulong userId, int pokemonPosition)
    {
        var session = await GetTradeSessionAsync(sessionId);
        if (session == null)
        {
            return TradeResult.Failure("Trade session not found.");
        }

        if (!session.IsParticipant(userId))
        {
            return TradeResult.Failure("You are not a participant in this trade session.");
        }

        if (session.Status != TradeStatus.Active)
        {
            return TradeResult.Failure("Trade session is not active.");
        }

        var pokemon = await GetPokemonByPosition(userId, (ulong)pokemonPosition);
        if (pokemon == null)
        {
            return TradeResult.Failure("You don't have that Pokemon or that Pokemon is currently in the market!");
        }

        var validationResult = ValidatePokemonForTrade(pokemon, pokemonPosition);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        if (session.GetPokemonBy(userId).Any(p => p.PokemonId == pokemon.Id))
        {
            return TradeResult.Failure("You already have this Pokemon in your trade.");
        }

        var entry = TradeEntry.ForPokemon(userId, pokemon);
        session.AddEntry(entry);
        await UpdateSessionInRedisAsync(session);

        return TradeResult.FromSuccess($"Added {pokemon.PokemonName} to your trade list.", entry);
    }

    /// <summary>
    ///     Adds credits to a trade session.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="userId">The user adding credits.</param>
    /// <param name="credits">The number of credits to add.</param>
    /// <returns>A TradeResult indicating success or failure.</returns>
    public async Task<TradeResult> AddCreditsToTradeAsync(Guid sessionId, ulong userId, ulong credits)
    {
        var session = await GetTradeSessionAsync(sessionId);
        if (session == null)
        {
            return TradeResult.Failure("Trade session not found.");
        }

        if (!session.IsParticipant(userId))
        {
            return TradeResult.Failure("You are not a participant in this trade session.");
        }

        if (session.Status != TradeStatus.Active)
        {
            return TradeResult.Failure("Trade session is not active.");
        }

        if (credits == 0)
        {
            return TradeResult.Failure("You need to add at least 1 credit!");
        }

        var userCredits = await GetUserCreditsAsync(userId);
        if (userCredits < credits)
        {
            return TradeResult.Failure("You don't have enough credits to cover that amount!");
        }

        session.RemoveEntriesBy(userId, TradeItemType.Credits);

        var entry = TradeEntry.ForCredits(userId, credits);
        session.AddEntry(entry);
        await UpdateSessionInRedisAsync(session);

        return TradeResult.FromSuccess($"Added {credits:N0} credits to the trade.", entry);
    }

    /// <summary>
    ///     Adds tokens to a trade session.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="userId">The user adding tokens.</param>
    /// <param name="tokenType">The type of tokens to add.</param>
    /// <param name="count">The number of tokens to add.</param>
    /// <returns>A TradeResult indicating success or failure.</returns>
    public async Task<TradeResult> AddTokensToTradeAsync(Guid sessionId, ulong userId, TokenType tokenType, int count)
    {
        var session = await GetTradeSessionAsync(sessionId);
        if (session == null)
        {
            return TradeResult.Failure("Trade session not found.");
        }

        if (!session.IsParticipant(userId))
        {
            return TradeResult.Failure("You are not a participant in this trade session.");
        }

        if (session.Status != TradeStatus.Active)
        {
            return TradeResult.Failure("Trade session is not active.");
        }

        if (count <= 0)
        {
            return TradeResult.Failure("You need to add at least 1 token!");
        }

        var userTokens = await GetUserTokensAsync(userId);
        if (!userTokens.TryGetValue(tokenType.GetDisplayName(), out var availableTokens) || availableTokens < count)
        {
            return TradeResult.Failure($"You don't have enough {tokenType.GetDisplayName()} tokens!");
        }

        var existingEntry = session.GetEntriesBy(userId)
            .FirstOrDefault(e => e.ItemType == TradeItemType.Tokens && e.TokenType == tokenType);

        if (existingEntry != null)
        {
            existingEntry.TokenCount += count;
        }
        else
        {
            var entry = TradeEntry.ForTokens(userId, tokenType, count);
            session.AddEntry(entry);
        }

        await UpdateSessionInRedisAsync(session);

        return TradeResult.FromSuccess($"Added {count} {tokenType.GetDisplayName()} tokens to the trade.");
    }

    /// <summary>
    ///     Removes a Pokemon from a trade session by collection position.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="userId">The user removing the Pokemon.</param>
    /// <param name="pokemonPosition">The position of the Pokemon in the user's collection.</param>
    /// <returns>A TradeResult indicating success or failure.</returns>
    public async Task<TradeResult> RemovePokemonFromTradeAsync(Guid sessionId, ulong userId, ulong pokemonPosition)
    {
        var session = await GetTradeSessionAsync(sessionId);
        if (session == null)
        {
            return TradeResult.Failure("Trade session not found.");
        }

        var pokemon = await GetPokemonByPosition(userId, pokemonPosition);
        if (pokemon == null)
        {
            return TradeResult.Failure("Pokemon not found.");
        }

        var entry = session.GetPokemonBy(userId).FirstOrDefault(p => p.PokemonId == pokemon.Id);
        if (entry == null)
        {
            var pokemonInTrade = session.GetPokemonBy(userId).ToList();
            if (pokemonInTrade.Any())
            {
                var tradeList = string.Join(", ", pokemonInTrade
                    .Where(e => e.PokemonId.HasValue)
                    .Take(3)
                    .Select(e => $"ID:{e.PokemonId}"));
                return TradeResult.Failure($"Position {pokemonPosition} ({pokemon.PokemonName} ID:{pokemon.Id}) is not in your trade list! Current trade: {tradeList}");
            }
            return TradeResult.Failure($"You do not have {pokemon.PokemonName} (position {pokemonPosition}) in your trade list!");
        }

        session.RemoveEntry(entry.Id);
        await UpdateSessionInRedisAsync(session);

        return TradeResult.FromSuccess($"Removed {pokemon.PokemonName} from your trade!");
    }

    /// <summary>
    ///     Removes a Pokemon from a trade session by trade entry index.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <param name="userId">The user removing the Pokemon.</param>
    /// <param name="tradeEntryIndex">The index of the Pokemon in the trade list to remove (0-based).</param>
    /// <returns>A TradeResult indicating success or failure.</returns>
    public async Task<TradeResult> RemovePokemonFromTradeByIndexAsync(Guid sessionId, ulong userId, ulong tradeEntryIndex)
    {
        var session = await GetTradeSessionAsync(sessionId);
        if (session == null)
        {
            return TradeResult.Failure("Trade session not found.");
        }

        var userEntries = session.GetPokemonBy(userId).ToList();
        
        if (tradeEntryIndex >= (ulong)userEntries.Count)
        {
            return TradeResult.Failure("Invalid trade entry index.");
        }

        var entry = userEntries[(int)tradeEntryIndex];

        var pokemonName = "Pokemon";
        if (entry.PokemonId.HasValue)
        {
            await using var db = await _context.GetConnectionAsync();
            var pokemon = await db.UserPokemon
                .FirstOrDefaultAsync(p => p.Id == entry.PokemonId.Value);
            pokemonName = pokemon?.PokemonName ?? "Pokemon";
        }

        session.RemoveEntry(entry.Id);
        await UpdateSessionInRedisAsync(session);

        return TradeResult.FromSuccess($"Removed {pokemonName} from your trade!");
    }

    /// <summary>
    ///     Attempts to execute a trade after both users have confirmed.
    /// </summary>
    /// <param name="sessionId">The trade session ID.</param>
    /// <returns>A TradeResult indicating success or failure.</returns>
    public async Task<TradeResult> ExecuteTradeAsync(Guid sessionId)
    {
        var session = await GetTradeSessionAsync(sessionId);
        if (session == null)
        {
            return TradeResult.Failure("Trade session not found.");
        }

        if (session.Status != TradeStatus.PendingConfirmation && session.Status != TradeStatus.Processing)
        {
            return TradeResult.Failure("Trade is not ready for execution.");
        }

        if (!session.IsBothConfirmed())
        {
            return TradeResult.Failure("Both users must confirm the trade.");
        }

        if (session.IsAttemptingTrade)
        {
            return TradeResult.Failure("Trade is already being processed.");
        }

        if (session.Status != TradeStatus.Processing)
        {
            session.Status = TradeStatus.Processing;
        }
        session.IsAttemptingTrade = true;
        await UpdateSessionInRedisAsync(session);

        try
        {
            await using var db = await _context.GetConnectionAsync();

            await using var transaction = await db.BeginTransactionAsync();

            var revalidationResult = await RevalidateTradeItemsAsync(session);
            if (!revalidationResult.Success)
            {
                session.Status = TradeStatus.Failed;
                await UpdateSessionInRedisAsync(session);
                return revalidationResult;
            }

            await TransferTokensAsync(session);
            await TransferCreditsAsync(session);
            var pokemonEvolutions = await TransferPokemonAsync(session);

            await transaction.CommitAsync();

            session.Status = TradeStatus.Completed;
            await UpdateSessionInRedisAsync(session);

            await LogTradeAsync(session);

            await NotifyTradeCompletionAsync(session, pokemonEvolutions);

            return TradeResult.FromSuccess("Trade completed successfully!");
        }
        catch (Exception ex)
        {
            session.Status = TradeStatus.Failed;
            session.IsAttemptingTrade = false;
            await UpdateSessionInRedisAsync(session);
            
            return TradeResult.Failure($"Trade execution failed: {ex.Message}");
        }
    }

    /// <summary>
    ///     Clears trade locks for both participants when a session is not found.
    /// </summary>
    /// <param name="userId">The current user requesting the session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearOrphanedTradeLocksAsync(ulong userId)
    {
        await _tradeLockService.RemoveTradeLockAsync(userId);

        var userSessions = _activeSessions.Values.Where(s => s.IsParticipant(userId)).ToList();
        
        foreach (var session in userSessions)
        {
            var otherParticipant = session.GetOtherPlayer(userId);
            if (otherParticipant.HasValue)
            {
                await _tradeLockService.RemoveTradeLockAsync(otherParticipant.Value);
            }

            _activeSessions.TryRemove(session.SessionId, out _);
        }
    }

    /// <summary>
    ///     Gets a trade session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to retrieve.</param>
    /// <returns>The trade session if found, null otherwise.</returns>
    public async Task<TradeSession?> GetTradeSessionAsync(Guid sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var cachedSession))
        {
            return cachedSession;
        }

        var database = _cache.Redis.GetDatabase();
        var sessionData = await database.StringGetAsync($"{TradeSessionPrefix}{sessionId}");
        
        if (!sessionData.HasValue)
        {
            return null;
        }

        var session = JsonSerializer.Deserialize<TradeSession>((string)sessionData!);
        if (session != null)
        {
            if (DateTime.UtcNow > session.ExpiresAt)
            {
                await database.KeyDeleteAsync($"{TradeSessionPrefix}{sessionId}");
                await _tradeLockService.RemoveTradeLockAsync(session.Player1Id);
                await _tradeLockService.RemoveTradeLockAsync(session.Player2Id);
                return null;
            }

            _activeSessions[sessionId] = session;
        }

        return session;
    }

    /// <summary>
    ///     Cancels a trade session.
    /// </summary>
    /// <param name="sessionId">The session ID to cancel.</param>
    /// <param name="cancelledBy">The user who cancelled the trade.</param>
    /// <returns>A TradeResult indicating success or failure.</returns>
    public async Task<TradeResult> CancelTradeSessionAsync(Guid sessionId, ulong cancelledBy)
    {
        var session = await GetTradeSessionAsync(sessionId);
        if (session == null)
        {
            return TradeResult.Failure("Trade session not found.");
        }

        if (!session.IsParticipant(cancelledBy))
        {
            return TradeResult.Failure("You are not a participant in this trade session.");
        }

        await _tradeLockService.RemoveTradeLockAsync(session.Player1Id);
        await _tradeLockService.RemoveTradeLockAsync(session.Player2Id);

        session.Status = TradeStatus.Cancelled;

        await UpdateCancelledTradeInterfaceAsync(session);

        _activeSessions.TryRemove(sessionId, out _);
        var database = _cache.Redis.GetDatabase();
        await database.KeyDeleteAsync($"{TradeSessionPrefix}{sessionId}");

        return TradeResult.FromSuccess("Trade session has been cancelled.");
    }

    /// <summary>
    ///     Generates a formatted trade summary as an embed description.
    /// </summary>
    /// <param name="session">The trade session to summarize.</param>
    /// <returns>A formatted string containing the trade summary.</returns>
    public async Task<string> GenerateTradeSummaryAsync(TradeSession session)
    {
        var summary = new StringBuilder();

        summary.AppendLine($"**<@{session.Player1Id}>** is offering:");
        await AppendUserOfferingsAsync(summary, session, session.Player1Id);

        summary.AppendLine("━━━━━━━━━━━━━━━━");

        summary.AppendLine($"**<@{session.Player2Id}>** is offering:");
        await AppendUserOfferingsAsync(summary, session, session.Player2Id);

        return summary.ToString();
    }

    /// <summary>
    ///     Updates the trade interface message with current trade state.
    /// </summary>
    /// <param name="session">The trade session to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateTradeInterfaceAsync(TradeSession session)
    {
        if (session.TradeMessage == null)
        {
            return;
        }

        var tradeSummary = await GenerateTradeSummaryAsync(session);
        
        var embed = new EmbedBuilder()
            .WithTitle("🔄 Active Trade Session")
            .WithDescription(tradeSummary)
            .WithColor(Color.Blue)
            .WithFooter($"Session ID: {session.SessionId}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        var components = new ComponentBuilder()
            .WithButton("Add Pokemon", $"trade_add_pokemon:{session.SessionId}", ButtonStyle.Secondary, new Emoji("🎯"))
            .WithButton("Remove Pokemon", $"trade_remove_pokemon:{session.SessionId}", ButtonStyle.Secondary, new Emoji("🗑️"))
            .WithButton("Add Credits", $"trade_add_credits:{session.SessionId}", ButtonStyle.Secondary, new Emoji("💰"))
            .WithButton("Add Tokens", $"trade_add_tokens:{session.SessionId}", ButtonStyle.Secondary, new Emoji("🎫"))
            .WithButton("Remove Tokens", $"trade_remove_tokens:{session.SessionId}", ButtonStyle.Secondary, new Emoji("🎪"))
            .WithButton("Confirm Trade", $"trade_confirm:{session.SessionId}", ButtonStyle.Success, new Emoji("✅"), 
                row: 1, disabled: !session.HasItems())
            .WithButton("Cancel Trade", $"trade_cancel:{session.SessionId}", ButtonStyle.Danger, new Emoji("❌"), row: 1)
            .Build();

        try
        {
            await session.TradeMessage.ModifyAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
            });
        }
        catch
        {
        }
    }

    /// <summary>
    ///     Checks if a user has any active trade sessions.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user has an active trade session, false otherwise.</returns>
    public async Task<bool> HasActiveTradeSessionAsync(ulong userId)
    {
        var hasInMemorySession = _activeSessions.Values.Any(s => 
            s.IsParticipant(userId) && 
            s.Status is TradeStatus.Active or TradeStatus.PendingConfirmation or TradeStatus.Processing);
        
        if (hasInMemorySession)
        {
            return true;
        }

        var database = _cache.Redis.GetDatabase();
        var server = _cache.Redis.GetServers().FirstOrDefault();
        if (server != null)
        {
            var keys = server.Keys(pattern: $"{TradeSessionPrefix}*");
            foreach (var key in keys)
            {
                try
                {
                    var sessionData = await database.StringGetAsync(key);
                    if (sessionData.HasValue)
                    {
                        var session = JsonSerializer.Deserialize<TradeSession>((string)sessionData!);
                        if (session != null && 
                            session.IsParticipant(userId) && 
                            session.Status is TradeStatus.Active or TradeStatus.PendingConfirmation or TradeStatus.Processing)
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        return false;
    }


    /// <summary>
    ///     Updates the trade interface to show that the trade has been cancelled.
    /// </summary>
    /// <param name="session">The cancelled trade session.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateCancelledTradeInterfaceAsync(TradeSession session)
    {
        if (session.TradeMessage == null)
        {
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("❌ Trade Cancelled")
            .WithDescription("This trade session has been cancelled. All trade locks have been removed.")
            .WithColor(Color.Red)
            .WithFooter($"Session ID: {session.SessionId}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        var components = new ComponentBuilder().Build();

        try
        {
            await session.TradeMessage.ModifyAsync(x =>
            {
                x.Embed = embed;
                x.Components = components;
            });
        }
        catch
        {
        }
    }

    #region Private Helper Methods

    /// <summary>
    ///     Validates that both players exist (have started the game) and that neither is trade-locked.
    ///     Also rejects self-trades.
    /// </summary>
    /// <param name="player1Id">The first participant's Discord ID.</param>
    /// <param name="player2Id">The second participant's Discord ID.</param>
    /// <returns>A successful <see cref="TradeResult"/> when both participants are eligible; otherwise a failure with a user-facing reason.</returns>
    private async Task<TradeResult> ValidateTradeParticipantsAsync(ulong player1Id, ulong player2Id)
    {
        if (player1Id == player2Id)
        {
            return TradeResult.Failure("You cannot trade with yourself!");
        }

        await using var db = await _context.GetConnectionAsync();
        
                var users = await db.Users
            .Where(u => u.UserId == player1Id || u.UserId == player2Id)
            .ToListAsync();

        if (users.Count != 2)
        {
            var missingUser = users.Any(u => u.UserId == player1Id) ? player2Id : player1Id;
            return TradeResult.Failure($"<@{missingUser}> has not started! Start with `/start` first!");
        }

                var tradeLocked = users.Where(u => u.TradeLock == true).ToList();
        if (tradeLocked.Any())
        {
            return TradeResult.Failure("A user is not allowed to trade.");
        }

        return TradeResult.FromSuccess("Users validated for trading.");
    }

    /// <summary>
    ///     Determines whether a user is currently engaged in a live trade — either via an in-memory active session
    ///     in a non-terminal state, or via the distributed trade lock held in Redis.
    /// </summary>
    /// <param name="userId">The Discord ID to check.</param>
    /// <returns><c>true</c> if the user is in an ongoing trade; otherwise <c>false</c>.</returns>
    private async Task<bool> IsUserInTradeAsync(ulong userId)
    {
        if (_activeSessions.Values.Any(s => s.IsParticipant(userId) &&
            s.Status is TradeStatus.Active or TradeStatus.PendingConfirmation or TradeStatus.Processing))
        {
            return true;
        }

        return await _tradeLockService.IsUserTradeLockedAsync(userId);
    }

    /// <summary>
    ///     Gets a Pokemon by user ID and position in their collection.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="position">The position in the collection.</param>
    /// <returns>The Pokemon at the specified position, or null if not found.</returns>
    public async Task<Database.Linq.Models.Pokemon.Pokemon?> GetPokemonByPosition(ulong userId, ulong position)
    {
        if (position <= 0)
        {
            return null;
        }

        await using var db = await _context.GetConnectionAsync();
        
        var ownership = await db.UserPokemonOwnerships
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Position == position);

        if (ownership == null)
        {
            return null;
        }

        return await db.UserPokemon
            .FirstOrDefaultAsync(p => p.Id == ownership.PokemonId);
    }

    /// <summary>
    ///     Validates if a Pokemon can be traded.
    /// </summary>
    /// <param name="pokemon">The Pokemon to validate.</param>
    /// <param name="position">The position of the Pokemon in the collection.</param>
    /// <returns>A TradeResult indicating success or failure with a message.</returns>
    public TradeResult ValidatePokemonForTrade(Database.Linq.Models.Pokemon.Pokemon pokemon, int position)
    {
        if (position == 1)
        {
            return TradeResult.Failure("You cannot trade your first Pokemon.");
        }

        if (pokemon.PokemonName == "Egg")
        {
            return TradeResult.Failure("You cannot trade eggs!");
        }

        if (!pokemon.Tradable)
        {
            return TradeResult.Failure("This Pokemon is not tradable.");
        }

        if (pokemon.Favorite)
        {
            return TradeResult.Failure($"You cannot trade your {pokemon.PokemonName} as it is favorited. Unfavorite it first with `/fav remove {position}`.");
        }

        return TradeResult.FromSuccess("Pokemon is valid for trading.");
    }

    /// <summary>
    ///     Reads the current MewCoin (credit) balance for a user.
    /// </summary>
    /// <param name="userId">The user's Discord ID.</param>
    /// <returns>The user's credit balance, or 0 if the user record is missing.</returns>
    private async Task<ulong> GetUserCreditsAsync(ulong userId)
    {
        await using var db = await _context.GetConnectionAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        return user?.MewCoins ?? 0;
    }

    /// <summary>
    ///     Loads and deserializes a user's type-token map. Returns an empty dictionary if the user has no
    ///     stored tokens or the JSON is malformed.
    /// </summary>
    /// <param name="userId">The user's Discord ID.</param>
    /// <returns>A dictionary keyed by Pokemon type name with current token counts.</returns>
    private async Task<Dictionary<string, int>> GetUserTokensAsync(ulong userId)
    {
        await using var db = await _context.GetConnectionAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user?.Tokens == null)
        {
            return new Dictionary<string, int>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(user.Tokens) ?? new Dictionary<string, int>();
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }

    /// <summary>
    ///     Serializes the trade session and writes it to Redis under the session prefix with a TTL one minute
    ///     longer than the configured session timeout, ensuring the cache outlives the in-memory session.
    /// </summary>
    /// <param name="session">The session to persist.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task StoreSessionInRedisAsync(TradeSession session)
    {
        var database = _cache.Redis.GetDatabase();
        var sessionJson = JsonSerializer.Serialize(session);
        await database.StringSetAsync($"{TradeSessionPrefix}{session.SessionId}", sessionJson, 
            TimeSpan.FromMinutes(SessionTimeoutMinutes + 1));
    }

    /// <summary>
    ///     Updates a trade session in Redis with current timestamp.
    /// </summary>
    /// <param name="session">The session to update.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateSessionInRedisAsync(TradeSession session)
    {
        session.LastUpdated = DateTime.UtcNow;
        await StoreSessionInRedisAsync(session);
    }

    /// <summary>
    ///     Performs a final integrity check immediately before executing a trade: confirms each offered Pokemon
    ///     is still owned by the offering player, and that credit/token balances cover what each side has staked.
    ///     Guards against races where a player's state changed after they confirmed.
    /// </summary>
    /// <param name="session">The trade session whose entries should be revalidated.</param>
    /// <returns>A successful <see cref="TradeResult"/> if every entry is still valid; otherwise a failure naming the offending player and reason.</returns>
    private async Task<TradeResult> RevalidateTradeItemsAsync(TradeSession session)
    {
        await using var db = await _context.GetConnectionAsync();

        foreach (var pokemonEntry in session.TradeEntries.Where(e => e.ItemType == TradeItemType.Pokemon))
        {
            var pokemon = await db.UserPokemon.FirstOrDefaultAsync(p => p.Id == pokemonEntry.PokemonId);
            if (pokemon == null)
            {
                return TradeResult.Failure($"<@{pokemonEntry.OfferedBy}> no longer owns one or more of the Pokemon they were trading!");
            }

            var ownership = await db.UserPokemonOwnerships
                .FirstOrDefaultAsync(o => o.UserId == pokemonEntry.OfferedBy && o.PokemonId == pokemon.Id);
            if (ownership == null)
            {
                return TradeResult.Failure($"<@{pokemonEntry.OfferedBy}> no longer owns one or more of the Pokemon they were trading!");
            }
        }

        foreach (var playerGroup in session.TradeEntries.Where(e => e.ItemType == TradeItemType.Credits).GroupBy(e => e.OfferedBy))
        {
            var totalCredits = playerGroup.Sum(e => (long)e.Credits);
            var userCredits = await GetUserCreditsAsync(playerGroup.Key);
            if (userCredits < (ulong)totalCredits)
            {
                return TradeResult.Failure($"<@{playerGroup.Key}> does not have enough credits to complete this trade!");
            }
        }

        foreach (var playerGroup in session.TradeEntries.Where(e => e.ItemType == TradeItemType.Tokens).GroupBy(e => e.OfferedBy))
        {
            var userTokens = await GetUserTokensAsync(playerGroup.Key);
            foreach (var tokenGroup in playerGroup.GroupBy(e => e.TokenType))
            {
                var totalTokens = tokenGroup.Sum(e => e.TokenCount);
                var tokenType = tokenGroup.Key?.GetDisplayName() ?? "";
                if (!userTokens.TryGetValue(tokenType, out var available) || available < totalTokens)
                {
                    return TradeResult.Failure($"<@{playerGroup.Key}> does not have enough {tokenType} tokens to complete the trade!");
                }
            }
        }

        return TradeResult.FromSuccess("All trade items revalidated successfully.");
    }

    /// <summary>
    ///     Applies token transfers between the two participants by adjusting each side's deserialized token map
    ///     and writing both back to the database.
    /// </summary>
    /// <param name="session">The session whose token entries should be settled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task TransferTokensAsync(TradeSession session)
    {
        await using var db = await _context.GetConnectionAsync();

        var p1Tokens = await GetUserTokensAsync(session.Player1Id);
        var p2Tokens = await GetUserTokensAsync(session.Player2Id);

        foreach (var tokenEntry in session.GetEntriesBy(session.Player1Id).Where(e => e.ItemType == TradeItemType.Tokens))
        {
            var tokenType = tokenEntry.TokenType!.Value.GetDisplayName();
            p1Tokens[tokenType] -= tokenEntry.TokenCount;
            p2Tokens[tokenType] = p2Tokens.GetValueOrDefault(tokenType, 0) + tokenEntry.TokenCount;
        }

        foreach (var tokenEntry in session.GetEntriesBy(session.Player2Id).Where(e => e.ItemType == TradeItemType.Tokens))
        {
            var tokenType = tokenEntry.TokenType!.Value.GetDisplayName();
            p2Tokens[tokenType] -= tokenEntry.TokenCount;
            p1Tokens[tokenType] = p1Tokens.GetValueOrDefault(tokenType, 0) + tokenEntry.TokenCount;
        }

        await db.Users.Where(u => u.UserId == session.Player1Id)
            .Set(u => u.Tokens, JsonSerializer.Serialize(p1Tokens))
            .UpdateAsync();
            
        await db.Users.Where(u => u.UserId == session.Player2Id)
            .Set(u => u.Tokens, JsonSerializer.Serialize(p2Tokens))
            .UpdateAsync();
    }

    /// <summary>
    ///     Applies credit transfers between the two participants in a single round-trip per side. No-op if
    ///     neither player offered any credits.
    /// </summary>
    /// <param name="session">The session whose credit entries should be settled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task TransferCreditsAsync(TradeSession session)
    {
        await using var db = await _context.GetConnectionAsync();

        var p1Credits = session.GetCreditsBy(session.Player1Id);
        var p2Credits = session.GetCreditsBy(session.Player2Id);

        if (p1Credits > 0 || p2Credits > 0)
        {
            await db.Users.Where(u => u.UserId == session.Player1Id)
                .Set(u => u.MewCoins, u => (u.MewCoins ?? 0) - p1Credits + p2Credits)
                .UpdateAsync();
                
            await db.Users.Where(u => u.UserId == session.Player2Id)
                .Set(u => u.MewCoins, u => (u.MewCoins ?? 0) - p2Credits + p1Credits)
                .UpdateAsync();
        }
    }

    private async Task<List<(ulong PokemonId, string PokemonName, ulong NewOwnerId, string? Evolution)>> TransferPokemonAsync(TradeSession session)
    {
        var evolutions = new List<(ulong PokemonId, string PokemonName, ulong NewOwnerId, string? Evolution)>();

        foreach (var pokemonEntry in session.GetPokemonBy(session.Player1Id))
        {
            var evolution = await TransferPokemonOwnershipAsync(pokemonEntry.PokemonId!.Value, session.Player1Id, session.Player2Id);
            evolutions.Add((pokemonEntry.PokemonId!.Value, pokemonEntry.Pokemon?.PokemonName ?? "Unknown", session.Player2Id, evolution));
        }

        foreach (var pokemonEntry in session.GetPokemonBy(session.Player2Id))
        {
            var evolution = await TransferPokemonOwnershipAsync(pokemonEntry.PokemonId!.Value, session.Player2Id, session.Player1Id);
            evolutions.Add((pokemonEntry.PokemonId!.Value, pokemonEntry.Pokemon?.PokemonName ?? "Unknown", session.Player1Id, evolution));
        }

        return evolutions;
    }

    /// <summary>
    ///     Reassigns ownership of a Pokemon to the receiving player by deleting the source ownership row,
    ///     inserting a new one at the receiver's next available position, clearing the market-enlist flag,
    ///     and finally checking whether the species triggers a trade-induced evolution.
    /// </summary>
    /// <param name="pokemonId">The ID of the Pokemon being transferred.</param>
    /// <param name="fromUserId">The current owner's Discord ID.</param>
    /// <param name="toUserId">The new owner's Discord ID.</param>
    /// <returns>The evolved-form name if the trade triggered an evolution; otherwise <c>null</c>.</returns>
    private async Task<string?> TransferPokemonOwnershipAsync(ulong pokemonId, ulong fromUserId, ulong toUserId)
    {
        await using var db = await _context.GetConnectionAsync();

        var oldOwnership = await db.UserPokemonOwnerships
            .FirstOrDefaultAsync(o => o.UserId == fromUserId && o.PokemonId == pokemonId);
        
        if (oldOwnership != null)
        {
            await db.UserPokemonOwnerships
                .Where(o => o.UserId == fromUserId && o.PokemonId == pokemonId)
                .DeleteAsync();
        }

        var maxPosition = await db.UserPokemonOwnerships
            .Where(o => o.UserId == toUserId)
            .MaxAsync(o => (ulong?)o.Position) ?? 0;

        var newOwnership = new UserPokemonOwnership
        {
            UserId = toUserId,
            PokemonId = pokemonId,
            Position = maxPosition + 1
        };

        await db.InsertAsync(newOwnership);

        await db.UserPokemon
            .Where(p => p.Id == pokemonId)
            .Set(p => p.MarketEnlist, false)
            .UpdateAsync();

        var pokemon = await db.UserPokemon.FirstAsync(p => p.Id == pokemonId);

        return await _evolutionService.CheckTradeEvolution(pokemon);
    }

    /// <summary>
    ///     Posts a formatted summary of the completed trade to the configured audit log channel.
    ///     Failures are intentionally swallowed so logging issues don't fail the trade.
    /// </summary>
    /// <param name="session">The completed session to log.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LogTradeAsync(TradeSession session)
    {
        try
        {
            if (_discordClient.GetChannel(LogChannelId) is ITextChannel channel)
            {
                var summary = await GenerateTradeSummaryAsync(session);
                var logMessage = $"🔄 **Trade Completed**\n" +
                                $"**Session ID**: `{session.SessionId}`\n" +
                                $"**Participants**: <@{session.Player1Id}> (`{session.Player1Id}`) ↔ <@{session.Player2Id}> (`{session.Player2Id}`)\n" +
                                $"**Completed**: <t:{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}:F>\n\n" +
                                $"{summary}";

                await channel.SendMessageAsync(logMessage);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    ///     Hook for post-trade participant notifications (e.g. trade-evolution announcements).
    ///     Failures are intentionally swallowed so notification issues don't fail the trade.
    /// </summary>
    /// <param name="session">The completed session.</param>
    /// <param name="evolutions">The list of trade-induced evolutions produced by <see cref="TransferPokemonOwnershipAsync"/>.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task NotifyTradeCompletionAsync(TradeSession session, List<(ulong PokemonId, string PokemonName, ulong NewOwnerId, string? Evolution)> evolutions)
    {
        try
        {
            var user1 = _discordClient.GetUser(session.Player1Id);
            var user2 = _discordClient.GetUser(session.Player2Id);

            var evolutionMessage = "";
            if (evolutions.Any(e => e.Evolution != null))
            {
                evolutionMessage = "\n\n🌟 **Trade Evolutions!**\n" + 
                    string.Join("\n", evolutions
                        .Where(e => e.Evolution != null)
                        .Select(e => $"• {e.PokemonName} evolved into **{e.Evolution}**!"));
            }

        }
        catch
        {
        }
    }

    /// <summary>
    ///     Appends a markdown-formatted breakdown of one user's contributions (Pokemon, credits, tokens) to a
    ///     trade-summary <see cref="StringBuilder"/>. Writes "Nothing Added" if the user offered nothing.
    /// </summary>
    /// <param name="summary">The summary buffer to append to.</param>
    /// <param name="session">The trade session being summarized.</param>
    /// <param name="userId">The Discord ID of the participant whose offerings should be rendered.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AppendUserOfferingsAsync(StringBuilder summary, TradeSession session, ulong userId)
    {
        var userEntries = session.GetEntriesBy(userId).ToList();

        if (!userEntries.Any())
        {
            summary.AppendLine("Nothing Added");
            return;
        }

        var pokemonEntries = userEntries.Where(e => e.ItemType == TradeItemType.Pokemon).ToList();
        if (pokemonEntries.Any())
        {
            summary.AppendLine("**Pokemon:**");
            foreach (var entry in pokemonEntries)
            {
                summary.AppendLine($"- {entry.GetDisplayString()}");
            }
        }

        var creditsTotal = session.GetCreditsBy(userId);
        if (creditsTotal > 0)
        {
            summary.AppendLine($"**Credits**\n- `{creditsTotal:N0}`");
        }

        var tokens = session.GetTokensBy(userId);
        if (tokens.Any())
        {
            summary.AppendLine("\n**Radiant Tokens:**");
            foreach (var (tokenType, count) in tokens)
            {
                summary.AppendLine($"- {tokenType.GetEmoji()} **{tokenType.GetDisplayName()}**: `{count}`");
            }
        }
    }

    /// <summary>
    ///     Gets the user's tradeable Pokemon for quick selection.
    /// </summary>
    /// <param name="userId">The user ID to get Pokemon for.</param>
    /// <returns>A list of tradeable Pokemon with their details.</returns>
    public async Task<List<TradeablePokemonInfo>> GetUserTradeablePokemonAsync(ulong userId)
    {
        try
        {
            await using var db = await _context.GetConnectionAsync();

            var userPokemon = await db.UserPokemonOwnerships
                .Where(o => o.UserId == userId)
                .Join(db.UserPokemon, o => o.PokemonId, p => p.Id, (o, p) => new { o.Position, Pokemon = p })
                .Where(x => x.Position > 1 &&
                           x.Pokemon.PokemonName != "Egg" &&
                           !x.Pokemon.Favorite &&
                           x.Pokemon.Tradable &&
                           !x.Pokemon.MarketEnlist)
                .OrderBy(x => x.Position)
                .Take(20)
                .Select(x => new TradeablePokemonInfo
                {
                    Position = x.Position,
                    PokemonName = x.Pokemon.PokemonName!,
                    Nickname = x.Pokemon.Nickname,
                    Level = x.Pokemon.Level,
                    Shiny = x.Pokemon.Shiny,
                    Radiant = x.Pokemon.Radiant
                })
                .ToListAsync();

            return userPokemon;
        }
        catch (Exception ex)
        {
            Log.Information($"Error getting user tradeable Pokemon: {ex.Message}");
            return new List<TradeablePokemonInfo>();
        }
    }

    #endregion
}