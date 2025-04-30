using System.Text;
using Discord.Interactions;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Pokemon.Services;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Humanizer;
using Serilog;

namespace EeveeCore.Modules.Pokemon;

/// <summary>
///     Provides Discord slash commands for Pokemon-related functionality.
///     Includes commands for viewing, managing, and interacting with Pokemon in the user's collection.
/// </summary>
/// <param name="interactivity">Service for handling interactive components like pagination.</param>
[Group("pokemon", "Pokemon related commands")]
public class PokemonSlashCommands(InteractiveService interactivity)
    : EeveeCoreSlashModuleBase<PokemonService>
{
    /// <summary>
    ///     Represents the different Pokemon variant types.
    ///     Used for specifying special forms when viewing Pokemon information.
    /// </summary>
    public enum PokemonVariantType
    {
        /// <summary>Shiny variant of a Pokemon</summary>
        Shiny,

        /// <summary>Radiant variant of a Pokemon</summary>
        Radiant,

        /// <summary>Shadow variant of a Pokemon</summary>
        Shadow
    }

    /// <summary>
    ///     Collection of footer texts that are randomly displayed in embeds.
    ///     Contains tips, information, and promotional messages for users.
    /// </summary>
    private readonly string[] _footers =
    [
        "Use /donate for exclusive rewards - Thank you everyone for your continued support!",
        "Upvote EeveeCoreBOT with the /vote command and get energy, credits, redeems and more!",
        "The latest updates can be viewed with the /updates command!",
        "Join our official server! discord.gg/EeveeCore <3.",
        "Take a look at one of our partners bots! Mewdeko - discord.gg/mewdeko, we think its the best all-purpose bot around!",
        "We are always looking for new help! Art Team, Staff Team, and Dev team-ask in the official server!"
    ];

    /// <summary>
    ///     Random number generator for various randomized elements.
    ///     Used for selecting random footers and generating random colors for embeds.
    /// </summary>
    private readonly Random _random = new();

    /// <summary>
    ///     Selects a Pokemon by its ID number.
    ///     Makes the specified Pokemon the user's active Pokemon for commands that operate on the selected Pokemon.
    /// </summary>
    /// <param name="pokeId">The ID of the Pokemon to select.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [SlashCommand("select", "Select a pokemon by ID number")]
    [RequireContext(ContextType.Guild)]
    public async Task SelectPokemon(int pokeId)
    {
        await DeferAsync();
        var result = await Service.SelectPokemon(ctx.User.Id, pokeId);
        if (!result.Success)
        {
            await ctx.Interaction.SendErrorFollowupAsync(result.Message);
            return;
        }

        await ctx.Interaction.SendConfirmFollowupAsync(result.Message);
    }

    /// <summary>
    ///     Displays a paginated list of the user's Pokemon collection.
    ///     Supports various sorting methods, filters, view modes, and search functionality.
    /// </summary>
    /// <param name="sortBy">The method to sort Pokemon by (iv, level, name, recent, type, favorite, party, champion).</param>
    /// <param name="filter">
    ///     Filter to apply to the Pokemon list (all, shiny, radiant, shadow, legendary, favorite, champion,
    ///     party, market).
    /// </param>
    /// <param name="viewMode">The display mode for the list (normal, compact, detailed).</param>
    /// <param name="search">Optional search term to filter Pokemon by name, nickname, tags, or moves.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [SlashCommand("list", "Shows a detailed list of all your obtained pokemon")]
    public async Task ListPokemon(
        [Summary("sort", "Sort method")]
        [Choice("IV", "iv")]
        [Choice("Level", "level")]
        [Choice("Name", "name")]
        [Choice("Recent", "recent")]
        [Choice("Type", "type")]
        [Choice("Favorites", "favorite")]
        [Choice("Party", "party")]
        [Choice("Champion", "champion")]
        string sortBy = "default",
        [Summary("filter", "Filter Pokemon")]
        [Choice("All", "all")]
        [Choice("Shiny", "shiny")]
        [Choice("Radiant", "radiant")]
        [Choice("Shadow", "shadow")]
        [Choice("Legendary", "legendary")]
        [Choice("Favorite", "favorite")]
        [Choice("Champion", "champion")]
        [Choice("Party", "party")]
        [Choice("Market", "market")]
        string filter = "all",
        [Summary("gender", "Filter by gender")]
        [Choice("All", "all")]
        [Choice("Male", "male")]
        [Choice("Female", "female")]
        [Choice("Genderless", "genderless")]
        string genderPicked = "all",
        [Summary("view", "View mode")]
        [Choice("Normal", "normal")]
        [Choice("Compact", "compact")]
        [Choice("Detailed", "detailed")]
        string viewMode = "normal",
        [Summary("search", "Search for specific Pokemon")]
        string search = null)
    {
        await DeferAsync();

        var sortOrder = sortBy switch
        {
            "iv" => SortOrder.Iv,
            "level" => SortOrder.Level,
            "name" => SortOrder.Name,
            "recent" => SortOrder.Recent,
            "type" => SortOrder.Type,
            "favorite" => SortOrder.Favorite,
            "party" => SortOrder.Party,
            "champion" => SortOrder.Champion,
            _ => SortOrder.Default
        };

        // Get filtered Pokemon list with all the necessary data from the service
        var (filteredList, stats, partyLookup, selectedPokemon) =
            await Service.GetFilteredPokemonList(ctx.User.Id, sortOrder, filter, genderPicked, search);

        if (filteredList.Count == 0 && genderPicked == "all" && sortBy == "default" && filter == "all" && string.IsNullOrWhiteSpace(search))
        {
            await ctx.Interaction.SendErrorFollowupAsync("You have not started!\nStart with `/start` first!");
            return;
        }

        // Handle empty results after filtering
        if (filteredList.Count == 0)
        {
            await ctx.Interaction.SendErrorFollowupAsync("No Pokemon match your filters! Try different filter options.");
            return;
        }

        // Generate statistics summary
        var statsEmbed = new EmbedBuilder()
            .WithTitle("Your Pokemon Collection")
            .WithColor(Color.Gold)
            .WithDescription(GenerateCollectionStats(stats, filter, filteredList.Count))
            .Build();

        // Create paginated list based on view mode
        var pages = new List<PageBuilder>();
        var itemsPerPage = viewMode switch
        {
            "compact" => 30,
            "detailed" => 8,
            _ => 15
        };

        var totalPages = (filteredList.Count - 1) / itemsPerPage + 1;

        // Build pages based on view mode
        for (var i = 0; i < totalPages; i++)
        {
            var pageItems = filteredList
                .Skip(i * itemsPerPage)
                .Take(itemsPerPage);

            var description = new StringBuilder();

            if (viewMode == "detailed")
                foreach (var pokemon in pageItems)
                {
                    description.AppendLine(GenerateDetailedListEntry(
                        pokemon,
                        partyLookup.Contains((int)pokemon.BotId),
                        pokemon.BotId == selectedPokemon));
                    description.AppendLine(); // Add space between entries
                }
            else // Normal or compact
                foreach (var pokemon in pageItems)
                {
                    var emoji = GetPokemonEmoji(pokemon.Shiny, pokemon.Radiant, pokemon.Skin);
                    var gender = GetGenderEmoji(pokemon.Gender);
                    var favorite = pokemon.Favorite ? "⭐ " : "";
                    var inParty = partyLookup.Contains((int)pokemon.BotId) ? "👥 " : "";
                    var isSelected = pokemon.BotId == selectedPokemon ? "🔍 " : "";
                    var champion = pokemon.Champion ? "🏆 " : "";
                    var market = pokemon.MarketEnlist ? "💰 " : "";

                    if (viewMode == "compact")
                    {
                        description.AppendLine(
                            $"{emoji}{gender}{favorite}{inParty}{isSelected}{champion}{market}**{pokemon.Name.Capitalize()}** | " +
                            $"Lv.{pokemon.Level} | " +
                            $"{pokemon.IvPercent:P0}");
                    }
                    else // normal view
                    {
                        description.AppendLine(
                            $"{emoji}{gender}{favorite}{inParty}{isSelected}{champion}{market}**{pokemon.Name.Capitalize()}** | " +
                            $"**No.** {pokemon.Number} | " +
                            $"**Level** {pokemon.Level} | " +
                            $"**IV%** {pokemon.IvPercent:P2}");

                        // Add nickname if different from Pokemon name
                        if (!string.IsNullOrEmpty(pokemon.Nickname) && pokemon.Nickname != pokemon.Name)
                            description.AppendLine(
                                $"└ Nickname: `{pokemon.Nickname}` | Held Item: `{pokemon.HeldItem ?? "None"}`");
                    }
                }

            // Add filter/sort info to title
            var filterInfo = filter != "all" ? $" ({filter.Capitalize()})" : "";
            var sortInfo = sortBy != "default" ? $" - Sorted by {sortBy.Capitalize()}" : "";

            var embed = new EmbedBuilder()
                .WithTitle($"Your Pokemon{filterInfo}{sortInfo}")
                .WithDescription(description.ToString())
                .WithColor(new Color(255, 182, 193))
                .WithFooter($"Page {i + 1}/{totalPages} • {filteredList.Count} Pokémon")
                .Build();

            pages.Add(PageBuilder.FromEmbed(embed));
        }

        // Add statistics page at the start
        pages.Insert(0, PageBuilder.FromEmbed(statsEmbed));

        // Add legend to understand icons
        var legendPage = new PageBuilder()
            .WithTitle("Icon Legend")
            .WithDescription(
                "⭐ - Favorite Pokemon\n" +
                "👥 - Party Member\n" +
                "🔍 - Currently Selected\n" +
                "🏆 - Champion Pokemon\n" +
                "💰 - Listed in Market\n" +
                "<:shiny:1057764628349853786> - Shiny Pokemon\n" +
                "<:radiant:1057764536456966275> - Radiant Pokemon\n" +
                "<:shadow:1057764584954568775> - Shadow Pokemon\n" +
                "♂️ - Male Pokemon\n" +
                "♀️ - Female Pokemon"
            )
            .WithColor(Color.Gold);

        pages.Add(legendPage);

        // Create paginator with enhanced controls
        var pager = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithUsers(ctx.User)
            .WithDefaultEmotes()
            .WithFooter(PaginatorFooter.PageNumber)
            .Build();

        await interactivity.SendPaginatorAsync(pager, Context.Interaction, TimeSpan.FromMinutes(10),
            InteractionResponseType.DeferredUpdateMessage);
    }

// Helper methods
    /// <summary>
    ///     Generates summary statistics for the user's Pokemon collection.
    ///     Creates a formatted string with counts of different Pokemon types and variants.
    /// </summary>
    /// <param name="allPokemon">The complete list of the user's Pokemon.</param>
    /// <param name="filtered">The filtered list of Pokemon based on current view options.</param>
    /// <param name="filter">The current filter being applied.</param>
    /// <returns>A formatted string containing collection statistics.</returns>
    private string GenerateCollectionStats(Dictionary<string, int> stats, string filter, int filteredCount)
    {
        var statsBuilder = new StringBuilder();

        // Shows stats for the filtered collection
        statsBuilder.AppendLine($"**Collection Overview** - {stats["Total"]} Pokémon");
        statsBuilder.AppendLine($"• Shiny: {stats["Shiny"]}");
        statsBuilder.AppendLine($"• Radiant: {stats["Radiant"]}");
        statsBuilder.AppendLine($"• Shadow: {stats["Shadow"]}");
        statsBuilder.AppendLine($"• Legendary: {stats["Legendary"]}");
        statsBuilder.AppendLine($"• Favorite: {stats["Favorite"]}");
        statsBuilder.AppendLine($"• Champion: {stats["Champion"]}");
        statsBuilder.AppendLine($"• Market Listed: {stats["Market"]}");
        statsBuilder.AppendLine($"• Male: {stats["Male"]}");
        statsBuilder.AppendLine($"• Female: {stats["Female"]}");
        statsBuilder.AppendLine($"• Genderless: {stats["Genderless"]}");

        // Show total collection size for comparison
        if (stats.ContainsKey("TotalCount") && stats["TotalCount"] != stats["Total"])
        {
            statsBuilder.AppendLine();
            statsBuilder.AppendLine($"**Total Collection Size**: {stats["TotalCount"]} Pokémon");
        }

        // If filtering, show filter summary
        if (filter != "all")
        {
            statsBuilder.AppendLine();
            statsBuilder.AppendLine($"**Current Filter**: {filter.Capitalize()} - {filteredCount} Pokémon");
        }

        statsBuilder.AppendLine();
        statsBuilder.AppendLine("Use the arrow controls to navigate your Pokemon list");
        statsBuilder.AppendLine("Check the last page for an icon legend");

        return statsBuilder.ToString();
    }

    /// <summary>
    ///     Generates a detailed text entry for a Pokemon in the list view.
    ///     Includes comprehensive information formatted for the detailed view mode.
    /// </summary>
    /// <param name="pokemon">The Pokemon entry to format.</param>
    /// <param name="inParty">Whether the Pokemon is in the user's party.</param>
    /// <param name="isSelected">Whether the Pokemon is currently selected.</param>
    /// <returns>A formatted string containing detailed Pokemon information.</returns>
    private string GenerateDetailedListEntry(PokemonListEntry pokemon, bool inParty, bool isSelected)
    {
        var entry = new StringBuilder();

        var emoji = GetPokemonEmoji(pokemon.Shiny, pokemon.Radiant, pokemon.Skin);
        var gender = GetGenderEmoji(pokemon.Gender);
        var favorite = pokemon.Favorite ? "⭐ " : "";
        var champion = pokemon.Champion ? "🏆 " : "";
        var partyMember = inParty ? "👥 " : "";
        var selected = isSelected ? "🔍 " : "";
        var market = pokemon.MarketEnlist ? "💰 " : "";

        entry.AppendLine(
            $"{emoji}{gender}{favorite}{partyMember}{selected}{champion}{market}**{pokemon.Name.Capitalize()}** (#{pokemon.Number})");

        if (!string.IsNullOrEmpty(pokemon.Nickname) && pokemon.Nickname != pokemon.Name)
            entry.AppendLine($"Nickname: `{pokemon.Nickname}`");

        entry.AppendLine($"Level: `{pokemon.Level}`  |  IV: `{pokemon.IvPercent:P2}`");

        if (!string.IsNullOrEmpty(pokemon.HeldItem) && pokemon.HeldItem.ToLower() != "none")
            entry.AppendLine($"Held Item: `{pokemon.HeldItem}`");

        // Add type info if available
        var types = Service.GetPokemonTypes(pokemon.Name).Result;
        if (types != null && types.Any())
            entry.AppendLine($"Type: {string.Join(" ", types.Select(GetTypeEmote))}");

        // Add move preview if available
        if (pokemon.Moves != null && pokemon.Moves.Length > 0 && !pokemon.Moves.All(string.IsNullOrEmpty))
        {
            var validMoves = pokemon.Moves.Where(m => !string.IsNullOrEmpty(m)).Take(4);
            if (validMoves.Any())
                entry.AppendLine($"Moves: `{string.Join("`, `", validMoves.Select(m => m.Titleize()))}`");
        }

        // Add tags if any
        if (pokemon.Tags != null && pokemon.Tags.Length > 0 && !pokemon.Tags.All(string.IsNullOrEmpty))
        {
            var validTags = pokemon.Tags.Where(t => !string.IsNullOrEmpty(t));
            if (validTags.Any())
                entry.AppendLine($"Tags: `{string.Join("`, `", validTags)}`");
        }

        // Add special flags
        var flags = new List<string>();
        if (!pokemon.Tradable) flags.Add("Not Tradable");
        if (!pokemon.Breedable) flags.Add("Not Breedable");

        if (flags.Any())
            entry.AppendLine($"Flags: `{string.Join("`, `", flags)}`");

        // Add catch date if available
        if (pokemon.Timestamp.HasValue)
            entry.AppendLine($"Caught on: `{pokemon.Timestamp.Value:MMM d, yyyy}`");

        return entry.ToString();
    }

    /// <summary>
    ///     Attempts to resurrect all dead Pokemon belonging to the user.
    ///     Also checks invalid references against dead Pokemon records to recover missing entries.
    ///     Displays a paginated list of successfully resurrected Pokemon with their details.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [SlashCommand("resurrect", "Attempt to resurrect dead Pokemon")]
    [RequireContext(ContextType.Guild)]
    public async Task ResurrectDeadPokemon()
    {
        await DeferAsync();

        // First, check for invalid references that might be dead Pokemon
        var (potentialDeadPokemon, _) = await Service.CheckInvalidReferencesAgainstDeadPokemon(ctx.User.Id);
        var recoveredCount = 0;

        if (potentialDeadPokemon.Count > 0)
        {
            // Recover the references first by creating ownership entries for them
            recoveredCount = await Service.RecoverDeadPokemonReferences(ctx.User.Id);

            if (recoveredCount > 0)
                await FollowupAsync(
                    $"Recovered {recoveredCount} references to dead Pokémon that were missing from your collection.");
        }

        // Now get all dead Pokemon, which should include any we just recovered
        var deadPokemon = await Service.GetDeadPokemon(ctx.User.Id);

        if (deadPokemon.Count == 0)
        {
            if (recoveredCount > 0)
                await FollowupAsync(
                    "Strange... recovered references to dead Pokémon, but couldn't find any actual dead Pokémon.");
            else
                await FollowupAsync("You do not have any dead Pokémon.");
            return;
        }

        await FollowupAsync("Checking for dead Pokémon... and performing necromancy.");

        // Pre-calculate all pages first
        var pages = new List<PageBuilder>();
        const int itemsPerPage = 30;

        for (var i = 0; i < deadPokemon.Count; i += itemsPerPage)
        {
            var pageBuilder = new StringBuilder();
            var pageItems = deadPokemon.Skip(i).Take(itemsPerPage);

            foreach (var pokemon in pageItems)
            {
                var ivPercentage = (pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                                    pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv) / 186.0 * 100;

                // Mark newly recovered Pokemon with a special emoji
                var recoveredEmoji = potentialDeadPokemon.Any(r => r.PokemonId == pokemon.Id) ? "🔄 " : "✅ ";

                pageBuilder.AppendLine(
                    $"{recoveredEmoji}**ID: `{pokemon.Id}` | Name: `{pokemon.PokemonName}` | IV%: `{ivPercentage:F2}`%**");
            }

            pages.Add(new PageBuilder()
                .WithColor(Color.Purple)
                .WithDescription(pageBuilder.ToString()));
        }

        // Add an info page at the beginning if we recovered any Pokemon
        if (recoveredCount > 0)
        {
            var infoPage = new PageBuilder()
                .WithColor(Color.Blue)
                .WithTitle("Pokémon Recovery Report")
                .WithDescription(
                    $"📊 **Recovery Summary**\n\n" +
                    $"• Found {recoveredCount} references to dead Pokémon that were missing from your collection\n" +
                    $"• Successfully linked these references to your actual dead Pokémon\n" +
                    $"• Proceeding with resurrection of all {deadPokemon.Count} dead Pokémon\n\n" +
                    $"In the following pages, recovered Pokémon are marked with 🔄\n" +
                    $"Regular resurrections are marked with ✅"
                );

            pages.Insert(0, infoPage);
        }

        // Now resurrect the Pokemon
        await Service.ResurrectPokemon(deadPokemon);

        if (pages.Count > 0)
        {
            var paginator = new StaticPaginatorBuilder()
                .WithPages(pages)
                .WithUsers(ctx.User)
                .WithDefaultEmotes()
                .WithFooter(PaginatorFooter.PageNumber)
                .Build();

            await interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(10),
                InteractionResponseType.DeferredUpdateMessage);
        }
    }

    /// <summary>
    ///     Displays detailed information about a Pokemon.
    ///     Shows stats, types, moves, and other relevant information about the specified Pokemon.
    ///     If no Pokemon is specified, shows information about the user's currently selected Pokemon.
    /// </summary>
    /// <param name="poke">The name or ID of the Pokemon to get information about, or null for the selected Pokemon.</param>
    /// <param name="variant">The variant type of the Pokemon (Shiny, Radiant, Shadow).</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [SlashCommand("info", "Get information about a pokemon")]
    [RequireContext(ContextType.Guild)]
    public async Task Info(string poke = null, PokemonVariantType? variant = null)
    {
        try
        {
            Database.Models.PostgreSQL.Pokemon.Pokemon? ownedPokemon = null;
            var totalPokemonCount = await Service.GetUserPokemonCount(ctx.User.Id);

            // Case 1: No parameter - get selected Pokemon
            if (string.IsNullOrWhiteSpace(poke))
            {
                ownedPokemon = await Service.GetSelectedPokemon(ctx.User.Id);
                if (ownedPokemon == null)
                {
                    await ctx.Interaction.SendErrorAsync(
                        "You do not have a pokemon selected. Use `/select` to select one!");
                    return;
                }

                var pokemonIndex = await Service.GetPokemonIndex(ctx.User.Id, ownedPokemon.Id);
                await DisplayOwnedPokemonInfo(ownedPokemon, totalPokemonCount, pokemonIndex);
                return;
            }

            // Case 2: "new", "newest", "latest" - get newest Pokemon
            if (poke.ToLower() is "newest" or "latest" or "atest" or "ewest" or "new")
            {
                ownedPokemon = await Service.GetNewestPokemon(ctx.User.Id);
                if (ownedPokemon == null)
                {
                    await ctx.Interaction.SendErrorAsync("You have not started!\nStart with `/start` first.");
                    return;
                }

                var pokemonIndex = await Service.GetPokemonIndex(ctx.User.Id, ownedPokemon.Id);
                await DisplayOwnedPokemonInfo(ownedPokemon, totalPokemonCount, pokemonIndex);
                return;
            }

            // Case 3: Numeric input - get Pokemon by number from user's list
            if (int.TryParse(poke, out var pokeNumber))
            {
                if (pokeNumber < 1)
                {
                    await ctx.Interaction.SendErrorAsync("That is not a valid pokemon number!");
                    return;
                }

                if (pokeNumber > 4000000000)
                {
                    await ctx.Interaction.SendErrorAsync("You probably don't have that many pokemon...");
                    return;
                }

                ownedPokemon = await Service.GetPokemonByNumber(ctx.User.Id, pokeNumber);
                if (ownedPokemon == null)
                {
                    await ctx.Interaction.SendErrorAsync(
                        "You do not have that many pokemon. Go catch some more first!");
                    return;
                }

                var pokemonIndex = await Service.GetPokemonIndex(ctx.User.Id, ownedPokemon.Id);
                await DisplayOwnedPokemonInfo(ownedPokemon, totalPokemonCount, pokemonIndex);
                return;
            }

            // Case 4: Pokemon name with potential variants
            var pokemonName = poke.ToLower().Replace("alolan", "alola").Split(' ');
            var shiny = false;
            var radiant = false;
            string skin = null;

            // Handle variant parameter
            if (variant.HasValue)
                switch (variant.Value)
                {
                    case PokemonVariantType.Shiny:
                        shiny = true;
                        break;
                    case PokemonVariantType.Radiant:
                        radiant = true;
                        break;
                    case PokemonVariantType.Shadow:
                        skin = "shadow";
                        break;
                }

            // Check name for variants
            if (pokemonName.Contains("shiny"))
            {
                shiny = true;
                pokemonName = pokemonName.Where(v => v != "shiny").ToArray();
            }
            else if (pokemonName.Contains("radiant"))
            {
                radiant = true;
                pokemonName = pokemonName.Where(v => v != "radiant").ToArray();
            }
            else if (pokemonName.Contains("shadow") && Array.IndexOf(pokemonName, "shadow") == 0)
            {
                skin = "shadow";
                pokemonName = pokemonName.Where(v => v != "shadow").ToArray();
            }

            var finalName = string.Join("-", pokemonName);
            var val = finalName.ToTitleCase();
            var pokemon = await Service.GetPokemonInfo(finalName);
            var (form, imageUrl) = await Service.GetPokemonFormInfo(pokemon.Name,
                pokemon.Name.ToLower().Contains("shiny"));

            // Get forms
            var forms = await Service.GetPokemonForms(val);
            if (string.IsNullOrEmpty(forms)) forms = "None";

            // Get Pokemon info
            var pokemonInfo = await Service.GetPokemonInfo(val);
            if (pokemonInfo == null)
            {
                await ctx.Interaction.SendErrorAsync(
                    "<:error:1009448089930694766> Oops! It seems the pokemon you tried to view with `/i` is currently having some issues on our side!\nPlease try again later!");
                return;
            }

            // Build the embed
            var emoji = GetPokemonEmoji(shiny, radiant, skin);
            var embed = new EmbedBuilder()
                .WithTitle($"{emoji}{val}")
                .WithColor(new Color(_random.Next(256), _random.Next(256), _random.Next(256)))
                .WithFooter(_footers[_random.Next(_footers.Length)]);

            var infoField = new StringBuilder();
            infoField.AppendLine($"**Abilities**: `{string.Join(", ", pokemonInfo.Abilities).ToTitleCase()}`");
            infoField.AppendLine($"**Types**: {string.Join(", ", pokemonInfo.Types.Select(GetTypeEmote))}");
            infoField.AppendLine($"**Egg Groups**: {string.Join(", ", pokemonInfo.EggGroups.Select(GetEggEmote))}");
            infoField.AppendLine($"**Weight**: `{pokemonInfo.Weight} kg`");

            var statsStr = new StringBuilder();
            statsStr.AppendLine($"{PokemonDisplayConstants.HP_DISPLAY} `{pokemonInfo.Stats.Hp}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.ATK_DISPLAY} `{pokemonInfo.Stats.Attack}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.DEF_DISPLAY} `{pokemonInfo.Stats.Defense}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.SPATK_DISPLAY} `{pokemonInfo.Stats.SpecialAttack}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.SPDEF_DISPLAY} `{pokemonInfo.Stats.SpecialDefense}`");
            statsStr.AppendLine($"{PokemonDisplayConstants.SPE_DISPLAY} `{pokemonInfo.Stats.Speed}`");

            infoField.AppendLine("\n**Stats**");
            infoField.Append(statsStr);

            embed.AddField("Pokemon Information", infoField.ToString());
            if (!string.IsNullOrEmpty(imageUrl)) embed.WithImageUrl(imageUrl);

            // Create and add the More Information button
            var components = new ComponentBuilder()
                .WithButton("More Information", $"pokeinfo:more,{pokemonInfo.Id}", ButtonStyle.Primary, new Emoji("ℹ️"))
                .Build();

            await ctx.Interaction.RespondAsync(embed: embed.Build(), components: components);
        }
        catch (Exception ex)
        {
            await ctx.Interaction.SendErrorAsync("That Pokemon does not exist!");
            Log.Error(ex, "Error in info command");
        }
    }

    /// <summary>
    ///     Displays the user's Pokedex progress.
    ///     Shows which Pokemon the user has caught and which ones they still need to find.
    ///     Different variants can be shown such as the national dex, unowned Pokemon, or specific variants like shiny or
    ///     shadow.
    /// </summary>
    /// <param name="variant">The variant type of Pokedex to display (national, unowned, shadow, shiny, radiant, skin).</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [SlashCommand("pokedex", "View your Pokedex progress")]
    public async Task Pokedex(string variant = null)
    {
        await DeferAsync();
        if (string.IsNullOrEmpty(variant))
        {
            await ShowNationalPokedex();
            return;
        }

        switch (variant.ToLower())
        {
            case "national":
                await ShowNationalPokedex();
                break;
            case "unowned":
                await ShowUnownedPokedex();
                break;
            case "shadow":
            case "shiny":
            case "radiant":
            case "skin":
                await ShowSpecialPokedex(variant);
                break;
            default:
                await ctx.Interaction.SendErrorFollowupAsync(
                    "Invalid variant selected. Available options: national, unowned, shadow, shiny, radiant, skin");
                break;
        }
    }

    /// <summary>
    ///     Displays the national Pokedex with owned Pokemon status.
    ///     Shows a paginated list of all Pokemon with indicators for which ones the user has caught.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task ShowNationalPokedex()
    {
        try
        {
            var pokemonList = await Service.GetAllPokemon();
            var ownedPokemon = await Service.GetUserPokemons(ctx.User.Id);

            var pages = new List<PageBuilder>();
            var currentPage = new StringBuilder();
            var pokemonCount = 0;

            foreach (var pokemon in pokemonList)
            {
                var isOwned = ownedPokemon.Any(p =>
                    p.PokemonName.Equals(pokemon.Identifier, StringComparison.OrdinalIgnoreCase));
                var status = isOwned ? "<:save:1337858300037042330>" : "<:delete:1337858391720202292>";
                currentPage.AppendLine($"**{pokemon.Identifier.Titleize()}** - {status}");
                pokemonCount++;

                if (pokemonCount % 20 == 0)
                {
                    pages.Add(new PageBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription(currentPage.ToString())
                        .WithTitle(
                            $"National Pokedex ({ownedPokemon.DistinctBy(x => x.PokemonName).Count()}/{pokemonList.Count})"));
                    currentPage.Clear();
                }
            }

            if (currentPage.Length > 0)
                pages.Add(new PageBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription(currentPage.ToString())
                    .WithTitle(
                        $"National Pokedex ({ownedPokemon.DistinctBy(x => x.PokemonName).Count()}/{pokemonList.Count})"));

            var paginator = new StaticPaginatorBuilder()
                .AddUser(ctx.User)
                .WithPages(pages)
                .WithDefaultEmotes()
                .WithFooter(PaginatorFooter.PageNumber)
                .Build();

            await interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(10),
                InteractionResponseType.DeferredUpdateMessage);
        }
        catch (Exception e)
        {
            Log.Error(e, "Error displaying national pokedex");
            await ctx.Interaction.SendErrorFollowupAsync("An error occurred while loading the Pokedex.");
        }
    }

    /// <summary>
    ///     Displays the Pokedex for a specific variant (shiny, radiant, shadow, skin).
    ///     Shows which Pokemon the user has caught in the specified variant form.
    /// </summary>
    /// <param name="variant">The variant type to display.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task ShowSpecialPokedex(string variant)
    {
        var pokemonList = await Service.GetAllPokemon();
        var userPokemon = await Service.GetUserPokemons(ctx.User.Id);

        var ownedPokemon = variant switch
        {
            "shiny" => userPokemon.Where(p => p.Shiny == true),
            "radiant" => userPokemon.Where(p => p.Radiant == true),
            "shadow" => userPokemon.Where(p => p.Skin == "shadow"),
            "skin" => userPokemon.Where(p => !string.IsNullOrEmpty(p.Skin)),
            _ => userPokemon
        };

        var pages = new List<PageBuilder>();
        var currentPage = new StringBuilder();
        var pokemonCount = 0;

        var variantIcon = variant switch
        {
            "shiny" => "<:starrr:1175872035927375953>",
            "radiant" => "<a:newradhmm:1061418796021194883>",
            "shadow" => "<:shadowicon4:1077328251556470925>",
            "skin" => "<:skin23:1012754684576014416>",
            _ => "✨"
        };

        foreach (var pokemon in pokemonList)
        {
            var isOwned = ownedPokemon.Any(p =>
                p.PokemonName.Equals(pokemon.Identifier, StringComparison.OrdinalIgnoreCase));
            var status = isOwned ? variantIcon : "<:delete:1051241645447848009>";
            currentPage.AppendLine($"**{pokemon.Identifier.Titleize()}** - {status}");
            pokemonCount++;

            if (pokemonCount % 20 == 0)
            {
                pages.Add(new PageBuilder()
                    .WithColor(Color.Blue)
                    .WithDescription(currentPage.ToString())
                    .WithTitle($"{variant.Titleize()} Pokedex ({ownedPokemon.Count()}/{pokemonList.Count})"));
                currentPage.Clear();
            }
        }

        if (currentPage.Length > 0)
            pages.Add(new PageBuilder()
                .WithColor(Color.Blue)
                .WithDescription(currentPage.ToString())
                .WithTitle($"{variant.Titleize()} Pokedex ({ownedPokemon.Count()}/{pokemonList.Count})"));

        var paginator = new StaticPaginatorBuilder()
            .AddUser(ctx.User)
            .WithPages(pages)
            .WithDefaultEmotes()
            .WithFooter(PaginatorFooter.PageNumber)
            .Build();

        await interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(10));
    }

    /// <summary>
    ///     Permanently releases a Pokemon.
    ///     Removes the Pokemon from the user's collection after confirmation.
    /// </summary>
    /// <param name="pokemonNumber">The number of the Pokemon to release.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [SlashCommand("release", "Release a pokemon permanently")]
    public async Task Release(int pokemonNumber)
    {
        try
        {
            var pokemon = await Service.GetPokemonByNumber(ctx.User.Id, pokemonNumber);
            if (pokemon == null)
            {
                await ctx.Interaction.SendErrorAsync("That pokemon does not exist!");
                return;
            }

            var ivPercentage = (pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                                pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv) / 186.0 * 100;

            var confirmEmbed = new EmbedBuilder()
                .WithTitle("Confirm Release")
                .WithDescription($"Are you sure you want to release your {pokemon.PokemonName} ({ivPercentage:F2}%)?")
                .WithColor(Color.Red)
                .WithFooter("This action cannot be undone!");

            var result = await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id);

            if (!result)
            {
                await ctx.Interaction.SendErrorAsync("Release cancelled.");
                return;
            }

            await Service.RemoveUserPokemon(ctx.User.Id, pokemon.Id, true);
            await ctx.Interaction.SendConfirmAsync($"You have successfully released your {pokemon.PokemonName}!");
        }
        catch (Exception ex)
        {
            await ctx.Interaction.SendErrorAsync(ex.Message);
        }
    }

    /// <summary>
    ///     Sacrifices a Pokemon to fill the user's soul-gauge.
    ///     Higher IV Pokemon provide more soul-gauge points. Requires confirmation before proceeding.
    ///     When the soul-gauge is full, users can use the /meditate command for rewards.
    /// </summary>
    /// <param name="pokemonNumber">The number of the Pokemon to sacrifice.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [SlashCommand("sacrifice", "Sacrifice a pokemon to fill your soul-gauge")]
    public async Task Sacrifice(int pokemonNumber)
    {
        try
        {
            var pokemon = await Service.GetPokemonByNumber(ctx.User.Id, pokemonNumber);
            if (pokemon == null)
            {
                await ctx.Interaction.SendErrorAsync("That pokemon does not exist!");
                return;
            }

            var ivPercentage = (pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                                pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv) / 186.0 * 100;

            var soulIncrement = ivPercentage switch
            {
                <= 49.99 => _random.NextDouble() * (0.03 - 0.01) + 0.01,
                <= 59.99 => _random.NextDouble() * (0.25 - 0.1) + 0.1,
                <= 64.99 => _random.NextDouble() * (0.25 - 0.15) + 0.15,
                <= 69.99 => _random.NextDouble() * (0.3 - 0.2) + 0.2,
                <= 74.99 => _random.NextDouble() * (0.4 - 0.35) + 0.35,
                <= 79.99 => _random.NextDouble() * (0.5 - 0.35) + 0.35,
                <= 84.99 => _random.NextDouble() * (2.0 - 0.9) + 0.9,
                <= 86.99 => _random.NextDouble() * (5.0 - 0.9) + 0.9,
                <= 88.99 => _random.NextDouble() * (10.0 - 5.0) + 5.0,
                <= 91.99 => _random.NextDouble() * (20.0 - 10.0) + 10.0,
                <= 92.99 => _random.NextDouble() * (40.0 - 10.0) + 10.0,
                <= 93.99 => _random.NextDouble() * (60.0 - 40.0) + 40.0,
                <= 95.99 => _random.NextDouble() * (100.0 - 60.0) + 60.0,
                <= 97.99 => _random.NextDouble() * (150.0 - 60.0) + 60.0,
                <= 100.0 => _random.NextDouble() * (175.0 - 150.0) + 150.0,
                _ => 0
            };

            var confirmEmbed = new EmbedBuilder()
                .WithTitle("Confirm Sacrifice")
                .WithDescription($"Are you sure you want to sacrifice your {pokemon.PokemonName} ({ivPercentage:F2}%)?")
                .WithColor(Color.Red)
                .WithFooter("This action cannot be undone!");

            var result = await PromptUserConfirmAsync(confirmEmbed, ctx.User.Id);

            if (!result)
            {
                await ctx.Interaction.SendErrorAsync("Sacrifice cancelled.");
                return;
            }

            await Service.RemoveUserPokemon(ctx.User.Id, pokemon.Id);
            await Service.IncrementSoulGauge(ctx.User.Id, soulIncrement);

            var currentGauge = await Service.GetUserSoulGauge(ctx.User.Id);
            var gaugeDisplay = currentGauge >= 1000
                ? string.Concat(Enumerable.Repeat("<:beambar2:1181749951844319293>", 10))
                : string.Concat(Enumerable.Repeat("<:beambar2:1181749951844319293>", currentGauge / 100)) +
                  "<:beamstart2:1181749967304536134>";

            var responseEmbed = new EmbedBuilder()
                .WithTitle("Pokemon Sacrificed")
                .WithDescription($"You sacrificed your {pokemon.PokemonName} ({ivPercentage:F2}%)\n" +
                                 $"Soul Gauge increased by {soulIncrement:F2}\n" +
                                 $"Current Gauge: {gaugeDisplay}")
                .WithColor(Color.Purple);

            if (currentGauge >= 1000)
                responseEmbed.AddField("Soul Gauge Full!",
                    "You must empty your gauge by communicating with the Ancient Pokemon to collect a reward and to continue collecting souls.\n" +
                    "Use `/meditate` to try to communicate with the Ancient Pokemons spirit.");

            await ctx.Interaction.RespondAsync(embed: responseEmbed.Build());
        }
        catch (Exception ex)
        {
            await ctx.Interaction.SendErrorAsync(ex.Message);
        }
    }

    /// <summary>
    ///     Handles interactions with the Pokemon info buttons.
    ///     Processes different actions like showing more information, going back to the main view, or closing the info panel.
    /// </summary>
    /// <param name="action">The action to perform (more, back, close).</param>
    /// <param name="param">The parameter for the action, typically a Pokemon ID.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [ComponentInteraction("pokeinfo:*,*", true)]
    public async Task HandleInfoButtons(string action, string param)
    {
        var interaction = ctx.Interaction as IComponentInteraction;

        switch (action)
        {
            case "more":
                var pokemonId = ulong.Parse(param);
                var pokemon = pokemonId != 0 ? await Service.GetPokemonById(pokemonId) : null;
                var pokemonName = pokemon?.PokemonName ?? interaction.Message.Embeds.First().Title.Split(' ').Last();

                var forms = await Service.GetPokemonForms(pokemonName);
                var evolutionLine = await Service.GetEvolutionLine(pokemonName);

                var moreInfoEmbed = new EmbedBuilder()
                    .WithColor(0xFF0060);

                // Add forms information
                if (forms == null || !forms.Any() || forms.Contains("None"))
                    moreInfoEmbed.AddField("Available Forms:", "`This Pokemon has no forms.`");
                else
                    moreInfoEmbed.AddField("Available Forms:",
                        $"{string.Join("\n", forms)}\n`/form (form name)`\nor `/mega evolve`");

                // Add evolution line
                if (!string.IsNullOrEmpty(evolutionLine))
                    moreInfoEmbed.AddField("More Info:", $"**Evolution Line**:\n{evolutionLine}");

                var components = new ComponentBuilder()
                    .WithButton("Back", $"pokeinfo:back,{param}")
                    .WithButton("Close", $"pokeinfo:close,{param}", ButtonStyle.Danger, new Emoji("➖"))
                    .Build();

                await interaction.UpdateAsync(msg =>
                {
                    msg.Embed = moreInfoEmbed.Build();
                    msg.Components = components;
                });
                break;

            case "back":
                // Get the original embed from the message
                var originalEmbed = interaction.Message.Embeds.First();
                var backComponents = new ComponentBuilder()
                    .WithButton("More Information", $"pokeinfo:more,{param}", ButtonStyle.Primary, new Emoji("ℹ️"))
                    .Build();

                await interaction.UpdateAsync(msg =>
                {
                    msg.Embed = originalEmbed as Embed;
                    msg.Components = backComponents;
                });
                break;

            case "close":
                await interaction.UpdateAsync(msg =>
                {
                    msg.Content = "Info closed.";
                    msg.Embed = null;
                    msg.Components = null;
                });
                break;
        }
    }

    /// <summary>
    ///     Displays the unowned Pokemon in the Pokedex.
    ///     Shows a paginated list of Pokemon that the user has not yet caught.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task ShowUnownedPokedex()
    {
        var pokemonList = await Service.GetAllPokemon();
        var ownedPokemon = await Service.GetUserPokemons(ctx.User.Id);
        var desc = new StringBuilder();
        var pages = new List<PageBuilder>();
        var pokemonCount = 0;

        foreach (var pokemon in pokemonList)
        {
            // Only add if the pokemon is NOT owned
            var isOwned = ownedPokemon.Any(p =>
                p.PokemonName.Equals(pokemon.Identifier, StringComparison.CurrentCultureIgnoreCase));
            if (!isOwned)
            {
                desc.AppendLine($"**{pokemon.Identifier.Titleize()}** - <:delete:1051241645447848009>");
                pokemonCount++;

                if (pokemonCount % 20 == 0)
                {
                    pages.Add(new PageBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription(desc.ToString())
                        .WithTitle($"Unowned Pokemon ({ownedPokemon.Count}/{pokemonList.Count})"));
                    desc.Clear();
                }
            }
        }

        if (desc.Length > 0)
            pages.Add(new PageBuilder()
                .WithColor(Color.Blue)
                .WithDescription(desc.ToString())
                .WithTitle($"Unowned Pokemon ({ownedPokemon.Count}/{pokemonList.Count})"));

        var paginator = new StaticPaginatorBuilder()
            .AddUser(ctx.User)
            .WithPages(pages)
            .WithDefaultEmotes()
            .WithFooter(PaginatorFooter.PageNumber)
            .Build();

        await interactivity.SendPaginatorAsync(paginator, ctx.Interaction, TimeSpan.FromMinutes(10));
    }

    /// <summary>
    ///     Gets the emoji for a Pokemon type.
    ///     Returns a custom Discord emoji for the specified type.
    /// </summary>
    /// <param name="type">The type of the Pokemon.</param>
    /// <returns>The emoji representation of the type.</returns>
    private static string GetTypeEmote(string type)
    {
        return type.ToLower() switch
        {
            "normal" => "<:normal:1061418793416294460>",
            "fire" => "<:fire:1061418789725798430>",
            "water" => "<:water:1061418798549037167>",
            _ => type
        };
    }

    /// <summary>
    ///     Gets the emoji for an egg group.
    ///     Returns a custom Discord emoji for the specified egg group.
    /// </summary>
    /// <param name="eggGroup">The egg group name.</param>
    /// <returns>The emoji representation of the egg group.</returns>
    private static string GetEggEmote(string eggGroup)
    {
        return eggGroup.ToLower() switch
        {
            "monster" => "<:monster:1061418792191606804>",
            "water1" => "<:water1:1061418796750913586>",
            "bug" => "<:bug:1061418785808072815>",
            _ => eggGroup
        };
    }

    /// <summary>
    ///     Formats the title of a Pokemon with its variant indicators.
    ///     Adds prefixes like ✨ for shiny, 🌟 for radiant, etc.
    /// </summary>
    /// <param name="name">The name of the Pokemon.</param>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <param name="skin">The skin of the Pokemon, if any.</param>
    /// <returns>The formatted title of the Pokemon.</returns>
    private static string GetPokemonTitle(string name, bool? shiny, bool? radiant, string skin)
    {
        if (shiny == true) name = "✨ " + name;
        if (radiant == true) name = "🌟 " + name;
        if (!string.IsNullOrEmpty(skin)) name = "💫 " + name;
        return name;
    }

    /// <summary>
    ///     Displays detailed information about an owned Pokemon.
    ///     Creates and sends an embed with comprehensive stats, moves, and other details about the Pokemon.
    /// </summary>
    /// <param name="pokemon">The Pokemon to display information about.</param>
    /// <param name="pokeCount">The total number of Pokemon the user has.</param>
    /// <param name="selectedPoke">The index of the selected Pokemon.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task DisplayOwnedPokemonInfo(Database.Models.PostgreSQL.Pokemon.Pokemon? pokemon, int pokeCount,
        int selectedPoke)
    {
        var pokemonInfo = await Service.GetPokemonInfo(pokemon.PokemonName);
        if (pokemonInfo == null)
        {
            await ctx.Interaction.SendErrorAsync("Error retrieving pokemon information.");
            return;
        }

        // Calculate stats
        var calculatedStats = await Service.CalculatePokemonStats(pokemon, pokemonInfo.Stats);
        var friendship = Service.CalculateFriendship(pokemon);

        // Calculate IV percentage
        var ivTotal = pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv +
                      pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv;
        var ivPercentage = ivTotal / 186.0 * 100;

        var genderEmoji = GetGenderEmoji(pokemon.Gender);

        // Get proper title prefix based on skin
        var titlePrefix = pokemon.Skin switch
        {
            "staff_gif" => "<:staff2:1012753310916296786> ",
            var s when s?.StartsWith("custom/") == true => "<a:custom:1012757910222274560> ",
            _ => ""
        };

        // Get movement bars if it's a special pokemon
        var specEmojis = !string.IsNullOrEmpty(pokemon.Skin)
            ? "<a:spec1:1036851754303770734><a:spec2:1036851753058062376><a:spec3:1036851751023812628>\n"
            : "";

        // Create the embed with enhanced title
        var embed = new EmbedBuilder()
            .WithTitle(
                $"{titlePrefix}{GetPokemonEmoji(pokemon.Shiny.GetValueOrDefault(), pokemon.Radiant.GetValueOrDefault(), pokemon.Skin)}{genderEmoji}{pokemon.PokemonName.Titleize()}")
            .WithColor(new Color(_random.Next(256), _random.Next(256), _random.Next(256)))
            .WithFooter($"Number {selectedPoke}/{pokeCount} | Global ID#: {pokemon.Id}");

        // Basic information section
        var infoField = new StringBuilder();
        infoField.AppendLine($"**Level**: `{pokemon.Level}`");

        // Add nickname if it exists and differs from Pokemon name
        if (!string.IsNullOrEmpty(pokemon.Nickname) && pokemon.Nickname != pokemon.PokemonName)
            infoField.AppendLine($"**Nickname**: `{pokemon.Nickname}`");

        if (!string.IsNullOrEmpty(pokemon.HeldItem) && pokemon.HeldItem.ToLower() != "none")
            infoField.AppendLine($"**Held Item**: `{pokemon.HeldItem}`");

        // Get and display the ability name
        var abilityName = await Service.GetAbilityName(pokemon.PokemonName, pokemon.AbilityIndex);
        infoField.AppendLine($"**Ability**: `{abilityName}`");

        // Display types
        infoField.AppendLine($"**Types**: {string.Join(", ", pokemonInfo.Types.Select(GetTypeEmote))}");
        infoField.AppendLine($"**Nature**: `{pokemon.Nature}`");

        // Friendship only if non-zero
        if (friendship > 0)
            infoField.AppendLine($"**Friendship**: `{friendship}`");

        // Add original trainer if available
        if (pokemon.CaughtBy.HasValue && pokemon.CaughtBy.Value > 0)
        {
            var trainerName = await Service.GetTrainerName(pokemon.CaughtBy.Value);
            infoField.AppendLine($"**Original Trainer**: `{trainerName}`");
        }

        // Add catch date if available
        if (pokemon.Timestamp.HasValue)
            infoField.AppendLine($"**Caught on**: `{pokemon.Timestamp.Value:MMM d, yyyy}`");

        // Add special status indicators
        var statusFlags = new List<string>();
        if (pokemon.Champion) statusFlags.Add("Champion");
        if (pokemon.Voucher.GetValueOrDefault()) statusFlags.Add("Voucher");
        if (!pokemon.Tradable) statusFlags.Add("Not Tradable");
        if (!pokemon.Breedable) statusFlags.Add("Not Breedable");

        if (statusFlags.Any())
            infoField.AppendLine($"**Status**: `{string.Join(", ", statusFlags)}`");

        // Add tags if any exist
        if (pokemon.Tags != null && pokemon.Tags.Length > 0 && !pokemon.Tags.All(string.IsNullOrEmpty))
            infoField.AppendLine($"**Tags**: `{string.Join(", ", pokemon.Tags.Where(t => !string.IsNullOrEmpty(t)))}`");

        // Add special indicator animations if needed
        infoField.AppendLine(specEmojis);

        embed.AddField("Pokemon Information", infoField.ToString());

        // Show the Pokemon's moves in a separate field
        if (pokemon.Moves != null && pokemon.Moves.Length > 0 && !pokemon.Moves.All(string.IsNullOrEmpty))
        {
            var movesField = new StringBuilder();
            foreach (var move in pokemon.Moves.Where(m => !string.IsNullOrEmpty(m)))
                movesField.AppendLine($"• `{move.Titleize()}`");

            if (movesField.Length > 0)
                embed.AddField("Moves", movesField.ToString());
            else
                embed.AddField("Moves", "No moves learned yet.");
        }
        else
        {
            embed.AddField("Moves", "No moves learned yet.");
        }

        // Stats field showing IVs, EVs, and total stats
        var statsField = new StringBuilder();
        statsField.AppendLine(
            $"{PokemonDisplayConstants.HP_DISPLAY} `{pokemon.HpIv}/31` | `{pokemon.HpEv}/252` | `{calculatedStats.MaxHp}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.ATK_DISPLAY} `{pokemon.AttackIv}/31` | `{pokemon.AttackEv}/252` | `{calculatedStats.Attack}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.DEF_DISPLAY} `{pokemon.DefenseIv}/31` | `{pokemon.DefenseEv}/252` | `{calculatedStats.Defense}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.SPATK_DISPLAY} `{pokemon.SpecialAttackIv}/31` | `{pokemon.SpecialAttackEv}/252` | `{calculatedStats.SpecialAttack}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.SPDEF_DISPLAY} `{pokemon.SpecialDefenseIv}/31` | `{pokemon.SpecialDefenseEv}/252` | `{calculatedStats.SpecialDefense}`");
        statsField.AppendLine(
            $"{PokemonDisplayConstants.SPE_DISPLAY} `{pokemon.SpeedIv}/31` | `{pokemon.SpeedEv}/252` | `{calculatedStats.Speed}`");

        embed.AddField($"Stats (IV | EV | Total) - {ivPercentage:F2}% IV", statsField.ToString());

        // Experience and Experience Cap if applicable
        if (pokemon.Level < 100)
            embed.AddField("Experience",
                $"`{pokemon.Experience:N0}` / `{pokemon.ExperienceCap:N0}` " +
                $"(`{(double)pokemon.Experience / pokemon.ExperienceCap * 100:F1}%` to next level)");

        // Get image URL
        var (form, imageUrl) = await Service.GetPokemonFormInfo(pokemon.PokemonName, pokemon.Shiny ?? false,
            pokemon.Radiant ?? false, pokemon.Skin);
        if (!string.IsNullOrEmpty(imageUrl)) embed.WithImageUrl(imageUrl);

        // Add usage statistics if counter exists
        if (pokemon.Counter.HasValue && pokemon.Counter.Value > 0)
            embed.AddField("Usage Stats", $"This Pokemon has been used `{pokemon.Counter.Value}` times.");

        var components = new ComponentBuilder()
            .WithButton("More Information", $"pokeinfo:more,{pokemon.Id}", ButtonStyle.Primary, new Emoji("ℹ️"))
            .Build();

        await ctx.Interaction.RespondAsync(embed: embed.Build(), components: components);
    }

    /// <summary>
    ///     Gets the emoji for a Pokemon based on its variant.
    ///     Returns appropriate emoji for shiny, radiant, shadow, or other special forms.
    /// </summary>
    /// <param name="shiny">Whether the Pokemon is shiny.</param>
    /// <param name="radiant">Whether the Pokemon is radiant.</param>
    /// <param name="skin">The skin of the Pokemon, if any.</param>
    /// <returns>The emoji representation of the Pokemon's variant.</returns>
    private string GetPokemonEmoji(bool shiny, bool radiant, string skin)
    {
        if (radiant) return "<:radiant:1057764536456966275>";
        if (shiny) return "<a:shiny:1057764628349853786>";
        return skin switch
        {
            "glitch" => "<:glitch:1057764553091534859>",
            "shadow" => "<:shadow:1057764584954568775>",
            _ => "<:blank:1338358271706136648>"
        };
    }

    /// <summary>
    ///     Gets the emoji for a Pokemon's gender.
    ///     Returns ♂️ for male, ♀️ for female, or empty string for genderless.
    /// </summary>
    /// <param name="gender">The gender of the Pokemon.</param>
    /// <returns>The emoji representation of the Pokemon's gender.</returns>
    private string GetGenderEmoji(string gender)
    {
        return gender?.ToLower() switch
        {
            "-m" or "male" => "♂️ ",
            "-f" or "female" => "♀️ ",
            _ => ""
        };
    }
}