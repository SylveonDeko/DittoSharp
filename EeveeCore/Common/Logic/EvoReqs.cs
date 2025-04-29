namespace EeveeCore.Common.Logic;

/// <summary>
///     Represents the evolution requirements for a Pokémon with a scoring system to determine complexity.
/// </summary>
public sealed class EvoReqs
{
    private readonly Dictionary<string, object> _raw;

    /// <summary>
    ///     Creates a new instance of the <see cref="EvoReqs" /> class.
    /// </summary>
    /// <param name="raw">The raw dictionary containing evolution requirements data.</param>
    private EvoReqs(Dictionary<string, object> raw)
    {
        _raw = raw;
    }

    /// <summary>
    ///     Gets a numeric value representing the complexity of evolution requirements.
    ///     Higher scores indicate more complex evolution conditions.
    /// </summary>
    /// <remarks>
    ///     The score is calculated based on the presence of various evolution conditions:
    ///     - Trigger items, held items, and known moves add 2 points each
    ///     - Happiness requirements and gender requirements add 1 point each
    ///     - Physical stat comparisons add 1 point
    ///     - Level requirements and region requirements add 0.5 points each
    /// </remarks>
    public double Score
    {
        get
        {
            double score = 0;
            if (_raw.GetValueOrDefault("trigger_item_id") != null) score += 2;
            if (_raw.GetValueOrDefault("held_item_id") != null) score += 2;
            if (_raw.GetValueOrDefault("known_move_id") != null) score += 2;
            if (_raw.GetValueOrDefault("minimum_happiness") != null) score += 1;
            if (_raw.GetValueOrDefault("gender_id") != null) score += 1;
            if (_raw.GetValueOrDefault("relative_physical_stats") != null) score += 1;
            if (_raw.GetValueOrDefault("minimum_level") != null) score += 0.5;
            if (_raw.GetValueOrDefault("region") != null) score += 0.5;
            return score;
        }
    }

    /// <summary>
    ///     Creates an <see cref="EvoReqs" /> instance from a raw dictionary of evolution data.
    /// </summary>
    /// <param name="raw">The raw dictionary containing evolution requirements data.</param>
    /// <returns>A new <see cref="EvoReqs" /> instance.</returns>
    public static EvoReqs FromRaw(Dictionary<string, object> raw)
    {
        return new EvoReqs(raw);
    }

    /// <summary>
    ///     Determines whether this evolution requires the use of an active item (evolution stone or similar).
    /// </summary>
    /// <returns><c>true</c> if an active item is required; otherwise, <c>false</c>.</returns>
    public bool UsedActiveItem()
    {
        return _raw.GetValueOrDefault("trigger_item_id") != null;
    }

    /// <summary>
    ///     Compares the complexity score of evolution requirements against a numeric value.
    /// </summary>
    /// <param name="left">The evolution requirements to compare.</param>
    /// <param name="right">The numeric value to compare against.</param>
    /// <returns><c>true</c> if the evolution requirements score is greater than the specified value; otherwise, <c>false</c>.</returns>
    public static bool operator >(EvoReqs left, double right)
    {
        return left.Score > right;
    }

    /// <summary>
    ///     Compares the complexity score of evolution requirements against a numeric value.
    /// </summary>
    /// <param name="left">The evolution requirements to compare.</param>
    /// <param name="right">The numeric value to compare against.</param>
    /// <returns><c>true</c> if the evolution requirements score is less than the specified value; otherwise, <c>false</c>.</returns>
    public static bool operator <(EvoReqs left, double right)
    {
        return left.Score < right;
    }
}