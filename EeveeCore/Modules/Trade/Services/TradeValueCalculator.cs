using EeveeCore.Modules.Trade.Models;
using LinqToDB;
using TokenType = EeveeCore.Modules.Trade.Models.TokenType;

namespace EeveeCore.Modules.Trade.Services;

/// <summary>
///     Service for calculating the estimated value of items in trades for fraud detection.
///     This service provides fair value estimates for Pokemon, credits, and tokens.
/// </summary>
public class TradeValueCalculator : INService
{
    private readonly LinqToDbConnectionProvider _context;
    private readonly IDataCache _cache;

    // Base value constants for different item types
    private const decimal BaseShinyMultiplier = 2.5m;
    private const decimal BaseRadiantMultiplier = 10.0m;
    private const decimal BaseLevelMultiplier = 0.1m;
    private const decimal BaseIVMultiplier = 0.05m;
    private const decimal CreditBaseValue = 1.0m; // 1 credit = 1 value unit
    private const decimal TokenBaseValue = 1000.0m; // Base value per token

    /// <summary>
    ///     Initializes a new instance of the <see cref="TradeValueCalculator" /> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cache">The cache service.</param>
    public TradeValueCalculator(LinqToDbConnectionProvider context, IDataCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    ///     Calculates the estimated total value of all items in a trade session for a specific user.
    /// </summary>
    /// <param name="session">The trade session to analyze.</param>
    /// <param name="userId">The user whose items to value.</param>
    /// <returns>The estimated total value of the user's trade items.</returns>
    public async Task<decimal> CalculateUserTradeValueAsync(TradeSession session, ulong userId)
    {
        decimal totalValue = 0;

        // Calculate Pokemon values
        var pokemonEntries = session.GetPokemonBy(userId);
        foreach (var entry in pokemonEntries)
        {
            if (entry.PokemonId.HasValue)
            {
                var pokemonValue = await CalculatePokemonValueAsync(entry.PokemonId.Value);
                totalValue += pokemonValue;
            }
        }

        // Calculate credits value
        var creditsAmount = session.GetCreditsBy(userId);
        totalValue += creditsAmount * CreditBaseValue;

        // Calculate tokens value
        var tokens = session.GetTokensBy(userId);
        foreach (var (tokenType, count) in tokens)
        {
            var tokenValue = CalculateTokenValue(tokenType, count);
            totalValue += tokenValue;
        }

        return totalValue;
    }

    /// <summary>
    ///     Calculates the estimated value of a specific Pokemon.
    /// </summary>
    /// <param name="pokemonId">The ID of the Pokemon to value.</param>
    /// <returns>The estimated value of the Pokemon.</returns>
    public async Task<decimal> CalculatePokemonValueAsync(ulong pokemonId)
    {
        await using var db = await _context.GetConnectionAsync();

        var pokemon = await db.UserPokemon
            .Where(p => p.Id == pokemonId)
            .Select(p => new
            {
                p.PokemonName,
                p.Level,
                p.Shiny,
                p.Radiant,
                p.HpIv,
                p.AttackIv,
                p.DefenseIv,
                p.SpecialAttackIv,
                p.SpecialDefenseIv,
                p.SpeedIv,
                p.Champion,
                p.Voucher
            })
            .FirstOrDefaultAsync();

        if (pokemon == null)
        {
            return 0m;
        }

        // Get base species value
        var baseValue = await GetSpeciesBaseValueAsync(pokemon.PokemonName!);

        // Apply rarity multipliers
        if (pokemon.Radiant == true)
        {
            baseValue *= BaseRadiantMultiplier;
        }
        else if (pokemon.Shiny == true)
        {
            baseValue *= BaseShinyMultiplier;
        }

        // Apply level multiplier (higher level = more valuable)
        var levelMultiplier = 1 + (pokemon.Level * BaseLevelMultiplier);
        baseValue *= levelMultiplier;

        // Apply IV multiplier (better IVs = more valuable)
        var totalIVs = pokemon.HpIv + pokemon.AttackIv + pokemon.DefenseIv + 
                      pokemon.SpecialAttackIv + pokemon.SpecialDefenseIv + pokemon.SpeedIv;
        var ivPercentage = totalIVs / 186.0m; // Perfect IVs = 31*6 = 186
        var ivMultiplier = 1 + (ivPercentage * BaseIVMultiplier);
        baseValue *= ivMultiplier;

        // Apply special status multipliers
        if (pokemon.Champion)
        {
            baseValue *= 1.5m; // Champion Pokemon are 50% more valuable
        }

        if (pokemon.Voucher == true)
        {
            baseValue *= 1.2m; // Voucher Pokemon are 20% more valuable
        }

        return Math.Round(baseValue, 2);
    }

    /// <summary>
    ///     Gets the base value for a Pokemon species based on rarity and market data.
    /// </summary>
    /// <param name="speciesName">The name of the Pokemon species.</param>
    /// <returns>The base value for the species.</returns>
    private async Task<decimal> GetSpeciesBaseValueAsync(string speciesName)
    {
        // Try to get cached species value first
        var cacheKey = $"species_value:{speciesName.ToLower()}";
        var database = _cache.Redis.GetDatabase();
        var cachedValue = await database.StringGetAsync(cacheKey);
        
        if (cachedValue.HasValue && decimal.TryParse(cachedValue, out var parsed))
        {
            return parsed;
        }

        await using var db = await _context.GetConnectionAsync();

        // Calculate base value from market data and rarity
        var marketData = await db.Market
            .Join(db.UserPokemon, m => m.PokemonId, p => p.Id, (m, p) => new { m.Price, p.PokemonName })
            .Where(mp => mp.PokemonName == speciesName)
            .Select(mp => mp.Price)
            .ToListAsync();

        decimal baseValue;

        if (marketData.Any())
        {
            // Use market median as base value
            var sortedPrices = marketData.OrderBy(p => p).ToList();
            var median = sortedPrices.Count % 2 == 0
                ? (sortedPrices[sortedPrices.Count / 2 - 1] + sortedPrices[sortedPrices.Count / 2]) / 2m
                : sortedPrices[sortedPrices.Count / 2];
            
            baseValue = median;
        }
        else
        {
            // Fallback to species rarity-based value
            baseValue = await GetSpeciesRarityValueAsync(speciesName);
        }

        // Cache the result for 1 hour
        await database.StringSetAsync(cacheKey, baseValue.ToString(), TimeSpan.FromHours(1));

        return baseValue;
    }

    /// <summary>
    ///     Gets the rarity-based value for a Pokemon species when no market data is available.
    /// </summary>
    /// <param name="speciesName">The name of the Pokemon species.</param>
    /// <returns>The rarity-based value for the species.</returns>
    private async Task<decimal> GetSpeciesRarityValueAsync(string speciesName)
    {
        await using var db = await _context.GetConnectionAsync();


        // Check how rare the Pokemon is by counting total in circulation
        var totalCount = await db.UserPokemon
            .Where(p => p.PokemonName == speciesName)
            .CountAsync();

        // Rarer Pokemon (fewer in circulation) are more valuable
        // Base calculation: 10,000 / (total + 1) with minimum of 100
        var rarityValue = Math.Max(100m, 10000m / (totalCount + 1));

        // Apply special multipliers for legendary/mythical Pokemon (simple name-based detection)
        if (IsLegendaryPokemon(speciesName))
        {
            rarityValue *= 5.0m;
        }
        else if (IsMythicalPokemon(speciesName))
        {
            rarityValue *= 10.0m;
        }

        return Math.Round(rarityValue, 2);
    }

    /// <summary>
    ///     Calculates the value of a specific amount of tokens.
    /// </summary>
    /// <param name="tokenType">The type of token.</param>
    /// <param name="count">The number of tokens.</param>
    /// <returns>The estimated value of the tokens.</returns>
    private static decimal CalculateTokenValue(TokenType tokenType, int count)
    {
        // Different token types have different base values
        var multiplier = tokenType switch
        {
            TokenType.Fire => 1.0m,
            TokenType.Water => 1.0m,
            TokenType.Grass => 1.0m,
            TokenType.Electric => 1.0m,
            TokenType.Psychic => 1.2m, // Slightly more valuable
            TokenType.Ice => 1.1m,
            TokenType.Dragon => 1.5m, // More valuable
            TokenType.Dark => 1.3m,
            TokenType.Fairy => 1.4m,
            TokenType.Fighting => 1.0m,
            TokenType.Poison => 0.9m, // Slightly less valuable
            TokenType.Ground => 1.0m,
            TokenType.Flying => 1.0m,
            TokenType.Bug => 0.8m, // Less valuable
            TokenType.Rock => 1.0m,
            TokenType.Ghost => 1.2m,
            TokenType.Steel => 1.1m,
            TokenType.Normal => 0.7m, // Least valuable
            _ => 1.0m
        };

        return count * TokenBaseValue * multiplier;
    }

    /// <summary>
    ///     Determines if a Pokemon species is considered legendary.
    /// </summary>
    /// <param name="speciesName">The name of the Pokemon species.</param>
    /// <returns>True if the Pokemon is likely legendary.</returns>
    private static bool IsLegendaryPokemon(string speciesName)
    {
        // Simple name-based detection for legendary Pokemon
        var legendaryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Articuno", "Zapdos", "Moltres", "Mewtwo", "Raikou", "Entei", "Suicune", "Lugia", "Ho-Oh",
            "Regirock", "Regice", "Registeel", "Latios", "Latias", "Kyogre", "Groudon", "Rayquaza",
            "Uxie", "Mesprit", "Azelf", "Dialga", "Palkia", "Heatran", "Regigigas", "Giratina",
            "Cresselia", "Cobalion", "Terrakion", "Virizion", "Tornadus", "Thundurus", "Reshiram",
            "Zekrom", "Landorus", "Kyurem", "Xerneas", "Yveltal", "Zygarde", "Tapu Koko", "Tapu Lele",
            "Tapu Bulu", "Tapu Fini", "Cosmog", "Cosmoem", "Solgaleo", "Lunala", "Necrozma",
            "Zacian", "Zamazenta", "Eternatus", "Kubfu", "Urshifu", "Regieleki", "Regidrago",
            "Glastrier", "Spectrier", "Calyrex", "Koraidon", "Miraidon"
        };

        return legendaryNames.Contains(speciesName);
    }

    /// <summary>
    ///     Determines if a Pokemon species is considered mythical.
    /// </summary>
    /// <param name="speciesName">The name of the Pokemon species.</param>
    /// <returns>True if the Pokemon is likely mythical.</returns>
    private static bool IsMythicalPokemon(string speciesName)
    {
        // Simple name-based detection for mythical Pokemon
        var mythicalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Mew", "Celebi", "Jirachi", "Deoxys", "Phione", "Manaphy", "Darkrai", "Shaymin",
            "Arceus", "Victini", "Keldeo", "Meloetta", "Genesect", "Diancie", "Hoopa", "Volcanion",
            "Magearna", "Marshadow", "Zeraora", "Meltan", "Melmetal", "Zarude"
        };

        return mythicalNames.Contains(speciesName);
    }

    /// <summary>
    ///     Calculates the value imbalance between two sides of a trade.
    /// </summary>
    /// <param name="senderValue">The total value given by the sender.</param>
    /// <param name="receiverValue">The total value given by the receiver.</param>
    /// <returns>A value imbalance score from 0.0 (balanced) to 1.0 (extremely imbalanced).</returns>
    public static double CalculateValueImbalanceScore(decimal senderValue, decimal receiverValue)
    {
        if (senderValue == 0 && receiverValue == 0)
        {
            return 0.0; // No trade items
        }

        if (senderValue == 0 || receiverValue == 0)
        {
            return 0.4; // One-sided trade (gift) - moderate risk, not maximum
        }

        var ratio = (double)(Math.Max(senderValue, receiverValue) / Math.Min(senderValue, receiverValue));
        
        // Convert ratio to a 0-1 score
        // Ratio of 1.0 (balanced) = score 0.0
        // Ratio of 10.0 (very imbalanced) = score ~0.9
        // Ratio of 50.0+ (extremely imbalanced) = score ~1.0
        var score = Math.Min(1.0, (ratio - 1.0) / 49.0);
        
        return score;
    }
}