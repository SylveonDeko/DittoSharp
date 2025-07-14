namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Calculates a double representing the effectiveness of `attacker_type` damage on this poke.
    ///     Handles type interactions, special move effects, abilities, and battle conditions.
    /// </summary>
    /// <param name="attackerType">The type of the attacking move.</param>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="attacker">The attacking Pokemon (for ability checks).</param>
    /// <param name="move">The move being used (for special effects).</param>
    /// <returns>The type effectiveness multiplier (0.0, 0.5, 1.0, 2.0, etc.).</returns>
    public double Effectiveness(ElementType attackerType, Battle battle, DuelPokemon attacker = null,
        Move.Move move = null)
    {
        if (attackerType == ElementType.TYPELESS) return 1;
        double effectiveness = 1;
        foreach (var defenderType in TypeIds.Where(defenderType => defenderType != ElementType.TYPELESS))
        {
            switch (move)
            {
                case { Effect: 380 } when defenderType == ElementType.WATER:
                    effectiveness *= 2;
                    continue;
                case { Effect: 373 } when defenderType == ElementType.FLYING &&
                                          !Grounded(battle, attacker,
                                              move):
                    return 1; // Ignores secondary types if defender is flying type and not grounded
            }

            if (Roost && defenderType == ElementType.FLYING) continue;
            if (Foresight && attackerType is ElementType.FIGHTING or ElementType.NORMAL &&
                defenderType == ElementType.GHOST) continue;
            if (MiracleEye && attackerType == ElementType.PSYCHIC && defenderType == ElementType.DARK) continue;
            switch (attackerType)
            {
                case ElementType.FIGHTING or ElementType.NORMAL when defenderType == ElementType.GHOST &&
                                                                     attacker != null &&
                                                                     (attacker.Ability() == Impl.Ability.SCRAPPY ||
                                                                      attacker.Ability() == Impl.Ability.MINDS_EYE):
                case ElementType.GROUND when defenderType == ElementType.FLYING && Grounded(battle, attacker, move):
                    continue;
            }

            var key = (attackerType, defenderType);
            if (!battle.TypeEffectiveness.TryGetValue(key, out var value)) continue;
            var e = value / 100.0;
            if (defenderType == ElementType.FLYING && e > 1 && move != null && battle.Weather.Get() == "h-wind") e = 1;
            if (battle.InverseBattle)
                e = e switch
                {
                    < 1 => 2,
                    > 1 => 0.5,
                    _ => e
                };

            effectiveness *= e;
        }

        if (attackerType == ElementType.FIRE && TarShot) effectiveness *= 2;
        if (effectiveness >= 1 && Hp == StartingHp && Ability(attacker, move) == Impl.Ability.TERA_SHELL)
            effectiveness = 0.5;
        return effectiveness;
    }
}