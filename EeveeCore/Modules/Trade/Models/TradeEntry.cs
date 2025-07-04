namespace EeveeCore.Modules.Trade.Models;

/// <summary>
///     Represents the type of item being traded.
/// </summary>
public enum TradeItemType
{
    /// <summary>A Pokemon being traded</summary>
    Pokemon,
    
    /// <summary>MewCoins (credits) being traded</summary>
    Credits,
    
    /// <summary>Radiant tokens being traded</summary>
    Tokens
}

/// <summary>
///     Represents a single item in a trade session.
///     Can contain a Pokemon, credits, or tokens being offered by a user.
/// </summary>
public class TradeEntry
{
    /// <summary>
    ///     Gets or sets the unique identifier for this trade entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the Discord user ID of the person offering this item.
    /// </summary>
    public ulong OfferedBy { get; set; }

    /// <summary>
    ///     Gets or sets the type of item being traded.
    /// </summary>
    public TradeItemType ItemType { get; set; }

    /// <summary>
    ///     Gets or sets the Pokemon being traded (null if not a Pokemon trade).
    /// </summary>

    /// <summary>
    ///     Gets or sets the Pokemon ID being traded (for reference).
    /// </summary>
    public ulong? PokemonId { get; set; }

    /// <summary>
    ///     Gets or sets the number of credits being traded (0 if not a credit trade).
    /// </summary>
    public ulong Credits { get; set; }

    /// <summary>
    ///     Gets or sets the type of tokens being traded (null if not a token trade).
    /// </summary>
    public TokenType? TokenType { get; set; }

    /// <summary>
    ///     Gets or sets the number of tokens being traded (0 if not a token trade).
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when this entry was added to the trade.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the pokemon for the trade.
    /// </summary>
    public Database.Linq.Models.Pokemon.Pokemon? Pokemon { get; set; }

    /// <summary>
    ///     Creates a new trade entry for a Pokemon.
    /// </summary>
    /// <param name="offeredBy">The user offering the Pokemon.</param>
    /// <param name="pokemon">The Pokemon being offered.</param>
    /// <returns>A new TradeEntry for the Pokemon.</returns>
    public static TradeEntry ForPokemon(ulong offeredBy, Database.Linq.Models.Pokemon.Pokemon pokemon)
    {
        return new TradeEntry
        {
            OfferedBy = offeredBy,
            ItemType = TradeItemType.Pokemon,
            Pokemon = pokemon,
            PokemonId = pokemon.Id
        };
    }

    /// <summary>
    ///     Creates a new trade entry for credits.
    /// </summary>
    /// <param name="offeredBy">The user offering the credits.</param>
    /// <param name="credits">The number of credits being offered.</param>
    /// <returns>A new TradeEntry for the credits.</returns>
    public static TradeEntry ForCredits(ulong offeredBy, ulong credits)
    {
        return new TradeEntry
        {
            OfferedBy = offeredBy,
            ItemType = TradeItemType.Credits,
            Credits = credits
        };
    }

    /// <summary>
    ///     Creates a new trade entry for tokens.
    /// </summary>
    /// <param name="offeredBy">The user offering the tokens.</param>
    /// <param name="tokenType">The type of tokens being offered.</param>
    /// <param name="count">The number of tokens being offered.</param>
    /// <returns>A new TradeEntry for the tokens.</returns>
    public static TradeEntry ForTokens(ulong offeredBy, TokenType tokenType, int count)
    {
        return new TradeEntry
        {
            OfferedBy = offeredBy,
            ItemType = TradeItemType.Tokens,
            TokenType = tokenType,
            TokenCount = count
        };
    }

    /// <summary>
    ///     Gets a display string for this trade entry.
    /// </summary>
    /// <returns>A formatted string describing this trade entry.</returns>
    public string GetDisplayString()
    {
        return ItemType switch
        {
            TradeItemType.Pokemon => $"**{Pokemon?.PokemonName}** `(GlobalID:{PokemonId})`",
            TradeItemType.Credits => $"**{Credits:N0}** MewCoins",
            TradeItemType.Tokens => $"{TokenType?.GetEmoji()} **{TokenType?.GetDisplayName()}**: `{TokenCount}`",
            _ => "Unknown Item"
        };
    }

    /// <summary>
    ///     Gets a short summary string for this trade entry.
    /// </summary>
    /// <returns>A brief description of this trade entry.</returns>
    public string GetSummaryString()
    {
        return ItemType switch
        {
            TradeItemType.Pokemon => Pokemon?.PokemonName ?? "Unknown Pokemon",
            TradeItemType.Credits => $"{Credits:N0} credits",
            TradeItemType.Tokens => $"{TokenCount} {TokenType?.GetDisplayName()} tokens",
            _ => "Unknown"
        };
    }
}