namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Calculates a raw stat value based on base stat, IVs, EVs, and nature.
    /// </summary>
    /// <param name="baseStat">The base stat value.</param>
    /// <param name="iv">The individual value (0-31).</param>
    /// <param name="ev">The effort value (0-252).</param>
    /// <param name="nature">The nature multiplier.</param>
    /// <returns>The calculated raw stat value.</returns>
    public int CalculateRawStat(int baseStat, int iv, int ev, double nature)
    {
        return (int)((int)Math.Round((2 * baseStat + iv + ev / 4.0) * Level / 100 + 5) * nature);
    }

    /// <summary>
    ///     Calculates a stat based on that stat's stage changes.
    /// </summary>
    /// <param name="stat">The base stat value to modify.</param>
    /// <param name="statStage">The stat stage modifier (-6 to +6).</param>
    /// <param name="crop">Optional parameter to crop stat stages ("bottom" or "top").</param>
    /// <returns>The modified stat value.</returns>
    public static double CalculateStat(double stat, int statStage, string crop = null)
    {
        switch (crop)
        {
            case "bottom":
                statStage = Math.Max(statStage, 0);
                break;
            case "top":
                statStage = Math.Min(statStage, 0);
                break;
        }

        double[] stageMultiplier = [2.0 / 8, 2.0 / 7, 2.0 / 6, 2.0 / 5, 2.0 / 4, 2.0 / 3, 1, 1.5, 2, 2.5, 3, 3.5, 4];
        return stageMultiplier[statStage + 6] * stat;
    }

    /// <summary>
    ///     Returns the raw attack of this poke, taking into account IVs EVs and natures and forms.
    /// </summary>
    /// <param name="checkPowerTrick">Whether to check for Power Trick effects.</param>
    /// <param name="checkPowerShift">Whether to check for Power Shift effects.</param>
    /// <returns>The raw attack stat value.</returns>
    public int GetRawAttack(bool checkPowerTrick = true, bool checkPowerShift = true)
    {
        if (PowerTrick && checkPowerTrick) return GetRawDefense(false, checkPowerShift);
        if (PowerShift && checkPowerShift) return GetRawDefense(checkPowerTrick, false);
        var stat = CalculateRawStat(Attack, AtkIV, AtkEV, NatureStatDeltas["Attack"]);
        if (AttackSplit != null) stat = (stat + AttackSplit.Value) / 2;
        return stat;
    }

    /// <summary>
    ///     Returns the raw defense of this poke, taking into account IVs EVs and natures and forms.
    /// </summary>
    /// <param name="checkPowerTrick">Whether to check for Power Trick effects.</param>
    /// <param name="checkPowerShift">Whether to check for Power Shift effects.</param>
    /// <returns>The raw defense stat value.</returns>
    public int GetRawDefense(bool checkPowerTrick = true, bool checkPowerShift = true)
    {
        if (PowerTrick && checkPowerTrick) return GetRawAttack(false, checkPowerShift);
        if (PowerShift && checkPowerShift) return GetRawAttack(checkPowerTrick, false);
        var stat = CalculateRawStat(Defense, DefIV, DefEV, NatureStatDeltas["Defense"]);
        if (DefenseSplit != null) stat = (stat + DefenseSplit.Value) / 2;
        return stat;
    }

    /// <summary>
    ///     Returns the raw special attack of this poke, taking into account IVs EVs and natures and forms.
    /// </summary>
    /// <param name="checkPowerShift">Whether to check for Power Shift effects.</param>
    /// <returns>The raw special attack stat value.</returns>
    public int GetRawSpAtk(bool checkPowerShift = true)
    {
        if (PowerShift && checkPowerShift) return GetRawSpDef(false);
        var stat = CalculateRawStat(SpAtk, SpAtkIV, SpAtkEV, NatureStatDeltas["Special attack"]);
        if (SpAtkSplit != null) stat = (stat + SpAtkSplit.Value) / 2;
        return stat;
    }

    /// <summary>
    ///     Returns the raw special defense of this poke, taking into account IVs EVs and natures and forms.
    /// </summary>
    /// <param name="checkPowerShift">Whether to check for Power Shift effects.</param>
    /// <returns>The raw special defense stat value.</returns>
    public int GetRawSpDef(bool checkPowerShift = true)
    {
        if (PowerShift && checkPowerShift) return GetRawSpAtk(false);
        var stat = CalculateRawStat(SpDef, SpDefIV, SpDefEV, NatureStatDeltas["Special defense"]);
        if (SpDefSplit != null) stat = (stat + SpDefSplit.Value) / 2;
        return stat;
    }

    /// <summary>
    ///     Returns the raw speed of this poke, taking into account IVs EVs and natures and forms.
    /// </summary>
    /// <returns>The raw speed stat value.</returns>
    public int GetRawSpeed()
    {
        return CalculateRawStat(Speed, SpeedIV, SpeedEV, NatureStatDeltas["Speed"]);
    }

    /// <summary>
    ///     Helper method to call calculate_stat for attack.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="critical">Whether this is for a critical hit calculation.</param>
    /// <param name="ignoreStages">Whether to ignore stat stage modifiers.</param>
    /// <returns>The final attack value including all modifiers.</returns>
    public int GetAttack(Battle battle, bool critical = false, bool ignoreStages = false)
    {
        double attack = GetRawAttack();
        if (!ignoreStages) attack = CalculateStat(attack, AttackStage, critical ? "bottom" : null);
        if (Ability() == Impl.Ability.GUTS && !string.IsNullOrEmpty(NonVolatileEffect.Current)) attack *= 1.5;
        if (Ability() == Impl.Ability.SLOW_START && ActiveTurns < 5) attack *= 0.5;
        if (Ability() == Impl.Ability.HUGE_POWER || Ability() == Impl.Ability.PURE_POWER) attack *= 2;
        if (Ability() == Impl.Ability.HUSTLE) attack *= 1.5;
        if (Ability() == Impl.Ability.DEFEATIST && Hp <= StartingHp / 2) attack *= 0.5;
        if (Ability() == Impl.Ability.GORILLA_TACTICS) attack *= 1.5;
        if (Ability() == Impl.Ability.FLOWER_GIFT &&
            (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun")) attack *= 1.5;
        if (Ability() == Impl.Ability.ORICHALCUM_PULSE &&
            (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun")) attack *= 4.0 / 3;
        if (HeldItem.Get() == "choice-band") attack *= 1.5;
        if (HeldItem.Get() == "light-ball" && _name == "Pikachu") attack *= 2;
        if (HeldItem.Get() == "thick-club" && _name is "Cubone" or "Marowak" or "Marowak-alola") attack *= 2;
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
            if (poke != null && poke != this && poke.Ability() == Impl.Ability.TABLETS_OF_RUIN)
                attack *= 0.75;

        if (GetRawAttack() >= GetRawDefense() && GetRawAttack() >= GetRawSpAtk() && GetRawAttack() >= GetRawSpDef() &&
            GetRawAttack() >= GetRawSpeed())
        {
            if ((Ability() == Impl.Ability.PROTOSYNTHESIS &&
                 (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun" || BoosterEnergy))
                || (Ability() == Impl.Ability.QUARK_DRIVE &&
                    (battle.Terrain.Item?.ToString() == "electric" || BoosterEnergy)))
            {
                attack *= 1.3;
            }
            else if ((Ability() == Impl.Ability.PROTOSYNTHESIS || Ability() == Impl.Ability.QUARK_DRIVE) &&
                     HeldItem.Get() == "booster-energy")
            {
                HeldItem.Use();
                BoosterEnergy = true;
                attack *= 1.3;
            }
        }

        return (int)attack;
    }

    /// <summary>
    ///     Helper method to call calculate_stat for defense.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="critical">Whether this is for a critical hit calculation.</param>
    /// <param name="ignoreStages">Whether to ignore stat stage modifiers.</param>
    /// <param name="attacker">The attacking Pokemon for ability calculations.</param>
    /// <param name="move">The move being used for ability calculations.</param>
    /// <returns>The final defense value including all modifiers.</returns>
    public int GetDefense(Battle battle, bool critical = false, bool ignoreStages = false, DuelPokemon attacker = null,
        Move.Move move = null)
    {
        double defense;
        if (battle.WonderRoom.Active())
            defense = GetRawSpDef();
        else
            defense = GetRawDefense();
        if (!ignoreStages) defense = CalculateStat(defense, DefenseStage, critical ? "top" : null);
        if (Ability(attacker, move) == Impl.Ability.MARVEL_SCALE &&
            !string.IsNullOrEmpty(NonVolatileEffect.Current)) defense *= 1.5;
        if (Ability(attacker, move) == Impl.Ability.FUR_COAT) defense *= 2;
        if (Ability(attacker, move) == Impl.Ability.GRASS_PELT && battle.Terrain.Item?.ToString() == "grassy")
            defense *= 1.5;
        if (HeldItem.Get() == "eviolite" && CanStillEvolve) defense *= 1.5;
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
            if (poke != null && poke != this && poke.Ability() == Impl.Ability.SWORD_OF_RUIN)
                defense *= 0.75;

        if (GetRawDefense() > GetRawAttack() && GetRawDefense() >= GetRawSpAtk() && GetRawDefense() >= GetRawSpDef() &&
            GetRawDefense() >= GetRawSpeed())
        {
            if ((Ability() == Impl.Ability.PROTOSYNTHESIS &&
                 (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun" || BoosterEnergy))
                || (Ability() == Impl.Ability.QUARK_DRIVE &&
                    (battle.Terrain.Item?.ToString() == "electric" || BoosterEnergy)))
            {
                defense *= 1.3;
            }
            else if ((Ability() == Impl.Ability.PROTOSYNTHESIS || Ability() == Impl.Ability.QUARK_DRIVE) &&
                     HeldItem.Get() == "booster-energy")
            {
                HeldItem.Use();
                BoosterEnergy = true;
                defense *= 1.3;
            }
        }

        return (int)defense;
    }

    /// <summary>
    ///     Helper method to call calculate_stat for spatk.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="critical">Whether this is for a critical hit calculation.</param>
    /// <param name="ignoreStages">Whether to ignore stat stage modifiers.</param>
    /// <returns>The final special attack value including all modifiers.</returns>
    public int GetSpAtk(Battle battle, bool critical = false, bool ignoreStages = false)
    {
        double spatk = GetRawSpAtk();
        if (!ignoreStages) spatk = CalculateStat(spatk, SpAtkStage, critical ? "bottom" : null);
        if (Ability() == Impl.Ability.DEFEATIST && Hp <= StartingHp / 2) spatk *= 0.5;
        if (Ability() == Impl.Ability.SOLAR_POWER &&
            (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun")) spatk *= 1.5;
        if (Ability() == Impl.Ability.HADRON_ENGINE && battle.Terrain.Item?.ToString() == "grassy") spatk *= 4.0 / 3;
        if (HeldItem.Get() == "choice-specs") spatk *= 1.5;
        if (HeldItem.Get() == "deep-sea-tooth" && _name == "Clamperl") spatk *= 2;
        if (HeldItem.Get() == "light-ball" && _name == "Pikachu") spatk *= 2;
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
            if (poke != null && poke != this && poke.Ability() == Impl.Ability.VESSEL_OF_RUIN)
                spatk *= 0.75;

        if (GetRawSpAtk() >= GetRawSpDef() && GetRawSpAtk() >= GetRawSpeed() && GetRawSpAtk() > GetRawAttack() &&
            GetRawSpAtk() > GetRawDefense())
        {
            if ((Ability() == Impl.Ability.PROTOSYNTHESIS &&
                 (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun" || BoosterEnergy))
                || (Ability() == Impl.Ability.QUARK_DRIVE &&
                    (battle.Terrain.Item?.ToString() == "electric" || BoosterEnergy)))
            {
                spatk *= 1.3;
            }
            else if ((Ability() == Impl.Ability.PROTOSYNTHESIS || Ability() == Impl.Ability.QUARK_DRIVE) &&
                     HeldItem.Get() == "booster-energy")
            {
                HeldItem.Use();
                BoosterEnergy = true;
                spatk *= 1.3;
            }
        }

        return (int)spatk;
    }

    /// <summary>
    ///     Helper method to call calculate_stat for spdef.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="critical">Whether this is for a critical hit calculation.</param>
    /// <param name="ignoreStages">Whether to ignore stat stage modifiers.</param>
    /// <param name="attacker">The attacking Pokemon for ability calculations.</param>
    /// <param name="move">The move being used for ability calculations.</param>
    /// <returns>The final special defense value including all modifiers.</returns>
    public int GetSpDef(Battle battle, bool critical = false, bool ignoreStages = false, DuelPokemon attacker = null,
        Move.Move move = null)
    {
        double spdef;
        if (battle.WonderRoom.Active())
            spdef = GetRawDefense();
        else
            spdef = GetRawSpDef();
        if (!ignoreStages) spdef = CalculateStat(spdef, SpDefStage, critical ? "top" : null);
        if (battle.Weather.Get() == "sandstorm" && TypeIds.Contains(ElementType.ROCK)) spdef *= 1.5;
        if (Ability(attacker, move) == Impl.Ability.FLOWER_GIFT &&
            (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun")) spdef *= 1.5;
        if (HeldItem.Get() == "deep-sea-scale" && _name == "Clamperl") spdef *= 2;
        if (HeldItem.Get() == "assault-vest") spdef *= 1.5;
        if (HeldItem.Get() == "eviolite" && CanStillEvolve) spdef *= 1.5;
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
            if (poke != null && poke != this && poke.Ability() == Impl.Ability.BEADS_OF_RUIN)
                spdef *= 0.75;

        if (GetRawSpDef() >= GetRawSpeed() && GetRawSpDef() > GetRawAttack() && GetRawSpDef() > GetRawDefense() &&
            GetRawSpDef() > GetRawSpAtk())
        {
            if ((Ability() == Impl.Ability.PROTOSYNTHESIS &&
                 (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun" || BoosterEnergy))
                || (Ability() == Impl.Ability.QUARK_DRIVE &&
                    (battle.Terrain.Item?.ToString() == "electric" || BoosterEnergy)))
            {
                spdef *= 1.3;
            }
            else if ((Ability() == Impl.Ability.PROTOSYNTHESIS || Ability() == Impl.Ability.QUARK_DRIVE) &&
                     HeldItem.Get() == "booster-energy")
            {
                HeldItem.Use();
                BoosterEnergy = true;
                spdef *= 1.3;
            }
        }

        return (int)spdef;
    }

    /// <summary>
    ///     Helper method to call calculate_stat for speed.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <returns>The final speed value including all modifiers.</returns>
    public int GetSpeed(Battle battle)
    {
        // Always active stage changes
        var speed = CalculateStat(GetRawSpeed(), SpeedStage);
        if (NonVolatileEffect.Paralysis() && Ability() != Impl.Ability.QUICK_FEET) speed /= 2;
        if (HeldItem.Get() == "iron-ball") speed /= 2;
        if (Owner.Tailwind.Active()) speed *= 2;
        if (Ability() == Impl.Ability.SLUSH_RUSH && battle.Weather.Get() == "hail") speed *= 2;
        if (Ability() == Impl.Ability.SAND_RUSH && battle.Weather.Get() == "sandstorm") speed *= 2;
        if (Ability() == Impl.Ability.SWIFT_SWIM &&
            (battle.Weather.Get() == "rain" || battle.Weather.Get() == "h-rain")) speed *= 2;
        if (Ability() == Impl.Ability.CHLOROPHYLL &&
            (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun")) speed *= 2;
        if (Ability() == Impl.Ability.SLOW_START && ActiveTurns < 5) speed *= 0.5;
        if (Ability() == Impl.Ability.UNBURDEN && !HeldItem.HasItem() && HeldItem.EverHadItem) speed *= 2;
        if (Ability() == Impl.Ability.QUICK_FEET && !string.IsNullOrEmpty(NonVolatileEffect.Current)) speed *= 1.5;
        if (Ability() == Impl.Ability.SURGE_SURFER && battle.Terrain.Item?.ToString() == "electric") speed *= 2;
        if (HeldItem.Get() == "choice-scarf") speed *= 1.5;
        if (GetRawSpeed() > GetRawAttack() && GetRawSpeed() > GetRawDefense() && GetRawSpeed() > GetRawSpAtk() &&
            GetRawSpeed() > GetRawSpDef())
        {
            if ((Ability() == Impl.Ability.PROTOSYNTHESIS &&
                 (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun" || BoosterEnergy))
                || (Ability() == Impl.Ability.QUARK_DRIVE &&
                    (battle.Terrain.Item?.ToString() == "electric" || BoosterEnergy)))
            {
                speed *= 1.5;
            }
            else if ((Ability() == Impl.Ability.PROTOSYNTHESIS || Ability() == Impl.Ability.QUARK_DRIVE) &&
                     HeldItem.Get() == "booster-energy")
            {
                HeldItem.Use();
                BoosterEnergy = true;
                speed *= 1.5;
            }
        }

        return (int)speed;
    }

    /// <summary>
    ///     Helper method to calculate accuracy stage.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <returns>The accuracy stage value.</returns>
    public int GetAccuracy(Battle battle)
    {
        return AccuracyStage;
    }

    /// <summary>
    ///     Helper method to calculate evasion stage.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <returns>The evasion stage value.</returns>
    public int GetEvasion(Battle battle)
    {
        return EvasionStage;
    }
}