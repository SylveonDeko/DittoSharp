using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Discord.Interactions;
using EeveeCore.Common.AutoCompletes;
using EeveeCore.Common.ModuleBases;
using EeveeCore.Database.Linq.Models.Pokemon;
using EeveeCore.Modules.Extras.Services;
using EeveeCore.Modules.Pokemon.Services;
using EeveeCore.Services.Impl;
using LinqToDB;
using MongoDB.Driver;
using Serilog;

namespace EeveeCore.Modules.Extras;

/// <summary>
///     Main module for Extras commands.
/// </summary>
public class ExtrasModule : EeveeCoreSlashModuleBase<ExtrasService>
{
    /// <summary>
    ///     Region enumeration for the region command.
    /// </summary>
    public enum Regions
    {
        /// <summary>
        ///     Original Kanto region.
        /// </summary>
        Original = 1,

        /// <summary>
        ///     Alola region.
        /// </summary>
        Alola = 2,

        /// <summary>
        ///     Galar region.
        /// </summary>
        Galar = 3,

        /// <summary>
        ///     Hisui region.
        /// </summary>
        Hisui = 4,

        /// <summary>
        ///     Paldea region.
        /// </summary>
        Paldea = 5
    }

    /// <summary>
    ///     Enum representing the different fishing rod types.
    /// </summary>
    public enum Rods
    {
        /// <summary>Old fishing rod.</summary>
        Old = 1,

        /// <summary>New fishing rod.</summary>
        New = 2,

        /// <summary>Good fishing rod.</summary>
        Good = 3,

        /// <summary>Super fishing rod.</summary>
        Super = 4,

        /// <summary>Ultra fishing rod.</summary>
        Ultra = 5,

        /// <summary>Supreme fishing rod.</summary>
        Supreme = 6,

        /// <summary>Epic fishing rod.</summary>
        Epic = 7,

        /// <summary>Master fishing rod.</summary>
        Master = 8
    }

    private static readonly HashSet<ulong> Bitches = [];
    private static readonly HashSet<ulong> MegaBitches = [334155028170407949];

    private static readonly string[] ChkPhrases =
    [
        "Tsk tsk, hands off!",
        "So entitled, stop pushing buttons that are not yours.",
        "That button shouldn't be pushed!",
        "Don't push buttons you don't own.",
        "It's not your responsibility to push buttons that don't belong to you.",
        "You shouldn't be pushing buttons that belong to someone else.",
        "I'd like to ask you to stop pushing buttons that aren't yours.",
        "Keep your hands off other people's buttons.",
        "It's time for you to stop pushing buttons that aren't yours.",
        "That's off limits!",
        "Leave it alone!",
        "You are not the right person for that.",
        "That's not yours!",
        "You can't press that!",
        "Don't you remember your mother teaching you not to touch things that don't belong to you?",
        "You do not have permission to do this!",
        "🎶Can't touch this 🔨🎶"
    ];

    private static readonly Dictionary<string, uint> Elements = new()
    {
        ["normal"] = 0xA9A87A,
        ["fire"] = 0xEE7D39,
        ["water"] = 0x6A91ED,
        ["grass"] = 0x7BC856,
        ["electric"] = 0xF7CF41,
        ["ice"] = 0x9AD8D8,
        ["fighting"] = 0xBE2C2D,
        ["poison"] = 0x9E409F,
        ["ground"] = 0xDFBF6E,
        ["flying"] = 0xA891EE,
        ["psychic"] = 0xF65689,
        ["bug"] = 0xA8B831,
        ["rock"] = 0xB69F40,
        ["ghost"] = 0x705796,
        ["dragon"] = 0x6F3CF5,
        ["dark"] = 0x6E5849,
        ["steel"] = 0xB8B8CF,
        ["fairy"] = 0xF6C9E3
    };

    // List of all Pokémon
    private static readonly List<string> TotalList = [];
    private readonly LinqToDbConnectionProvider _dbContextProvider;
    private readonly GuildSettingsService _guildSettingsService;
    private readonly IMongoService _mongoService;
    private readonly PokemonService _pokemonService;
    private readonly Random _random = new();
    private bool _hidden;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExtrasModule" /> class.
    /// </summary>
    /// <param name="mongoService">The MongoDB service for database operations.</param>
    /// <param name="dbContextProvider">The database context provider.</param>
    /// <param name="guildSettingsService">The guild settings service.</param>
    /// <param name="pokemonService">The Pokémon service for evolution and other related operations.</param>
    public ExtrasModule(
        IMongoService mongoService,
        LinqToDbConnectionProvider dbContextProvider,
        GuildSettingsService guildSettingsService,
        PokemonService pokemonService)
    {
        _mongoService = mongoService;
        _dbContextProvider = dbContextProvider;
        _guildSettingsService = guildSettingsService;
        _pokemonService = pokemonService;


        InitializePokemonList();
    }

    /// <summary>
    ///     Initializes the list of all Pokémon names.
    /// </summary>
    private async void InitializePokemonList()
    {
        // This would be populated from a database or other source
        var pokemonFiles = await _mongoService.PFile.Find(_ => true).ToListAsync();
        foreach (var pokemon in pokemonFiles)
            if (!string.IsNullOrEmpty(pokemon.Identifier))
                TotalList.Add(pokemon.Identifier.Capitalize());
    }

    /// <summary>
    ///     Handles the interaction when the "Chest" button is clicked on the balance display.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("chest_button", true)]
    public async Task ChestButtonHandler()
    {
        await DeferAsync();
        await BalanceChests(ctx.User);
    }

    /// <summary>
    ///     Handles the interaction when the "Misc" button is clicked on the balance display.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("misc_button", true)]
    public async Task MiscButtonHandler()
    {
        await DeferAsync();
        await BalanceMisc(ctx.User);
    }

    /// <summary>
    ///     Handles the interaction when the "Radiant Tokens" button is clicked on the balance display.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [ComponentInteraction("tokens_button", true)]
    public async Task TokensButtonHandler()
    {
        await DeferAsync();
        await BalanceTokens(ctx.User);
    }

    /// <summary>
    ///     Sets the user's active region, which affects regional Pokémon forms.
    /// </summary>
    /// <param name="location">The region to set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("region", "Set your region, affects your pokemon's regional evolutions")]
    public async Task Region(Regions location)
    {
        await using var db = await _dbContextProvider.GetConnectionAsync();

        await db.Users.Where(u => u.UserId == ctx.User.Id)
            .Set(x => x.Region, location.ToString().ToLower())
            .UpdateAsync();

        

        await RespondAsync($"Your region has been set to **{location.ToString().Capitalize()}**.", ephemeral: true);
    }

    /// <summary>
    ///     Sets the trainer nickname for the user.
    /// </summary>
    /// <param name="name">The nickname to set.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("trainernick", "Sets your trainer nickname for use in global leaderboards and other users")]
    public async Task TrainerNick(string name)
    {
        if (name.Contains("@here") || name.Contains("@everyone") || name.Contains("http"))
        {
            await RespondAsync("Nope.");
            return;
        }

        if (name.Length > 18)
        {
            await RespondAsync("Trainer nick too long!");
            return;
        }

        if (Regex.IsMatch(name, @"^[ -~]*$") == false)
        {
            await RespondAsync("Unicode characters cannot be used in your trainer nick.");
            return;
        }

        if (name.Contains("|"))
        {
            await RespondAsync("`|` cannot be used in your trainer nick.");
            return;
        }

        await using var db = await _dbContextProvider.GetConnectionAsync();

        var userNick = await db.Users
            .Where(u => u.UserId == ctx.User.Id)
            .Select(u => u.TrainerNickname)
            .FirstOrDefaultAsync();

        if (userNick != null)
        {
            await RespondAsync("You have already set your trainer nick.");
            return;
        }

        var existingUser = await db.Users
            .Where(u => u.TrainerNickname == name)
            .Select(u => u.UserId)
            .FirstOrDefaultAsync();

        if (existingUser != 0)
        {
            await RespondAsync("That nick is already taken. Try another one.");
            return;
        }

        await db.Users.Where(u => u.UserId == ctx.User.Id)
            .Set(x => x.TrainerNickname, name)
            .UpdateAsync();

        

        await RespondAsync("Successfully Changed Trainer Nick");
    }

    /// <summary>
    ///     Sets the Pokémon the user is hunting for shadow encounters.
    /// </summary>
    /// <param name="pokemon">The Pokémon to hunt.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("hunt", "Set a Pokemon to hunt for shadow encounters")]
    public async Task Hunt(string pokemon)
    {
        pokemon = pokemon.Capitalize();

        if (!TotalList.Contains(pokemon))
        {
            await RespondAsync("You have chosen an invalid Pokemon.");
            return;
        }

        await using var db = await _dbContextProvider.GetConnectionAsync();

        var userData = await db.Users
            .Where(u => u.UserId == ctx.User.Id)
            .Select(u => new { u.Hunt, u.Chain })
            .FirstOrDefaultAsync();

        if (userData == null)
        {
            await RespondAsync("You have not started!\nStart with `/start` first.");
            return;
        }

        var hunt = userData.Hunt;
        var chain = userData.Chain;

        if (hunt == pokemon)
        {
            await RespondAsync("You are already hunting that pokemon!");
            return;
        }

        var addChain = 0;

        if (chain > 0 && !string.IsNullOrEmpty(hunt))
        {
            // This would normally use ConfirmView which we don't have available
            var confirmed = await PromptUserConfirmAsync(
                $"Are you sure you want to abandon your hunt for **{hunt}**?\nYou will lose your streak of **{chain}**.",
                ctx.User.Id
            );

            if (!confirmed) return;
        }
        else if (chain > 0 && string.IsNullOrEmpty(hunt))
        {
            addChain = 500;
            await RespondAsync("Binding loose chain to new Pokemon.");
        }

        await db.Users.Where(u => u.UserId == ctx.User.Id)
            .Set(x => x.Hunt, pokemon)
            .Set(x => x.Chain, addChain)
            .UpdateAsync();

        

        var embed = new EmbedBuilder()
            .WithTitle("Shadow Hunt")
            .WithDescription($"Successfully changed shadow hunt selection to **{pokemon}**.")
            .WithColor(new Color(0xFFB6C1));

        // This would normally call a method to get the Pokemon image
        // embed.WithImageUrl(await GetPokemonImage(pokemon, skin: "shadow"));

        await RespondAsync(embed: embed.Build());

        // Log the hunt change
        var logChannel = (IMessageChannel)await ctx.Client.GetChannelAsync(1005559143886766121);
        await logChannel.SendMessageAsync($"`{ctx.User.Id} - {hunt} @ {chain}x -> {pokemon}`");
    }

    /// <summary>
    ///     Equips a fishing rod from the user's bag.
    /// </summary>
    /// <param name="fishingRod">The type of fishing rod to equip.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("equip", "Equip a fishing pole from your bag")]
    public async Task EquipFishingRod(Rods fishingRod)
    {
        var normalizedItem = fishingRod.ToString().ToLower() + "-rod";
        string[] rods =
            ["old-rod", "new-rod", "good-rod", "super-rod", "ultra-rod", "supreme-rod", "master-rod", "epic-rod"];

        if (!rods.Contains(normalizedItem))
        {
            await RespondAsync("Not a valid Fishing rod, please try again.", ephemeral: true);
            return;
        }

        await using var db = await _dbContextProvider.GetConnectionAsync();

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == ctx.User.Id);
        if (user == null)
        {
            await RespondAsync("You have not Started!\nStart with `/start` first!", ephemeral: true);
            return;
        }

        var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Items ?? "{}") ??
                        new Dictionary<string, int>();

        if (!inventory.TryGetValue(normalizedItem, out var count) || count < 1)
        {
            await RespondAsync($"You don't have {fishingRod.ToString()} rod in your inventory.", ephemeral: true);
            return;
        }

        var fishLevel = user.FishingLevel ?? 0;

        if (normalizedItem == "supreme-rod" && fishLevel < 105)
        {
            await RespondAsync("You need to be fishing level 105 to use this item!");
            return;
        }

        if (normalizedItem == "epic-rod" && fishLevel < 150)
        {
            await RespondAsync("You need to be fishing level 150 to use this item!");
            return;
        }

        if (normalizedItem == "master-rod" && fishLevel < 200)
        {
            await RespondAsync("You need to be fishing level 200 to use this item!");
            return;
        }

        await db.Users.Where(u => u.UserId == ctx.User.Id)
            .Set(u => u.HeldItem, normalizedItem)
            .UpdateAsync();

        

        await RespondAsync($"You have successfully equipped {fishingRod.ToString()} rod.", ephemeral: true);
    }

    /// <summary>
    ///     Shows the user's balance and related information.
    /// </summary>
    /// <param name="user">The user to show the balance for, or the command user if null.</param>
    /// <param name="hidden">Whether to show the balance as an ephemeral message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("bal", "Lists credits, redeems, EV points, upvote points, and selected fishing rod")]
    public async Task Balance(IUser user = null, bool hidden = false)
    {
        user ??= ctx.User;
        _hidden = hidden;

        await using var db = await _dbContextProvider.GetConnectionAsync();

        var details = await db.Users.FirstOrDefaultAsync(u => u.UserId == user.Id);
        if (details == null)
        {
            await RespondAsync($"{user.Username} has not started!");
            return;
        }

        if (!details.Visible.GetValueOrDefault() && user.Id != ctx.User.Id)
        {
            await RespondAsync(
                $"You are not permitted to see the Trainer card of {user.Username}",
                ephemeral: hidden
            );
            return;
        }

        var voteStreak = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - details.LastVote > 36 * 60 * 60
            ? 0
            : details.VoteStreak;

        var trainerNick = details.TrainerNickname ?? user.Username;
        var region = details.Region;
        var heldItem = details.HeldItem;
        var staffRank = details.Staff;

        StringBuilder desc = new();
        desc.AppendLine($"{trainerNick}'s\n__**Balances**__");
        desc.AppendLine($"**Credits**: {details.MewCoins}");
        desc.AppendLine($"**Redeems**: {details.Redeems}");
        desc.AppendLine($"**Mystery Tokens**: {details.MysteryToken}");
        desc.AppendLine($"**[Dittopia](https://discord.gg/eeveecore) Rep.**: {details.OsRep}\n");
        desc.AppendLine($"**EV Points**: {details.EvPoints}");
        desc.AppendLine($"**Upvote Points**: {details.UpvotePoints}");
        desc.AppendLine($"**Vote Streak**: `{voteStreak}`");
        desc.AppendLine($"**Holding**: {heldItem.Capitalize().Replace("-", " ")}");
        desc.AppendLine($"**Region**: {region.Capitalize()}");

        if (details.Voucher > 0) desc.AppendLine($"\n\n**Unused Vouchers**: {details.Voucher}");

        var embed = new EmbedBuilder()
            .WithColor(new Color(0xFFB6C1))
            .WithDescription(desc.ToString());

        if (staffRank.ToLower() == "gym")
        {
            embed.WithAuthor(
                "Official Gym Leader",
                "https://cdn.discordapp.com/attachments/1004310910313181325/1038076633803931729/ezgif-4-9aa2641b3d.gif"
            );

            embed.AddField(
                "Bot Role",
                "**Dittopia Gym Leader**"
            );
        }
        else if (staffRank.ToLower() != "user")
        {
            embed.WithFooter(
                $"DittoBOT {staffRank}",
                $"attachment://di.webp"
            );

            embed.WithAuthor(
                "Official Staff Member",
                "https://cdn.discordapp.com/emojis/1075509351223144520.gif?size=80&quality=lossless"
            );

            embed.AddField(
                "Bot Staff Rank",
                staffRank
            );
        }
        else
        {
            embed.WithAuthor("Trainer Information");
        }

        // Create component buttons for the embedded response
        var components = new ComponentBuilder()
            .WithButton("Chests", "chest_button", ButtonStyle.Secondary)
            .WithButton("Misc", "misc_button", ButtonStyle.Secondary)
            .WithButton("Radiant Tokens", "tokens_button", ButtonStyle.Secondary)
            .Build();

        // Check if we need to send di.webp attachment for staff members
        if (staffRank.ToLower() != "user" && staffRank.ToLower() != "gym")
        {
            var diImagePath = Path.Combine("data", "images", "di.webp");
            if (File.Exists(diImagePath))
            {
                await using var fileStream = new FileStream(diImagePath, FileMode.Open, FileAccess.Read);
                var fileAttachment = new FileAttachment(fileStream, "di.webp");
                await RespondWithFileAsync(embed: embed.Build(), components: components, ephemeral: hidden, attachment: fileAttachment);
                return;
            }
        }
        
        await RespondAsync(embed: embed.Build(), components: components, ephemeral: hidden);
    }

    /// <summary>
    ///     Displays information about chests in the user's inventory.
    /// </summary>
    /// <param name="user">The user whose chest information to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This method is called when the user clicks the "Chests" button on the balance display.
    ///     It replaces the original message with a new embed showing chest counts.
    /// </remarks>
    public async Task BalanceChests(IUser user)
    {
        await using var db = await _dbContextProvider.GetConnectionAsync();

        var details = await db.Users.FirstOrDefaultAsync(u => u.UserId == user.Id);
        if (details == null)
        {
            await RespondAsync($"{user.Username} has not started!", ephemeral: _hidden);
            return;
        }

        if (!details.Visible.GetValueOrDefault() && user.Id != ctx.User.Id)
        {
            await RespondAsync(
                $"You are not permitted to see the Trainer card of {user.Username}",
                ephemeral: _hidden
            );
            return;
        }

        var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(details.Inventory ?? "{}") ??
                        new Dictionary<string, int>();

        var common = inventory.GetValueOrDefault("common chest", 0);
        var rare = inventory.GetValueOrDefault("rare chest", 0);
        var mythic = inventory.GetValueOrDefault("mythic chest", 0);
        var legend = inventory.GetValueOrDefault("legend chest", 0);

        var staffRank = details.Staff;
        var trainerNick = details.TrainerNickname ?? user.Username;

        StringBuilder desc = new();
        desc.AppendLine("### You have:");
        desc.AppendLine($"\n- <:legend:1212910198335737876>`Legend`:\n    - **{legend}**");
        desc.AppendLine($"\n- <:mythic:1212910137858330674>`Mythic`:\n    - **{mythic}**");
        desc.AppendLine($"\n- <:rare:1212910022137348106>`Rare`:\n    - **{rare}**");
        desc.AppendLine($"\n- <:common:1212910253524389898>`Common`:\n    - **{common}**");

        var embed = new EmbedBuilder()
            .WithColor(new Color(0xFFB6C1))
            .WithDescription(desc.ToString());

        if (staffRank.ToLower() != "user")
        {
            embed.WithFooter(
                $"DittoBOT {staffRank}",
                $"attachment://di.webp"
            );

            embed.WithAuthor(
                $"{trainerNick}'s Chests",
                "https://cdn.discordapp.com/emojis/1075509351223144520.gif?size=80&quality=lossless"
            );
        }
        else
        {
            embed.WithAuthor($"{trainerNick}'s Chests");
        }

        // Recreate the component buttons to keep them available
        var components = new ComponentBuilder()
            .WithButton("Chests", "chest_button", ButtonStyle.Secondary)
            .WithButton("Misc", "misc_button", ButtonStyle.Secondary)
            .WithButton("Radiant Tokens", "tokens_button", ButtonStyle.Secondary)
            .Build();

        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed.Build();
            msg.Components = components;
        });
    }

    /// <summary>
    ///     Displays information about radiant tokens in the user's inventory.
    /// </summary>
    /// <param name="user">The user whose token information to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This method is called when the user clicks the "Radiant Tokens" button on the balance display.
    ///     It replaces the original message with a new embed showing token counts.
    /// </remarks>
    public async Task BalanceTokens(IUser user)
    {
        await using var db = await _dbContextProvider.GetConnectionAsync();

        var details = await db.Users.FirstOrDefaultAsync(u => u.UserId == user.Id);
        if (details == null)
        {
            await RespondAsync($"{user.Username} has not started!", ephemeral: _hidden);
            return;
        }

        if (!details.Visible.GetValueOrDefault() && user.Id != ctx.User.Id)
        {
            await RespondAsync(
                $"You are not permitted to see the token counts of {user.Username}",
                ephemeral: _hidden
            );
            return;
        }

        var tokenData = details.Tokens;

        if (string.IsNullOrEmpty(tokenData))
        {
            await RespondAsync($"{user.Username} has no tokens.", ephemeral: _hidden);
            return;
        }

        var tokens = JsonSerializer.Deserialize<Dictionary<string, int>>(tokenData ?? "{}") ??
                     new Dictionary<string, int>();

        StringBuilder desc = new();
        desc.AppendLine("### You have:");

        var hasTokens = false;
        foreach (var (typeName, count) in tokens)
            if (count > 0)
            {
                desc.AppendLine($"- {typeName}: **{count}**");
                hasTokens = true;
            }

        if (!hasTokens) desc = new StringBuilder($"{user.Username} has no tokens.");

        var embed = new EmbedBuilder()
            .WithColor(new Color(0xFFB6C1))
            .WithDescription(desc.ToString());

        var trainerNick = details.TrainerNickname ?? user.Username;
        var staffRank = details.Staff;

        if (staffRank.ToLower() != "user")
        {
            embed.WithFooter(
                $"DittoBOT {staffRank}",
                $"attachment://di.webp"
            );

            embed.WithAuthor(
                $"{trainerNick}'s Tokens",
                "https://cdn.discordapp.com/emojis/1075509351223144520.gif?size=80&quality=lossless"
            );
        }
        else
        {
            embed.WithAuthor($"{trainerNick}'s Tokens");
        }

        // Recreate the component buttons to keep them available
        var components = new ComponentBuilder()
            .WithButton("Chests", "chest_button", ButtonStyle.Secondary)
            .WithButton("Misc", "misc_button", ButtonStyle.Secondary)
            .WithButton("Radiant Tokens", "tokens_button", ButtonStyle.Secondary)
            .Build();

        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed.Build();
            msg.Components = components;
        });
    }

    /// <summary>
    ///     Displays miscellaneous information about a user's account and inventory.
    /// </summary>
    /// <param name="user">The user whose miscellaneous information to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This method is called when the user clicks the "Misc" button on the balance display.
    ///     It creates a follow-up message with detailed information about various aspects
    ///     of the user's account including market slots, daycare, Pokemon owned,
    ///     shadow hunt, and fishing stats.
    /// </remarks>
    public async Task BalanceMisc(IUser user)
    {
        StringBuilder desc = new();

        await using var db = await _dbContextProvider.GetConnectionAsync();

        var details = await db.Users.FirstOrDefaultAsync(u => u.UserId == user.Id);
        if (details == null)
        {
            await FollowupAsync($"{user.Username} has not started!", ephemeral: _hidden);
            return;
        }

        if (!details.Visible.GetValueOrDefault() && user.Id != ctx.User.Id)
        {
            await FollowupAsync(
                $"You are not permitted to see the Trainer card of {user.Username}",
                ephemeral: _hidden
            );
            return;
        }

        var marketUsed = await db.Market
            .CountAsync(m => m.OwnerId == user.Id && m.BuyerId == null);

        // Count eggs using the ownership table and Pokemon table
        var daycared = await db.UserPokemonOwnerships
            .Join(db.UserPokemon,
                o => o.PokemonId,
                p => p.Id,
                (o, p) => new { Ownership = o, Pokemon = p })
            .CountAsync(j => j.Ownership.UserId == user.Id && j.Pokemon.PokemonName == "Egg");

        // Get total Pokemon count from the ownership table
        var pokemonCount = await db.UserPokemonOwnerships
            .CountAsync(o => o.UserId == user.Id);

        var bike = details.Bike ?? false;
        var trainerNick = details.TrainerNickname ?? user.Username;
        var daycareLimit = details.DaycareLimit ?? 1;
        var heldItem = details.HeldItem;
        var marketLimit = details.MarketLimit;
        var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(details.Inventory ?? "{}") ??
                        new Dictionary<string, int>();
        var staffRank = details.Staff;
        var hunt = details.Hunt;
        var huntProgress = details.Chain;
        var fishingLevel = details.FishingLevel ?? 1;
        var fishingExp = details.FishingExp ?? 0;
        var fishingLevelCap = details.FishingLevelCap ?? 50;

        // Generate visual health bars
        var energy = Service.DoHealth(10, details.Energy ?? 10);
        var fishingExpBar = Service.DoHealth((int)fishingLevelCap, (int)fishingExp);

        var marketLimitBonus = 0;

        var marketText = $"{marketUsed}/{marketLimit}";
        if (marketLimitBonus > 0) marketText += $" (+ {marketLimitBonus}!)";

        desc.AppendLine($"\n**Market Slots**: `{marketText}`");
        desc.AppendLine($"| **Daycare**: `{daycared}/{daycareLimit}`");
        desc.AppendLine($"\n**Pokemon Owned**: `{pokemonCount:,}`");

        if (!string.IsNullOrEmpty(hunt))
            desc.AppendLine($"\n**Shadow Hunt**: `{hunt} ({huntProgress}x)`");
        else
            desc.AppendLine("\n**Shadow Hunt**: Select with `/hunt`!");

        desc.AppendLine($"\n**Bicycle**: {bike}");
        desc.AppendLine("\n**General Inventory**\n");

        // Filter out chest items from display
        inventory.Remove("coin-case");

        foreach (var (item, count) in inventory)
        {
            if (item.Contains("common chest") ||
                item.Contains("rare chest") ||
                item.Contains("mythic chest") ||
                item.Contains("legend chest") ||
                item.Contains("spooky chest") ||
                item.Contains("fleshy chest") ||
                item.Contains("ghost detector") ||
                item.Contains("horrific chest") ||
                item.Contains("heart chest"))
                continue;

            if (item.Contains("breeding"))
                desc.AppendLine(
                    $"{item.Replace("-", " ").Capitalize()} `{count}` `({Service.CalculateBreedingMultiplier(count)})`");
            else if (item.Contains("iv"))
                desc.AppendLine(
                    $"{item.Replace("-", " ").Capitalize()} `{count}` `({Service.CalculateIvMultiplier(count)})`");
            else
                desc.AppendLine($"{item.Replace("-", " ").Capitalize()} `{count}`x");
        }

        var embed = new EmbedBuilder()
            .WithColor(new Color(0xFFB6C1))
            .WithDescription(desc.ToString());

        embed.AddField("Energy", energy);
        embed.AddField(
            "Fishing Stats",
            $"Fishing Level - {fishingLevel}\nFishing Exp - {fishingExp}/{fishingLevelCap}\n{fishingExpBar}"
        );

        if (staffRank.ToLower() != "user")
        {
            embed.WithFooter(
                $"DittoBOT {staffRank}",
                $"attachment://di.webp"
            );

            embed.WithAuthor($"{trainerNick}'s Miscellaneous Balances");
        }
        else
        {
            embed.WithAuthor($"{trainerNick}'s Miscellaneous Balances");
        }

        // Recreate the component buttons
        var components = new ComponentBuilder()
            .WithButton("Chests", "chest_button", ButtonStyle.Secondary)
            .WithButton("Misc", "misc_button", ButtonStyle.Secondary)
            .WithButton("Radiant Tokens", "tokens_button", ButtonStyle.Secondary)
            .Build();

        await FollowupAsync(embed: embed.Build(), components: components, ephemeral: _hidden);
    }

    /// <summary>
    ///     Shows the items in the user's bag.
    /// </summary>
    /// <param name="hidden">Whether to show the bag as an ephemeral message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [SlashCommand("bag", "Lists your items.")]
    public async Task Bag(bool hidden = true)
    {
        await using var db = await _dbContextProvider.GetConnectionAsync();

        var items = await db.Users
            .Where(u => u.UserId == ctx.User.Id)
            .Select(u => u.Items)
            .FirstOrDefaultAsync();

        if (items == null)
        {
            await RespondAsync("You have not Started!\nStart with `/start` first!", ephemeral: true);
            return;
        }

        var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(items ?? "{}") ??
                        new Dictionary<string, int>();

        StringBuilder desc = new();
        foreach (var item in inventory.Keys.OrderBy(k => k))
            if (inventory[item] > 0)
                desc.AppendLine($"{item.Replace("-", " ").Capitalize()} : {inventory[item]}x");

        if (desc.Length == 0)
        {
            var emptyEmbed = new EmbedBuilder()
                .WithTitle("Your Current Bag")
                .WithColor(new Color(0x5D3FD3))
                .WithDescription("Nothin here..");

            await RespondAsync(embed: emptyEmbed.Build(), ephemeral: hidden);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Your Current Bag")
            .WithColor(new Color(0x5D3FD3))
            .WithDescription(desc.ToString());

        await RespondAsync(embed: embed.Build(), ephemeral: hidden);
    }

    /// <summary>
    ///     Group of commands for spreading Honey in channels.
    /// </summary>
    [Group("spread", "Commands for spreading Honey")]
    public class SpreadCommands : EeveeCoreSlashModuleBase<ExtrasService>
    {
        private readonly LinqToDbConnectionProvider _dbContextProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpreadCommands" /> class.
        /// </summary>
        /// <param name="dbContextProvider">The database context provider.</param>
        public SpreadCommands(LinqToDbConnectionProvider dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        /// <summary>
        ///     Spreads honey in the current channel to increase legendary and rare spawn rates.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("honey", "Spread honey in the current channel to increase legendary and rare spawn rates")]
        public async Task SpreadHoney()
        {
            await using var db = await _dbContextProvider.GetConnectionAsync();

            var inventory = await db.Users
                .FirstOrDefaultAsync(u => u.UserId == ctx.User.Id);

            if (inventory == null)
            {
                await RespondAsync("You have not Started!\nStart with `/start` first!");
                return;
            }

            var honey = await db.Honey
                .FirstOrDefaultAsync(h => h.ChannelId == ctx.Channel.Id);

            if (honey != null)
            {
                await RespondAsync($"There is already honey in this channel!\n🕐**Ends:** <t:{honey.Expires}:R> 🕐");
                return;
            }

            var inventoryDict = JsonSerializer.Deserialize<Dictionary<string, int>>(inventory.Inventory ?? "{}")
                                ?? new Dictionary<string, int>();

            if (!inventoryDict.TryGetValue("honey", out var honeyCount) || honeyCount < 1)
            {
                await RespondAsync("You do not have any units of Honey!");
                return;
            }

            inventoryDict["honey"]--;
            var expires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

            await db.InsertAsync(new Honey
            {
                ChannelId = ctx.Channel.Id,
                Expires = (int)expires,
                OwnerId = ctx.User.Id,
                Type = "honey"
            });

            inventory.Inventory = JsonSerializer.Serialize(inventoryDict);
            await db.UpdateAsync(inventory);

            

            await RespondAsync(
                $"You have successfully spread some of your honey!\n> 🕐**Lasts for:** <t:{expires}:R> 🕐");
        }

        /// <summary>
        ///     Displays active honey in the current server.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("server", "Server Honey Stats")]
        public async Task SpreadServer()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Server Stats")
                .WithColor(0xFFBC61)
                .AddField("Official Server", "[Join the Official Server](https://discord.gg/ditto)");

            await using var db = await _dbContextProvider.GetConnectionAsync();
            var channelIds = (await ctx.Guild.GetTextChannelsAsync()).Select(c => c.Id).ToList();
            var honeyLocations = await db.Honey
                .Where(h => channelIds.Contains(h.ChannelId))
                .ToListAsync();

            StringBuilder desc = new();
            foreach (var honey in honeyLocations)
            {
                var honeyType = honey.Type switch
                {
                    "honey" => "Honey",
                    "ghost" => "Ghost Detector",
                    "cheer" => "Christmas Cheer",
                    _ => honey.Type
                };

                desc.AppendLine($"**{honeyType}** active in:");
                desc.AppendLine($"<#{honey.ChannelId}> -> `Expires:` <t:{honey.Expires}:R>**\n");
            }

            embed.WithDescription(desc.ToString());

            await RespondAsync(embed: embed.Build());
        }
    }

    /// <summary>
    ///     Group of commands for setting Pokémon properties.
    /// </summary>
    [Group("set", "Used to set Pokemon properties")]
    public class SetCommands : EeveeCoreSlashModuleBase<ExtrasService>
    {
        // List of natures
        private static readonly List<string> NatureList =
        [
            "Hardy", "Lonely", "Brave", "Adamant", "Naughty",
            "Bold", "Docile", "Relaxed", "Impish", "Lax",
            "Timid", "Hasty", "Serious", "Jolly", "Naive",
            "Modest", "Mild", "Quiet", "Bashful", "Rash",
            "Calm", "Gentle", "Sassy", "Careful", "Quirky"
        ];

        private readonly LinqToDbConnectionProvider _dbContextProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SetCommands" /> class.
        /// </summary>
        /// <param name="dbContextProvider">The database context provider.</param>
        public SetCommands(LinqToDbConnectionProvider dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        /// <summary>
        ///     Uses a nature capsule to change the selected Pokémon's nature.
        /// </summary>
        /// <param name="nature">The name of the nature to set.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("nature", "Uses a nature capsule to change your selected Pokemon's nature")]
        public async Task SetNature(string nature)
        {
            if (!NatureList.Contains(nature.Capitalize()))
            {
                await RespondAsync("That Nature does not exist!");
                return;
            }

            nature = nature.Capitalize();
            await using var db = await _dbContextProvider.GetConnectionAsync();

            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == ctx.User.Id);
            if (user == null)
            {
                await RespondAsync("You have not Started!\nStart with `/start` first!");
                return;
            }

            var inventory = JsonSerializer.Deserialize<Dictionary<string, int>>(user.Inventory ?? "{}")
                            ?? new Dictionary<string, int>();

            if (!inventory.TryGetValue("nature-capsules", out var capsules) || capsules <= 0)
            {
                await RespondAsync("You have no nature capsules! Buy some with `/redeem nature capsules`.");
                return;
            }

            inventory["nature-capsules"]--;

            var selectedPokemon = await db.UserPokemon.FirstOrDefaultAsync(p => p.Id == user.Selected);
            if (selectedPokemon == null)
            {
                await RespondAsync("You don't have a Pokémon selected!");
                return;
            }

            user.Inventory = JsonSerializer.Serialize(inventory);
            await db.UpdateAsync(user);

            await db.UserPokemon
                .Where(p => p.Id == user.Selected)
                .Set(p => p.Nature, nature)
                .UpdateAsync();

            

            await RespondAsync($"You have successfully changed your selected Pokemon's nature to {nature}");
        }

        /// <summary>
        ///     Sets or resets the nickname of the selected Pokémon.
        /// </summary>
        /// <param name="nick">The new nickname, or "None" to reset.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("nick", "Set or reset your selected pokemon's nickname")]
        public async Task SetNick(string nick = "None")
        {
            if (nick.Length > 150)
            {
                await RespondAsync("Nickname is too long!");
                return;
            }

            // Check for inappropriate content
            if (nick.Contains("@here") ||
                nick.Contains("@everyone") ||
                nick.Contains("http") ||
                nick.Contains("nigger") ||
                nick.Contains("nigga") ||
                nick.Contains("gay") ||
                nick.Contains("fag") ||
                nick.Contains("kike") ||
                nick.Contains("jew") ||
                nick.Contains("faggot"))
            {
                await RespondAsync("Nope.");
                return;
            }

            await using var db = await _dbContextProvider.GetConnectionAsync();
            var selectedId = await db.Users
                .Where(u => u.UserId == ctx.User.Id)
                .Select(u => u.Selected)
                .FirstOrDefaultAsync();

            if (selectedId == null)
            {
                await RespondAsync("You don't have a Pokémon selected!");
                return;
            }

            await db.UserPokemon.Where(p => p.Id == selectedId)
                .Set(p => p.Nickname, nick)
                .UpdateAsync();

            

            if (nick == "None")
                await RespondAsync("Successfully reset Pokemon nickname.");
            else
                await RespondAsync($"Successfully changed Pokemon nickname to {nick}.");
        }
    }

    /// <summary>
    ///     Group of commands for toggling visibility settings.
    /// </summary>
    [Group("visible", "Toggle visibility settings")]
    public class VisibleCommands : EeveeCoreSlashModuleBase<ExtrasService>
    {
        private readonly LinqToDbConnectionProvider _dbContextProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VisibleCommands" /> class.
        /// </summary>
        /// <param name="dbContextProvider">The database context provider.</param>
        public VisibleCommands(LinqToDbConnectionProvider dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        /// <summary>
        ///     Toggles visibility of the user's balance to other users.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("bal", "Toggle your balance being visible to other users")]
        public async Task VisibleBalance()
        {
            await using var db = await _dbContextProvider.GetConnectionAsync();

            await db.Users.Where(u => u.UserId == ctx.User.Id)
                .Set(u => u.Visible, u => !u.Visible)
                .UpdateAsync();

            

            await RespondAsync("Toggled trainer card visibility!");
        }

        /// <summary>
        ///     Toggles visibility of the user's donation total on balance statements.
        /// </summary>
        /// <param name="toggle">True to show donations, false to hide them.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("donations", "Toggle your donation total being shown on your balance statement")]
        public async Task VisibleDonations(bool toggle = false)
        {
            await using var db = await _dbContextProvider.GetConnectionAsync();

            await db.Users.Where(u => u.UserId == ctx.User.Id)
                .Set(u => u.ShowDonations, toggle)
                .UpdateAsync();

            

            if (toggle)
                await RespondAsync("Your donations total will now show on your balance.");
            else
                await RespondAsync("Your donations total will no longer show on your balance.");
        }
    }

    /// <summary>
    ///     Group of commands for looking up Pokémon data.
    /// </summary>
    [Group("lookup", "Commands for looking up pokemon data")]
    public class LookupCommands : EeveeCoreSlashModuleBase<ExtrasService>
    {
        private readonly Dictionary<string, uint> _elements;
        private readonly IMongoService _mongoService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LookupCommands" /> class.
        /// </summary>
        /// <param name="mongoService">The MongoDB service for database operations.</param>
        public LookupCommands(IMongoService mongoService)
        {
            _mongoService = mongoService;
            _elements = new Dictionary<string, uint>
            {
                ["normal"] = 0xA9A87A,
                ["fire"] = 0xEE7D39,
                ["water"] = 0x6A91ED,
                ["grass"] = 0x7BC856,
                ["electric"] = 0xF7CF41,
                ["ice"] = 0x9AD8D8,
                ["fighting"] = 0xBE2C2D,
                ["poison"] = 0x9E409F,
                ["ground"] = 0xDFBF6E,
                ["flying"] = 0xA891EE,
                ["psychic"] = 0xF65689,
                ["bug"] = 0xA8B831,
                ["rock"] = 0xB69F40,
                ["ghost"] = 0x705796,
                ["dragon"] = 0x6F3CF5,
                ["dark"] = 0x6E5849,
                ["steel"] = 0xB8B8CF,
                ["fairy"] = 0xF6C9E3
            };
        }

        /// <summary>
        ///     Looks up information about a Pokémon move.
        /// </summary>
        /// <param name="move">The name of the move to look up.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("move", "Look up information about a Pokémon move")]
        public async Task LookupMove(string move)
        {
            move = move.ToLower().Replace(" ", "-");

            // Validate the move exists
            var exists = await _mongoService.Moves.Find(x => x.Identifier == move).FirstOrDefaultAsync();
            if (exists == null)
            {
                await RespondAsync("That move does not exist!");
                return;
            }

            // Build the embed
            var prio = exists.Priority;
            var pp = exists.PP;
            var type =
                (await _mongoService.Types.Find(t => t.TypeId == exists.TypeId).FirstOrDefaultAsync())?.Identifier
                .Capitalize() ?? "Unknown";
            var acc = exists.Accuracy;
            var power = exists.Power;
            var dclass = exists.DamageClassId switch
            {
                1 => "Status",
                2 => "Physical",
                3 => "Special",
                _ => "Unknown"
            };

            var embed = new EmbedBuilder()
                .WithTitle(move.Capitalize().Replace("-", " "))
                .WithColor(new Color(_elements.GetValueOrDefault<string, uint>(type.ToLower(), 0x000001)))
                .WithDescription($"**Damage Class:** `{dclass}` " +
                                 (power.HasValue ? $"| **Power:** `{power}`" : "") +
                                 $"\n**Accuracy:** `{acc}` " +
                                 $"| **Type:** `{type}` " +
                                 $"| **PP:** `{pp}` " +
                                 (prio != 0 ? $"\n**Priority:** `{prio}`" : ""));

            // Get effect information
            var effectEntries = await _mongoService.Moves
                .Find(m => m.Identifier == move && m.EffectChance != null)
                .FirstOrDefaultAsync();

            var effectsDescription = "No effect information available.";
            if (effectEntries != null) effectsDescription = $"Effect chance: {effectEntries.EffectChance}%";

            embed.AddField("Effect", effectsDescription);

            await RespondAsync(embed: embed.Build());
        }

        /// <summary>
        ///     Looks up information about a Pokémon ability.
        /// </summary>
        /// <param name="ability">The name of the ability to look up.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("ability", "Look up information about a Pokémon ability")]
        public async Task LookupAbility(string ability)
        {
            ability = ability.ToLower().Replace(" ", "-");

            // Validate the ability exists
            var exists = await _mongoService.Abilities.Find(x => x.Identifier == ability).FirstOrDefaultAsync();
            if (exists == null)
            {
                await RespondAsync("That ability does not exist!");
                return;
            }

            // Build the embed
            var embed = new EmbedBuilder()
                .WithTitle(ability.Capitalize().Replace("-", " "))
                .WithColor(new Color(0xF699CD));

            embed.AddField("Effect", "Effect information is stored in the database");

            // Find Pokémon with this ability
            var pokeWithAbility = await _mongoService.PokeAbilities
                .Find(a => a.AbilityId == exists.AbilityId)
                .ToListAsync();

            var pokemonIds = pokeWithAbility.Select(p => p.PokemonId).ToList();

            var pokemonList = await _mongoService.Forms
                .Find(f => pokemonIds.Contains(f.PokemonId))
                .Project(f => f.Identifier)
                .ToListAsync();

            if (pokemonList.Any())
            {
                var pokemonDesc = string.Join("\n", pokemonList.Select(p => p.Capitalize()));
                var pokeEmbed = new EmbedBuilder()
                    .WithTitle("Pokemon with " + ability.Capitalize().Replace("-", " "))
                    .WithColor(new Color(0xF699CD))
                    .WithDescription(pokemonDesc);

                await RespondAsync(embed: embed.Build());
                await FollowupAsync(embed: pokeEmbed.Build());
            }
            else
            {
                await RespondAsync(embed: embed.Build());
            }
        }

        /// <summary>
        ///     Looks up type effectiveness information for one or two Pokémon types.
        /// </summary>
        /// <param name="type1">The first type to look up.</param>
        /// <param name="type2">Optional second type to look up.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("type", "Look up type effectiveness information")]
        public async Task LookupType(string type1, string type2 = null)
        {
            var typeIds = new Dictionary<int, string>
            {
                [1] = "Normal",
                [2] = "Fighting",
                [3] = "Flying",
                [4] = "Poison",
                [5] = "Ground",
                [6] = "Rock",
                [7] = "Bug",
                [8] = "Ghost",
                [9] = "Steel",
                [10] = "Fire",
                [11] = "Water",
                [12] = "Grass",
                [13] = "Electric",
                [14] = "Psychic",
                [15] = "Ice",
                [16] = "Dragon",
                [17] = "Dark",
                [18] = "Fairy"
            };

            var typeEffectiveness = new Dictionary<(string, string), double>();

            var allTypeEffectiveness = await _mongoService.TypeEffectiveness.Find(_ => true).ToListAsync();
            foreach (var te in allTypeEffectiveness)
                if (typeIds.TryGetValue(te.DamageTypeId, out var damageType) &&
                    typeIds.TryGetValue(te.TargetTypeId, out var targetType))
                    typeEffectiveness[(damageType, targetType)] = te.DamageFactor / 100.0;

            type1 = type1.Capitalize();
            List<string> types = [type1];

            if (!string.IsNullOrEmpty(type2)) types.Add(type2.Capitalize());

            // Validate types
            foreach (var t in types)
                if (!typeIds.Values.Contains(t))
                {
                    await RespondAsync($"{t} is not a valid type.");
                    return;
                }

            var attackEffectiveness = new Dictionary<double, List<string>>();
            var defenseEffectiveness = new Dictionary<double, List<string>>();

            // Calculate attack effectiveness (only for single type)
            if (type2 == null)
                foreach (var targetType in typeIds.Values)
                {
                    var effectiveness = 1.0;
                    if (typeEffectiveness.TryGetValue((type1, targetType), out var factor)) effectiveness *= factor;

                    if (!attackEffectiveness.ContainsKey(effectiveness))
                        attackEffectiveness[effectiveness] = [];

                    attackEffectiveness[effectiveness].Add(targetType);
                }

            // Calculate defense effectiveness
            foreach (var damageType in typeIds.Values)
            {
                var effectiveness = 1.0;
                foreach (var defenseType in types)
                    if (typeEffectiveness.TryGetValue((damageType, defenseType), out var factor))
                        effectiveness *= factor;

                if (!defenseEffectiveness.ContainsKey(effectiveness))
                    defenseEffectiveness[effectiveness] = [];

                defenseEffectiveness[effectiveness].Add(damageType);
            }

            // Build description
            StringBuilder desc = new();

            // Defense effectiveness
            if (defenseEffectiveness.TryGetValue(4.0, out var quadDamage) && quadDamage.Any())
                desc.AppendLine($"**x4 damage from:** `{string.Join(", ", quadDamage)}`");

            if (defenseEffectiveness.TryGetValue(2.0, out var doubleDamage) && doubleDamage.Any())
                desc.AppendLine($"**x2 damage from:** `{string.Join(", ", doubleDamage)}`");

            if (defenseEffectiveness.TryGetValue(1.0, out var normalDamage) && normalDamage.Any())
                desc.AppendLine($"**x1 damage from:** `{string.Join(", ", normalDamage)}`");

            if (defenseEffectiveness.TryGetValue(0.5, out var halfDamage) && halfDamage.Any())
                desc.AppendLine($"**x1/2 damage from:** `{string.Join(", ", halfDamage)}`");

            if (defenseEffectiveness.TryGetValue(0.25, out var quarterDamage) && quarterDamage.Any())
                desc.AppendLine($"**x1/4 damage from:** `{string.Join(", ", quarterDamage)}`");

            if (defenseEffectiveness.TryGetValue(0.0, out var immuneDamage) && immuneDamage.Any())
                desc.AppendLine($"**Immune to damage from:** `{string.Join(", ", immuneDamage)}`");

            desc.AppendLine();

            // Attack effectiveness (only for single type)
            if (type2 == null)
            {
                if (attackEffectiveness.TryGetValue(2.0, out var superEffective) && superEffective.Any())
                    desc.AppendLine($"**x2 damage to:** `{string.Join(", ", superEffective)}`");

                if (attackEffectiveness.TryGetValue(1.0, out var normalEffective) && normalEffective.Any())
                    desc.AppendLine($"**x1 damage to:** `{string.Join(", ", normalEffective)}`");

                if (attackEffectiveness.TryGetValue(0.5, out var notEffective) && notEffective.Any())
                    desc.AppendLine($"**x1/2 damage to:** `{string.Join(", ", notEffective)}`");

                if (attackEffectiveness.TryGetValue(0.0, out var noEffect) && noEffect.Any())
                    desc.AppendLine($"**Does nothing to:** `{string.Join(", ", noEffect)}`");
            }

            var embed = new EmbedBuilder()
                .WithTitle(string.Join(", ", types))
                .WithColor(new Color(0xF699CD))
                .WithDescription(desc.ToString());

            await RespondAsync(embed: embed.Build());
        }
    }

    /// <summary>
    ///     Pokémon command group for interacting with Pokémon.
    /// </summary>
    [Group("poke", "Commands for interacting with your pokemon!")]
    public class PokemonCommands : EeveeCoreSlashModuleBase<ExtrasService>
    {
        /// <summary>
        ///     Slot choices for learning moves.
        /// </summary>
        public enum SlotChoice
        {
            /// <summary>Move slot 1.</summary>
            One = 1,

            /// <summary>Move slot 2.</summary>
            Two = 2,

            /// <summary>Move slot 3.</summary>
            Three = 3,

            /// <summary>Move slot 4.</summary>
            Four = 4
        }

        private readonly LinqToDbConnectionProvider _dbContextProvider;
        private readonly IMongoService _mongoService;
        private readonly PokemonService _pokemonService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PokemonCommands" /> class.
        /// </summary>
        /// <param name="mongoService">The MongoDB service for database operations.</param>
        /// <param name="dbContextProvider">The database context provider.</param>
        /// <param name="pokemonService">The Pokémon service for evolution and other related operations.</param>
        public PokemonCommands(
            IMongoService mongoService,
            LinqToDbConnectionProvider dbContextProvider,
            PokemonService pokemonService)
        {
            _mongoService = mongoService;
            _dbContextProvider = dbContextProvider;
            _pokemonService = pokemonService;
        }

        /// <summary>
        ///     Autocomplete handler for moves.
        /// </summary>
        /// <param name="context">The interaction.</param>
        /// <param name="move">The current input to complete.</param>
        /// <returns>A list of move choices.</returns>
        public async Task<IEnumerable<AutocompleteResult>> MovesAutoComplete(IInteractionContext context, string move)
        {
            await using var db = await _dbContextProvider.GetConnectionAsync();

            var selectedPokemon = await (
                from pokemon in db.UserPokemon
                join user in db.Users on pokemon.Id equals user.Selected
                where user.UserId == context.User.Id
                select pokemon
            ).FirstOrDefaultAsync();

            if (selectedPokemon == null)
                return Array.Empty<AutocompleteResult>();

            var moves = await Service.GetMoves(selectedPokemon.PokemonName.ToLower());

            if (moves == null)
                return Array.Empty<AutocompleteResult>();

            return moves
                .Where(m => m.StartsWith(move.ToLower()))
                .Take(25)
                .Select(m => new AutocompleteResult(m, m));
        }

        /// <summary>
        ///     Learns moves in all four slots for the selected Pokémon.
        /// </summary>
        /// <param name="first">The move to learn in slot 1.</param>
        /// <param name="second">The move to learn in slot 2.</param>
        /// <param name="third">The move to learn in slot 3.</param>
        /// <param name="fourth">The move to learn in slot 4.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("learn_all", "Learn all four moves for your selected Pokémon")]
        public async Task LearnAll(
            [Autocomplete(typeof(MovesAutoCompleteHandler))]
            string first,
            [Autocomplete(typeof(MovesAutoCompleteHandler))]
            string second,
            [Autocomplete(typeof(MovesAutoCompleteHandler))]
            string third,
            [Autocomplete(typeof(MovesAutoCompleteHandler))]
            string fourth)
        {
            first = first.Replace(" ", "-").ToLower();
            second = second.Replace(" ", "-").ToLower();
            third = third.Replace(" ", "-").ToLower();
            fourth = fourth.Replace(" ", "-").ToLower();

            await using var db = await _dbContextProvider.GetConnectionAsync();

            var selectedPokemon = await (
                from pokemon in db.UserPokemon
                join user in db.Users on pokemon.Id equals user.Selected
                where user.UserId == ctx.User.Id
                select pokemon
            ).FirstOrDefaultAsync();

            if (selectedPokemon == null)
            {
                await RespondAsync("You must select a pokemon!", ephemeral: true);
                return;
            }

            var skin = selectedPokemon.Skin;
            var radiant = selectedPokemon.Radiant;
            var shiny = selectedPokemon.Shiny;
            var nickname = selectedPokemon.Nickname;
            var pokemonName = selectedPokemon.PokemonName;

            var moves = await Service.GetMoves(pokemonName.ToLower());

            if (moves == null)
            {
                await RespondAsync("That pokemon cannot learn any moves! You might need to `/deform` it first.",
                    ephemeral: true);
                Log.Warning($"Could not get moves for {pokemonName}");
                return;
            }

            if (!moves.Contains(first))
            {
                await RespondAsync($"Your {pokemonName} can not learn that Move (1)\n`Retry this command again`",
                    ephemeral: true);
                return;
            }

            if (!moves.Contains(second))
            {
                await RespondAsync($"Your {pokemonName} can not learn that Move (2)\n`Retry this command again`",
                    ephemeral: true);
                return;
            }

            if (!moves.Contains(third))
            {
                await RespondAsync($"Your {pokemonName} can not learn that Move (3)\n`Retry this command again`",
                    ephemeral: true);
                return;
            }

            if (!moves.Contains(fourth))
            {
                await RespondAsync($"Your {pokemonName} can not learn that Move (4)\n`Retry this command again`",
                    ephemeral: true);
                return;
            }

            // Update the moves
            string[] newMoves = [first.ToLower(), second.ToLower(), third.ToLower(), fourth.ToLower()];

            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .Set(p => p.Moves, newMoves)
                .UpdateAsync();

            

            var displayName = nickname == "None" ? "" : nickname;
            var emote = "";

            if (shiny == true) emote = "<:shiny:1264836377627988041>";

            if (radiant == true) emote = "<:radiant:1264971529402450065>>";

            if (skin == "shadow") emote = "<:shadowicon4:1077328251556470925>";

            await RespondAsync(
                $"Your __**{displayName}({pokemonName.Capitalize()}){emote}**__ successfully learned the following moves:\n" +
                $"> __**{first.Capitalize()}**__, __**{second.Capitalize()}**__, __**{third.Capitalize()}**__, and __**{fourth.Capitalize()}**__",
                ephemeral: true
            );

            await _pokemonService.TryEvolve(selectedPokemon.Id);
        }

        /// <summary>
        ///     Learns a move in a specific slot for the selected Pokémon.
        /// </summary>
        /// <param name="slot">The slot to learn the move in.</param>
        /// <param name="move">The move to learn.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("learn", "Learn a move for your selected Pokémon")]
        public async Task Learn(
            SlotChoice slot,
            [Autocomplete(typeof(MovesAutoCompleteHandler))]
            string move)
        {
            move = move.Replace(" ", "-").ToLower();

            await using var db = await _dbContextProvider.GetConnectionAsync();

            var selectedPokemon = await (
                from pokemon in db.UserPokemon
                join user in db.Users on pokemon.Id equals user.Selected
                where user.UserId == ctx.User.Id
                select pokemon
            ).FirstOrDefaultAsync();

            if (selectedPokemon == null)
            {
                await RespondAsync("You do not have that Pokemon!", ephemeral: true);
                return;
            }

            var pokemonName = selectedPokemon.PokemonName;
            var moves = await Service.GetMoves(pokemonName.ToLower());

            if (moves == null)
            {
                await RespondAsync("That pokemon cannot learn any moves! You might need to `/deform` it first.",
                    ephemeral: true);
                Log.Warning($"Could not get moves for {pokemonName}");
                return;
            }

            if (!moves.Contains(move))
            {
                await RespondAsync($"Your {pokemonName} can not learn that Move", ephemeral: true);
                return;
            }

            // Get current moves and update the specific slot
            var currentMoves = selectedPokemon.Moves.ToArray();
            currentMoves[(int)slot - 1] = move.ToLower();

            await db.UserPokemon.Where(p => p.Id == selectedPokemon.Id)
                .Set(p => p.Moves, currentMoves)
                .UpdateAsync();

            

            await RespondAsync($"You have successfully learned {move} as your slot {(int)slot} move", ephemeral: true);

            await _pokemonService.TryEvolve(selectedPokemon.Id);
        }

        /// <summary>
        ///     Shows the moves the selected Pokémon has learned.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("moves", "Show moves your pokemon has learned")]
        public async Task Moves()
        {
            await using var db = await _dbContextProvider.GetConnectionAsync();

            var selectedPokemon = await (
                from pokemon in db.UserPokemon
                join user in db.Users on pokemon.Id equals user.Selected
                where user.UserId == ctx.User.Id
                select pokemon
            ).FirstOrDefaultAsync();

            if (selectedPokemon == null)
            {
                await RespondAsync("You do not have a selected pokemon. Select one with `/select` first.");
                return;
            }

            var moveSet = selectedPokemon.Moves.ToArray();
            var moveOne = moveSet[0];
            var moveTwo = moveSet[1];
            var moveThree = moveSet[2];
            var moveFour = moveSet[3];

            var embed = new EmbedBuilder()
                .WithTitle("Moves")
                .WithColor(new Color(0xFFB6C1))
                .AddField("**Move 1**:", moveOne)
                .AddField("**Move 2**:", moveTwo)
                .AddField("**Move 3**:", moveThree)
                .AddField("**Move 4**:", moveFour)
                .WithFooter("See available moves with /moveset");

            await RespondAsync(embed: embed.Build());
        }

        /// <summary>
        ///     Shows the moves that the selected Pokémon can learn.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("moveset", "Show moves your selected Pokemon can learn")]
        public async Task Moveset()
        {
            await using var db = await _dbContextProvider.GetConnectionAsync();

            var pokemonName = await db.UserPokemon
                .Where(p => p.Id == db.Users
                    .Where(u => u.UserId == ctx.User.Id)
                    .Select(u => u.Selected)
                    .FirstOrDefault())
                .Select(p => p.PokemonName)
                .FirstOrDefaultAsync();

            if (pokemonName == null)
            {
                await RespondAsync("You have not selected a Pokemon");
                return;
            }

            pokemonName = pokemonName.ToLower();

            if (pokemonName == "egg")
            {
                await RespondAsync("There are no available moves for an Egg");
                return;
            }

            var moves = await Service.GetMoves(pokemonName);

            if (moves == null)
            {
                await RespondAsync("That pokemon cannot learn any moves! You might need to `/deform` it first.");
                Log.Warning($"Could not get moves for {pokemonName}");
                return;
            }

            StringBuilder desc = new();

            foreach (var move in moves)
            {
                var formattedMove = move.Capitalize().Replace("'", "");

                var lowercaseMove = move.ToLower();
                var moveInfo = await _mongoService.Moves.Find(m => m.Identifier == lowercaseMove)
                    .FirstOrDefaultAsync();

                if (moveInfo == null && lowercaseMove.Contains('-'))
                {
                    var baseMoveIdentifier = lowercaseMove.Split('-')[0];
                    moveInfo = await _mongoService.Moves.Find(m => m.Identifier == baseMoveIdentifier)
                        .FirstOrDefaultAsync();
                }

                if (moveInfo == null)
                {
                    await RespondAsync("An error occurred finding moves for that pokemon.");
                    Log.Warning($"A move is not in mongo moves - {move}");
                    return;
                }

                var power = moveInfo.Power;
                var accuracy = moveInfo.Accuracy;
                var typeId = moveInfo.TypeId;
                var damageClassId = moveInfo.DamageClassId;

                string damageClass;
                if (damageClassId == 1)
                    damageClass = "<:status1:1013111246121353359><:status2:1013111249095098497>";
                else if (damageClassId == 2)
                    damageClass = "<:phys1:1013111228803059754><:phys2:1013111230937960479>";
                else // damageClassId == 3
                    damageClass = "<:special1:1013111240966557706><:special2:1013111242547802183>";

                var typeInfo = await _mongoService.Types.Find(t => t.TypeId == typeId).FirstOrDefaultAsync();
                var typeName = typeInfo?.Identifier.Capitalize() ?? "Unknown";

                desc.AppendLine(
                    $"**{damageClass} {formattedMove.Replace("-", " ")}** - Power:`{power}` Acc:`{accuracy}` Type:`{typeName}`");
            }

            var embed = new EmbedBuilder()
                .WithTitle("Learnable Move List")
                .WithColor(new Color(0xFFB6C1))
                .WithDescription(desc.ToString());

            await RespondAsync(embed: embed.Build());
        }

        /// <summary>
        ///     Adds EVs (Effort Values) to a stat for the selected Pokémon.
        /// </summary>
        /// <param name="amount">The amount of EVs to add.</param>
        /// <param name="stat">The stat to add EVs to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [SlashCommand("addevs", "Add ev's to one of your pokemon if you have ev points to spend")]
        public async Task AddEvs(int amount, string stat)
        {
            stat = stat.ToLower();

            if (!new[] { "attack", "hp", "defense", "special attack", "special defense", "speed" }.Contains(stat))
            {
                await RespondAsync(
                    "Correct usage of this command is: `/add evs <amount> <stat_name>`\n" +
                    "Example: `/add evs 252 speed` To add to your __Selected__ Pokemon`"
                );
                return;
            }

            if (amount > 252)
            {
                await RespondAsync("You can not add more than 252 EVs to a stat");
                return;
            }

            if (amount < 1)
            {
                await RespondAsync("You must add at least 1 EV to a stat");
                return;
            }

            await using var db = await _dbContextProvider.GetConnectionAsync();

            var selectedId = await db.Users
                .Where(u => u.UserId == ctx.User.Id)
                .Select(u => u.Selected)
                .FirstOrDefaultAsync();

            if (selectedId == null)
            {
                await RespondAsync("You do not have a selected pokemon!\nUse `/select` to select a pokemon.");
                return;
            }

            var evPoints = await db.Users
                .Where(u => u.UserId == ctx.User.Id)
                .Select(u => u.EvPoints)
                .FirstOrDefaultAsync();

            if (evPoints == null)
            {
                await RespondAsync("You have not Started!\nStart with `/start` first!");
                return;
            }

            if (evPoints < amount)
            {
                await RespondAsync($"You do not have {amount} EV Points to add!");
                return;
            }

            try
            {
                // Update the appropriate stat based on the input
                switch (stat)
                {
                    case "attack":
                        await db.UserPokemon.Where(p => p.Id == selectedId)
                            .Set(p => p.AttackEv, p => p.AttackEv + amount)
                            .UpdateAsync();
                        break;
                    case "defense":
                        await db.UserPokemon.Where(p => p.Id == selectedId)
                            .Set(p => p.DefenseEv, p => p.DefenseEv + amount)
                            .UpdateAsync();
                        break;
                    case "hp":
                        await db.UserPokemon.Where(p => p.Id == selectedId)
                            .Set(p => p.HpEv, p => p.HpEv + amount)
                            .UpdateAsync();
                        break;
                    case "special attack":
                        await db.UserPokemon.Where(p => p.Id == selectedId)
                            .Set(p => p.SpecialAttackEv, p => p.SpecialAttackEv + amount)
                            .UpdateAsync();
                        break;
                    case "special defense":
                        await db.UserPokemon.Where(p => p.Id == selectedId)
                            .Set(p => p.SpecialDefenseEv, p => p.SpecialDefenseEv + amount)
                            .UpdateAsync();
                        break;
                    case "speed":
                        await db.UserPokemon.Where(p => p.Id == selectedId)
                            .Set(p => p.SpeedEv, p => p.SpeedEv + amount)
                            .UpdateAsync();
                        break;
                }

                // Deduct the EV points from the user
                await db.Users.Where(u => u.UserId == ctx.User.Id)
                    .Set(u => u.EvPoints, u => u.EvPoints - amount)
                    .UpdateAsync();

                

                await RespondAsync(
                    $"You have successfully added {amount} EVs to the {stat} Stat of your Selected Pokemon!");
            }
            catch (Exception)
            {
                await RespondAsync("Your Pokemon has maxed all 510 EVs");
            }
        }
    }
}