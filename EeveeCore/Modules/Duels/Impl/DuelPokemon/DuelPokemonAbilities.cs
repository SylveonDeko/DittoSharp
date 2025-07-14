namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Gets this Pokemon's effective ability, considering ability-ignoring effects from moves and abilities.
    ///     Returns the ability that should be used for calculations, which may be suppressed (returns 0) 
    ///     if the ability is being ignored by effects like Mold Breaker.
    /// </summary>
    /// <param name="attacker">The Pokemon using a move that might ignore this Pokemon's ability.</param>
    /// <param name="move">The move being used that might ignore this Pokemon's ability.</param>
    /// <returns>The effective ability, or 0 if the ability is being ignored.</returns>
    public Ability Ability(DuelPokemon attacker = null, Move.Move move = null)
    {
        // Currently there are two categories of ability ignores, and both only apply when a move is used.
        // Since this could change, the method signature is flexible. However, without both present, it
        // should not consider the existing options.
        if (move == null || attacker == null || attacker == this) return (Ability)AbilityId;
        if (!AbilityIgnorable()) return (Ability)AbilityId;
        if (move.Effect is 411 or 460) return 0;
        switch (attacker.AbilityId)
        {
            case (int)Impl.Ability.MOLD_BREAKER or (int)Impl.Ability.TURBOBLAZE or (int)Impl.Ability.TERAVOLT
                or (int)Impl.Ability.NEUTRALIZING_GAS:
            case (int)Impl.Ability.MYCELIUM_MIGHT when move.DamageClass == DamageClass.STATUS:
                return 0;
            default:
                return (Ability)AbilityId;
        }
    }

    /// <summary>
    ///     Returns True if this pokemon's current ability can be changed.
    ///     Certain signature abilities like Multitype (Arceus) and Stance Change (Aegislash) cannot be changed.
    /// </summary>
    /// <returns>True if the ability can be changed, false otherwise.</returns>
    public bool AbilityChangeable()
    {
        return AbilityId != (int)Impl.Ability.MULTITYPE &&
               AbilityId != (int)Impl.Ability.STANCE_CHANGE &&
               AbilityId != (int)Impl.Ability.SCHOOLING &&
               AbilityId != (int)Impl.Ability.COMATOSE &&
               AbilityId != (int)Impl.Ability.SHIELDS_DOWN &&
               AbilityId != (int)Impl.Ability.DISGUISE &&
               AbilityId != (int)Impl.Ability.RKS_SYSTEM &&
               AbilityId != (int)Impl.Ability.BATTLE_BOND &&
               AbilityId != (int)Impl.Ability.POWER_CONSTRUCT &&
               AbilityId != (int)Impl.Ability.ICE_FACE &&
               AbilityId != (int)Impl.Ability.GULP_MISSILE &&
               AbilityId != (int)Impl.Ability.ZERO_TO_HERO;
    }

    /// <summary>
    ///     Returns True if this pokemon's current ability can be given to another pokemon.
    ///     Abilities like Trace and form-changing abilities cannot be copied or given to other Pokemon.
    /// </summary>
    /// <returns>True if the ability can be given to another Pokemon, false otherwise.</returns>
    public bool AbilityGiveable()
    {
        return AbilityId != (int)Impl.Ability.TRACE &&
               AbilityId != (int)Impl.Ability.FORECAST &&
               AbilityId != (int)Impl.Ability.FLOWER_GIFT &&
               AbilityId != (int)Impl.Ability.ZEN_MODE &&
               AbilityId != (int)Impl.Ability.ILLUSION &&
               AbilityId != (int)Impl.Ability.IMPOSTER &&
               AbilityId != (int)Impl.Ability.POWER_OF_ALCHEMY &&
               AbilityId != (int)Impl.Ability.RECEIVER &&
               AbilityId != (int)Impl.Ability.DISGUISE &&
               AbilityId != (int)Impl.Ability.STANCE_CHANGE &&
               AbilityId != (int)Impl.Ability.POWER_CONSTRUCT &&
               AbilityId != (int)Impl.Ability.ICE_FACE &&
               AbilityId != (int)Impl.Ability.HUNGER_SWITCH &&
               AbilityId != (int)Impl.Ability.GULP_MISSILE &&
               AbilityId != (int)Impl.Ability.ZERO_TO_HERO;
    }

    /// <summary>
    ///     Returns True if this pokemon's current ability can be ignored by effects like Mold Breaker.
    ///     Abilities that provide defensive benefits or modify incoming effects can typically be ignored.
    /// </summary>
    /// <returns>True if the ability can be ignored, false otherwise.</returns>
    public bool AbilityIgnorable()
    {
        return AbilityId is (int)Impl.Ability.AROMA_VEIL or (int)Impl.Ability.BATTLE_ARMOR
            or (int)Impl.Ability.BIG_PECKS or (int)Impl.Ability.BULLETPROOF or (int)Impl.Ability.CLEAR_BODY
            or (int)Impl.Ability.CONTRARY or (int)Impl.Ability.DAMP or (int)Impl.Ability.DAZZLING
            or (int)Impl.Ability.DISGUISE or (int)Impl.Ability.DRY_SKIN or (int)Impl.Ability.FILTER
            or (int)Impl.Ability.FLASH_FIRE or (int)Impl.Ability.FLOWER_GIFT or (int)Impl.Ability.FLOWER_VEIL
            or (int)Impl.Ability.FLUFFY or (int)Impl.Ability.FRIEND_GUARD or (int)Impl.Ability.FUR_COAT
            or (int)Impl.Ability.HEATPROOF or (int)Impl.Ability.HEAVY_METAL or (int)Impl.Ability.HYPER_CUTTER
            or (int)Impl.Ability.ICE_FACE or (int)Impl.Ability.ICE_SCALES or (int)Impl.Ability.IMMUNITY
            or (int)Impl.Ability.INNER_FOCUS or (int)Impl.Ability.INSOMNIA or (int)Impl.Ability.KEEN_EYE
            or (int)Impl.Ability.LEAF_GUARD or (int)Impl.Ability.LEVITATE or (int)Impl.Ability.LIGHT_METAL
            or (int)Impl.Ability.LIGHTNING_ROD or (int)Impl.Ability.LIMBER or (int)Impl.Ability.MAGIC_BOUNCE
            or (int)Impl.Ability.MAGMA_ARMOR or (int)Impl.Ability.MARVEL_SCALE or (int)Impl.Ability.MIRROR_ARMOR
            or (int)Impl.Ability.MOTOR_DRIVE or (int)Impl.Ability.MULTISCALE or (int)Impl.Ability.OBLIVIOUS
            or (int)Impl.Ability.OVERCOAT or (int)Impl.Ability.OWN_TEMPO or (int)Impl.Ability.PASTEL_VEIL
            or (int)Impl.Ability.PUNK_ROCK or (int)Impl.Ability.QUEENLY_MAJESTY or (int)Impl.Ability.SAND_VEIL
            or (int)Impl.Ability.SAP_SIPPER or (int)Impl.Ability.SHELL_ARMOR or (int)Impl.Ability.SHIELD_DUST
            or (int)Impl.Ability.SIMPLE or (int)Impl.Ability.SNOW_CLOAK or (int)Impl.Ability.SOLID_ROCK
            or (int)Impl.Ability.SOUNDPROOF or (int)Impl.Ability.STICKY_HOLD or (int)Impl.Ability.STORM_DRAIN
            or (int)Impl.Ability.STURDY or (int)Impl.Ability.SUCTION_CUPS or (int)Impl.Ability.SWEET_VEIL
            or (int)Impl.Ability.TANGLED_FEET or (int)Impl.Ability.TELEPATHY or (int)Impl.Ability.THICK_FAT
            or (int)Impl.Ability.UNAWARE or (int)Impl.Ability.VITAL_SPIRIT or (int)Impl.Ability.VOLT_ABSORB
            or (int)Impl.Ability.WATER_ABSORB or (int)Impl.Ability.WATER_BUBBLE or (int)Impl.Ability.WATER_VEIL
            or (int)Impl.Ability.WHITE_SMOKE or (int)Impl.Ability.WONDER_GUARD or (int)Impl.Ability.WONDER_SKIN
            or (int)Impl.Ability.ARMOR_TAIL or (int)Impl.Ability.EARTH_EATER or (int)Impl.Ability.GOOD_AS_GOLD
            or (int)Impl.Ability.PURIFYING_SALT or (int)Impl.Ability.WELL_BAKED_BODY;
    }

    /// <summary>
    ///     Returns this pokemon's current weight.
    ///     Dynamically modifies the weight based on the ability of this pokemon.
    /// </summary>
    /// <param name="attacker">The attacking Pokemon (for ability checks).</param>
    /// <param name="move">The move being used (for ability checks).</param>
    /// <returns>The effective weight considering abilities and modifiers.</returns>
    public int Weight(DuelPokemon attacker = null, Move.Move move = null)
    {
        var curAbility = Ability(attacker, move);
        var curWeight = StartingWeight;
        switch (curAbility)
        {
            case Impl.Ability.HEAVY_METAL:
                curWeight *= 2;
                break;
            case Impl.Ability.LIGHT_METAL:
                curWeight /= 2;
                curWeight = Math.Max(1, curWeight);
                break;
        }

        curWeight -= Autotomize * 1000;
        curWeight = Math.Max(1, curWeight);
        return curWeight;
    }

    /// <summary>
    ///     Determines if this Pokemon is grounded (affected by Ground-type moves and certain field effects).
    ///     Considers abilities like Levitate, items like Air Balloon, and temporary effects.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="attacker">The attacking Pokemon (for ability checks).</param>
    /// <param name="move">The move being used (for ability checks).</param>
    /// <returns>True if the Pokemon is grounded, false if it's airborne.</returns>
    public bool Grounded(Battle battle, DuelPokemon attacker = null, Move.Move move = null)
    {
        if (battle.Gravity.Active()) return true;
        if (HeldItem.Get() == "iron-ball") return true;
        if (GroundedByMove) return true;
        if (TypeIds.Contains(ElementType.FLYING) && !Roost) return false;
        if (Ability(attacker, move) == Impl.Ability.LEVITATE) return false;
        if (HeldItem.Get() == "air-balloon") return false;
        if (MagnetRise.Active()) return false;
        return !Telekinesis.Active();
    }

    /// <summary>
    ///     Returns a Move that can be used with assist, or null if none exists.
    ///     This selects a random move from the pool of moves from pokes in the user's party that are eligable.
    /// </summary>
    /// <returns>A random move that can be used with Assist, or null if no valid moves exist.</returns>
    public Move.Move? GetAssistMove()
    {
        var moves = (from t in Owner.Party.Where((t, idx) => idx != Owner.LastIdx)
            from move in t.Moves
            where move.SelectableByAssist()
            select move).ToList();

        return moves.Count == 0 ? null : moves[new Random().Next(moves.Count)];
    }
}