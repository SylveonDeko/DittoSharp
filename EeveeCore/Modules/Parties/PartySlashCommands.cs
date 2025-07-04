using Discord.Interactions;
using EeveeCore.Common.AutoCompletes;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Parties.Services;

namespace EeveeCore.Modules.Parties;

/// <summary>
///     Provides Discord slash commands for managing Pokémon parties.
///     Allows users to create, view, modify, and manage multiple party configurations.
/// </summary>
[Group("party", "Commands for loading, registering, and deleting parties")]
public class PartyModule : EeveeCoreSlashModuleBase<PartyService>
{
    /// <summary>
    ///     Displays an interactive menu for configuring a party.
    ///     Shows current party composition and allows modification via buttons.
    /// </summary>
    /// <param name="partyName">The name of the party to configure (autocompleted).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("setup", "Menu for party configuration")]
    public async Task PartySetup([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName)
    {
        // Fetches party information and creates a party configuration menu
        var (description, party, partyPokeIds, pokemonIndices, pokemonNames) =
            await Service.GetPartySetupData(ctx.User.Id, partyName);

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

    /// <summary>
    ///     Displays information about a party's Pokémon composition.
    ///     Shows the current active party if no party name is provided.
    /// </summary>
    /// <param name="partyName">The name of the party to view, or null for the active party (autocompleted).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("view", "View your loaded party or another party you have saved")]
    public async Task PartyView([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName = null)
    {
        var embed = await Service.GetPartyViewEmbed(ctx.User.Id, partyName);

        if (embed == null)
        {
            await ErrorAsync("You have not started!\nStart with `/start` first!");
            return;
        }

        await ctx.Interaction.RespondAsync(embed: embed.Build());
    }

    /// <summary>
    ///     Adds a Pokémon to a specified slot in the active party.
    ///     Uses the currently selected Pokémon if no specific index is provided.
    /// </summary>
    /// <param name="slot">The slot number to add the Pokémon to (1-6).</param>
    /// <param name="poke">The index of the Pokémon in the user's collection, or 0 to use the selected Pokémon.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("add", "Add a pokemon to a slot in your party")]
    public async Task PartyAdd(int slot, int poke = 0)
    {
        if (slot < 1 || slot > 6)
        {
            await ErrorAsync("You only add a Pokemon to a slot between 1 and 6!");
            return;
        }

        var result = await Service.AddPokemonToParty(ctx.User.Id, slot, poke);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    /// <summary>
    ///     Removes a Pokémon from a specified slot in the active party.
    ///     Leaves the slot empty after removal.
    /// </summary>
    /// <param name="slot">The slot number to remove the Pokémon from (1-6).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("remove", "Remove a pokemon from a slot in your party")]
    public async Task PartyRemove(int slot)
    {
        if (slot < 1 || slot > 6)
        {
            await ErrorAsync("Slot must be between 1 and 6!");
            return;
        }

        var result = await Service.RemovePokemonFromParty(ctx.User.Id, slot);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    /// <summary>
    ///     Saves the current active party under a specified name.
    ///     Overwrites any existing party with the same name.
    /// </summary>
    /// <param name="partyName">The name to save the party under (max 20 characters).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("register", "Register your current party with a name")]
    public async Task PartyRegister(string partyName)
    {
        partyName = partyName.ToLower();

        if (partyName.Length > 20)
        {
            await ErrorAsync("That party name is too long. Please choose a shorter one.");
            return;
        }

        var result = await Service.RegisterParty(ctx.User.Id, partyName);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    /// <summary>
    ///     Deletes a saved party configuration.
    ///     Requires confirmation before deletion.
    /// </summary>
    /// <param name="partyName">The name of the party to delete (autocompleted).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("deregister", "Deregister a Party from your saved partys")]
    public async Task PartyDeregister([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName)
    {
        partyName = partyName.ToLower();

        // Check if the party exists
        var partyExists = await Service.DoesPartyExist(ctx.User.Id, partyName);

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

        var result = await Service.DeregisterParty(ctx.User.Id, partyName);

        if (result.Success)
            await ctx.Interaction.ModifyOriginalResponseAsync(props => props.Content = result.Message);
        else
            await ctx.Interaction.ModifyOriginalResponseAsync(props => props.Content = $"Error: {result.Message}");
    }

    /// <summary>
    ///     Loads a saved party into the active party slot.
    ///     Replaces the current active party configuration.
    /// </summary>
    /// <param name="partyName">The name of the party to load (autocompleted).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("load", "Load a registered party save by name")]
    public async Task PartyLoad([Autocomplete(typeof(PartyNameAutocompleteHandler))] string partyName)
    {
        partyName = partyName.ToLower();

        var result = await Service.LoadParty(ctx.User.Id, partyName);

        if (result.Success)
            await ConfirmAsync(result.Message);
        else
            await ErrorAsync(result.Message);
    }

    /// <summary>
    ///     Displays a list of all saved party configurations for the user.
    ///     Shows party names that can be referenced in other commands.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("list", "List your saved partys")]
    public async Task PartyList()
    {
        var parties = await Service.GetUserParties(ctx.User.Id);

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

/// <summary>
///     Handles component interactions for the party system.
///     Processes button clicks, modals, and other interactive elements
///     related to party management.
/// </summary>
/// <param name="Service">The service that handles party data operations.</param>
public class PartyInteractionModule(PartyService Service) : EeveeCoreSlashModuleBase<PartyService>
{
    /// <summary>
    ///     Handles interactions with party slot buttons.
    ///     Opens a modal to input Pokémon ID for the selected slot.
    /// </summary>
    /// <param name="slotNumber">The slot number the user clicked (1-6).</param>
    /// <param name="partyName">The name of the party being modified.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    ///     Processes submissions from the add Pokémon modal.
    ///     Updates the specified party with the selected Pokémon.
    /// </summary>
    /// <param name="slotNumberStr">The slot number as a string (1-6).</param>
    /// <param name="partyName">The name of the party being modified.</param>
    /// <param name="modal">The modal containing the Pokémon ID input.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

            var result = await Service.AddPokemonToPartySlot(ctx.User.Id, slotNumber, pokeId, partyName);

            if (!result.Success)
            {
                await FollowupAsync(result.Message, ephemeral: true);
                return;
            }

            // Get updated party details and create updated UI
            var (description, updatedParty, partyPokeIds, pokemonIndices, pokemonNames) =
                await Service.GetPartySetupData(ctx.User.Id, partyName);

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
                "Something went wrong\nPlease try running this command again.\nJoin the official server in `/EeveeCore info` command if this continues for help.",
                ephemeral: true);
        }
    }

    /// <summary>
    ///     Handles the close menu button interaction.
    ///     Clears the setup menu and displays a message.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

/// <summary>
///     Defines the modal form for adding a Pokémon to a party slot.
///     Collects the Pokémon number from the user's input.
/// </summary>
public class AddPokemonModal : IModal
{
    /// <summary>
    ///     The Pokémon number/index from the user's collection.
    ///     Input by the user in the modal text field.
    /// </summary>
    [InputLabel("Pokemon Number from your pokemon list")]
    [ModalTextInput("pokemon", maxLength: 15)]
    public string Pokemon { get; set; }

    /// <summary>
    ///     The title displayed at the top of the modal.
    /// </summary>
    public string Title => "Add a pokemon to your party";
}