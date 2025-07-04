using System.Text;
using Discord.Interactions;
using EeveeCore.Common.Attributes.Interactions;
using EeveeCore.Common.AutoCompletes;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Modules.Market.Services;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace EeveeCore.Modules.Market;

/// <summary>
///     Provides Discord slash commands for Market-related functionality.
///     Includes commands for buying, selling, and managing Pokemon on the market.
/// </summary>
/// <param name="interactivity">Service for handling interactive components like pagination.</param>
/// <param name="tradeLockService">The trade lock service.</param>
[Group("market", "Pokemon market commands")]
public class MarketSlashCommands(InteractiveService interactivity, ITradeLockService tradeLockService) 
    : EeveeCoreSlashModuleBase<MarketService>
{

    /// <summary>
    ///     Adds a Pokemon to the market for sale.
    /// </summary>
    /// <param name="pokemonPosition">The position of the Pokemon in your collection.</param>
    /// <param name="price">The price to list the Pokemon for (in coins).</param>
    [SlashCommand("add", "Add a Pokemon to the market")]
    [TradeLock]
    public async Task AddPokemonToMarket(
        [Summary("pokemon", "The position number of the Pokemon in your collection")]
        [Autocomplete(typeof(AllPokemonAutocompleteHandler))] int pokemonPosition,
        [Summary("price", "The price to list the Pokemon for (in coins)")] int price)
    {
        await DeferAsync();

        var success = await tradeLockService.ExecuteWithTradeLockAsync(ctx.User, async () =>
        {
            var result = await Service.AddPokemonToMarketAsync(ctx.User.Id, (ulong)pokemonPosition, price);
            
            if (result.Success)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("🏪 Pokemon Listed on Market")
                    .WithDescription(result.Message)
                    .WithColor(Color.Green)
                    .WithFooter($"Listing ID: {result.Data}")
                    .Build();
                    
                await ctx.Interaction.FollowupAsync(embed: embed);
            }
            else
            {
                await ctx.Interaction.SendEphemeralFollowupErrorAsync(result.Message);
            }
        });

        if (!success)
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync($"{ctx.User.Username} is currently in a trade!");
        }
    }

    /// <summary>
    ///     Buys a Pokemon from the market.
    /// </summary>
    /// <param name="listingId">The market listing ID of the Pokemon to buy.</param>
    [SlashCommand("buy", "Buy a Pokemon from the market")]
    [TradeLock]
    public async Task BuyPokemonFromMarket(
        [Summary("listing_id", "The market listing ID of the Pokemon to buy")] ulong listingId)
    {
        var success = await tradeLockService.ExecuteWithTradeLockAsync(ctx.User, async () =>
        {
            var lockSuccess = await Service.ExecuteWithMarketLockAsync(listingId, async () =>
            {
                // Show confirmation first
                var listing = await Service.GetPokemonAsync(listingId);
                if (listing == null)
                {
                    await ctx.Interaction.RespondAsync("That listing does not exist or has already ended.", ephemeral: true);
                    return;
                }

                var components = new ComponentBuilder()
                    .WithButton("Confirm Purchase", $"market_buy_confirm:{listingId}", ButtonStyle.Success, new Emoji("✅"))
                    .WithButton("Cancel", $"market_buy_cancel:{listingId}", ButtonStyle.Danger, new Emoji("❌"));

                var embed = new EmbedBuilder()
                    .WithTitle("Confirm Purchase")
                    .WithDescription($"Are you sure you want to buy a level {listing.Level} {listing.PokemonName} for {listing.Price} credits?")
                    .WithColor(Color.Orange)
                    .Build();

                await ctx.Interaction.RespondAsync(embed: embed, components: components.Build());
            });

            if (!lockSuccess)
            {
                await ctx.Interaction.RespondAsync("Someone is already in the process of buying that pokemon. You can try again later.", ephemeral: true);
            }
        });

        if (!success)
        {
            await ctx.Interaction.RespondAsync($"{ctx.User.Username} is currently in a trade!", ephemeral: true);
        }
    }

    /// <summary>
    ///     Removes a Pokemon from the market.
    /// </summary>
    /// <param name="listingId">The market listing ID of the Pokemon to remove.</param>
    [SlashCommand("remove", "Remove your Pokemon from the market")]
    public async Task RemovePokemonFromMarket(
        [Summary("listing_id", "The market listing ID of the Pokemon to remove")]
        [Autocomplete(typeof(MarketListingsAutocompleteHandler))] ulong listingId)
    {
        await DeferAsync();

        var result = await Service.RemovePokemonFromMarketAsync(ctx.User.Id, listingId);
        
        if (result.Success)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏪 Pokemon Removed from Market")
                .WithDescription(result.Message)
                .WithColor(Color.Orange)
                .Build();
                
            await ctx.Interaction.FollowupAsync(embed: embed);
        }
        else
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync(result.Message);
        }
    }

    /// <summary>
    ///     Views information about a Pokemon listing on the market.
    /// </summary>
    /// <param name="listingId">The market listing ID to view information for.</param>
    [SlashCommand("info", "View information about a Pokemon listing")]
    public async Task ViewMarketListing(
        [Summary("listing_id", "The market listing ID to view information for")] ulong listingId)
    {
        await DeferAsync();

        var pokemon = await Service.GetPokemonAsync(listingId);
        
        if (pokemon == null)
        {
            await ctx.Interaction.SendEphemeralFollowupErrorAsync("That listing does not exist or has already ended.");
            return;
        }

        // Create a basic Pokemon info embed
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

        await ctx.Interaction.FollowupAsync(embed: embed);
    }

    /// <summary>
    ///     Shows all Pokemon currently available on the market.
    /// </summary>
    /// <param name="sortBy">How to sort the market listings.</param>
    /// <param name="filter">Filter Pokemon by type or rarity.</param>
    /// <param name="search">Search for specific Pokemon by name.</param>
    [SlashCommand("list", "Browse all Pokemon currently for sale on the market")]
    public async Task ListMarketPokemon(
        [Summary("sort", "Sort method")]
        [Choice("Price (Low to High)", "price_asc")]
        [Choice("Price (High to Low)", "price_desc")]
        [Choice("Level (High to Low)", "level_desc")]
        [Choice("Level (Low to High)", "level_asc")]
        [Choice("IV Total (High to Low)", "iv_desc")]
        [Choice("Recently Listed", "recent")]
        [Choice("Pokemon Name", "name")]
        string sortBy = "recent",
        [Summary("filter", "Filter Pokemon")]
        [Choice("All", "all")]
        [Choice("Shiny", "shiny")]
        [Choice("Radiant", "radiant")]
        [Choice("Shadow", "shadow")]
        [Choice("Legendary", "legendary")]
        string filter = "all",
        [Summary("search", "Search for specific Pokemon")]
        string? search = null)
    {
        await DeferAsync();

        var result = await Service.GetMarketListingsAsync(sortBy, filter, search);
        
        if (result.Listings.Count == 0)
        {
            var message = result.HasFilters 
                ? "No Pokemon are currently listed on the market that match your criteria."
                : "The market is currently empty.";
                
            await ctx.Interaction.SendEphemeralFollowupErrorAsync(message);
            return;
        }

        // Create paginated market listing
        var pages = new List<PageBuilder>();
        var itemsPerPage = 10;
        var totalPages = (result.Listings.Count - 1) / itemsPerPage + 1;

        for (var i = 0; i < totalPages; i++)
        {
            var pageItems = result.Listings
                .Skip(i * itemsPerPage)
                .Take(itemsPerPage);

            var description = new StringBuilder();
            description.AppendLine("**Currently Available Pokemon:**\n");
            
            foreach (var listing in pageItems)
            {
                var emoji = GetMarketPokemonEmoji(listing.Shiny, listing.Radiant, listing.Skin);
                var genderEmoji = GetGenderEmoji(listing.Gender);
                var ivTotal = listing.HpIv + listing.AttackIv + listing.DefenseIv + 
                             listing.SpecialAttackIv + listing.SpecialDefenseIv + listing.SpeedIv;
                
                description.AppendLine($"{emoji} **{listing.PokemonName}** {genderEmoji} • Lv.{listing.Level} • {ivTotal}/186 IV");
                description.AppendLine($"💰 **{listing.Price:N0}** coins • ID: `{listing.ListingId}`");
                description.AppendLine($"Use `/market info {listing.ListingId}` for details\n");
            }

            var page = new PageBuilder()
                .WithTitle("🏪 Pokemon Market")
                .WithColor(Color.Green)
                .WithDescription(description.ToString())
                .WithFooter($"Page {i + 1}/{totalPages} • {result.Listings.Count} Pokemon listed");

            pages.Add(page);
        }

        // Create and send paginator
        var paginator = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithUsers(ctx.User)
            .WithDefaultEmotes()
            .WithFooter(PaginatorFooter.PageNumber)
            .Build();

        await interactivity.SendPaginatorAsync(paginator, Context.Interaction, TimeSpan.FromMinutes(10),
            InteractionResponseType.DeferredUpdateMessage);
    }

    /// <summary>
    ///     Gets the appropriate emoji for market Pokemon display.
    /// </summary>
    private static string GetMarketPokemonEmoji(bool? shiny, bool? radiant, string? skin)
    {
        if (radiant == true) return "💎";
        if (shiny == true) return "✨";
        if (skin == "shadow") return "🌑";
        return "🔹";
    }

    /// <summary>
    ///     Gets the gender emoji for Pokemon display.
    /// </summary>
    private static string GetGenderEmoji(string gender)
    {
        return gender switch
        {
            "-m" => "♂️",
            "-f" => "♀️",
            _ => ""
        };
    }
}