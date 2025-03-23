// File: MoveEffectsProcessing.cs

namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Processes special move effects that need to run before damage calculation.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string ProcessMoveEffects(DuelPokemon attacker, DuelPokemon defender, Battle battle, ElementType currentType, int? effectChance, bool bounced)
    {
        var msg = "";

        switch (Effect)
        {
            // User Faints
            case 8 or 444:
                msg += attacker.Faint(battle);
                break;
            // User takes damage
            case 420:
                msg += attacker.Damage(attacker.StartingHp / 2, battle, source: "its head exploding (tragic)");
                break;
        }

        // User's type changes
        if (currentType != ElementType.TYPELESS)
        {
            if (attacker.Ability() == Ability.PROTEAN)
            {
                attacker.TypeIds = new List<ElementType> { currentType };
                var t = currentType.ToString().ToLower();
                msg += $"{attacker.Name} transformed into a {t} type using its protean!\n";
            }

            if (attacker.Ability() == Ability.LIBERO)
            {
                attacker.TypeIds = new List<ElementType> { currentType };
                var t = currentType.ToString().ToLower();
                msg += $"{attacker.Name} transformed into a {t} type using its libero!\n";
            }
        }

        // Status effects reflected by magic coat or magic bounce.
        if (IsAffectedByMagicCoat() &&
            (defender.Ability(attacker, this) == Ability.MAGIC_BOUNCE || defender.MagicCoat) &&
            !bounced)
        {
            msg += $"It was reflected by {defender.Name}'s magic bounce!\n";
            var hm = defender.HasMoved;
            msg += Use(defender, attacker, battle, false, bounced: true);
            defender.HasMoved = hm;
            return msg;
        }

        // Check Effect
        if (!CheckEffective(attacker, defender, battle) && !bounced)
        {
            msg += "It had no effect...\n";
            switch (Effect)
            {
                case 120:
                    attacker.FuryCutter = 0;
                    break;
                case 46 or 478:
                    msg += attacker.Damage(attacker.StartingHp / 2, battle, source: "recoil");
                    break;
                case 28 or 81 or 118:
                    attacker.LockedMove = null;
                    break;
            }

            attacker.LastMoveFailed = true;
            return msg;
        }

        // Check Semi-invulnerable - treated as a miss
        if (!CheckSemiInvulnerable(attacker, defender, battle))
        {
            msg += $"{defender.Name} avoided the attack!\n";
            switch (Effect)
            {
                case 120:
                    attacker.FuryCutter = 0;
                    break;
                case 46 or 478:
                    msg += attacker.Damage(attacker.StartingHp / 2, battle, source: "recoil");
                    break;
                case 28 or 81 or 118:
                    attacker.LockedMove = null;
                    break;
            }

            return msg;
        }

        // Check Protection
        var checkProtect = CheckProtect(attacker, defender, battle);
        var checkProtectItem1 = checkProtect.Item1;
        var checkProtectItem2 = checkProtect.Item2;
        if (!checkProtectItem1)
        {
            msg += $"{defender.Name} was protected against the attack!\n";
            msg += checkProtectItem2;
            switch (Effect)
            {
                case 120:
                    attacker.FuryCutter = 0;
                    break;
                case 46 or 478:
                    msg += attacker.Damage(attacker.StartingHp / 2, battle, source: "recoil");
                    break;
                case 28 or 81 or 118:
                    attacker.LockedMove = null;
                    break;
            }

            return msg;
        }

        // Check Hit
        if (!CheckHit(attacker, defender, battle))
        {
            msg += "But it missed!\n";
            switch (Effect)
            {
                case 120:
                    attacker.FuryCutter = 0;
                    break;
                case 46 or 478:
                    msg += attacker.Damage(attacker.StartingHp / 2, battle, source: "recoil");
                    break;
                case 28 or 81 or 118:
                    attacker.LockedMove = null;
                    break;
            }

            return msg;
        }

        // Absorbs
        if (TargetsOpponent() && Effect != 459)
            switch (currentType)
            {
                // Heal
                case ElementType.ELECTRIC when
                    defender.Ability(attacker, this) == Ability.VOLT_ABSORB:
                    msg += $"{defender.Name}'s volt absorb absorbed the move!\n";
                    msg += defender.Heal(defender.StartingHp / 4, "absorbing the move");
                    return msg;
                case ElementType.WATER when
                    defender.Ability(attacker, this) == Ability.WATER_ABSORB:
                    msg += $"{defender.Name}'s water absorb absorbed the move!\n";
                    msg += defender.Heal(defender.StartingHp / 4, "absorbing the move");
                    return msg;
                case ElementType.WATER when
                    defender.Ability(attacker, this) == Ability.DRY_SKIN:
                    msg += $"{defender.Name}'s dry skin absorbed the move!\n";
                    msg += defender.Heal(defender.StartingHp / 4, "absorbing the move");
                    return msg;
                case ElementType.GROUND when
                    defender.Ability(attacker, this) == Ability.EARTH_EATER:
                    msg += $"{defender.Name}'s earth eater absorbed the move!\n";
                    msg += defender.Heal(defender.StartingHp / 4, "absorbing the move");
                    return msg;
                // Stat stage changes
                case ElementType.ELECTRIC when
                    defender.Ability(attacker, this) == Ability.LIGHTNING_ROD:
                    msg += $"{defender.Name}'s lightning rod absorbed the move!\n";
                    msg += defender.AppendSpAtk(1, defender, this);
                    return msg;
                case ElementType.ELECTRIC when
                    defender.Ability(attacker, this) == Ability.MOTOR_DRIVE:
                    msg += $"{defender.Name}'s motor drive absorbed the move!\n";
                    msg += defender.AppendSpeed(1, defender, this);
                    return msg;
                case ElementType.WATER when
                    defender.Ability(attacker, this) == Ability.STORM_DRAIN:
                    msg += $"{defender.Name}'s storm drain absorbed the move!\n";
                    msg += defender.AppendSpAtk(1, defender, this);
                    return msg;
                case ElementType.GRASS when
                    defender.Ability(attacker, this) == Ability.SAP_SIPPER:
                    msg += $"{defender.Name}'s sap sipper absorbed the move!\n";
                    msg += defender.AppendAttack(1, defender, this);
                    return msg;
                case ElementType.FIRE when
                    defender.Ability(attacker, this) == Ability.WELL_BAKED_BODY:
                    msg += $"{defender.Name}'s well baked body absorbed the move!\n";
                    msg += defender.AppendDefense(2, defender, this);
                    return msg;
                // Other
                case ElementType.FIRE when
                    defender.Ability(attacker, this) == Ability.FLASH_FIRE:
                    defender.FlashFire = true;
                    msg += $"{defender.Name} used its flash fire to buff its fire type moves!\n";
                    return msg;
            }

        // Stat stage from type items
        if (defender.Substitute == 0)
        {
            switch (currentType)
            {
                case ElementType.WATER when defender.HeldItem == "absorb-bulb":
                    msg += defender.AppendSpAtk(1, defender, this, "its absorb bulb");
                    defender.HeldItem.Use();
                    break;
                case ElementType.ELECTRIC when defender.HeldItem == "cell-battery":
                    msg += defender.AppendAttack(1, defender, this, "its cell battery");
                    defender.HeldItem.Use();
                    break;
            }

            switch (currentType)
            {
                case ElementType.WATER when defender.HeldItem == "luminous-moss":
                    msg += defender.AppendSpDef(1, defender, this, "its luminous moss");
                    defender.HeldItem.Use();
                    break;
                case ElementType.ICE when defender.HeldItem == "snowball":
                    msg += defender.AppendAttack(1, defender, this, "its snowball");
                    defender.HeldItem.Use();
                    break;
            }
        }

        // Process special move effects
        switch (Effect)
        {
            // Metronome
            case 84:
            {
                attacker.HasMoved = false;
                var random = new Random();
                var raw = battle.MetronomeMoves[random.Next(battle.MetronomeMoves.Count)];
                var newMove = new Move(new Dictionary<string, object>
                {
                    ["id"] = raw.id,
                    ["identifier"] = raw.identifier,
                    ["power"] = raw.power,
                    ["pp"] = raw.pp,
                    ["accuracy"] = raw.accuracy,
                    ["priority"] = raw.priority,
                    ["type_id"] = raw.type_id,
                    ["damage_class_id"] = raw.damage_class_id,
                    ["effect_id"] = raw.effect_id,
                    ["effect_chance"] = raw.effect_chance,
                    ["target_id"] = raw.target_id,
                    ["crit_rate"] = raw.crit_rate,
                    ["min_hits"] = raw.min_hits,
                    ["max_hits"] = raw.max_hits
                });
                msg += newMove.Use(attacker, defender, battle);
                return msg;
            }
            // Brick break - runs before damage calculation
            case 187:
            {
                if (defender.Owner.AuroraVeil.Active())
                {
                    defender.Owner.AuroraVeil.SetTurns(0);
                    msg += $"{defender.Name}'s aurora veil wore off!\n";
                }

                if (defender.Owner.LightScreen.Active())
                {
                    defender.Owner.LightScreen.SetTurns(0);
                    msg += $"{defender.Name}'s light screen wore off!\n";
                }

                if (defender.Owner.Reflect.Active())
                {
                    defender.Owner.Reflect.SetTurns(0);
                    msg += $"{defender.Name}'s reflect wore off!\n";
                }

                break;
            }
            // Sleep talk
            case 98:
            {
                var eligibleMoves = attacker.Moves.Where(m => m.SelectableBySleepTalk()).ToList();
                if (eligibleMoves.Count > 0)
                {
                    var move = eligibleMoves[new Random().Next(eligibleMoves.Count)];
                    msg += move.Use(attacker, defender, battle, false, true);
                    return msg;
                }

                msg += "But it failed!\n";
                return msg;
            }
            // Mirror OldMove/Copy Cat
            case 10 or 243:
            {
                if (defender.LastMove != null)
                {
                    msg += defender.LastMove.Use(attacker, defender, battle, false);
                    return msg;
                }

                msg += "But it failed!\n";
                return msg;
            }
            // Me First
            case 242:
            {
                if (defender.Owner.SelectedAction is Trainer.MoveAction move)
                {
                    msg += move.Move.Use(attacker, defender, battle, false);
                    return msg;
                }

                msg += "But it failed!\n";
                return msg;
            }
            // Assist
            case 181:
            {
                var assistMove = attacker.GetAssistMove();
                if (assistMove != null)
                {
                    msg += assistMove.Use(attacker, defender, battle, false);
                    return msg;
                }

                msg += "But it failed!\n";
                return msg;
            }
            // Spectral Thief
            case 410:
            {
                if (defender.AttackStage > 0)
                {
                    var stage = defender.AttackStage;
                    defender.AttackStage = 0;
                    msg += $"{defender.Name}'s attack stage was reset!\n";
                    msg += attacker.AppendAttack(stage, attacker, this);
                }

                if (defender.DefenseStage > 0)
                {
                    var stage = defender.DefenseStage;
                    defender.DefenseStage = 0;
                    msg += $"{defender.Name}'s defense stage was reset!\n";
                    msg += attacker.AppendDefense(stage, attacker, this);
                }

                if (defender.SpAtkStage > 0)
                {
                    var stage = defender.SpAtkStage;
                    defender.SpAtkStage = 0;
                    msg += $"{defender.Name}'s special attack stage was reset!\n";
                    msg += attacker.AppendSpAtk(stage, attacker, this);
                }

                if (defender.SpDefStage > 0)
                {
                    var stage = defender.SpDefStage;
                    defender.SpDefStage = 0;
                    msg += $"{defender.Name}'s special defense stage was reset!\n";
                    msg += attacker.AppendSpDef(stage, attacker, this);
                }

                if (defender.SpeedStage > 0)
                {
                    var stage = defender.SpeedStage;
                    defender.SpeedStage = 0;
                    msg += $"{defender.Name}'s speed stage was reset!\n";
                    msg += attacker.AppendSpeed(stage, attacker, this);
                }

                if (defender.EvasionStage > 0)
                {
                    var stage = defender.EvasionStage;
                    defender.EvasionStage = 0;
                    msg += $"{defender.Name}'s evasion stage was reset!\n";
                    msg += attacker.AppendEvasion(stage, attacker, this);
                }

                if (defender.AccuracyStage > 0)
                {
                    var stage = defender.AccuracyStage;
                    defender.AccuracyStage = 0;
                    msg += $"{defender.Name}'s accuracy stage was reset!\n";
                    msg += attacker.AppendAccuracy(stage, attacker, this);
                }

                break;
            }
            // Future Sight
            case 149:
                defender.Owner.FutureSight.Set((attacker, this), 3);
                msg += $"{attacker.Name} foresaw an attack!\n";
                return msg;
            // Present
            case 123:
            {
                var action = new Random().Next(1, 5);
                if (action == 1)
                {
                    if (defender.Hp == defender.StartingHp)
                        msg += "It had no effect!\n";
                    else
                        msg += defender.Heal(defender.StartingHp / 4, $"{attacker.Name}'s present");

                    return msg;
                }

                var presentPower = action == 2 ? 40 : action == 3 ? 80 : 120;
                var presentMove = Present(presentPower);
                var (msgadd, hits) = presentMove.Attack(attacker, defender, battle);
                msg += msgadd;
                return msg;
            }
            // Incinerate
            case 315 when defender.HeldItem.IsBerry(false):
            {
                if (defender.Ability(attacker, this) == Ability.STICKY_HOLD)
                {
                    msg += $"{defender.Name}'s sticky hand kept hold of its item!\n";
                }
                else
                {
                    defender.HeldItem.Remove();
                    msg += $"{defender.Name}'s berry was incinerated!\n";
                }

                break;
            }
            // Poltergeist
            case 446:
                msg += $"{defender.Name} is about to be attacked by its {defender.HeldItem.Get()}!\n";
                break;
        }

        return msg;
    }
}