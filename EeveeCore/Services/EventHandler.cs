using Serilog;

namespace EeveeCore.Services;

/// <summary>
///     Provides asynchronous event handling for Discord.NET events while preserving gateway thread safety
/// </summary>
public sealed class EventHandler : IDisposable
{
    private readonly DiscordShardedClient _client;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventHandler" /> class.
    ///     Registers all Discord.NET event handlers and sets up asynchronous event handling.
    /// </summary>
    /// <param name="client">The Discord sharded client instance to handle events for.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client" /> is null.</exception>
    public EventHandler(DiscordShardedClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        RegisterEvents();
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _client.MessageReceived -= ClientOnMessageReceived;
            _client.UserJoined -= ClientOnUserJoined;
            _client.UserLeft -= ClientOnUserLeft;
            _client.MessageDeleted -= ClientOnMessageDeleted;
            _client.GuildMemberUpdated -= ClientOnGuildMemberUpdated;
            _client.MessageUpdated -= ClientOnMessageUpdated;
            _client.MessagesBulkDeleted -= ClientOnMessagesBulkDeleted;
            _client.UserBanned -= ClientOnUserBanned;
            _client.UserUnbanned -= ClientOnUserUnbanned;
            _client.UserVoiceStateUpdated -= ClientOnUserVoiceStateUpdated;
            _client.UserUpdated -= ClientOnUserUpdated;
            _client.ChannelCreated -= ClientOnChannelCreated;
            _client.ChannelDestroyed -= ClientOnChannelDestroyed;
            _client.ChannelUpdated -= ClientOnChannelUpdated;
            _client.RoleDeleted -= ClientOnRoleDeleted;
            _client.ReactionAdded -= ClientOnReactionAdded;
            _client.ReactionRemoved -= ClientOnReactionRemoved;
            _client.ReactionsCleared -= ClientOnReactionsCleared;
            _client.InteractionCreated -= ClientOnInteractionCreated;
            _client.UserIsTyping -= ClientOnUserIsTyping;
            _client.PresenceUpdated -= ClientOnPresenceUpdated;
            _client.JoinedGuild -= ClientOnJoinedGuild;
            _client.GuildScheduledEventCreated -= ClientOnEventCreated;
            _client.RoleUpdated -= ClientOnRoleUpdated;
            _client.GuildUpdated -= ClientOnGuildUpdated;
            _client.RoleCreated -= ClientOnRoleCreated;
            _client.ThreadCreated -= ClientOnThreadCreated;
            _client.ThreadUpdated -= ClientOnThreadUpdated;
            _client.ThreadDeleted -= ClientOnThreadDeleted;
            _client.ThreadMemberJoined -= ClientOnThreadMemberJoined;
            _client.ThreadMemberLeft -= ClientOnThreadMemberLeft;
            _client.AuditLogCreated -= ClientOnAuditLogCreated;
            _client.GuildAvailable -= ClientOnGuildAvailable;
            _client.LeftGuild -= ClientOnLeftGuild;
            _client.InviteCreated -= ClientOnInviteCreated;
            _client.InviteDeleted -= ClientOnInviteDeleted;

            Log.Information("Successfully unregistered all Discord event handlers");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while disposing AsyncEventHandler");
        }
    }

    /// <summary>
    ///     Wires every Discord client event to its corresponding <c>ClientOn*</c> bridge so user code can
    ///     subscribe to the strongly-typed events on this class instead.
    /// </summary>
    private void RegisterEvents()
    {
        _client.MessageReceived += ClientOnMessageReceived;
        _client.UserJoined += ClientOnUserJoined;
        _client.UserLeft += ClientOnUserLeft;
        _client.MessageDeleted += ClientOnMessageDeleted;
        _client.GuildMemberUpdated += ClientOnGuildMemberUpdated;
        _client.MessageUpdated += ClientOnMessageUpdated;
        _client.MessagesBulkDeleted += ClientOnMessagesBulkDeleted;
        _client.UserBanned += ClientOnUserBanned;
        _client.UserUnbanned += ClientOnUserUnbanned;
        _client.UserVoiceStateUpdated += ClientOnUserVoiceStateUpdated;
        _client.UserUpdated += ClientOnUserUpdated;
        _client.ChannelCreated += ClientOnChannelCreated;
        _client.ChannelDestroyed += ClientOnChannelDestroyed;
        _client.ChannelUpdated += ClientOnChannelUpdated;
        _client.RoleDeleted += ClientOnRoleDeleted;
        _client.ReactionAdded += ClientOnReactionAdded;
        _client.ReactionRemoved += ClientOnReactionRemoved;
        _client.ReactionsCleared += ClientOnReactionsCleared;
        _client.InteractionCreated += ClientOnInteractionCreated;
        _client.UserIsTyping += ClientOnUserIsTyping;
        _client.PresenceUpdated += ClientOnPresenceUpdated;
        _client.JoinedGuild += ClientOnJoinedGuild;
        _client.GuildScheduledEventCreated += ClientOnEventCreated;
        _client.RoleUpdated += ClientOnRoleUpdated;
        _client.GuildUpdated += ClientOnGuildUpdated;
        _client.RoleCreated += ClientOnRoleCreated;
        _client.ThreadCreated += ClientOnThreadCreated;
        _client.ThreadUpdated += ClientOnThreadUpdated;
        _client.ThreadDeleted += ClientOnThreadDeleted;
        _client.ThreadMemberJoined += ClientOnThreadMemberJoined;
        _client.ThreadMemberLeft += ClientOnThreadMemberLeft;
        _client.AuditLogCreated += ClientOnAuditLogCreated;
        _client.GuildAvailable += ClientOnGuildAvailable;
        _client.LeftGuild += ClientOnLeftGuild;
        _client.InviteCreated += ClientOnInviteCreated;
        _client.InviteDeleted += ClientOnInviteDeleted;
    }

    /// <summary>
    ///     Safely executes an event handler with error logging
    /// </summary>
    private static void SafeExecuteHandler(Func<Task> handlerAction, string eventName)
    {
        _ = ExecuteHandlerAsync(handlerAction, eventName);
    }

    /// <summary>
    ///     Awaits a handler action and logs any exception (and inner exception) it throws, tagged with the
    ///     originating event name. Used by <see cref="SafeExecuteHandler"/> to ensure subscriber faults
    ///     never propagate back into the Discord gateway loop.
    /// </summary>
    /// <param name="handlerAction">The handler invocation to await.</param>
    /// <param name="eventName">The event name used in error log messages.</param>
    /// <returns>A task that completes once the handler finishes (succeeded or failed).</returns>
    private static async Task ExecuteHandlerAsync(Func<Task> handlerAction, string eventName)
    {
        try
        {
            await handlerAction().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Error occurred in {EventName} handler: {ErrorMessage}",
                eventName,
                ex.Message);

            if (ex.InnerException != null)
                Log.Error(ex.InnerException,
                    "Inner exception in {EventName} handler: {ErrorMessage}",
                    eventName,
                    ex.InnerException.Message);
        }
    }

    #region Delegates

    /// <summary>
    ///     Represents an asynchronous event handler with a single parameter.
    /// </summary>
    /// <typeparam name="T">The type of the event argument.</typeparam>
    /// <param name="args">The event data.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public delegate Task AsyncEventHandler<in T>(T args);

    /// <summary>
    ///     Represents an asynchronous event handler with two parameters.
    /// </summary>
    /// <typeparam name="T1">The type of the first event argument.</typeparam>
    /// <typeparam name="T2">The type of the second event argument.</typeparam>
    /// <param name="args1">The first event argument.</param>
    /// <param name="args2">The second event argument.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public delegate Task AsyncEventHandler<in T1, in T2>(T1 args1, T2 args2);

    /// <summary>
    ///     Represents an asynchronous event handler with three parameters.
    /// </summary>
    /// <typeparam name="T1">The type of the first event argument.</typeparam>
    /// <typeparam name="T2">The type of the second event argument.</typeparam>
    /// <typeparam name="T3">The type of the third event argument.</typeparam>
    /// <param name="args1">The first event argument.</param>
    /// <param name="args2">The second event argument.</param>
    /// <param name="args3">The third event argument.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public delegate Task AsyncEventHandler<in T1, in T2, in T3>(T1 args1, T2 args2, T3 args3);

    /// <summary>
    ///     Represents an asynchronous event handler with four parameters.
    /// </summary>
    /// <typeparam name="T1">The type of the first event argument.</typeparam>
    /// <typeparam name="T2">The type of the second event argument.</typeparam>
    /// <typeparam name="T3">The type of the third event argument.</typeparam>
    /// <typeparam name="T4">The type of the fourth event argument.</typeparam>
    /// <param name="args1">The first event argument.</param>
    /// <param name="args2">The second event argument.</param>
    /// <param name="args3">The third event argument.</param>
    /// <param name="args4">The fourth event argument.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public delegate Task AsyncEventHandler<in T1, in T2, in T3, in T4>(T1 args1, T2 args2, T3 args3, T4 args4);

    #endregion

    #region Events

    /// <summary>
    ///     Occurs when a message is received in any channel the bot has access to.
    /// </summary>
    public event AsyncEventHandler<SocketMessage>? MessageReceived;

    /// <summary>
    ///     Occurs when an invite is created in a guild.
    /// </summary>
    public event AsyncEventHandler<IInvite>? InviteCreated;

    /// <summary>
    ///     Occurs when an invite is deleted from a guild channel.
    /// </summary>
    public event AsyncEventHandler<IGuildChannel, string>? InviteDeleted;

    /// <summary>
    ///     Occurs when a guild scheduled event is created.
    /// </summary>
    public event AsyncEventHandler<SocketGuildEvent>? EventCreated;

    /// <summary>
    ///     Occurs when a role is created in a guild.
    /// </summary>
    public event AsyncEventHandler<SocketRole>? RoleCreated;

    /// <summary>
    ///     Occurs when a guild's settings are updated.
    /// </summary>
    public event AsyncEventHandler<SocketGuild, SocketGuild>? GuildUpdated;

    /// <summary>
    ///     Occurs when a user joins a guild.
    /// </summary>
    public event AsyncEventHandler<IGuildUser>? UserJoined;

    /// <summary>
    ///     Occurs when a role is updated in a guild.
    /// </summary>
    public event AsyncEventHandler<SocketRole, SocketRole>? RoleUpdated;

    /// <summary>
    ///     Occurs when a user leaves a guild.
    /// </summary>
    public event AsyncEventHandler<IGuild, IUser>? UserLeft;

    /// <summary>
    ///     Occurs when a message is deleted.
    /// </summary>
    public event AsyncEventHandler<Cacheable<IMessage, ulong>, Cacheable<IMessageChannel, ulong>>? MessageDeleted;

    /// <summary>
    ///     Occurs when a guild member's information is updated.
    /// </summary>
    public event AsyncEventHandler<Cacheable<SocketGuildUser, ulong>, SocketGuildUser>? GuildMemberUpdated;

    /// <summary>
    ///     Occurs when a message is edited.
    /// </summary>
    public event AsyncEventHandler<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel>? MessageUpdated;

    /// <summary>
    ///     Occurs when multiple messages are deleted at once.
    /// </summary>
    public event AsyncEventHandler<IReadOnlyCollection<Cacheable<IMessage, ulong>>, Cacheable<IMessageChannel, ulong>>?
        MessagesBulkDeleted;

    /// <summary>
    ///     Occurs when a user is banned from a guild.
    /// </summary>
    public event AsyncEventHandler<SocketUser, SocketGuild>? UserBanned;

    /// <summary>
    ///     Occurs when a user is unbanned from a guild.
    /// </summary>
    public event AsyncEventHandler<SocketUser, SocketGuild>? UserUnbanned;

    /// <summary>
    ///     Occurs when a user's information is updated.
    /// </summary>
    public event AsyncEventHandler<SocketUser, SocketUser>? UserUpdated;

    /// <summary>
    ///     Occurs when a user's voice state changes.
    /// </summary>
    public event AsyncEventHandler<SocketUser, SocketVoiceState, SocketVoiceState>? UserVoiceStateUpdated;

    /// <summary>
    ///     Occurs when a channel is created in a guild.
    /// </summary>
    public event AsyncEventHandler<SocketChannel>? ChannelCreated;

    /// <summary>
    ///     Occurs when a channel is deleted from a guild.
    /// </summary>
    public event AsyncEventHandler<SocketChannel>? ChannelDestroyed;

    /// <summary>
    ///     Occurs when a channel's settings are updated.
    /// </summary>
    public event AsyncEventHandler<SocketChannel, SocketChannel>? ChannelUpdated;

    /// <summary>
    ///     Occurs when a role is deleted from a guild.
    /// </summary>
    public event AsyncEventHandler<SocketRole>? RoleDeleted;

    /// <summary>
    ///     Occurs when a reaction is added to a message.
    /// </summary>
    public event AsyncEventHandler<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction>?
        ReactionAdded;

    /// <summary>
    ///     Occurs when a reaction is removed from a message.
    /// </summary>
    public event AsyncEventHandler<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction>?
        ReactionRemoved;

    /// <summary>
    ///     Occurs when all reactions are removed from a message.
    /// </summary>
    public event AsyncEventHandler<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>>? ReactionsCleared;

    /// <summary>
    ///     Occurs when an interaction (slash command, button, etc.) is created.
    /// </summary>
    public event AsyncEventHandler<SocketInteraction>? InteractionCreated;

    /// <summary>
    ///     Occurs when a user starts typing in a channel.
    /// </summary>
    public event AsyncEventHandler<Cacheable<IUser, ulong>, Cacheable<IMessageChannel, ulong>>? UserIsTyping;

    /// <summary>
    ///     Occurs when a user's presence (status, activity) is updated.
    /// </summary>
    public event AsyncEventHandler<SocketUser, SocketPresence, SocketPresence>? PresenceUpdated;

    /// <summary>
    ///     Occurs when the bot joins a new guild.
    /// </summary>
    public event AsyncEventHandler<IGuild>? JoinedGuild;

    /// <summary>
    ///     Occurs when a thread is created in a guild.
    /// </summary>
    public event AsyncEventHandler<SocketThreadChannel>? ThreadCreated;

    /// <summary>
    ///     Occurs when a thread's settings are updated.
    /// </summary>
    public event AsyncEventHandler<Cacheable<SocketThreadChannel, ulong>, SocketThreadChannel>? ThreadUpdated;

    /// <summary>
    ///     Occurs when a thread is deleted from a guild.
    /// </summary>
    public event AsyncEventHandler<Cacheable<SocketThreadChannel, ulong>>? ThreadDeleted;

    /// <summary>
    ///     Occurs when a user joins a thread.
    /// </summary>
    public event AsyncEventHandler<SocketThreadUser>? ThreadMemberJoined;

    /// <summary>
    ///     Occurs when a user leaves a thread.
    /// </summary>
    public event AsyncEventHandler<SocketThreadUser>? ThreadMemberLeft;

    /// <summary>
    ///     Occurs when a new audit log entry is created.
    /// </summary>
    public event AsyncEventHandler<SocketAuditLogEntry, SocketGuild>? AuditLogCreated;

    /// <summary>
    ///     Occurs when a guild becomes available.
    /// </summary>
    public event AsyncEventHandler<SocketGuild>? GuildAvailable;

    /// <summary>
    ///     Occurs when the bot leaves or is removed from a guild.
    /// </summary>
    public event AsyncEventHandler<SocketGuild>? LeftGuild;

    #endregion

    #region Event Handlers

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="InviteDeleted"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="InviteDeleted"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="InviteDeleted"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnInviteDeleted(SocketGuildChannel arg1, string arg2)
    {
        if (InviteDeleted is not null)
            SafeExecuteHandler(() => InviteDeleted(arg1, arg2), nameof(InviteDeleted));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="InviteCreated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="InviteCreated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnInviteCreated(SocketInvite arg)
    {
        if (InviteCreated is not null)
            SafeExecuteHandler(() => InviteCreated(arg), nameof(InviteCreated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="LeftGuild"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="LeftGuild"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnLeftGuild(SocketGuild arg)
    {
        if (LeftGuild is not null)
            SafeExecuteHandler(() => LeftGuild(arg), nameof(LeftGuild));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="GuildAvailable"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="GuildAvailable"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnGuildAvailable(SocketGuild arg)
    {
        if (GuildAvailable is not null)
            SafeExecuteHandler(() => GuildAvailable(arg), nameof(GuildAvailable));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="AuditLogCreated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="AuditLogCreated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="AuditLogCreated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnAuditLogCreated(SocketAuditLogEntry arg1, SocketGuild arg2)
    {
        if (AuditLogCreated is not null)
            SafeExecuteHandler(() => AuditLogCreated(arg1, arg2), nameof(AuditLogCreated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ThreadMemberLeft"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="ThreadMemberLeft"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnThreadMemberLeft(SocketThreadUser arg)
    {
        if (ThreadMemberLeft is not null)
            SafeExecuteHandler(() => ThreadMemberLeft(arg), nameof(ThreadMemberLeft));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ThreadMemberJoined"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="ThreadMemberJoined"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnThreadMemberJoined(SocketThreadUser arg)
    {
        if (ThreadMemberJoined is not null)
            SafeExecuteHandler(() => ThreadMemberJoined(arg), nameof(ThreadMemberJoined));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ThreadDeleted"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="ThreadDeleted"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnThreadDeleted(Cacheable<SocketThreadChannel, ulong> arg)
    {
        if (ThreadDeleted is not null)
            SafeExecuteHandler(() => ThreadDeleted(arg), nameof(ThreadDeleted));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ThreadUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="ThreadUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="ThreadUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnThreadUpdated(Cacheable<SocketThreadChannel, ulong> arg1, SocketThreadChannel arg2)
    {
        if (ThreadUpdated is not null)
            SafeExecuteHandler(() => ThreadUpdated(arg1, arg2), nameof(ThreadUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ThreadCreated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="ThreadCreated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnThreadCreated(SocketThreadChannel arg)
    {
        if (ThreadCreated is not null)
            SafeExecuteHandler(() => ThreadCreated(arg), nameof(ThreadCreated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="JoinedGuild"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="JoinedGuild"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnJoinedGuild(SocketGuild arg)
    {
        if (JoinedGuild is not null)
            SafeExecuteHandler(() => JoinedGuild(arg), nameof(JoinedGuild));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="PresenceUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="PresenceUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="PresenceUpdated"/> event subscribers.</param>
    /// <param name="arg3">Forwarded to the <see cref="PresenceUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnPresenceUpdated(SocketUser arg1, SocketPresence arg2, SocketPresence arg3)
    {
        if (PresenceUpdated is not null)
            SafeExecuteHandler(() => PresenceUpdated(arg1, arg2, arg3), nameof(PresenceUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="UserIsTyping"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="UserIsTyping"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="UserIsTyping"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnUserIsTyping(Cacheable<IUser, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        if (UserIsTyping is not null)
            SafeExecuteHandler(() => UserIsTyping(arg1, arg2), nameof(UserIsTyping));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="InteractionCreated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="InteractionCreated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnInteractionCreated(SocketInteraction arg)
    {
        if (InteractionCreated is not null)
            SafeExecuteHandler(() => InteractionCreated(arg), nameof(InteractionCreated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ReactionsCleared"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="ReactionsCleared"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="ReactionsCleared"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnReactionsCleared(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        if (ReactionsCleared is not null)
            SafeExecuteHandler(() => ReactionsCleared(arg1, arg2), nameof(ReactionsCleared));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ReactionRemoved"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="ReactionRemoved"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="ReactionRemoved"/> event subscribers.</param>
    /// <param name="arg3">Forwarded to the <see cref="ReactionRemoved"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnReactionRemoved(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2,
        SocketReaction arg3)
    {
        if (ReactionRemoved is not null)
            SafeExecuteHandler(() => ReactionRemoved(arg1, arg2, arg3), nameof(ReactionRemoved));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ReactionAdded"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="ReactionAdded"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="ReactionAdded"/> event subscribers.</param>
    /// <param name="arg3">Forwarded to the <see cref="ReactionAdded"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2,
        SocketReaction arg3)
    {
        if (ReactionAdded is not null)
            SafeExecuteHandler(() => ReactionAdded(arg1, arg2, arg3), nameof(ReactionAdded));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="RoleDeleted"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="RoleDeleted"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnRoleDeleted(SocketRole arg)
    {
        if (RoleDeleted is not null)
            SafeExecuteHandler(() => RoleDeleted(arg), nameof(RoleDeleted));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ChannelUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="ChannelUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="ChannelUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnChannelUpdated(SocketChannel arg1, SocketChannel arg2)
    {
        if (ChannelUpdated is not null)
            SafeExecuteHandler(() => ChannelUpdated(arg1, arg2), nameof(ChannelUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ChannelDestroyed"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="ChannelDestroyed"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnChannelDestroyed(SocketChannel arg)
    {
        if (ChannelDestroyed is not null)
            SafeExecuteHandler(() => ChannelDestroyed(arg), nameof(ChannelDestroyed));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="ChannelCreated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="ChannelCreated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnChannelCreated(SocketChannel arg)
    {
        if (ChannelCreated is not null)
            SafeExecuteHandler(() => ChannelCreated(arg), nameof(ChannelCreated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="UserUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="UserUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="UserUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnUserUpdated(SocketUser arg1, SocketUser arg2)
    {
        if (UserUpdated is not null)
            SafeExecuteHandler(() => UserUpdated(arg1, arg2), nameof(UserUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="UserVoiceStateUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="UserVoiceStateUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="UserVoiceStateUpdated"/> event subscribers.</param>
    /// <param name="arg3">Forwarded to the <see cref="UserVoiceStateUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnUserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
    {
        if (UserVoiceStateUpdated is not null)
            SafeExecuteHandler(() => UserVoiceStateUpdated(arg1, arg2, arg3), nameof(UserVoiceStateUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="UserUnbanned"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="UserUnbanned"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="UserUnbanned"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnUserUnbanned(SocketUser arg1, SocketGuild arg2)
    {
        if (UserUnbanned is not null)
            SafeExecuteHandler(() => UserUnbanned(arg1, arg2), nameof(UserUnbanned));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="UserBanned"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="UserBanned"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="UserBanned"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnUserBanned(SocketUser arg1, SocketGuild arg2)
    {
        if (UserBanned is not null)
            SafeExecuteHandler(() => UserBanned(arg1, arg2), nameof(UserBanned));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="MessagesBulkDeleted"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="MessagesBulkDeleted"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="MessagesBulkDeleted"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnMessagesBulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> arg1,
        Cacheable<IMessageChannel, ulong> arg2)
    {
        if (MessagesBulkDeleted is not null)
            SafeExecuteHandler(() => MessagesBulkDeleted(arg1, arg2), nameof(MessagesBulkDeleted));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="MessageUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="MessageUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="MessageUpdated"/> event subscribers.</param>
    /// <param name="arg3">Forwarded to the <see cref="MessageUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnMessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (MessageUpdated is not null)
            SafeExecuteHandler(() => MessageUpdated(arg1, arg2, arg3), nameof(MessageUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="GuildMemberUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="GuildMemberUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="GuildMemberUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> arg1, SocketGuildUser arg2)
    {
        if (GuildMemberUpdated is not null)
            SafeExecuteHandler(() => GuildMemberUpdated(arg1, arg2), nameof(GuildMemberUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="MessageDeleted"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="MessageDeleted"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="MessageDeleted"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnMessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        if (MessageDeleted is not null)
            SafeExecuteHandler(() => MessageDeleted(arg1, arg2), nameof(MessageDeleted));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="UserLeft"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="UserLeft"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="UserLeft"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnUserLeft(SocketGuild arg1, SocketUser arg2)
    {
        if (UserLeft is not null)
            SafeExecuteHandler(() => UserLeft(arg1, arg2), nameof(UserLeft));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="UserJoined"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="UserJoined"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnUserJoined(SocketGuildUser arg)
    {
        if (UserJoined is not null)
            SafeExecuteHandler(() => UserJoined(arg), nameof(UserJoined));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="MessageReceived"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg">Forwarded to the <see cref="MessageReceived"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnMessageReceived(SocketMessage arg)
    {
        if (MessageReceived is not null)
            SafeExecuteHandler(() => MessageReceived(arg), nameof(MessageReceived));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="EventCreated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="args">Forwarded to the <see cref="EventCreated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnEventCreated(SocketGuildEvent args)
    {
        if (EventCreated is not null)
            SafeExecuteHandler(() => EventCreated(args), nameof(EventCreated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="RoleUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="RoleUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="RoleUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnRoleUpdated(SocketRole arg1, SocketRole arg2)
    {
        if (RoleUpdated is not null)
            SafeExecuteHandler(() => RoleUpdated(arg1, arg2), nameof(RoleUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="GuildUpdated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="arg1">Forwarded to the <see cref="GuildUpdated"/> event subscribers.</param>
    /// <param name="arg2">Forwarded to the <see cref="GuildUpdated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnGuildUpdated(SocketGuild arg1, SocketGuild arg2)
    {
        if (GuildUpdated is not null)
            SafeExecuteHandler(() => GuildUpdated(arg1, arg2), nameof(GuildUpdated));
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Bridges the Discord client's underlying event to the strongly-typed <see cref="RoleCreated"/> event,
    ///     dispatching to subscribers via <see cref="SafeExecuteHandler"/> so a failing subscriber cannot break the gateway loop.
    /// </summary>
    /// <param name="args">Forwarded to the <see cref="RoleCreated"/> event subscribers.</param>
    /// <returns>A completed task; subscribers run on a background task.</returns>
    private Task ClientOnRoleCreated(SocketRole args)
    {
        if (RoleCreated is not null)
            SafeExecuteHandler(() => RoleCreated(args), nameof(RoleCreated));
        return Task.CompletedTask;
    }

    #endregion
}