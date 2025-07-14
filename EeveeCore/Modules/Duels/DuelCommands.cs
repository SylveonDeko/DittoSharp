using System.Text.Json;
using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Duels.Impl;
using EeveeCore.Modules.Duels.Impl.DuelPokemon;
using EeveeCore.Modules.Duels.Services;
using EeveeCore.Services.Impl;
using LinqToDB;
using Serilog;

namespace EeveeCore.Modules.Duels;

/// <summary>
///     Provides Discord slash commands for initiating and managing Pokémon battles.
///     Handles different battle types including challenge, single, party, inverse, and NPC duels.
///     Manages cooldowns, battle initiation, and rewards for NPC battles.
/// </summary>
[Group("duel", "Duel related commands")]
public class PokemonBattleModule : EeveeCoreSlashModuleBase<DuelService>
{
    /// <summary>
    ///     Date format used for storing and parsing timestamps in Redis.
    /// </summary>
    private const string DATE_FORMAT = "MM/dd/yyyy, HH:mm:ss";

    /// <summary>
    ///     Collection of local GIF files to display during battle loading screens.
    /// </summary>
    private static readonly string[] PregameGifs =
    [
        Path.Combine("data", "images", "duel1.gif"),
        Path.Combine("data", "images", "duel2.gif"),
        Path.Combine("data", "images", "duel3.gif"),
        Path.Combine("data", "images", "duel4.gif")
    ];

    private readonly DiscordShardedClient _client;
    private readonly LinqToDbConnectionProvider _db;
    private readonly IMongoService _mongoService;
    private readonly RedisCache _redis;

    /// <summary>
    ///     Initializes a new instance of the PokemonBattleModule class with required dependencies.
    /// </summary>
    /// <param name="mongoService">The MongoDB service for accessing Pokémon data.</param>
    /// <param name="db">The database context provider for Entity Framework operations.</param>
    /// <param name="client">The Discord client for user and channel interactions.</param>
    /// <param name="redis">The Redis cache for cooldown management.</param>
    public PokemonBattleModule(
        IMongoService mongoService,
        LinqToDbConnectionProvider db,
        DiscordShardedClient client,
        RedisCache redis)
    {
        _mongoService = mongoService;
        _db = db;
        _client = client;
        _redis = redis;
    }

    /// <summary>
    ///     Initiates a challenge to another user for a Pokémon battle.
    ///     Provides buttons for the opponent to accept or reject the challenge.
    /// </summary>
    /// <param name="opponent">The user to challenge.</param>
    /// <param name="battleType">The type of battle: normal or inverse.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Initiates a 1v1 single Pokémon duel with another user.
    ///     Checks cooldowns and provides buttons for the opponent to accept or reject.
    /// </summary>
    /// <param name="opponent">The user to duel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Initiates a 6v6 party duel with another user's full party.
    ///     Checks cooldowns and provides buttons for the opponent to accept or reject.
    /// </summary>
    /// <param name="opponent">The user to duel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Initiates a 6v6 inverse battle with another user's full party.
    ///     In inverse battles, type effectiveness is reversed.
    ///     Checks cooldowns and provides buttons for the opponent to accept or reject.
    /// </summary>
    /// <param name="opponent">The user to duel.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Initiates a 1v1 duel with an NPC (AI-controlled) trainer.
    ///     Consumes energy, finds a suitable NPC Pokémon, and runs the battle asynchronously.
    ///     Provides rewards if the player wins.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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
            await using var dbContext = await _db.GetConnectionAsync();

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
            if (userData.Energy <= 0 &&
                ctx.Channel.Id != 1351200647743275142) // Skip energy check in designated channel
            {
                await FollowupAsync(
                    "You don't have any energy left!");
                return;
            }

            // Deduct energy (except in special channel)
            var energyImmune = ctx.Channel.Id == 1351200647743275142;
            if (!energyImmune)
            {
                userData.Energy -= 1;
                await dbContext.UpdateAsync(userData);
            }

            // Create loading embed with local GIF attachment
            var selectedGifPath = PregameGifs[new Random().Next(PregameGifs.Length)];
            var loadingEmbed = new EmbedBuilder()
                .WithTitle("Pokemon Battle loading...")
                .WithDescription("Preparing your battle against an NPC trainer!")
                .WithColor(new Color(255, 182, 193));

            if (File.Exists(selectedGifPath))
            {
                loadingEmbed.WithImageUrl("attachment://duel.gif");
                await using var fileStream = new FileStream(selectedGifPath, FileMode.Open, FileAccess.Read);
                var fileAttachment = new FileAttachment(fileStream, "duel.gif");
                await FollowupWithFileAsync(embed: loadingEmbed.Build(), attachment: fileAttachment);
            }
            else
            {
                await FollowupAsync(embed: loadingEmbed.Build());
            }

            // Find an NPC Pokemon of similar level (remove complex array operations)
            var npcPokemonList = await dbContext.UserPokemon
                .Where(p => p.Level >= selectedPokemon.Level - 10 &&
                            p.Level <= selectedPokemon.Level + 10 &&
                            p.AttackIv <= 31 &&
                            p.DefenseIv <= 31 &&
                            p.SpecialAttackIv <= 31 &&
                            p.SpecialDefenseIv <= 31 &&
                            p.SpeedIv <= 31 &&
                            p.HpIv <= 31)
                .OrderByDescending(p => p.Id)
                .Take(1000)
                .ToListAsync();

            // Filter out Pokemon with "tackle" moves in memory (avoid LinqToDB translation issues)
            if (npcPokemonList.Count > 0)
            {
                npcPokemonList = npcPokemonList
                    .Where(p => p.Moves == null || !p.Moves.Any(m => m != null && m.Equals("tackle", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

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
            var playerTrainer = new MemberTrainer(ctx.User, [playerPokemon]);
            var npcTrainer = new NPCTrainer([npcPokemonDuel]);

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

    /// <summary>
    ///     Handles rewards for winning an NPC battle.
    ///     Grants credits, updates achievements, and gives XP to Pokémon that participated.
    ///     Applies battle multipliers from user inventory if present.
    /// </summary>
    /// <param name="userId">The Discord ID of the user who won.</param>
    /// <param name="winner">The winning Trainer object.</param>
    /// <param name="battle">The Battle object that was completed.</param>
    /// <param name="userData">The user's database record for updating.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleNpcRewards(ulong userId, Trainer winner, Battle battle, dynamic userData)
    {
        try
        {
            await using var db = await _db.GetConnectionAsync();

            decimal battleMulti = 1;

            // Handle inventory which might be a string, JSON object, or dictionary
            try
            {
                if (userData.Inventory != null)
                {
                    // Check if Inventory is a string that needs parsing
                    if (userData.Inventory is string inventoryStr)
                    {
                        // Try to parse as JSON if it's a string
                        var inventoryJson = JsonDocument.Parse(inventoryStr);
                        if (inventoryJson.RootElement.TryGetProperty("battle-multiplier", out var element))
                        {
                            if (element.ValueKind == JsonValueKind.Number)
                                battleMulti = element.GetDecimal();
                            else if (element.ValueKind == JsonValueKind.String &&
                                     decimal.TryParse(element.GetString(), out var parsed))
                                battleMulti = parsed;
                        }
                    }
                    // If it's a JObject (Newtonsoft.Json)
                    else if (userData.Inventory.GetType().Name.Contains("JObject"))
                    {
                        // Use dynamic for JObject access
                        var jsonObj = userData.Inventory;
                        if (jsonObj["battle-multiplier"] != null)
                            battleMulti = Convert.ToDecimal(jsonObj["battle-multiplier"]);
                    }
                    // If it's a dictionary
                    else if (userData.Inventory is IDictionary<string, object> dict &&
                             dict.TryGetValue("battle-multiplier", out var value))
                    {
                        if (value is int intVal)
                            battleMulti = intVal;
                        else if (value is long longVal)
                            battleMulti = longVal;
                        else if (value is double doubleVal)
                            battleMulti = Convert.ToDecimal(doubleVal);
                        else if (value is string strVal && decimal.TryParse(strVal, out var parsedVal))
                            battleMulti = parsedVal;
                    }
                }

                // Safety cap for multiplier
                battleMulti = Math.Min(battleMulti, 50m);
            }
            catch
            {
                // Fallback to default if any error occurs
                battleMulti = 1;
            }

            var creds = new Random().Next(100, 600);
            creds = (int)(creds * Math.Min(battleMulti, 50m));

            var desc = $"You received {creds} credits for winning the duel!\n\n";

            // Update achievements
            await db.Achievements
                .Where(a => a.UserId == userId)
                .Set(a => a.DuelsTotal, a => a.DuelsTotal + 1)
                .Set(a => a.NpcWins, a => a.NpcWins + 1)
                .UpdateAsync();

            // Add credits to user
            await db.Users
                .Where(u => u.UserId == userId)
                .Set(u => u.MewCoins, u => u.MewCoins + Convert.ToUInt64(creds))
                .UpdateAsync();

            // Grant XP to winning Pokemon
            foreach (var poke in winner.Party.Where(p => p is { Hp: > 0, EverSentOut: true }))
            {
                // Get Pokemon data
                var pokeData = await db.UserPokemon
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
                await db.UserPokemon
                    .Where(p => p.Id == poke.Id)
                    .Set(p => p.Happiness, p => p.Happiness + 1)
                    .Set(p => p.Experience, p => p.Experience + exp)
                    .UpdateAsync();
            }

            // Add advertisement
            desc +=
                "\nConsider joining the [Official Server](https://discord.gg/EeveeCore) if you are a fan of pokemon duels!\n";

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

    /// <summary>
    ///     Checks cooldowns for PvP duels to prevent spam and enforce daily limits.
    ///     Verifies channel permissions, command cooldowns, and daily duel count.
    /// </summary>
    /// <param name="userId">The Discord ID of the user initiating the duel.</param>
    /// <param name="opponentId">The Discord ID of the opponent.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns true if the duel can proceed,
    ///     false if it is blocked by cooldowns.
    /// </returns>
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

    /// <summary>
    ///     Checks cooldowns for NPC duels, which have simpler rules than PvP duels.
    ///     Verifies channel permissions and command cooldowns but does not enforce daily limits.
    /// </summary>
    /// <param name="userId">The Discord ID of the user initiating the NPC duel.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns true if the duel can proceed,
    ///     false if it is blocked by cooldowns.
    /// </returns>
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