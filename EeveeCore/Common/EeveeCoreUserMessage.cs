// ReSharper disable NotNullMemberIsNotInitialized

namespace EeveeCore.Common;

/// <inheritdoc />
public class EeveeCoreMessage : IUserMessage
{
    /// <inheritdoc />
    public ulong Id => 0;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt => DateTime.Now;

    /// <inheritdoc />
    public Task DeleteAsync(RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task AddReactionAsync(IEmote emote, RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RemoveReactionAsync(IEmote emote, ulong userId, RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RemoveAllReactionsAsync(RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RemoveAllReactionsForEmoteAsync(IEmote emote, RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(IEmote emoji, int limit,
        RequestOptions? options = null,
        ReactionType type = ReactionType.Normal)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public MessageType Type => MessageType.Default;

    /// <inheritdoc />
    public MessageSource Source => MessageSource.User;

    /// <inheritdoc />
    public bool IsTTS => false;

    /// <inheritdoc />
    public bool IsPinned => false;

    /// <inheritdoc />
    public bool IsSuppressed => false;

    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    public string CleanContent { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset Timestamp => DateTimeOffset.Now;

    /// <inheritdoc />
    public DateTimeOffset? EditedTimestamp => DateTimeOffset.Now;

    /// <inheritdoc />
    public IMessageChannel Channel { get; set; } = null!;

    /// <inheritdoc />
    public IUser Author { get; set; } = null!;

    /// <inheritdoc />
    public IReadOnlyCollection<IAttachment> Attachments { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IEmbed> Embeds { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<ITag> Tags { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<ulong> MentionedChannelIds { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<ulong> MentionedRoleIds { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<ulong> MentionedUserIds { get; set; } = [];

    /// <inheritdoc />
    public bool MentionedEveryone { get; set; }

    /// <inheritdoc />
    public MessageActivity Activity { get; set; } = null!;

    /// <inheritdoc />
    public MessageApplication Application { get; set; } = null!;

    /// <inheritdoc />
    public MessageReference Reference { get; set; } = null!;

    /// <inheritdoc />
    public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions { get; set; } = new Dictionary<IEmote, ReactionMetadata>();

    /// <inheritdoc />
    public IReadOnlyCollection<IMessageComponent> Components { get; set; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IStickerItem> Stickers { get; set; } = [];

    /// <inheritdoc />
    public MessageFlags? Flags { get; set; }

    /// <inheritdoc />
    public IMessageInteraction Interaction { get; set; } = null!;

    /// <inheritdoc />
    public Task ModifyAsync(Action<MessageProperties> func, RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task PinAsync(RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task UnpinAsync(RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task CrosspostAsync(RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public string Resolve(
        TagHandling userHandling = TagHandling.Name,
        TagHandling channelHandling = TagHandling.Name,
        TagHandling roleHandling = TagHandling.Name,
        TagHandling everyoneHandling = TagHandling.Ignore,
        TagHandling emojiHandling = TagHandling.Name)
    {
        throw new NotImplementedException();
    }


    /// <inheritdoc />
    public Task EndPollAsync(RequestOptions? options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetPollAnswerVotersAsync(uint answerId, int? limit = null,
        ulong? afterId = null,
        RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public MessageResolvedData ResolvedData { get; } = null!;

    /// <inheritdoc />
    public IUserMessage ReferencedMessage { get; set; } = null!;

    /// <inheritdoc />
    public IMessageInteractionMetadata InteractionMetadata { get; } = null!;

    /// <inheritdoc />
    public IReadOnlyCollection<MessageSnapshot> ForwardedMessages { get; } = [];

    /// <inheritdoc />
    public Poll? Poll { get; } = null;

    /// <inheritdoc />
    public IThreadChannel Thread => throw new NotImplementedException();

    /// <inheritdoc />
    public MessageRoleSubscriptionData RoleSubscriptionData => throw new NotImplementedException();

    /// <inheritdoc />
    public PurchaseNotification PurchaseNotification { get; } = default;

    /// <inheritdoc />
    public MessageCallData? CallData { get; } = null;

    /// <inheritdoc />
    public IAsyncEnumerable<IReadOnlyCollection<IUser>> GetReactionUsersAsync(IEmote emoji, int limit,
        RequestOptions? options = null)
    {
        throw new NotImplementedException();
    }
}