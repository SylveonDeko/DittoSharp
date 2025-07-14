namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Applies post-damage effects.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string ApplyPostEffects(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle, int? effectChance,
        int numHits)
    {
        var msg = "";

        switch (Effect)
        {
            // Stockpile
            case 161:
                attacker.Stockpile += 1;
                msg += $"{attacker.Name} stores energy!\n";
                break;
            case 162:
                msg += attacker.AppendDefense(-attacker.Stockpile, attacker, this);
                msg += attacker.AppendSpDef(-attacker.Stockpile, attacker, this);
                attacker.Stockpile = 0;
                break;
            // Healing
            case 33:
            case 215:
                msg += attacker.Heal(attacker.StartingHp / 2);
                break;
            case 434:
            case 457:
                msg += attacker.Heal(attacker.StartingHp / 4);
                break;
            case 310 when attacker.Ability() == Ability.MEGA_LAUNCHER:
                msg += defender.Heal(defender.StartingHp * 3 / 4);
                break;
            case 310:
                msg += defender.Heal(defender.StartingHp / 2);
                break;
            case 133 when new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()):
                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                break;
            case 133 when battle.Weather.Get() == "h-wind":
                msg += attacker.Heal(attacker.StartingHp / 2);
                break;
            case 133 when !string.IsNullOrEmpty(battle.Weather.Get()):
                msg += attacker.Heal(attacker.StartingHp / 4);
                break;
            case 133:
                msg += attacker.Heal(attacker.StartingHp / 2);
                break;
            case 85:
                defender.LeechSeed = true;
                msg += $"{defender.Name} was seeded!\n";
                break;
            case 163:
            {
                int healFactor;
                switch (attacker.Stockpile)
                {
                    case 1: healFactor = 4; break;
                    case 2: healFactor = 2; break;
                    case 3: healFactor = 1; break;
                    default: healFactor = 4; break;
                }

                msg += attacker.Heal(attacker.StartingHp / healFactor, "stockpiled energy");
                msg += attacker.AppendDefense(-attacker.Stockpile, attacker, this);
                msg += attacker.AppendSpDef(-attacker.Stockpile, attacker, this);
                attacker.Stockpile = 0;
                break;
            }
            case 180:
                attacker.Owner.Wish.Set(attacker.StartingHp / 2);
                msg += $"{attacker.Name} makes a wish!\n";
                break;
            case 382 when battle.Weather.Get() == "sandstorm":
                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                break;
            case 382:
                msg += attacker.Heal(attacker.StartingHp / 2);
                break;
            case 387 when battle.Terrain.Item?.ToString() == "grassy":
                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                break;
            case 387:
                msg += attacker.Heal(attacker.StartingHp / 2);
                break;
            case 388:
                msg += attacker.Heal(defender.GetAttack(battle));
                break;
            case 400:
            {
                var status = defender.NonVolatileEffect.Current;
                defender.NonVolatileEffect.Reset();
                msg += $"{defender.Name}'s {status} was healed!\n";
                msg += attacker.Heal(attacker.StartingHp / 2);
                break;
            }
        }

        // Status effect application
        msg += ApplyStatusEffects(attacker, defender, battle, effectChance);

        switch (Effect)
        {
            case 194:
            case 457:
            case 472:
                attacker.NonVolatileEffect.Reset();
                msg += $"{attacker.Name}'s status was cleared!\n";
                break;
            case 386:
            {
                if (defender.NonVolatileEffect.Burn())
                {
                    defender.NonVolatileEffect.Reset();
                    msg += $"{defender.Name}'s burn was healed!\n";
                }

                break;
            }
        }

        return msg;
    }

    /// <summary>
    ///     Applies status effects from moves.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string ApplyStatusEffects(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle, int? effectChance)
    {
        var msg = "";

        // Status effects
        switch (Effect)
        {
            // Burns
            case 5 or 126 or 201 or 254 or 274 or 333 or 365 or 458 or 465 or 500 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker, this);

                break;
            }
            case 168:
            case 429 when defender.StatIncreased:
                msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker, this);
                break;
            // Tri Attack
            case 37 when effectChance.HasValue:
            {
                var statuses = new[] { "burn", "freeze", "paralysis" };
                var status = statuses[new Random().Next(statuses.Length)];
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                break;
            }
            // Secret Power
            case 464 when effectChance.HasValue:
            {
                var statuses = new[] { "poison", "paralysis", "sleep" };
                var status = statuses[new Random().Next(statuses.Length)];
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                break;
            }
            // Freeze
            case 6 or 261 or 275 or 380 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("freeze", battle, attacker, this);

                break;
            }
            // Paralysis
            case 7 or 153 or 263 or 264 or 276 or 332 or 372 or 396 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker, this);

                break;
            }
            case 68:
                msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker, this);
                break;
            // Poison
            case 3 or 78 or 210 or 447 or 461 when
                effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker, this);

                break;
            }
            case 67:
            case 390:
            case 486:
                msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker, this);
                break;
            // Toxic
            case 203 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker, this);

                break;
            }
            case 34:
                msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker, this);
                break;
            // Sleep
            case 2 when Id == 464 && attacker.Name != "Darkrai":
                msg += $"{attacker.Name} can't use the move!\n";
                break;
            case 2:
                msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this);
                break;
            case 330 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this);

                break;
            }
            case 38:
            {
                msg += attacker.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this, 3,
                    true);
                if (attacker.NonVolatileEffect.Sleep())
                {
                    msg += $"{attacker.Name}'s slumber restores its health back to full!\n";
                    attacker.Hp = attacker.StartingHp;
                }

                break;
            }
            // Confusion
            case 50:
            case 119:
            case 167:
            case 200:
                msg += defender.Confuse(attacker, this);
                break;
            // This checks if attacker.LockedMove is not null as locked_move is cleared if the poke dies to rocky helmet or similar items
            case 28 when attacker.LockedMove != null && attacker.LockedMove.IsLastTurn():
                msg += attacker.Confuse();
                break;
            case 77 or 268 or 334 or 478 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.Confuse(attacker, this);

                break;
            }
            case 497 when defender.StatIncreased:
                msg += defender.Confuse(attacker, this);
                break;
        }

        return msg;
    }

    /// <summary>
    ///     Applies flinch effects.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string ApplyFlinchEffects(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle, int? effectChance,
        int numHits)
    {
        var msg = "";

        // Flinch
        if (!defender.HasMoved)
            for (var hit = 0; hit < numHits; hit++)
            {
                if (defender.Flinched) break;

                if (new[] { 32, 76, 93, 147, 151, 159, 274, 275, 276, 425, 475, 501 }.Contains(Effect) &&
                    effectChance.HasValue)
                {
                    if (new Random().Next(1, 101) <= effectChance)
                        msg += defender.Flinch(move: this, attacker: attacker);
                }
                else if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                {
                    if (attacker.Ability() == Ability.STENCH)
                    {
                        if (new Random().Next(1, 101) <= 10)
                            msg += defender.Flinch(move: this, attacker: attacker, source: "its stench");
                    }
                    else if (attacker.HeldItem == "kings-rock")
                    {
                        if (new Random().Next(1, 101) <= 10)
                            msg += defender.Flinch(move: this, attacker: attacker, source: "its kings rock");
                    }
                    else if (attacker.HeldItem == "razor-fang")
                    {
                        if (new Random().Next(1, 101) <= 10)
                            msg += defender.Flinch(move: this, attacker: attacker, source: "its razor fang");
                    }
                }
            }

        return msg;
    }
}