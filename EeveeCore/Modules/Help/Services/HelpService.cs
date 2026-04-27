using System.Text;
using Discord.Interactions;
using EeveeCore.Common.Attributes.Interactions;

namespace EeveeCore.Modules.Help.Services;

/// <summary>
///     A service for handling help commands. Owns the curated module catalog used to render
///     friendly category labels and descriptions in place of raw class names.
/// </summary>
public class HelpService : INService
{
    /// <summary>
    ///     Curated catalog of user-facing categories. Keyed by the top-level module class name
    ///     (Discord.Net's <c>Module.GetTopLevelModule().Name</c>).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, CategoryInfo> Catalog = new Dictionary<string, CategoryInfo>(StringComparer.OrdinalIgnoreCase)
    {
        ["PokemonSlashCommands"] = new("Pokémon", "View, manage, and inspect your Pokémon collection."),
        ["StartModule"] = new("Getting Started", "Pick your starter and learn the basics."),
        ["SpawnSlashCommands"] = new("Spawn", "Configure where and how Pokémon spawn in your server."),
        ["BreedingModule"] = new("Breeding", "Breed Pokémon to produce eggs and inherit traits."),
        ["PokemonBattleModule"] = new("Duels", "Battle your Pokémon against other trainers or NPCs."),
        ["ItemSlashCommands"] = new("Items", "Manage and use items from your bag."),
        ["MarketSlashCommands"] = new("Market", "Browse and trade Pokémon on the global market."),
        ["TradeSlashCommands"] = new("Trading", "Direct trades with other trainers."),
        ["GiftSlashCommands"] = new("Gifting", "Send and receive gifts."),
        ["TradeAdminSlashCommands"] = new("Trade Admin", "Trade fraud-detection tools (staff only)."),
        ["PartyModule"] = new("Parties", "Build and manage your active Pokémon parties."),
        ["HatcheryModule"] = new("Hatchery", "Manage your eggs and the hatchery."),
        ["AchievementsSlashCommands"] = new("Achievements", "Track your in-game achievements."),
        ["FishingSlashCommands"] = new("Fishing", "Fish for Pokémon and rare encounters."),
        ["MissionsSlashCommands"] = new("Missions", "Complete daily, weekly, and event missions."),
        ["GamesSlashCommands"] = new("Mini-Games", "Casual games for in-game currency."),
        ["SimpleGamesSlashCommands"] = new("Mini-Games", "Casual games for in-game currency."),
        ["ShopSlashCommands"] = new("Shop", "Buy items, boosters, and skins."),
        ["VoucherSlashCommands"] = new("Vouchers", "Redeem voucher codes for rewards."),
        ["ExtrasSlashCommands"] = new("Extras", "Miscellaneous utilities and tools."),
        ["HelpSlashCommand"] = new("Help", "Bot help and command reference.")
    };

    private readonly DiscordShardedClient client;
    private readonly InteractionService interactionService;

    /// <summary>
    ///     Initializes a new instance of <see cref="HelpService" />.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="interactionService">The Discord interaction service.</param>
    public HelpService(DiscordShardedClient client, InteractionService interactionService)
    {
        this.client = client;
        this.interactionService = interactionService;
    }

    /// <summary>
    ///     Returns the categories that should appear in the help menu, in display order.
    ///     Multiple top-level modules with the same catalog label are merged into one entry.
    /// </summary>
    public IReadOnlyList<CategoryEntry> GetCategories()
    {
        var byLabel = new Dictionary<string, CategoryEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var cmd in interactionService.SlashCommands)
        {
            var className = cmd.Module.GetTopLevelModule().Name;
            var info = Catalog.TryGetValue(className, out var meta) ? meta : new CategoryInfo(className, "");

            if (!byLabel.TryGetValue(info.Label, out var entry))
            {
                entry = new CategoryEntry(info.Label, info.Label, info.Description, 0, new List<string>());
                byLabel[info.Label] = entry;
            }

            if (!entry.ClassNames.Contains(className, StringComparer.OrdinalIgnoreCase))
                entry.ClassNames.Add(className);
            entry.CommandCount += 1;
        }

        return byLabel.Values
            .OrderBy(e => e.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    ///     Builds the home help embed listing all categories with their descriptions.
    /// </summary>
    /// <param name="user">The user requesting help.</param>
    public EmbedBuilder GetHelpEmbed(IUser user)
    {
        var categories = GetCategories();
        var lines = categories
            .Select(c => string.IsNullOrEmpty(c.Description)
                ? Format.Bold(c.Label)
                : $"{Format.Bold(c.Label)}: {c.Description}");

        var description =
            "Pick a category from the menu below to see its commands.\n" +
            "Use `/help search` to look up a specific command by name.\n\n" +
            string.Join("\n", lines);

        if (description.Length > 4000)
            description = description[..3997] + "…";

        var embed = new EmbedBuilder()
            .WithAuthor(client.CurrentUser.Username, client.CurrentUser.GetAvatarUrl())
            .WithTitle("Help")
            .WithDescription(description)
            .WithFooter($"Requested by {user.Username}", user.GetAvatarUrl())
            .WithOkColor();

        return embed;
    }

    /// <summary>
    ///     Builds the category dropdown for the home help message.
    /// </summary>
    public ComponentBuilder GetHelpComponents()
    {
        var categories = GetCategories();
        var components = new ComponentBuilder();

        foreach (var batch in categories.Chunk(25).Select((b, i) => (Index: i, Items: b)))
        {
            var menu = new SelectMenuBuilder()
                .WithCustomId($"helpselect:{batch.Index}")
                .WithPlaceholder("Pick a category…");

            foreach (var entry in batch.Items)
            {
                var description = string.IsNullOrEmpty(entry.Description)
                    ? $"{entry.CommandCount} command(s)"
                    : Truncate(entry.Description, 100);
                menu.AddOption(entry.Label, entry.Key, description);
            }

            components.WithSelectMenu(menu);
        }

        return components;
    }

    /// <summary>
    ///     Builds an embed listing all commands within a category.
    /// </summary>
    /// <param name="categoryKey">The top-level module class name to list.</param>
    /// <param name="user">The user requesting help.</param>
    /// <param name="permittedNames">
    ///     Optional set of slash-command names the user is permitted to run.
    ///     When supplied, commands not in this set are marked unavailable.
    /// </param>
    public EmbedBuilder GetCategoryEmbed(string categoryKey, IUser user, ISet<string>? permittedNames = null)
    {
        var entry = GetCategories().FirstOrDefault(c =>
            string.Equals(c.Key, categoryKey, StringComparison.OrdinalIgnoreCase));

        var info = entry is not null
            ? new CategoryInfo(entry.Label, entry.Description)
            : Catalog.TryGetValue(categoryKey, out var meta)
                ? meta
                : new CategoryInfo(categoryKey, "");

        var classNames = entry?.ClassNames ?? new List<string> { categoryKey };

        var commands = interactionService.SlashCommands
            .Where(c => classNames.Contains(c.Module.GetTopLevelModule().Name, StringComparer.OrdinalIgnoreCase))
            .DistinctBy(c => $"{GetSlashPath(c)}")
            .OrderBy(c => GetSlashPath(c), StringComparer.Ordinal)
            .ToList();

        var embed = new EmbedBuilder()
            .WithAuthor(client.CurrentUser.Username, client.CurrentUser.GetAvatarUrl())
            .WithTitle(info.Label)
            .WithFooter($"Requested by {user.Username}", user.GetAvatarUrl())
            .WithOkColor();

        if (!string.IsNullOrEmpty(info.Description))
            embed.WithDescription(info.Description);

        if (commands.Count == 0)
        {
            embed.AddField("Commands", "*No commands available.*");
            return embed;
        }

        var sb = new StringBuilder();
        foreach (var cmd in commands)
        {
            var allowed = permittedNames is null || permittedNames.Contains(cmd.Name);
            var marker = allowed ? "•" : "✗";
            var path = GetSlashPath(cmd);
            var desc = string.IsNullOrWhiteSpace(cmd.Description) ? "*(no description)*" : cmd.Description;

            var line = $"{marker} `/{path}`: {desc}\n";
            if (sb.Length + line.Length > 1024)
            {
                embed.AddField("Commands", sb.ToString().TrimEnd());
                sb.Clear();
            }

            sb.Append(line);
        }

        if (sb.Length > 0)
            embed.AddField(embed.Fields.Count == 0 ? "Commands" : "​", sb.ToString().TrimEnd());

        if (permittedNames is not null)
            embed.AddField("Legend", "• you can run this  ·  ✗ you cannot run this", false);

        return embed;
    }

    /// <summary>
    ///     Builds an embed describing a single command.
    /// </summary>
    public EmbedBuilder GetCommandHelp(SlashCommandInfo cmd)
    {
        var path = GetSlashPath(cmd);
        var topLevel = cmd.Module.GetTopLevelModule().Name;
        var category = Catalog.TryGetValue(topLevel, out var meta) ? meta.Label : topLevel;

        var embed = new EmbedBuilder()
            .WithTitle($"/{path}")
            .WithDescription(string.IsNullOrWhiteSpace(cmd.Description) ? "*(no description)*" : cmd.Description)
            .AddField("Category", category, true)
            .WithOkColor();

        if (cmd.Parameters.Count > 0)
        {
            var paramLines = cmd.Parameters.Select(p =>
            {
                var name = p.Name;
                var pdesc = string.IsNullOrWhiteSpace(p.Description) ? "" : $": {p.Description}";
                var optional = p.IsRequired ? "" : " *(optional)*";
                return $"`{name}`{optional}{pdesc}";
            });
            embed.AddField("Parameters", string.Join("\n", paramLines));
        }

        var reqs = GetCommandRequirements(cmd);
        if (reqs.Length > 0)
            embed.AddField("Requires", string.Join("\n", reqs));

        return embed;
    }

    private static string GetSlashPath(SlashCommandInfo cmd)
    {
        var parts = new List<string>();
        for (var m = cmd.Module; m is not null; m = m.Parent)
        {
            if (!string.IsNullOrEmpty(m.SlashGroupName))
                parts.Insert(0, m.SlashGroupName);
        }

        parts.Add(cmd.Name);
        return string.Join(" ", parts);
    }

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max) return s;
        return s[..(max - 1)] + "…";
    }

    private static string[] GetCommandRequirements(SlashCommandInfo cmd)
    {
        var list = new List<string>();
        if (cmd.Preconditions.Any(p => p is RequireAdminAttribute))
            list.Add("Bot Owner");

        if (cmd.Preconditions.FirstOrDefault(p => p is RequireUserPermissionAttribute) is RequireUserPermissionAttribute up)
        {
            if (up.GuildPermission is { } gp) list.Add($"User: {gp} (server)");
            if (up.ChannelPermission is { } cp) list.Add($"User: {cp} (channel)");
        }

        if (cmd.Preconditions.FirstOrDefault(p => p is RequireBotPermissionAttribute) is RequireBotPermissionAttribute bp)
        {
            if (bp.GuildPermission is { } gp) list.Add($"Bot: {gp} (server)");
            if (bp.ChannelPermission is { } cp) list.Add($"Bot: {cp} (channel)");
        }

        return list.ToArray();
    }
}

/// <summary>Curated metadata for a help category.</summary>
/// <param name="Label">User-facing display name.</param>
/// <param name="Description">One-line description of the category.</param>
public record CategoryInfo(string Label, string Description);

/// <summary>
///     A category entry shown in the help menu. May aggregate multiple top-level modules under one label.
/// </summary>
/// <param name="Key">Stable lookup value (the label) used as the dropdown option value.</param>
/// <param name="Label">User-facing display name.</param>
/// <param name="Description">One-line description.</param>
/// <param name="CommandCount">Number of distinct slash commands across all aggregated modules.</param>
/// <param name="ClassNames">Top-level module class names that contribute commands to this category.</param>
public record CategoryEntry(
    string Key,
    string Label,
    string Description,
    int CommandCount,
    List<string> ClassNames)
{
    /// <summary>Mutable command count, incremented as commands are discovered.</summary>
    public int CommandCount { get; set; } = CommandCount;
}
