using Discord.Interactions;
using Ditto.Common.ModuleBases;
using Ditto.Modules.Parties.Services;
using Microsoft.EntityFrameworkCore;

namespace Ditto.Modules.Parties;

[Group("party", "Commands for loading, registering, and deleting parties")]
public class PartyModule(PartyService partyService) : DittoSlashModuleBase<PartyService>
{
    [SlashCommand("setup", "Menu for party configuration")]
    public async Task PartySetup([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName)
    {
        // Fetches party information and creates a party configuration menu
        var (description, party, partyPokeIds, pokemonIndices, pokemonNames) =
            await partyService.GetPartySetupData(ctx.User.Id, partyName);

        if (party == null)
        {
            await ErrorAsync("No party found with the name specified.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Party Configuration")
            .WithDescription(description)
            .WithColor(new Color(0xDD, 0x00, 0xDD))
            .WithFooter("Use the Buttons below to set your party");

        // Create component builder with party buttons
        var components = new ComponentBuilder();

        // Add slot buttons - Row 1
        components.WithButton(customId: $"party:slot:1:{partyName}", emote: new Emoji("<:1:1013539737014907030>"),
            label: pokemonNames[0], style: ButtonStyle.Secondary, row: 0);
        components.WithButton(customId: $"party:slot:2:{partyName}", emote: new Emoji("<:2:1013539739263041618>"),
            label: pokemonNames[1], style: ButtonStyle.Secondary, row: 0);
        components.WithButton(customId: $"party:slot:3:{partyName}", emote: new Emoji("<:3:1013539741502812310>"),
            label: pokemonNames[2], style: ButtonStyle.Secondary, row: 0);

        // Add slot buttons - Row 2
        components.WithButton(customId: $"party:slot:4:{partyName}", emote: new Emoji("<:4:1013539744027783208>"),
            label: pokemonNames[3], style: ButtonStyle.Secondary, row: 1);
        components.WithButton(customId: $"party:slot:5:{partyName}", emote: new Emoji("<:5:1013539745692909740>"),
            label: pokemonNames[4], style: ButtonStyle.Secondary, row: 1);
        components.WithButton(customId: $"party:slot:6:{partyName}", emote: new Emoji("<:6:1013539747517444208>"),
            label: pokemonNames[5], style: ButtonStyle.Secondary, row: 1);

        // Add close button - Row 3
        components.WithButton(customId: "party:close", label: "Close Menu",
            emote: new Emoji("<:delete:1051241645447848009>"), style: ButtonStyle.Danger, row: 2);

        await ctx.Interaction.RespondAsync(embed: embed.Build(), components: components.Build(), ephemeral: true);
    }

    [SlashCommand("view", "View your loaded party or another party you have saved")]
    public async Task PartyView([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName = null)
    {
        var embed = await partyService.GetPartyViewEmbed(ctx.User.Id, partyName);

        if (embed == null)
        {
            await ErrorAsync("You have not started!\nStart with `/start` first!");
            return;
        }

        await ctx.Interaction.RespondAsync(embed: embed.Build());
    }

    [SlashCommand("add", "Add a pokemon to a slot in your party")]
    public async Task PartyAdd(int slot, int poke = 0)
    {
        if (slot < 1 || slot > 6)
        {
            await ErrorAsync("You only add a Pokemon to a slot between 1 and 6!");
            return;
        }

        var result = await partyService.AddPokemonToParty(ctx.User.Id, slot, poke);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    [SlashCommand("remove", "Remove a pokemon from a slot in your party")]
    public async Task PartyRemove(int slot)
    {
        if (slot < 1 || slot > 6)
        {
            await ErrorAsync("Slot must be between 1 and 6!");
            return;
        }

        var result = await partyService.RemovePokemonFromParty(ctx.User.Id, slot);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    [SlashCommand("register", "Register your current party with a name")]
    public async Task PartyRegister(string partyName)
    {
        partyName = partyName.ToLower();

        if (partyName.Length > 20)
        {
            await ErrorAsync("That party name is too long. Please choose a shorter one.");
            return;
        }

        var result = await partyService.RegisterParty(ctx.User.Id, partyName);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    [SlashCommand("deregister", "Deregister a Party from your saved partys")]
    public async Task PartyDeregister([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName)
    {
        partyName = partyName.ToLower();

        // Check if the party exists
        var partyExists = await partyService.DoesPartyExist(ctx.User.Id, partyName);

        if (!partyExists)
        {
            await ErrorAsync($"You do not have a party with the name `{partyName}`.");
            return;
        }

        // Confirm deletion
        var confirmed =
            await PromptUserConfirmAsync($"Are you sure you want to deregister party `{partyName}`?", ctx.User.Id);

        if (!confirmed)
        {
            await ctx.Interaction.ModifyOriginalResponseAsync(props => props.Content = "Party deletion canceled.");
            return;
        }

        var result = await partyService.DeregisterParty(ctx.User.Id, partyName);

        if (result.Success)
            await ctx.Interaction.ModifyOriginalResponseAsync(props => props.Content = result.Message);
        else
            await ctx.Interaction.ModifyOriginalResponseAsync(props => props.Content = $"Error: {result.Message}");
    }

    [SlashCommand("load", "Load a registered party save by name")]
    public async Task PartyLoad([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName)
    {
        partyName = partyName.ToLower();

        var result = await partyService.LoadParty(ctx.User.Id, partyName);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    [SlashCommand("list", "List your saved partys")]
    public async Task PartyList()
    {
        var parties = await partyService.GetUserParties(ctx.User.Id);

        if (parties == null || !parties.Any())
        {
            await ErrorAsync("You do not have any saved parties. Register one with `/party register` first.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Your Saved Parties")
            .WithColor(new Color(0xDD, 0x00, 0xDD))
            .WithDescription(string.Join("\n", parties));

        await ctx.Interaction.RespondAsync(embed: embed.Build());
    }
}

public class PartyInteractionModule(PartyService partyService) : DittoSlashModuleBase<PartyService>
{
    [ComponentInteraction("party:slot:*:*")]
    public async Task HandlePartySlot(string slotNumber, string partyName)
    {
        // Create and show modal for adding Pokémon
        var modal = new ModalBuilder()
            .WithTitle("Add a pokemon to your party")
            .WithCustomId($"party:add:{slotNumber}:{partyName}")
            .AddTextInput("Pokemon Number from your pokemon list", "pokemon", required: true, maxLength: 15);

        await ctx.Interaction.RespondWithModalAsync(modal.Build());
    }

    [ModalInteraction("party:add:*:*")]
    public async Task HandleAddPokemonModal(string slotNumberStr, string partyName, AddPokemonModal modal)
    {
        await ctx.Interaction.DeferAsync(true);

        try
        {
            if (!int.TryParse(modal.Pokemon, out var pokeId) || !int.TryParse(slotNumberStr, out var slotNumber))
            {
                await FollowupAsync("Invalid ID provided. Please enter a number.", ephemeral: true);
                return;
            }

            var result = await partyService.AddPokemonToPartySlot(ctx.User.Id, slotNumber, pokeId, partyName);

            if (!result.Success)
            {
                await FollowupAsync(result.Message, ephemeral: true);
                return;
            }

            // Get updated party details and create updated UI
            var (description, updatedParty, partyPokeIds, pokemonIndices, pokemonNames) =
                await partyService.GetPartySetupData(ctx.User.Id, partyName);

            var embed = new EmbedBuilder()
                .WithTitle($"Slot {slotNumber} Updated!")
                .WithDescription(description)
                .WithColor(new Color(0xDD, 0x00, 0xDD))
                .WithFooter($"Updated Pokemon Party | Party: {partyName}");

            // Create component builder with updated party buttons
            var components = new ComponentBuilder();

            // Add slot buttons - Row 1
            components.WithButton(customId: $"party:slot:1:{partyName}", emote: new Emoji("<:1:1013539737014907030>"),
                label: pokemonNames[0], style: ButtonStyle.Secondary, row: 0);
            components.WithButton(customId: $"party:slot:2:{partyName}", emote: new Emoji("<:2:1013539739263041618>"),
                label: pokemonNames[1], style: ButtonStyle.Secondary, row: 0);
            components.WithButton(customId: $"party:slot:3:{partyName}", emote: new Emoji("<:3:1013539741502812310>"),
                label: pokemonNames[2], style: ButtonStyle.Secondary, row: 0);

            // Add slot buttons - Row 2
            components.WithButton(customId: $"party:slot:4:{partyName}", emote: new Emoji("<:4:1013539744027783208>"),
                label: pokemonNames[3], style: ButtonStyle.Secondary, row: 1);
            components.WithButton(customId: $"party:slot:5:{partyName}", emote: new Emoji("<:5:1013539745692909740>"),
                label: pokemonNames[4], style: ButtonStyle.Secondary, row: 1);
            components.WithButton(customId: $"party:slot:6:{partyName}", emote: new Emoji("<:6:1013539747517444208>"),
                label: pokemonNames[5], style: ButtonStyle.Secondary, row: 1);

            // Add close button - Row 3
            components.WithButton(customId: "party:close", label: "Close Menu",
                emote: new Emoji("<:delete:1051241645447848009>"), style: ButtonStyle.Danger, row: 2);

            await FollowupAsync(embed: embed.Build(), components: components.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            await ctx.Client.GetChannelAsync(1004311971853779005)
                .ContinueWith(t =>
                    (t.Result as IMessageChannel)?.SendMessageAsync(
                        $"**__ERROR OCCURRED__**: ```{ex.Message}\n{ex.StackTrace}```"));

            await FollowupAsync(
                "Something went wrong\nPlease try running this command again.\nJoin the official server in `/ditto info` command if this continues for help.",
                ephemeral: true);
        }
    }

    [ComponentInteraction("party:close")]
    public async Task HandleCloseMenu()
    {
        await ctx.Interaction.ModifyOriginalResponseAsync(props =>
        {
            props.Content =
                "Configuration menu closed!\n**Please use the `/party setup` command again to restart this menu.**";
            props.Embeds = new Embed[] { };
            props.Components = new ComponentBuilder().Build();
        });
    }
}

// Autocomplete handler for party names
public class PartyNameAutocompleteHandler(DittoContext db) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var currentValue = autocompleteInteraction.Data.Current.Value.ToString();

        var userParties = await db.Parties
            .Where(p => p.UserId == context.User.Id)
            .Select(p => p.Name)
            .ToListAsync();

        if (string.IsNullOrEmpty(currentValue))
            return AutocompletionResult.FromSuccess(
                userParties.Take(25).Select(p => new AutocompleteResult(p, p))
            );

        var filteredParties = userParties
            .Where(p => p.Contains(currentValue, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(p => new AutocompleteResult(p, p));

        return AutocompletionResult.FromSuccess(filteredParties);
    }
}

// Modal for adding Pokemon to a party
public class AddPokemonModal : IModal
{
    [InputLabel("Pokemon Number from your pokemon list")]
    [ModalTextInput("pokemon", maxLength: 15)]
    public string Pokemon { get; set; }

    public string Title => "Add a pokemon to your party";
}