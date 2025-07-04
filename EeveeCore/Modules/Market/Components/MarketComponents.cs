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

    /// <summary>
    ///     Handles the select menu for viewing Pokemon information from the market list.
    /// </summary>
    /// <param name="values">The selected values from the select menu.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("pokemon_info_select")]
    public async Task HandlePokemonInfoSelect(string[] values)
    {
        if (values.Length == 0 || string.IsNullOrEmpty(values[0]))
        {
            await ctx.Interaction.RespondAsync("No Pokemon selected.", ephemeral: true);
            return;
        }

        // Extract listing ID from the value format "market_info:listingId"
        var selectedValue = values[0];
        if (!selectedValue.StartsWith("market_info:"))
        {
            await ctx.Interaction.RespondAsync("Invalid selection.", ephemeral: true);
            return;
        }

        var listingIdStr = selectedValue.Substring("market_info:".Length);
        if (!ulong.TryParse(listingIdStr, out var listingId))
        {
            await ctx.Interaction.RespondAsync("Invalid listing ID.", ephemeral: true);
            return;
        }

        await HandleMarketInfo(listingId);
    }

    /// <summary>
    ///     Handles the info button for viewing Pokemon information from the market list.
    /// </summary>
    /// <param name="listingId">The market listing ID to view information for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("market_info:*")]
    public async Task HandleMarketInfo(ulong listingId)
    {
        await DeferAsync();

        var pokemon = await Service.GetPokemonAsync(listingId);
        
        if (pokemon == null)
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync("That listing does not exist or has already ended.");
            return;
        }

        // Create a basic Pokemon info embed (same logic as slash command)
        var embed = new EmbedBuilder()
            .WithTitle($"Market Listing #{listingId}")
            .WithDescription($"**{pokemon.PokemonName}** (Level {pokemon.Level})")
            .AddField("Price", $"{pokemon.Price:N0} coins", true)
            .AddField("Gender", pokemon.Gender, true)
            .AddField("Nature", pokemon.Nature, true)
            .AddField("IVs", $"HP: {pokemon.HpIv} | ATK: {pokemon.AttackIv} | DEF: {pokemon.DefenseIv}\n" +
                             $"SPATK: {pokemon.SpecialAttackIv} | SPDEF: {pokemon.SpecialDefenseIv} | SPD: {pokemon.SpeedIv}", false)
            .AddField("Shiny", pokemon.Shiny == true ? "Yes" : "No", true)
            .AddField("Radiant", pokemon.Radiant == true ? "Yes" : "No", true)
            .AddField("Moves", string.Join(", ", pokemon.Moves), false)
            .WithColor(pokemon.Shiny == true ? Color.Gold : Color.Blue)
            .WithFooter($"Use `/market buy {listingId}` to purchase this Pokemon")
            .Build();

        await ctx.Interaction.FollowupAsync(embed: embed, ephemeral: true);
    }
}