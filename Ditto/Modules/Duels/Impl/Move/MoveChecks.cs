namespace Ditto.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Checks if the move hits a semi-invulnerable pokemon.
    /// </summary>
    /// <returns>True if this move hits, False otherwise.</returns>
    public bool CheckSemiInvulnerable(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        if (!TargetsOpponent()) return true;

        if (attacker.Ability() == Ability.NO_GUARD ||
            defender.Ability(attacker, this) == Ability.NO_GUARD)
            return true;

        if (defender.MindReader.Active() && defender.MindReader.Item == attacker) return true;

        if (defender.Dive && !new[] { 258, 262 }.Contains(Effect)) return false;

        if (defender.Dig && !new[] { 127, 148 }.Contains(Effect)) return false;

        if (defender.Fly && !new[] { 147, 150, 153, 208, 288, 334, 373 }.Contains(Effect)) return false;

        if (defender.ShadowForce) return false;

        return true;
    }

    /// <summary>
    ///     Checks if the move hits through protection effects.
    /// </summary>
    /// <returns>
    ///     A tuple (boolean, string) where the boolean indicates if the move hits, and the string is a message to add to
    ///     the battle log.
    /// </returns>
    public (bool, string) CheckProtect(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        var msg = "";
        // Moves that don't target the opponent can't be protected by the target.
        if (!TargetsOpponent()) return (true, msg);

        // Moves which bypass all protection.
        if (new[] { 149, 224, 273, 360, 438, 489 }.Contains(Effect)) return (true, msg);

        if (attacker.Ability() == Ability.UNSEEN_FIST && MakesContact(attacker)) return (true, msg);

        if (defender.CraftyShield && DamageClass == DamageClass.STATUS) return (false, msg);

        // Moves which bypass all protection except for crafty shield.
        if (new[] { 29, 107, 179, 412 }.Contains(Effect)) return (true, msg);

        if (defender.Protect) return (false, msg);

        if (defender.SpikyShield)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
                msg += attacker.Damage(attacker.StartingHp / 8, battle, source: $"{defender.Name}'s spiky shield");

            return (false, msg);
        }

        if (defender.BanefulBunker)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
                msg += attacker.NonVolatileEffect.ApplyStatus("poison", battle, defender,
                    source: $"{defender.Name}'s baneful bunker");

            return (false, msg);
        }

        if (defender.WideGuard && TargetsMultiple()) return (false, msg);

        if (GetPriority(attacker, defender, battle) > 0 && battle.Terrain.Item?.ToString() == "psychic" &&
            defender.Grounded(battle, attacker, this))
            return (false, msg);

        if (defender.MatBlock && DamageClass != DamageClass.STATUS) return (false, msg);

        if (defender.KingShield && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
                msg += attacker.AppendAttack(-1, defender, this,
                    $"{defender.Name}'s king shield");

            return (false, msg);
        }

        if (defender.Obstruct && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
                msg += attacker.AppendDefense(-2, defender, this,
                    $"{defender.Name}'s obstruct");

            return (false, msg);
        }

        if (defender.SilkTrap && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
                msg += attacker.AppendSpeed(-1, defender, this,
                    $"{defender.Name}'s silk trap");

            return (false, msg);
        }

        if (defender.BurningBulwark && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
                msg += attacker.NonVolatileEffect.ApplyStatus("burn", battle, defender,
                    source: $"{defender.Name}'s burning bulwark");

            return (false, msg);
        }

        if (defender.QuickGuard && GetPriority(attacker, defender, battle) > 0) return (false, msg);

        return (true, msg);
    }

    /// <summary>
    ///     Checks if this move hits based on accuracy.
    /// </summary>
    /// <returns>True if this move hits, False otherwise.</returns>
    public bool CheckHit(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        var micleBerryUsed = attacker.MicleBerryAte;
        attacker.MicleBerryAte = false;

        // Moves that have a null accuracy always hit.
        if (Accuracy == null) return true;

        // During hail, this bypasses accuracy checks
        if (Effect == 261 && battle.Weather.Get() == "hail") return true;

        // During rain, this bypasses accuracy checks
        if (new[] { 153, 334, 357, 365, 396 }.Contains(Effect) &&
            new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()))
            return true;

        switch (Effect)
        {
            // If used by a poison type, this bypasses accuracy checks
            case 34 when attacker.TypeIds.Contains(ElementType.POISON):
            // If used against a minimized poke, this bypasses accuracy checks
            case 338 when defender.Minimized:
                return true;
        }

        // These DO allow OHKO moves to bypass accuracy checks
        if (TargetsOpponent())
        {
            if (defender.MindReader.Active() && defender.MindReader.Item == attacker) return true;

            if (attacker.Ability() == Ability.NO_GUARD) return true;

            if (defender.Ability(attacker, this) == Ability.NO_GUARD) return true;
        }

        // OHKO moves
        if (Effect == 39)
        {
            var attackerLevel = 30 + (attacker.Level - defender.Level);
            return new Random().NextDouble() * 100 <= attackerLevel;
        }

        // This does NOT allow OHKO moves to bypass accuracy checks
        if (attacker.Telekinesis.Active()) return true;

        double accuracy = Accuracy.Value;
        // When used during harsh sunlight, this has an accuracy of 50%
        if (new[] { 153, 334 }.Contains(Effect) && new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
            accuracy = 50;

        if (TargetsOpponent())
            if (defender.Ability(attacker, this) == Ability.WONDER_SKIN &&
                DamageClass == DamageClass.STATUS)
                accuracy = 50;

        var stage = defender.Ability(attacker, this) == Ability.UNAWARE
            ? 0
            : attacker.GetAccuracy(battle);

        if (!(
                Effect == 304 ||
                defender.Foresight ||
                defender.MiracleEye ||
                new[] { Ability.UNAWARE, Ability.KEEN_EYE, Ability.MINDS_EYE }.Contains(attacker.Ability())
            ))
            stage -= defender.GetEvasion(battle);

        stage = Math.Min(6, Math.Max(-6, stage));
        var stageMultiplier = new[]
        {
            3.0 / 9.0, 3.0 / 8.0, 3.0 / 7.0, 3.0 / 6.0, 3.0 / 5.0, 3.0 / 4.0,
            1.0,
            4.0 / 3.0, 5.0 / 3.0, 2.0, 7.0 / 3.0, 8.0 / 3.0, 3.0
        };

        accuracy *= stageMultiplier[stage + 6];

        if (TargetsOpponent())
        {
            if (defender.Ability(attacker, this) == Ability.TANGLED_FEET &&
                defender.Confusion.Active())
                accuracy *= 0.5;

            if (defender.Ability(attacker, this) == Ability.SAND_VEIL &&
                battle.Weather.Get() == "sandstorm")
                accuracy *= 0.8;

            if (defender.Ability(attacker, this) == Ability.SNOW_CLOAK &&
                battle.Weather.Get() == "hail")
                accuracy *= 0.8;
        }

        if (attacker.Ability() == Ability.COMPOUND_EYES) accuracy *= 1.3;

        if (attacker.Ability() == Ability.HUSTLE && DamageClass == DamageClass.PHYSICAL) accuracy *= 0.8;

        if (attacker.Ability() == Ability.VICTORY_STAR) accuracy *= 1.1;

        if (battle.Gravity.Active()) accuracy *= 5.0 / 3.0;

        if (attacker.HeldItem == "wide-lens") accuracy *= 1.1;

        if (attacker.HeldItem == "zoom-lens" && defender.HasMoved) accuracy *= 1.2;

        if (defender.HeldItem == "bright-powder") accuracy *= 0.9;

        if (micleBerryUsed) accuracy *= 1.2;

        return new Random().NextDouble() * 100 <= accuracy;
    }

    /// <summary>
    ///     Checks if a move has an effect on a pokemon.
    /// </summary>
    /// <returns>True if a move has an effect on a pokemon.</returns>
    public bool CheckEffective(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        // What if I :flushed: used Hold Hands :flushed: in a double battle :flushed: with you? :flushed:
        // (and you weren't protected by Crafty Shield or in the semi-invulnerable turn of a move like Fly or Dig)
        if (new[] { 86, 174, 368, 370, 371, 389 }.Contains(Effect)) return false;

        if (!TargetsOpponent()) return true;

        switch (Effect)
        {
            case 266 when defender.Ability(attacker, this) == Ability.OBLIVIOUS:
            case 39 when defender.Ability(attacker, this) == Ability.STURDY:
            case 39 when Id == 329 && defender.TypeIds.Contains(ElementType.ICE):
            case 400 when string.IsNullOrEmpty(defender.NonVolatileEffect.Current):
                return false;
        }

        if (IsSoundBased() && defender.Ability(attacker, this) == Ability.SOUNDPROOF) return false;

        if (IsBallOrBomb() && defender.Ability(attacker, this) == Ability.BULLETPROOF) return false;

        if (attacker.Ability() == Ability.PRANKSTER && defender.TypeIds.Contains(ElementType.DARK))
        {
            if (DamageClass == DamageClass.STATUS) return false;

            // If the attacker used a status move that called this move, even if this move is not a status move then it should still be considered affected by prankster.
            if (attacker.Owner.SelectedAction is Trainer.MoveAction { Move.DamageClass: DamageClass.STATUS })
                return false;
        }

        if (defender.Ability(attacker, this) == Ability.GOOD_AS_GOLD &&
            DamageClass == DamageClass.STATUS)
            return false;

        // Status moves do not care about type effectiveness - except for thunder wave FOR SOME REASON...
        if (DamageClass == DamageClass.STATUS && Id != 86) return true;

        var currentType = GetType(attacker, defender, battle);
        if (currentType == ElementType.TYPELESS) return true;

        var effectiveness = defender.Effectiveness(currentType, battle, attacker, this);
        if (Effect == 338) effectiveness *= defender.Effectiveness(ElementType.FLYING, battle, attacker, this);

        if (effectiveness == 0) return false;

        if (currentType == ElementType.GROUND && !defender.Grounded(battle, attacker, this) &&
            Effect != 373 && !battle.InverseBattle)
            return false;

        if (Effect != 459)
            switch (currentType)
            {
                case ElementType.ELECTRIC when
                    defender.Ability(attacker, this) == Ability.VOLT_ABSORB &&
                    defender.Hp == defender.StartingHp:
                case ElementType.WATER when
                    (defender.Ability(attacker, this) == Ability.WATER_ABSORB ||
                     defender.Ability(attacker, this) == Ability.DRY_SKIN) &&
                    defender.Hp == defender.StartingHp:
                    return false;
            }

        if (currentType == ElementType.FIRE &&
            defender.Ability(attacker, this) == Ability.FLASH_FIRE &&
            defender.FlashFire)
            return false;

        if (effectiveness <= 1 && defender.Ability(attacker, this) == Ability.WONDER_GUARD) return false;

        return true;
    }

    /// <summary>
    ///     Returns True if the move can be executed, False otherwise.
    ///     Checks different requirements for moves that can make them fail.
    /// </summary>
    public bool CheckExecutable(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        if (attacker.Taunt.Active() && DamageClass == DamageClass.STATUS) return false;

        if (attacker.Silenced.Active() && IsSoundBased()) return false;

        if (IsAffectedByHealBlock() && attacker.HealBlock.Active()) return false;

        if (IsPowderOrSpore() && (defender.TypeIds.Contains(ElementType.GRASS) ||
                                  defender.Ability(attacker, this) == Ability.OVERCOAT ||
                                  defender.HeldItem == "safety-goggles"))
            return false;

        if (battle.Weather.Get() == "h-sun" && GetType(attacker, defender, battle) == ElementType.WATER &&
            DamageClass != DamageClass.STATUS)
            return false;

        if (battle.Weather.Get() == "h-rain" && GetType(attacker, defender, battle) == ElementType.FIRE &&
            DamageClass != DamageClass.STATUS)
            return false;

        if (attacker.Disable.Active() && attacker.Disable.Item == this) return false;

        if (attacker != defender && defender.Imprison && defender.Moves.Any(m => m.Id == Id)) return false;

        // Since we only have single battles, these moves always fail
        if (new[] { 173, 301, 308, 316, 363, 445, 494 }.Contains(Effect)) return false;

        if (new[] { 93, 98 }.Contains(Effect) && !attacker.NonVolatileEffect.Sleep()) return false;

        if (new[] { 9, 108 }.Contains(Effect) && !defender.NonVolatileEffect.Sleep()) return false;

        if (Effect == 364 && !defender.NonVolatileEffect.Poison()) return false;

        if (new[] { 162, 163 }.Contains(Effect) && attacker.Stockpile == 0) return false;

        switch (Effect)
        {
            case 85 when defender.TypeIds.Contains(ElementType.GRASS) || defender.LeechSeed:
            case 193 when attacker.Imprison:
            case 166 when defender.Torment:
            case 91 when defender.Encore.Active() || defender.LastMove == null || defender.LastMove.PP == 0:
            case 87 when defender.Disable.Active() || defender.LastMove == null || defender.LastMove.PP == 0:
                return false;
        }

        if (new[] { 96, 101 }.Contains(Effect) && (defender.LastMove == null || defender.LastMove.PP == 0))
            return false;

        switch (Effect)
        {
            case 176 when defender.Taunt.Active():
            case 29 when !defender.Owner.ValidSwaps(attacker, battle, false).Any():
                return false;
        }

        if (new[] { 128, 154, 493 }.Contains(Effect) &&
            !attacker.Owner.ValidSwaps(defender, battle, false).Any())
            return false;

        if (Effect == 161 && attacker.Stockpile >= 3) return false;

        if (new[] { 90, 145, 228, 408 }.Contains(Effect) && attacker.LastMoveDamage == null) return false;

        if (Effect == 145 && attacker.LastMoveDamage.Item2 != DamageClass.SPECIAL) return false;

        if (new[] { 90, 408 }.Contains(Effect) && attacker.LastMoveDamage.Item2 != DamageClass.PHYSICAL) return false;

        if (new[] { 10, 243 }.Contains(Effect) &&
            (defender.LastMove == null || !defender.LastMove.SelectableByMirrorMove()))
            return false;

        switch (Effect)
        {
            case 83 when defender.LastMove == null || !defender.LastMove.SelectableByMimic():
            case 180 when attacker.Owner.Wish.Active():
            case 388 when defender.AttackStage == -6:
                return false;
        }

        if (new[] { 143, 485, 493 }.Contains(Effect) && attacker.Hp <= attacker.StartingHp / 2) return false;

        switch (Effect)
        {
            case 414 when attacker.Hp < attacker.StartingHp / 3:
            case 80 when attacker.Hp <= attacker.StartingHp / 4:
            case 48 when attacker.FocusEnergy:
            case 190 when attacker.Hp >= defender.Hp:
            case 194 when !(attacker.NonVolatileEffect.Burn() || attacker.NonVolatileEffect.Paralysis() ||
                            attacker.NonVolatileEffect.Poison()):
            case 235 when
                !attacker.NonVolatileEffect.Current.Any() || defender.NonVolatileEffect.Current.Any():
                return false;
        }

        if (new[] { 121, 266 }.Contains(Effect) && ("-x" == attacker.Gender || "-x" == defender.Gender ||
                                                    attacker.Gender == defender.Gender ||
                                                    defender.Ability(attacker, this) ==
                                                    Ability.OBLIVIOUS))
            return false;

        if (new[] { 367, 392 }.Contains(Effect) &&
            !new[] { Ability.PLUS, Ability.MINUS }.Contains(attacker.Ability()))
            return false;

        if (Effect == 39 && attacker.Level < defender.Level) return false;

        if (new[] { 46, 86, 156, 264, 286 }.Contains(Effect) && battle.Gravity.Active()) return false;

        switch (Effect)
        {
            case 113 when defender.Owner.Spikes == 3:
            case 250 when defender.Owner.ToxicSpikes == 2:
                return false;
        }

        if (new[] { 159, 377, 383 }.Contains(Effect) && attacker.ActiveTurns != 0) return false;

        switch (Effect)
        {
            case 98 when !attacker.Moves.Any(m => m.SelectableBySleepTalk()):
            case 407 when battle.Weather.Get() != "hail":
            case 407 when attacker.Owner.AuroraVeil.Active():
            case 47 when attacker.Owner.Mist.Active():
                return false;
        }

        if (new[] { 80, 493 }.Contains(Effect) && attacker.Substitute > 0) return false;

        switch (Effect)
        {
            case 398 when !attacker.TypeIds.Contains(ElementType.FIRE):
            case 481 when !attacker.TypeIds.Contains(ElementType.ELECTRIC):
            case 376 when defender.TypeIds.Contains(ElementType.GRASS):
            case 343 when defender.TypeIds.Contains(ElementType.GHOST):
            case 107 when defender.Trapping:
            case 182 when attacker.Ingrain:
            case 94 when GetConversion2(attacker, defender, battle) == null:
            case 121 when defender.Infatuated == attacker:
            case 248 when defender.Ability(attacker, this) == Ability.INSOMNIA:
                return false;
        }

        if (new[] { 242, 249 }.Contains(Effect) && (defender.HasMoved || defender.Owner.SelectedAction.IsSwitch ||
                                                    defender.Owner.SelectedAction is Trainer.MoveAction
                                                    {
                                                        Move.DamageClass: DamageClass.STATUS
                                                    }))
            return false;

        switch (Effect)
        {
            case 252 when attacker.AquaRing:
            case 253 when attacker.MagnetRise.Active():
            case 221 when attacker.Owner.HealingWish:
            case 271 when attacker.Owner.LunarDance:
                return false;
        }

        if (new[] { 240, 248, 299, 300 }.Contains(Effect) && !defender.AbilityChangeable()) return false;

        switch (Effect)
        {
            case 300 when !attacker.AbilityGiveable():
            case 241 when attacker.LuckyChant.Active():
            case 125 when attacker.Owner.Safeguard.Active():
            case 293 when !attacker.TypeIds.Intersect(defender.TypeIds).Any():
            case 295 when defender.Ability(attacker, this) == Ability.MULTITYPE:
            case 319 when defender.TypeIds.Count == 0:
            case 171 when attacker.LastMoveDamage != null:
            case 179 when !(attacker.AbilityChangeable() && defender.AbilityGiveable()):
            case 181 when attacker.GetAssistMove() == null:
                return false;
        }

        if (new[] { 112, 117, 184, 195, 196, 279, 307, 345, 350, 354, 356, 362, 378, 384, 454, 488, 499 }
                .Contains(Effect) &&
            defender.HasMoved)
            return false;

        switch (Effect)
        {
            case 192 when !(attacker.AbilityChangeable() && attacker.AbilityGiveable() &&
                            defender.AbilityChangeable() && defender.AbilityGiveable()):
            case 226 when attacker.Owner.Tailwind.Active():
                return false;
        }

        if (new[] { 90, 92, 145 }.Contains(Effect) && attacker.Substitute > 0) return false;

        if (new[] { 85, 92, 169, 178, 188, 206, 388 }.Contains(Effect) && defender.Substitute > 0) return false;

        switch (Effect)
        {
            case 234 when attacker.HeldItem.Power == null || attacker.Ability() == Ability.STICKY_HOLD:
            case 178 when new[] { Ability.STICKY_HOLD }.Contains(attacker.Ability()) ||
                          defender.Ability(attacker, this) == Ability.STICKY_HOLD ||
                          !attacker.HeldItem.CanRemove() || !defender.HeldItem.CanRemove():
            case 202 when attacker.Owner.MudSport.Active():
            case 211 when attacker.Owner.WaterSport.Active():
            case 149 when defender.Owner.FutureSight.Active():
            case 188 when defender.NonVolatileEffect.Current.Any() ||
                          new[] { Ability.INSOMNIA, Ability.VITAL_SPIRIT, Ability.SWEET_VEIL }.Contains(
                              defender.Ability(attacker, this)) ||
                          defender.Yawn.Active():
            case 188 when battle.Terrain.Item?.ToString() == "electric" && attacker.Grounded(battle):
                return false;
        }

        if (new[] { 340, 351 }.Contains(Effect) && !new[] { attacker, defender }.Any(p =>
                p.TypeIds.Contains(ElementType.GRASS) && p.Grounded(battle) &&
                p is { Dive: false, Dig: false, Fly: false, ShadowForce: false }))
            return false;

        if (Effect == 341 && defender.Owner.StickyWeb) return false;

        if (new[] { 112, 117, 356, 362, 384, 454, 488, 499 }.Contains(Effect) &&
            new Random().Next(1, attacker.ProtectionChance + 1) != 1)
            return false;

        switch (Effect)
        {
            case 403 when defender.LastMove == null || defender.LastMove.PP == 0 ||
                          !defender.LastMove.SelectableByInstruct() || defender.LockedMove != null:
            case 378 when defender.TypeIds.Contains(ElementType.GRASS) ||
                          defender.Ability(attacker, this) == Ability.OVERCOAT ||
                          defender.HeldItem == "safety-goggles":
            case 233 when defender.Embargo.Active():
            case 324 when !attacker.HeldItem.HasItem() || defender.HeldItem.HasItem() ||
                          !attacker.HeldItem.CanRemove():
            case 185 when attacker.HeldItem.HasItem() || attacker.HeldItem.LastUsed == null:
            case 430 when
                !defender.HeldItem.HasItem() || !defender.HeldItem.CanRemove() || defender.CorrosiveGas:
            case 114 when defender.Foresight:
            case 217 when defender.MiracleEye:
            case 38 when attacker.NonVolatileEffect.Sleep() || attacker.Hp == attacker.StartingHp ||
                         attacker.Name == "Minior":
            case 427 when attacker.NoRetreat:
            case 99 when attacker.DestinyBondCooldown.Active():
                return false;
        }

        if (new[] { 116, 137, 138, 165 }.Contains(Effect) &&
            new[] { "h-rain", "h-sun", "h-wind" }.Contains(battle.Weather.Get()))
            return false;

        if ((new[] { 8, 420, 444 }.Contains(Effect) && new[] { Ability.DAMP }.Contains(attacker.Ability())) ||
            defender.Ability(attacker, this) == Ability.DAMP)
            return false;

        if (new[] { 223, 453 }.Contains(Effect) && !attacker.HeldItem.IsBerry()) return false;

        switch (Effect)
        {
            case 369 when battle.Terrain.Item?.ToString() == "electric":
            case 352 when battle.Terrain.Item?.ToString() == "grassy":
            case 353 when battle.Terrain.Item?.ToString() == "misty":
            case 395 when battle.Terrain.Item?.ToString() == "psychic":
            case 66 when attacker.Owner.Reflect.Active():
            case 36 when attacker.Owner.LightScreen.Active():
            case 110 when attacker.TypeIds.Contains(ElementType.GHOST) && defender.Curse:
            case 58 when defender.Substitute > 0 || defender.Name != null:
            case 446 when defender.HeldItem.Get() == null:
            case 448 when battle.Terrain.Item == null:
            case 452 when defender.Octolock:
            case 280 when defender.DefenseSplit.HasValue || defender.SpDefSplit.HasValue ||
                          attacker.DefenseSplit.HasValue || attacker.SpDefSplit.HasValue:
            case 281 when defender.AttackSplit.HasValue || defender.SpAtkSplit.HasValue ||
                          attacker.AttackSplit.HasValue || attacker.SpAtkSplit.HasValue:
            case 456 when defender.TypeIds is [ElementType.PSYCHIC] ||
                          defender.Ability(attacker, this) == Ability.RKS_SYSTEM:
            case 83 when !attacker.Moves.Contains(this):
            case 501 when defender.HasMoved || defender.Owner.SelectedAction.IsSwitch ||
                          (defender.Owner.SelectedAction is Trainer.MoveAction selectedAction &&
                           selectedAction.Move.GetPriority(defender, attacker, battle) <= 0):
                return false;
        }

        if (new[] { Ability.QUEENLY_MAJESTY, Ability.DAZZLING, Ability.ARMOR_TAIL }
                .Contains(defender.Ability(attacker, this)) &&
            GetPriority(attacker, defender, battle) > 0)
            return false;

        return true;
    }
}