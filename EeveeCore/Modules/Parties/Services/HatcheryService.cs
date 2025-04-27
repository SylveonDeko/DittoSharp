using EeveeCore.Database.Models.PostgreSQL.Pokemon;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Modules.Parties.Services;

/// <summary>
///     Provides functionality for managing Pokémon egg hatcheries.
///     Handles operations for adding, removing, and monitoring eggs in different hatchery groups
///     with varying incubation rates and slot limits based on user's Patreon tier.
/// </summary>
/// <param name="db">The database context for accessing egg and user data.</param>
/// <param name="client">The Discord client for user and channel interactions.</param>
public class HatcheryService(EeveeCoreContext db, DiscordShardedClient client) : INService
{
    private const string PremiumX2Icon = "<:premiumX2:1064764945578852382>";
    private const string PremiumX3Icon = "<:premiumX3:1064764942848376893>";

    /// <summary>
    ///     Dictionary storing paged results for users viewing hatchery data.
    ///     Maps user IDs to their current page state.
    /// </summary>
    private readonly ConcurrentDictionary<ulong, (List<EmbedBuilder> Pages, int CurrentPage)> _pagedResults = new();

    /// <summary>
    ///     Stores a paged result for a user.
    ///     Used for maintaining pagination state when browsing hatchery information.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="pages">The list of embeds representing each page.</param>
    /// <param name="currentPage">The current page index being viewed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StorePagedResult(ulong userId, List<EmbedBuilder> pages, int currentPage)
    {
        _pagedResults[userId] = (pages, currentPage);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Retrieves a paged result for a user.
    ///     Used when a user navigates through hatchery pages.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns the user's
    ///     pages and current page index, or null if not found.
    /// </returns>
    public Task<(List<EmbedBuilder> Pages, int CurrentPage)> GetPagedResult(ulong userId)
    {
        if (_pagedResults.TryGetValue(userId, out var result)) return Task.FromResult(result);

        return Task.FromResult<(List<EmbedBuilder> Pages, int CurrentPage)>((null, 0));
    }

    /// <summary>
    ///     Updates a user's current page index in their paged result.
    ///     Called when a user navigates to a different page.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="newPage">The new page index to set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdatePagedResult(ulong userId, int newPage)
    {
        if (_pagedResults.TryGetValue(userId, out var result)) _pagedResults[userId] = (result.Pages, newPage);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Generates an embed displaying a user's hatchery information for a specific group.
    ///     Shows eggs in each slot along with their incubation progress.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="group">The hatchery group number (1-3).</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns an EmbedBuilder
    ///     with the hatchery information, or null if the group is invalid.
    /// </returns>
    public async Task<EmbedBuilder> GetHatcheryViewEmbed(ulong userId, short group)
    {
        // Validate group number
        if (group < 1 || group > 3) return null;

        // Get user's Patreon tier to determine available slots
        var patreonTier = await GetPatreonTier(userId);
        string maxMsg;

        // Initialize embed
        var embed = new EmbedBuilder()
            .WithTitle($"{await GetUserNameAsync(userId)}'s Hatchery (Group #{group})")
            .WithColor(new Color(0xff, 0x00, 0x60));

        // Get egg data from the hatchery group
        var eggData = await GetHatcheryData(userId, group, patreonTier);
        if (eggData.Item3.Count == 0) return null;

        var (slots, maxSlots, slotData) = eggData;

        // Format the slot information into the embed description
        var description = string.Empty;
        foreach (var (slotNumber, eggName, pokemonNumber, stepCounter) in slotData)
            if (!string.IsNullOrEmpty(eggName))
                description +=
                    $"**__Slot {slotNumber}__** - **Steps:** `{stepCounter}`\n`{eggName} [{pokemonNumber}]`\n\n";

        embed.WithDescription(description);

        // Add the maximum egg slots information
        switch (patreonTier)
        {
            case PatreonTier.None:
            case PatreonTier.Silver:
                maxMsg = "6";
                break;
            case PatreonTier.Gold:
                maxMsg = $"7 {PremiumX2Icon}";
                break;
            case PatreonTier.Crystal:
                maxMsg = $"10 {PremiumX3Icon}";
                break;
            default:
                maxMsg = "6";
                break;
        }

        embed.AddField("Max Egg Slots", maxMsg, false);

        // Add footer based on group
        switch (group)
        {
            case 1:
                embed.WithFooter("Group 1 | Full Steps Counted");
                break;
            case 2:
                embed.WithFooter("Group 2 | 1/3 Steps Counted");
                break;
            case 3:
                embed.WithFooter("Group 3 | 1/6 Steps Counted");
                break;
            default:
                embed.WithFooter("Something went wrong | No Steps Counted");
                break;
        }

        return embed;
    }

    /// <summary>
    ///     Adds an egg to a specific slot in a hatchery group.
    ///     Performs validation checks for slot access based on Patreon tier.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="eggIndex">The index of the egg in the user's Pokémon collection.</param>
    /// <param name="group">The hatchery group number (1-3).</param>
    /// <param name="slot">The slot number within the group (1-10).</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns a tuple with
    ///     a success indicator and a result message.
    /// </returns>
    public async Task<(bool Success, string Message)> AddEggToHatchery(ulong userId, int eggIndex, short group,
        int slot)
    {
        try
        {
            // Validate parameters
            if (group < 1 || group > 3 || slot < 1 || slot > 10) return (false, "Invalid group or slot number.");

            // Get user's Patreon tier to check slot access
            var patreonTier = await GetPatreonTier(userId);

            switch (slot)
            {
                // Check if the user has access to the specified slot based on their Patreon tier
                case 7 when patreonTier < PatreonTier.Gold:
                    return (false, "You must be a Gold or Crystal Patreon to add eggs to slot 7.");
                case >= 8 when patreonTier < PatreonTier.Crystal:
                    return (false, "You must be a Crystal Patreon to add eggs to slots 8-10.");
            }

            // Get the user and check if they exist
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Pokemon == null || user.Pokemon.Length == 0)
                return (false, "You have not started or don't have any Pokémon.");

            // Check if the provided index is valid
            if (eggIndex <= 0 || eggIndex > user.Pokemon.Length) return (false, "Invalid egg index.");

            // Get the egg ID from the user's collection
            var eggId = user.Pokemon[eggIndex - 1];

            // Check if the Pokémon is actually an egg
            var egg = await db.UserPokemon
                .FirstOrDefaultAsync(p => p.Id == eggId && p.PokemonName == "Egg");

            if (egg == null) return (false, "The provided ID is not a valid Egg or doesn't exist.");

            // Check if this egg is already in any hatchery
            var hatcheriesWithEgg = await db.EggHatcheries
                .Where(h => h.UserId == userId &&
                            (h.Slot1 == eggId || h.Slot2 == eggId || h.Slot3 == eggId ||
                             h.Slot4 == eggId || h.Slot5 == eggId || h.Slot6 == eggId ||
                             h.Slot7 == eggId || h.Slot8 == eggId || h.Slot9 == eggId || h.Slot10 == eggId))
                .AnyAsync();

            if (hatcheriesWithEgg) return (false, "This Egg is already in a hatchery slot.");

            // Get or create the hatchery record for this user and group
            var hatchery = await db.EggHatcheries
                .FirstOrDefaultAsync(h => h.UserId == userId && h.Group == group);

            if (hatchery == null)
            {
                // Create new hatchery record
                hatchery = new EggHatchery
                {
                    UserId = userId,
                    Group = group
                };
                db.EggHatcheries.Add(hatchery);
            }

            // Check if the selected slot is already occupied
            var isSlotOccupied = false;
            switch (slot)
            {
                case 1: isSlotOccupied = hatchery.Slot1 != null; break;
                case 2: isSlotOccupied = hatchery.Slot2 != null; break;
                case 3: isSlotOccupied = hatchery.Slot3 != null; break;
                case 4: isSlotOccupied = hatchery.Slot4 != null; break;
                case 5: isSlotOccupied = hatchery.Slot5 != null; break;
                case 6: isSlotOccupied = hatchery.Slot6 != null; break;
                case 7: isSlotOccupied = hatchery.Slot7 != null; break;
                case 8: isSlotOccupied = hatchery.Slot8 != null; break;
                case 9: isSlotOccupied = hatchery.Slot9 != null; break;
                case 10: isSlotOccupied = hatchery.Slot10 != null; break;
            }

            if (isSlotOccupied) return (false, $"Slot {slot} in group {group} is already occupied.");

            // Update the appropriate slot with the egg ID
            switch (slot)
            {
                case 1: hatchery.Slot1 = eggId; break;
                case 2: hatchery.Slot2 = eggId; break;
                case 3: hatchery.Slot3 = eggId; break;
                case 4: hatchery.Slot4 = eggId; break;
                case 5: hatchery.Slot5 = eggId; break;
                case 6: hatchery.Slot6 = eggId; break;
                case 7: hatchery.Slot7 = eggId; break;
                case 8: hatchery.Slot8 = eggId; break;
                case 9: hatchery.Slot9 = eggId; break;
                case 10: hatchery.Slot10 = eggId; break;
            }

            await db.SaveChangesAsync();

            return (true, $"Egg has been added to slot {slot} in group {group}.");
        }
        catch (Exception ex)
        {
            // Log the error
            if (client.GetChannel(1004311971853779005) is SocketTextChannel errorChannel)
                await errorChannel.SendMessageAsync($"**__ERROR OCCURRED__**: ```{ex.Message}\n{ex.StackTrace}```");

            return (false, "Something went wrong\nPlease try running this command again.");
        }
    }

    /// <summary>
    ///     Removes an egg from a specific slot in a hatchery group.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="group">The hatchery group number (1-3).</param>
    /// <param name="slot">The slot number within the group (1-10).</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns a tuple with
    ///     a success indicator and a result message.
    /// </returns>
    public async Task<(bool Success, string Message)> RemoveEggFromHatchery(ulong userId, int group, int slot)
    {
        try
        {
            // Validate parameters
            if (group < 1 || group > 3 || slot < 1 || slot > 10) return (false, "Invalid group or slot number.");

            // Get the hatchery for this user and group
            var hatchery = await db.EggHatcheries
                .FirstOrDefaultAsync(h => h.UserId == userId && h.Group == group);

            if (hatchery == null) return (false, $"You don't have a hatchery in group {group}.");

            // Check if there's an egg in the selected slot
            ulong? eggId = null;
            switch (slot)
            {
                case 1: eggId = hatchery.Slot1; break;
                case 2: eggId = hatchery.Slot2; break;
                case 3: eggId = hatchery.Slot3; break;
                case 4: eggId = hatchery.Slot4; break;
                case 5: eggId = hatchery.Slot5; break;
                case 6: eggId = hatchery.Slot6; break;
                case 7: eggId = hatchery.Slot7; break;
                case 8: eggId = hatchery.Slot8; break;
                case 9: eggId = hatchery.Slot9; break;
                case 10: eggId = hatchery.Slot10; break;
            }

            if (eggId == null) return (false, $"Slot {slot} in group {group} is empty.");

            // Remove the egg from the slot
            switch (slot)
            {
                case 1: hatchery.Slot1 = null; break;
                case 2: hatchery.Slot2 = null; break;
                case 3: hatchery.Slot3 = null; break;
                case 4: hatchery.Slot4 = null; break;
                case 5: hatchery.Slot5 = null; break;
                case 6: hatchery.Slot6 = null; break;
                case 7: hatchery.Slot7 = null; break;
                case 8: hatchery.Slot8 = null; break;
                case 9: hatchery.Slot9 = null; break;
                case 10: hatchery.Slot10 = null; break;
            }

            await db.SaveChangesAsync();

            return (true, $"Removed egg from slot {slot} in group {group}.");
        }
        catch (Exception ex)
        {
            // Log the error
            if (client.GetChannel(1004311971853779005) is SocketTextChannel errorChannel)
                await errorChannel.SendMessageAsync($"**__ERROR OCCURRED__**: ```{ex.Message}\n{ex.StackTrace}```");

            return (false, "Something went wrong\nPlease try running this command again.");
        }
    }

    /// <summary>
    ///     Sets multiple eggs in a hatchery group simultaneously.
    ///     Used for batch processing of egg placement.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
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
    /// <returns>
    ///     A task representing the asynchronous operation that returns a dictionary
    ///     mapping group numbers to lists of successfully added slot numbers.
    /// </returns>
    public async Task<Dictionary<int, List<int>>> SetMultipleEggs(ulong userId, short group, int? slot1, int? slot2,
        int? slot3,
        int? slot4, int? slot5, int? slot6, int? slot7, int? slot8, int? slot9, int? slot10)
    {
        var slotArray = new[] { slot1, slot2, slot3, slot4, slot5, slot6, slot7, slot8, slot9, slot10 };
        var addedEggs = new Dictionary<int, List<int>>();

        // Process each provided egg index
        for (var slotIndex = 0; slotIndex < slotArray.Length; slotIndex++)
        {
            var eggIndex = slotArray[slotIndex];
            if (eggIndex.HasValue)
            {
                var slot = slotIndex + 1;
                var result = await AddEggToHatchery(userId, eggIndex.Value, group, slot);

                if (result.Success)
                {
                    if (!addedEggs.ContainsKey(group)) addedEggs[group] = new List<int>();
                    addedEggs[group].Add(slot);
                }
            }
        }

        return addedEggs;
    }

    /// <summary>
    ///     Swaps two eggs between slots in the hatchery system.
    ///     Can swap eggs between different groups.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="egg1Id">The ID of the first egg to swap.</param>
    /// <param name="egg2Id">The ID of the second egg to swap.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns a tuple with
    ///     a success indicator and a result message.
    /// </returns>
    public async Task<(bool Success, string Message)> SwapEggs(ulong userId, ulong egg1Id, ulong egg2Id)
    {
        try
        {
            // Find which slots these eggs are in
            var hatcheries = await db.EggHatcheries
                .Where(h => h.UserId == userId)
                .ToListAsync();

            EggHatchery hatchery1 = null;
            EggHatchery hatchery2 = null;
            var egg1Slot = 0;
            var egg2Slot = 0;
            short? egg1Group = 0;
            short? egg2Group = 0;

            // Find the first egg
            foreach (var hatchery in hatcheries)
            {
                if (hatchery.Slot1 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 1;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot2 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 2;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot3 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 3;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot4 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 4;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot5 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 5;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot6 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 6;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot7 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 7;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot8 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 8;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot9 == egg1Id)
                {
                    hatchery1 = hatchery;
                    egg1Slot = 9;
                    egg1Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot10 != egg1Id) continue;
                hatchery1 = hatchery;
                egg1Slot = 10;
                egg1Group = hatchery.Group;
                break;
            }

            // Find the second egg
            foreach (var hatchery in hatcheries)
            {
                if (hatchery.Slot1 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 1;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot2 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 2;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot3 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 3;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot4 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 4;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot5 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 5;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot6 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 6;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot7 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 7;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot8 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 8;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot9 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 9;
                    egg2Group = hatchery.Group;
                    break;
                }

                if (hatchery.Slot10 == egg2Id)
                {
                    hatchery2 = hatchery;
                    egg2Slot = 10;
                    egg2Group = hatchery.Group;
                    break;
                }
            }

            if (hatchery1 == null || hatchery2 == null)
                return (false, "One or both of the provided egg IDs are not in the hatchery.");

            // Swap the eggs
            ulong? temp = null;
            switch (egg1Slot)
            {
                case 1:
                    temp = hatchery1.Slot1;
                    hatchery1.Slot1 = null;
                    break;
                case 2:
                    temp = hatchery1.Slot2;
                    hatchery1.Slot2 = null;
                    break;
                case 3:
                    temp = hatchery1.Slot3;
                    hatchery1.Slot3 = null;
                    break;
                case 4:
                    temp = hatchery1.Slot4;
                    hatchery1.Slot4 = null;
                    break;
                case 5:
                    temp = hatchery1.Slot5;
                    hatchery1.Slot5 = null;
                    break;
                case 6:
                    temp = hatchery1.Slot6;
                    hatchery1.Slot6 = null;
                    break;
                case 7:
                    temp = hatchery1.Slot7;
                    hatchery1.Slot7 = null;
                    break;
                case 8:
                    temp = hatchery1.Slot8;
                    hatchery1.Slot8 = null;
                    break;
                case 9:
                    temp = hatchery1.Slot9;
                    hatchery1.Slot9 = null;
                    break;
                case 10:
                    temp = hatchery1.Slot10;
                    hatchery1.Slot10 = null;
                    break;
            }

            ulong? temp2 = null;
            switch (egg2Slot)
            {
                case 1:
                    temp2 = hatchery2.Slot1;
                    hatchery2.Slot1 = temp;
                    break;
                case 2:
                    temp2 = hatchery2.Slot2;
                    hatchery2.Slot2 = temp;
                    break;
                case 3:
                    temp2 = hatchery2.Slot3;
                    hatchery2.Slot3 = temp;
                    break;
                case 4:
                    temp2 = hatchery2.Slot4;
                    hatchery2.Slot4 = temp;
                    break;
                case 5:
                    temp2 = hatchery2.Slot5;
                    hatchery2.Slot5 = temp;
                    break;
                case 6:
                    temp2 = hatchery2.Slot6;
                    hatchery2.Slot6 = temp;
                    break;
                case 7:
                    temp2 = hatchery2.Slot7;
                    hatchery2.Slot7 = temp;
                    break;
                case 8:
                    temp2 = hatchery2.Slot8;
                    hatchery2.Slot8 = temp;
                    break;
                case 9:
                    temp2 = hatchery2.Slot9;
                    hatchery2.Slot9 = temp;
                    break;
                case 10:
                    temp2 = hatchery2.Slot10;
                    hatchery2.Slot10 = temp;
                    break;
            }

            switch (egg1Slot)
            {
                case 1: hatchery1.Slot1 = temp2; break;
                case 2: hatchery1.Slot2 = temp2; break;
                case 3: hatchery1.Slot3 = temp2; break;
                case 4: hatchery1.Slot4 = temp2; break;
                case 5: hatchery1.Slot5 = temp2; break;
                case 6: hatchery1.Slot6 = temp2; break;
                case 7: hatchery1.Slot7 = temp2; break;
                case 8: hatchery1.Slot8 = temp2; break;
                case 9: hatchery1.Slot9 = temp2; break;
                case 10: hatchery1.Slot10 = temp2; break;
            }

            await db.SaveChangesAsync();

            return (true, $"Eggs {egg1Id} and {egg2Id} have been swapped.");
        }
        catch (Exception ex)
        {
            // Log the error
            if (client.GetChannel(1004311971853779005) is SocketTextChannel errorChannel)
                await errorChannel.SendMessageAsync($"**__ERROR OCCURRED__**: ```{ex.Message}\n{ex.StackTrace}```");

            return (false, "Something went wrong\nPlease try running this command again.");
        }
    }

    /// <summary>
    ///     Retrieves hatchery data for a specific user and group.
    ///     Collects information about available slots, max slots, and egg data for display.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <param name="group">The hatchery group number (1-3).</param>
    /// <param name="patreonTier">The user's Patreon tier for slot access validation.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns a tuple with:
    ///     - Number of available slots
    ///     - Maximum number of slots based on Patreon tier
    ///     - List of slot data (slot number, egg name, index in party, step counter)
    /// </returns>
    private async
        Task<(int AvailableSlots, int MaxSlots,
            List<(int SlotNumber, string EggName, int? PokemonNumber, int StepCounter)>
            )> GetHatcheryData(ulong userId, short group, PatreonTier patreonTier)
    {
        // Get max slots based on Patreon tier
        var maxSlots = patreonTier switch
        {
            PatreonTier.Crystal => 10,
            PatreonTier.Gold => 7,
            _ => 6
        };

        // Get the hatchery data for this user and group
        var hatchery = await db.EggHatcheries
            .FirstOrDefaultAsync(h => h.UserId == userId && h.Group == group);

        if (hatchery == null)
        {
            // Create a new hatchery for this user and group
            hatchery = new EggHatchery
            {
                UserId = userId,
                Group = group
            };
            db.EggHatcheries.Add(hatchery);
            await db.SaveChangesAsync();
        }

        // Get user's Pokemon collection for finding indices
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user?.Pokemon == null) return (0, maxSlots, new List<(int, string, int?, int)>());

        // Get all slots data
        var slotData = new List<(int SlotNumber, string EggName, int? PokemonNumber, int StepCounter)>();
        var availableSlots = 0;

        // Helper function to process each slot
        async Task ProcessSlot(int slotNumber, ulong? eggId)
        {
            if (eggId.HasValue && eggId.Value > 0)
            {
                var pokemon = await db.UserPokemon
                    .FirstOrDefaultAsync(p => p.Id == eggId.Value);

                if (pokemon != null)
                {
                    var pokemonIndex = Array.IndexOf(user.Pokemon, eggId.Value) + 1;
                    slotData.Add((slotNumber, pokemon.Name, pokemonIndex, pokemon.Counter ?? 0));
                }
                else
                {
                    slotData.Add((slotNumber, "Empty", null, 0));
                    availableSlots++;
                }
            }
            else
            {
                slotData.Add((slotNumber, "Empty", null, 0));
                availableSlots++;
            }
        }

        // Process all slots
        await ProcessSlot(1, hatchery.Slot1);
        await ProcessSlot(2, hatchery.Slot2);
        await ProcessSlot(3, hatchery.Slot3);
        await ProcessSlot(4, hatchery.Slot4);
        await ProcessSlot(5, hatchery.Slot5);
        await ProcessSlot(6, hatchery.Slot6);

        // Process premium slots based on Patreon tier
        if (patreonTier >= PatreonTier.Gold) await ProcessSlot(7, hatchery.Slot7);

        if (patreonTier >= PatreonTier.Crystal)
        {
            await ProcessSlot(8, hatchery.Slot8);
            await ProcessSlot(9, hatchery.Slot9);
            await ProcessSlot(10, hatchery.Slot10);
        }

        return (availableSlots, maxSlots, slotData);
    }

    /// <summary>
    ///     Determines a user's Patreon tier from their database record.
    ///     Used for feature access validation like premium hatchery slots.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns the user's PatreonTier.
    ///     Returns PatreonTier.None if the user is not found or has no Patreon status.
    /// </returns>
    private async Task<PatreonTier> GetPatreonTier(ulong userId)
    {
        var user = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Patreon, u.PatreonOverride })
            .FirstOrDefaultAsync();

        if (user == null)
            return PatreonTier.None;

        var patreonStatus = !string.IsNullOrEmpty(user.PatreonOverride) ? user.PatreonOverride : user.Patreon;

        return patreonStatus switch
        {
            "Crystal Patreon" => PatreonTier.Crystal,
            "Gold Patreon" => PatreonTier.Gold,
            "Silver Patreon" => PatreonTier.Silver,
            _ => PatreonTier.None
        };
    }

    /// <summary>
    ///     Retrieves a user's Discord username from their ID.
    /// </summary>
    /// <param name="userId">The Discord ID of the user.</param>
    /// <returns>
    ///     A task representing the asynchronous operation that returns the user's username,
    ///     or "Unknown User" if the user cannot be found.
    /// </returns>
    private async Task<string> GetUserNameAsync(ulong userId)
    {
        try
        {
            var user = client.GetUser(userId);
            return user?.Username ?? "Unknown User";
        }
        catch
        {
            return "Unknown User";
        }
    }
}

/// <summary>
///     Defines Patreon subscription tiers that determine feature access levels.
///     Higher tiers provide access to more slots in the egg hatchery.
/// </summary>
public enum PatreonTier
{
    /// <summary>
    ///     No Patreon subscription, access to standard features only.
    ///     Allows access to 6 hatchery slots per group.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Silver tier Patreon subscription.
    ///     Allows access to 6 hatchery slots per group (same as None).
    /// </summary>
    Silver = 1,

    /// <summary>
    ///     Gold tier Patreon subscription.
    ///     Allows access to 7 hatchery slots per group.
    /// </summary>
    Gold = 2,

    /// <summary>
    ///     Crystal tier Patreon subscription.
    ///     Allows access to 10 hatchery slots per group.
    /// </summary>
    Crystal = 3
}