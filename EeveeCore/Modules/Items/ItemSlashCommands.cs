using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Items.Services;

namespace EeveeCore.Modules.Items;

[Group("items", "Item related commands")]
public class ItemSlashCommands : EeveeCoreSlashModuleBase<ItemsService>
{
    [SlashCommand("unequip", "Unequip an item from your selected pokemon")]
    public async Task Unequip()
    {
        var result = await Service.Unequip(ctx.User.Id);
        await RespondAsync(result.Message);
    }

    [SlashCommand("drop", "Have selected pokemon drop their held item")]
    public async Task Drop()
    {
        var result = await Service.Drop(ctx.User.Id);
        await RespondAsync(result.Message);
    }

    [SlashCommand("transfer", "Transfer an item from your selected pokemon to another")]
    public async Task Transfer(int pokemonNumber)
    {
        var result = await Service.Transfer(ctx.User.Id, pokemonNumber);
        await RespondAsync(result.Message);
    }

    [SlashCommand("equip", "Equip an item from your bag to your selected pokemon")]
    public async Task Equip(string itemName)
    {
        var result = await Service.Equip(ctx.User.Id, itemName);
        await RespondAsync(result.Message);
    }

    [SlashCommand("apply", "Use an active item to evolve a poke")]
    public async Task Apply(string itemName)
    {
        var result = await Service.Apply(ctx.User.Id, itemName, ctx.Channel);
        await RespondAsync(result.Message);
    }

    [Group("buy", "Commands to buy stuff from the Shop with credits")]
    public class BuyCommands : EeveeCoreSlashModuleBase<ItemsService>
    {
        [SlashCommand("item", "Buy an item from the shop")]
        public async Task Item(string itemName)
        {
            var result = await Service.BuyItem(ctx.User.Id, itemName);
            await RespondAsync(result.Message);
        }

        [SlashCommand("daycare", "Buy daycare spaces")]
        public async Task Daycare(int amount = 1)
        {
            var result = await Service.BuyDaycare(ctx.User.Id, amount);
            await RespondAsync(result.Message);
        }

        [SlashCommand("vitamins", "Buy vitamins for your selected pokemon")]
        public async Task Vitamins(string itemName, int amount)
        {
            var result = await Service.BuyVitamins(ctx.User.Id, itemName, amount);
            await RespondAsync(result.Message);
        }

        [SlashCommand("candy", "Buy rare candy to level up your selected pokemon")]
        public async Task Candy(int amount = 1)
        {
            var result = await Service.BuyCandy(ctx.User.Id, amount);
            await RespondAsync(result.Message);
        }

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

        [SlashCommand("redeems", "Buy redeems for credits")]
        public async Task Redeems(int? amount = null)
        {
            var result = await Service.BuyRedeems(ctx.User.Id, amount);
            if (result.Embed != null)
                await RespondAsync(embed: result.Embed);
            else
                await RespondAsync(result.Message);
        }
    }
}