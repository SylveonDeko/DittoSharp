namespace Ditto.Common.Logic;

public sealed class EvoReqs
{
    private readonly Dictionary<string, object> _raw;

    private EvoReqs(Dictionary<string, object> raw)
    {
        _raw = raw;
    }

    public static EvoReqs FromRaw(Dictionary<string, object> raw) => new(raw);

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

    public bool UsedActiveItem() => _raw.GetValueOrDefault("trigger_item_id") != null;

    public static bool operator >(EvoReqs left, double right) => left.Score > right;
    public static bool operator <(EvoReqs left, double right) => left.Score < right;
}