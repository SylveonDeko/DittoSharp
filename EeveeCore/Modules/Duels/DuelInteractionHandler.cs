using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Database.DbContextStuff;
using EeveeCore.Modules.Duels.Extensions;
using EeveeCore.Modules.Duels.Impl;
using EeveeCore.Modules.Duels.Impl.Helpers;
using EeveeCore.Modules.Duels.Impl.Move;
using EeveeCore.Modules.Duels.Services;
using EeveeCore.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EeveeCore.Modules.Duels;

/// <summary>
///     Handles component interactions for Pokémon battles.
///     Processes user input via buttons and commands during battles,
///     manages battle state, and coordinates battle flow.
/// </summary>
/// ///
/// <param name="mongoService">The MongoDB service for accessing Pokémon data.</param>
/// <param name="battleRenderer">The renderer for creating battle images.</param>
/// <param name="dbContext">The database context provider for Entity Framework operations.</param>
/// <param name="client">The Discord client for user and channel interactions.</param>
public class DuelInteractionHandler(
    IMongoService mongoService,
    DuelRenderer battleRenderer,
    DbContextProvider dbContext,
    DiscordShardedClient client) : EeveeCoreSlashModuleBase<DuelService>
{
    private static readonly Dictionary<(ulong, ulong), Battle?> ActiveBattles = new();

    /// <summary>
    ///     Collection of GIFs to display during battle loading screens.
    /// </summary>
    private static readonly string[] PregameGifs =
    [
        "https://skylarr1227.github.io/images/duel1.gif",
        "https://skylarr1227.github.io/images/duel2.gif",
        "https://skylarr1227.github.io/images/duel3.gif",
        "https://skylarr1227.github.io/images/duel4.gif"
    ];

    /// <summary>
    ///     Runs a battle in a background task, handling the battle flow,
    ///     error conditions, and post-battle rewards.
    /// </summary>
    /// <param name="battle">The Battle object to run.</param>
    /// <param name="dbProvider">The database context provider for Entity Framework operations.</param>
    /// <param name="client">The Discord client for user and channel interactions.</param>
    /// <param name="duelService">The duel service for managing battle state.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns the winning Trainer.
    /// </returns>
    public static async Task<Trainer> RunBattle(Battle battle, DbContextProvider dbProvider,
        DiscordShardedClient client, DuelService duelService)
    {
        Trainer? winner = null;
        var battleId = battle.Context.Interaction.Id.ToString();

        try
        {
            // Run the actual battle
            winner = await battle.Run();
        }
        catch (HttpRequestException e)
        {
            await battle.Channel.SendMessageAsync(
                "The bot encountered an unexpected network issue, " +
                "and the duel could not continue. " +
                "Please try again in a few moments.\n" +
                "Note: Do not report this as a bug.");
        }
        catch (TimeoutException e)
        {
            await battle.Channel.SendMessageAsync(
                "The battle timed out. " +
                "Please try again in a few moments.\n" +
                "Note: Do not report this as a bug.");
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

            if (errorChannel != null)
                foreach (var page in PaginateErrorMessage(stackTrace, uniqueId))
                    await errorChannel.SendMessageAsync($"```csharp\n{page}\n```");

            Log.Error(e, "Duels encountered an error.");
        }

        // Handle rewards if we have a winner
        if (winner != null)
            try
            {
                // Check for human vs human battle
                if (battle.Trainer1 is MemberTrainer t1 && battle.Trainer2 is MemberTrainer t2)
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
                    foreach (var poke in winner.Party.Where(poke => poke.Hp != 0 && poke.EverSentOut))
                    {
                        // Get the Pokémon's current data directly from the database
                        var pokeData = await db.UserPokemon
                            .Where(p => p.Id == poke.Id)
                            .FirstOrDefaultAsync();

                        if (pokeData == null)
                            continue;

                        var heldItem = pokeData.HeldItem?.ToLower();
                        var currentExp = pokeData.Experience;

                        // Calculate XP gain
                        if (heldItem == "xp-block") continue;
                        {
                            var expValue = 150 * poke.Level / 7.0;

                            if (heldItem == "lucky-egg") expValue *= 2.5;

                            // Limit exp to prevent integer overflow
                            var exp = Math.Min((int)expValue, int.MaxValue - currentExp);

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

        // Always clean up battle references in Redis
        await duelService.EndBattle(battle);

        return winner;
    }

    /// <summary>
    ///     Splits a long error message into manageable chunks for Discord messages.
    ///     Discord has a character limit for messages, so long stack traces need to be paginated.
    /// </summary>
    /// <param name="message">The error message or stack trace to paginate.</param>
    /// <param name="errorId">The unique identifier for the error.</param>
    /// <returns>
    ///     A list of string chunks, each under the Discord message character limit.
    /// </returns>
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
    ///     Removes a battle from the active battles dictionary.
    ///     Cleans up both user-to-user and reverse mappings.
    /// </summary>
    /// <param name="battle">The Battle object to remove from tracking.</param>
    private static void CleanupBattle(Battle? battle)
    {
        if (battle.Trainer1 is MemberTrainer memberTrainer1 && battle.Trainer2 is MemberTrainer memberTrainer2)
        {
            ActiveBattles.Remove((memberTrainer1.Id, memberTrainer2.Id));
            ActiveBattles.Remove((memberTrainer2.Id, memberTrainer1.Id));
        }
    }

    /// <summary>
    ///     Handles accepting a duel challenge.
    ///     Creates the appropriate battle type based on the challenge parameters.
    /// </summary>
    /// <param name="challengerId">The Discord ID of the user who initiated the challenge.</param>
    /// <param name="battleType">The type of battle: single, party, inverse, or normal.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("duel:accept:*:*")]
    public async Task AcceptDuel(string challengerId, string battleType)
    {
        try
        {
            await DeferAsync();
            var challengerIdUlong = ulong.Parse(challengerId);

            // Check if either user is already in a battle
            if (await Service.IsUserInBattle(challengerIdUlong))
            {
                await ctx.Interaction.SendEphemeralErrorAsync("The challenger is already in a battle!");
                return;
            }

            if (await Service.IsUserInBattle(ctx.User.Id))
            {
                await ctx.Interaction.SendEphemeralErrorAsync("You are already in a battle!");
                return;
            }

            // Check if both users are in the guild
            var challenger = await ctx.Guild.GetUserAsync(challengerIdUlong);
            if (challenger == null)
            {
                await ctx.Interaction.SendEphemeralErrorAsync("Could not find the challenger.");
                return;
            }

            // Create loading embed
            var loadingEmbed = new EmbedBuilder()
                .WithTitle("Pokemon Battle accepted! Loading...")
                .WithDescription("Please wait")
                .WithColor(new Color(255, 182, 193))
                .WithImageUrl(PregameGifs[new Random().Next(PregameGifs.Length)]);

            await FollowupAsync(embed: loadingEmbed.Build());

            switch (battleType)
            {
                // Handle different battle types
                case "single":
                    await HandleSingleBattle(challenger);
                    break;
                case "inverse":
                case "party":
                case "normal":
                {
                    var inverseBattle = battleType == "inverse";
                    await HandlePartyBattle(challenger, inverseBattle);
                    break;
                }
                default:
                    await ctx.Interaction.SendEphemeralErrorAsync("Unknown battle type.");
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error accepting duel");
            await ctx.Interaction.SendEphemeralErrorAsync("An error occurred accepting the duel. Please try again.");
        }
    }

    /// <summary>
    ///     Handles rejecting a duel challenge.
    ///     Updates the original challenge message to indicate rejection.
    /// </summary>
    /// <param name="challengerId">The Discord ID of the user who initiated the challenge.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("duel:reject:*")]
    public async Task RejectDuel(string challengerId)
    {
        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = $"{ctx.User.Username} rejected the battle challenge.";
            properties.Components = new ComponentBuilder().Build();
        });
    }

    /// <summary>
    ///     Handles the request to select a lead Pokémon at the start of a battle.
    ///     Displays buttons for each available Pokémon in the trainer's party.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:select_lead")]
    public async Task SelectLead()
    {
        await DeferAsync();
        // Find battle for this user
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        // Find the trainer
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        // Check if they already selected
        if (trainer.Event.Task.IsCompleted)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You have already selected a lead Pokémon!");
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

    /// <summary>
    ///     Handles the selection of a lead Pokémon at the start of a battle.
    ///     Sets the chosen Pokémon as the current active Pokémon for the trainer.
    /// </summary>
    /// <param name="index">The index of the selected Pokémon in the party.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:lead:*")]
    public async Task SelectLeadPokemon(int index)
    {
        await DeferAsync(true);
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        if (trainer.Event.Task.IsCompleted)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You have already selected a lead Pokémon!");
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
            await ctx.Interaction.SendEphemeralErrorAsync($"Error selecting Pokémon: {ex.Message}");
        }
    }

    /// <summary>
    ///     Displays available battle actions for the current turn.
    ///     Shows move buttons, swap option, and other actions based on the battle state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:actions")]
    public async Task ViewActions()
    {
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;
        var opponent = castTrainer1?.Id == ctx.User.Id ? battle.Trainer2 : battle.Trainer1;

        if (battle.Turn != battle.CurrentInteractionTurn())
        {
            await ctx.Interaction.SendEphemeralErrorAsync("This button has expired.");
            return;
        }

        if (trainer.SelectedAction != null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You have already selected an action.");
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

    /// <summary>
    ///     Handles the selection of a move during battle.
    ///     Sets the chosen move as the trainer's action for the current turn.
    /// </summary>
    /// <param name="moveIdStr">The index of the selected move or "struggle" for Struggle move.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:move:*")]
    public async Task SelectMove(string moveIdStr)
    {
        await DeferAsync();
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        if (trainer.SelectedAction != null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You have already selected an action!");
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

        await ctx.Interaction.SendEphemeralFollowupConfirmAsync(
            $"You picked {selectedMove.PrettyName}. Waiting for opponent.");
    }

    /// <summary>
    ///     Displays available Pokémon to swap to during battle.
    ///     Shows buttons for each valid swap option based on battle conditions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:swap_request")]
    public async Task SwapRequest()
    {
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;
        var opponent = castTrainer1?.Id == ctx.User.Id ? battle.Trainer2 : battle.Trainer1;

        if (trainer.SelectedAction != null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You have already selected an action!");
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

    /// <summary>
    ///     Handles the selection of a Pokémon to swap to during battle.
    ///     Sets the swap action as the trainer's action for the current turn.
    /// </summary>
    /// <param name="index">The index of the selected Pokémon in the party.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:swap:*")]
    public async Task SwapPokemon(int index)
    {
        await DeferAsync();
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;

        if (trainer.SelectedAction != null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You have already selected an action!");
            return;
        }

        try
        {
            trainer.SelectedAction = new Trainer.SwitchAction(index);
            trainer.Event.SetResult(true);

            await ctx.Interaction.SendEphemeralFollowupConfirmAsync(
                $"You picked {trainer.Party[index].Name}. Waiting for opponent.");
        }
        catch (Exception ex)
        {
            await ctx.Interaction.SendEphemeralErrorAsync($"Error selecting Pokémon: {ex.Message}");
        }
    }

    /// <summary>
    ///     Displays a confirmation prompt for forfeiting a battle.
    ///     Shows confirm and cancel buttons to prevent accidental forfeits.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Handles confirmation of a battle forfeit.
    ///     Signals the forfeit to the battle system and updates the message.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:forfeit_confirm")]
    public async Task ForfeitConfirm()
    {
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
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

    /// <summary>
    ///     Handles cancellation of a battle forfeit.
    ///     Updates the message to indicate the forfeit was cancelled.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:forfeit_cancel")]
    public async Task ForfeitCancel()
    {
        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = "Not forfeiting.";
            properties.Components = new ComponentBuilder().Build();
        });
    }

    /// <summary>
    ///     Toggles the mega evolution state for the current Pokémon.
    ///     Updates the battle action buttons to reflect the new state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:mega_toggle")]
    public async Task ToggleMegaEvolution()
    {
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
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

    /// <summary>
    ///     Displays available Pokémon to swap to when a mid-turn swap is required.
    ///     Shows buttons for each valid swap option.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:view_swap")]
    public async Task ViewSwapOptions()
    {
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
            return;
        }

        var castTrainer1 = battle.Trainer1 as MemberTrainer;
        var trainer = castTrainer1?.Id == ctx.User.Id ? battle.Trainer1 : battle.Trainer2;
        var opponent = castTrainer1?.Id == ctx.User.Id ? battle.Trainer2 : battle.Trainer1;

        if (battle.Turn != battle.CurrentSwapTurn())
        {
            await ctx.Interaction.SendEphemeralErrorAsync("This button has expired.");
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

    /// <summary>
    ///     Handles the selection of a Pokémon during a mid-turn swap.
    ///     Switches to the selected Pokémon and signals completion of the swap.
    /// </summary>
    /// <param name="index">The index of the selected Pokémon in the party.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("battle:mid_swap:*")]
    public async Task MidSwapPokemon(int index)
    {
        var battle = Service.FindBattle(ctx.User.Id);
        if (battle == null)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You are not in a battle!");
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
            await ctx.Interaction.SendEphemeralErrorAsync($"Error selecting Pokémon: {ex.Message}");
        }
    }

    /// <summary>
    ///     Sets up and handles a 1v1 single Pokémon battle.
    ///     Verifies both trainers have valid selected Pokémon and creates the battle.
    /// </summary>
    /// <param name="challenger">The Discord user who initiated the challenge.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleSingleBattle(IUser challenger)
    {
        await using var db = await dbContext.GetContextAsync();

        // Get challenger's selected Pokemon
        var challengerSelected = await db.Users
            .Where(u => u.UserId == challenger.Id)
            .Select(u => u.Selected)
            .FirstOrDefaultAsync();

        if (challengerSelected == 0)
        {
            await ctx.Interaction.SendEphemeralErrorAsync(
                $"{challenger.Username} has not selected a Pokemon! Select one with `/select <id>` first!");
            return;
        }

        var challengerPoke = await db.UserPokemon
            .FirstOrDefaultAsync(p => p.Id == challengerSelected);

        if (challengerPoke == null || challengerPoke.PokemonName.Equals("Egg", StringComparison.OrdinalIgnoreCase))
        {
            await ctx.Interaction.SendEphemeralErrorAsync($"{challenger.Username} has no valid Pokemon selected!");
            return;
        }

        // Get opponent's selected Pokemon
        var opponentSelected = await db.Users
            .Where(u => u.UserId == ctx.User.Id)
            .Select(u => u.Selected)
            .FirstOrDefaultAsync();

        if (opponentSelected == 0)
        {
            await ctx.Interaction.SendEphemeralErrorAsync(
                "You have not selected a Pokemon! Select one with `/select <id>` first!");
            return;
        }

        var opponentPoke = await db.UserPokemon
            .FirstOrDefaultAsync(p => p.Id == opponentSelected);

        if (opponentPoke == null || opponentPoke.PokemonName.Equals("Egg", StringComparison.OrdinalIgnoreCase))
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You have no valid Pokemon selected!");
            return;
        }

        // Create DuelPokemon objects
        var challengerDuelPoke = await DuelPokemon.Create(ctx, challengerPoke, mongoService);
        var opponentDuelPoke = await DuelPokemon.Create(ctx, opponentPoke, mongoService);

        // Create trainers
        var trainer1 = new MemberTrainer(challenger, [challengerDuelPoke]);
        var trainer2 = new MemberTrainer(ctx.User, [opponentDuelPoke]);

        // Create battle
        var battle = new Battle(ctx, ctx.Channel, trainer1, trainer2, mongoService);

        // Store battle in local dictionary
        ActiveBattles[(challenger.Id, ctx.User.Id)] = battle;

        // Register battle in Redis
        var battleId = ctx.Interaction.Id.ToString();
        await Service.RegisterBattle(challenger.Id, ctx.User.Id, battle, ctx.Interaction.Id);

        // Update the original response
        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = $"Battle between {trainer1.Name} and {trainer2.Name} has begun!";
            properties.Components = new ComponentBuilder().Build();
        });

        // Start battle immediately
        _ = RunBattle(battle, dbContext, client, Service);
    }

    /// <summary>
    ///     Sets up and handles a party battle (6v6) with all Pokémon in both trainers' parties.
    ///     Verifies both trainers have valid parties and creates the battle.
    /// </summary>
    /// <param name="challenger">The Discord user who initiated the challenge.</param>
    /// <param name="inverseBattle">Whether this is an inverse battle (type effectiveness is reversed).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandlePartyBattle(IUser challenger, bool inverseBattle)
    {
        // Get party Pokemon for both users
        var challengerPokemon = await Service.GetUserPokemonParty(challenger.Id, ctx);
        var opponentPokemon = await Service.GetUserPokemonParty(ctx.User.Id, ctx);

        if (challengerPokemon.Count == 0)
        {
            await ctx.Interaction.SendEphemeralErrorAsync(
                $"{challenger.Username} doesn't have any Pokemon in their party!");
            return;
        }

        if (opponentPokemon.Count == 0)
        {
            await ctx.Interaction.SendEphemeralErrorAsync("You don't have any Pokemon in your party!");
            return;
        }

        // Create trainers
        var trainer1 = new MemberTrainer(challenger, challengerPokemon);
        var trainer2 = new MemberTrainer(ctx.User, opponentPokemon);

        // Create battle
        var battle = new Battle(ctx, ctx.Channel, trainer1, trainer2, mongoService, inverseBattle);

        // Store in local dictionary
        ActiveBattles[(challenger.Id, ctx.User.Id)] = battle;

        // Register in Redis
        var battleId = ctx.Interaction.Id.ToString();
        await Service.RegisterBattle(challenger.Id, ctx.User.Id, battle, ctx.Interaction.Id);

        // Update the original response
        await ctx.Interaction.ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = $"Battle between {trainer1.Name} and {trainer2.Name} has begun!";
            properties.Components = new ComponentBuilder().Build();
        });

        // Generate team preview and set up events for selection
        battle.Trainer1.Event = new TaskCompletionSource<bool>();
        battle.Trainer2.Event = new TaskCompletionSource<bool>();

        await battleRenderer.GenerateTeamPreview(battle);

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
                await RunBattle(battle, dbContext, client, Service);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error waiting for lead Pokémon selection");
                await battle.Channel.SendMessageAsync("An error occurred starting the battle. Please try again.");
                await Service.EndBattle(battle);
            }
        });
    }
}