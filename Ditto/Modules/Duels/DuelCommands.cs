using Discord.Interactions;
using Ditto.Common.ModuleBases;
using Ditto.Database.DbContextStuff;
using Ditto.Modules.Duels.Impl;
using Ditto.Modules.Duels.Services;
using Ditto.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ditto.Modules.Duels;

[Group("duel", "Duel related commands")]
public class PokemonBattleModule : DittoSlashModuleBase<DuelService>
{
    private readonly IMongoService _mongoService;
    private readonly DbContextProvider _db;
    private readonly DiscordShardedClient _client;
    private readonly RedisCache _redis;

    private static readonly string[] PregameGifs =
    [
        "https://skylarr1227.github.io/images/duel1.gif",
        "https://skylarr1227.github.io/images/duel2.gif",
        "https://skylarr1227.github.io/images/duel3.gif",
        "https://skylarr1227.github.io/images/duel4.gif"
    ];

    private const string DATE_FORMAT = "MM/dd/yyyy, HH:mm:ss";

    public PokemonBattleModule(
        IMongoService mongoService,
        DbContextProvider db,
        DiscordShardedClient client,
        RedisCache redis)
    {
        _mongoService = mongoService;
        _db = db;
        _client = client;
        _redis = redis;
    }

    [SlashCommand("challenge", "Challenge another user to a Pokémon battle")]
    public async Task ChallengeBattle(IUser opponent,
        [Choice("Normal", "normal")] [Choice("Inverse", "inverse")]
        string battleType = "normal")
    {
        if (opponent.IsBot)
        {
            await ErrorAsync("You cannot challenge a bot to a battle!");
            return;
        }

        if (opponent.Id == ctx.User.Id)
        {
            await ErrorAsync("You cannot challenge yourself to a battle!");
            return;
        }

        var components = new ComponentBuilder()
            .WithButton("Accept", $"duel:accept:{ctx.User.Id}:{battleType}", ButtonStyle.Success)
            .WithButton("Reject", $"duel:reject:{ctx.User.Id}", ButtonStyle.Danger)
            .Build();

        await RespondAsync(
            $"{opponent.Mention} You have been challenged to a {battleType} battle by {ctx.User.Username}!",
            components: components);
    }

    [SlashCommand("single", "1v1 duel with another user's selected pokemon")]
    public async Task SingleDuel(IUser opponent)
    {
        // Check if the opponent is a bot or self
        if (opponent.IsBot)
        {
            await ErrorAsync("You cannot duel a bot!");
            return;
        }

        if (opponent.Id == ctx.User.Id)
        {
            await ErrorAsync("You cannot duel yourself!");
            return;
        }

        // Check cooldowns
        if (!await CheckDuelCooldowns(ctx.User.Id, opponent.Id))
            return;

        // Create confirmation buttons
        var components = new ComponentBuilder()
            .WithButton("Accept", $"duel:accept:{ctx.User.Id}:single", ButtonStyle.Success)
            .WithButton("Reject", $"duel:reject:{ctx.User.Id}", ButtonStyle.Danger)
            .Build();

        await RespondAsync($"{opponent.Mention} You have been challenged to a 1v1 duel by {ctx.User.Username}!",
            components: components);
    }

    [SlashCommand("party", "6v6 duel with another user's selected party")]
    public async Task PartyDuel(IUser opponent)
    {
        // Check if the opponent is a bot or self
        if (opponent.IsBot)
        {
            await ErrorAsync("You cannot duel a bot!");
            return;
        }

        if (opponent.Id == ctx.User.Id)
        {
            await ErrorAsync("You cannot duel yourself!");
            return;
        }

        // Check cooldowns
        if (!await CheckDuelCooldowns(ctx.User.Id, opponent.Id))
            return;

        // Create confirmation buttons
        var components = new ComponentBuilder()
            .WithButton("Accept", $"duel:accept:{ctx.User.Id}:party", ButtonStyle.Success)
            .WithButton("Reject", $"duel:reject:{ctx.User.Id}", ButtonStyle.Danger)
            .Build();

        await RespondAsync($"{opponent.Mention} You have been challenged to a 6v6 party duel by {ctx.User.Username}!",
            components: components);
    }

    [SlashCommand("inverse", "6v6 inverse battle with another user's selected party")]
    public async Task InverseDuel(IUser opponent)
    {
        // Check if the opponent is a bot or self
        if (opponent.IsBot)
        {
            await ErrorAsync("You cannot duel a bot!");
            return;
        }

        if (opponent.Id == ctx.User.Id)
        {
            await ErrorAsync("You cannot duel yourself!");
            return;
        }

        // Check cooldowns
        if (!await CheckDuelCooldowns(ctx.User.Id, opponent.Id))
            return;

        // Create confirmation buttons
        var components = new ComponentBuilder()
            .WithButton("Accept", $"duel:accept:{ctx.User.Id}:inverse", ButtonStyle.Success)
            .WithButton("Reject", $"duel:reject:{ctx.User.Id}", ButtonStyle.Danger)
            .Build();

        await RespondAsync(
            $"{opponent.Mention} You have been challenged to a 6v6 inverse battle by {ctx.User.Username}!",
            components: components);
    }

    [SlashCommand("npc", "1v1 duel with an NPC AI")]
    public async Task NpcDuel()
    {
        await DeferAsync();

        // Check cooldowns for NPC duels
        if (!await CheckNpcDuelCooldowns(ctx.User.Id))
            return;

        try
        {
            // Check if user is already in a battle
            if (await Service.IsUserInBattle(ctx.User.Id))
            {
                await FollowupAsync("You are already in a battle! Please finish your current battle first.");
                return;
            }

            // Retrieve the user's data and selected Pokemon
            await using var dbContext = await _db.GetContextAsync();

            // Check if user exists and has energy
            var userData = await dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == ctx.User.Id);

            if (userData == null)
            {
                await FollowupAsync("You have not Started! Start with `/start` first!");
                return;
            }

            // Check if user has selected a Pokemon
            var selectedPokemonId = userData.Selected;
            if (selectedPokemonId == 0)
            {
                await FollowupAsync("You have not selected a Pokemon! Select one with `/select <id>` first!");
                return;
            }

            // Get the selected Pokemon
            var selectedPokemon = await dbContext.UserPokemon
                .FirstOrDefaultAsync(p => p.Id == selectedPokemonId);

            if (selectedPokemon == null)
            {
                await FollowupAsync("Failed to find your selected Pokemon!");
                return;
            }

            // Check if the selected Pokemon is an egg
            if (selectedPokemon.PokemonName.Equals("Egg", StringComparison.OrdinalIgnoreCase))
            {
                await FollowupAsync("You have an egg selected! Select a different pokemon with `/select <id>` first!");
                return;
            }

            // Check if user has energy
            if (userData.Energy <= 0 && ctx.Channel.Id != 998291646443704320) // Skip energy check in designated channel
            {
                await FollowupAsync(
                    "You don't have any energy left! You can get more energy now by voting! Try using `/ditto vote`!");
                return;
            }

            // Deduct energy (except in special channel)
            var energyImmune = ctx.Channel.Id == 998291646443704320;
            if (!energyImmune)
            {
                userData.Energy -= 1;
                await dbContext.SaveChangesAsync();
            }

            // Create loading embed
            var loadingEmbed = new EmbedBuilder()
                .WithTitle("Pokemon Battle loading...")
                .WithDescription("Preparing your battle against an NPC trainer!")
                .WithColor(new Color(255, 182, 193))
                .WithImageUrl(PregameGifs[new Random().Next(PregameGifs.Length)]);

            await FollowupAsync(embed: loadingEmbed.Build());

            // Find an NPC Pokemon of similar level
            var npcPokemonList = await dbContext.UserPokemon
                .Where(p => p.Level >= selectedPokemon.Level - 10 &&
                            p.Level <= selectedPokemon.Level + 10 &&
                            !p.Moves.Contains("tackle") &&
                            p.AttackIv <= 31 &&
                            p.DefenseIv <= 31 &&
                            p.SpecialAttackIv <= 31 &&
                            p.SpecialDefenseIv <= 31 &&
                            p.SpeedIv <= 31 &&
                            p.HpIv <= 31)
                .OrderByDescending(p => p.Id)
                .Take(1000)
                .ToListAsync();

            if (npcPokemonList.Count == 0)
            {
                await FollowupAsync("Could not find a suitable NPC Pokemon for battle. Please try again later.");
                return;
            }

            // Randomly select one NPC Pokemon
            var npcPokemon = npcPokemonList[new Random().Next(npcPokemonList.Count)];

            // Create DuelPokemon objects
            var playerPokemon = await DuelPokemon.Create(ctx, selectedPokemon, _mongoService);
            var npcPokemonDuel = await DuelPokemon.Create(ctx, npcPokemon, _mongoService);

            // Create trainers
            var playerTrainer = new MemberTrainer(ctx.User, new List<DuelPokemon> { playerPokemon });
            var npcTrainer = new NPCTrainer(new List<DuelPokemon> { npcPokemonDuel });

            // Create battle
            var battle = new Battle(ctx, ctx.Channel, playerTrainer, npcTrainer, _mongoService);

            // Register battle in Redis with 0 as opponent ID for NPC battles
            var battleId = ctx.Interaction.Id.ToString();
            await Service.RegisterBattle(ctx.User.Id, 0, battle, ctx.Interaction.Id);

            // Run the battle
            _ = Task.Run(async () =>
            {
                try
                {
                    // Run the battle and get winner
                    var winner = await DuelInteractionHandler.RunBattle(battle, _db, _client, Service);

                    // Handle NPC-specific rewards if user won
                    if (winner == playerTrainer && !energyImmune)
                        await HandleNpcRewards(ctx.User.Id, winner, battle, userData);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error running NPC battle");
                    await Service.EndBattle(battle);
                }
            });
        }
        catch (Exception ex)
        {
            await FollowupAsync($"An error occurred: {ex.Message}");
            Log.Error(ex, "Error in NPC duel");
        }
    }

// Separate method to handle NPC battle rewards
    private async Task HandleNpcRewards(ulong userId, Trainer winner, Battle battle, dynamic userData)
    {
        try
        {
            await using var dbContext = await _db.GetContextAsync();

            // Calculate credits reward
            var battleMulti = userData.Inventory?.ContainsKey("battle-multiplier") == true
                ? userData.Inventory["battle-multiplier"]
                : 1;

            var creds = new Random().Next(100, 600);
            creds *= Math.Min(Convert.ToInt32(battleMulti), 50);

            var desc = $"You received {creds} credits for winning the duel!\n\n";

            // Update achievements
            await dbContext.Achievements
                .Where(a => a.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.DuelsTotal, a => a.DuelsTotal + 1)
                    .SetProperty(a => a.NpcWins, a => a.NpcWins + 1));

            // Add credits to user
            await dbContext.Users
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.MewCoins, u => u.MewCoins + Convert.ToUInt64(creds)));

            // Grant XP to winning Pokemon
            foreach (var poke in winner.Party.Where(p => p.Hp > 0 && p.EverSentOut))
            {
                // Get Pokemon data
                var pokeData = await dbContext.UserPokemon
                    .FirstOrDefaultAsync(p => p.Id == poke.Id);

                if (pokeData == null)
                    continue;

                var heldItem = pokeData.HeldItem?.ToLower();
                var currentExp = pokeData.Experience;

                // Skip XP-blocked Pokemon
                if (heldItem == "xp-block")
                    continue;

                // Calculate XP
                var expValue = 150 * poke.Level / 7.0;

                if (heldItem == "lucky-egg") expValue *= 2.5;

                // Limit exp to prevent integer overflow
                var exp = Math.Min((int)expValue, int.MaxValue - currentExp);

                desc += $"{poke.Name} got {exp} exp from winning.\n";

                // Update Pokemon
                await dbContext.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Happiness, p => p.Happiness + 1)
                        .SetProperty(p => p.Experience, p => p.Experience + exp));
            }

            // Add advertisement
            desc +=
                "\nConsider joining the [Official Server](https://discord.gg/ditto) if you are a fan of pokemon duels!\n";

            // Send rewards message
            await battle.Channel.SendMessageAsync(
                embed: new EmbedBuilder()
                    .WithDescription(desc)
                    .WithColor(new Color(255, 182, 193))
                    .Build());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling NPC battle rewards");
        }
    }

    private async Task<bool> CheckDuelCooldowns(ulong userId, ulong opponentId)
    {
        // Check channel permissions
        var perms = (await ctx.Guild.GetCurrentUserAsync()).GetPermissions(ctx.Channel as ITextChannel);

        if (!perms.SendMessages || !perms.EmbedLinks || !perms.AttachFiles)
        {
            await RespondAsync(
                "I need `send_messages`, `embed_links`, and `attach_files` permissions in order to let you duel!",
                ephemeral: true);
            return false;
        }

        try
        {
            // Get per-command cooldown for this user
            var db = _redis.Redis.GetDatabase();
            var duelReset = await db.StringGetAsync($"duelcooldowns:{userId}");
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (duelReset.HasValue && double.Parse(duelReset) > currentTime)
            {
                var resetInSeconds = (int)(double.Parse(duelReset) - currentTime);
                await RespondAsync($"Command on cooldown for {resetInSeconds}s", ephemeral: true);
                return false;
            }

            // Set the per-command cooldown (20 seconds)
            await db.StringSetAsync($"duelcooldowns:{userId}", (currentTime + 20).ToString());

            // Check daily duel count limit
            var duelResetTime = await db.StringGetAsync("duelcooldownreset");
            DateTime resetTime;

            if (!duelResetTime.HasValue)
            {
                resetTime = DateTime.UtcNow;
                await db.StringSetAsync("duelcooldownreset", resetTime.ToString(DATE_FORMAT));
                await db.HashSetAsync("dailyduelcooldowns", userId.ToString(), "0");
            }
            else
            {
                resetTime = DateTime.ParseExact(duelResetTime.ToString(), DATE_FORMAT, null);

                // If it's been more than a day since the last reset
                if (DateTime.UtcNow > resetTime.AddDays(1))
                {
                    resetTime = DateTime.UtcNow;
                    await db.StringSetAsync("duelcooldownreset", resetTime.ToString(DATE_FORMAT));
                    await db.KeyDeleteAsync("dailyduelcooldowns");
                    await db.HashSetAsync("dailyduelcooldowns", userId.ToString(), "0");
                }
            }

            // Get current daily duel count
            var usedCount = await db.HashGetAsync("dailyduelcooldowns", userId.ToString());
            var used = 0;

            if (usedCount.HasValue)
                used = int.Parse(usedCount.ToString());
            else
                await db.HashSetAsync("dailyduelcooldowns", userId.ToString(), "0");

            // Check if user has hit the daily limit
            if (used >= 50)
            {
                await RespondAsync("You have hit the maximum number of duels per day!", ephemeral: true);
                return false;
            }

            // Increment daily duel count
            await db.HashSetAsync("dailyduelcooldowns", userId.ToString(), (used + 1).ToString());

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking duel cooldowns");
            await RespondAsync("An error occurred checking cooldowns. Please try again later.", ephemeral: true);
            return false;
        }
    }

    // Simplified cooldown check for NPC duels
    private async Task<bool> CheckNpcDuelCooldowns(ulong userId)
    {
        // Check channel permissions
        var perms = (await ctx.Guild.GetCurrentUserAsync()).GetPermissions(ctx.Channel as ITextChannel);

        if (!perms.SendMessages || !perms.EmbedLinks || !perms.AttachFiles)
        {
            await FollowupAsync(
                "I need `send_messages`, `embed_links`, and `attach_files` permissions in order to let you duel!");
            return false;
        }

        try
        {
            // Get per-command cooldown for this user
            var db = _redis.Redis.GetDatabase();
            var duelReset = await db.StringGetAsync($"duelcooldowns:{userId}");
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (duelReset.HasValue && double.Parse(duelReset) > currentTime)
            {
                var resetInSeconds = (int)(double.Parse(duelReset) - currentTime);
                await FollowupAsync($"Command on cooldown for {resetInSeconds}s");
                return false;
            }

            // Set the per-command cooldown (20 seconds)
            await db.StringSetAsync($"duelcooldowns:{userId}", (currentTime + 20).ToString());

            return true; // NPC duels don't count against daily limit
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking NPC duel cooldowns");
            await FollowupAsync("An error occurred checking cooldowns. Please try again later.");
            return false;
        }
    }
}