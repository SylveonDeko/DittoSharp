using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Items.Services;

namespace EeveeCore.Modules.Items;

/// <summary>
///     Provides Discord slash commands for interacting with Pokémon items.
///     Handles commands for equipping, unequipping, transferring, and buying items.
/// </summary>
[Group("items", "Item related commands")]
public class ItemSlashCommands : EeveeCoreSlashModuleBase<ItemsService>
{
    /// <summary>
    ///     Unequips an item from the user's selected Pokémon and returns it to their inventory.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("unequip", "Unequip an item from your selected pokemon")]
    public async Task Unequip()
    {
        var result = await Service.Unequip(ctx.User.Id);
        await RespondAsync(result.Message);
    }

    /// <summary>
    ///     Makes the user's selected Pokémon drop their held item without returning it to inventory.
    ///     The item is effectively discarded.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("drop", "Have selected pokemon drop their held item")]
    public async Task Drop()
    {
        var result = await Service.Drop(ctx.User.Id);
        await RespondAsync(result.Message);
    }

    /// <summary>
    ///     Transfers an item from the user's selected Pokémon to another Pokémon in their party.
    /// </summary>
    /// <param name="pokemonNumber">The pokemon number to transfer to..</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("transfer", "Transfer an item from your selected pokemon to another")]
    public async Task Transfer(ulong pokemonNumber)
    {
        var result = await Service.Transfer(ctx.User.Id, pokemonNumber);
        await RespondAsync(result.Message);
    }

    /// <summary>
    ///     Equips an item from the user's bag to their selected Pokémon.
    /// </summary>
    /// <param name="itemName">The name of the item to equip.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("equip", "Equip an item from your bag to your selected pokemon")]
    public async Task Equip(string itemName)
    {
        var result = await Service.Equip(ctx.User.Id, itemName);
        await RespondAsync(result.Message);
    }

    /// <summary>
    ///     Uses an active item (like an evolution stone) on the user's selected Pokémon.
    ///     These items are typically consumed to trigger evolution or other effects.
    /// </summary>
    /// <param name="itemName">The name of the item to apply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("apply", "Use an active item to evolve a poke")]
    public async Task Apply(string itemName)
    {
        var result = await Service.Apply(ctx.User.Id, itemName, ctx.Channel);
        await RespondAsync(result.Message);
    }

    /// <summary>
    ///     Provides Discord slash commands for purchasing items and services with credits.
    /// </summary>
    [Group("buy", "Commands to buy stuff from the Shop with credits")]
    public class BuyCommands : EeveeCoreSlashModuleBase<ItemsService>
    {
        /// <summary>
        ///     Buys an item from the shop.
        ///     The item may be added to inventory or equipped to the selected Pokémon
        ///     depending on the item type.
        /// </summary>
        /// <param name="itemName">The name of the item to buy.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("item", "Buy an item from the shop")]
        public async Task Item(string itemName)
        {
            var result = await Service.BuyItem(ctx.User.Id, itemName);
            await RespondAsync(result.Message);
        }

        /// <summary>
        ///     Buys additional daycare spaces for breeding Pokémon.
        ///     Each space costs 10,000 credits.
        /// </summary>
        /// <param name="amount">The number of daycare spaces to buy. Defaults to 1.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("daycare", "Buy daycare spaces")]
        public async Task Daycare(int amount = 1)
        {
            var result = await Service.BuyDaycare(ctx.User.Id, amount);
            await RespondAsync(result.Message);
        }

        /// <summary>
        ///     Buys vitamins to increase the EVs (Effort Values) of the user's selected Pokémon.
        ///     Each vitamin costs 100 credits and adds 10 EVs to a specific stat.
        /// </summary>
        /// <param name="itemName">The name of the vitamin to buy.</param>
        /// <param name="amount">The amount of the vitamin to buy.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("vitamins", "Buy vitamins for your selected pokemon")]
        public async Task Vitamins(string itemName, int amount)
        {
            var result = await Service.BuyVitamins(ctx.User.Id, itemName, amount);
            await RespondAsync(result.Message);
        }

        /// <summary>
        ///     Buys and applies Rare Candy to level up the user's selected Pokémon.
        ///     Each candy costs 100 credits and adds one level, up to a maximum of level 100.
        /// </summary>
        /// <param name="amount">The number of Rare Candies to buy and use. Defaults to 1.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("candy", "Buy rare candy to level up your selected pokemon")]
        public async Task Candy(int amount = 1)
        {
            var result = await Service.BuyCandy(ctx.User.Id, amount);
            await RespondAsync(result.Message);
        }

        /// <summary>
        ///     Buys a radiant chest that can contain rare Pokémon or items.
        ///     Chests can be purchased with either credits or redeems and have weekly purchase limits.
        ///     Requires confirmation before completing the purchase.
        /// </summary>
        /// <param name="chestType">The type of chest to buy (rare, mythic, or legend).</param>
        /// <param name="creditsOrRedeems">The currency to use for purchase (credits or redeems).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("chest", "Buy a radiant chest")]
        public async Task Chest(string chestType, string creditsOrRedeems)
        {
            if (!await PromptUserConfirmAsync(
                    $"Are you sure you want to buy a {chestType} chest for {creditsOrRedeems}?", ctx.User.Id))
            {
                await RespondAsync("Purchase cancelled.", ephemeral: true);
                return;
            }

            var result = await Service.BuyChest(ctx.User.Id, chestType, creditsOrRedeems);
            await RespondAsync(result.Message);
        }

        /// <summary>
        ///     Buys redeems for the user, which can be used to redeem special Pokémon or items.
        ///     Redeems cost 60,000 credits each and have a weekly purchase limit of 100.
        ///     If no amount is specified, displays current purchase stats.
        /// </summary>
        /// <param name="amount">The number of redeems to buy, or null to show current purchase stats.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("redeems", "Buy redeems for credits")]
        public async Task Redeems(int? amount = null)
        {
            var result = await Service.BuyRedeems(ctx.User.Id, (ulong?)amount);
            if (result.Embed != null)
                await RespondAsync(embed: result.Embed);
            else
                await RespondAsync(result.Message);
        }
    }
}