using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using EeveeCore.Database.DbContextStuff;
using EeveeCore.Modules.Duels.Impl;
using EeveeCore.Services.Impl;

namespace EeveeCore.Modules.Duels.Services;

public class DuelService : INService
{
    private readonly IMongoService _mongoService;
    private readonly DiscordShardedClient _client;
    private readonly DbContextProvider _db;
    private readonly RedisCache _redis;
    private DateTime? _duelResetTime;
    private const string DATE_FORMAT = "MM/dd/yyyy, HH:mm:ss";

    // Active battles dictionary - accessible from both PokemonBattleModule and DuelInteractionHandler
    private readonly Dictionary<(ulong, ulong), Battle> _activeBattles = new();

    public DuelService(
        IMongoService mongoService,
        DbContextProvider db,
        DiscordShardedClient client,
        RedisCache redis)
    {
        _mongoService = mongoService;
        _db = db;
        _client = client;
        _redis = redis;

        // Initialize Redis cooldown storage
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Initialize Redis cooldown hash maps
            var db = _redis.Redis.GetDatabase();

            // Make sure the duelcooldowns hash exists
            await db.HashSetAsync("duelcooldowns", "examplekey", "examplevalue");

            // Make sure the daily cooldowns hash exists
            await db.HashSetAsync("dailyduelcooldowns", "examplekey", "examplevalue");

            // Get or set the duel reset time
            var resetTime = await db.StringGetAsync("duelcooldownreset");
            if (!resetTime.HasValue)
            {
                _duelResetTime = DateTime.UtcNow;
                await db.StringSetAsync("duelcooldownreset", _duelResetTime.Value.ToString(DATE_FORMAT));
            }
            else
            {
                _duelResetTime = DateTime.ParseExact(resetTime.ToString(), DATE_FORMAT, null);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing Redis for duel cooldowns");
        }
    }

    /// <summary>
    ///     Gets a user's Pokémon party from the database
    /// </summary>
    public async Task<List<DuelPokemon>> GetUserPokemonParty(ulong userId, IInteractionContext ctx)
    {
        var duelPokemon = new List<DuelPokemon>();

        try
        {
            await using var dbContext = await _db.GetContextAsync();
            // Get the user's party array directly
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Party == null) return duelPokemon; // Return empty list

            // Filter out zeros from the party array
            var partyIds = user.Party.Where(id => id > 0).ToList();
            if (!partyIds.Any()) return duelPokemon;

            // Fetch all Pokémon data for the party in a single query
            var partyPokemon = await dbContext.UserPokemon
                .Where(x => x.Owner.HasValue)
                .Where(p => partyIds.Contains(p.Id))
                .ToListAsync();

            // Filter out eggs
            partyPokemon = partyPokemon
                .Where(p => !p.PokemonName.Equals("Egg", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Sort the Pokémon in the order they appear in the party array
            partyPokemon = partyPokemon
                .OrderBy(p => partyIds.IndexOf(p.Id))
                .ToList();

            // Create DuelPokemon objects for each party Pokémon
            foreach (var pokemon in partyPokemon)
            {
                // Use the factory method to create a DuelPokemon object
                var duelPoke = await DuelPokemon.Create(ctx, pokemon, _mongoService);
                if (duelPoke != null) duelPokemon.Add(duelPoke);
            }

            // Create a MemberTrainer to be the owner of these Pokémon
            if (duelPokemon.Any())
            {
                var trainer = new MemberTrainer(
                    _client.GetUser(userId),
                    duelPokemon
                );

                // Set the owner reference for each Pokémon
                foreach (var pokemon in duelPokemon) pokemon.Owner = trainer;
            }

            return duelPokemon;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred getting Pokemon party: {Error}", ex.Message);
            return [];
        }
    }


    #region Battle Management

    /// <summary>
    /// Registers a battle and handles both local tracking and Redis persistence
    /// </summary>
    public async Task RegisterBattle(ulong userId1, ulong userId2, Battle battle, ulong interactionId)
    {
        try
        {
            Log.Debug("Registering battle {BattleId} between users {User1} and {User2}",
                interactionId, userId1, userId2);

            // Add to local dictionary
            _activeBattles[(userId1, userId2)] = battle;

            // If it's a PvP battle, add the reverse mapping too for easier lookup
            if (userId2 != 0)
                _activeBattles[(userId2, userId1)] = battle;

            // Store in Redis
            var db = _redis.Redis.GetDatabase();

            // Mark users as in battle
            await db.StringSetAsync($"user_in_battle:{userId1}", "true", TimeSpan.FromHours(2));
            if (userId2 != 0)
                await db.StringSetAsync($"user_in_battle:{userId2}", "true", TimeSpan.FromHours(2));

            // Store battle metadata in a hash
            var battleKey = $"battle:{interactionId}";
            await db.HashSetAsync(battleKey, new HashEntry[]
            {
                new("user1", userId1),
                new("user2", userId2),
                new("created", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            });

            // Set expiration to 2 hours (max battle duration)
            await db.KeyExpireAsync(battleKey, TimeSpan.FromHours(2));

            // Create user-to-battle mapping for quick lookup
            await db.StringSetAsync($"user_battle:{userId1}", interactionId, TimeSpan.FromHours(2));
            if (userId2 != 0)
                await db.StringSetAsync($"user_battle:{userId2}", interactionId, TimeSpan.FromHours(2));

            Log.Information("Battle registered: {InteractionId} between {User1} and {User2}",
                interactionId, userId1, userId2);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error registering battle {BattleId} in Redis", interactionId);
        }
    }

    /// <summary>
    /// Finds a battle for a user from the local dictionary
    /// </summary>
    public Battle FindBattle(ulong userId)
    {
        foreach (var kvp in _activeBattles)
        {
            var battle = kvp.Value;

            // Check if user is either trainer
            if ((battle.Trainer1 as MemberTrainer)?.Id == userId ||
                (battle.Trainer2 as MemberTrainer)?.Id == userId)
                return battle;
        }

        return null;
    }

    /// <summary>
    /// Checks if a user is in a battle (checks both local dictionary and Redis)
    /// </summary>
    public async Task<bool> IsUserInBattle(ulong userId)
    {
        // Check local dictionary first
        if (FindBattle(userId) != null)
            return true;

        // If not found locally, check Redis
        try
        {
            var db = _redis.Redis.GetDatabase();
            var exists =  await db.KeyExistsAsync($"user_in_battle:{userId}");
            if (!exists) return false;
            await db.KeyDeleteAsync($"user_in_battle:{userId}");
            return false;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if user {UserId} is in battle", userId);
            return false;
        }
    }

    /// <summary>
    /// Ends a battle and cleans up all references in both dictionary and Redis
    /// </summary>
    public async Task EndBattle(Battle battle, string battleId = null)
    {
        try
        {
            // Get user IDs
            ulong user1Id = 0;
            ulong user2Id = 0;

            if (battle.Trainer1 is MemberTrainer t1)
                user1Id = t1.Id;

            if (battle.Trainer2 is MemberTrainer t2)
                user2Id = t2.Id;

            Log.Debug("Ending battle between users {User1} and {User2}", user1Id, user2Id);

            // Remove from dictionary
            if (user1Id != 0 && user2Id != 0)
            {
                _activeBattles.Remove((user1Id, user2Id));
                _activeBattles.Remove((user2Id, user1Id));
            }
            else if (user1Id != 0)
            {
                _activeBattles.Remove((user1Id, 0));
            }

            // Clean up Redis
            var db = _redis.Redis.GetDatabase();

            // Remove user battle flags
            if (user1Id != 0)
                await db.KeyDeleteAsync($"user_in_battle:{user1Id}");
            if (user2Id != 0)
                await db.KeyDeleteAsync($"user_in_battle:{user2Id}");

            // If we have a battle ID, clean up battle metadata
            if (!string.IsNullOrEmpty(battleId))
            {
                // Delete user-to-battle mappings
                if (user1Id != 0)
                    await db.KeyDeleteAsync($"user_battle:{user1Id}");
                if (user2Id != 0)
                    await db.KeyDeleteAsync($"user_battle:{user2Id}");

                // Delete battle metadata
                await db.KeyDeleteAsync($"battle:{battleId}");

                Log.Information("Battle metadata removed: {BattleId}", battleId);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error ending battle");
        }
    }

    #endregion
}