namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Helper method to call append_stat for attack.
    /// </summary>
    /// <param name="stageChange">The amount to change the attack stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change.</returns>
    public string AppendAttack(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "attack", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for defense.
    /// </summary>
    /// <param name="stageChange">The amount to change the defense stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change.</returns>
    public string AppendDefense(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "defense", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for special attack.
    /// </summary>
    /// <param name="stageChange">The amount to change the special attack stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change.</returns>
    public string AppendSpAtk(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "special attack", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for special defense.
    /// </summary>
    /// <param name="stageChange">The amount to change the special defense stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change.</returns>
    public string AppendSpDef(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "special defense", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for speed.
    /// </summary>
    /// <param name="stageChange">The amount to change the speed stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change.</returns>
    public string AppendSpeed(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "speed", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for accuracy.
    /// </summary>
    /// <param name="stageChange">The amount to change the accuracy stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change.</returns>
    public string AppendAccuracy(int stageChange, DuelPokemon attacker = null, Move.Move move = null,
        string source = "", bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "accuracy", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for evasion.
    /// </summary>
    /// <param name="stageChange">The amount to change the evasion stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change.</returns>
    public string AppendEvasion(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "evasion", source, checkLooping);
    }

    /// <summary>
    ///     Adds a stat stage change to this pokemon.
    ///     Returns a formatted string describing the stat change.
    /// </summary>
    /// <param name="stageChange">The amount to change the stat stage.</param>
    /// <param name="attacker">The attacking Pokemon causing the change.</param>
    /// <param name="move">The move causing the change.</param>
    /// <param name="stat">The name of the stat being changed.</param>
    /// <param name="source">A description of the source of the change.</param>
    /// <param name="checkLooping">Whether to check for looping abilities like Opportunist.</param>
    /// <returns>A formatted message describing the stat change and any resulting effects.</returns>
    public string AppendStat(int stageChange, DuelPokemon attacker, Move.Move move, string stat, string source,
        bool checkLooping = true)
    {
        var msg = "";
        if (Substitute > 0 && attacker != this && attacker != null &&
            (move == null || move.IsAffectedBySubstitute())) return "";
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        var deltaMessages = new Dictionary<int, string>
        {
            { -3, $"{Name}'s {stat} severely fell{source}!\n" },
            { -2, $"{Name}'s {stat} harshly fell{source}!\n" },
            { -1, $"{Name}'s {stat} fell{source}!\n" },
            { 1, $"{Name}'s {stat} rose{source}!\n" },
            { 2, $"{Name}'s {stat} rose sharply{source}!\n" },
            { 3, $"{Name}'s {stat} rose drastically{source}!\n" }
        };
        var delta = stageChange;
        if (Ability(attacker, move) == Impl.Ability.SIMPLE) delta *= 2;
        if (Ability(attacker, move) == Impl.Ability.CONTRARY) delta *= -1;

        int currentStage;
        switch (stat)
        {
            case "attack":
                currentStage = AttackStage;
                break;
            case "defense":
                currentStage = DefenseStage;
                break;
            case "special attack":
                currentStage = SpAtkStage;
                break;
            case "special defense":
                currentStage = SpDefStage;
                break;
            case "speed":
                currentStage = SpeedStage;
                break;
            case "accuracy":
                currentStage = AccuracyStage;
                break;
            case "evasion":
                currentStage = EvasionStage;
                break;
            default:
                throw new ArgumentException($"invalid stat {stat}");
        }

        // Cap stat stages within -6 to 6
        if (delta < 0)
        {
            //-6 -5 -4 ..  2
            // 0 -1 -2 .. -8
            var cap = currentStage * -1 - 6;
            delta = Math.Max(delta, cap);
            if (delta == 0) return $"{Name}'s {stat} won't go any lower!\n";
        }
        else
        {
            // 6  5  4 .. -2
            // 0  1  2 ..  8
            var cap = currentStage * -1 + 6;
            delta = Math.Min(delta, cap);
            if (delta == 0) return $"{Name}'s {stat} won't go any higher!\n";
        }

        // Prevent stat changes
        if (delta < 0 && attacker != this)
        {
            if (Ability(attacker, move) == Impl.Ability.CLEAR_BODY ||
                Ability(attacker, move) == Impl.Ability.WHITE_SMOKE ||
                Ability(attacker, move) == Impl.Ability.FULL_METAL_BODY)
            {
                var abilityName = ((Ability)AbilityId).GetPrettyName();
                return $"{Name}'s {abilityName} prevented its {stat} from being lowered!\n";
            }

            if (Ability(attacker, move) == Impl.Ability.HYPER_CUTTER && stat == "attack")
                return $"{Name}'s claws stayed sharp because of its hyper cutter!\n";
            if (Ability(attacker, move) == Impl.Ability.KEEN_EYE && stat == "accuracy")
                return $"{Name}'s aim stayed true because of its keen eye!\n";
            if (Ability(attacker, move) == Impl.Ability.MINDS_EYE && stat == "accuracy")
                return $"{Name}'s aim stayed true because of its mind's eye!\n";
            if (Ability(attacker, move) == Impl.Ability.BIG_PECKS && stat == "defense")
                return $"{Name}'s defense stayed strong because of its big pecks!\n";
            if (Owner.Mist.Active() && (attacker == null || attacker.Ability() != Impl.Ability.INFILTRATOR))
                return $"The mist around {Name}'s feet prevented its {stat} from being lowered!\n";
            if (Ability(attacker, move) == Impl.Ability.FLOWER_VEIL && TypeIds.Contains(ElementType.GRASS)) return "";
            if (Ability(attacker, move) == Impl.Ability.MIRROR_ARMOR && attacker != null && checkLooping)
            {
                msg += $"{Name} reflected the stat change with its mirror armor!\n";
                msg += attacker.AppendStat(delta, this, null, stat, "", false);
                return msg;
            }
        }

        switch (delta)
        {
            // Remember if stats were changed for certain moves
            case > 0:
                StatIncreased = true;
                break;
            case < 0:
                StatDecreased = true;
                break;
        }

        switch (stat)
        {
            case "attack":
                AttackStage += delta;
                break;
            case "defense":
                DefenseStage += delta;
                break;
            case "special attack":
                SpAtkStage += delta;
                break;
            case "special defense":
                SpDefStage += delta;
                break;
            case "speed":
                SpeedStage += delta;
                break;
            case "accuracy":
                AccuracyStage += delta;
                break;
            case "evasion":
                EvasionStage += delta;
                break;
            default:
                throw new ArgumentException($"invalid stat {stat}");
        }

        var formattedDelta = Math.Min(Math.Max(delta, -3), 3);
        msg += deltaMessages[formattedDelta];

        // TODO: fix this hacky way of doing this, but probably not until multi battles...
        var battle = HeldItem.Battle;

        switch (delta)
        {
            // Effects that happen after a pokemon gains stats
            case < 0:
            {
                if (attacker != this)
                {
                    if (Ability(attacker, move) == Impl.Ability.DEFIANT)
                        msg += AppendAttack(2, this, source: "its defiance");
                    if (Ability(attacker, move) == Impl.Ability.COMPETITIVE)
                        msg += AppendSpAtk(2, this, source: "its competitiveness");
                }

                if (HeldItem.Get() == "eject-pack")
                {
                    // This assumes that neither attacker or poke are needed if not checking traps
                    var swaps = Owner.ValidSwaps(null, null, false);
                    if (swaps.Count > 0)
                    {
                        msg += $"{Name} is switched out by its eject pack!\n";
                        HeldItem.Use();
                        msg += Remove(battle);
                        // Force this pokemon to immediately return to be attacked
                        Owner.MidTurnRemove = true;
                    }
                }

                break;
            }
            case > 0:
            {
                foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
                    if (poke != null && poke != this && poke.Ability() == Impl.Ability.OPPORTUNIST && checkLooping)
                    {
                        msg += $"{poke.Name} seizes the opportunity to boost its stat with its opportunist!\n";
                        msg += poke.AppendStat(delta, poke, null, stat, "", false);
                    }

                break;
            }
        }

        return msg;
    }
}