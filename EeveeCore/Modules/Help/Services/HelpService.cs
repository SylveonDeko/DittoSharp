using Discord.Interactions;
using EeveeCore.Common.Attributes.Interactions;
using MoreLinq;

namespace EeveeCore.Modules.Help.Services;

/// <summary>
///     A service for handling help commands.
/// </summary>
public class HelpService : INService
{
    private readonly DiscordShardedClient client;
    private readonly GuildSettingsService guildSettings;
    private readonly InteractionService interactionService;


    /// <summary>
    ///     Initializes a new instance of <see cref="HelpService" />.
    /// </summary>
    /// <param name="client">The discord client</param>
    /// <param name="bot">The bot itself</param>
    /// <param name="blacklistService">The user/server blacklist service</param>
    /// <param name="cmds">The command service</param>
    /// <param name="perms">The global permissions service</param>
    /// <param name="nPerms">The per server permission service</param>
    /// <param name="interactionService">The discord interaction service</param>
    /// <param name="guildSettings">Service to get guild configs</param>
    /// <param name="eventHandler">The event handler Sylveon made because the events in dnet were single threaded.</param>
    public HelpService(
        DiscordShardedClient client,
        InteractionService interactionService,
        GuildSettingsService guildSettings, EventHandler eventHandler)
    {
        this.client = client;
        eventHandler.MessageReceived += HandlePing;
        eventHandler.JoinedGuild += HandleJoin;
        this.interactionService = interactionService;
        this.guildSettings = guildSettings;
    }


    /// <summary>
    ///     Builds the select menus for the modules
    /// </summary>
    /// <param name="guild">The guild the help menu was executed in, may be null if in dm</param>
    /// <param name="user">The user that executed the help menu</param>
    /// <param name="descriptions">Whether descriptions are on or off</param>
    /// <returns>A <see cref="ComponentBuilder" /> instance with the bots modules in it</returns>
    public ComponentBuilder GetHelpComponents(IGuild? guild, IUser user, bool descriptions = true)
    {
        var modules = interactionService.SlashCommands.Select(x => x.Module).Where(x => !x.IsSubModule).Distinct();
        var compBuilder = new ComponentBuilder();
        var menuCount = (modules.Count() - 1) / 25 + 1;

        for (var j = 0; j < menuCount; j++)
        {
            var selMenu = new SelectMenuBuilder().WithCustomId($"helpselect:{j}");
            foreach (var i in modules.Skip(j * 25).Take(25))
                selMenu.Options.Add(new SelectMenuOptionBuilder()
                    .WithLabel(i.Name).WithDescription(GetModuleDescription(i.Name, guild))
                    .WithValue(i.Name.ToLower()));

            compBuilder.WithSelectMenu(selMenu); // add the select menu to the component builder
        }

        compBuilder.WithButton("Toggle Descriptions", $"toggle-descriptions:{descriptions},{user.Id}");
        return compBuilder;
    }


    /// <summary>
    ///     Builds the help embed for the help menu
    /// </summary>
    /// <param name="description">Whether descriptions for each module are on or off</param>
    /// <param name="guild">The guild where the help menu was executed</param>
    /// <param name="channel">The channel where the help menu was executed</param>
    /// <param name="user">The user who executed the help menu</param>
    /// <returns></returns>
    public async Task<EmbedBuilder> GetHelpEmbed(bool description, IGuild? guild, IMessageChannel channel, IUser user)
    {
        var prefix = await guildSettings.GetPrefix(guild);
        EmbedBuilder embed = new();
        embed.WithAuthor(new EmbedAuthorBuilder().WithName("EeveeCore Help")
            .WithIconUrl(client.CurrentUser.GetAvatarUrl()));
        embed.WithOkColor();

        var modules = interactionService.SlashCommands.Select(x => x.Module)
            .Where(x => !x.IsSubModule).Distinct();
        var count = 0;
        if (description)
            foreach (var mod in modules)
                embed.AddField(mod.Name,
                    $">>> {GetModuleDescription(mod.Name, guild)}", true);
        else
            foreach (var i in modules.Batch(modules.Count() / 2))
            {
                var categoryStrings = i.Select(x =>
                    Format.Bold(x.Name)
                );

                embed.AddField(
                    count == 0 ? "Categories" : "_ _",
                    string.Join("\n", categoryStrings),
                    true
                );
                count++;
            }

        return embed;
    }


    private string? GetModuleDescription(string module, IGuild? guild)
    {
        return module.ToLower() switch
        {
            _ => null
        };
    }

    private async Task HandlePing(SocketMessage msg)
    {
        if (msg.Content == $"<@{client.CurrentUser.Id}>" || msg.Content == $"<@!{client.CurrentUser.Id}>")
            if (msg.Channel is ITextChannel chan)
            {
                var eb = new EmbedBuilder();
                eb.WithOkColor();
                eb.WithDescription(
                    "Hi there!");
                eb.WithThumbnailUrl("https://cdn.discordapp.com/emojis/914307922287276052.gif");
                eb.WithFooter(new EmbedFooterBuilder().WithText(client.CurrentUser.Username)
                    .WithIconUrl(client.CurrentUser.GetAvatarUrl()));
                await chan.SendMessageAsync(embed: eb.Build()).ConfigureAwait(false);
            }
    }

    private async Task HandleJoin(IGuild guild)
    {
        var cb = new ComponentBuilder();
        var e = await guild.GetDefaultChannelAsync();
        var px = await guildSettings.GetPrefix(guild);
        var eb = new EmbedBuilder
        {
            Description =
                $"Hi, thanks for inviting EeveeCore! I hope you like the bot, and discover all its features! The default prefix is `{px}.` This can be changed with the prefix command."
        };
        eb.AddField("How to look for commands",
            $"1) Use the {px}cmds command to see all the categories\n2) use {px}cmds with the category name to glance at what commands it has. ex: `{px}cmds mod`\n3) Use {px}h with a command name to view its help. ex: `{px}h purge`");
        eb.AddField("Have any questions, or need my invite link?",
            "Support Server: https://discord.gg/mewdeko \nInvite Link: https://mewdeko.tech/invite");
        eb.AddField("Youtube Channel", "https://youtube.com/channel/UCKJEaaZMJQq6lH33L3b_sTg");
        eb.WithThumbnailUrl(
            "https://cdn.discordapp.com/emojis/968564817784877066.gif");
        eb.WithOkColor();
        await e.SendMessageAsync(embed: eb.Build())
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the help for a command
    /// </summary>
    /// <param name="com">The command in question</param>
    /// <param name="guild">The guild where this was executed</param>
    /// <param name="user">The user who executed the command</param>
    /// <returns>A tuple containing a <see cref="ComponentBuilder" /> and <see cref="EmbedBuilder" /></returns>
    public async Task<EmbedBuilder> GetCommandHelp(CommandInfo<SlashCommandParameterInfo> com, IGuild? guild,
        IGuildUser user)
    {
        var prefix = await guildSettings.GetPrefix(guild);
        var potentialCommand = interactionService.SlashCommands.FirstOrDefault(x =>
            string.Equals(x.Name, com.Name, StringComparison.CurrentCultureIgnoreCase));
        var em = new EmbedBuilder().AddField(fb =>
            fb.WithName(potentialCommand.Name).WithValue(potentialCommand.Description).WithIsInline(true));

        var reqs = GetCommandRequirements(com);
        var botReqs = GetCommandBotRequirements(com);
        if (reqs.Length > 0)
            em.AddField("User Permissions", string.Join("\n", reqs));
        if (botReqs.Length > 0)
            em.AddField("Bot Permissions", string.Join("\n", botReqs));

        if (potentialCommand is not null)
        {
            var globalCommands = await client.Rest.GetGlobalApplicationCommands();
            var guildCommands = await client.Rest.GetGuildApplicationCommands(guild.Id);
            var globalCommand = globalCommands.FirstOrDefault(x => x.Name == potentialCommand.Module.SlashGroupName);
            var guildCommand = guildCommands.FirstOrDefault(x => x.Name == potentialCommand.Module.SlashGroupName);
            if (globalCommand is not null)
                em.AddField("Slash Command",
                    potentialCommand == null
                        ? "`None`"
                        : $"</{potentialCommand.Module.SlashGroupName} {potentialCommand.Name}:{globalCommand.Id}>");
            else if (guildCommand is not null)
                em.AddField("Slash Command",
                    potentialCommand == null
                        ? "`None`"
                        : $"</{potentialCommand.Module.SlashGroupName} {potentialCommand.Name}:{guildCommand.Id}>");
        }

        em
            .WithFooter(
                $"Module: {com.Module.GetTopLevelModule().Name} || Submodule: {com.Module.Name.Replace("Commands", "")} || Method Name: {com.MethodName}")
            .WithColor(EeveeCore.OkColor);

        return em;
    }


    private static string[] GetCommandRequirements(CommandInfo<SlashCommandParameterInfo> cmd,
        GuildPermission? overrides = null)
    {
        var toReturn = new List<string>();

        if (cmd.Preconditions.Any(x => x is RequireAdminAttribute))
            toReturn.Add("Bot Owner Only");

        var userPerm =
            (RequireUserPermissionAttribute)cmd.Preconditions.FirstOrDefault(ca =>
                ca is RequireUserPermissionAttribute);

        var userPermString = string.Empty;
        if (userPerm is not null)
        {
            if (userPerm.ChannelPermission is { } cPerm)
                userPermString = GetPreconditionString(cPerm);
            if (userPerm.GuildPermission is { } gPerm)
                userPermString = GetPreconditionString(gPerm);
        }

        if (overrides is null)
        {
            if (!string.IsNullOrWhiteSpace(userPermString))
                toReturn.Add(userPermString);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(userPermString))
                toReturn.Add(Format.Strikethrough(userPermString));

            toReturn.Add(GetPreconditionString(overrides.Value));
        }

        return toReturn.ToArray();
    }

    private static string[] GetCommandBotRequirements(CommandInfo<SlashCommandParameterInfo> cmd)
    {
        var toReturn = new List<string>();

        if (cmd.Preconditions.Any(x => x is RequireAdminAttribute))
            toReturn.Add("Bot Owner Only");

        var botPerm =
            (RequireBotPermissionAttribute)cmd.Preconditions.FirstOrDefault(ca => ca is RequireBotPermissionAttribute)!;

        var botPermString = string.Empty;
        if (botPerm is not null)
        {
            if (botPerm.ChannelPermission is { } cPerm)
                botPermString = GetPreconditionString(cPerm);
            if (botPerm.GuildPermission is { } gPerm)
                botPermString = GetPreconditionString(gPerm);
        }

        if (!string.IsNullOrWhiteSpace(botPermString))
            toReturn.Add(botPermString);

        return toReturn.ToArray();
    }

    private static string FormatParameterType(Type type)
    {
        // Handle arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var formattedElementType = FormatParameterType(elementType);
            // Use triple underscores for arrays
            var underscores = new string('_', type.GetArrayRank() * 3);
            return $"{formattedElementType}{underscores}";
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var genericTypeName = genericTypeDef.FullName.Split('`')[0].Replace('+', '.').Replace('.', '_');
            var genericArgs = string.Join("_", type.GetGenericArguments().Select(FormatParameterType));
            return $"{genericTypeName}_{genericArgs}";
        }

        // Handle nested types and replace '+' with '.'
        var fullName = type.FullName.Replace('+', '.').Replace('.', '_');
        return fullName;
    }


    private static string GetPreconditionString(ChannelPermission perm)
    {
        return (perm + " Channel Permission").Replace("Guild", "Server", StringComparison.InvariantCulture);
    }

    private static string GetPreconditionString(GuildPermission perm)
    {
        return (perm + " Server Permission").Replace("Guild", "Server", StringComparison.InvariantCulture);
    }
}