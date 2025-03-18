#nullable enable
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Fergun.Interactive;
using Serilog;
using ModuleInfo = Discord.Commands.ModuleInfo;

namespace Ditto.Extensions;

public static class Extensions
{
    public static readonly Regex UrlRegex = new(@"^(https?|ftp)://(?<path>[^\s/$.?#].[^\s]*)$", RegexOptions.Compiled);

    public static TOut[] Map<TIn, TOut>(this TIn[] arr, Func<TIn, TOut> f) => Array.ConvertAll(arr, x => f(x));

    public static async Task SendConfirmAsync(this IDiscordInteraction interaction, string? message)
        => await interaction.RespondAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build()).ConfigureAwait(false);

    public static async Task SendEphemeralConfirmAsync(this IDiscordInteraction interaction, string message)
        => await interaction.RespondAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build(), ephemeral: true).ConfigureAwait(false);

    public static async Task SendErrorAsync(this IDiscordInteraction interaction, string? message)
        => await interaction.RespondAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build(), components: new ComponentBuilder()
            .WithButton(label: "Support Server", style: ButtonStyle.Link, url: "https://discord.gg/Ditto").Build()).ConfigureAwait(false);

    public static async Task SendEphemeralErrorAsync(this IDiscordInteraction interaction, string? message)
        => await interaction.RespondAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build(), ephemeral: true, components: new ComponentBuilder()
            .WithButton(label: "Support Server", style: ButtonStyle.Link, url: "https://discord.gg/Ditto").Build()).ConfigureAwait(false);

    public static async Task<IUserMessage> SendConfirmFollowupAsync(this IDiscordInteraction interaction, string message)
        => await interaction.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build()).ConfigureAwait(false);

    public static async Task<IUserMessage> SendConfirmFollowupAsync(this IDiscordInteraction interaction, string message, ComponentBuilder builder)
        => await interaction.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build(), components: builder.Build()).ConfigureAwait(false);

    public static async Task<IUserMessage> SendEphemeralFollowupConfirmAsync(this IDiscordInteraction interaction, string message)
        => await interaction.FollowupAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(message).Build(), ephemeral: true).ConfigureAwait(false);

    public static async Task<IUserMessage> SendErrorFollowupAsync(this IDiscordInteraction interaction, string message)
        => await interaction.FollowupAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build(), components: new ComponentBuilder()
            .WithButton(label: "Support Server", style: ButtonStyle.Link, url: "https://discord.gg/Ditto").Build()).ConfigureAwait(false);

    public static async Task<IUserMessage> SendEphemeralFollowupErrorAsync(this IDiscordInteraction interaction, string message)
        => await interaction.FollowupAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build(), ephemeral: true, components: new ComponentBuilder()
            .WithButton(label: "Support Server", style: ButtonStyle.Link, url: "https://discord.gg/Ditto").Build()).ConfigureAwait(false);

    public static List<ulong> GetGuildIds(this DiscordSocketClient client) => client.Guilds.Select(x => x.Id).ToList();

    // ReSharper disable once InvalidXmlDocComment
    /// Generates a string in the format 00:mm:ss if timespan is less than 2m.
    /// </summary>
    /// <param name="span">Timespan to convert to string</param>
    /// <returns>Formatted duration string</returns>
    public static string ToPrettyStringHm(this TimeSpan span)
    {
        if (span < TimeSpan.FromMinutes(2))
            return $"{span:mm}m {span:ss}s";
        return $"{(int)span.TotalHours:D2}h {span:mm}m";
    }

    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        foreach (var i in items) list.Add(i);
    }

    public static void RemoveRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        foreach (var i in items) list.Remove(i);
    }

    public static bool TryGetUrlPath(this string input, out string path)
    {
        var match = UrlRegex.Match(input);
        if (match.Success)
        {
            path = match.Groups["path"].Value;
            return true;
        }

        path = string.Empty;
        return false;
    }

    public static IEmote? ToIEmote(this string emojiStr) =>
        Emote.TryParse(emojiStr, out var maybeEmote)
            ? maybeEmote
            : new Emoji(emojiStr);

    public static bool TryToIEmote(this string emojiStr, out IEmote value)
    {
        value = Emote.TryParse(emojiStr, out var emoteValue)
            ? emoteValue
            : Emoji.TryParse(emojiStr, out var emojiValue)
                ? emojiValue
                : null;
        return value is not null;
    }

    /// <summary>
    ///     First 10 characters of teh bot token.
    /// </summary>
    public static string RedisKey(this IBotCredentials bc) => bc.Token[..10];

    public static async Task<string?> ReplaceAsync(this Regex regex, string? input,
        Func<Match, Task<string>> replacementFn)
    {
        var sb = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in regex.Matches(input))
        {
            sb.Append(input, lastIndex, match.Index - lastIndex)
                .Append(await replacementFn(match).ConfigureAwait(false));

            lastIndex = match.Index + match.Length;
        }

        sb.Append(input, lastIndex, input.Length - lastIndex);
        return sb.ToString();
    }

    public static void ThrowIfNull<T>(this T o, string name) where T : class
    {
        if (o == null)
            throw new ArgumentNullException(nameof(o));
    }

    public static bool IsAuthor(this IMessage msg, IDiscordClient client) => msg.Author?.Id == client.CurrentUser.Id;

    public static string GetFullUsage(string commandName, string args, string? prefix) =>
        $"{prefix}{commandName} {(StringExtensions.TryFormat(args, [prefix], out var output) ? output : args)}";

    public static EmbedBuilder AddPaginatedFooter(this EmbedBuilder embed, int curPage, int? lastPage)
    {
        if (lastPage != null)
            return embed.WithFooter(efb => efb.WithText($"{curPage + 1} / {lastPage + 1}"));
        return embed.WithFooter(efb => efb.WithText(curPage.ToString()));
    }

    public static EmbedBuilder WithOkColor(this EmbedBuilder eb) => eb.WithColor(Ditto.OkColor);

    public static EmbedBuilder WithErrorColor(this EmbedBuilder eb) => eb.WithColor(Ditto.ErrorColor);

    public static PageBuilder WithOkColor(this PageBuilder eb) => eb.WithColor(Ditto.OkColor);

    public static PageBuilder WithErrorColor(this PageBuilder eb) => eb.WithColor(Ditto.ErrorColor);

    public static HttpClient AddFakeHeaders(this HttpClient http)
    {
        AddFakeHeaders(http.DefaultRequestHeaders);
        return http;
    }

    public static void AddFakeHeaders(this HttpHeaders dict)
    {
        dict.Clear();
        dict.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        dict.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1");
    }

    public static void DeleteAfter(this IUserMessage? msg, int seconds)
    {
        if (msg is null) return;

        Task.Run(async () =>
        {
            await Task.Delay(seconds * 1000).ConfigureAwait(false);
            try
            {
                await msg.DeleteAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        });
    }

    public static void DeleteAfter(this IMessage? msg, int seconds)
    {
        if (msg is null) return;

        Task.Run(async () =>
        {
            await Task.Delay(seconds * 1000).ConfigureAwait(false);
            try
            {
                await msg.DeleteAsync().ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
        });
    }

    public static ModuleInfo GetTopLevelModule(this ModuleInfo module)
    {
        while (module.Parent != null) module = module.Parent;
        return module;
    }

    public static async Task<IEnumerable<IGuildUser>> GetMembersAsync(this IRole role) =>
        (await role.Guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false)).Where(u =>
            u.RoleIds.Contains(role.Id));

    public static Stream ToStream(this IEnumerable<byte> bytes, bool canWrite = false)
    {
        var ms = new MemoryStream(bytes as byte[] ?? bytes.ToArray(), canWrite);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public static IEnumerable<IRole> GetRoles(this IGuildUser user) => user.RoleIds.Select(r => user.Guild.GetRole(r)).Where(r => r != null);

    public static bool IsImage(this HttpResponseMessage msg) => IsImage(msg, out _);

    public static bool IsImage(this HttpResponseMessage msg, out string? mimeType)
    {
        if (msg.Content.Headers.ContentType != null) _ = msg.Content.Headers.ContentType.MediaType;
        mimeType = msg.Content.Headers.ContentType.MediaType;
        return mimeType is "image/png" or "image/jpeg" or "image/gif";
    }

    public static IEnumerable<Type> LoadFrom(this IServiceCollection collection, Assembly assembly)
    {
        // list of all the types which are added with this method
        var addedTypes = new List<Type>();

        Type[] allTypes;
        try
        {
            // first, get all types in te assembly
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Log.Error(ex, "Error loading assembly types");
            return [];
        }

        // all types which have INService implementation are services
        // which are supposed to be loaded with this method
        // ignore all interfaces and abstract classes
        var services = new Queue<Type>(allTypes
            .Where(x => x.GetInterfaces().Contains(typeof(INService))
                        && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract
#if GLOBAL_Ditto
                        && x.GetTypeInfo().GetCustomAttribute<NoPublicBotAttribute>() == null
#endif
            )
            .ToArray());

        // we will just return those types when we're done instantiating them
        addedTypes.AddRange(services);

        // get all interfaces which inherit from INService
        // as we need to also add a service for each one of interfaces
        // so that DI works for them too
        var interfaces = new HashSet<Type>(allTypes
            .Where(x => x.GetInterfaces().Contains(typeof(INService))
                        && x.GetTypeInfo().IsInterface));

        // keep instantiating until we've instantiated them all
        while (services.Count > 0)
        {
            var serviceType = services.Dequeue(); //get a type i need to add

            if (collection.FirstOrDefault(x => x.ServiceType == serviceType) !=
                null) // if that type is already added, skip
            {
                continue;
            }

            //also add the same type
            var interfaceType = interfaces.FirstOrDefault(x => serviceType.GetInterfaces().Contains(x));
            if (interfaceType != null)
            {
                addedTypes.Add(interfaceType);
                collection.AddSingleton(interfaceType, serviceType);
            }
            else
            {
                collection.AddSingleton(serviceType, serviceType);
            }
        }

        return addedTypes;
    }

    // public static SlashCommandOptionBuilder AddOptions(this SlashCommandOptionBuilder builder, IEnumerable<SlashCommandOptionBuilder> options)
    // {
    //     foreach (var option in options)
    //     {
    //         builder.AddOption(option);
    //     }
    //
    //     return builder;
    // }

    public static string[] GetCtNames(this IApplicationCommand command)
    {
        var baseName = command.Name;
        var sgs = command.Options.Where(x =>
            x.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup);

        if (!sgs.Any())
            return
            [
                baseName
            ];

        var ctNames = new List<string>();
        foreach (var sg in sgs)
            if (sg.Type == ApplicationCommandOptionType.SubCommand)
                ctNames.Add(baseName + " " + sg.Name);
            else
                ctNames.AddRange(sg.Options.Select(x => baseName + " " + sg.Name + " " + x.Name));

        return ctNames.ToArray();
    }

    public static string GetRealName(this SocketInteraction interaction)
    {
        switch (interaction)
        {
            case SocketUserCommand uCmd:
                return uCmd.Data.Name;
            case SocketMessageCommand mCmd:
                return mCmd.Data.Name;
            default:
            {
                if (interaction is not SocketSlashCommand sCmd) throw new ArgumentException("interaction is not a valid type");
                return (sCmd.Data.Name
                        + " "
                        + ((sCmd.Data.Options?.FirstOrDefault()?.Type is ApplicationCommandOptionType.SubCommand
                               or ApplicationCommandOptionType.SubCommandGroup
                               ? sCmd.Data.Options?.First().Name
                               : "")
                           ?? "")
                        + " "
                        + (sCmd.Data.Options?.FirstOrDefault()?.Options?.FirstOrDefault()?.Type
                           == ApplicationCommandOptionType.SubCommand
                            ? sCmd.Data.Options?.FirstOrDefault()?.Options?.FirstOrDefault()?.Name
                            : "")
                        ?? "").Trim();
            }
        }
    }
}