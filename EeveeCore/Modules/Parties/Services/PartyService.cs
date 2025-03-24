using System.Text;
using EeveeCore.Database.Models.PostgreSQL.Pokemon;
using Microsoft.EntityFrameworkCore;

namespace EeveeCore.Modules.Parties.Services;

public class PartyService(EeveeCoreContext db, DiscordShardedClient client) : INService
{
    private readonly ConcurrentDictionary<ulong, (List<EmbedBuilder> Pages, int CurrentPage)> _pagedResults = new();

    /// <summary>
    ///     Creates a formatted table string from rows and headers
    /// </summary>
    public string FormatTable(List<List<string?>> rows, List<string> headers)
    {
        // Calculate column widths
        var columnWidths = new int[headers.Count];
        for (var i = 0; i < headers.Count; i++)
        {
            columnWidths[i] = headers[i].Length;
            foreach (var row in rows)
                if (i < row.Count && row[i] != null)
                    columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
        }

        // Format headers
        var sb = new StringBuilder();
        sb.Append('|');
        for (var i = 0; i < headers.Count; i++)
            sb.Append(' ').Append(headers[i].PadRight(columnWidths[i])).Append(" |");
        sb.AppendLine();
        sb.Append('|');

        // Add header separator
        for (var i = 0; i < headers.Count; i++) sb.Append(new string('-', columnWidths[i] + 2)).Append('|');
        sb.AppendLine();

        // Format rows
        foreach (var row in rows)
        {
            sb.Append('|');
            for (var i = 0; i < headers.Count; i++)
            {
                var value = i < row.Count && row[i] != null ? row[i] : "";
                sb.Append(' ').Append(value.PadRight(columnWidths[i])).Append(" |");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Get party details for setup menu
    /// </summary>
    public async
        Task<(string Description, ulong[] Party, ulong[] PartyPokeIds, int[] PokemonIndices, string[] PokemonNames)>
        GetPartySetupData(ulong userId, string partyName)
    {
        // Initialize arrays for pokemon data
        var pokemonNames = new string?[6] { "None", "None", "None", "None", "None", "None" };
        var pokemonIndices = new int[6] { 0, 0, 0, 0, 0, 0 };
        var partyPokeIds = new ulong[6] { 0, 0, 0, 0, 0, 0 };

        // Get party data from database
        var party = await db.Parties
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == partyName);

        // If party doesn't exist, try to register it from user's current party
        if (party == null)
        {
            var firstUser = await db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (firstUser?.Party == null) return (null, null, null, null, null);

            // Create a new party save
            party = new Party
            {
                UserId = userId,
                Name = partyName,
                Slot1 = firstUser.Party[0],
                Slot2 = firstUser.Party[1],
                Slot3 = firstUser.Party[2],
                Slot4 = firstUser.Party[3],
                Slot5 = firstUser.Party[4],
                Slot6 = firstUser.Party[5]
            };

            db.Parties.Add(party);
            await db.SaveChangesAsync();
        }

        // Extract party Pokemon IDs into array
        var partyPokemonIds = new ulong[6]
        {
            party.Slot1 ?? 0,
            party.Slot2 ?? 0,
            party.Slot3 ?? 0,
            party.Slot4 ?? 0,
            party.Slot5 ?? 0,
            party.Slot6 ?? 0
        };

        // Get the user's Pokemon collection to find indices
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user?.Pokemon != null)
            for (var i = 0; i < 6; i++)
            {
                if (partyPokemonIds[i] <= 0)
                    continue;

                // Get the Pokemon's name and position in the user's collection
                var pokemon = await db.UserPokemon
                    .FirstOrDefaultAsync(p => p.Id == partyPokemonIds[i]);

                if (pokemon != null)
                {
                    pokemonNames[i] = pokemon.PokemonName;
                    partyPokeIds[i] = pokemon.Id;

                    // Find the Pokemon's index in the user's collection
                    var index = Array.IndexOf(user.Pokemon, pokemon.Id);
                    if (index != -1) pokemonIndices[i] = index + 1; // Add 1 for user-friendly indexing
                }
            }

        // Build the table for display
        var table = new List<List<string?>>
        {
            new() { "1", pokemonIndices[0] > 0 ? pokemonIndices[0].ToString() : "None", pokemonNames[0] },
            new() { "2", pokemonIndices[1] > 0 ? pokemonIndices[1].ToString() : "None", pokemonNames[1] },
            new() { "3", pokemonIndices[2] > 0 ? pokemonIndices[2].ToString() : "None", pokemonNames[2] },
            new() { "4", pokemonIndices[3] > 0 ? pokemonIndices[3].ToString() : "None", pokemonNames[3] },
            new() { "5", pokemonIndices[4] > 0 ? pokemonIndices[4].ToString() : "None", pokemonNames[4] },
            new() { "6", pokemonIndices[5] > 0 ? pokemonIndices[5].ToString() : "None", pokemonNames[5] }
        };

        var description = "Use the buttons below to set pokemon to a corresponding slot number in your party\n";
        description += $"**```\n{FormatTable(table, new List<string> { "Slot #", "PokeID", "Name" })}\n```**";
        description += $"\nMenu Timeout: ~<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 122}:R>";

        return (description, partyPokemonIds, partyPokeIds, pokemonIndices, pokemonNames);
    }

    /// <summary>
    ///     Get an embed displaying party information
    /// </summary>
    public async Task<EmbedBuilder> GetPartyViewEmbed(ulong userId, string partyName = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"Party Info: {partyName}")
            .WithColor(new Color(0xEE, 0xE6, 0x47));

        // If no party name is provided, display the current active party
        if (string.IsNullOrEmpty(partyName))
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Party == null) return null;

            for (var i = 0; i < user.Party.Length; i++)
            {
                var pokemonId = user.Party[i];
                var pokemonName = "None";
                int? pokemonIndex = null;

                if (pokemonId > 0)
                {
                    var pokemon = await db.UserPokemon
                        .FirstOrDefaultAsync(p => p.Id == pokemonId);

                    if (pokemon != null)
                    {
                        pokemonName = pokemon.PokemonName;
                        pokemonIndex = Array.IndexOf(user.Pokemon, pokemonId) + 1;
                    }
                }

                embed.AddField(
                    $"Slot {i + 1} Pokemon",
                    pokemonIndex.HasValue ? $"{pokemonName} [{pokemonIndex}]" : pokemonName
                );
            }
        }
        else
        {
            // Display the named saved party
            var party = await db.Parties
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == partyName);

            if (party == null) return null;

            var partySlots = new[]
            {
                party.Slot1,
                party.Slot2,
                party.Slot3,
                party.Slot4,
                party.Slot5,
                party.Slot6
            };

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            for (var i = 0; i < partySlots.Length; i++)
            {
                var pokemonId = partySlots[i];
                var pokemonName = "None";
                int? pokemonIndex = null;

                if (pokemonId.HasValue && pokemonId.Value > 0)
                {
                    var pokemon = await db.UserPokemon
                        .FirstOrDefaultAsync(p => p.Id == pokemonId.Value);

                    if (pokemon != null && user?.Pokemon != null)
                    {
                        pokemonName = pokemon.PokemonName;
                        pokemonIndex = Array.IndexOf(user.Pokemon, pokemonId.Value) + 1;
                    }
                }

                embed.AddField(
                    $"Slot {i + 1} Pokemon",
                    pokemonIndex.HasValue ? $"{pokemonName} [{pokemonIndex}]" : pokemonName
                );
            }
        }

        embed.WithFooter($"Use '/party load {partyName}' and then '/party add <slot_number>' to add a Pokemon");
        return embed;
    }

    /// <summary>
    ///     Add a Pokemon to the party
    /// </summary>
    public async Task<(bool Success, string Message)> AddPokemonToParty(ulong userId, int slot, int pokeIndex = 0)
    {
        // Adjust slot for zero-based indexing
        var slotIndex = slot - 1;

        // Check if user exists
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.Party == null) return (false, "You have not started!\nStart with `/start` first!");

        // Determine which Pokemon to add
        ulong pokemonId;
        if (pokeIndex > 0)
        {
            // Use the specified Pokemon index
            if (pokeIndex > user.Pokemon.Length)
                return (false, "Invalid Pokemon ID. You don't have that many Pokemon.");
            pokemonId = user.Pokemon[pokeIndex - 1];
        }
        else
        {
            // Use the currently selected Pokemon
            if (!user.Selected.HasValue || user.Selected.Value <= 0)
                return (false, "You don't have a Pokemon selected. Select one first or specify an ID.");
            pokemonId = user.Selected.Value;
        }

        // Check if the Pokemon exists and isn't an egg
        var pokemon = await db.UserPokemon
            .FirstOrDefaultAsync(p => p.Id == pokemonId);

        if (pokemon == null) return (false, "You do not have that Pokemon!");

        if (pokemon.PokemonName.Equals("Egg", StringComparison.OrdinalIgnoreCase))
            return (false, "You cannot add an Egg to your party! Use `/hatchery` commands instead!");

        // Check if this Pokemon is already in the party
        if (user.Party.Contains(pokemonId)) return (false, "That Pokemon already occupies a Team Slot!");

        // Update party array
        var updatedParty = user.Party.ToArray();
        updatedParty[slotIndex] = pokemonId;
        user.Party = updatedParty;

        await db.SaveChangesAsync();

        return (true, $"Your {pokemon.PokemonName} is now on your party, Slot number {slot}");
    }

    /// <summary>
    ///     Remove a Pokemon from the party
    /// </summary>
    public async Task<(bool Success, string Message)> RemovePokemonFromParty(ulong userId, int slot)
    {
        // Adjust slot for zero-based indexing
        var slotIndex = slot - 1;

        // Check if user exists and has started
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.Party == null) return (false, "You have not started!\nStart with `/start` first!");

        // Get the Pokemon to remove
        var pokemonId = user.Party[slotIndex];
        var pokemon = await db.UserPokemon
            .FirstOrDefaultAsync(p => p.Id == pokemonId);

        // Update party array
        var updatedParty = user.Party.ToArray();
        updatedParty[slotIndex] = 0;
        user.Party = updatedParty;

        await db.SaveChangesAsync();

        if (pokemon == null) return (false, "No Pokemon in that slot.");

        return (true, $"You have successfully removed {pokemon.PokemonName} from Pokemon Number {slot} In your Party!");
    }

    /// <summary>
    ///     Register a party with a name
    /// </summary>
    public async Task<(bool Success, string Message)> RegisterParty(ulong userId, string partyName)
    {
        // Check if user exists
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.Party == null) return (false, "You have not started!\nStart with `/start` first.");

        // Check if a party with this name already exists
        var existingParty = await db.Parties
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == partyName);

        if (existingParty != null)
        {
            // Update existing party
            existingParty.Slot1 = user.Party[0];
            existingParty.Slot2 = user.Party[1];
            existingParty.Slot3 = user.Party[2];
            existingParty.Slot4 = user.Party[3];
            existingParty.Slot5 = user.Party[4];
            existingParty.Slot6 = user.Party[5];

            await db.SaveChangesAsync();
            return (true, $"Successfully updated party save {partyName}");
        }

        // Create new party
        var newParty = new Party
        {
            UserId = userId,
            Name = partyName,
            Slot1 = user.Party[0],
            Slot2 = user.Party[1],
            Slot3 = user.Party[2],
            Slot4 = user.Party[3],
            Slot5 = user.Party[4],
            Slot6 = user.Party[5]
        };

        db.Parties.Add(newParty);
        await db.SaveChangesAsync();
        return (true, "Successfully created a new party save.");
    }

    /// <summary>
    ///     Check if a party exists
    /// </summary>
    public async Task<bool> DoesPartyExist(ulong userId, string partyName)
    {
        return await db.Parties
            .AnyAsync(p => p.UserId == userId && p.Name == partyName);
    }

    /// <summary>
    ///     Deregister a party
    /// </summary>
    public async Task<(bool Success, string Message)> DeregisterParty(ulong userId, string partyName)
    {
        // Find the party
        var party = await db.Parties
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == partyName);

        if (party == null) return (false, $"You do not have a party with the name `{partyName}`.");

        // Remove the party
        db.Parties.Remove(party);
        await db.SaveChangesAsync();

        return (true, $"Successfully deregistered party `{partyName}`.");
    }

    /// <summary>
    ///     Load a party into the user's active party
    /// </summary>
    public async Task<(bool Success, string Message)> LoadParty(ulong userId, string partyName)
    {
        // Find the party
        var party = await db.Parties
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == partyName);

        if (party == null) return (false, "You don't have a party registered with that name.");

        // Find the user
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.Party == null) return (false, "You have not started!\nStart with `/start` first!");

        // Load party into user's active party
        var updatedParty = new ulong[6]
        {
            party.Slot1 ?? 0,
            party.Slot2 ?? 0,
            party.Slot3 ?? 0,
            party.Slot4 ?? 0,
            party.Slot5 ?? 0,
            party.Slot6 ?? 0
        };

        user.Party = updatedParty;
        await db.SaveChangesAsync();

        return (true, "Successfully, updated current party from saved data.");
    }

    /// <summary>
    ///     Get a list of the user's saved parties
    /// </summary>
    public async Task<List<string>> GetUserParties(ulong userId)
    {
        var parties = await db.Parties
            .Where(p => p.UserId == userId)
            .Select(p => p.Name)
            .ToListAsync();

        return parties;
    }

    /// <summary>
    ///     Add a Pokemon to a specific party slot
    /// </summary>
    public async Task<(bool Success, string Message)> AddPokemonToPartySlot(ulong userId, int slot, int pokeIndex,
        string partyName)
    {
        try
        {
            // Check if the Pokemon exists
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Pokemon == null)
                return (false, "You have not started!\nStart with `/start` first!");

            if (pokeIndex <= 0 || pokeIndex > user.Pokemon.Length)
                return (false, "Invalid ID Provided\nPlease try again.");

            var pokemonId = user.Pokemon[pokeIndex - 1];

            // Check if the Pokemon exists in the user's collection
            var pokemon = await db.UserPokemon
                .FirstOrDefaultAsync(p => p.Id == pokemonId);

            if (pokemon == null) return (false, "Invalid ID Provided\nPlease try again.");

            // Find the party
            var party = await db.Parties
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == partyName);

            if (party == null)
            {
                // If the party doesn't exist, create it from the current user party
                var result = await RegisterParty(userId, partyName);
                if (!result.Success) return result;

                party = await db.Parties
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == partyName);
            }

            // Check if the Pokemon is already in the party
            var partySlots = new[]
            {
                party.Slot1,
                party.Slot2,
                party.Slot3,
                party.Slot4,
                party.Slot5,
                party.Slot6
            };

            if (partySlots.Any(s => s.HasValue && s.Value == pokemonId))
                return (false, "This pokemon already occupies a slot in your party!");

            // Update the slot
            switch (slot)
            {
                case 1:
                    party.Slot1 = pokemonId;
                    break;
                case 2:
                    party.Slot2 = pokemonId;
                    break;
                case 3:
                    party.Slot3 = pokemonId;
                    break;
                case 4:
                    party.Slot4 = pokemonId;
                    break;
                case 5:
                    party.Slot5 = pokemonId;
                    break;
                case 6:
                    party.Slot6 = pokemonId;
                    break;
                default:
                    return (false, "Invalid slot number.");
            }

            await db.SaveChangesAsync();
            return (true, $"Successfully added {pokemon.PokemonName} to slot {slot} in party {partyName}");
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
    ///     Store a paged result for a user
    /// </summary>
    public Task StorePagedResult(ulong userId, List<EmbedBuilder> pages, int currentPage)
    {
        _pagedResults[userId] = (pages, currentPage);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Get a paged result for a user
    /// </summary>
    public Task<(List<EmbedBuilder> Pages, int CurrentPage)> GetPagedResult(ulong userId)
    {
        if (_pagedResults.TryGetValue(userId, out var result)) return Task.FromResult(result);

        return Task.FromResult<(List<EmbedBuilder> Pages, int CurrentPage)>((null, 0));
    }

    /// <summary>
    ///     Update a paged result for a user
    /// </summary>
    public Task UpdatePagedResult(ulong userId, int newPage)
    {
        if (_pagedResults.TryGetValue(userId, out var result)) _pagedResults[userId] = (result.Pages, newPage);

        return Task.CompletedTask;
    }
}

/// <summary>
///     Result class for service operations
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; }

    public static ServiceResult FromSuccess(string message)
    {
        return new ServiceResult { Success = true, Message = message };
    }

    public static ServiceResult FromError(string message)
    {
        return new ServiceResult { Success = false, Message = message };
    }
}