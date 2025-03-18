using System.Text;
using System.Text.Json;
using Ditto.Database.DbContextStuff;
using Ditto.Services.Impl;
using Duels;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Ditto.Modules.Duels.Services;

public class DuelService : INService
{
    private readonly ILogger<DuelService> _logger;
    private readonly IMongoService _mongoDb;
    private readonly DbContextProvider _dbContextProvider;
    private readonly IDataCache _cache;
    private readonly DiscordSocketClient _client;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();
    private DateTime? _duelResetTime;
    private readonly Dictionary<ulong, Battle> _games = new();
    private readonly Dictionary<ulong, int> _ranks = new();
    private const string DATE_FORMAT = "MM/dd/yyyy, HH:mm:ss";

    // Pre-game GIFs
    private readonly string[] PREGAME_GIFS = new[]
    {
        "https://skylarr1227.github.io/images/duel1.gif",
        "https://skylarr1227.github.io/images/duel2.gif",
        "https://skylarr1227.github.io/images/duel3.gif",
        "https://skylarr1227.github.io/images/duel4.gif"
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="DuelService" /> class.
    /// </summary>
    /// <param name="logger">The logging service.</param>
    /// <param name="mongoDb">The MongoDB service.</param>
    /// <param name="dbContextProvider">The PostgreSQL context provider.</param>
    /// <param name="cache">The Redis cache service.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="httpClient">The HTTP client for external API calls.</param>
    public DuelService(
        ILogger<DuelService> logger,
        IMongoService mongoDb,
        DbContextProvider dbContextProvider,
        IDataCache cache,
        DiscordSocketClient client,
        HttpClient httpClient)
    {
        _logger = logger;
        _mongoDb = mongoDb;
        _dbContextProvider = dbContextProvider;
        _cache = cache;
        _client = client;
        _httpClient = httpClient;

        // Initialize
        _ = InitializeAsync();
    }

    /// <summary>
    ///     Initializes the Redis cache for duel cooldowns and prepares the service.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InitializeAsync()
    {
        try
        {
            // Initialize Redis cache for duel cooldowns
            await _cache.Redis.GetDatabase()
                .HashSetAsync("duelcooldowns", new[] { new HashEntry("examplekey", "examplevalue") });
            await _cache.Redis.GetDatabase()
                .HashSetAsync("dailyduelcooldowns", new[] { new HashEntry("examplekey", "examplevalue") });

            var duelResetTime = await _cache.Redis.GetDatabase().StringGetAsync("duelcooldownreset");

            if (duelResetTime.IsNull)
            {
                _duelResetTime = DateTime.Now;
                await _cache.Redis.GetDatabase()
                    .StringSetAsync("duelcooldownreset", _duelResetTime.Value.ToString(DATE_FORMAT));
            }
            else
            {
                _duelResetTime = DateTime.ParseExact(duelResetTime.ToString(), DATE_FORMAT, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing DuelService Redis cache");
            _duelResetTime = DateTime.Now;
        }
    }

    /// <summary>
    ///     Execute a 1v1 duel between two users.
    /// </summary>
    /// <param name="user">The user initiating the duel.</param>
    /// <param name="opponent">The user being challenged.</param>
    /// <param name="context">The interaction context.</param>
    /// <returns>The result of the command execution.</returns>
    public async Task<CommandResult> SingleDuel(IUser user, IUser opponent, IInteractionContext context)
    {
        // Check if user is trying to duel themselves
        if (opponent.Id == user.Id) return new CommandResult { Message = "You cannot duel yourself!" };

        // Ask opponent to accept the duel
        var accepted = await GetOpponentAcceptance(context, opponent, "one pokemon duel");
        if (!accepted) return new CommandResult { Message = "Duel request was declined or timed out." };

        // Send loading message
        var embed = new EmbedBuilder()
            .WithTitle("Pokemon Battle accepted! Loading...")
            .WithDescription("Please wait")
            .WithColor(new Color(255, 182, 193))
            .WithImageUrl(PREGAME_GIFS[_random.Next(PREGAME_GIFS.Length)])
            .Build();

        await context.Interaction.FollowupAsync(embed: embed);

        // Check cooldowns
        if (!await CheckCooldowns(context, opponent))
            return new CommandResult { Message = "Duel is on cooldown. Please try again later." };

        // Get the selected Pokemon for each trainer from PostgreSQL
        await using var db = await _dbContextProvider.GetContextAsync();

        var challenger1 = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == db.Users
            .Where(u => u.UserId == user.Id)
            .Select(u => u.Selected)
            .FirstOrDefault());

        var challenger2 = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == db.Users
            .Where(u => u.UserId == opponent.Id)
            .Select(u => u.Selected)
            .FirstOrDefault());

        if (challenger1 == null)
            return new CommandResult
                { Message = $"{user.Username} has not selected a Pokemon!\nSelect one with `/select <id>` first!" };

        if (challenger2 == null)
            return new CommandResult
                { Message = $"{opponent.Username} has not selected a Pokemon!\nSelect one with `/select <id>` first!" };

        if (challenger1.PokemonName.ToLower() == "egg")
            return new CommandResult
            {
                Message = $"{user.Username} has an egg selected!\nSelect a different pokemon with `/select <id>` first!"
            };

        if (challenger2.PokemonName.ToLower() == "egg")
            return new CommandResult
            {
                Message =
                    $"{opponent.Username} has an egg selected!\nSelect a different pokemon with `/select <id>` first!"
            };

        // Create Pokemon objects
        var p1Current = await DuelPokemon.Create(context, challenger1);
        var p2Current = await DuelPokemon.Create(context, challenger2);

        // Create trainers
        var owner1 = new MemberTrainer(user, new List<DuelPokemon> { p1Current });
        var owner2 = new MemberTrainer(opponent, new List<DuelPokemon> { p2Current });

        // Create battle
        var battle = new Battle(context, context.Channel, owner1, owner2, _mongoDb);

        // Run the battle
        var winner = await WrappedRun(battle);

        if (winner == null) return new CommandResult { Message = "Battle ended with no winner." };

        // Update achievements and grant XP
        var description = "";

        if (winner is MemberTrainer trainer)
        {
            // Update achievements in PostgreSQL
            await using (var dbContext = await _dbContextProvider.GetContextAsync())
            {
                await dbContext.Achievements
                    .Where(a => a.UserId == trainer.Id)
                    .ExecuteUpdateAsync(a => a
                        .SetProperty(x => x.DuelSingleWins, x => x.DuelSingleWins + 1));

                await dbContext.Achievements
                    .Where(a => a.UserId == trainer.Id || a.UserId == opponent.Id)
                    .ExecuteUpdateAsync(a => a
                        .SetProperty(x => x.DuelsTotal, x => x.DuelsTotal + 1));
            }

            // Grant XP to winner's Pokemon
            foreach (var poke in winner.Party)
            {
                await using var dbContext = await _dbContextProvider.GetContextAsync();

                // Get the Pokemon's held item and current XP
                var data = await dbContext.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .Select(p => new { p.HeldItem, p.Experience })
                    .FirstOrDefaultAsyncEF();

                if (data == null) continue;

                var heldItem = data.HeldItem.ToLower();
                var currentExp = data.Experience;

                var exp = 0;
                if (heldItem != "xp-block")
                {
                    exp = 150 * poke.Level / 7;
                    if (heldItem == "lucky-egg") exp = (int)(exp * 2.5);
                    // Max int for the exp column
                    exp = Math.Min(exp, 2147483647 - currentExp);
                    description += $"{poke.Name} got {exp} exp from winning.\n";
                }

                // Update Pokemon in PostgreSQL
                await dbContext.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(x => x.Happiness, x => x.Happiness + 1)
                        .SetProperty(x => x.Experience, x => x.Experience + exp));
            }

            if (!string.IsNullOrEmpty(description))
                return new CommandResult
                {
                    Embed = new EmbedBuilder()
                        .WithDescription(description)
                        .WithColor(new Color(255, 182, 193))
                        .Build()
                };

            return new CommandResult { Message = "Battle completed successfully!" };
        }

        return new CommandResult { Message = "Battle completed successfully!" };
    }

    /// <summary>
    ///     Execute a 6v6 party duel between two users.
    /// </summary>
    /// <param name="user">The user initiating the duel.</param>
    /// <param name="opponent">The user being challenged.</param>
    /// <param name="context">The interaction context.</param>
    /// <returns>The result of the command execution.</returns>
    public async Task<CommandResult> PartyDuel(IUser user, IUser opponent, IInteractionContext context)
    {
        return await RunPartyDuel(user, opponent, context);
    }

    /// <summary>
    ///     Execute a 6v6 inverse battle between two users.
    /// </summary>
    /// <param name="user">The user initiating the duel.</param>
    /// <param name="opponent">The user being challenged.</param>
    /// <param name="context">The interaction context.</param>
    /// <returns>The result of the command execution.</returns>
    public async Task<CommandResult> InverseDuel(IUser user, IUser opponent, IInteractionContext context)
    {
        return await RunPartyDuel(user, opponent, context, true);
    }

    /// <summary>
    ///     Execute a party duel or inverse battle between two users.
    /// </summary>
    /// <param name="user">The user initiating the duel.</param>
    /// <param name="opponent">The user being challenged.</param>
    /// <param name="context">The interaction context.</param>
    /// <param name="inverseBattle">Whether this is an inverse battle.</param>
    /// <returns>The result of the command execution.</returns>
    private async Task<CommandResult> RunPartyDuel(IUser user, IUser opponent, IInteractionContext context,
        bool inverseBattle = false)
    {
        var battleType = "party duel";
        if (inverseBattle) battleType += " in the **inverse battle ruleset**";

        // Check if user is trying to duel themselves
        if (opponent.Id == user.Id) return new CommandResult { Message = "You cannot duel yourself!" };

        // Ask opponent to accept the duel
        var accepted = await GetOpponentAcceptance(context, opponent, battleType);
        if (!accepted) return new CommandResult { Message = "Duel request was declined or timed out." };

        // Send loading message
        var embed = new EmbedBuilder()
            .WithTitle("Pokemon Battle accepted! Loading...")
            .WithDescription("Please wait")
            .WithColor(new Color(255, 182, 193))
            .WithImageUrl(PREGAME_GIFS[_random.Next(PREGAME_GIFS.Length)])
            .Build();

        await context.Interaction.FollowupAsync(embed: embed);

        // Check cooldowns
        if (!await CheckCooldowns(context, opponent))
            return new CommandResult { Message = "Duel is on cooldown. Please try again later." };

        // Get party Pokemon for each trainer from PostgreSQL
        await using var db = await _dbContextProvider.GetContextAsync();

        // Get party1 information
        var user1Data = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == user.Id);
        if (user1Data == null || user1Data.Party == null || !user1Data.Party.Any(id => id != 0))
            return new CommandResult { Message = $"{user.Username} has not started!\nStart with `/start` first!" };

        var party1 = user1Data.Party.Where(id => id != 0).ToList();
        var raw1 = await db.UserPokemon
            .Where(p => party1.Contains(p.Id) && p.PokemonName != "Egg")
            .OrderBy(p => party1.IndexOf(p.Id))
            .ToListAsyncEF();

        // Get party2 information
        var user2Data = await db.Users.FirstOrDefaultAsyncEF(u => u.UserId == opponent.Id);
        if (user2Data == null || user2Data.Party == null || !user2Data.Party.Any(id => id != 0))
            return new CommandResult { Message = $"{opponent.Username} has not started!\nStart with `/start` first!" };

        var party2 = user2Data.Party.Where(id => id != 0).ToList();
        var raw2 = await db.UserPokemon
            .Where(p => party2.Contains(p.Id) && p.PokemonName != "Egg")
            .OrderBy(p => party2.IndexOf(p.Id))
            .ToListAsyncEF();

        if (!raw1.Any())
            return new CommandResult
                { Message = $"{user.Username} has no pokemon in their party!\nAdd some with `/party` first!" };

        if (!raw2.Any())
            return new CommandResult
                { Message = $"{opponent.Username} has no pokemon in their party!\nAdd some with `/party` first!" };

        // Create Pokemon objects
        var pokes1 = new List<DuelPokemon>();
        foreach (var pdata in raw1)
        {
            var poke = await DuelPokemon.Create(context, pdata);
            pokes1.Add(poke);
        }

        var pokes2 = new List<DuelPokemon>();
        foreach (var pdata in raw2)
        {
            var poke = await DuelPokemon.Create(context, pdata);
            pokes2.Add(poke);
        }

        // Create trainers
        var owner1 = new MemberTrainer(user, pokes1);
        var owner2 = new MemberTrainer(opponent, pokes2);

        // Create battle
        var battle = new Battle(context, context.Channel, owner1, owner2, _mongoDb, inverseBattle);

        // Show team preview
        owner1.Event = new TaskCompletionSource<bool>();
        owner2.Event = new TaskCompletionSource<bool>();
        var previewView = await GenerateTeamPreview(battle);

        await owner1.Event.Task;
        await owner2.Event.Task;

        previewView.Stop();

        // Run the battle
        var winner = await WrappedRun(battle);

        if (winner == null) return new CommandResult { Message = "Battle ended with no winner." };

        // Update achievements and grant XP
        var description = "";

        // Update achievements in PostgreSQL
        if (winner is not MemberTrainer trainer)
            return new CommandResult { Message = "Battle completed successfully!" };
        {
            await using (var dbContext = await _dbContextProvider.GetContextAsync())
            {
                // Update total duels for both participants
                await dbContext.Achievements
                    .Where(a => a.UserId == user.Id || a.UserId == opponent.Id)
                    .ExecuteUpdateAsync(a => a
                        .SetProperty(x => x.DuelsTotal, x => x.DuelsTotal + 1));

                // Update specific duel type win count
                if (inverseBattle)
                    await dbContext.Achievements
                        .Where(a => a.UserId == trainer.Id)
                        .ExecuteUpdateAsync(a => a
                            .SetProperty(x => x.DuelInverseWins, x => x.DuelInverseWins + 1));
                else
                    await dbContext.Achievements
                        .Where(a => a.UserId == trainer.Id)
                        .ExecuteUpdateAsync(a => a
                            .SetProperty(x => x.DuelPartyWins, x => x.DuelPartyWins + 1));
            }

            // Grant XP to winner's Pokemon that participated
            foreach (var poke in winner.Party)
            {
                if (poke.Hp == 0 || !poke.EverSentOut) continue;

                await using var dbContext = await _dbContextProvider.GetContextAsync();

                // Get the Pokemon's held item and current XP
                var data = await dbContext.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .Select(p => new { p.HeldItem, p.Experience })
                    .FirstOrDefaultAsyncEF();

                if (data == null) continue;

                var heldItem = data.HeldItem.ToLower();
                var currentExp = data.Experience;

                var exp = 0;
                if (heldItem != "xp-block")
                {
                    exp = 150 * poke.Level / 7;
                    double expTotal = exp;
                    if (heldItem == "lucky-egg")
                    {
                        exp = (int)(exp * 2.5);
                        expTotal = exp;
                    }

                    // Max int for the exp column
                    exp = Math.Min(exp, 2147483647 - currentExp);
                    description += $"{poke.Name} got {exp} exp from winning.\n";
                }

                // Update Pokemon in PostgreSQL
                await dbContext.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(x => x.Happiness, x => x.Happiness + 1)
                        .SetProperty(x => x.Experience, x => x.Experience + exp));
            }

            if (!string.IsNullOrEmpty(description))
                return new CommandResult
                {
                    Embed = new EmbedBuilder()
                        .WithDescription(description)
                        .WithColor(new Color(255, 182, 193))
                        .Build()
                };
        }

        return new CommandResult { Message = "Battle completed successfully!" };
    }

    /// <summary>
    ///     Execute a duel against an NPC trainer.
    /// </summary>
    /// <param name="user">The user initiating the duel.</param>
    /// <param name="context">The interaction context.</param>
    /// <returns>The result of the command execution.</returns>
    public async Task<CommandResult> NpcDuel(IUser user, IInteractionContext context)
    {
        var energyImmune = false;

        // Get user data from PostgreSQL
        await using var db = await _dbContextProvider.GetContextAsync();
        var userData = await db.Users
            .Where(u => u.UserId == user.Id)
            .Select(u => new { u.Energy, u.Inventory, u.Selected })
            .FirstOrDefaultAsyncEF();

        if (userData == null)
            return new CommandResult { Message = "You have not started!\nStart with `/start` first!" };

        // Get selected Pokemon
        var challenger1 = await db.UserPokemon.FirstOrDefaultAsyncEF(p => p.Id == userData.Selected);
        if (challenger1 == null)
            return new CommandResult
                { Message = $"{user.Mention} has not selected a Pokemon!\nSelect one with `/select <id>` first!" };

        if (challenger1.PokemonName.ToLower() == "egg")
            return new CommandResult
            {
                Message = $"{user.Mention} has an egg selected!\nSelect a different pokemon with `/select <id>` first!"
            };

        // Check energy
        if (userData.Energy <= 0)
            return new CommandResult
            {
                Message =
                    "You don't have any energy left!\nYou can get more energy now by voting!\nTry using `/ditto vote`!"
            };

        if (context.Channel.Id == 998291646443704320) // Special channel immune to energy consumption
            energyImmune = true;
        else
            // Reduce energy in PostgreSQL
            await db.Users
                .Where(u => u.UserId == user.Id)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Energy, x => x.Energy - 1));

        // Get NPC Pokemon from PostgreSQL
        var npcPokemon = await db.UserPokemon
            .Where(p =>
                p.Level >= challenger1.Level - 10 &&
                p.Level <= challenger1.Level + 10 &&
                !p.Moves.Contains("tackle") &&
                p.AttackIv <= 31 && p.DefenseIv <= 31 && p.SpecialAttackIv <= 31 &&
                p.SpecialDefenseIv <= 31 && p.SpeedIv <= 31 && p.HpIv <= 31)
            .OrderByDescending(p => p.Id)
            .Take(1000)
            .ToListAsyncEF();

        if (npcPokemon == null || !npcPokemon.Any())
            return new CommandResult { Message = "No suitable NPC Pokemon found. Please try again." };

        var challenger2 = npcPokemon[_random.Next(npcPokemon.Count)];
        challenger2.Nickname = "None";

        // Create Pokemon objects
        var p1Current = await DuelPokemon.Create(context, challenger1);
        var p2Current = await DuelPokemon.Create(context, challenger2);

        // Create trainers
        var owner1 = new MemberTrainer(user, new List<DuelPokemon> { p1Current });
        var owner2 = new NPCTrainer(new List<DuelPokemon> { p2Current });

        // Create battle
        var battle = new Battle(context, context.Channel, owner1, owner2, _mongoDb);

        // Run the battle
        var winner = await WrappedRun(battle);

        if (winner != owner1) return new CommandResult { Message = "You lost to the NPC trainer." };

        if (energyImmune)
            return new CommandResult
                { Message = "Battle completed successfully, but no rewards given in this channel." };

        // Grant credits & XP
        var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(userData.Inventory ?? "{}") ??
                        new Dictionary<string, int>();
        var battleMulti = inventory.GetValueOrDefault("battle-multiplier", 1);

        var creds = Convert.ToUInt64(_random.Next(100, 600));
        creds *= Convert.ToUInt64(Math.Min(battleMulti, 50));
        var description = $"You received {creds} credits for winning the duel!\n\n";

        // Update PostgreSQL
        await using (var dbContext = await _dbContextProvider.GetContextAsync())
        {
            // Update achievements
            await dbContext.Achievements
                .Where(a => a.UserId == user.Id)
                .ExecuteUpdateAsync(a => a
                    .SetProperty(x => x.DuelsTotal, x => x.DuelsTotal + 1)
                    .SetProperty(x => x.NpcWins, x => x.NpcWins + 1));

            // Add credits
            await dbContext.Users
                .Where(u => u.UserId == user.Id)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.MewCoins, x => x.MewCoins + creds));

            // Grant XP to Pokemon
            foreach (var poke in winner.Party)
            {
                // Get the Pokemon's held item and current XP
                var data = await dbContext.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .Select(p => new { p.HeldItem, p.Experience })
                    .FirstOrDefaultAsyncEF();

                if (data == null) continue;

                var heldItem = data.HeldItem.ToLower();
                var currentExp = data.Experience;

                var exp = 0;
                if (heldItem != "xp-block")
                {
                    exp = 150 * poke.Level / 7;
                    if (heldItem == "lucky-egg") exp = (int)(exp * 2.5);
                    // Max int for the exp column
                    exp = Math.Min(exp, 2147483647 - currentExp);
                    description += $"{poke.Name} got {exp} exp from winning.\n";
                }

                // Update Pokemon
                await dbContext.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(x => x.Happiness, x => x.Happiness + 1)
                        .SetProperty(x => x.Experience, x => x.Experience + exp));
            }
        }

        description +=
            "\nConsider joining the [Official Server](https://discord.gg/ditto) if you are a fan of pokemon duels!\n";

        return new CommandResult
        {
            Embed = new EmbedBuilder()
                .WithDescription(description)
                .WithColor(new Color(255, 182, 193))
                .Build()
        };
    }

    /// <summary>
    ///     Updates the ranking between two members based on the result of a duel.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="winner">The winning user.</param>
    /// <param name="loser">The losing user.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<string> UpdateRanks(IInteractionContext context, IUser winner, IUser loser)
    {
        // This is the MAX amount of ELO that can be swapped in any particular match.
        // Matches between players of the same rank will transfer half this amount.
        const int K = 50;

        var R1 = _ranks.GetValueOrDefault(winner.Id, 1000);
        var R2 = _ranks.GetValueOrDefault(loser.Id, 1000);

        var E1 = 1 / (1 + Math.Pow(10, (R2 - R1) / 400.0));
        var E2 = 1 / (1 + Math.Pow(10, (R1 - R2) / 400.0));

        // If tieing is added, this needs to be the score of each player
        const double S1 = 1;
        const double S2 = 0;

        var newR1 = (int)Math.Round(R1 + K * (S1 - E1));
        var newR2 = (int)Math.Round(R2 + K * (S2 - E2));

        _ranks[winner.Id] = newR1;
        _ranks[loser.Id] = newR2;

        var message = "**__Rank Adjustments__**\n" +
                      $"**{winner.Username}**: {R1} -> {newR1} ({(newR1 - R1 >= 0 ? "+" : "")}{newR1 - R1})\n" +
                      $"**{loser.Username}**: {R2} -> {newR2} ({(newR2 - R2 >= 0 ? "+" : "")}{newR2 - R2})";

        await context.Channel.SendMessageAsync(message);
        return message;
    }

    /// <summary>
    ///     Check if the opponent accepts the duel request.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="opponent">The user being challenged.</param>
    /// <param name="battleType">The type of battle.</param>
    /// <returns>True if accepted, false otherwise.</returns>
    private async Task<bool> GetOpponentAcceptance(IInteractionContext context, IUser opponent, string battleType)
    {
        await context.Interaction.DeferAsync();
        var componentBuilder = new ComponentBuilder()
            .WithButton("Accept", "duel_accept", ButtonStyle.Success)
            .WithButton("Reject", "duel_reject", ButtonStyle.Danger);

        var message = await context.Interaction.FollowupAsync(
            $"{opponent.Mention} You have been challenged to {battleType} by {context.User.Username}!",
            components: componentBuilder.Build());

        // Set up a TaskCompletionSource to wait for the response
        var tcs = new TaskCompletionSource<bool>();

        // Store the temporary handler so we can remove it later
        EventHandler.AsyncEventHandler<SocketInteraction> handler = null;

        handler = async interaction =>
        {
            if (interaction is SocketMessageComponent component &&
                component.Message.Id == message.Id &&
                component.User.Id == opponent.Id)
            {
                if (component.Data.CustomId == "duel_accept")
                {
                    await component.UpdateAsync(x => x.Components = new ComponentBuilder().Build());
                    tcs.TrySetResult(true);
                }
                else if (component.Data.CustomId == "duel_reject")
                {
                    await component.UpdateAsync(x => x.Components = new ComponentBuilder().Build());
                    tcs.TrySetResult(false);
                }
            }
        };

        // Add the handler
        _client.InteractionCreated += data =>
        {
            _ = handler.Invoke(data);
            return Task.CompletedTask;
        };

        // Set a timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60)).ContinueWith(_ =>
        {
            tcs.TrySetResult(false);
            return Task.CompletedTask;
        });

        // Wait for the result
        var result = await tcs.Task;

        // Clean up
        _client.InteractionCreated -= handler;

        return result;
    }

    /// <summary>
    ///     Check cooldowns to determine if the duel can proceed.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="opponent">The opponent user.</param>
    /// <returns>True if cooldowns allow the duel, false otherwise.</returns>
    private async Task<bool> CheckCooldowns(IInteractionContext context, IUser opponent)
    {
        try
        {
            // Check channel permissions
            var perms = context.Channel.GetPermissionOverwrites(context.Guild.CurrentUser)?.ToAllowDeny()
                        ?? (context.Channel as IGuildChannel)?.GetPermissionOverwrite(context.Guild.EveryoneRole)
                        ?.ToAllowDeny();

            if (perms != null)
            {
                var (allow, deny) = perms.Value;
                if (!allow.HasFlag(ChannelPermission.SendMessages) ||
                    !allow.HasFlag(ChannelPermission.EmbedLinks) ||
                    !allow.HasFlag(ChannelPermission.AttachFiles) ||
                    deny.HasFlag(ChannelPermission.SendMessages) ||
                    deny.HasFlag(ChannelPermission.EmbedLinks) ||
                    deny.HasFlag(ChannelPermission.AttachFiles))
                {
                    await context.Interaction.FollowupAsync(
                        "I need `send_messages`, `embed_links`, and `attach_files` perms in order to let you duel!");
                    return false;
                }
            }

            // Check user cooldown in Redis
            var db = _cache.Redis.GetDatabase();

            // Get duel cooldown for the user
            var duelReset = await db.HashGetAsync("duelcooldowns", context.User.Id.ToString());

            if (!duelReset.IsNull)
            {
                var resetTime = (double)duelReset;
                if (resetTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    var resetIn = resetTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var cooldown = $"{Math.Round(resetIn)}s";
                    await context.Interaction.FollowupAsync($"Command on cooldown for {cooldown}");
                    return false;
                }
            }

            // Set user cooldown (20 seconds)
            await db.HashSetAsync("duelcooldowns", context.User.Id.ToString(),
                DateTimeOffset.UtcNow.AddSeconds(20).ToUnixTimeSeconds());

            // Check daily duel limit
            if (DateTime.Now > _duelResetTime.Value.AddSeconds(5))
            {
                var tempTime = await db.StringGetAsync("duelcooldownreset");

                if (_duelResetTime.Value.ToString(DATE_FORMAT) == tempTime.ToString())
                {
                    _duelResetTime = DateTime.Now;
                    await db.StringSetAsync("duelcooldownreset", _duelResetTime.Value.ToString(DATE_FORMAT));
                    await db.KeyDeleteAsync("dailyduelcooldowns");
                    await db.HashSetAsync("dailyduelcooldowns", context.User.Id.ToString(), 0);
                    return true;
                }

                _duelResetTime = DateTime.ParseExact(tempTime.ToString(), DATE_FORMAT, null);
                var used = await db.HashGetAsync("dailyduelcooldowns", context.User.Id.ToString());

                if (used.IsNull)
                {
                    await db.HashSetAsync("dailyduelcooldowns", context.User.Id.ToString(), 0);
                    return true;
                }
            }
            else
            {
                var used = await db.HashGetAsync("dailyduelcooldowns", context.User.Id.ToString());

                if (used.IsNull)
                {
                    await db.HashSetAsync("dailyduelcooldowns", context.User.Id.ToString(), 0);
                    return true;
                }

                if ((int)used >= 50)
                {
                    await context.Interaction.FollowupAsync("You have hit the maximum number of duels per day!");
                    return false;
                }

                await db.HashSetAsync("dailyduelcooldowns", context.User.Id.ToString(), (int)used + 1);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duel cooldowns for user {UserId}", context.User.Id);
            return false;
        }
    }

    /// <summary>
    ///     Runs the battle and handles any errors that occur.
    /// </summary>
    /// <param name="battle">The battle to run.</param>
    /// <returns>The winner of the battle, or null if it errored.</returns>
    private async Task<Trainer> WrappedRun(Battle battle)
    {
        Trainer winner = null;
        var duelStart = DateTime.Now;

        try
        {
            winner = await battle.Run();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error in battle");
            await battle.Channel.SendMessageAsync(
                "The bot encountered an unexpected network issue, " +
                "and the duel could not continue. " +
                "Please try again in a few moments.\n" +
                "Note: Do not report this as a bug.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout in battle");
            await battle.Channel.SendMessageAsync(
                "The duel timed out. Please try again in a few moments.");
        }
        catch (Exception ex)
        {
            var uniqueId = battle.Context.Interaction?.Id ?? 0;

            await battle.Channel.SendMessageAsync(
                "`The duel encountered an error.\n`" +
                $"Your error code is **`{uniqueId}`**.\n" +
                "Please post this code in <#1053787533139521637> with " +
                "details about what was happening when this error occurred.");

            // Log the exception
            _logger.LogError(ex, "Exception ID {UniqueId} in battle", uniqueId);

            // Store the battle for debugging
            _games[uniqueId] = battle;

            // Send error messages to error channels
            var stackTrace = ex.ToString();
            await SendErrorMessage(1053049266261725184, uniqueId, stackTrace);
            await SendErrorMessage(784188157712531466, uniqueId, stackTrace);
        }

        // Check for potential cheating between human players
        if (!battle.Trainer1.IsHuman() || !battle.Trainer2.IsHuman()) return winner;
        await CheckForCheating(((MemberTrainer)battle.Trainer1).Id, ((MemberTrainer)battle.Trainer2).Id, duelStart);
        await CheckForCheating(((MemberTrainer)battle.Trainer2).Id, ((MemberTrainer)battle.Trainer1).Id, duelStart);

        return winner;
    }

    /// <summary>
    ///     Send error messages to the specified error channels.
    /// </summary>
    /// <param name="channelId">The channel ID to send the error to.</param>
    /// <param name="uniqueId">The unique ID of the error.</param>
    /// <param name="stackTrace">The stack trace of the error.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SendErrorMessage(ulong channelId, ulong uniqueId, string stackTrace)
    {
        try
        {
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel == null) return;

            // Paginate the stack trace for Discord's message limit
            const int maxLength = 1900;
            for (var i = 0; i < stackTrace.Length; i += maxLength)
            {
                var length = Math.Min(maxLength, stackTrace.Length - i);
                var chunk = stackTrace.Substring(i, length);

                if (i == 0) chunk = $"Exception ID {uniqueId}\n\n{chunk}";

                await channel.SendMessageAsync($"```py\n{chunk}\n```");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error message to channel {ChannelId}", channelId);
        }
    }

    /// <summary>
    ///     Check for potential cheating between players.
    /// </summary>
    /// <param name="checkerId">The user ID to check for cheating.</param>
    /// <param name="targetId">The target user ID.</param>
    /// <param name="duelStart">The time the duel started.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CheckForCheating(ulong checkerId, ulong targetId, DateTime duelStart)
    {
        try
        {
            await using var db = await _dbContextProvider.GetContextAsync();

            var cases = await db.SkyLogs
                .Where(s => s.UserId == checkerId &&
                            s.Time > duelStart.AddMinutes(-30) &&
                            s.Arguments.Contains("mock") && s.Arguments.Contains(targetId.ToString()))
                .OrderByDescending(s => s.Time)
                .ToListAsyncEF();

            if (cases.Any())
            {
                var omitted = Math.Max(0, cases.Count - 10);
                var desc = new StringBuilder();

                desc.AppendLine($"**<@{checkerId}> MAY BE CHEATING IN DUELS!**");
                desc.AppendLine(
                    $"Dueled with <@{targetId}> at <t:{((DateTimeOffset)duelStart).ToUnixTimeSeconds()}:F> and ran the following commands:\n");

                foreach (var r in cases.Take(10))
                {
                    var unixTime = ((DateTimeOffset)r.Time).ToUnixTimeSeconds();
                    desc.AppendLine($"<t:{unixTime}:F>");
                    desc.AppendLine($"`{r.Arguments}`\n");
                }

                if (omitted > 0) desc.AppendLine($"Plus {omitted} omitted commands.");

                desc.AppendLine("Check `skylog` for more information.");

                var embed = new EmbedBuilder()
                    .WithTitle("ALERT")
                    .WithDescription(desc.ToString())
                    .WithColor(new Color(221, 17, 17))
                    .Build();

                var alertChannel = _client.GetChannel(1006200660997456023) as IMessageChannel;
                if (alertChannel != null) await alertChannel.SendMessageAsync(embed: embed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for cheating between users {CheckerId} and {TargetId}", checkerId,
                targetId);
        }
    }

    /// <summary>
    ///     Generates a team preview for the battle.
    /// </summary>
    /// <param name="battle">The battle to generate a preview for.</param>
    /// <returns>The preview view.</returns>
    private async Task<PreviewPromptView> GenerateTeamPreview(Battle battle)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Pokemon Battle accepted! Loading...")
            .WithDescription("Team Preview")
            .WithColor(new Color(255, 182, 193))
            .WithFooter("Who Wins!?")
            .WithImageUrl("attachment://team_preview.png")
            .Build();

        // Prepare data for the team preview API
        const string URL = "http://178.28.0.11:5864/build_team_preview";

        var player1PokemonInfo = battle.Trainer1.Party.Select(pokemon =>
            (pokemon.Name.Replace(" ", "-"), pokemon.Level)).ToList();

        var player1Data = new
        {
            name = battle.Trainer1.Name,
            pokemon_info = player1PokemonInfo.Select(async pokemon =>
                ($"pixel_sprites/{await GetBattleFileName(pokemon.Item1, battle.Context)}", pokemon.Item2)).ToList()
        };

        var player2PokemonInfo = battle.Trainer2.Party.Select(pokemon =>
            (pokemon.Name.Replace(" ", "-"), pokemon.Level)).ToList();

        var player2Data = new
        {
            name = battle.Trainer2.Name,
            pokemon_info = player2PokemonInfo.Select(async pokemon =>
                ($"pixel_sprites/{await GetBattleFileName(pokemon.Item1, battle.Context)}", pokemon.Item2)).ToList()
        };

        // Serialize data for the API
        var parameters = new Dictionary<string, string>
        {
            ["player1_data"] = JsonSerializer.Serialize(player1Data),
            ["player2_data"] = JsonSerializer.Serialize(player2Data)
        };

        // Call the API to generate the preview image
        using var content = new FormUrlEncodedContent(parameters);
        using var response = await _httpClient.PostAsync(URL, content);
        var imageBytes = await response.Content.ReadAsByteArrayAsync();

        using var imageStream = new MemoryStream(imageBytes);
        var previewView = new PreviewPromptView(battle);

        await battle.Channel.SendFileAsync(
            imageStream,
            "team_preview.png",
            embed: embed,
            components: previewView.Build());

        return previewView;
    }

    /// <summary>
    ///     Gets the filename for a Pokemon in battle.
    /// </summary>
    /// <param name="pokemonName">The name of the Pokemon.</param>
    /// <param name="context">The interaction context.</param>
    /// <returns>The filename to use.</returns>
    private async Task<string> GetBattleFileName(string pokemonName, IInteractionContext context)
    {
        // This is a simplified implementation - you would need to replace with your actual logic
        return pokemonName.ToLower() + ".png";
    }

    /// <summary>
    ///     Result data for command execution.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        ///     Gets or sets the text message to display.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        ///     Gets or sets the embed to display.
        /// </summary>
        public Embed Embed { get; init; }

        /// <summary>
        ///     Gets or sets whether the message should be ephemeral.
        /// </summary>
        public bool Ephemeral { get; init; }

        /// <summary>
        ///     Gets whether the command executed successfully.
        /// </summary>
        public bool Success => !string.IsNullOrEmpty(Message) || Embed != null;
    }
}