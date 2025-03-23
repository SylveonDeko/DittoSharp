namespace EeveeCore.Modules.Duels.Impl.Move;

/// <summary>
///     Represents an instance of a move.
/// </summary>
public partial class Move
{
    private static readonly int[] SourceArray = new[] { 128, 154, 229, 347, 493 };

    public Move(IDictionary<string, object> moveData)
    {
        Id = Convert.ToInt32(moveData["id"]);
        Name = (string)moveData["identifier"];
        PrettyName = Name[..1].ToUpper() + Name[1..].Replace("-", " ");
        Power = moveData["power"] as int?;
        PP = Convert.ToInt32(moveData["pp"]);
        StartingPP = PP;
        Accuracy = moveData["accuracy"] as int?;
        Priority = Convert.ToInt32(moveData["priority"]);
        Type = (ElementType)Convert.ToInt32(moveData["type_id"]);
        DamageClass = (DamageClass)Convert.ToInt32(moveData["damage_class_id"]);
        Effect = Convert.ToInt32(moveData["effect_id"]);
        EffectChance = moveData["effect_chance"] as int?;
        Target = (MoveTarget)Convert.ToInt32(moveData["target_id"]);
        CritRate = Convert.ToInt32(moveData["crit_rate"]);
        MinHits = moveData["min_hits"] as int?;
        MaxHits = moveData["max_hits"] as int?;
        Used = false;
    }

    /// <summary>
    ///     Constructor that takes a MongoDB Move model and converts it to a game Move object
    /// </summary>
    public Move(Database.Models.Mongo.Pokemon.Move moveData)
    {
        Id = moveData.MoveId;
        Name = moveData.Identifier;
        PrettyName = Name[..1].ToUpper() + Name[1..].Replace("-", " ");
        Power = moveData.Power;
        PP = moveData.PP;
        StartingPP = PP;
        Accuracy = moveData.Accuracy;
        Priority = moveData.Priority;
        Type = (ElementType)moveData.TypeId;
        DamageClass = (DamageClass)moveData.DamageClassId;
        Effect = moveData.EffectId;
        EffectChance = moveData.EffectChance;
        Target = (MoveTarget)moveData.TargetId;
        CritRate = moveData.CritRate;
        MinHits = moveData.MinHits;
        MaxHits = moveData.MaxHits;
        Used = false;
    }

    /// <summary>
    ///     Copy constructor that can also override the Type property
    /// </summary>
    public Move(Move other)
    {
        Id = other.Id;
        Name = other.Name;
        PrettyName = other.PrettyName;
        Power = other.Power;
        PP = other.PP;
        StartingPP = other.StartingPP;
        Accuracy = other.Accuracy;
        Priority = other.Priority;
        Type = other.Type;
        DamageClass = other.DamageClass;
        Effect = other.Effect;
        EffectChance = other.EffectChance;
        Target = other.Target;
        CritRate = other.CritRate;
        MinHits = other.MinHits;
        MaxHits = other.MaxHits;
        Used = other.Used;
    }

    public int Id { get; }
    public string Name { get; }
    public string PrettyName { get; }
    public int? Power { get; }
    public int PP { get; set; }
    public int StartingPP { get; }
    public int? Accuracy { get; }
    public int Priority { get; }
    public ElementType Type { get; set; }
    public DamageClass DamageClass { get; }
    public int Effect { get; }
    public int? EffectChance { get; }
    public MoveTarget Target { get; }
    public int CritRate { get; }
    public int? MinHits { get; }
    public int? MaxHits { get; }
    public bool Used { get; set; }

    /// <summary>
    ///     Generate a copy of this move.
    /// </summary>
    public Move Copy()
    {
        return new Move(new Dictionary<string, object>
        {
            ["id"] = Id,
            ["identifier"] = Name,
            ["power"] = Power,
            ["pp"] = PP,
            ["accuracy"] = Accuracy,
            ["priority"] = Priority,
            ["type_id"] = Type,
            ["damage_class_id"] = DamageClass,
            ["effect_id"] = Effect,
            ["effect_chance"] = EffectChance,
            ["target_id"] = Target,
            ["crit_rate"] = CritRate,
            ["min_hits"] = MinHits,
            ["max_hits"] = MaxHits
        });
    }

    /// <summary>
    ///     Gets a random new type for attacker that is resistant to defender's last move type.
    /// </summary>
    /// <returns>A random possible type id, or null if there is no valid type.</returns>
    public static ElementType? GetConversion2(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        if (defender.LastMove == null) return null;

        var moveType = defender.LastMove.GetType(attacker, defender, battle);
        var newTypes = new HashSet<ElementType>();

        foreach (ElementType e in Enum.GetValues(typeof(ElementType)))
        {
            if (e == ElementType.TYPELESS) continue;

            if (battle.InverseBattle)
            {
                if (battle.TypeEffectiveness[(moveType, e)] > 100) newTypes.Add(e);
            }
            else
            {
                if (battle.TypeEffectiveness[(moveType, e)] < 100) newTypes.Add(e);
            }
        }

        // Remove existing types
        foreach (var t in attacker.TypeIds) newTypes.Remove(t);

        if (newTypes.Count == 0) return null;

        return newTypes.ElementAt(new Random().Next(newTypes.Count));
    }

    /// <summary>
    ///     Generate an instance of the move struggle.
    /// </summary>
    public static Move Struggle()
    {
        return new Move(new Dictionary<string, object>
        {
            ["id"] = 165,
            ["identifier"] = "struggle",
            ["power"] = 50,
            ["pp"] = 999999999999,
            ["accuracy"] = null,
            ["priority"] = 0,
            ["type_id"] = ElementType.TYPELESS,
            ["damage_class_id"] = 2,
            ["effect_id"] = 255,
            ["effect_chance"] = null,
            ["target_id"] = 10,
            ["crit_rate"] = 0,
            ["min_hits"] = null,
            ["max_hits"] = null
        });
    }

    /// <summary>
    ///     Generate an instance of the move confusion.
    /// </summary>
    public static Move Confusion()
    {
        return new Move(new Dictionary<string, object>
        {
            ["id"] = 0xCFCF,
            ["identifier"] = "confusion",
            ["power"] = 40,
            ["pp"] = 999999999999,
            ["accuracy"] = null,
            ["priority"] = 0,
            ["type_id"] = ElementType.TYPELESS,
            ["damage_class_id"] = DamageClass.PHYSICAL,
            ["effect_id"] = 1,
            ["effect_chance"] = null,
            ["target_id"] = 7,
            ["crit_rate"] = 0,
            ["min_hits"] = null,
            ["max_hits"] = null
        });
    }

    /// <summary>
    ///     Generate an instance of the move present.
    /// </summary>
    public static Move Present(int power)
    {
        return new Move(new Dictionary<string, object>
        {
            ["id"] = 217,
            ["identifier"] = "present",
            ["power"] = power,
            ["pp"] = 999999999999,
            ["accuracy"] = 90,
            ["priority"] = 0,
            ["type_id"] = ElementType.NORMAL,
            ["damage_class_id"] = DamageClass.PHYSICAL,
            ["effect_id"] = 123,
            ["effect_chance"] = null,
            ["target_id"] = 10,
            ["crit_rate"] = 0,
            ["min_hits"] = null,
            ["max_hits"] = null
        });
    }

    public override string ToString()
    {
        return $"Move(name={Name}, power={Power}, effect_id={Effect})";
    }
}