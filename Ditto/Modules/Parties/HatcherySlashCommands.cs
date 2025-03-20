using Discord.Interactions;
using Ditto.Common.ModuleBases;
using Ditto.Modules.Parties.Services;

namespace Ditto.Modules.Parties;

[Group("hatchery", "Commands for managing your eggs")]
public class HatcheryModule(HatcheryService hatcheryService) : DittoSlashModuleBase<HatcheryService>
{
    [SlashCommand("view", "View your egg hatchery groups")]
    public async Task ViewHatchery()
    {
        // Create embeds for all three groups
        var embedPages = new List<EmbedBuilder>();

        for (short group = 1; group <= 3; group++)
        {
            var embed = await hatcheryService.GetHatcheryViewEmbed(ctx.User.Id, group);

            if (embed == null)
            {
                await ErrorAsync($"You have no eggs in your hatchery group {group}.");
                return;
            }

            embedPages.Add(embed);
        }

        switch (embedPages.Count)
        {
            case 0:
                await ErrorAsync("Could not retrieve hatchery information.");
                return;
            // If there's only one page, just send it directly
            case 1:
                await ctx.Interaction.RespondAsync(embed: embedPages[0].Build());
                return;
        }

        // Create pagination components
        var components = new ComponentBuilder()
            .WithButton(customId: "hatchery:prev", emote: new Emoji("<a:leftarrow:1061558390809174066>"),
                style: ButtonStyle.Primary)
            .WithButton(customId: "hatchery:next", emote: new Emoji("<a:rightarrow:1061557943398580275>"),
                style: ButtonStyle.Primary);

        // Send the first page
        await ctx.Interaction.RespondAsync(embed: embedPages[0].Build(), components: components.Build());

        // Store the pagination state
        await hatcheryService.StorePagedResult(ctx.User.Id, embedPages, 0);
    }

    [SlashCommand("set", "Add multiple eggs to your hatchery group")]
    public async Task SetEggs(
        [Choice("1", 1)] [Choice("2", 2)] [Choice("3", 3)]
        short group,
        int? slot1 = null,
        int? slot2 = null,
        int? slot3 = null,
        int? slot4 = null,
        int? slot5 = null,
        int? slot6 = null,
        int? slot7 = null,
        int? slot8 = null,
        int? slot9 = null,
        int? slot10 = null)
    {
        if (group < 1 || group > 3)
        {
            await ErrorAsync("Invalid group number.");
            return;
        }

        // Add the eggs to the hatchery
        var addedEggs = await hatcheryService.SetMultipleEggs(
            ctx.User.Id, group, slot1, slot2, slot3, slot4, slot5, slot6, slot7, slot8, slot9, slot10);

        if (addedEggs.Count == 0)
        {
            await ConfirmAsync("No eggs were added to the hatchery.");
            return;
        }

        // Create a confirmation embed
        var embed = new EmbedBuilder()
            .WithTitle("Eggs Added to Hatchery")
            .WithColor(Ditto.OkColor);

        foreach (var groupEntry in addedEggs)
            embed.AddField(
                $"Group {groupEntry.Key}",
                $"Eggs added to slots: {string.Join(", ", groupEntry.Value)}",
                false);

        await ctx.Interaction.RespondAsync(embed: embed.Build());
    }

    [SlashCommand("remove", "Remove an egg from your hatchery")]
    public async Task RemoveEgg(
        [Choice("1", 1)] [Choice("2", 2)] [Choice("3", 3)]
        int group,
        [Choice("1", 1)]
        [Choice("2", 2)]
        [Choice("3", 3)]
        [Choice("4", 4)]
        [Choice("5", 5)]
        [Choice("6", 6)]
        [Choice("7", 7)]
        [Choice("8", 8)]
        [Choice("9", 9)]
        [Choice("10", 10)]
        int slot)
    {
        var result = await hatcheryService.RemoveEggFromHatchery(ctx.User.Id, group, slot);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    [SlashCommand("swap", "Swap two eggs in your hatchery")]
    public async Task SwapEggs(ulong egg1, ulong egg2)
    {
        var result = await hatcheryService.SwapEggs(ctx.User.Id, egg1, egg2);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }
}

public class HatcheryInteractionModule(HatcheryService hatcheryService) : DittoSlashModuleBase<HatcheryService>
{
    [ComponentInteraction("hatchery:prev")]
    public async Task HandlePrevPage()
    {
        var (pages, currentPage) = await hatcheryService.GetPagedResult(ctx.User.Id);

        if (pages == null || pages.Count == 0)
        {
            await ErrorAsync("Pagination session expired. Please run the command again.");
            return;
        }

        var newPage = (currentPage - 1 + pages.Count) % pages.Count;
        await Service.UpdatePagedResult(ctx.User.Id, newPage);

        await ctx.Interaction.ModifyOriginalResponseAsync(props => { props.Embed = pages[newPage].Build(); });
    }

    [ComponentInteraction("hatchery:next")]
    public async Task HandleNextPage()
    {
        var (pages, currentPage) = await hatcheryService.GetPagedResult(ctx.User.Id);

        if (pages == null || pages.Count == 0)
        {
            await ErrorAsync("Pagination session expired. Please run the command again.");
            return;
        }

        var newPage = (currentPage + 1) % pages.Count;
        await hatcheryService.UpdatePagedResult(ctx.User.Id, newPage);

        await ctx.Interaction.ModifyOriginalResponseAsync(props => { props.Embed = pages[newPage].Build(); });
    }
}