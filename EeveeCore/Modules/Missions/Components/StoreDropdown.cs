using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Missions.Common;
using EeveeCore.Modules.Missions.Services;
using Serilog;

namespace EeveeCore.Modules.Missions.Components;

/// <summary>
///     Dropdown component for the crystal slime store that handles item purchases.
/// </summary>
public class StoreDropdown : SelectMenuBuilder
{
    /// <summary>
    ///     Initializes a new instance of the StoreDropdown class.
    /// </summary>
    /// <param name="storeItems">Dictionary of available store items.</param>
    /// <param name="missionService">The mission service instance.</param>
    public StoreDropdown(Dictionary<string, StoreItemConfig> storeItems, MissionService missionService)
    {
        WithCustomId("store_dropdown");
        WithPlaceholder("Exchange Crystallized Slime for:");
        WithMinValues(1);
        WithMaxValues(1);

        foreach (var (key, item) in storeItems)
        {
            AddOption(
                item.Name,
                key,
                $"Cost: {item.Cost} Crystallized Slime"
            );
        }
    }
}

/// <summary>
///     Handles interactions for the store dropdown component.
/// </summary>
public class StoreComponents : EeveeCoreSlashModuleBase<MissionService>
{
    // Store items are now loaded from MissionConstants.StoreItems

    /// <summary>
    ///     Handles store dropdown selection.
    /// </summary>
    /// <param name="itemKeys">The selected item keys from the dropdown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("store_dropdown")]
    public async Task HandleStoreDropdown(string[] itemKeys)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            var itemKey = itemKeys[0];
            
            if (!MissionConstants.StoreItems.TryGetValue(itemKey, out var item))
            {
                await FollowupAsync("Invalid item selected.", ephemeral: true);
                return;
            }

            var userId = ctx.User.Id;
            var userBalance = await Service.GetUserCrystalSlimeAsync(userId);

            if (userBalance < item.Cost)
            {
                await FollowupAsync("Not enough crystallized slime.", ephemeral: true);
                return;
            }

            // Process the purchase based on item type
            var success = await ProcessPurchase(userId, itemKey, item);
            
            if (!success)
            {
                await FollowupAsync("Purchase failed. Please try again later.", ephemeral: true);
                return;
            }

            // Deduct the cost
            await Service.DeductCrystalSlimeAsync(userId, item.Cost);

            // Send success message based on item type
            await SendPurchaseSuccessMessage(itemKey, item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing store purchase for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while processing your purchase. Please try again later.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Processes the purchase based on the item type.
    /// </summary>
    /// <param name="userId">The user ID making the purchase.</param>
    /// <param name="itemKey">The item key being purchased.</param>
    /// <param name="item">The item details.</param>
    /// <returns>True if successful, false otherwise.</returns>
    private async Task<bool> ProcessPurchase(ulong userId, string itemKey, StoreItemConfig item)
    {
        try
        {
            switch (itemKey)
            {
                case "friendship-stone":
                    await Service.AddItemToInventoryAsync(userId, "friendship-stone", 1);
                    break;

                case "credits_small":
                    await Service.AddMewCoinsAsync(userId, item.Reward);
                    break;

                case "shadow-essence":
                    var (chain, hunt) = await Service.GetUserChainAndHuntAsync(userId);
                    if (string.IsNullOrEmpty(hunt))
                    {
                        await FollowupAsync("You have not set a shadow hunt yet.", ephemeral: true);
                        return false;
                    }
                    var chainIncrease = new Random().Next(item.MinChainIncrease, item.MaxChainIncrease + 1);
                    await Service.IncreaseChainAsync(userId, chainIncrease);
                    break;

                case "small_chance_ticket":
                    // Start lottery game
                    var amounts = GenerateMeowthTicketAmounts(MissionConstants.LotteryChoiceCount);
                    var lotteryView = new ComponentBuilder()
                        .AddRow(new ActionRowBuilder()
                            .WithButton("Choice 1", $"lottery_choice_0:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary)
                            .WithButton("Choice 2", $"lottery_choice_1:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary)
                            .WithButton("Choice 3", $"lottery_choice_2:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary))
                        .AddRow(new ActionRowBuilder()
                            .WithButton("Choice 4", $"lottery_choice_3:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary)
                            .WithButton("Choice 5", $"lottery_choice_4:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary)
                            .WithButton("Choice 6", $"lottery_choice_5:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary))
                        .AddRow(new ActionRowBuilder()
                            .WithButton("Choice 7", $"lottery_choice_6:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary)
                            .WithButton("Choice 8", $"lottery_choice_7:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary)
                            .WithButton("Choice 9", $"lottery_choice_8:{userId}:{string.Join(",", amounts)}", ButtonStyle.Secondary))
                        .AddRow(new ActionRowBuilder()
                            .WithButton("Keep", $"lottery_keep:{userId}", ButtonStyle.Primary, disabled: true));

                    await FollowupAsync("🎰 **Meowth Ticket Lottery!**\n\nChoose your first button to reveal the prize:\n*You'll then decide whether to keep it or risk it for potentially more!*", components: lotteryView.Build(), ephemeral: true);
                    return true;

                case "vip_token_single":
                case "vip_token_pack":
                    await Service.AddVipTokensAsync(userId, item.VipTokensAmount);
                    break;

                default:
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing purchase for item {ItemKey}", itemKey);
            return false;
        }
    }

    /// <summary>
    ///     Sends a success message based on the purchased item.
    /// </summary>
    /// <param name="itemKey">The item key that was purchased.</param>
    /// <param name="item">The item details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SendPurchaseSuccessMessage(string itemKey, StoreItemConfig item)
    {
        var message = itemKey switch
        {
            "friendship-stone" => $"You bought a {item.Name}! Use it with `/apply friendship-stone`.",
            "credits_small" => $"Successfully purchased {item.Name} for {item.Cost} crystallized slime!",
            "shadow-essence" => "You bought some shadow essence! Your chain has been increased!",
            "small_chance_ticket" => "Good luck with your Meowth Ticket!",
            "vip_token_single" => $"Successfully purchased {item.VipTokensAmount} VIP token for {item.Cost} crystallized slime!",
            "vip_token_pack" => $"Successfully purchased {item.VipTokensAmount} VIP tokens for {item.Cost} crystallized slime!",
            _ => $"Successfully purchased {item.Name}!"
        };

        await FollowupAsync(message, ephemeral: true);
    }

    /// <summary>
    ///     Generates random amounts for Meowth ticket lottery.
    /// </summary>
    /// <param name="numChoices">Number of choices to generate.</param>
    /// <returns>Array of random amounts.</returns>
    private static int[] GenerateMeowthTicketAmounts(int numChoices)
    {
        var amounts = MissionConstants.LotteryAmounts;
        var weights = MissionConstants.LotteryWeights;
        
        var random = new Random();
        var result = new int[numChoices];
        
        for (var i = 0; i < numChoices; i++)
        {
            var totalWeight = weights.Sum();
            var randomWeight = random.Next(totalWeight);
            var currentWeight = 0;
            
            for (var j = 0; j < amounts.Length; j++)
            {
                currentWeight += weights[j];
                if (randomWeight < currentWeight)
                {
                    result[i] = amounts[j];
                    break;
                }
            }
        }
        
        return result;
    }

    /// <summary>
    ///     Handles lottery choice button interactions.
    /// </summary>
    /// <param name="choiceIndex">The index of the choice made.</param>
    /// <param name="userId">The user ID making the choice.</param>
    /// <param name="amounts">Comma-separated amounts for all choices.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("lottery_choice_*:*:*")]
    public async Task HandleLotteryChoice(int choiceIndex, ulong userId, string amounts)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            // Verify the user matches
            if (ctx.User.Id != userId)
            {
                await FollowupAsync("This lottery game is not for you!", ephemeral: true);
                return;
            }

            var amountArray = amounts.Split(',').Select(int.Parse).ToArray();
            if (choiceIndex < 0 || choiceIndex >= amountArray.Length)
            {
                await FollowupAsync("Invalid choice selected.", ephemeral: true);
                return;
            }

            var revealedAmount = amountArray[choiceIndex];

            // Create new view with revealed choice and keep/risk options
            var updatedView = new ComponentBuilder()
                .AddRow(new ActionRowBuilder()
                    .WithButton("Choice 1", $"lottery_choice_0:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 0)
                    .WithButton("Choice 2", $"lottery_choice_1:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 1)
                    .WithButton("Choice 3", $"lottery_choice_2:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 2))
                .AddRow(new ActionRowBuilder()
                    .WithButton("Choice 4", $"lottery_choice_3:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 3)
                    .WithButton("Choice 5", $"lottery_choice_4:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 4)
                    .WithButton("Choice 6", $"lottery_choice_5:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 5))
                .AddRow(new ActionRowBuilder()
                    .WithButton("Choice 7", $"lottery_choice_6:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 6)
                    .WithButton("Choice 8", $"lottery_choice_7:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 7)
                    .WithButton("Choice 9", $"lottery_choice_8:{userId}:{amounts}", ButtonStyle.Secondary, disabled: choiceIndex == 8))
                .AddRow(new ActionRowBuilder()
                    .WithButton("Keep", $"lottery_keep:{userId}:{revealedAmount}", ButtonStyle.Success)
                    .WithButton("Risk It!", $"lottery_risk:{userId}:{amounts}:{choiceIndex}", ButtonStyle.Danger));

            await FollowupAsync(
                $"🎰 **You revealed: {revealedAmount:N0} MewCoins!**\n\n" +
                $"**Decision time!**\n" +
                $"• **Keep**: Take your {revealedAmount:N0} MewCoins and end the game\n" +
                $"• **Risk It**: Forfeit your current amount and pick another button for a chance at more!\n\n" +
                $"⏰ *You have {MissionConstants.LotteryTimeoutSeconds} seconds to decide...*",
                components: updatedView.Build(),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling lottery choice for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while processing your lottery choice.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the keep button for lottery games.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="amount">The amount to award.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("lottery_keep:*:*")]
    public async Task HandleLotteryKeep(ulong userId, int amount)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            // Verify the user matches
            if (ctx.User.Id != userId)
            {
                await FollowupAsync("This lottery game is not for you!", ephemeral: true);
                return;
            }

            // Award the MewCoins
            await Service.AddMewCoinsAsync(userId, (ulong)amount);

            var embed = new EmbedBuilder()
                .WithTitle("🎰 Lottery Complete!")
                .WithDescription($"**Congratulations!**\nYou kept your winnings and received **{amount:N0} MewCoins**!")
                .WithColor(0x00FF00)
                .WithFooter("Thanks for playing the Meowth Ticket Lottery!")
                .Build();

            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling lottery keep for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while processing your lottery keep.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the risk button for lottery games.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="amounts">Comma-separated amounts for all choices.</param>
    /// <param name="previousChoice">The previously selected choice index.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("lottery_risk:*:*:*")]
    public async Task HandleLotteryRisk(ulong userId, string amounts, int previousChoice)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            // Verify the user matches
            if (ctx.User.Id != userId)
            {
                await FollowupAsync("This lottery game is not for you!", ephemeral: true);
                return;
            }

            var amountArray = amounts.Split(',').Select(int.Parse).ToArray();

            // Create new view with previous choice disabled
            var riskView = new ComponentBuilder()
                .AddRow(new ActionRowBuilder()
                    .WithButton("Choice 1", $"lottery_final_0:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 0)
                    .WithButton("Choice 2", $"lottery_final_1:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 1)
                    .WithButton("Choice 3", $"lottery_final_2:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 2))
                .AddRow(new ActionRowBuilder()
                    .WithButton("Choice 4", $"lottery_final_3:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 3)
                    .WithButton("Choice 5", $"lottery_final_4:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 4)
                    .WithButton("Choice 6", $"lottery_final_5:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 5))
                .AddRow(new ActionRowBuilder()
                    .WithButton("Choice 7", $"lottery_final_6:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 6)
                    .WithButton("Choice 8", $"lottery_final_7:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 7)
                    .WithButton("Choice 9", $"lottery_final_8:{userId}:{amounts}", ButtonStyle.Secondary, disabled: previousChoice == 8));

            await FollowupAsync(
                $"🎲 **You chose to risk it!**\n\n" +
                $"Your previous choice is now locked out. Choose your final button:\n" +
                $"⚠️ *This is your final choice - no more chances!*",
                components: riskView.Build(),
                ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling lottery risk for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while processing your lottery risk.", ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the final lottery choice after risking.
    /// </summary>
    /// <param name="choiceIndex">The final choice index.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="amounts">Comma-separated amounts for all choices.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("lottery_final_*:*:*")]
    public async Task HandleLotteryFinalChoice(int choiceIndex, ulong userId, string amounts)
    {
        await DeferAsync(ephemeral: true);

        try
        {
            // Verify the user matches
            if (ctx.User.Id != userId)
            {
                await FollowupAsync("This lottery game is not for you!", ephemeral: true);
                return;
            }

            var amountArray = amounts.Split(',').Select(int.Parse).ToArray();
            if (choiceIndex < 0 || choiceIndex >= amountArray.Length)
            {
                await FollowupAsync("Invalid choice selected.", ephemeral: true);
                return;
            }

            var finalAmount = amountArray[choiceIndex];

            // Award the MewCoins
            await Service.AddMewCoinsAsync(userId, (ulong)finalAmount);

            var embed = new EmbedBuilder()
                .WithTitle("🎰 Final Lottery Result!")
                .WithDescription($"**Your final choice revealed: {finalAmount:N0} Coins!**\n\nYou have been awarded **{finalAmount:N0} MewCoins**!")
                .WithColor(new Color(finalAmount >= 50000 ? 0xFFD700u : finalAmount >= 10000 ? 0x00FF00u : 0xFF4500u))
                .WithFooter("Thanks for playing the Meowth Ticket Lottery!")
                .Build();

            await FollowupAsync(embed: embed, ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling final lottery choice for user {UserId}", ctx.User.Id);
            await FollowupAsync("An error occurred while processing your final lottery choice.", ephemeral: true);
        }
    }
}

// StoreItem class moved to MissionConstants as StoreItemConfig