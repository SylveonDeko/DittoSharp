namespace EeveeCore.Modules.Duels.Impl.Move;

/// <summary>
///     Represents an instance of a move that can be used in Pokémon battles.
///     Each move has attributes like power, accuracy, type, and special effects.
/// </summary>
public partial class Move
{
    private static readonly int[] SourceArray = [128, 154, 229, 347, 493];

    /// <summary>
    ///     Initializes a new instance of the Move class using a dictionary of move data.
    /// </summary>
    /// <param name="moveData">A dictionary containing the move's attributes and properties.</param>
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
    ///     Initializes a new instance of the Move class from a MongoDB Move model.
    ///     Converts a database model into a game Move object.
    /// </summary>
    /// <param name="moveData">The MongoDB Move model to convert.</param>
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
    ///     Copy constructor that creates a new Move instance from an existing one.
    /// </summary>
    /// <param name="other">The Move instance to copy.</param>
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

    /// <summary>
    ///     Gets the unique identifier for this move.
    /// </summary>
    public int Id { get; }

    /// <summary>
    ///     Gets the internal name identifier for this move.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the formatted display name for this move, with proper capitalization and spacing.
    /// </summary>
    public string PrettyName { get; }

    /// <summary>
    ///     Gets the base power of this move, which determines damage output.
    ///     Null for moves that don't directly deal damage.
    /// </summary>
    public int? Power { get; }

    /// <summary>
    ///     Gets or sets the current Power Points (PP) remaining for this move.
    ///     PP is consumed when the move is used.
    /// </summary>
    public int PP { get; set; }

    /// <summary>
    ///     Gets the initial Power Points (PP) value for this move.
    /// </summary>
    public int StartingPP { get; }

    /// <summary>
    ///     Gets the accuracy percentage of this move.
    ///     Null for moves that always hit.
    /// </summary>
    public int? Accuracy { get; }

    /// <summary>
    ///     Gets the priority value of this move, which affects move order in battle.
    ///     Higher values go first.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    ///     Gets or sets the elemental type of this move.
    ///     The type determines effectiveness against different Pokémon types.
    /// </summary>
    public ElementType Type { get; set; }

    /// <summary>
    ///     Gets the damage class of this move (Physical, Special, or Status).
    ///     Determines which stats are used in damage calculation.
    /// </summary>
    public DamageClass DamageClass { get; }

    /// <summary>
    ///     Gets the effect ID for this move, determining its special effects.
    /// </summary>
    public int Effect { get; }

    /// <summary>
    ///     Gets the percentage chance for the move's secondary effect to activate.
    ///     Null for moves without a chance-based effect.
    /// </summary>
    public int? EffectChance { get; }

    /// <summary>
    ///     Gets the targeting behavior of this move (single opponent, all opponents, user, etc.).
    /// </summary>
    public MoveTarget Target { get; }

    /// <summary>
    ///     Gets the critical hit rate modifier for this move.
    ///     Higher values increase the chance of landing a critical hit.
    /// </summary>
    public int CritRate { get; }

    /// <summary>
    ///     Gets the minimum number of hits for multi-hit moves.
    ///     Null for moves that hit only once.
    /// </summary>
    public int? MinHits { get; }

    /// <summary>
    ///     Gets the maximum number of hits for multi-hit moves.
    ///     Null for moves that hit only once.
    /// </summary>
    public int? MaxHits { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether this move has been used in the current turn.
    /// </summary>
    public bool Used { get; set; }

    /// <summary>
    ///     Creates a new instance of this move with identical properties.
    /// </summary>
    /// <returns>A copy of this move.</returns>
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
    ///     Gets a random new type for the Conversion2 move that is resistant to the defender's last move type.
    /// </summary>
    /// <param name="attacker">The Pokémon using Conversion2.</param>
    /// <param name="defender">The opposing Pokémon whose last move is being countered.</param>
    /// <param name="battle">The current battle context.</param>
    /// <returns>A random element type resistant to the defender's last move, or null if no valid type is found.</returns>
    public static ElementType? GetConversion2(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
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
    ///     Creates an instance of the Struggle move, which is used when a Pokémon has no PP left in any moves.
    /// </summary>
    /// <returns>A Move instance representing Struggle.</returns>
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
    ///     Creates an instance of the Confusion move, which is used when a Pokémon hits itself in confusion.
    /// </summary>
    /// <returns>A Move instance representing the self-inflicted Confusion damage.</returns>
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
    ///     Creates an instance of the Present move with the specified power.
    ///     Present's power varies randomly between calls.
    /// </summary>
    /// <param name="power">The power value to use for this instance of Present.</param>
    /// <returns>A Move instance representing Present with the specified power.</returns>
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

    /// <summary>
    ///     Returns a string representation of this move.
    /// </summary>
    /// <returns>A string containing the move's name, power, and effect ID.</returns>
    public override string ToString()
    {
        return $"Move(name={Name}, power={Power}, effect_id={Effect})";
    }
}