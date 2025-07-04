using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Market.Services;

namespace EeveeCore.Modules.Market.Components;

/// <summary>
///     Handles interaction components for the market system.
///     Processes confirmation buttons for buying Pokemon from the market.
/// </summary>
public class MarketComponents : EeveeCoreSlashModuleBase<MarketService>
{
    private readonly ITradeLockService _tradeLockService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MarketComponents" /> class.
    /// </summary>
    /// <param name="tradeLockService">The trade lock service.</param>
    public MarketComponents(ITradeLockService tradeLockService)
    {
        _tradeLockService = tradeLockService;
    }

    /// <summary>
    ///     Handles the confirmation button for buying a Pokemon from the market.
    /// </summary>
    /// <param name="listingId">The market listing ID to purchase.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("market_buy_confirm:*")]
    public async Task HandleBuyConfirmation(ulong listingId)
    {
        await DeferAsync();

        var success = await _tradeLockService.ExecuteWithTradeLockAsync(ctx.User, async () =>
        {
            var lockSuccess = await Service.ExecuteWithMarketLockAsync(listingId, async () =>
            {
                var result = await Service.BuyPokemonFromMarketAsync(ctx.User.Id, listingId);
                
                if (result.Success)
                {
                    await ctx.Interaction.FollowupAsync(result.Message);
                }
                else
                {
                    await ctx.Interaction.SendEphemeralFollowupErrorAsync(result.Message);
                }
            });

            if (!lockSuccess)
            {
                await ctx.Interaction.SendEphemeralFollowupErrorAsync("Someone is already in the process of buying that pokemon. You can try again later.");
            }
        });

        if (!success)
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync($"{ctx.User.Username} is currently in a trade!");
        }
    }

    /// <summary>
    ///     Handles the cancellation button for buying a Pokemon from the market.
    /// </summary>
    /// <param name="listingId">The market listing ID that was being considered for purchase.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("market_buy_cancel:*")]
    public async Task HandleBuyCancel(ulong listingId)
    {
        await ctx.Interaction.RespondAsync("Purchase cancelled.", ephemeral: true);
    }
}