using System.Text;
using Discord.Interactions;
using Ditto.Common.ModuleBases;
using Ditto.Database.DbContextStuff;
using Ditto.Modules.Duels.Impl;
using Ditto.Modules.Duels.Impl.Helpers;
using Ditto.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SkiaSharp;

namespace Ditto.Modules.Duels;

[Group("duel", "Duel related commands")]
public class PokemonBattleModule(IMongoService mongoService) : DittoSlashModuleBase<BattleService>
{
    private readonly IMongoService _mongoService = mongoService;

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

        await RespondAsync($"{opponent.Mention} You have been challenged to {battleType} by {ctx.User.Username}!",
            components: components);
    }
}

public class BattleService : INService
{
    private readonly IMongoService _mongoService;
    private readonly HttpClient _httpClient;
    private readonly string _resourcePath;
    private readonly DbContextProvider db;
    private readonly DiscordShardedClient client;

    public BattleService(IMongoService mongoService, IServiceProvider services, DbContextProvider db,
        DiscordShardedClient client)
    {
        _mongoService = mongoService;
        this.db = db;
        this.client = client;
        _httpClient = new HttpClient();
        _resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
    }

    /// <summary>
    ///     Gets a user's Pokémon party from the database
    /// </summary>
    public async Task<List<DuelPokemon>> GetUserPokemonParty(ulong userId, IInteractionContext ctx)
    {
        var duelPokemon = new List<DuelPokemon>();

        try
        {
            await using var _db = await db.GetContextAsync();
            // Get the user's party array directly
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Party == null) return duelPokemon; // Return empty list

            // Filter out zeros from the party array
            var partyIds = user.Party.Where(id => id > 0).ToList();
            if (!partyIds.Any()) return duelPokemon;

            // Fetch all Pokémon data for the party in a single query
            var partyPokemon = await _db.UserPokemon
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
                    client.GetUser(userId),
                    duelPokemon
                );

                // Set the owner reference for each Pokémon
                foreach (var pokemon in duelPokemon) pokemon.Owner = trainer;
            }

            return duelPokemon;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured.");

            return [];
        }
    }
}

public class PokemonBattleInteractions(IMongoService mongoService, BattleRenderer battleRenderer, DbContextProvider db, DiscordShardedClient client)
    : DittoSlashModuleBase<BattleService>
{
    /// <summary>
    ///     Runs a battle in a background task, handling errors similar to the original Python implementation
    /// </summary>
    public static async Task RunBattle(Battle battle, DbContextProvider dbProvider, DiscordShardedClient client)
    {
        var duelStart = DateTime.Now;
        Trainer winner = null;

        try
        {
            // Run the actual battle
            winner = await battle.Run();

            if (winner == null) return; // Battle ended without a winner (likely due to error or forfeit)
        }
        catch (HttpRequestException e)
        {
            await battle.Channel.SendMessageAsync(
                "The bot encountered an unexpected network issue, " +
                "and the duel could not continue. " +
                "Please try again in a few moments.\n" +
                "Note: Do not report this as a bug.");
            return;
        }
        catch (TimeoutException e)
        {
            await battle.Channel.SendMessageAsync(
                "The battle timed out. " +
                "Please try again in a few moments.\n" +
                "Note: Do not report this as a bug.");
            return;
        }
        catch (Exception e)
        {
            var uniqueId = battle.Context.Interaction.Id;

            await battle.Channel.SendMessageAsync(
                "`The duel encountered an error.\n`" +
                $"Your error code is **`{uniqueId}`**.\n" +
                "Please post this code in the support channel with " +
                "details about what was happening when this error occurred.");

            // Log error to appropriate channels
            var errorChannel = client.GetChannel(1351696540065857597) as IMessageChannel;
            var stackTrace = e.ToString();

            if (errorChannel == null) return;
            // Split message if needed
            foreach (var page in PaginateErrorMessage(stackTrace, uniqueId))
                await errorChannel.SendMessageAsync($"```csharp\n{page}\n```");

            Log.Error(e, "Duels encountered an error.");
            return;
        }

        // At this point we have a winner, handle rewards
        try
        {
            // Check for human vs human battle
            if (battle.Trainer1 is MemberTrainer t1 &&
                battle.Trainer2 is MemberTrainer t2)
            {
                // Handle post-battle rewards, XP gain, etc.
                var description = "";

                await using var db = await dbProvider.GetContextAsync();

                var memWinner = winner as MemberTrainer;
                // Update achievements for both players
                await db.Achievements.Where(a => a.UserId == memWinner.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.DuelPartyWins, a => a.DuelPartyWins + 1));

                await db.Achievements.Where(a => a.UserId == t1.Id || a.UserId == t2.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.DuelsTotal, a => a.DuelsTotal + 1));

                // Grant XP to winning Pokémon
                foreach (var poke in winner.Party)
                {
                    if (poke.Hp == 0 || !poke.EverSentOut)
                        continue;

                    // Get the Pokémon's current data directly from the database
                    var pokeData = await db.UserPokemon
                        .Where(p => p.Id == poke.Id)
                        .FirstOrDefaultAsync();

                    if (pokeData == null)
                        continue;

                    var heldItem = pokeData.HeldItem?.ToLower();
                    var currentExp = pokeData.Experience;

                    // Calculate XP gain
                    if (heldItem != "xp-block")
                    {
                        double expValue = (150 * poke.Level) / 7.0;

                        if (heldItem == "lucky-egg")
                        {
                            expValue *= 2.5;
                        }

                        // Limit exp to prevent integer overflow
                        int exp = Math.Min((int)expValue, int.MaxValue - currentExp);

                        description += $"{poke.Name} got {exp} exp from winning.\n";

                        // Update the Pokémon in the database
                        await db.UserPokemon
                            .Where(p => p.Id == poke.Id)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(p => p.Happiness, p => p.Happiness + 1)
                                .SetProperty(p => p.Experience, p => p.Experience + exp));
                    }
                }

                // Save changes
                await db.SaveChangesAsync();

                // Display XP gains if any
                if (!string.IsNullOrEmpty(description))
                {
                    var rewardEmbed = new EmbedBuilder()
                        .WithDescription(description)
                        .WithColor(new Color(255, 182, 193))
                        .Build();

                    await battle.Channel.SendMessageAsync(embed: rewardEmbed);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the entire battle result
            Log.Error(ex, "Error processing battle rewards");
        }
        finally
        {
            // Clean up battle resources
            CleanupBattle(battle);
        }
    }

    /// <summary>
    ///     Splits a long error message into manageable chunks
    /// </summary>
    private static List<string> PaginateErrorMessage(string message, ulong errorId)
    {
        const int maxLength = 1900; // Limit for code blocks in Discord
        var parts = new List<string>();

        // Add error ID to the first part
        var firstPart = $"Exception ID {errorId}\n\n{message}";

        // Split the message if it's too long
        if (firstPart.Length <= maxLength)
        {
            parts.Add(firstPart);
        }
        else
        {
            // Add the header to the first chunk
            var header = $"Exception ID {errorId}\n\n";
            var remainingLength = maxLength - header.Length;

            parts.Add(header + firstPart.Substring(header.Length, remainingLength));

            // Split the rest
            for (var i = header.Length + remainingLength; i < message.Length; i += maxLength)
            {
                var length = Math.Min(maxLength, message.Length - i);
                parts.Add(message.Substring(i, length));
            }
        }

        return parts;
    }

    /// <summary>
    ///     Removes the battle from active battles
    /// </summary>
    private static void CleanupBattle(Battle battle)
    {
        var memberTrainer1 = battle.Trainer1 as MemberTrainer;
        var memberTrainer2 = battle.Trainer2 as MemberTrainer;

        if (memberTrainer1 != null && memberTrainer2 != null)
        {
            _activeBattles.Remove((memberTrainer1.Id, memberTrainer2.Id));
            _activeBattles.Remove((memberTrainer2.Id, memberTrainer1.Id));
        }
    }

    private static readonly Dictionary<(ulong, ulong), Battle> _activeBattles = new();

    [ComponentInteraction("duel:accept:*:*")]
    public async Task AcceptDuel(string challengerId, string battleType)
    {
        try
        {
            await DeferAsync();
            var challengerIdUlong = ulong.Parse(challengerId);
            var inverseBattle = battleType == "inverse";

            // Check if both users are in the guild
            var challenger = await ctx.Guild.GetUserAsync(challengerIdUlong);
            if (challenger == null)
            {
                await ErrorAsync("Could not find the challenger.");
                return;
            }

            // Get both users' parties
            var challengerParty = await Service.GetUserPokemonParty(challengerIdUlong, ctx);
            var opponentParty = await Service.GetUserPokemonParty(ctx.User.Id, ctx);

            if (challengerParty.Count == 0 || opponentParty.Count == 0)
            {
                await ErrorAsync("One or both users don't have any Pokémon!");
                return;
            }

            // Create trainers
            var trainer1 = new MemberTrainer(challenger, challengerParty);
            var trainer2 = new MemberTrainer(ctx.User, opponentParty);

            // Create battle
            var battle = new Battle(ctx, ctx.Channel, trainer1, trainer2, mongoService, inverseBattle);
            _activeBattles[(challengerIdUlong, ctx.User.Id)] = battle;

            // Generate team preview
            await battleRenderer.GenerateTeamPreview(battle);

            // Modify the original response
            await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"Battle between {trainer1.Name} and {trainer2.Name} has begun!";
                properties.Components = new ComponentBuilder().Build();
            });

            // Create the event wait tasks for each trainer
            battle.Trainer1.Event = new TaskCompletionSource<bool>();
            battle.Trainer2.Event = new TaskCompletionSource<bool>();

            // Start the battle in the background after both trainers select lead Pokémon
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for both trainers to select their Pokémon
                    await Task.WhenAll(
                        battle.Trainer1.Event.Task,
                        battle.Trainer2.Event.Task
                    );

                    // Proceed with the battle
                    await RunBattle(battle, db, client);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error waiting for lead Pokémon selection");
                    await battle.Channel.SendMessageAsync("An error occurred starting the battle. Please try again.");
                }
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error accepting duel");
            await ErrorAsync("An error occurred accepting the duel. Please try again.");
        }
    }

    [ComponentInteraction("duel:reject:*")]
    public async Task RejectDuel(string challengerId)
    {
        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = $"{ctx.User.Username} rejected the battle challenge.";
            properties.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("battle:select_lead")]
    public async Task SelectLead()
    {
        await DeferAsync();
        // Find battle for this user
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        // Find the trainer
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        // Check if they already selected
        if (trainer.Event.Task.IsCompleted)
        {
            await ErrorAsync("You have already selected a lead Pokémon!");
            return;
        }

        var components = new ComponentBuilder();

        // Add buttons for each Pokémon
        for (var i = 0; i < trainer.Party.Count; i++)
        {
            var pokemon = trainer.Party[i];
            components.WithButton(
                $"{pokemon.Name} | {pokemon.Hp}hp",
                $"battle:lead:{i}",
                ButtonStyle.Secondary,
                row: i / 2);
        }

        await ctx.Interaction.FollowupAsync("Pick a Pokémon to lead with:", components: components.Build(),
            ephemeral: true);
    }

    [ComponentInteraction("battle:lead:*")]
    public async Task SelectLeadPokemon(int index)
    {
        await DeferAsync(true);
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        if (trainer.Event.Task.IsCompleted)
        {
            await ErrorAsync("You have already selected a lead Pokémon!");
            return;
        }

        try
        {
            // Switch to the selected Pokémon
            trainer.SwitchPoke(index);

            // Signal that the Pokémon has been selected
            trainer.Event.SetResult(true);

            // Acknowledge the selection to the user
            await ctx.Interaction.FollowupAsync(
                $"You will lead with {trainer.CurrentPokemon.Name}. Waiting for opponent.", ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error selecting lead Pokémon");
            await ErrorAsync($"Error selecting Pokémon: {ex.Message}");
        }
    }

    [ComponentInteraction("battle:actions")]
    public async Task ViewActions()
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;
        var opponent = castTrainer1?.Id == ctx.User.Id ? battle.Trainer2 : battle.Trainer1;

        if (battle.Turn != battle.CurrentInteractionTurn())
        {
            await ErrorAsync("This button has expired.");
            return;
        }

        if (trainer.SelectedAction != null)
        {
            await ErrorAsync("You have already selected an action.");
            return;
        }

        var moveResult = trainer.ValidMoves(opponent.CurrentPokemon);

        // Handle forced moves
        if (moveResult.Type == ValidMovesResult.ResultType.ForcedMove)
        {
            trainer.SelectedAction = new Trainer.MoveAction(moveResult.ForcedMove);
            trainer.Event.SetResult(true);

            await ctx.Interaction.RespondAsync(
                $"You were forced to play: {moveResult.ForcedMove.PrettyName}",
                ephemeral: true);
            return;
        }

        var components = new ComponentBuilder();

        // Handle struggle case
        if (moveResult.Type == ValidMovesResult.ResultType.Struggle)
        {
            var struggleMove = Move.Struggle();
            components.WithButton(struggleMove.PrettyName, "battle:move:struggle", ButtonStyle.Secondary);

            var swapData = trainer.ValidSwaps(opponent.CurrentPokemon, battle);
            components.WithButton("Swap Pokémon", "battle:swap_request", disabled: swapData.Count == 0, row: 0);
            components.WithButton("Forfeit", "battle:forfeit", ButtonStyle.Danger, row: 0);

            await ctx.Interaction.RespondAsync("Pick an action:", components: components.Build(), ephemeral: true);
            return;
        }

        // Add move buttons
        for (var i = 0; i < trainer.CurrentPokemon.Moves.Count; i++)
        {
            var move = trainer.CurrentPokemon.Moves[i];
            var label = $"{move.PrettyName}";
            if (move.Id != 165) // Not Struggle
                label += $" | {move.PP}pp";

            components.WithButton(
                label,
                $"battle:move:{i}",
                ButtonStyle.Secondary,
                disabled: !moveResult.ValidMoveIndexes.Contains(i),
                row: i / 2);
        }

        // Add swap button
        var validSwaps = trainer.ValidSwaps(opponent.CurrentPokemon, battle);
        components.WithButton("Swap Pokémon", "battle:swap_request", disabled: validSwaps.Count == 0, row: 2);

        // Add forfeit button
        components.WithButton("Forfeit", "battle:forfeit", ButtonStyle.Danger, row: 2);

        // Add mega evolution button if applicable
        if (trainer.CurrentPokemon != null &&
            trainer.CurrentPokemon.MegaTypeIds != null &&
            !trainer.HasMegaEvolved)
        {
            var megaStyle = trainer.CurrentPokemon.ShouldMegaEvolve ? ButtonStyle.Success : ButtonStyle.Secondary;
            components.WithButton("Mega Evolve", "battle:mega_toggle", megaStyle, row: 0);
        }

        await ctx.Interaction.RespondAsync("Pick an action:", components: components.Build(), ephemeral: true);
    }

    [ComponentInteraction("battle:move:*")]
    public async Task SelectMove(string moveIdStr)
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        if (trainer.SelectedAction != null)
        {
            await ErrorAsync("You have already selected an action!");
            return;
        }

        Move selectedMove;
        if (moveIdStr == "struggle")
        {
            selectedMove = Move.Struggle();
        }
        else
        {
            var moveIndex = int.Parse(moveIdStr);
            selectedMove = trainer.CurrentPokemon.Moves[moveIndex];
        }

        trainer.SelectedAction = new Trainer.MoveAction(selectedMove);
        trainer.Event.SetResult(true);

        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = $"You picked {selectedMove.PrettyName}. Waiting for opponent.";
            properties.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("battle:swap_request")]
    public async Task SwapRequest()
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;
        var opponent = castTrainer1?.Id == ctx.User.Id ? battle.Trainer2 : battle.Trainer1;

        if (trainer.SelectedAction != null)
        {
            await ErrorAsync("You have already selected an action!");
            return;
        }

        var validSwaps = trainer.ValidSwaps(opponent.CurrentPokemon, battle);
        var components = new ComponentBuilder();

        for (var i = 0; i < trainer.Party.Count; i++)
        {
            var pokemon = trainer.Party[i];
            components.WithButton(
                $"{pokemon.Name} | {pokemon.Hp}hp",
                $"battle:swap:{i}",
                ButtonStyle.Secondary,
                disabled: !validSwaps.Contains(i),
                row: i / 2);
        }

        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = "Pick a Pokémon:";
            properties.Components = components.Build();
        });
    }

    [ComponentInteraction("battle:swap:*")]
    public async Task SwapPokemon(int index)
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        if (trainer.SelectedAction != null)
        {
            await ErrorAsync("You have already selected an action!");
            return;
        }

        try
        {
            trainer.SelectedAction = new Trainer.SwitchAction(index);
            trainer.Event.SetResult(true);

            await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"You picked {trainer.Party[index].Name}. Waiting for opponent.";
                properties.Components = new ComponentBuilder().Build();
            });
        }
        catch (Exception ex)
        {
            await ErrorAsync($"Error selecting Pokémon: {ex.Message}");
        }
    }

    [ComponentInteraction("battle:forfeit")]
    public async Task ForfeitPrompt()
    {
        var components = new ComponentBuilder()
            .WithButton("Forfeit", "battle:forfeit_confirm", ButtonStyle.Danger)
            .WithButton("Cancel", "battle:forfeit_cancel", ButtonStyle.Secondary)
            .Build();

        await ctx.Interaction.RespondAsync("Are you sure you want to forfeit?", components: components,
            ephemeral: true);
    }

    [ComponentInteraction("battle:forfeit_confirm")]
    public async Task ForfeitConfirm()
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        trainer.SelectedAction = null;
        trainer.Event.SetResult(true);

        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = "Forfeited.";
            properties.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("battle:forfeit_cancel")]
    public async Task ForfeitCancel()
    {
        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = "Not forfeiting.";
            properties.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("battle:mega_toggle")]
    public async Task ToggleMegaEvolution()
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;
        var opponent = castTrainer1?.Id == ctx.User.Id ? battle.Trainer2 : battle.Trainer1;

        // Toggle mega evolution
        trainer.CurrentPokemon.ShouldMegaEvolve = !trainer.CurrentPokemon.ShouldMegaEvolve;

        // Rebuild the action menu with updated mega evolution state
        var moveResult = trainer.ValidMoves(opponent.CurrentPokemon);

        var components = new ComponentBuilder();

        // Handle struggle case
        if (moveResult.Type == ValidMovesResult.ResultType.Struggle)
        {
            var struggleMove = Move.Struggle();
            components.WithButton(struggleMove.PrettyName, "battle:move:struggle", ButtonStyle.Secondary);

            var swapData = trainer.ValidSwaps(opponent.CurrentPokemon, battle);
            components.WithButton("Swap Pokémon", "battle:swap_request", disabled: swapData.Count == 0, row: 0);
            components.WithButton("Forfeit", "battle:forfeit", ButtonStyle.Danger, row: 0);

            // Add mega evolution button with updated state
            var megaStyle = trainer.CurrentPokemon.ShouldMegaEvolve ? ButtonStyle.Success : ButtonStyle.Secondary;
            components.WithButton("Mega Evolve", "battle:mega_toggle", megaStyle, row: 1);

            await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
            {
                properties.Content =
                    $"Mega Evolution {(trainer.CurrentPokemon.ShouldMegaEvolve ? "enabled" : "disabled")}. Pick an action:";
                properties.Components = components.Build();
            });
            return;
        }

        // Add move buttons
        for (var i = 0; i < trainer.CurrentPokemon.Moves.Count; i++)
        {
            var move = trainer.CurrentPokemon.Moves[i];
            var label = $"{move.PrettyName}";
            if (move.Id != 165) // Not Struggle
                label += $" | {move.PP}pp";

            components.WithButton(
                label,
                $"battle:move:{i}",
                ButtonStyle.Secondary,
                disabled: !moveResult.ValidMoveIndexes.Contains(i),
                row: i / 2);
        }

        // Add swap button
        var validSwaps = trainer.ValidSwaps(opponent.CurrentPokemon, battle);
        components.WithButton("Swap Pokémon", "battle:swap_request", disabled: validSwaps.Count == 0, row: 2);

        // Add forfeit button
        components.WithButton("Forfeit", "battle:forfeit", ButtonStyle.Danger, row: 2);

        // Add mega evolution button with updated state
        var megaButtonStyle = trainer.CurrentPokemon.ShouldMegaEvolve ? ButtonStyle.Success : ButtonStyle.Secondary;
        components.WithButton("Mega Evolve", "battle:mega_toggle", megaButtonStyle, row: 0);

        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content =
                $"Mega Evolution {(trainer.CurrentPokemon.ShouldMegaEvolve ? "enabled" : "disabled")}. Pick an action:";
            properties.Components = components.Build();
        });
    }

    [ComponentInteraction("battle:view_swap")]
    public async Task ViewSwapOptions()
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;
        var opponent = castTrainer1?.Id == ctx.User.Id ? battle.Trainer2 : battle.Trainer1;

        if (battle.Turn != battle.CurrentSwapTurn())
        {
            await ErrorAsync("This button has expired.");
            return;
        }

        var validSwaps = trainer.ValidSwaps(opponent.CurrentPokemon, battle, false);
        var components = new ComponentBuilder();

        for (var i = 0; i < trainer.Party.Count; i++)
        {
            var pokemon = trainer.Party[i];
            components.WithButton(
                $"{pokemon.Name} | {pokemon.Hp}hp",
                $"battle:mid_swap:{i}",
                ButtonStyle.Secondary,
                disabled: !validSwaps.Contains(i),
                row: i / 2);
        }

        await ctx.Interaction.RespondAsync("Pick a Pokémon to swap to:", components: components.Build(),
            ephemeral: true);
    }

    [ComponentInteraction("battle:mid_swap:*")]
    public async Task MidSwapPokemon(int index)
    {
        var battle = FindBattleForUser(ctx.User.Id);
        if (battle == null)
        {
            await ErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        try
        {
            trainer.SwitchPoke(index, battle.CurrentMidTurn());
            trainer.Event.SetResult(true);

            await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"You picked {trainer.Party[index].Name}.";
                properties.Components = new ComponentBuilder().Build();
            });
        }
        catch (Exception ex)
        {
            await ErrorAsync($"Error selecting Pokémon: {ex.Message}");
        }
    }

    private Battle FindBattleForUser(ulong userId)
    {
        foreach (var battle in _activeBattles.Values)
            if ((battle.Trainer1 as MemberTrainer)?.Id == userId ||
                (battle.Trainer2 as MemberTrainer)?.Id == userId)
                return battle;

        return null;
    }
}

// Add extension properties to Battle class
public static class BattleExtensions
{
    private static readonly Dictionary<Battle, int> _currentInteractionTurns = new();
    private static readonly Dictionary<Battle, int> _currentSwapTurns = new();
    private static readonly Dictionary<Battle, bool> _currentMidTurns = new();

    public static int CurrentInteractionTurn(this Battle battle)
    {
        if (!_currentInteractionTurns.ContainsKey(battle))
            _currentInteractionTurns[battle] = 0;
        return _currentInteractionTurns[battle];
    }

    public static void SetCurrentInteractionTurn(this Battle battle, int turn)
    {
        _currentInteractionTurns[battle] = turn;
    }

    public static int CurrentSwapTurn(this Battle battle)
    {
        if (!_currentSwapTurns.ContainsKey(battle))
            _currentSwapTurns[battle] = 0;
        return _currentSwapTurns[battle];
    }

    public static void SetCurrentSwapTurn(this Battle battle, int turn)
    {
        _currentSwapTurns[battle] = turn;
    }

    public static bool CurrentMidTurn(this Battle battle)
    {
        if (!_currentMidTurns.ContainsKey(battle))
            _currentMidTurns[battle] = false;
        return _currentMidTurns[battle];
    }

    public static void SetCurrentMidTurn(this Battle battle, bool midTurn)
    {
        _currentMidTurns[battle] = midTurn;
    }
}

/// <summary>
///     Renders battle images locally using SkiaSharp
/// </summary>
public class BattleRenderer : INService
{
    private readonly Dictionary<string, SKBitmap> _imageCache = new();
    private readonly HttpClient _httpClient = new();
    private const string ResourcePath = "data/";

    /// <summary>
    ///     Generate and send a team preview image
    /// </summary>
    public async Task<IUserMessage> GenerateTeamPreview(Battle battle)
    {
        // Create embed for team preview
        var embed = new EmbedBuilder()
            .WithTitle("Pokemon Battle accepted! Loading...")
            .WithDescription("Team Preview")
            .WithColor(new Color(255, 182, 193))
            .WithFooter("Who Wins!?");

        // Generate preview image
        using var previewImage = await GenerateTeamPreviewImage(battle);
        using var memoryStream = new MemoryStream();
        previewImage.Encode(SKEncodedImageFormat.Png, 100).SaveTo(memoryStream);
        memoryStream.Position = 0;

        // Create components for selecting lead Pokémon
        var components = new ComponentBuilder()
            .WithButton("Select a lead pokemon", "battle:select_lead")
            .Build();

        // Send the message
        return await battle.Channel.SendFileAsync(
            memoryStream,
            "team_preview.png",
            embed: embed.Build(),
            components: components);
    }

    /// <summary>
    ///     Generate a team preview image
    /// </summary>
    private async Task<SKImage> GenerateTeamPreviewImage(Battle battle)
    {
        var width = 800;
        var height = 400;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Fill background
        canvas.Clear(SKColors.LightGray);

        // Draw header
        using var titlePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 32,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };
        canvas.DrawText("Team Preview", width / 2, 40, titlePaint);

        // Draw trainer names
        using var trainerPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        // Draw trainer 1 name in red
        trainerPaint.Color = new SKColor(255, 0, 0);
        canvas.DrawText(battle.Trainer1.Name, 150, 80, trainerPaint);

        // Draw trainer 2 name in blue
        trainerPaint.Color = new SKColor(0, 0, 255);
        canvas.DrawText(battle.Trainer2.Name, width - 150, 80, trainerPaint);

        // Draw dividing line
        using var linePaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(width / 2, 60, width / 2, height - 20, linePaint);

        // Draw trainer 1's Pokémon
        await DrawTrainerTeam(canvas, battle.Trainer1, 50, 100, 300, 280);

        // Draw trainer 2's Pokémon
        await DrawTrainerTeam(canvas, battle.Trainer2, 450, 100, 300, 280);

        return surface.Snapshot();
    }

    /// <summary>
    ///     Draw a trainer's team on the preview
    /// </summary>
    private async Task DrawTrainerTeam(SKCanvas canvas, Trainer trainer, int x, int y, int width, int height)
    {
        var pokemonPerRow = 2;
        var pokemonWidth = width / pokemonPerRow;
        var pokemonHeight = height / 3;

        using var namePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        using var levelPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        for (var i = 0; i < trainer.Party.Count; i++)
        {
            var pokemon = trainer.Party[i];
            var row = i / pokemonPerRow;
            var col = i % pokemonPerRow;

            var pokemonX = x + col * pokemonWidth;
            var pokemonY = y + row * pokemonHeight;

            // Get Pokémon sprite
            var fileName = await GetPokemonFileName(pokemon);
            var sprite = await LoadPokemonBitmap($"pixel_sprites/{fileName}");

            if (sprite != null)
            {
                // Draw Pokémon sprite
                var rect = new SKRect(pokemonX + 10, pokemonY + 5, pokemonX + 74, pokemonY + 69);
                canvas.DrawBitmap(sprite, rect);

                // Draw Pokémon name and level
                canvas.DrawText(pokemon.Name, pokemonX + 80, pokemonY + 30, namePaint);
                canvas.DrawText($"Lv. {pokemon.Level}", pokemonX + 80, pokemonY + 50, levelPaint);
            }
        }
    }

    /// <summary>
    ///     Generate and send a main battle message
    /// </summary>
    public async Task<IUserMessage> GenerateMainBattleMessage(Battle battle)
    {
        // Create embed for battle
        var embed = new EmbedBuilder()
            .WithTitle($"Battle between {battle.Trainer1.Name} and {battle.Trainer2.Name}")
            .WithColor(new Color(255, 182, 193))
            .WithFooter("Who Wins!?");

        // Generate battle image
        using var battleImage = await GenerateBattleImage(battle);
        using var memoryStream = new MemoryStream();
        battleImage.Encode(SKEncodedImageFormat.Png, 100).SaveTo(memoryStream);
        memoryStream.Position = 0;

        // Create components for battle actions
        var components = new ComponentBuilder()
            .WithButton("View your actions", "battle:actions")
            .Build();

        // Update battle interaction turn
        battle.SetCurrentInteractionTurn(battle.Turn);

        // Send the message
        return await battle.Channel.SendFileAsync(
            memoryStream,
            "battle.png",
            embed: embed.Build(),
            components: components);
    }


    /// <summary>
    ///     Generate a battle image
    /// </summary>
    public async Task<SKImage> GenerateBattleImage(Battle battle)
    {
        var width = 800;
        var height = 450;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Draw background
        await DrawBackground(canvas, battle.BgNum, width, height);

        // Draw weather effects if any
        if (battle.Weather.WeatherType is not null)
            await DrawWeatherEffect(canvas, battle.Weather.Get(), width, height);

        // Draw trick room effect if active
        if (battle.TrickRoom.Active()) DrawTrickRoomEffect(canvas, width, height);

        // Draw Pokémon
        await DrawBattlePokemon(canvas, battle, width, height);

        // Draw HP bars and info
        DrawPokemonStatus(canvas, battle, width, height);

        return surface.Snapshot();
    }

    /// <summary>
    ///     Draw the battle background
    /// </summary>
    private async Task DrawBackground(SKCanvas canvas, int bgNum, int width, int height)
    {
        var bgPath = Path.Combine(ResourcePath, "backgrounds", $"bg{bgNum}.png");

        if (File.Exists(bgPath))
        {
            var background = await LoadBitmapFromFile(bgPath);
            var rect = new SKRect(0, 0, width, height);
            canvas.DrawBitmap(background, rect);
        }
        else
        {
            // Fallback to a gradient background
            using var paint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(0, height),
                    new[] { new SKColor(135, 206, 235), new SKColor(34, 139, 34) },
                    null,
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(0, 0, width, height, paint);
        }
    }

    /// <summary>
    ///     Draw weather effects
    /// </summary>
    private async Task DrawWeatherEffect(SKCanvas canvas, string? weatherType, int width, int height)
    {
        switch (weatherType)
        {
            case "rain":
                DrawRainEffect(canvas, width, height);
                break;
            case "h-sun":
                DrawSunEffect(canvas, width, height);
                break;
            case "sandstorm": // Sandstorm
                DrawSandstormEffect(canvas, width, height);
                break;
            case "hail":
                DrawHailEffect(canvas, width, height);
                break;
            case "fog":
                DrawFogEffect(canvas, width, height);
                break;
        }
    }

    /// <summary>
    ///     Draw rain effect
    /// </summary>
    private void DrawRainEffect(SKCanvas canvas, int width, int height)
    {
        var random = new Random();

        // Draw rain drops
        using var rainPaint = new SKPaint
        {
            Color = new SKColor(150, 200, 255, 180),
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };

        for (var i = 0; i < 100; i++)
        {
            float x1 = random.Next(width);
            float y1 = random.Next(height);
            var x2 = x1 - 10;
            var y2 = y1 + 20;

            canvas.DrawLine(x1, y1, x2, y2, rainPaint);
        }

        // Add a blue overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 150, 30)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw sun effect
    /// </summary>
    private void DrawSunEffect(SKCanvas canvas, int width, int height)
    {
        // Draw sun
        using var sunPaint = new SKPaint
        {
            Color = new SKColor(255, 200, 0, 150),
            IsAntialias = true
        };

        canvas.DrawCircle(width / 2, height / 4, 60, sunPaint);

        // Draw sun rays
        using var rayPaint = new SKPaint
        {
            Color = new SKColor(255, 200, 0, 100),
            IsAntialias = true,
            StrokeWidth = 5
        };

        for (var i = 0; i < 12; i++)
        {
            var angle = i * 30 * Math.PI / 180;
            var x1 = width / 2 + (float)(80 * Math.Cos(angle));
            var y1 = height / 4 + (float)(80 * Math.Sin(angle));
            var x2 = width / 2 + (float)(120 * Math.Cos(angle));
            var y2 = height / 4 + (float)(120 * Math.Sin(angle));

            canvas.DrawLine(x1, y1, x2, y2, rayPaint);
        }

        // Add a yellow overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(255, 200, 0, 30)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw sandstorm effect
    /// </summary>
    private void DrawSandstormEffect(SKCanvas canvas, int width, int height)
    {
        var random = new Random();

        // Draw sand particles
        using var sandPaint = new SKPaint
        {
            Color = new SKColor(210, 180, 140, 150),
            IsAntialias = true
        };

        for (var i = 0; i < 200; i++)
        {
            float x = random.Next(width);
            float y = random.Next(height);
            float size = random.Next(1, 4);

            canvas.DrawRect(x, y, x + size, y + size, sandPaint);
        }

        // Add a sand-colored overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(210, 180, 140, 50)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw hail effect
    /// </summary>
    private void DrawHailEffect(SKCanvas canvas, int width, int height)
    {
        var random = new Random();

        // Draw hail particles
        using var hailPaint = new SKPaint
        {
            Color = new SKColor(220, 240, 255, 200),
            IsAntialias = true
        };

        for (var i = 0; i < 80; i++)
        {
            float x = random.Next(width);
            float y = random.Next(height);
            float size = random.Next(2, 6);

            canvas.DrawCircle(x, y, size, hailPaint);
        }

        // Add a blue-white overlay
        using var overlayPaint = new SKPaint
        {
            Color = new SKColor(200, 220, 255, 30)
        };
        canvas.DrawRect(0, 0, width, height, overlayPaint);
    }

    /// <summary>
    ///     Draw fog effect
    /// </summary>
    private void DrawFogEffect(SKCanvas canvas, int width, int height)
    {
        // Draw fog overlay
        using var fogPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 100)
        };
        canvas.DrawRect(0, 0, width, height, fogPaint);

        // Draw fog patches
        var random = new Random();
        using var patchPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 70),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 20)
        };

        for (var i = 0; i < 15; i++)
        {
            float x = random.Next(width);
            float y = random.Next(height);
            float sizeX = random.Next(50, 150);
            float sizeY = random.Next(20, 50);

            canvas.DrawOval(new SKRect(x, y, x + sizeX, y + sizeY), patchPaint);
        }
    }

    /// <summary>
    ///     Draw trick room effect
    /// </summary>
    private void DrawTrickRoomEffect(SKCanvas canvas, int width, int height)
    {
        // Draw grid pattern
        using var gridPaint = new SKPaint
        {
            Color = new SKColor(180, 100, 220, 100),
            IsAntialias = true,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        // Horizontal lines
        for (var y = 0; y < height; y += 30) canvas.DrawLine(0, y, width, y, gridPaint);

        // Vertical lines
        for (var x = 0; x < width; x += 30) canvas.DrawLine(x, 0, x, height, gridPaint);

        // Add a purple tint
        using var tintPaint = new SKPaint
        {
            Color = new SKColor(180, 100, 220, 20)
        };
        canvas.DrawRect(0, 0, width, height, tintPaint);
    }

    /// <summary>
    ///     Draw the Pokémon on the battle scene
    /// </summary>
    private async Task DrawBattlePokemon(SKCanvas canvas, Battle battle, int width, int height)
    {
        // Draw player's Pokémon (left side)
        if (battle.Trainer1.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer1.CurrentPokemon;
            var directory = "images";

            // Determine skin or default sprite
            if (pokemon.Skin != null && !pokemon.Skin.Contains("verification")) directory = "skins";

            var fileName = await GetPokemonFileName(pokemon);

            if (pokemon.Substitute > 0)
            {
                // Draw substitute
                var substitute = await LoadPokemonBitmap("images/substitute.png");
                if (substitute != null)
                {
                    var rect = new SKRect(100, height - 180, 200, height - 80);
                    canvas.DrawBitmap(substitute, rect);
                }
            }
            else
            {
                // Draw Pokémon
                var sprite = await LoadPokemonBitmap($"{directory}/{fileName}");
                if (sprite != null)
                {
                    var rect = new SKRect(100, height - 200, 220, height - 80);
                    canvas.DrawBitmap(sprite, rect);
                }
            }
        }

        // Draw opponent's Pokémon (right side)
        if (battle.Trainer2.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer2.CurrentPokemon;
            var directory = "images";

            // Determine skin or default sprite
            if (pokemon.Skin != null && !pokemon.Skin.Contains("verification")) directory = "skins";

            var fileName = await GetPokemonFileName(pokemon);

            if (pokemon.Substitute > 0)
            {
                // Draw substitute
                var substitute = await LoadPokemonBitmap("images/substitute.png");
                if (substitute != null)
                {
                    var rect = new SKRect(width - 200, 100, width - 100, 200);
                    canvas.DrawBitmap(substitute, rect);
                }
            }
            else
            {
                // Draw Pokémon
                var sprite = await LoadPokemonBitmap($"{directory}/{fileName}");
                if (sprite != null)
                {
                    var rect = new SKRect(width - 220, 100, width - 100, 220);
                    canvas.DrawBitmap(sprite, rect);
                }
            }
        }
    }

    /// <summary>
    ///     Draw Pokémon status and HP bars
    /// </summary>
    private void DrawPokemonStatus(SKCanvas canvas, Battle battle, int width, int height)
    {
        // Draw player's Pokémon info
        if (battle.Trainer1.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer1.CurrentPokemon;
            DrawPokemonHpBar(canvas, pokemon, 40, height - 65, 280, true);
        }

        // Draw opponent's Pokémon info
        if (battle.Trainer2.CurrentPokemon != null)
        {
            var pokemon = battle.Trainer2.CurrentPokemon;
            DrawPokemonHpBar(canvas, pokemon, width - 320, 40, 280, false);
        }

        // Draw trainer names
        using var trainerPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        canvas.DrawText(battle.Trainer1.Name, 20, height - 20, trainerPaint);
        canvas.DrawText(battle.Trainer2.Name, width - 150, 30, trainerPaint);
    }

    /// <summary>
    ///     Draw a Pokémon's HP bar
    /// </summary>
    private void DrawPokemonHpBar(SKCanvas canvas, DuelPokemon pokemon, float x, float y, float width, bool isPlayer)
    {
        // Draw name and level
        using var namePaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright)
        };

        var nameText = $"{pokemon.Name} Lv.{pokemon.Level}";
        canvas.DrawText(nameText, x, y, namePaint);

        // Draw HP bar background
        using var barBgPaint = new SKPaint
        {
            Color = new SKColor(60, 60, 60, 200),
            IsAntialias = true
        };

        var barRect = new SKRect(x, y + 10, x + width, y + 30);
        canvas.DrawRect(barRect, barBgPaint);

        // Calculate HP percentage
        var hpPercent = (float)pokemon.Hp / pokemon.StartingHp;
        var hpBarWidth = width * hpPercent;

        // Determine HP bar color
        var hpColor = hpPercent switch
        {
            > 0.5f => SKColors.Green,
            > 0.2f => SKColors.Yellow,
            _ => SKColors.Red
        };

        using var hpPaint = new SKPaint();
        hpPaint.Color = hpColor;
        hpPaint.IsAntialias = true;

        var hpRect = new SKRect(x, y + 10, x + hpBarWidth, y + 30);
        canvas.DrawRect(hpRect, hpPaint);

        // Draw HP text
        using var hpTextPaint = new SKPaint();
        hpTextPaint.Color = SKColors.White;
        hpTextPaint.TextSize = 16;
        hpTextPaint.IsAntialias = true;

        var hpText = $"{pokemon.Hp}/{pokemon.StartingHp} HP";
        canvas.DrawText(hpText, x, y + 50, hpTextPaint);

        // Draw status condition if any
        if (pokemon != null && !pokemon.NonVolatileEffect.Current.IsNullOrWhiteSpace())
        {
            using var statusPaint = new SKPaint
            {
                Color = GetStatusColor(pokemon.NonVolatileEffect.Current),
                TextSize = 16,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal,
                    SKFontStyleSlant.Upright)
            };

            var statusText = pokemon.NonVolatileEffect.Current;
            canvas.DrawText(statusText, x + width - 60, y + 50, statusPaint);
        }
    }

    /// <summary>
    ///     Get color for status condition
    /// </summary>
    private SKColor GetStatusColor(string status)
    {
        return status switch
        {
            "burn" => new SKColor(255, 100, 0),
            "freeze" => new SKColor(100, 200, 255),
            "paralysis" => new SKColor(255, 255, 0),
            "poison" or "toxic" => new SKColor(180, 0, 180),
            "sleep" => new SKColor(100, 100, 100),
            _ => SKColors.White
        };
    }

    /// <summary>
    ///     Generate and send battle text message
    /// </summary>
    public async Task GenerateTextBattleMessage(Battle battle)
    {
        if (string.IsNullOrEmpty(battle.Msg))
            return;

        var msg = battle.Msg.Trim();
        battle.Msg = ""; // Clear the message

        // Split message if it's too long
        var embedBuilder = new EmbedBuilder().WithColor(new Color(255, 182, 193));

        // If message is too long, split it
        if (msg.Length > 2000)
        {
            var parts = SplitMessage(msg, 2000);
            foreach (var part in parts)
                await battle.Channel.SendMessageAsync(embed: embedBuilder.WithDescription(part).Build());
        }
        else
        {
            await battle.Channel.SendMessageAsync(embed: embedBuilder.WithDescription(msg).Build());
        }
    }

    /// <summary>
    ///     Split a long message into parts
    /// </summary>
    private List<string> SplitMessage(string message, int maxLength)
    {
        var parts = new List<string>();
        var lines = message.Split('\n');
        var currentPart = new StringBuilder();

        foreach (var line in lines)
        {
            if (currentPart.Length + line.Length + 1 > maxLength)
            {
                parts.Add(currentPart.ToString().Trim());
                currentPart.Clear();
            }

            currentPart.AppendLine(line);
        }

        if (currentPart.Length > 0) parts.Add(currentPart.ToString().Trim());

        return parts;
    }

    /// <summary>
    ///     Get the filename for a Pokémon's sprite
    /// </summary>
    private async Task<string> GetPokemonFileName(DuelPokemon pokemon)
    {
        var baseName = pokemon.Name.Replace(" ", "-").ToLower();
        var variant = "";

        if (pokemon.Shiny)
            variant = "-shiny";
        else if (pokemon.Radiant)
            variant = "-radiant";

        if (pokemon.Skin != null && !pokemon.Skin.Contains("verification"))
            return $"{baseName}{variant}-{pokemon.Skin}.png";

        return $"{baseName}{variant}.png";
    }

    /// <summary>
    ///     Load a Pokémon bitmap
    /// </summary>
    private async Task<SKBitmap> LoadPokemonBitmap(string relativePath)
    {
        var fullPath = Path.Combine(ResourcePath, relativePath);

        // Check if file exists
        if (!File.Exists(fullPath))
        {
            // Try fallback
            var fallbackPath = Path.Combine(ResourcePath, "images", "unknown.png");
            if (File.Exists(fallbackPath))
                return await LoadBitmapFromFile(fallbackPath);

            // Create a placeholder
            return CreatePlaceholderBitmap();
        }

        return await LoadBitmapFromFile(fullPath);
    }

    /// <summary>
    ///     Load bitmap from file
    /// </summary>
    private async Task<SKBitmap> LoadBitmapFromFile(string path)
    {
        // Check cache first
        if (_imageCache.TryGetValue(path, out var cached))
            return cached;

        return await Task.Run(() =>
        {
            try
            {
                var bitmap = SKBitmap.Decode(path);
                _imageCache[path] = bitmap;
                return bitmap;
            }
            catch
            {
                return CreatePlaceholderBitmap();
            }
        });
    }

    /// <summary>
    ///     Create a placeholder bitmap
    /// </summary>
    private SKBitmap CreatePlaceholderBitmap()
    {
        var bitmap = new SKBitmap(64, 64);
        using var canvas = new SKCanvas(bitmap);

        // Fill with question mark
        canvas.Clear(SKColors.LightGray);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 48,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true
        };

        canvas.DrawText("?", 32, 48, paint);

        return bitmap;
    }
}