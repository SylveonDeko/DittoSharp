using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Parties.Services;

namespace EeveeCore.Modules.Parties;

/// <summary>
///     Provides Discord slash commands for managing Pokémon egg hatcheries.
///     Allows users to view, add, remove, and swap eggs across different hatchery groups.
/// </summary>
[Group("hatchery", "Commands for managing your eggs")]
public class HatcheryModule : EeveeCoreSlashModuleBase<HatcheryService>
{
    /// <summary>
    ///     Displays the user's egg hatchery groups as a paginated embed.
    ///     Shows information about eggs in each hatchery group and their incubation progress.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("view", "View your egg hatchery groups")]
    public async Task ViewHatchery()
    {
        // Create embeds for all three groups
        var embedPages = new List<EmbedBuilder>();

        for (short group = 1; group <= 3; group++)
        {
            var embed = await Service.GetHatcheryViewEmbed(ctx.User.Id, group);

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
        await Service.StorePagedResult(ctx.User.Id, embedPages, 0);
    }

    /// <summary>
    ///     Adds multiple eggs to a specific hatchery group simultaneously.
    ///     Allows batch processing of egg placements across different slots.
    /// </summary>
    /// <param name="group">The hatchery group number (1-3).</param>
    /// <param name="slot1">The egg index for slot 1, or null if not changed.</param>
    /// <param name="slot2">The egg index for slot 2, or null if not changed.</param>
    /// <param name="slot3">The egg index for slot 3, or null if not changed.</param>
    /// <param name="slot4">The egg index for slot 4, or null if not changed.</param>
    /// <param name="slot5">The egg index for slot 5, or null if not changed.</param>
    /// <param name="slot6">The egg index for slot 6, or null if not changed.</param>
    /// <param name="slot7">The egg index for slot 7, or null if not changed.</param>
    /// <param name="slot8">The egg index for slot 8, or null if not changed.</param>
    /// <param name="slot9">The egg index for slot 9, or null if not changed.</param>
    /// <param name="slot10">The egg index for slot 10, or null if not changed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
        var addedEggs = await Service.SetMultipleEggs(
            ctx.User.Id, group, slot1, slot2, slot3, slot4, slot5, slot6, slot7, slot8, slot9, slot10);

        if (addedEggs.Count == 0)
        {
            await ConfirmAsync("No eggs were added to the hatchery.");
            return;
        }

        // Create a confirmation embed
        var embed = new EmbedBuilder()
            .WithTitle("Eggs Added to Hatchery")
            .WithColor(EeveeCore.OkColor);

        foreach (var groupEntry in addedEggs)
            embed.AddField(
                $"Group {groupEntry.Key}",
                $"Eggs added to slots: {string.Join(", ", groupEntry.Value)}",
                false);

        await ctx.Interaction.RespondAsync(embed: embed.Build());
    }

    /// <summary>
    ///     Removes an egg from a specific slot in a hatchery group.
    ///     Clears the slot for future use.
    /// </summary>
    /// <param name="group">The hatchery group number (1-3).</param>
    /// <param name="slot">The slot number within the group (1-10).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
        var result = await Service.RemoveEggFromHatchery(ctx.User.Id, group, slot);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    /// <summary>
    ///     Swaps two eggs between slots in the hatchery system.
    ///     Can swap eggs between different groups.
    /// </summary>
    /// <param name="egg1">The ID of the first egg to swap.</param>
    /// <param name="egg2">The ID of the second egg to swap.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("swap", "Swap two eggs in your hatchery")]
    public async Task SwapEggs(ulong egg1, ulong egg2)
    {
        var result = await Service.SwapEggs(ctx.User.Id, egg1, egg2);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }
}

/// <summary>
///     Handles component interactions for the hatchery system.
///     Processes pagination buttons for browsing hatchery groups.
/// </summary>
/// <param name="Service">The service that handles hatchery data operations.</param>
public class HatcheryInteractionModule(HatcheryService Service) : EeveeCoreSlashModuleBase<HatcheryService>
{
    /// <summary>
    ///     Handles the previous page button interaction.
    ///     Navigates to the previous hatchery group in the pagination.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("hatchery:prev")]
    public async Task HandlePrevPage()
    {
        var (pages, currentPage) = await Service.GetPagedResult(ctx.User.Id);

        if (pages == null || pages.Count == 0)
        {
            await ErrorAsync("Pagination session expired. Please run the command again.");
            return;
        }

        var newPage = (currentPage - 1 + pages.Count) % pages.Count;
        await Service.UpdatePagedResult(ctx.User.Id, newPage);

        await ctx.Interaction.ModifyOriginalResponseAsync(props => { props.Embed = pages[newPage].Build(); });
    }

    /// <summary>
    ///     Handles the next page button interaction.
    ///     Navigates to the next hatchery group in the pagination.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("hatchery:next")]
    public async Task HandleNextPage()
    {
        var (pages, currentPage) = await Service.GetPagedResult(ctx.User.Id);

        if (pages == null || pages.Count == 0)
        {
            await ErrorAsync("Pagination session expired. Please run the command again.");
            return;
        }

        var newPage = (currentPage + 1) % pages.Count;
        await Service.UpdatePagedResult(ctx.User.Id, newPage);

        await ctx.Interaction.ModifyOriginalResponseAsync(props => { props.Embed = pages[newPage].Build(); });
    }
}