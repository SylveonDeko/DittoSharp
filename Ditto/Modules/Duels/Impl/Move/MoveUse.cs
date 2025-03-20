// File: MoveUse.cs

namespace Ditto.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Uses this move as attacker on defender.
    /// </summary>
    /// <returns>A string of formatted results of the move.</returns>
    public string Use(DuelPokemon attacker, DuelPokemon defender, Battle battle, bool usePP = true,
        bool overrideSleep = false, bool bounced = false)
    {
        // This handles an edge case for moves that cause the target to swap out
        if (attacker.HasMoved && usePP) return "";

        Used = true;
        if (usePP)
        {
            attacker.HasMoved = true;
            attacker.LastMove = this;
            attacker.BeakBlast = false;
            attacker.DestinyBond = false;
            // Reset semi-invulnerable status in case this is turn 2
            attacker.Dive = false;
            attacker.Dig = false;
            attacker.Fly = false;
            attacker.ShadowForce = false;
        }

        var currentType = GetType(attacker, defender, battle);
        var effectChance = GetEffectChance(attacker, defender, battle);
        var msg = "";

        if (Effect is 5 or 126 or 168 or 254 or 336 or 398 or 458 or 500 && attacker.NonVolatileEffect.Freeze())
        {
            attacker.NonVolatileEffect.Reset();
            msg += $"{attacker.Name} thawed out!\n";
        }

        if (attacker.NonVolatileEffect.Freeze())
        {
            if (usePP && new Random().Next(0, 5) == 0)
            {
                attacker.NonVolatileEffect.Reset();
                msg += $"{attacker.Name} is no longer frozen!\n";
            }
            else
            {
                msg += $"{attacker.Name} is frozen solid!\n";
                if (Effect == 28) attacker.LockedMove = null;

                return msg;
            }
        }

        if (attacker.NonVolatileEffect.Paralysis() && new Random().Next(0, 4) == 0)
        {
            msg += $"{attacker.Name} is paralyzed! It can't move!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return msg;
        }

        if (attacker.Infatuated == defender && new Random().Next(0, 2) == 0)
        {
            msg += $"{attacker.Name} is in love with {defender.Name} and can't bare to hurt them!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return msg;
        }

        if (attacker.Flinched)
        {
            msg += $"{attacker.Name} flinched! It can't move!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return msg;
        }

        if (attacker.NonVolatileEffect.Sleep())
        {
            if (usePP && attacker.NonVolatileEffect.SleepTimer.NextTurn())
            {
                attacker.NonVolatileEffect.Reset();
                msg += $"{attacker.Name} woke up!\n";
            }
            else if (Effect != 93 && Effect != 98 && attacker.Ability() != Ability.COMATOSE && !overrideSleep)
            {
                msg += $"{attacker.Name} is fast asleep!\n";
                if (Effect == 28) attacker.LockedMove = null;

                return msg;
            }
        }

        if (attacker.Confusion.NextTurn()) msg += $"{attacker.Name} is no longer confused!\n";

        if (attacker.Confusion.Active() && new Random().Next(0, 3) == 0)
        {
            msg += $"{attacker.Name} hurt itself in its confusion!\n";
            var (msgadd, numhits) = Confusion().Attack(attacker, attacker, battle);
            msg += msgadd;
            if (Effect == 28) attacker.LockedMove = null;

            return msg;
        }

        if (attacker.Ability() == Ability.TRUANT && attacker.TruantTurn % 2 == 1)
        {
            msg += $"{attacker.Name} is loafing around!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return msg;
        }

        if (!bounced)
        {
            msg += $"{attacker.Name} used {PrettyName}!\n";
            attacker.Metronome.Use(Name);
        }

        // PP
        if (attacker.LockedMove == null && usePP)
        {
            PP -= 1;
            if (defender.Ability(attacker, this) == Ability.PRESSURE && PP != 0)
                if (TargetsOpponent() || new[] { 113, 193, 196, 250, 267 }.Contains(Effect))
                    PP -= 1;

            if (PP == 0) msg += "It ran out of PP!\n";
        }

        // User is using a choice item and had not used a move yet, set that as their only move.
        if (attacker.ChoiceMove == null && usePP)
        {
            if (attacker.HeldItem == "choice-scarf" || attacker.HeldItem == "choice-band" ||
                attacker.HeldItem == "choice-specs")
                attacker.ChoiceMove = this;
            else if (attacker.Ability() == Ability.GORILLA_TACTICS) attacker.ChoiceMove = this;
        }

        // Stance change
        if (attacker.Ability() == Ability.STANCE_CHANGE)
        {
            if (attacker.Name == "Aegislash" &&
                DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                if (attacker.Form("Aegislash-blade"))
                    msg += $"{attacker.Name} draws its blade!\n";

            if (attacker.Name == "Aegislash-blade" && Effect == 356)
                if (attacker.Form("Aegislash"))
                    msg += $"{attacker.Name} readies its shield!\n";
        }

        // Powder damage
        if (attacker.Powdered && currentType == ElementType.FIRE && battle.Weather.Get() != "h-rain")
        {
            msg += attacker.Damage(attacker.StartingHp / 4, battle, source: "its powder exploding");
            return msg;
        }

        // Snatch steal
        if (defender.Snatching && SelectableBySnatch())
        {
            msg += $"{defender.Name} snatched the move!\n";
            msg += Use(defender, attacker, battle, false);
            return msg;
        }

        // Check Fail
        if (!CheckExecutable(attacker, defender, battle))
        {
            msg += "But it failed!\n";
            if (Effect is 28 or 118) attacker.LockedMove = null;

            attacker.LastMoveFailed = true;
            return msg;
        }

        // Setup for multi-turn moves
        if (attacker.LockedMove == null)
        {
            switch (Effect)
            {
                // 2 turn moves
                // During sun, this move does not need to charge
                case 152 when !new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()):
                    attacker.LockedMove = new LockedMove(this, 2);
                    break;
                // During rain, this move does not need to charge
                case 502:
                {
                    if (!new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()))
                        attacker.LockedMove = new LockedMove(this, 2);
                    else
                        // If this move isn't charging, the spatk increase has to happen manually
                        msg += attacker.AppendSpAtk(1, attacker, this);

                    break;
                }
            }

            if (new[] { 40, 76, 81, 146, 156, 256, 257, 264, 273, 332, 333, 366, 451 }.Contains(Effect))
                attacker.LockedMove = new LockedMove(this, 2);

            switch (Effect)
            {
                // 3 turn moves
                case 27:
                    attacker.LockedMove = new LockedMove(this, 3);
                    attacker.Bide = 0;
                    break;
                case 160:
                {
                    attacker.LockedMove = new LockedMove(this, 3);
                    attacker.Uproar.SetTurns(3);
                    if (attacker.NonVolatileEffect.Sleep())
                    {
                        attacker.NonVolatileEffect.Reset();
                        msg += $"{attacker.Name} woke up!\n";
                    }

                    if (defender.NonVolatileEffect.Sleep())
                    {
                        defender.NonVolatileEffect.Reset();
                        msg += $"{defender.Name} woke up!\n";
                    }

                    break;
                }
                // 5 turn moves
                case 118:
                    attacker.LockedMove = new LockedMove(this, 5);
                    break;
                // 2-3 turn moves
                case 28:
                    attacker.LockedMove = new LockedMove(this, new Random().Next(2, 4));
                    break;
            }

            switch (Effect)
            {
                // 2-5 turn moves
                case 160:
                    attacker.LockedMove = new LockedMove(this, new Random().Next(2, 6));
                    break;
                // Semi-invulnerable
                case 256:
                    attacker.Dive = true;
                    break;
                case 257:
                    attacker.Dig = true;
                    break;
                case 156 or 264:
                    attacker.Fly = true;
                    break;
                case 273:
                    attacker.ShadowForce = true;
                    break;
            }
        }

        // Early exits for moves that hit a certain turn when it is not that turn
        // Turn 1 hit moves
        if (Effect == 81 && attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn != 0)
            {
                msg += "It's recharging!\n";
                return msg;
            }
        }
        // Turn 2 hit moves
        else if (new[] { 40, 76, 146, 152, 156, 256, 257, 264, 273, 332, 333, 366, 451, 502 }.Contains(Effect) &&
                 attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn != 1)
            {
                switch (Effect)
                {
                    case 146:
                        msg += attacker.AppendDefense(1, attacker, this);
                        break;
                    case 451 or 502:
                        msg += attacker.AppendSpAtk(1, attacker, this);
                        break;
                    default:
                    {
                        msg += "It's charging up!\n";
                        // Gulp Missile
                        if (Effect == 256 && attacker.Ability() == Ability.GULP_MISSILE &&
                            attacker.Name == "Cramorant")
                        {
                            if (attacker.Hp > attacker.StartingHp / 2)
                            {
                                if (attacker.Form("Cramorant-gulping"))
                                    msg += $"{attacker.Name} gulped up an arrokuda!\n";
                            }
                            else
                            {
                                if (attacker.Form("Cramorant-gorging"))
                                    msg += $"{attacker.Name} gulped up a pikachu!\n";
                            }
                        }

                        break;
                    }
                }

                return msg;
            }
        }
        // Turn 3 hit moves
        else if (Effect == 27)
        {
            if (attacker.LockedMove.Turn != 2)
            {
                msg += "It's storing energy!\n";
                return msg;
            }
        }

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
            // Mirror Move/Copy Cat
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

        var i = 0;
        // Turn 1 hit moves
        if (Effect == 81 && attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn == 0)
            {
                (var msgadd, i) = Attack(attacker, defender, battle);
                msg += msgadd;
            }
        }
        // Turn 2 hit moves
        else if (new[] { 40, 76, 146, 152, 156, 256, 257, 264, 273, 332, 333, 366, 451, 502 }.Contains(Effect) &&
                 attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn == 1)
                if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                {
                    (var msgadd, i) = Attack(attacker, defender, battle);
                    msg += msgadd;
                }
        }
        else
        {
            switch (Effect)
            {
                // Turn 3 hit moves
                case 27:
                {
                    if (attacker.LockedMove.Turn == 2)
                    {
                        msg += defender.Damage(attacker.Bide.Value * 2, battle, this, currentType,
                            attacker);
                        attacker.Bide = null;
                        i = 1;
                    }

                    break;
                }
                // Counter attack moves
                case 228:
                    msg += defender.Damage((int)(1.5 * attacker.LastMoveDamage.Item1), battle, this,
                        currentType, attacker);
                    i = 1;
                    break;
                case 145:
                    msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                case 90:
                    msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                // Static-damage moves
                case 41:
                    msg += defender.Damage(defender.Hp / 2, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 42:
                    msg += defender.Damage(40, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 88:
                    msg += defender.Damage(attacker.Level, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 89:
                {
                    // 0.5-1.5, increments of .1
                    var scale = new Random().Next(0, 11) / 10.0 + 0.5;
                    msg += defender.Damage((int)(attacker.Level * scale), battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                }
                case 131:
                    msg += defender.Damage(20, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 190:
                    msg += defender.Damage(Math.Max(0, defender.Hp - attacker.Hp), battle, this,
                        currentType, attacker);
                    i = 1;
                    break;
                case 39:
                    msg += defender.Damage(defender.Hp, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 321:
                    msg += defender.Damage(attacker.Hp, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 413:
                    msg += defender.Damage(3 * (defender.Hp / 4), battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                // Beat up, a stupid move
                case 155:
                {
                    foreach (var poke in attacker.Owner.Party)
                    {
                        if (defender.Hp == 0) break;

                        if (poke.Hp == 0) continue;

                        if (poke == attacker)
                        {
                            var (msgadd, nh) = Attack(attacker, defender, battle);
                            msg += msgadd;
                            i += nh;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(poke.NonVolatileEffect.Current)) continue;

                            var moveData = new Dictionary<string, object>
                            {
                                ["id"] = 251,
                                ["identifier"] = "beat-up",
                                ["power"] = poke.GetRawAttack() / 10 + 5,
                                ["pp"] = 100,
                                ["accuracy"] = 100,
                                ["priority"] = 0,
                                ["type_id"] = (int)ElementType.DARK,
                                ["damage_class_id"] = (int)DamageClass.PHYSICAL,
                                ["effect_id"] = 1,
                                ["effect_chance"] = null,
                                ["target_id"] = 10,
                                ["crit_rate"] = 0,
                                ["min_hits"] = null,
                                ["max_hits"] = null
                            };
                            var fakeMove = new Move(moveData);
                            var (msgadd, nh) = fakeMove.Attack(attacker, defender, battle);
                            msg += msgadd;
                            i += nh;
                        }
                    }

                    break;
                }
                // Other damaging moves
                default:
                {
                    if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                    {
                        (var msgadd, i) = Attack(attacker, defender, battle);
                        msg += msgadd;
                    }

                    break;
                }
            }
        }

        // Fusion Flare/Bolt effect tracking
        battle.LastMoveEffect = Effect;

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
            // Status effects
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
            case 37 when effectChance.HasValue:
            {
                var statuses = new[] { "burn", "freeze", "paralysis" };
                var status = statuses[new Random().Next(statuses.Length)];
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                break;
            }
            case 464 when effectChance.HasValue:
            {
                var statuses = new[] { "poison", "paralysis", "sleep" };
                var status = statuses[new Random().Next(statuses.Length)];
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                break;
            }
            case 6 or 261 or 275 or 380 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("freeze", battle, attacker, this);

                break;
            }
            case 7 or 153 or 263 or 264 or 276 or 332 or 372 or 396 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker, this);

                break;
            }
            case 68:
                msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker, this);
                break;
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
            case 203 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                    msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker, this);

                break;
            }
            case 34:
                msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker, this);
                break;
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

        // Stage changes
        // +1
        if (Effect is 11 or 209 or 213 or 278 or 313 or 323 or 328 or 392 or 414 or 427 or 468 or 472 or 487)
            msg += attacker.AppendAttack(1, attacker, this);

        if (Effect is 12 or 157 or 161 or 207 or 209 or 323 or 367 or 414 or 427 or 467 or 468 or 472)
            msg += attacker.AppendDefense(1, attacker, this);

        if (Effect is 14 or 212 or 291 or 328 or 392 or 414 or 427 or 472)
            msg += attacker.AppendSpAtk(1, attacker, this);

        if (Effect is 161 or 175 or 207 or 212 or 291 or 367 or 414 or 427 or 472)
            msg += attacker.AppendSpDef(1, attacker, this);

        switch (Effect)
        {
            case 130 or 213 or 291 or 296 or 414 or 427 or 442 or 468 or 469 or 487:
                msg += attacker.AppendSpeed(1, attacker, this);
                break;
            case 17 or 467:
                msg += attacker.AppendEvasion(1, attacker, this);
                break;
            case 278 or 323:
                msg += attacker.AppendAccuracy(1, attacker, this);
                break;
            case 139 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(1, attacker, this);

                break;
            }
            case 140 or 375 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendAttack(1, attacker, this);

                break;
            }
            case 277 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpAtk(1, attacker, this);

                break;
            }
            case 433 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpeed(1, attacker, this);

                break;
            }
            case 167:
                msg += defender.AppendSpAtk(1, attacker, this);
                break;
            // +2
            case 51 or 309:
                msg += attacker.AppendAttack(2, attacker, this);
                break;
            case 52 or 453:
                msg += attacker.AppendDefense(2, attacker, this);
                break;
        }

        if (Effect is 53 or 285 or 309 or 313 or 366) msg += attacker.AppendSpeed(2, attacker, this);

        if (Effect is 54 or 309 or 366) msg += attacker.AppendSpAtk(2, attacker, this);

        switch (Effect)
        {
            case 55 or 366:
                msg += attacker.AppendSpDef(2, attacker, this);
                break;
            case 109:
                msg += attacker.AppendEvasion(2, attacker, this);
                break;
            case 119 or 432 or 483:
                msg += defender.AppendAttack(2, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 432:
                msg += defender.AppendSpAtk(2, attacker, this);
                break;
            case 359 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(2, attacker, this);

                break;
            }
            // -1
            case 19 or 206 or 344 or 347 or 357 or 365 or 388 or 412:
                msg += defender.AppendAttack(-1, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 20 or 206:
                msg += defender.AppendDefense(-1, attacker, this);
                break;
            case 344 or 347 or 358 or 412:
                msg += defender.AppendSpAtk(-1, attacker, this);
                break;
            case 428:
                msg += defender.AppendSpDef(-1, attacker, this);
                break;
            case 331 or 390:
                msg += defender.AppendSpeed(-1, attacker, this);
                break;
            case 24:
                msg += defender.AppendAccuracy(-1, attacker, this);
                break;
            case 25 or 259:
                msg += defender.AppendEvasion(-1, attacker, this);
                break;
            case 69 or 396 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendAttack(-1, attacker, this);

                break;
            }
            case 70 or 397 or 435 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendDefense(-1, attacker, this);

                break;
            }
            case 475:
            {
                // This one has two different chance percents, one has to be hardcoded
                if (new Random().Next(1, 101) <= 50) msg += defender.AppendDefense(-1, attacker, this);

                break;
            }
            case 21 or 71 or 357 or 477 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpeed(-1, attacker, this);

                break;
            }
            case 72 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpAtk(-1, attacker, this);

                break;
            }
            case 73 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-1, attacker, this);

                break;
            }
            case 74 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendAccuracy(-1, attacker, this);

                break;
            }
            case 183:
                msg += attacker.AppendAttack(-1, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 183 or 230 or 309 or 335 or 405 or 438 or 442:
                msg += attacker.AppendDefense(-1, attacker, this);
                break;
            case 480:
                msg += attacker.AppendSpAtk(-1, attacker, this);
                break;
        }

        if (Effect is 230 or 309 or 335) msg += attacker.AppendSpDef(-1, attacker, this);

        switch (Effect)
        {
            case 219 or 335:
                msg += attacker.AppendSpeed(-1, attacker, this);
                break;
            // -2
            case 59 or 169:
                msg += defender.AppendAttack(-2, attacker, this);
                break;
            case 60 or 483:
                msg += defender.AppendDefense(-2, attacker, this);
                break;
            case 61:
                msg += defender.AppendSpeed(-2, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 62 or 169 or 266:
                msg += defender.AppendSpAtk(-2, attacker, this);
                break;
            case 63:
                msg += defender.AppendSpDef(-2, attacker, this);
                break;
            case 272 or 297 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-2, attacker, this);

                break;
            }
            case 205:
                msg += attacker.AppendSpAtk(-2, attacker, this);
                break;
            case 479:
                msg += attacker.AppendSpeed(-2, attacker, this);
                break;
            // other
            case 26:
                attacker.AttackStage = 0;
                attacker.DefenseStage = 0;
                attacker.SpAtkStage = 0;
                attacker.SpDefStage = 0;
                attacker.SpeedStage = 0;
                attacker.AccuracyStage = 0;
                attacker.EvasionStage = 0;
                defender.AttackStage = 0;
                defender.DefenseStage = 0;
                defender.SpAtkStage = 0;
                defender.SpDefStage = 0;
                defender.SpeedStage = 0;
                defender.AccuracyStage = 0;
                defender.EvasionStage = 0;
                msg += "All pokemon had their stat stages reset!\n";
                break;
            case 305:
                defender.AttackStage = 0;
                defender.DefenseStage = 0;
                defender.SpAtkStage = 0;
                defender.SpDefStage = 0;
                defender.SpeedStage = 0;
                defender.AccuracyStage = 0;
                defender.EvasionStage = 0;
                msg += $"{defender.Name} had their stat stages reset!\n";
                break;
            case 141 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                {
                    msg += attacker.AppendAttack(1, attacker, this);
                    msg += attacker.AppendDefense(1, attacker, this);
                    msg += attacker.AppendSpAtk(1, attacker, this);
                    msg += attacker.AppendSpDef(1, attacker, this);
                    msg += attacker.AppendSpeed(1, attacker, this);
                }

                break;
            }
            case 143:
                msg += attacker.Damage(attacker.StartingHp / 2, battle);
                msg += attacker.AppendAttack(12, attacker, this);
                break;
            case 317:
            {
                var amount = 1;
                if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get())) amount = 2;

                msg += attacker.AppendAttack(amount, attacker, this);
                msg += attacker.AppendSpAtk(amount, attacker, this);
                break;
            }
            case 364 when defender.NonVolatileEffect.Poison():
                msg += defender.AppendAttack(-1, attacker, this);
                msg += defender.AppendSpAtk(-1, attacker, this);
                msg += defender.AppendSpeed(-1, attacker, this);
                break;
            case 329:
                msg += attacker.AppendDefense(3, attacker, this);
                break;
            case 322:
                msg += attacker.AppendSpAtk(3, attacker, this);
                break;
            case 227:
            {
                var validStats = new List<Func<int, DuelPokemon, Move, string, bool, string>>();

                if (attacker.AttackStage < 6)
                    validStats.Add(attacker.AppendAttack);
                if (attacker.DefenseStage < 6)
                    validStats.Add(attacker.AppendDefense);
                if (attacker.SpAtkStage < 6)
                    validStats.Add(attacker.AppendSpAtk);
                if (attacker.SpDefStage < 6)
                    validStats.Add(attacker.AppendSpDef);
                if (attacker.SpeedStage < 6)
                    validStats.Add(attacker.AppendSpeed);
                if (attacker.EvasionStage < 6)
                    validStats.Add(attacker.AppendEvasion);
                if (attacker.AccuracyStage < 6)
                    validStats.Add(attacker.AppendAccuracy);

                if (validStats.Count > 0)
                {
                    var statRaiseFunc =
                        validStats[new Random().Next(validStats.Count)];
                    msg += statRaiseFunc(2, attacker, this, "", false);
                }
                else
                {
                    msg += $"None of {attacker.Name}'s stats can go any higher!\n";
                }

                break;
            }
            case 473:
            {
                var rawAtk = attacker.GetRawAttack() + attacker.GetRawSpAtk();
                var rawDef = attacker.GetRawDefense() + attacker.GetRawSpDef();
                if (rawAtk > rawDef)
                {
                    msg += attacker.AppendAttack(1, attacker, this);
                    msg += attacker.AppendSpAtk(1, attacker, this);
                }
                else
                {
                    msg += attacker.AppendDefense(1, attacker, this);
                    msg += attacker.AppendSpDef(1, attacker, this);
                }

                break;
            }
            case 485:
                msg += attacker.Damage(attacker.StartingHp / 2, battle);
                msg += attacker.AppendAttack(2, attacker, this);
                msg += attacker.AppendSpAtk(2, attacker, this);
                msg += attacker.AppendSpeed(2, attacker, this);
                break;
        }

        // Flinch
        if (!defender.HasMoved)
            for (var hit = 0; hit < i; hit++)
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

        switch (Effect)
        {
            // Move locking
            case 87 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects its move from being disabled!\n";
                break;
            case 87:
                defender.Disable.Set(defender.LastMove, new Random().Next(4, 8));
                msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was disabled!\n";
                break;
            case 176 when defender.Ability(attacker, this) == Ability.OBLIVIOUS:
                msg += $"{defender.Name} is too oblivious to be taunted!\n";
                break;
            case 176 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being taunted!\n";
                break;
            case 176:
            {
                if (defender.HasMoved)
                    defender.Taunt.SetTurns(4);
                else
                    defender.Taunt.SetTurns(3);

                msg += $"{defender.Name} is being taunted!\n";
                break;
            }
            case 91 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being encored!\n";
                break;
            case 91:
            {
                defender.Encore.Set(defender.LastMove, 4);
                if (!defender.HasMoved) defender.Owner.SelectedAction = new Trainer.MoveAction(defender.LastMove);

                msg += $"{defender.Name} is giving an encore!\n";
                break;
            }
            case 166 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being tormented!\n";
                break;
            case 166:
                defender.Torment = true;
                msg += $"{defender.Name} is tormented!\n";
                break;
            case 193:
                attacker.Imprison = true;
                msg += $"{attacker.Name} imprisons!\n";
                break;
            case 237 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                break;
            case 237:
                defender.HealBlock.SetTurns(5);
                msg += $"{defender.Name} is blocked from healing!\n";
                break;
            case 496 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                break;
            case 496:
                defender.HealBlock.SetTurns(2);
                msg += $"{defender.Name} is blocked from healing!\n";
                break;
            // Weather changing
            case 116:
                msg += battle.Weather.Set("sandstorm", attacker);
                break;
            case 137:
                msg += battle.Weather.Set("rain", attacker);
                break;
            case 138:
                msg += battle.Weather.Set("sun", attacker);
                break;
            case 165:
                msg += battle.Weather.Set("hail", attacker);
                break;
            // Terrain changing
            case 352:
                msg += battle.Terrain.Set("grassy", attacker);
                break;
            case 353:
                msg += battle.Terrain.Set("misty", attacker);
                break;
            case 369:
                msg += battle.Terrain.Set("electric", attacker);
                break;
            case 395:
                msg += battle.Terrain.Set("psychic", attacker);
                break;
        }

        // Protection
        if (new[] { 112, 117, 279, 356, 362, 384, 454, 488, 499 }.Contains(Effect))
        {
            attacker.ProtectionUsed = true;
            attacker.ProtectionChance *= 3;
        }

        switch (Effect)
        {
            case 112:
                attacker.Protect = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
            case 117:
                attacker.Endure = true;
                msg += $"{attacker.Name} braced itself!\n";
                break;
            case 279:
                attacker.WideGuard = true;
                msg += $"Wide guard protects {attacker.Name}!\n";
                break;
            case 350:
                attacker.CraftyShield = true;
                msg += $"A crafty shield protects {attacker.Name} from status moves!\n";
                break;
            case 356:
                attacker.KingShield = true;
                msg += $"{attacker.Name} shields itself!\n";
                break;
            case 362:
                attacker.SpikyShield = true;
                msg += $"{attacker.Name} shields itself!\n";
                break;
            case 377:
                attacker.MatBlock = true;
                msg += $"{attacker.Name} shields itself!\n";
                break;
            case 384:
                attacker.BanefulBunker = true;
                msg += $"{attacker.Name} bunkers down!\n";
                break;
            case 307:
                attacker.QuickGuard = true;
                msg += $"{attacker.Name} guards itself!\n";
                break;
            case 454:
                attacker.Obstruct = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
            case 488:
                attacker.SilkTrap = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
            case 499:
                attacker.BurningBulwark = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
        }

        // Life orb
        if (
            attacker.HeldItem == "life-orb" &&
            defender.Owner.HasAlivePokemon() &&
            DamageClass != DamageClass.STATUS &&
            (attacker.Ability() != Ability.SHEER_FORCE || EffectChance == null) &&
            Effect != 149
        )
            msg += attacker.Damage(attacker.StartingHp / 10, battle, source: "its life orb");

        // Swap outs
        // A poke is force-swapped out before activating red-card
        if (Effect is 29 or 314)
        {
            var swaps = defender.Owner.ValidSwaps(attacker, battle, false);
            if (swaps.Count == 0)
            {
                // Do nothing
            }
            else if (defender.Ability(attacker, this) == Ability.SUCTION_CUPS)
            {
                msg += $"{defender.Name}'s suction cups kept it in place!\n";
            }
            else if (defender.Ability(attacker, this) == Ability.GUARD_DOG)
            {
                msg += $"{defender.Name}'s guard dog kept it in place!\n";
            }
            else if (defender.Ingrain)
            {
                msg += $"{defender.Name} is ingrained in the ground!\n";
            }
            else
            {
                msg += $"{defender.Name} fled in fear!\n";
                msg += defender.Remove(battle);
                var idx = swaps[new Random().Next(swaps.Count)];
                defender.Owner.SwitchPoke(idx, true);
                msg += defender.Owner.CurrentPokemon.SendOut(attacker, battle);
                // Safety in case the poke dies on send out.
                if (defender.Owner.CurrentPokemon != null) defender.Owner.CurrentPokemon.HasMoved = true;
            }
        }
        // A red-card forces the attacker to swap to a random poke, even if they used a switch out move
        else if (defender.HeldItem == "red-card" && defender.Hp > 0 && DamageClass != DamageClass.STATUS)
        {
            var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
            if (swaps.Count == 0)
            {
                // Do nothing
            }
            else if (attacker.Ability(defender, this) == Ability.SUCTION_CUPS)
            {
                msg += $"{attacker.Name}'s suction cups kept it in place from {defender.Name}'s red card!\n";
                defender.HeldItem.Use();
            }
            else if (attacker.Ability(defender, this) == Ability.GUARD_DOG)
            {
                msg += $"{attacker.Name}'s guard dog kept it in place from {defender.Name}'s red card!\n";
                defender.HeldItem.Use();
            }
            else if (attacker.Ingrain)
            {
                msg += $"{attacker.Name} is ingrained in the ground from {defender.Name}'s red card!\n";
                defender.HeldItem.Use();
            }
            else
            {
                msg += $"{defender.Name} held up its red card against {attacker.Name}!\n";
                defender.HeldItem.Use();
                msg += attacker.Remove(battle);
                var idx = swaps[new Random().Next(swaps.Count)];
                attacker.Owner.SwitchPoke(idx, true);
                msg += attacker.Owner.CurrentPokemon.SendOut(defender, battle);
                // Safety in case the poke dies on send out.
                if (attacker.Owner.CurrentPokemon != null) attacker.Owner.CurrentPokemon.HasMoved = true;
            }
        }
        else if (new[] { 128, 154, 229, 347 }.Contains(Effect))
        {
            var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
            if (swaps.Count > 0)
            {
                msg += $"{attacker.Name} went back!\n";
                if (Effect == 128) attacker.Owner.BatonPass = new BatonPass(attacker);

                msg += attacker.Remove(battle);
                // Force this pokemon to immediately return to be attacked
                attacker.Owner.MidTurnRemove = true;
            }
        }

        // Trapping
        if (new[] { 107, 374, 385, 449, 452 }.Contains(Effect) && !defender.Trapping)
        {
            defender.Trapping = true;
            msg += $"{defender.Name} can't escape!\n";
        }

        if (Effect == 449 && !attacker.Trapping)
        {
            attacker.Trapping = true;
            msg += $"{attacker.Name} can't escape!\n";
        }

        // Attacker faints
        if (new[] { 169, 221, 271, 321 }.Contains(Effect)) msg += attacker.Faint(battle);

        switch (Effect)
        {
            // Struggle
            case 255:
                msg += attacker.Damage(attacker.StartingHp / 4, battle, attacker: attacker);
                break;
            // Pain Split
            case 92:
            {
                var hp = (attacker.Hp + defender.Hp) / 2;
                attacker.Hp = Math.Min(attacker.StartingHp, hp);
                defender.Hp = Math.Min(defender.StartingHp, hp);
                msg += "The battlers share their pain!\n";
                break;
            }
            // Spite
            case 101:
                defender.LastMove.PP = Math.Max(0, defender.LastMove.PP - 4);
                msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was reduced!\n";
                break;
            // Eerie Spell
            case 439 when defender.LastMove != null:
                defender.LastMove.PP = Math.Max(0, defender.LastMove.PP - 3);
                msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was reduced!\n";
                break;
            // Heal Bell
            case 103:
            {
                foreach (var poke in attacker.Owner.Party) poke.NonVolatileEffect.Reset();

                msg += $"A bell chimed, and all of {attacker.Owner.Name}'s pokemon had status conditions removed!\n";
                break;
            }
            // Psycho Shift
            case 235:
            {
                var transferedStatus = attacker.NonVolatileEffect.Current;
                msg += defender.NonVolatileEffect.ApplyStatus(transferedStatus, battle, attacker, this);
                if (defender.NonVolatileEffect.Current == transferedStatus)
                {
                    attacker.NonVolatileEffect.Reset();
                    msg += $"{attacker.Name}'s {transferedStatus} was transfered to {defender.Name}!\n";
                }
                else
                {
                    msg += "But it failed!\n";
                }

                break;
            }
            // Defog
            case 259:
                defender.Owner.Spikes = 0;
                defender.Owner.ToxicSpikes = 0;
                defender.Owner.StealthRock = false;
                defender.Owner.StickyWeb = false;
                defender.Owner.AuroraVeil = new ExpiringEffect(0);
                defender.Owner.LightScreen = new ExpiringEffect(0);
                defender.Owner.Reflect = new ExpiringEffect(0);
                defender.Owner.Mist = new ExpiringEffect(0);
                defender.Owner.Safeguard = new ExpiringEffect(0);
                attacker.Owner.Spikes = 0;
                attacker.Owner.ToxicSpikes = 0;
                attacker.Owner.StealthRock = false;
                attacker.Owner.StickyWeb = false;
                battle.Terrain.End();
                msg += $"{attacker.Name} blew away the fog!\n";
                break;
            // Trick room
            case 260:
            {
                if (battle.TrickRoom.Active())
                {
                    battle.TrickRoom.SetTurns(0);
                    msg += "The Dimensions returned back to normal!\n";
                }
                else
                {
                    battle.TrickRoom.SetTurns(5);
                    msg += $"{attacker.Name} twisted the dimensions!\n";
                }

                break;
            }
            // Magic Room
            case 287:
            {
                if (battle.MagicRoom.Active())
                {
                    battle.MagicRoom.SetTurns(0);
                    msg += "The room returns to normal, and held items regain their effect!\n";
                }
                else
                {
                    battle.MagicRoom.SetTurns(5);
                    msg += "A bizzare area was created, and pokemon's held items lost their effect!\n";
                }

                break;
            }
            // Wonder Room
            case 282:
            {
                if (battle.WonderRoom.Active())
                {
                    battle.WonderRoom.SetTurns(0);
                    msg += "The room returns to normal, and stats swap back to what they were before!\n";
                }
                else
                {
                    battle.WonderRoom.SetTurns(5);
                    msg += "A bizzare area was created, and pokemon's defense and special defense were swapped!\n";
                }

                break;
            }
            // Perish Song
            case 115:
            {
                msg += "All pokemon hearing the song will faint after 3 turns!\n";
                if (attacker.PerishSong.Active())
                    msg += $"{attacker.Name} is already under the effect of perish song!\n";
                else
                    attacker.PerishSong.SetTurns(4);

                if (defender.PerishSong.Active())
                    msg += $"{defender.Name} is already under the effect of perish song!\n";
                else if (defender.Ability(attacker, this) == Ability.SOUNDPROOF)
                    msg += $"{defender.Name}'s soundproof protects it from hearing the song!\n";
                else
                    defender.PerishSong.SetTurns(4);

                break;
            }
            // Nightmare
            case 108:
                defender.Nightmare = true;
                msg += $"{defender.Name} fell into a nightmare!\n";
                break;
            // Gravity
            case 216:
            {
                battle.Gravity.SetTurns(5);
                msg += "Gravity intensified!\n";
                defender.Telekinesis.SetTurns(0);
                if (defender.Fly)
                {
                    defender.Fly = false;
                    defender.LockedMove = null;
                    msg += $"{defender.Name} fell from the sky!\n";
                }

                break;
            }
            // Spikes
            case 113:
                defender.Owner.Spikes += 1;
                msg += $"Spikes were scattered around the feet of {defender.Owner.Name}'s team!\n";
                break;
            // Toxic Spikes
            case 250:
                defender.Owner.ToxicSpikes += 1;
                msg += $"Toxic spikes were scattered around the feet of {defender.Owner.Name}'s team!\n";
                break;
            // Stealth Rock
            case 267:
                defender.Owner.StealthRock = true;
                msg += $"Pointed stones float in the air around {defender.Owner.Name}'s team!\n";
                break;
            // Sticky Web
            case 341:
                defender.Owner.StickyWeb = true;
                msg += $"A sticky web is shot around the feet of {defender.Owner.Name}'s team!\n";
                break;
            // Defense curl
            case 157 when !attacker.DefenseCurl:
                attacker.DefenseCurl = true;
                break;
            // Psych Up
            case 144:
                attacker.AttackStage = defender.AttackStage;
                attacker.DefenseStage = defender.DefenseStage;
                attacker.SpAtkStage = defender.SpAtkStage;
                attacker.SpDefStage = defender.SpDefStage;
                attacker.SpeedStage = defender.SpeedStage;
                attacker.AccuracyStage = defender.AccuracyStage;
                attacker.EvasionStage = defender.EvasionStage;
                attacker.FocusEnergy = defender.FocusEnergy;
                msg += "It psyched itself up!\n";
                break;
            // Conversion
            case 31:
            {
                var t = attacker.Moves[0].Type;
                if (!Enum.IsDefined(typeof(ElementType), t)) t = ElementType.NORMAL;

                attacker.TypeIds = new List<ElementType> { t };
                var typeName = t.ToString().ToLower();
                msg += $"{attacker.Name} transformed into a {typeName} type!\n";
                break;
            }
            // Conversion 2
            case 94:
            {
                var conversion2 = GetConversion2(attacker, defender, battle);
                if (conversion2.HasValue)
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
                var (protectItem1, s) = CheckProtect(attacker, defender, battle);
                if (!protectItem1)
                {
                    msg += $"{defender.Name} was protected against the attack!\n";
                    msg += s;
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
                    // Mirror Move/Copy Cat
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

                var numHits1 = 0;
                // Turn 1 hit moves
                if (Effect == 81 && attacker.LockedMove != null)
                {
                    if (attacker.LockedMove.Turn == 0)
                    {
                        (var msgadd, numHits1) = Attack(attacker, defender, battle);
                        msg += msgadd;
                    }
                }
                // Turn 2 hit moves
                else if (new[] { 40, 76, 146, 152, 156, 256, 257, 264, 273, 332, 333, 366, 451, 502 }
                             .Contains(Effect) && attacker.LockedMove != null)
                {
                    if (attacker.LockedMove.Turn == 1)
                        if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                        {
                            (var msgadd, numHits1) = Attack(attacker, defender, battle);
                            msg += msgadd;
                        }
                }
                else
                {
                    switch (Effect)
                    {
                        // Turn 3 hit moves
                        case 27:
                        {
                            if (attacker.LockedMove.Turn == 2)
                            {
                                msg += defender.Damage(attacker.Bide.Value * 2, battle, this, currentType,
                                    attacker);
                                attacker.Bide = null;
                                numHits1 = 1;
                            }

                            break;
                        }
                        // Counter attack moves
                        case 228:
                            msg += defender.Damage((int)(1.5 * attacker.LastMoveDamage.Item1), battle, this,
                                currentType, attacker);
                            numHits1 = 1;
                            break;
                        case 145:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                                attacker);
                            numHits1 = 1;
                            break;
                        case 90:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                                attacker);
                            numHits1 = 1;
                            break;
                        // Static-damage moves
                        case 41:
                            msg += defender.Damage(defender.Hp / 2, battle, this, currentType,
                                attacker);
                            numHits1 = 1;
                            break;
                        case 42:
                            msg += defender.Damage(40, battle, this, currentType, attacker);
                            numHits1 = 1;
                            break;
                        case 88:
                            msg += defender.Damage(attacker.Level, battle, this, currentType,
                                attacker);
                            numHits1 = 1;
                            break;
                        case 89:
                        {
                            // 0.5-1.5, increments of .1
                            var scale = new Random().Next(0, 11) / 10.0 + 0.5;
                            msg += defender.Damage((int)(attacker.Level * scale), battle, this, currentType,
                                attacker);
                            numHits1 = 1;
                            break;
                        }
                        case 131:
                            msg += defender.Damage(20, battle, this, currentType, attacker);
                            numHits1 = 1;
                            break;
                        case 190:
                            msg += defender.Damage(Math.Max(0, defender.Hp - attacker.Hp), battle, this,
                                currentType, attacker);
                            numHits1 = 1;
                            break;
                        case 39:
                            msg += defender.Damage(defender.Hp, battle, this, currentType, attacker);
                            numHits1 = 1;
                            break;
                        case 321:
                            msg += defender.Damage(attacker.Hp, battle, this, currentType, attacker);
                            numHits1 = 1;
                            break;
                        case 413:
                            msg += defender.Damage(3 * (defender.Hp / 4), battle, this, currentType,
                                attacker);
                            numHits1 = 1;
                            break;
                        // Beat up, a stupid move
                        case 155:
                        {
                            foreach (var poke in attacker.Owner.Party)
                            {
                                if (defender.Hp == 0) break;

                                if (poke.Hp == 0) continue;

                                if (poke == attacker)
                                {
                                    var (msgadd, nh) = Attack(attacker, defender, battle);
                                    msg += msgadd;
                                    numHits1 += nh;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(poke.NonVolatileEffect.Current)) continue;

                                    var moveData = new Dictionary<string, object>
                                    {
                                        ["id"] = 251,
                                        ["identifier"] = "beat-up",
                                        ["power"] = poke.GetRawAttack() / 10 + 5,
                                        ["pp"] = 100,
                                        ["accuracy"] = 100,
                                        ["priority"] = 0,
                                        ["type_id"] = (int)ElementType.DARK,
                                        ["damage_class_id"] = (int)DamageClass.PHYSICAL,
                                        ["effect_id"] = 1,
                                        ["effect_chance"] = null,
                                        ["target_id"] = 10,
                                        ["crit_rate"] = 0,
                                        ["min_hits"] = null,
                                        ["max_hits"] = null
                                    };
                                    var fakeMove = new Move(moveData);
                                    var (msgadd, nh) = fakeMove.Attack(attacker, defender, battle);
                                    msg += msgadd;
                                    numHits1 += nh;
                                }
                            }

                            break;
                        }
                        // Other damaging moves
                        default:
                        {
                            if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                            {
                                (var msgadd, numHits1) = Attack(attacker, defender, battle);
                                msg += msgadd;
                            }

                            break;
                        }
                    }
                }

                // Fusion Flare/Bolt effect tracking
                battle.LastMoveEffect = Effect;

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
                    case 33 or 215:
                        msg += attacker.Heal(attacker.StartingHp / 2);
                        break;
                    case 434 or 457:
                        msg += attacker.Heal(attacker.StartingHp / 4);
                        break;
                    case 310:
                    {
                        if (attacker.Ability() == Ability.MEGA_LAUNCHER)
                            msg += defender.Heal(defender.StartingHp * 3 / 4);
                        else
                            msg += defender.Heal(defender.StartingHp / 2);

                        break;
                    }
                    case 133:
                    {
                        if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                            msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                        else if (battle.Weather.Get() == "h-wind")
                            msg += attacker.Heal(attacker.StartingHp / 2);
                        else if (!string.IsNullOrEmpty(battle.Weather.Get()))
                            msg += attacker.Heal(attacker.StartingHp / 4);
                        else
                            msg += attacker.Heal(attacker.StartingHp / 2);

                        break;
                    }
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
                    case 382:
                    {
                        if (battle.Weather.Get() == "sandstorm")
                            msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                        else
                            msg += attacker.Heal(attacker.StartingHp / 2);

                        break;
                    }
                    case 387:
                    {
                        if (battle.Terrain.Item?.ToString() == "grassy")
                            msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                        else
                            msg += attacker.Heal(attacker.StartingHp / 2);

                        break;
                    }
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
                    // Status effects
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
                    case 37 when effectChance.HasValue:
                    {
                        var statuses = new[] { "burn", "freeze", "paralysis" };
                        var status = statuses[new Random().Next(statuses.Length)];
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                        break;
                    }
                    case 464 when effectChance.HasValue:
                    {
                        var statuses = new[] { "poison", "paralysis", "sleep" };
                        var status = statuses[new Random().Next(statuses.Length)];
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                        break;
                    }
                    case 6 or 261 or 275 or 380 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("freeze", battle, attacker, this);

                        break;
                    }
                    case 7 or 153 or 263 or 264 or 276 or 332 or 372 or 396 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker,
                                this);

                        break;
                    }
                    case 68:
                        msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker, this);
                        break;
                    case 3 or 78 or 210 or 447 or 461 when
                        effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker, this);

                        break;
                    }
                    case 67 or 390 or 486:
                        msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker, this);
                        break;
                    case 203 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker,
                                this);

                        break;
                    }
                    case 34:
                        msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker, this);
                        break;
                    case 2:
                    {
                        if (Id == 464 && attacker.Name != "Darkrai")
                            msg += $"{attacker.Name} can't use the move!\n";
                        else
                            msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this);

                        break;
                    }
                    case 330 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this);

                        break;
                    }
                    case 38:
                    {
                        msg += attacker.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this,
                            3, true);
                        if (attacker.NonVolatileEffect.Sleep())
                        {
                            msg += $"{attacker.Name}'s slumber restores its health back to full!\n";
                            attacker.Hp = attacker.StartingHp;
                        }

                        break;
                    }
                    case 50 or 119 or 167 or 200:
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

                switch (Effect)
                {
                    case 194 or 457 or 472:
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

                // Stage changes
                // +1
                if (Effect is 11 or 209 or 213 or 278 or 313 or 323 or 328 or 392 or 414 or 427 or 468 or 472 or 487)
                    msg += attacker.AppendAttack(1, attacker, this);

                if (Effect is 12 or 157 or 161 or 207 or 209 or 323 or 367 or 414 or 427 or 467 or 468 or 472)
                    msg += attacker.AppendDefense(1, attacker, this);

                if (Effect is 14 or 212 or 291 or 328 or 392 or 414 or 427 or 472)
                    msg += attacker.AppendSpAtk(1, attacker, this);

                if (Effect is 161 or 175 or 207 or 212 or 291 or 367 or 414 or 427 or 472)
                    msg += attacker.AppendSpDef(1, attacker, this);

                switch (Effect)
                {
                    case 130 or 213 or 291 or 296 or 414 or 427 or 442 or 468 or 469 or 487:
                        msg += attacker.AppendSpeed(1, attacker, this);
                        break;
                    case 17 or 467:
                        msg += attacker.AppendEvasion(1, attacker, this);
                        break;
                    case 278 or 323:
                        msg += attacker.AppendAccuracy(1, attacker, this);
                        break;
                    case 139 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(1, attacker, this);

                        break;
                    }
                    case 140 or 375 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendAttack(1, attacker, this);

                        break;
                    }
                    case 277 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpAtk(1, attacker, this);

                        break;
                    }
                    case 433 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpeed(1, attacker, this);

                        break;
                    }
                    case 167:
                        msg += defender.AppendSpAtk(1, attacker, this);
                        break;
                    // +2
                    case 51 or 309:
                        msg += attacker.AppendAttack(2, attacker, this);
                        break;
                    case 52 or 453:
                        msg += attacker.AppendDefense(2, attacker, this);
                        break;
                }

                if (Effect is 53 or 285 or 309 or 313 or 366) msg += attacker.AppendSpeed(2, attacker, this);

                if (Effect is 54 or 309 or 366) msg += attacker.AppendSpAtk(2, attacker, this);

                switch (Effect)
                {
                    case 55 or 366:
                        msg += attacker.AppendSpDef(2, attacker, this);
                        break;
                    case 109:
                        msg += attacker.AppendEvasion(2, attacker, this);
                        break;
                    case 119 or 432 or 483:
                        msg += defender.AppendAttack(2, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 432:
                        msg += defender.AppendSpAtk(2, attacker, this);
                        break;
                    case 359 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(2, attacker, this);

                        break;
                    }
                    // -1
                    case 19 or 206 or 344 or 347 or 357 or 365 or 388 or 412:
                        msg += defender.AppendAttack(-1, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 20 or 206:
                        msg += defender.AppendDefense(-1, attacker, this);
                        break;
                    case 344 or 347 or 358 or 412:
                        msg += defender.AppendSpAtk(-1, attacker, this);
                        break;
                    case 428:
                        msg += defender.AppendSpDef(-1, attacker, this);
                        break;
                    case 331 or 390:
                        msg += defender.AppendSpeed(-1, attacker, this);
                        break;
                    case 24:
                        msg += defender.AppendAccuracy(-1, attacker, this);
                        break;
                    case 25 or 259:
                        msg += defender.AppendEvasion(-1, attacker, this);
                        break;
                    case 69 or 396 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendAttack(-1, attacker, this);

                        break;
                    }
                    case 70 or 397 or 435 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.AppendDefense(-1, attacker, this);

                        break;
                    }
                    case 475:
                    {
                        // This one has two different chance percents, one has to be hardcoded
                        if (new Random().Next(1, 101) <= 50) msg += defender.AppendDefense(-1, attacker, this);

                        break;
                    }
                    case 21 or 71 or 357 or 477 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpeed(-1, attacker, this);

                        break;
                    }
                    case 72 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpAtk(-1, attacker, this);

                        break;
                    }
                    case 73 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-1, attacker, this);

                        break;
                    }
                    case 74 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.AppendAccuracy(-1, attacker, this);

                        break;
                    }
                    case 183:
                        msg += attacker.AppendAttack(-1, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 183 or 230 or 309 or 335 or 405 or 438 or 442:
                        msg += attacker.AppendDefense(-1, attacker, this);
                        break;
                    case 480:
                        msg += attacker.AppendSpAtk(-1, attacker, this);
                        break;
                }

                if (Effect is 230 or 309 or 335) msg += attacker.AppendSpDef(-1, attacker, this);

                switch (Effect)
                {
                    case 219 or 335:
                        msg += attacker.AppendSpeed(-1, attacker, this);
                        break;
                    // -2
                    case 59 or 169:
                        msg += defender.AppendAttack(-2, attacker, this);
                        break;
                    case 60 or 483:
                        msg += defender.AppendDefense(-2, attacker, this);
                        break;
                    case 61:
                        msg += defender.AppendSpeed(-2, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 62 or 169 or 266:
                        msg += defender.AppendSpAtk(-2, attacker, this);
                        break;
                    case 63:
                        msg += defender.AppendSpDef(-2, attacker, this);
                        break;
                    case 272 or 297 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-2, attacker, this);

                        break;
                    }
                    case 205:
                        msg += attacker.AppendSpAtk(-2, attacker, this);
                        break;
                    case 479:
                        msg += attacker.AppendSpeed(-2, attacker, this);
                        break;
                    // other
                    case 26:
                        attacker.AttackStage = 0;
                        attacker.DefenseStage = 0;
                        attacker.SpAtkStage = 0;
                        attacker.SpDefStage = 0;
                        attacker.SpeedStage = 0;
                        attacker.AccuracyStage = 0;
                        attacker.EvasionStage = 0;
                        defender.AttackStage = 0;
                        defender.DefenseStage = 0;
                        defender.SpAtkStage = 0;
                        defender.SpDefStage = 0;
                        defender.SpeedStage = 0;
                        defender.AccuracyStage = 0;
                        defender.EvasionStage = 0;
                        msg += "All pokemon had their stat stages reset!\n";
                        break;
                    case 305:
                        defender.AttackStage = 0;
                        defender.DefenseStage = 0;
                        defender.SpAtkStage = 0;
                        defender.SpDefStage = 0;
                        defender.SpeedStage = 0;
                        defender.AccuracyStage = 0;
                        defender.EvasionStage = 0;
                        msg += $"{defender.Name} had their stat stages reset!\n";
                        break;
                    case 141 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                        {
                            msg += attacker.AppendAttack(1, attacker, this);
                            msg += attacker.AppendDefense(1, attacker, this);
                            msg += attacker.AppendSpAtk(1, attacker, this);
                            msg += attacker.AppendSpDef(1, attacker, this);
                            msg += attacker.AppendSpeed(1, attacker, this);
                        }

                        break;
                    }
                    case 143:
                        msg += attacker.Damage(attacker.StartingHp / 2, battle);
                        msg += attacker.AppendAttack(12, attacker, this);
                        break;
                    case 317:
                    {
                        var amount = 1;
                        if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get())) amount = 2;

                        msg += attacker.AppendAttack(amount, attacker, this);
                        msg += attacker.AppendSpAtk(amount, attacker, this);
                        break;
                    }
                    case 364 when defender.NonVolatileEffect.Poison():
                        msg += defender.AppendAttack(-1, attacker, this);
                        msg += defender.AppendSpAtk(-1, attacker, this);
                        msg += defender.AppendSpeed(-1, attacker, this);
                        break;
                    case 329:
                        msg += attacker.AppendDefense(3, attacker, this);
                        break;
                    case 322:
                        msg += attacker.AppendSpAtk(3, attacker, this);
                        break;
                    case 227:
                    {
                        var validStats = new List<Func<int, DuelPokemon, Move, string, bool, string>>();

                        if (attacker.AttackStage < 6)
                            validStats.Add(attacker.AppendAttack);
                        if (attacker.DefenseStage < 6)
                            validStats.Add(attacker.AppendDefense);
                        if (attacker.SpAtkStage < 6)
                            validStats.Add(attacker.AppendSpAtk);
                        if (attacker.SpDefStage < 6)
                            validStats.Add(attacker.AppendSpDef);
                        if (attacker.SpeedStage < 6)
                            validStats.Add(attacker.AppendSpeed);
                        if (attacker.EvasionStage < 6)
                            validStats.Add(attacker.AppendEvasion);
                        if (attacker.AccuracyStage < 6)
                            validStats.Add(attacker.AppendAccuracy);

                        if (validStats.Count > 0)
                        {
                            var statRaiseFunc =
                                validStats[new Random().Next(validStats.Count)];
                            msg += statRaiseFunc(2, attacker, this, "", false);
                        }
                        else
                        {
                            msg += $"None of {attacker.Name}'s stats can go any higher!\n";
                        }

                        break;
                    }
                    case 473:
                    {
                        var rawAtk = attacker.GetRawAttack() + attacker.GetRawSpAtk();
                        var rawDef = attacker.GetRawDefense() + attacker.GetRawSpDef();
                        if (rawAtk > rawDef)
                        {
                            msg += attacker.AppendAttack(1, attacker, this);
                            msg += attacker.AppendSpAtk(1, attacker, this);
                        }
                        else
                        {
                            msg += attacker.AppendDefense(1, attacker, this);
                            msg += attacker.AppendSpDef(1, attacker, this);
                        }

                        break;
                    }
                    case 485:
                        msg += attacker.Damage(attacker.StartingHp / 2, battle);
                        msg += attacker.AppendAttack(2, attacker, this);
                        msg += attacker.AppendSpAtk(2, attacker, this);
                        msg += attacker.AppendSpeed(2, attacker, this);
                        break;
                }

                // Flinch
                if (!defender.HasMoved)
                    for (var hit = 0; hit < numHits1; hit++)
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

                switch (Effect)
                {
                    // Move locking
                    case 87:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects its move from being disabled!\n";
                        }
                        else
                        {
                            defender.Disable.Set(defender.LastMove, new Random().Next(4, 8));
                            msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was disabled!\n";
                        }

                        break;
                    }
                    case 176:
                    {
                        if (defender.Ability(attacker, this) == Ability.OBLIVIOUS)
                        {
                            msg += $"{defender.Name} is too oblivious to be taunted!\n";
                        }
                        else if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being taunted!\n";
                        }
                        else
                        {
                            if (defender.HasMoved)
                                defender.Taunt.SetTurns(4);
                            else
                                defender.Taunt.SetTurns(3);

                            msg += $"{defender.Name} is being taunted!\n";
                        }

                        break;
                    }
                    case 91:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being encored!\n";
                        }
                        else
                        {
                            defender.Encore.Set(defender.LastMove, 4);
                            if (!defender.HasMoved)
                                defender.Owner.SelectedAction = new Trainer.MoveAction(defender.LastMove);

                            msg += $"{defender.Name} is giving an encore!\n";
                        }

                        break;
                    }
                    case 166:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being tormented!\n";
                        }
                        else
                        {
                            defender.Torment = true;
                            msg += $"{defender.Name} is tormented!\n";
                        }

                        break;
                    }
                    case 193:
                        attacker.Imprison = true;
                        msg += $"{attacker.Name} imprisons!\n";
                        break;
                    case 237:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                        }
                        else
                        {
                            defender.HealBlock.SetTurns(5);
                            msg += $"{defender.Name} is blocked from healing!\n";
                        }

                        break;
                    }
                    case 496:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                        }
                        else
                        {
                            defender.HealBlock.SetTurns(2);
                            msg += $"{defender.Name} is blocked from healing!\n";
                        }

                        break;
                    }
                    // Weather changing
                    case 116:
                        msg += battle.Weather.Set("sandstorm", attacker);
                        break;
                    case 137:
                        msg += battle.Weather.Set("rain", attacker);
                        break;
                    case 138:
                        msg += battle.Weather.Set("sun", attacker);
                        break;
                    case 165:
                        msg += battle.Weather.Set("hail", attacker);
                        break;
                    // Terrain changing
                    case 352:
                        msg += battle.Terrain.Set("grassy", attacker);
                        break;
                    case 353:
                        msg += battle.Terrain.Set("misty", attacker);
                        break;
                    case 369:
                        msg += battle.Terrain.Set("electric", attacker);
                        break;
                    case 395:
                        msg += battle.Terrain.Set("psychic", attacker);
                        break;
                }

                // Protection
                if (new[] { 112, 117, 279, 356, 362, 384, 454, 488, 499 }.Contains(Effect))
                {
                    attacker.ProtectionUsed = true;
                    attacker.ProtectionChance *= 3;
                }

                switch (Effect)
                {
                    case 112:
                        attacker.Protect = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                    case 117:
                        attacker.Endure = true;
                        msg += $"{attacker.Name} braced itself!\n";
                        break;
                    case 279:
                        attacker.WideGuard = true;
                        msg += $"Wide guard protects {attacker.Name}!\n";
                        break;
                    case 350:
                        attacker.CraftyShield = true;
                        msg += $"A crafty shield protects {attacker.Name} from status moves!\n";
                        break;
                    case 356:
                        attacker.KingShield = true;
                        msg += $"{attacker.Name} shields itself!\n";
                        break;
                    case 362:
                        attacker.SpikyShield = true;
                        msg += $"{attacker.Name} shields itself!\n";
                        break;
                    case 377:
                        attacker.MatBlock = true;
                        msg += $"{attacker.Name} shields itself!\n";
                        break;
                    case 384:
                        attacker.BanefulBunker = true;
                        msg += $"{attacker.Name} bunkers down!\n";
                        break;
                    case 307:
                        attacker.QuickGuard = true;
                        msg += $"{attacker.Name} guards itself!\n";
                        break;
                    case 454:
                        attacker.Obstruct = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                    case 488:
                        attacker.SilkTrap = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                    case 499:
                        attacker.BurningBulwark = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                }

                // Life orb
                if (
                    attacker.HeldItem == "life-orb" &&
                    defender.Owner.HasAlivePokemon() &&
                    DamageClass != DamageClass.STATUS &&
                    (attacker.Ability() != Ability.SHEER_FORCE || EffectChance == null) &&
                    Effect != 149
                )
                    msg += attacker.Damage(attacker.StartingHp / 10, battle, source: "its life orb");

                // Swap outs
                // A poke is force-swapped out before activating red-card
                if (Effect is 29 or 314)
                {
                    var swaps = defender.Owner.ValidSwaps(attacker, battle, false);
                    if (swaps.Count == 0)
                    {
                        // Do nothing
                    }
                    else if (defender.Ability(attacker, this) == Ability.SUCTION_CUPS)
                    {
                        msg += $"{defender.Name}'s suction cups kept it in place!\n";
                    }
                    else if (defender.Ability(attacker, this) == Ability.GUARD_DOG)
                    {
                        msg += $"{defender.Name}'s guard dog kept it in place!\n";
                    }
                    else if (defender.Ingrain)
                    {
                        msg += $"{defender.Name} is ingrained in the ground!\n";
                    }
                    else
                    {
                        msg += $"{defender.Name} fled in fear!\n";
                        msg += defender.Remove(battle);
                        var idx = swaps[new Random().Next(swaps.Count)];
                        defender.Owner.SwitchPoke(idx, true);
                        msg += defender.Owner.CurrentPokemon.SendOut(attacker, battle);
                        // Safety in case the poke dies on send out.
                        if (defender.Owner.CurrentPokemon != null) defender.Owner.CurrentPokemon.HasMoved = true;
                    }
                }
                // A red-card forces the attacker to swap to a random poke, even if they used a switch out move
                else if (defender.HeldItem == "red-card" && defender.Hp > 0 && DamageClass != DamageClass.STATUS)
                {
                    var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
                    if (swaps.Count == 0)
                    {
                        // Do nothing
                    }
                    else if (attacker.Ability(defender, this) == Ability.SUCTION_CUPS)
                    {
                        msg += $"{attacker.Name}'s suction cups kept it in place from {defender.Name}'s red card!\n";
                        defender.HeldItem.Use();
                    }
                    else if (attacker.Ability(defender, this) == Ability.GUARD_DOG)
                    {
                        msg += $"{attacker.Name}'s guard dog kept it in place from {defender.Name}'s red card!\n";
                        defender.HeldItem.Use();
                    }
                    else if (attacker.Ingrain)
                    {
                        msg += $"{attacker.Name} is ingrained in the ground from {defender.Name}'s red card!\n";
                        defender.HeldItem.Use();
                    }
                    else
                    {
                        msg += $"{defender.Name} held up its red card against {attacker.Name}!\n";
                        defender.HeldItem.Use();
                        msg += attacker.Remove(battle);
                        var idx = swaps[new Random().Next(swaps.Count)];
                        attacker.Owner.SwitchPoke(idx, true);
                        msg += attacker.Owner.CurrentPokemon.SendOut(defender, battle);
                        // Safety in case the poke dies on send out.
                        if (attacker.Owner.CurrentPokemon != null) attacker.Owner.CurrentPokemon.HasMoved = true;
                    }
                }
                else if (new[] { 128, 154, 229, 347 }.Contains(Effect))
                {
                    var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
                    if (swaps.Count > 0)
                    {
                        msg += $"{attacker.Name} went back!\n";
                        if (Effect == 128) attacker.Owner.BatonPass = new BatonPass(attacker);

                        msg += attacker.Remove(battle);
                        // Force this pokemon to immediately return to be attacked
                        attacker.Owner.MidTurnRemove = true;
                    }
                }

                // Trapping
                if (new[] { 107, 374, 385, 449, 452 }.Contains(Effect) && !defender.Trapping)
                {
                    defender.Trapping = true;
                    msg += $"{defender.Name} can't escape!\n";
                }

                if (Effect == 449 && !attacker.Trapping)
                {
                    attacker.Trapping = true;
                    msg += $"{attacker.Name} can't escape!\n";
                }

                // Attacker faints
                if (new[] { 169, 221, 271, 321 }.Contains(Effect)) msg += attacker.Faint(battle);

                switch (Effect)
                {
                    // Struggle
                    case 255:
                        msg += attacker.Damage(attacker.StartingHp / 4, battle, attacker: attacker);
                        break;
                    // Pain Split
                    case 92:
                    {
                        var hp = (attacker.Hp + defender.Hp) / 2;
                        attacker.Hp = Math.Min(attacker.StartingHp, hp);
                        defender.Hp = Math.Min(defender.StartingHp, hp);
                        msg += "The battlers share their pain!\n";
                        break;
                    }
                    // Spite
                    case 101:
                        defender.LastMove.PP = Math.Max(0, defender.LastMove.PP - 4);
                        msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was reduced!\n";
                        break;
                    // Eerie Spell
                    case 439 when defender.LastMove != null:
                        defender.LastMove.PP = Math.Max(0, defender.LastMove.PP - 3);
                        msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was reduced!\n";
                        break;
                    // Heal Bell
                    case 103:
                    {
                        foreach (var poke in attacker.Owner.Party) poke.NonVolatileEffect.Reset();

                        msg +=
                            $"A bell chimed, and all of {attacker.Owner.Name}'s pokemon had status conditions removed!\n";
                        break;
                    }
                    // Psycho Shift
                    case 235:
                    {
                        var transferedStatus = attacker.NonVolatileEffect.Current;
                        msg += defender.NonVolatileEffect.ApplyStatus(transferedStatus, battle, attacker,
                            this);
                        if (defender.NonVolatileEffect.Current == transferedStatus)
                        {
                            attacker.NonVolatileEffect.Reset();
                            msg += $"{attacker.Name}'s {transferedStatus} was transfered to {defender.Name}!\n";
                        }
                        else
                        {
                            msg += "But it failed!\n";
                        }

                        break;
                    }
                    // Defog
                    case 259:
                        defender.Owner.Spikes = 0;
                        defender.Owner.ToxicSpikes = 0;
                        defender.Owner.StealthRock = false;
                        defender.Owner.StickyWeb = false;
                        defender.Owner.AuroraVeil = new ExpiringEffect(0);
                        defender.Owner.LightScreen = new ExpiringEffect(0);
                        defender.Owner.Reflect = new ExpiringEffect(0);
                        defender.Owner.Mist = new ExpiringEffect(0);
                        defender.Owner.Safeguard = new ExpiringEffect(0);
                        attacker.Owner.Spikes = 0;
                        attacker.Owner.ToxicSpikes = 0;
                        attacker.Owner.StealthRock = false;
                        attacker.Owner.StickyWeb = false;
                        battle.Terrain.End();
                        msg += $"{attacker.Name} blew away the fog!\n";
                        break;
                    // Trick room
                    case 260:
                    {
                        if (battle.TrickRoom.Active())
                        {
                            battle.TrickRoom.SetTurns(0);
                            msg += "The Dimensions returned back to normal!\n";
                        }
                        else
                        {
                            battle.TrickRoom.SetTurns(5);
                            msg += $"{attacker.Name} twisted the dimensions!\n";
                        }

                        break;
                    }
                    // Magic Room
                    case 287:
                    {
                        if (battle.MagicRoom.Active())
                        {
                            battle.MagicRoom.SetTurns(0);
                            msg += "The room returns to normal, and held items regain their effect!\n";
                        }
                        else
                        {
                            battle.MagicRoom.SetTurns(5);
                            msg += "A bizzare area was created, and pokemon's held items lost their effect!\n";
                        }

                        break;
                    }
                    // Wonder Room
                    case 282:
                    {
                        if (battle.WonderRoom.Active())
                        {
                            battle.WonderRoom.SetTurns(0);
                            msg += "The room returns to normal, and stats swap back to what they were before!\n";
                        }
                        else
                        {
                            battle.WonderRoom.SetTurns(5);
                            msg +=
                                "A bizzare area was created, and pokemon's defense and special defense were swapped!\n";
                        }

                        break;
                    }
                    // Perish Song
                    case 115:
                    {
                        msg += "All pokemon hearing the song will faint after 3 turns!\n";
                        if (attacker.PerishSong.Active())
                            msg += $"{attacker.Name} is already under the effect of perish song!\n";
                        else
                            attacker.PerishSong.SetTurns(4);

                        if (defender.PerishSong.Active())
                            msg += $"{defender.Name} is already under the effect of perish song!\n";
                        else if (defender.Ability(attacker, this) == Ability.SOUNDPROOF)
                            msg += $"{defender.Name}'s soundproof protects it from hearing the song!\n";
                        else
                            defender.PerishSong.SetTurns(4);

                        break;
                    }
                    // Nightmare
                    case 108:
                        defender.Nightmare = true;
                        msg += $"{defender.Name} fell into a nightmare!\n";
                        break;
                    // Gravity
                    case 216:
                    {
                        battle.Gravity.SetTurns(5);
                        msg += "Gravity intensified!\n";
                        defender.Telekinesis.SetTurns(0);
                        if (defender.Fly)
                        {
                            defender.Fly = false;
                            defender.LockedMove = null;
                            msg += $"{defender.Name} fell from the sky!\n";
                        }

                        break;
                    }
                    // Spikes
                    case 113:
                        defender.Owner.Spikes += 1;
                        msg += $"Spikes were scattered around the feet of {defender.Owner.Name}'s team!\n";
                        break;
                    // Toxic Spikes
                    case 250:
                        defender.Owner.ToxicSpikes += 1;
                        msg += $"Toxic spikes were scattered around the feet of {defender.Owner.Name}'s team!\n";
                        break;
                    // Stealth Rock
                    case 267:
                        defender.Owner.StealthRock = true;
                        msg += $"Pointed stones float in the air around {defender.Owner.Name}'s team!\n";
                        break;
                    // Sticky Web
                    case 341:
                        defender.Owner.StickyWeb = true;
                        msg += $"A sticky web is shot around the feet of {defender.Owner.Name}'s team!\n";
                        break;
                    // Defense curl
                    case 157 when !attacker.DefenseCurl:
                        attacker.DefenseCurl = true;
                        break;
                    // Psych Up
                    case 144:
                        attacker.AttackStage = defender.AttackStage;
                        attacker.DefenseStage = defender.DefenseStage;
                        attacker.SpAtkStage = defender.SpAtkStage;
                        attacker.SpDefStage = defender.SpDefStage;
                        attacker.SpeedStage = defender.SpeedStage;
                        attacker.AccuracyStage = defender.AccuracyStage;
                        attacker.EvasionStage = defender.EvasionStage;
                        attacker.FocusEnergy = defender.FocusEnergy;
                        msg += "It psyched itself up!\n";
                        break;
                    // Conversion
                    case 31:
                    {
                        var t = attacker.Moves[0].Type;
                        if (!Enum.IsDefined(typeof(ElementType), t)) t = ElementType.NORMAL;

                        attacker.TypeIds = new List<ElementType> { t };
                        var typeName = t.ToString().ToLower();
                        msg += $"{attacker.Name} transformed into a {typeName} type!\n";
                        break;
                    }
                    // Conversion 2
                    case 94:
                    {
                        var t = GetConversion2(attacker, defender, battle);
                        if (t.HasValue)
                        {
                            attacker.TypeIds = new List<ElementType> { t.Value };
                            var typeName = t.Value.ToString().ToLower();
                            msg += $"{attacker.Name} transformed into a {typeName} type!\n";
                        }

                        break;
                    }
                    // Burn up
                    case 398:
                        attacker.TypeIds.Remove(ElementType.FIRE);
                        msg += $"{attacker.Name} lost its fire type!\n";
                        break;
                    // Double shock
                    case 481:
                        attacker.TypeIds.Remove(ElementType.ELECTRIC);
                        msg += $"{attacker.Name} lost its electric type!\n";
                        break;
                    // Forest's Curse
                    case 376:
                        defender.TypeIds.Add(ElementType.GRASS);
                        msg += $"{defender.Name} added grass type!\n";
                        break;
                    // Trick or Treat
                    case 343:
                        defender.TypeIds.Add(ElementType.GHOST);
                        msg += $"{defender.Name} added ghost type!\n";
                        break;
                    // Soak
                    case 295:
                        defender.TypeIds = new List<ElementType> { ElementType.WATER };
                        msg += $"{defender.Name} was transformed into a water type!\n";
                        break;
                    // Magic Powder
                    case 456:
                        defender.TypeIds = new List<ElementType> { ElementType.PSYCHIC };
                        msg += $"{defender.Name} was transformed into a psychic type!\n";
                        break;
                    // Camouflage
                    case 214:
                    {
                        switch (battle.Terrain.Item?.ToString())
                        {
                            case "grassy":
                                attacker.TypeIds = new List<ElementType> { ElementType.GRASS };
                                msg += $"{attacker.Name} was transformed into a grass type!\n";
                                break;
                            case "misty":
                                attacker.TypeIds = new List<ElementType> { ElementType.FAIRY };
                                msg += $"{attacker.Name} was transformed into a fairy type!\n";
                                break;
                            case "electric":
                                attacker.TypeIds = new List<ElementType> { ElementType.ELECTRIC };
                                msg += $"{attacker.Name} was transformed into an electric type!\n";
                                break;
                            case "psychic":
                                attacker.TypeIds = new List<ElementType> { ElementType.PSYCHIC };
                                msg += $"{attacker.Name} was transformed into a psychic type!\n";
                                break;
                            default:
                                attacker.TypeIds = new List<ElementType> { ElementType.NORMAL };
                                msg += $"{attacker.Name} was transformed into a normal type!\n";
                                break;
                        }

                        break;
                    }
                    // Role Play
                    case 179:
                    {
                        attacker.AbilityId = defender.AbilityId;
                        var abilityName = ((Ability)attacker.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{attacker.Name} acquired {abilityName}!\n";
                        msg += attacker.SendOutAbility(defender, battle);
                        break;
                    }
                    // Simple Beam
                    case 299:
                        defender.AbilityId = (int)Ability.SIMPLE;
                        msg += $"{defender.Name} acquired simple!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        break;
                    // Entrainment
                    case 300:
                    {
                        defender.AbilityId = attacker.AbilityId;
                        var abilityName = ((Ability)defender.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{defender.Name} acquired {abilityName}!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        break;
                    }
                    // Worry Seed
                    case 248:
                    {
                        defender.AbilityId = (int)Ability.INSOMNIA;
                        if (defender.NonVolatileEffect.Sleep()) defender.NonVolatileEffect.Reset();

                        msg += $"{defender.Name} acquired insomnia!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        break;
                    }
                    // Skill Swap
                    case 192:
                    {
                        (defender.AbilityId, attacker.AbilityId) = (attacker.AbilityId, defender.AbilityId);
                        var defenderAbilityName = ((Ability)defender.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{defender.Name} acquired {defenderAbilityName}!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        var attackerAbilityName = ((Ability)attacker.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{attacker.Name} acquired {attackerAbilityName}!\n";
                        msg += attacker.SendOutAbility(defender, battle);
                        break;
                    }
                    // Aurora Veil
                    case 407:
                    {
                        if (attacker.HeldItem == "light-clay")
                            attacker.Owner.AuroraVeil.SetTurns(8);
                        else
                            attacker.Owner.AuroraVeil.SetTurns(5);

                        msg += $"{attacker.Name} put up its aurora veil!\n";
                        break;
                    }
                    // Light Screen
                    case 36 or 421:
                    {
                        if (attacker.HeldItem == "light-clay")
                            attacker.Owner.LightScreen.SetTurns(8);
                        else
                            attacker.Owner.LightScreen.SetTurns(5);

                        msg += $"{attacker.Name} put up its light screen!\n";
                        break;
                    }
                    // Reflect
                    case 66 or 422:
                    {
                        if (attacker.HeldItem == "light-clay")
                            attacker.Owner.Reflect.SetTurns(8);
                        else
                            attacker.Owner.Reflect.SetTurns(5);

                        msg += $"{attacker.Name} put up its reflect!\n";
                        break;
                    }
                    // Mist
                    case 47:
                        attacker.Owner.Mist.SetTurns(5);
                        msg += $"{attacker.Name} gained the protection of mist!\n";
                        break;
                    // Bind
                    case 43:
                    case 262 when defender.Substitute == 0 && !defender.Bind.Active():
                    {
                        if (attacker.HeldItem == "grip-claw")
                            defender.Bind.SetTurns(7);
                        else
                            defender.Bind.SetTurns(new Random().Next(4, 6));

                        msg += $"{defender.Name} was squeezed!\n";
                        break;
                    }
                    // Sketch
                    case 96:
                    {
                        var m = defender.LastMove.Copy();
                        attacker.Moves[attacker.Moves.IndexOf(this)] = m;
                        msg += $"The move {m.PrettyName} was sketched!\n";
                        break;
                    }
                    // Transform
                    case 58:
                        msg += $"{attacker.Name} transformed into {defender.Name}!\n";
                        attacker.Transform(defender);
                        break;
                    // Substitute
                    case 80:
                    {
                        var hp = attacker.StartingHp / 4;
                        msg += attacker.Damage(hp, battle, attacker: attacker, source: "building a substitute");
                        attacker.Substitute = hp;
                        attacker.Bind = new ExpiringEffect(0);
                        msg += $"{attacker.Name} made a substitute!\n";
                        break;
                    }
                    // Shed Tail
                    case 493:
                    {
                        var hp = attacker.StartingHp / 4;
                        msg += attacker.Damage(attacker.StartingHp / 2, battle, attacker: attacker,
                            source: "building a substitute");
                        attacker.Owner.NextSubstitute = hp;
                        attacker.Bind = new ExpiringEffect(0);
                        msg += $"{attacker.Name} left behind a substitute!\n";
                        msg += attacker.Remove(battle);
                        // Force this pokemon to immediately return to be attacked
                        attacker.Owner.MidTurnRemove = true;
                        break;
                    }
                    // Throat Chop
                    case 393 when !defender.Silenced.Active():
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                        {
                            defender.Silenced.SetTurns(3);
                            msg += $"{defender.Name} was silenced!\n";
                        }

                        break;
                    }
                    // Speed Swap
                    case 399:
                    {
                        (attacker.Speed, defender.Speed) = (defender.Speed, attacker.Speed);
                        msg += "Both pokemon exchange speed!\n";
                        break;
                    }
                    // Mimic
                    case 83:
                    {
                        var m = defender.LastMove.Copy();
                        m.PP = m.StartingPP;
                        attacker.Moves[attacker.Moves.IndexOf(this)] = m;
                        msg += $"{attacker.Name} mimicked {m.PrettyName}!\n";
                        break;
                    }
                    // Rage
                    case 82:
                        attacker.Rage = true;
                        msg += $"{attacker.Name}'s rage is building!\n";
                        break;
                    // Mind Reader
                    case 95:
                        defender.MindReader.Set(attacker, 2);
                        msg += $"{attacker.Name} took aim at {defender.Name}!\n";
                        break;
                    // Destiny Bond
                    case 99:
                        attacker.DestinyBond = true;
                        attacker.DestinyBondCooldown.SetTurns(2);
                        msg += $"{attacker.Name} is trying to take its foe with it!\n";
                        break;
                    // Ingrain
                    case 182:
                        attacker.Ingrain = true;
                        msg += $"{attacker.Name} planted its roots!\n";
                        break;
                    // Attract
                    case 121:
                        msg += defender.Infatuate(attacker, this);
                        break;
                    // Heart Swap
                    case 251:
                    {
                        (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                        (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                        (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                        (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                        (attacker.SpeedStage, defender.SpeedStage) = (defender.SpeedStage, attacker.SpeedStage);

                        (attacker.AccuracyStage, defender.AccuracyStage) =
                            (defender.AccuracyStage, attacker.AccuracyStage);

                        (attacker.EvasionStage, defender.EvasionStage) = (defender.EvasionStage, attacker.EvasionStage);

                        msg += $"{attacker.Name} switched stat changes with {defender.Name}!\n";
                        break;
                    }
                    // Power Swap
                    case 244:
                    {
                        (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                        (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                        msg +=
                            $"{attacker.Name} switched attack and special attack stat changes with {defender.Name}!\n";
                        break;
                    }
                    // Guard Swap
                    case 245:
                    {
                        (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                        (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                        msg +=
                            $"{attacker.Name} switched defense and special defense stat changes with {defender.Name}!\n";
                        break;
                    }
                    // Aqua Ring
                    case 252:
                        attacker.AquaRing = true;
                        msg += $"{attacker.Name} surrounded itself with a veil of water!\n";
                        break;
                    // Magnet Rise
                    case 253:
                        attacker.MagnetRise.SetTurns(5);
                        msg += $"{attacker.Name} levitated with electromagnetism!\n";
                        break;
                    // Healing Wish
                    case 221:
                        attacker.Owner.HealingWish = true;
                        msg += $"{attacker.Name}'s replacement will be restored!\n";
                        break;
                    // Lunar Dance
                    case 271:
                        attacker.Owner.LunarDance = true;
                        msg += $"{attacker.Name}'s replacement will be restored!\n";
                        break;
                    // Gastro Acid
                    case 240:
                        defender.AbilityId = 0;
                        msg += $"{defender.Name}'s ability was disabled!\n";
                        break;
                    // Lucky Chant
                    case 241:
                        attacker.LuckyChant.SetTurns(5);
                        msg += $"{attacker.Name} is shielded from critical hits!\n";
                        break;
                    // Safeguard
                    case 125:
                        attacker.Owner.Safeguard.SetTurns(5);
                        msg += $"{attacker.Name} is protected from status effects!\n";
                        break;
                    // Guard Split
                    case 280:
                        attacker.DefenseSplit = defender.GetRawDefense();
                        attacker.SpDefSplit = defender.GetRawSpDef();
                        defender.DefenseSplit = attacker.GetRawDefense();
                        defender.SpDefSplit = attacker.GetRawSpDef();
                        msg += $"{attacker.Name} and {defender.Name} shared their guard!\n";
                        break;
                    // Power Split
                    case 281:
                        attacker.AttackSplit = defender.GetRawAttack();
                        attacker.SpAtkSplit = defender.GetRawSpAtk();
                        defender.AttackSplit = attacker.GetRawAttack();
                        defender.SpAtkSplit = attacker.GetRawSpAtk();
                        msg += $"{attacker.Name} and {defender.Name} shared their power!\n";
                        break;
                    // Smack Down/Thousand Arrows
                    case 288 or 373:
                    {
                        defender.Telekinesis.SetTurns(0);
                        if (defender.Fly)
                        {
                            defender.Fly = false;
                            defender.LockedMove = null;
                            defender.HasMoved = true;
                            msg += $"{defender.Name} was shot out of the air!\n";
                        }

                        if (!defender.Grounded(battle, attacker, this))
                        {
                            defender.GroundedByMove = true;
                            msg += $"{defender.Name} was grounded!\n";
                        }

                        break;
                    }
                    // Reflect Type
                    case 319:
                        attacker.TypeIds = new List<ElementType>(defender.TypeIds);
                        msg += $"{attacker.Name}'s type changed to match {defender.Name}!\n";
                        break;
                    // Charge
                    case 175:
                        // TODO: Gen 9 makes charge last until an electric move is used
                        attacker.Charge.SetTurns(2);
                        msg += $"{attacker.Name} charges up electric type moves!\n";
                        break;
                    // Magic Coat
                    case 184:
                        attacker.MagicCoat = true;
                        msg += $"{attacker.Name} shrouded itself with a magic coat!\n";
                        break;
                    // Tailwind
                    case 226:
                    {
                        msg += $"It was reflected by {defender.Name}'s magic bounce!\n";
                        var hm = defender.HasMoved;
                        msg += Use(defender, attacker, battle, false, bounced: true);
                        defender.HasMoved = hm;
                        return msg;
                    }
                    // Fling
                    case 234 when attacker.HeldItem.CanRemove():
                    {
                        var item = attacker.HeldItem.Name;
                        msg += $"{attacker.Name}'s {item} was flung away!\n";
                        if (attacker.HeldItem.IsBerry())
                        {
                            msg += attacker.HeldItem.EatBerry(defender, attacker, this);
                        }
                        else
                        {
                            attacker.HeldItem.Use();
                            switch (item)
                            {
                                case "flame-orb":
                                    msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker,
                                        this);
                                    break;
                                case "kings-rock" or "razor-fang":
                                    msg += defender.Flinch(attacker, this);
                                    break;
                                case "light-ball":
                                    msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker,
                                        this);
                                    break;
                                case "mental-herb":
                                    defender.Infatuated = null;
                                    defender.Taunt = new ExpiringEffect(0);
                                    defender.Encore = new ExpiringItem();
                                    defender.Torment = false;
                                    defender.Disable = new ExpiringItem();
                                    defender.HealBlock = new ExpiringEffect(0);
                                    msg += $"{defender.Name} feels refreshed!\n";
                                    break;
                                case "poison-barb":
                                    msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker,
                                        this);
                                    break;
                                case "toxic-orb":
                                    msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker,
                                        this);
                                    break;
                                case "white-herb":
                                    defender.AttackStage = Math.Max(0, defender.AttackStage);
                                    defender.DefenseStage = Math.Max(0, defender.DefenseStage);
                                    defender.SpAtkStage = Math.Max(0, defender.SpAtkStage);
                                    defender.SpDefStage = Math.Max(0, defender.SpDefStage);
                                    defender.SpeedStage = Math.Max(0, defender.SpeedStage);
                                    defender.AccuracyStage = Math.Max(0, defender.AccuracyStage);
                                    defender.EvasionStage = Math.Max(0, defender.EvasionStage);
                                    msg += $"{defender.Name} feels refreshed!\n";
                                    break;
                            }
                        }

                        break;
                    }
                    // Thief
                    case 106 when defender.HeldItem.HasItem() && defender.HeldItem.CanRemove() &&
                                  defender.Substitute == 0 && !attacker.HeldItem.HasItem():
                    {
                        if (defender.Ability(attacker, this) == Ability.STICKY_HOLD)
                        {
                            msg += $"{defender.Name}'s sticky hand kept hold of its item!\n";
                        }
                        else
                        {
                            defender.HeldItem.Transfer(attacker.HeldItem);
                            msg += $"{defender.Name}'s {attacker.HeldItem.Name} was stolen!\n";
                        }

                        break;
                    }
                    // Trick
                    case 178:
                    {
                        attacker.HeldItem.Swap(defender.HeldItem);
                        msg += $"{attacker.Name} and {defender.Name} swapped their items!\n";
                        if (attacker.HeldItem.Name != null)
                            msg += $"{attacker.Name} gained {attacker.HeldItem.Name}!\n";

                        if (defender.HeldItem.Name != null)
                            msg += $"{defender.Name} gained {defender.HeldItem.Name}!\n";

                        break;
                    }
                    // Knock off
                    case 189 when defender.HeldItem.HasItem() && defender.HeldItem.CanRemove() &&
                                  defender.Substitute == 0 && attacker.Hp > 0:
                    {
                        if (defender.Ability(attacker, this) == Ability.STICKY_HOLD)
                        {
                            msg += $"{defender.Name}'s sticky hand kept hold of its item!\n";
                        }
                        else
                        {
                            msg += $"{defender.Name} lost its {defender.HeldItem.Name}!\n";
                            defender.HeldItem.Remove();
                        }

                        break;
                    }
                    // Teatime
                    case 476:
                    {
                        var msgadd = "";
                        foreach (var poke in new[] { attacker, defender })
                            msgadd += poke.HeldItem.EatBerry(attacker: attacker, move: this);

                        msg += msgadd;
                        if (string.IsNullOrEmpty(msgadd)) msg += "But nothing happened...";

                        break;
                    }
                    // Corrosive Gas
                    case 430:
                    {
                        if (defender.Ability(attacker, this) == Ability.STICKY_HOLD)
                        {
                            msg += $"{defender.Name}'s sticky hand kept hold of its item!\n";
                        }
                        else
                        {
                            msg += $"{defender.Name}'s {defender.HeldItem.Name} was corroded!\n";
                            defender.CorrosiveGas = true;
                        }

                        break;
                    }
                    // Mud Sport
                    case 202:
                        attacker.Owner.MudSport.SetTurns(6);
                        msg += "Electricity's power was weakened!\n";
                        break;
                    // Water Sport
                    case 211:
                        attacker.Owner.WaterSport.SetTurns(6);
                        msg += "Fire's power was weakened!\n";
                        break;
                    // Power Trick
                    case 239:
                        attacker.PowerTrick = !attacker.PowerTrick;
                        msg += $"{attacker.Name} switched its Attack and Defense!\n";
                        break;
                    // Power Shift
                    case 466:
                        attacker.PowerShift = !attacker.PowerShift;
                        msg += $"{attacker.Name} switched its offensive and defensive stats!\n";
                        break;
                    // Yawn
                    case 188:
                    {
                        if (battle.Terrain.Item?.ToString() == "electric" &&
                            defender.Grounded(battle, attacker, this))
                        {
                            msg += $"{defender.Name} keeps alert from being shocked by the electric terrain!\n";
                        }
                        else
                        {
                            defender.Yawn.SetTurns(2);
                            msg += $"{defender.Name} is drowsy!\n";
                        }

                        break;
                    }
                    // Rototiller
                    case 340:
                    {
                        foreach (var p in new[] { attacker, defender })
                        {
                            if (!p.TypeIds.Contains(ElementType.GRASS)) continue;

                            if (!p.Grounded(battle)) continue;

                            if (p.Dive || p.Dig || p.Fly || p.ShadowForce) continue;

                            msg += p.AppendAttack(1, attacker, this);
                            msg += p.AppendSpAtk(1, attacker, this);
                        }

                        break;
                    }
                    // Flower Shield
                    case 351:
                    {
                        foreach (var p in new[] { attacker, defender })
                        {
                            if (!p.TypeIds.Contains(ElementType.GRASS)) continue;

                            if (!p.Grounded(battle)) continue;

                            if (p.Dive || p.Dig || p.Fly || p.ShadowForce) continue;

                            msg += p.AppendDefense(1, attacker, this);
                        }

                        break;
                    }
                    // Ion Deluge
                    case 345:
                        attacker.IonDeluge = true;
                        msg += $"{attacker.Name} charges up the air!\n";
                        break;
                    // Topsy Turvy
                    case 348:
                        defender.AttackStage = -defender.AttackStage;
                        defender.DefenseStage = -defender.DefenseStage;
                        defender.SpAtkStage = -defender.SpAtkStage;
                        defender.SpDefStage = -defender.SpDefStage;
                        defender.SpeedStage = -defender.SpeedStage;
                        defender.AccuracyStage = -defender.AccuracyStage;
                        defender.EvasionStage = -defender.EvasionStage;
                        msg += $"{defender.Name}'s stat stages were inverted!\n";
                        break;
                    // Electrify
                    case 354:
                        defender.Electrify = true;
                        msg += $"{defender.Name}'s move was charged with electricity!\n";
                        break;
                    // Instruct
                    case 403:
                    {
                        var hm = defender.HasMoved;
                        defender.HasMoved = false;
                        msg += defender.LastMove.Use(defender, attacker, battle);
                        defender.HasMoved = hm;
                        break;
                    }
                    // Core Enforcer
                    case 402 when defender.HasMoved && defender.AbilityChangeable():
                        defender.AbilityId = 0;
                        msg += $"{defender.Name}'s ability was nullified!\n";
                        break;
                    // Laser Focus
                    case 391:
                        attacker.LaserFocus.SetTurns(2);
                        msg += $"{attacker.Name} focuses!\n";
                        break;
                    // Powder
                    case 378:
                        defender.Powdered = true;
                        msg += $"{defender.Name} was coated in powder!\n";
                        break;
                    // Rapid/Mortal Spin
                    case 130 or 486:
                        attacker.Bind.SetTurns(0);
                        attacker.Trapping = false;
                        attacker.LeechSeed = false;
                        attacker.Owner.Spikes = 0;
                        attacker.Owner.ToxicSpikes = 0;
                        attacker.Owner.StealthRock = false;
                        attacker.Owner.StickyWeb = false;
                        msg += $"{attacker.Name} was released!\n";
                        break;
                    // Snatch
                    case 196:
                        attacker.Snatching = true;
                        msg += $"{attacker.Name} waits for a target to make a move!\n";
                        break;
                    // Telekinesis
                    case 286:
                        defender.Telekinesis.SetTurns(5);
                        msg += $"{defender.Name} was hurled into the air!\n";
                        break;
                    // Embargo
                    case 233:
                        defender.Embargo.SetTurns(6);
                        msg += $"{defender.Name} can't use items anymore!\n";
                        break;
                    // Echoed Voice
                    case 303:
                        attacker.EchoedVoicePower = Math.Min(attacker.EchoedVoicePower + 40, 200);
                        attacker.EchoedVoiceUsed = true;
                        msg += $"{attacker.Name}'s voice echos!\n";
                        break;
                    // Bestow
                    case 324:
                        attacker.HeldItem.Transfer(defender.HeldItem);
                        msg += $"{attacker.Name} gave its {defender.HeldItem.Name} to {defender.Name}!\n";
                        break;
                    // Curse
                    case 110:
                    {
                        if (attacker.TypeIds.Contains(ElementType.GHOST))
                        {
                            msg += attacker.Damage(attacker.StartingHp / 2, battle, source: "inflicting the curse");
                            defender.Curse = true;
                            msg += $"{defender.Name} was cursed!\n";
                        }
                        else
                        {
                            msg += attacker.AppendSpeed(-1, attacker, this);
                            msg += attacker.AppendAttack(1, attacker, this);
                            msg += attacker.AppendDefense(1, attacker, this);
                        }

                        break;
                    }
                    // Autotomize
                    case 285:
                        attacker.Autotomize += 1;
                        msg += $"{attacker.Name} became nimble!\n";
                        break;
                    // Fell Stinger
                    case 342 when defender.Hp == 0:
                        msg += attacker.AppendAttack(3, attacker, this);
                        break;
                    // Fairy Lock
                    case 355:
                        attacker.FairyLock.SetTurns(2);
                        msg += $"{attacker.Name} prevents escape next turn!\n";
                        break;
                    // Grudge
                    case 195:
                        attacker.Grudge = true;
                        msg += $"{attacker.Name} has a grudge!\n";
                        break;
                    // Foresight
                    case 114:
                        defender.Foresight = true;
                        msg += $"{attacker.Name} identified {defender.Name}!\n";
                        break;
                    // Miracle Eye
                    case 217:
                        defender.MiracleEye = true;
                        msg += $"{attacker.Name} identified {defender.Name}!\n";
                        break;
                    // Clangorous Soul
                    case 414:
                        msg += attacker.Damage(attacker.StartingHp / 3, battle);
                        break;
                    // No Retreat
                    case 427:
                        attacker.NoRetreat = true;
                        msg += $"{attacker.Name} takes its last stand!\n";
                        break;
                    // Recycle
                    case 185:
                    {
                        attacker.HeldItem.Recover(attacker.HeldItem);
                        msg += $"{attacker.Name} recovered their {attacker.HeldItem.Name}!\n";
                        if (attacker.HeldItem.ShouldEatBerry(defender))
                            msg += attacker.HeldItem.EatBerry(attacker: defender, move: this);

                        break;
                    }
                    // Court Change
                    case 431:
                    {
                        // Swap spikes
                        (attacker.Owner.Spikes, defender.Owner.Spikes) = (defender.Owner.Spikes, attacker.Owner.Spikes);

                        // Swap toxic spikes
                        (attacker.Owner.ToxicSpikes, defender.Owner.ToxicSpikes) =
                            (defender.Owner.ToxicSpikes, attacker.Owner.ToxicSpikes);

                        // Swap stealth rock
                        (attacker.Owner.StealthRock, defender.Owner.StealthRock) =
                            (defender.Owner.StealthRock, attacker.Owner.StealthRock);

                        // Swap sticky web
                        (attacker.Owner.StickyWeb, defender.Owner.StickyWeb) =
                            (defender.Owner.StickyWeb, attacker.Owner.StickyWeb);

                        // Swap aurora veil
                        (attacker.Owner.AuroraVeil, defender.Owner.AuroraVeil) =
                            (defender.Owner.AuroraVeil, attacker.Owner.AuroraVeil);

                        // Swap light screen
                        (attacker.Owner.LightScreen, defender.Owner.LightScreen) =
                            (defender.Owner.LightScreen, attacker.Owner.LightScreen);

                        // Swap reflect
                        (attacker.Owner.Reflect, defender.Owner.Reflect) =
                            (defender.Owner.Reflect, attacker.Owner.Reflect);

                        // Swap mist
                        (attacker.Owner.Mist, defender.Owner.Mist) = (defender.Owner.Mist, attacker.Owner.Mist);

                        // Swap safeguard
                        (attacker.Owner.Safeguard, defender.Owner.Safeguard) =
                            (defender.Owner.Safeguard, attacker.Owner.Safeguard);

                        // Swap tailwind
                        (attacker.Owner.Tailwind, defender.Owner.Tailwind) =
                            (defender.Owner.Tailwind, attacker.Owner.Tailwind);

                        msg += "Active battle effects swapped sides!\n";
                        break;
                    }
                    // Roost
                    case 215:
                    {
                        attacker.Roost = true;
                        if (attacker.TypeIds.Contains(ElementType.FLYING))
                            msg += $"{attacker.Name}'s flying type is suppressed!\n";

                        break;
                    }
                    // Pluck
                    case 225 when defender.Ability(attacker, this) != Ability.STICKY_HOLD:
                        msg += defender.HeldItem.EatBerry(attacker);
                        break;
                    // Focus energy
                    case 48:
                        attacker.FocusEnergy = true;
                        msg += $"{attacker.Name} focuses on its target!\n";
                        break;
                    // Natural Gift
                    case 223:
                        msg += $"{attacker.Name}'s {attacker.HeldItem.Name} was consumed!\n";
                        attacker.HeldItem.Use();
                        break;
                    // Gulp Missile
                    case 258 when attacker.Ability() == Ability.GULP_MISSILE && attacker.Name == "Cramorant":
                    {
                        if (attacker.Hp > attacker.StartingHp / 2)
                        {
                            if (attacker.Form("Cramorant-gulping")) msg += $"{attacker.Name} gulped up an arrokuda!\n";
                        }
                        else
                        {
                            if (attacker.Form("Cramorant-gorging")) msg += $"{attacker.Name} gulped up a pikachu!\n";
                        }

                        break;
                    }
                    // Steel Roller
                    case 418:
                    case 448 when battle.Terrain.Item != null:
                        battle.Terrain.End();
                        msg += "The terrain was cleared!\n";
                        break;
                    // Octolock
                    case 452:
                        defender.Octolock = true;
                        msg += $"{defender.Name} is octolocked!\n";
                        break;
                    // Stuff Cheeks
                    case 453:
                        msg += attacker.HeldItem.EatBerry();
                        break;
                    // Plasma Fists
                    case 455:
                    {
                        if (!battle.PlasmaFists)
                        {
                            battle.PlasmaFists = true;
                            msg += $"{attacker.Name} electrifies the battlefield, energizing normal type moves!\n";
                        }

                        break;
                    }
                    // Secret Power
                    case 198:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            switch (battle.Terrain.Item?.ToString())
                            {
                                case "grassy":
                                    msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker,
                                        this);
                                    break;
                                case "misty":
                                    msg += defender.AppendSpAtk(-1, attacker, this);
                                    break;
                                case "psychic":
                                    msg += defender.AppendSpeed(-1, attacker, this);
                                    break;
                                default:
                                    msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker,
                                        this);
                                    break;
                            }

                        break;
                    }
                }


                if (IsSoundBased() && attacker.HeldItem == "throat-spray")
                {
                    msg += attacker.AppendSpAtk(1, attacker, source: "its throat spray");
                    attacker.HeldItem.Use();
                }

                switch (Effect)
                {
                    // Tar Shot
                    case 477 when !defender.TarShot:
                        defender.TarShot = true;
                        msg += $"{defender.Name} is covered in sticky tar!\n";
                        break;
                    // Tidy Up
                    case 487:
                        defender.Owner.Spikes = 0;
                        defender.Owner.ToxicSpikes = 0;
                        defender.Owner.StealthRock = false;
                        defender.Owner.StickyWeb = false;
                        defender.Substitute = 0;
                        attacker.Owner.Spikes = 0;
                        attacker.Owner.ToxicSpikes = 0;
                        attacker.Owner.StealthRock = false;
                        attacker.Owner.StickyWeb = false;
                        attacker.Substitute = 0;
                        msg += $"{attacker.Name} tidied up!\n";
                        break;
                    // Syrup Bomb
                    case 503:
                        defender.SyrupBomb.SetTurns(4);
                        msg += $"{defender.Name} got covered in sticky candy syrup!\n";
                        break;
                }

                // Dancer Ability - Runs at the end of move usage
                if (defender.Ability(attacker, this) == Ability.DANCER && IsDance() && usePP)
                {
                    var hm = defender.HasMoved;
                    msg += Use(defender, attacker, battle, false);
                    defender.HasMoved = hm;
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
                var protectionResult = CheckProtect(attacker, defender, battle);
                var wasHit = protectionResult.Item1;
                var msgdelta = protectionResult.Item2;
                if (!wasHit)
                {
                    msg += $"{defender.Name} was protected against the attack!\n";
                    msg += msgdelta;
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
                    // Mirror Move/Copy Cat
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
                        string msgadd;
                        int hits;
                        (msgadd, hits) = presentMove.Attack(attacker, defender, battle);
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

                var numHits = 0;
                // Turn 1 hit moves
                if (Effect == 81 && attacker.LockedMove != null)
                {
                    if (attacker.LockedMove.Turn == 0)
                    {
                        string msgadd;
                        (msgadd, numHits) = Attack(attacker, defender, battle);
                        msg += msgadd;
                    }
                }
                // Turn 2 hit moves
                else if (new[] { 40, 76, 146, 152, 156, 256, 257, 264, 273, 332, 333, 366, 451, 502 }
                             .Contains(Effect) && attacker.LockedMove != null)
                {
                    if (attacker.LockedMove.Turn == 1)
                        if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                        {
                            string msgadd;
                            (msgadd, numHits) = Attack(attacker, defender, battle);
                            msg += msgadd;
                        }
                }
                else
                {
                    switch (Effect)
                    {
                        // Turn 3 hit moves
                        case 27:
                        {
                            if (attacker.LockedMove.Turn == 2)
                            {
                                msg += defender.Damage(attacker.Bide.Value * 2, battle, this, currentType,
                                    attacker);
                                attacker.Bide = null;
                                numHits = 1;
                            }

                            break;
                        }
                        // Counter attack moves
                        case 228:
                            msg += defender.Damage((int)(1.5 * attacker.LastMoveDamage.Item1), battle, this,
                                currentType, attacker);
                            numHits = 1;
                            break;
                        case 145:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                                attacker);
                            numHits = 1;
                            break;
                        case 90:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                                attacker);
                            numHits = 1;
                            break;
                        // Static-damage moves
                        case 41:
                            msg += defender.Damage(defender.Hp / 2, battle, this, currentType,
                                attacker);
                            numHits = 1;
                            break;
                        case 42:
                            msg += defender.Damage(40, battle, this, currentType, attacker);
                            numHits = 1;
                            break;
                        case 88:
                            msg += defender.Damage(attacker.Level, battle, this, currentType,
                                attacker);
                            numHits = 1;
                            break;
                        case 89:
                        {
                            // 0.5-1.5, increments of .1
                            var scale = new Random().Next(0, 11) / 10.0 + 0.5;
                            msg += defender.Damage((int)(attacker.Level * scale), battle, this, currentType,
                                attacker);
                            numHits = 1;
                            break;
                        }
                        case 131:
                            msg += defender.Damage(20, battle, this, currentType, attacker);
                            numHits = 1;
                            break;
                        case 190:
                            msg += defender.Damage(Math.Max(0, defender.Hp - attacker.Hp), battle, this,
                                currentType, attacker);
                            numHits = 1;
                            break;
                        case 39:
                            msg += defender.Damage(defender.Hp, battle, this, currentType, attacker);
                            numHits = 1;
                            break;
                        case 321:
                            msg += defender.Damage(attacker.Hp, battle, this, currentType, attacker);
                            numHits = 1;
                            break;
                        case 413:
                            msg += defender.Damage(3 * (defender.Hp / 4), battle, this, currentType,
                                attacker);
                            numHits = 1;
                            break;
                        // Beat up, a stupid move
                        case 155:
                        {
                            foreach (var poke in attacker.Owner.Party)
                            {
                                if (defender.Hp == 0) break;

                                if (poke.Hp == 0) continue;

                                if (poke == attacker)
                                {
                                    string msgadd;
                                    int nh;
                                    (msgadd, nh) = Attack(attacker, defender, battle);
                                    msg += msgadd;
                                    numHits += nh;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(poke.NonVolatileEffect.Current)) continue;

                                    var moveData = new Dictionary<string, object>
                                    {
                                        ["id"] = 251,
                                        ["identifier"] = "beat-up",
                                        ["power"] = poke.GetRawAttack() / 10 + 5,
                                        ["pp"] = 100,
                                        ["accuracy"] = 100,
                                        ["priority"] = 0,
                                        ["type_id"] = (int)ElementType.DARK,
                                        ["damage_class_id"] = (int)DamageClass.PHYSICAL,
                                        ["effect_id"] = 1,
                                        ["effect_chance"] = null,
                                        ["target_id"] = 10,
                                        ["crit_rate"] = 0,
                                        ["min_hits"] = null,
                                        ["max_hits"] = null
                                    };
                                    var fakeMove = new Move(moveData);
                                    string msgadd;
                                    int nh;
                                    (msgadd, nh) = fakeMove.Attack(attacker, defender, battle);
                                    msg += msgadd;
                                    numHits += nh;
                                }
                            }

                            break;
                        }
                        // Other damaging moves
                        default:
                        {
                            if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                            {
                                string msgadd;
                                (msgadd, numHits) = Attack(attacker, defender, battle);
                                msg += msgadd;
                            }

                            break;
                        }
                    }
                }

                // Fusion Flare/Bolt effect tracking
                battle.LastMoveEffect = Effect;

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
                    case 33 or 215:
                        msg += attacker.Heal(attacker.StartingHp / 2);
                        break;
                    case 434 or 457:
                        msg += attacker.Heal(attacker.StartingHp / 4);
                        break;
                    case 310:
                    {
                        if (attacker.Ability() == Ability.MEGA_LAUNCHER)
                            msg += defender.Heal(defender.StartingHp * 3 / 4);
                        else
                            msg += defender.Heal(defender.StartingHp / 2);

                        break;
                    }
                    case 133:
                    {
                        if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                            msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                        else if (battle.Weather.Get() == "h-wind")
                            msg += attacker.Heal(attacker.StartingHp / 2);
                        else if (!string.IsNullOrEmpty(battle.Weather.Get()))
                            msg += attacker.Heal(attacker.StartingHp / 4);
                        else
                            msg += attacker.Heal(attacker.StartingHp / 2);

                        break;
                    }
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
                    case 382:
                    {
                        if (battle.Weather.Get() == "sandstorm")
                            msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                        else
                            msg += attacker.Heal(attacker.StartingHp / 2);

                        break;
                    }
                    case 387:
                    {
                        if (battle.Terrain.Item?.ToString() == "grassy")
                            msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                        else
                            msg += attacker.Heal(attacker.StartingHp / 2);

                        break;
                    }
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
                    // Status effects
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
                    case 37 when effectChance.HasValue:
                    {
                        var statuses = new[] { "burn", "freeze", "paralysis" };
                        var status = statuses[new Random().Next(statuses.Length)];
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                        break;
                    }
                    case 464 when effectChance.HasValue:
                    {
                        var statuses = new[] { "poison", "paralysis", "sleep" };
                        var status = statuses[new Random().Next(statuses.Length)];
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker, this);

                        break;
                    }
                    case 6 or 261 or 275 or 380 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("freeze", battle, attacker, this);

                        break;
                    }
                    case 7 or 153 or 263 or 264 or 276 or 332 or 372 or 396 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker,
                                this);

                        break;
                    }
                    case 68:
                        msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker, this);
                        break;
                    case 3 or 78 or 210 or 447 or 461 when
                        effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker, this);

                        break;
                    }
                    case 67 or 390 or 486:
                        msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker, this);
                        break;
                    case 203 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker,
                                this);

                        break;
                    }
                    case 34:
                        msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker, this);
                        break;
                    case 2:
                    {
                        if (Id == 464 && attacker.Name != "Darkrai")
                            msg += $"{attacker.Name} can't use the move!\n";
                        else
                            msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this);

                        break;
                    }
                    case 330 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this);

                        break;
                    }
                    case 38:
                    {
                        msg += attacker.NonVolatileEffect.ApplyStatus("sleep", battle, attacker, this,
                            3, true);
                        if (attacker.NonVolatileEffect.Sleep())
                        {
                            msg += $"{attacker.Name}'s slumber restores its health back to full!\n";
                            attacker.Hp = attacker.StartingHp;
                        }

                        break;
                    }
                    case 50 or 119 or 167 or 200:
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

                switch (Effect)
                {
                    case 194 or 457 or 472:
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

                // Stage changes
                // +1
                if (Effect is 11 or 209 or 213 or 278 or 313 or 323 or 328 or 392 or 414 or 427 or 468 or 472 or 487)
                    msg += attacker.AppendAttack(1, attacker, this);

                if (Effect is 12 or 157 or 161 or 207 or 209 or 323 or 367 or 414 or 427 or 467 or 468 or 472)
                    msg += attacker.AppendDefense(1, attacker, this);

                if (Effect is 14 or 212 or 291 or 328 or 392 or 414 or 427 or 472)
                    msg += attacker.AppendSpAtk(1, attacker, this);

                if (Effect is 161 or 175 or 207 or 212 or 291 or 367 or 414 or 427 or 472)
                    msg += attacker.AppendSpDef(1, attacker, this);

                switch (Effect)
                {
                    case 130 or 213 or 291 or 296 or 414 or 427 or 442 or 468 or 469 or 487:
                        msg += attacker.AppendSpeed(1, attacker, this);
                        break;
                    case 17 or 467:
                        msg += attacker.AppendEvasion(1, attacker, this);
                        break;
                    case 278 or 323:
                        msg += attacker.AppendAccuracy(1, attacker, this);
                        break;
                    case 139 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(1, attacker, this);

                        break;
                    }
                    case 140 or 375 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendAttack(1, attacker, this);

                        break;
                    }
                    case 277 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpAtk(1, attacker, this);

                        break;
                    }
                    case 433 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpeed(1, attacker, this);

                        break;
                    }
                    case 167:
                        msg += defender.AppendSpAtk(1, attacker, this);
                        break;
                    // +2
                    case 51 or 309:
                        msg += attacker.AppendAttack(2, attacker, this);
                        break;
                    case 52 or 453:
                        msg += attacker.AppendDefense(2, attacker, this);
                        break;
                }

                if (Effect is 53 or 285 or 309 or 313 or 366) msg += attacker.AppendSpeed(2, attacker, this);

                if (Effect is 54 or 309 or 366) msg += attacker.AppendSpAtk(2, attacker, this);

                switch (Effect)
                {
                    case 55 or 366:
                        msg += attacker.AppendSpDef(2, attacker, this);
                        break;
                    case 109:
                        msg += attacker.AppendEvasion(2, attacker, this);
                        break;
                    case 119 or 432 or 483:
                        msg += defender.AppendAttack(2, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 432:
                        msg += defender.AppendSpAtk(2, attacker, this);
                        break;
                    case 359 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(2, attacker, this);

                        break;
                    }
                    // -1
                    case 19 or 206 or 344 or 347 or 357 or 365 or 388 or 412:
                        msg += defender.AppendAttack(-1, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 20 or 206:
                        msg += defender.AppendDefense(-1, attacker, this);
                        break;
                    case 344 or 347 or 358 or 412:
                        msg += defender.AppendSpAtk(-1, attacker, this);
                        break;
                    case 428:
                        msg += defender.AppendSpDef(-1, attacker, this);
                        break;
                    case 331 or 390:
                        msg += defender.AppendSpeed(-1, attacker, this);
                        break;
                    case 24:
                        msg += defender.AppendAccuracy(-1, attacker, this);
                        break;
                    case 25 or 259:
                        msg += defender.AppendEvasion(-1, attacker, this);
                        break;
                    case 69 or 396 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendAttack(-1, attacker, this);

                        break;
                    }
                    case 70 or 397 or 435 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.AppendDefense(-1, attacker, this);

                        break;
                    }
                    case 475:
                    {
                        // This one has two different chance percents, one has to be hardcoded
                        if (new Random().Next(1, 101) <= 50) msg += defender.AppendDefense(-1, attacker, this);

                        break;
                    }
                    case 21 or 71 or 357 or 477 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpeed(-1, attacker, this);

                        break;
                    }
                    case 72 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpAtk(-1, attacker, this);

                        break;
                    }
                    case 73 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-1, attacker, this);

                        break;
                    }
                    case 74 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                            msg += defender.AppendAccuracy(-1, attacker, this);

                        break;
                    }
                    case 183:
                        msg += attacker.AppendAttack(-1, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 183 or 230 or 309 or 335 or 405 or 438 or 442:
                        msg += attacker.AppendDefense(-1, attacker, this);
                        break;
                    case 480:
                        msg += attacker.AppendSpAtk(-1, attacker, this);
                        break;
                }

                if (Effect is 230 or 309 or 335) msg += attacker.AppendSpDef(-1, attacker, this);

                switch (Effect)
                {
                    case 219 or 335:
                        msg += attacker.AppendSpeed(-1, attacker, this);
                        break;
                    // -2
                    case 59 or 169:
                        msg += defender.AppendAttack(-2, attacker, this);
                        break;
                    case 60 or 483:
                        msg += defender.AppendDefense(-2, attacker, this);
                        break;
                    case 61:
                        msg += defender.AppendSpeed(-2, attacker, this);
                        break;
                }

                switch (Effect)
                {
                    case 62 or 169 or 266:
                        msg += defender.AppendSpAtk(-2, attacker, this);
                        break;
                    case 63:
                        msg += defender.AppendSpDef(-2, attacker, this);
                        break;
                    case 272 or 297 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-2, attacker, this);

                        break;
                    }
                    case 205:
                        msg += attacker.AppendSpAtk(-2, attacker, this);
                        break;
                    case 479:
                        msg += attacker.AppendSpeed(-2, attacker, this);
                        break;
                    // other
                    case 26:
                        attacker.AttackStage = 0;
                        attacker.DefenseStage = 0;
                        attacker.SpAtkStage = 0;
                        attacker.SpDefStage = 0;
                        attacker.SpeedStage = 0;
                        attacker.AccuracyStage = 0;
                        attacker.EvasionStage = 0;
                        defender.AttackStage = 0;
                        defender.DefenseStage = 0;
                        defender.SpAtkStage = 0;
                        defender.SpDefStage = 0;
                        defender.SpeedStage = 0;
                        defender.AccuracyStage = 0;
                        defender.EvasionStage = 0;
                        msg += "All pokemon had their stat stages reset!\n";
                        break;
                    case 305:
                        defender.AttackStage = 0;
                        defender.DefenseStage = 0;
                        defender.SpAtkStage = 0;
                        defender.SpDefStage = 0;
                        defender.SpeedStage = 0;
                        defender.AccuracyStage = 0;
                        defender.EvasionStage = 0;
                        msg += $"{defender.Name} had their stat stages reset!\n";
                        break;
                    case 141 when effectChance.HasValue:
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                        {
                            msg += attacker.AppendAttack(1, attacker, this);
                            msg += attacker.AppendDefense(1, attacker, this);
                            msg += attacker.AppendSpAtk(1, attacker, this);
                            msg += attacker.AppendSpDef(1, attacker, this);
                            msg += attacker.AppendSpeed(1, attacker, this);
                        }

                        break;
                    }
                    case 143:
                        msg += attacker.Damage(attacker.StartingHp / 2, battle);
                        msg += attacker.AppendAttack(12, attacker, this);
                        break;
                    case 317:
                    {
                        var amount = 1;
                        if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get())) amount = 2;

                        msg += attacker.AppendAttack(amount, attacker, this);
                        msg += attacker.AppendSpAtk(amount, attacker, this);
                        break;
                    }
                    case 364 when defender.NonVolatileEffect.Poison():
                        msg += defender.AppendAttack(-1, attacker, this);
                        msg += defender.AppendSpAtk(-1, attacker, this);
                        msg += defender.AppendSpeed(-1, attacker, this);
                        break;
                    case 329:
                        msg += attacker.AppendDefense(3, attacker, this);
                        break;
                    case 322:
                        msg += attacker.AppendSpAtk(3, attacker, this);
                        break;
                    case 227:
                    {
                        var validStats = new List<Func<int, DuelPokemon, Move, string, bool, string>>();

                        if (attacker.AttackStage < 6)
                            validStats.Add(attacker.AppendAttack);
                        if (attacker.DefenseStage < 6)
                            validStats.Add(attacker.AppendDefense);
                        if (attacker.SpAtkStage < 6)
                            validStats.Add(attacker.AppendSpAtk);
                        if (attacker.SpDefStage < 6)
                            validStats.Add(attacker.AppendSpDef);
                        if (attacker.SpeedStage < 6)
                            validStats.Add(attacker.AppendSpeed);
                        if (attacker.EvasionStage < 6)
                            validStats.Add(attacker.AppendEvasion);
                        if (attacker.AccuracyStage < 6)
                            validStats.Add(attacker.AppendAccuracy);

                        if (validStats.Count > 0)
                        {
                            var statRaiseFunc =
                                validStats[new Random().Next(validStats.Count)];
                            msg += statRaiseFunc(2, attacker, this, "", false);
                        }
                        else
                        {
                            msg += $"None of {attacker.Name}'s stats can go any higher!\n";
                        }

                        break;
                    }
                    case 473:
                    {
                        var rawAtk = attacker.GetRawAttack() + attacker.GetRawSpAtk();
                        var rawDef = attacker.GetRawDefense() + attacker.GetRawSpDef();
                        if (rawAtk > rawDef)
                        {
                            msg += attacker.AppendAttack(1, attacker, this);
                            msg += attacker.AppendSpAtk(1, attacker, this);
                        }
                        else
                        {
                            msg += attacker.AppendDefense(1, attacker, this);
                            msg += attacker.AppendSpDef(1, attacker, this);
                        }

                        break;
                    }
                    case 485:
                        msg += attacker.Damage(attacker.StartingHp / 2, battle);
                        msg += attacker.AppendAttack(2, attacker, this);
                        msg += attacker.AppendSpAtk(2, attacker, this);
                        msg += attacker.AppendSpeed(2, attacker, this);
                        break;
                }

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

                switch (Effect)
                {
                    // Move locking
                    case 87:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects its move from being disabled!\n";
                        }
                        else
                        {
                            defender.Disable.Set(defender.LastMove, new Random().Next(4, 8));
                            msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was disabled!\n";
                        }

                        break;
                    }
                    case 176:
                    {
                        if (defender.Ability(attacker, this) == Ability.OBLIVIOUS)
                        {
                            msg += $"{defender.Name} is too oblivious to be taunted!\n";
                        }
                        else if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being taunted!\n";
                        }
                        else
                        {
                            if (defender.HasMoved)
                                defender.Taunt.SetTurns(4);
                            else
                                defender.Taunt.SetTurns(3);

                            msg += $"{defender.Name} is being taunted!\n";
                        }

                        break;
                    }
                    case 91:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being encored!\n";
                        }
                        else
                        {
                            defender.Encore.Set(defender.LastMove, 4);
                            if (!defender.HasMoved)
                                defender.Owner.SelectedAction = new Trainer.MoveAction(defender.LastMove);

                            msg += $"{defender.Name} is giving an encore!\n";
                        }

                        break;
                    }
                    case 166:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being tormented!\n";
                        }
                        else
                        {
                            defender.Torment = true;
                            msg += $"{defender.Name} is tormented!\n";
                        }

                        break;
                    }
                    case 193:
                        attacker.Imprison = true;
                        msg += $"{attacker.Name} imprisons!\n";
                        break;
                    case 237:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                        }
                        else
                        {
                            defender.HealBlock.SetTurns(5);
                            msg += $"{defender.Name} is blocked from healing!\n";
                        }

                        break;
                    }
                    case 496:
                    {
                        if (defender.Ability(attacker, this) == Ability.AROMA_VEIL)
                        {
                            msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                        }
                        else
                        {
                            defender.HealBlock.SetTurns(2);
                            msg += $"{defender.Name} is blocked from healing!\n";
                        }

                        break;
                    }
                    // Weather changing
                    case 116:
                        msg += battle.Weather.Set("sandstorm", attacker);
                        break;
                    case 137:
                        msg += battle.Weather.Set("rain", attacker);
                        break;
                    case 138:
                        msg += battle.Weather.Set("sun", attacker);
                        break;
                    case 165:
                        msg += battle.Weather.Set("hail", attacker);
                        break;
                    // Terrain changing
                    case 352:
                        msg += battle.Terrain.Set("grassy", attacker);
                        break;
                    case 353:
                        msg += battle.Terrain.Set("misty", attacker);
                        break;
                    case 369:
                        msg += battle.Terrain.Set("electric", attacker);
                        break;
                    case 395:
                        msg += battle.Terrain.Set("psychic", attacker);
                        break;
                }

                // Protection
                if (new[] { 112, 117, 279, 356, 362, 384, 454, 488, 499 }.Contains(Effect))
                {
                    attacker.ProtectionUsed = true;
                    attacker.ProtectionChance *= 3;
                }

                switch (Effect)
                {
                    case 112:
                        attacker.Protect = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                    case 117:
                        attacker.Endure = true;
                        msg += $"{attacker.Name} braced itself!\n";
                        break;
                    case 279:
                        attacker.WideGuard = true;
                        msg += $"Wide guard protects {attacker.Name}!\n";
                        break;
                    case 350:
                        attacker.CraftyShield = true;
                        msg += $"A crafty shield protects {attacker.Name} from status moves!\n";
                        break;
                    case 356:
                        attacker.KingShield = true;
                        msg += $"{attacker.Name} shields itself!\n";
                        break;
                    case 362:
                        attacker.SpikyShield = true;
                        msg += $"{attacker.Name} shields itself!\n";
                        break;
                    case 377:
                        attacker.MatBlock = true;
                        msg += $"{attacker.Name} shields itself!\n";
                        break;
                    case 384:
                        attacker.BanefulBunker = true;
                        msg += $"{attacker.Name} bunkers down!\n";
                        break;
                    case 307:
                        attacker.QuickGuard = true;
                        msg += $"{attacker.Name} guards itself!\n";
                        break;
                    case 454:
                        attacker.Obstruct = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                    case 488:
                        attacker.SilkTrap = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                    case 499:
                        attacker.BurningBulwark = true;
                        msg += $"{attacker.Name} protected itself!\n";
                        break;
                }

                // Life orb
                if (
                    attacker.HeldItem == "life-orb" &&
                    defender.Owner.HasAlivePokemon() &&
                    DamageClass != DamageClass.STATUS &&
                    (attacker.Ability() != Ability.SHEER_FORCE || EffectChance == null) &&
                    Effect != 149
                )
                    msg += attacker.Damage(attacker.StartingHp / 10, battle, source: "its life orb");

                // Swap outs
                // A poke is force-swapped out before activating red-card
                if (Effect is 29 or 314)
                {
                    var swaps = defender.Owner.ValidSwaps(attacker, battle, false);
                    if (swaps.Count == 0)
                    {
                        // Do nothing
                    }
                    else if (defender.Ability(attacker, this) == Ability.SUCTION_CUPS)
                    {
                        msg += $"{defender.Name}'s suction cups kept it in place!\n";
                    }
                    else if (defender.Ability(attacker, this) == Ability.GUARD_DOG)
                    {
                        msg += $"{defender.Name}'s guard dog kept it in place!\n";
                    }
                    else if (defender.Ingrain)
                    {
                        msg += $"{defender.Name} is ingrained in the ground!\n";
                    }
                    else
                    {
                        msg += $"{defender.Name} fled in fear!\n";
                        msg += defender.Remove(battle);
                        var idx = swaps[new Random().Next(swaps.Count)];
                        defender.Owner.SwitchPoke(idx, true);
                        msg += defender.Owner.CurrentPokemon.SendOut(attacker, battle);
                        // Safety in case the poke dies on send out.
                        if (defender.Owner.CurrentPokemon != null) defender.Owner.CurrentPokemon.HasMoved = true;
                    }
                }
                // A red-card forces the attacker to swap to a random poke, even if they used a switch out move
                else if (defender.HeldItem == "red-card" && defender.Hp > 0 && DamageClass != DamageClass.STATUS)
                {
                    var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
                    if (swaps.Count == 0)
                    {
                        // Do nothing
                    }
                    else if (attacker.Ability(defender, this) == Ability.SUCTION_CUPS)
                    {
                        msg += $"{attacker.Name}'s suction cups kept it in place from {defender.Name}'s red card!\n";
                        defender.HeldItem.Use();
                    }
                    else if (attacker.Ability(defender, this) == Ability.GUARD_DOG)
                    {
                        msg += $"{attacker.Name}'s guard dog kept it in place from {defender.Name}'s red card!\n";
                        defender.HeldItem.Use();
                    }
                    else if (attacker.Ingrain)
                    {
                        msg += $"{attacker.Name} is ingrained in the ground from {defender.Name}'s red card!\n";
                        defender.HeldItem.Use();
                    }
                    else
                    {
                        msg += $"{defender.Name} held up its red card against {attacker.Name}!\n";
                        defender.HeldItem.Use();
                        msg += attacker.Remove(battle);
                        var idx = swaps[new Random().Next(swaps.Count)];
                        attacker.Owner.SwitchPoke(idx, true);
                        msg += attacker.Owner.CurrentPokemon.SendOut(defender, battle);
                        // Safety in case the poke dies on send out.
                        if (attacker.Owner.CurrentPokemon != null) attacker.Owner.CurrentPokemon.HasMoved = true;
                    }
                }
                else if (new[] { 128, 154, 229, 347 }.Contains(Effect))
                {
                    var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
                    if (swaps.Count > 0)
                    {
                        msg += $"{attacker.Name} went back!\n";
                        if (Effect == 128) attacker.Owner.BatonPass = new BatonPass(attacker);

                        msg += attacker.Remove(battle);
                        // Force this pokemon to immediately return to be attacked
                        attacker.Owner.MidTurnRemove = true;
                    }
                }

                // Trapping
                if (new[] { 107, 374, 385, 449, 452 }.Contains(Effect) && !defender.Trapping)
                {
                    defender.Trapping = true;
                    msg += $"{defender.Name} can't escape!\n";
                }

                if (Effect == 449 && !attacker.Trapping)
                {
                    attacker.Trapping = true;
                    msg += $"{attacker.Name} can't escape!\n";
                }

                // Attacker faints
                if (new[] { 169, 221, 271, 321 }.Contains(Effect)) msg += attacker.Faint(battle);

                switch (Effect)
                {
                    // Struggle
                    case 255:
                        msg += attacker.Damage(attacker.StartingHp / 4, battle, attacker: attacker);
                        break;
                    // Pain Split
                    case 92:
                    {
                        var hp = (attacker.Hp + defender.Hp) / 2;
                        attacker.Hp = Math.Min(attacker.StartingHp, hp);
                        defender.Hp = Math.Min(defender.StartingHp, hp);
                        msg += "The battlers share their pain!\n";
                        break;
                    }
                    // Spite
                    case 101:
                        defender.LastMove.PP = Math.Max(0, defender.LastMove.PP - 4);
                        msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was reduced!\n";
                        break;
                    // Eerie Spell
                    case 439 when defender.LastMove != null:
                        defender.LastMove.PP = Math.Max(0, defender.LastMove.PP - 3);
                        msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was reduced!\n";
                        break;
                    // Heal Bell
                    case 103:
                    {
                        foreach (var poke in attacker.Owner.Party) poke.NonVolatileEffect.Reset();

                        msg +=
                            $"A bell chimed, and all of {attacker.Owner.Name}'s pokemon had status conditions removed!\n";
                        break;
                    }
                    // Psycho Shift
                    case 235:
                    {
                        var transferedStatus = attacker.NonVolatileEffect.Current;
                        msg += defender.NonVolatileEffect.ApplyStatus(transferedStatus, battle, attacker,
                            this);
                        if (defender.NonVolatileEffect.Current == transferedStatus)
                        {
                            attacker.NonVolatileEffect.Reset();
                            msg += $"{attacker.Name}'s {transferedStatus} was transfered to {defender.Name}!\n";
                        }
                        else
                        {
                            msg += "But it failed!\n";
                        }

                        break;
                    }
                    // Defog
                    case 259:
                        defender.Owner.Spikes = 0;
                        defender.Owner.ToxicSpikes = 0;
                        defender.Owner.StealthRock = false;
                        defender.Owner.StickyWeb = false;
                        defender.Owner.AuroraVeil = new ExpiringEffect(0);
                        defender.Owner.LightScreen = new ExpiringEffect(0);
                        defender.Owner.Reflect = new ExpiringEffect(0);
                        defender.Owner.Mist = new ExpiringEffect(0);
                        defender.Owner.Safeguard = new ExpiringEffect(0);
                        attacker.Owner.Spikes = 0;
                        attacker.Owner.ToxicSpikes = 0;
                        attacker.Owner.StealthRock = false;
                        attacker.Owner.StickyWeb = false;
                        battle.Terrain.End();
                        msg += $"{attacker.Name} blew away the fog!\n";
                        break;
                    // Trick room
                    case 260:
                    {
                        if (battle.TrickRoom.Active())
                        {
                            battle.TrickRoom.SetTurns(0);
                            msg += "The Dimensions returned back to normal!\n";
                        }
                        else
                        {
                            battle.TrickRoom.SetTurns(5);
                            msg += $"{attacker.Name} twisted the dimensions!\n";
                        }

                        break;
                    }
                    // Magic Room
                    case 287:
                    {
                        if (battle.MagicRoom.Active())
                        {
                            battle.MagicRoom.SetTurns(0);
                            msg += "The room returns to normal, and held items regain their effect!\n";
                        }
                        else
                        {
                            battle.MagicRoom.SetTurns(5);
                            msg += "A bizzare area was created, and pokemon's held items lost their effect!\n";
                        }

                        break;
                    }
                    // Wonder Room
                    case 282:
                    {
                        if (battle.WonderRoom.Active())
                        {
                            battle.WonderRoom.SetTurns(0);
                            msg += "The room returns to normal, and stats swap back to what they were before!\n";
                        }
                        else
                        {
                            battle.WonderRoom.SetTurns(5);
                            msg +=
                                "A bizzare area was created, and pokemon's defense and special defense were swapped!\n";
                        }

                        break;
                    }
                    // Perish Song
                    case 115:
                    {
                        msg += "All pokemon hearing the song will faint after 3 turns!\n";
                        if (attacker.PerishSong.Active())
                            msg += $"{attacker.Name} is already under the effect of perish song!\n";
                        else
                            attacker.PerishSong.SetTurns(4);

                        if (defender.PerishSong.Active())
                            msg += $"{defender.Name} is already under the effect of perish song!\n";
                        else if (defender.Ability(attacker, this) == Ability.SOUNDPROOF)
                            msg += $"{defender.Name}'s soundproof protects it from hearing the song!\n";
                        else
                            defender.PerishSong.SetTurns(4);

                        break;
                    }
                    // Nightmare
                    case 108:
                        defender.Nightmare = true;
                        msg += $"{defender.Name} fell into a nightmare!\n";
                        break;
                    // Gravity
                    case 216:
                    {
                        battle.Gravity.SetTurns(5);
                        msg += "Gravity intensified!\n";
                        defender.Telekinesis.SetTurns(0);
                        if (defender.Fly)
                        {
                            defender.Fly = false;
                            defender.LockedMove = null;
                            msg += $"{defender.Name} fell from the sky!\n";
                        }

                        break;
                    }
                    // Spikes
                    case 113:
                        defender.Owner.Spikes += 1;
                        msg += $"Spikes were scattered around the feet of {defender.Owner.Name}'s team!\n";
                        break;
                    // Toxic Spikes
                    case 250:
                        defender.Owner.ToxicSpikes += 1;
                        msg += $"Toxic spikes were scattered around the feet of {defender.Owner.Name}'s team!\n";
                        break;
                    // Stealth Rock
                    case 267:
                        defender.Owner.StealthRock = true;
                        msg += $"Pointed stones float in the air around {defender.Owner.Name}'s team!\n";
                        break;
                    // Sticky Web
                    case 341:
                        defender.Owner.StickyWeb = true;
                        msg += $"A sticky web is shot around the feet of {defender.Owner.Name}'s team!\n";
                        break;
                    // Defense curl
                    case 157 when !attacker.DefenseCurl:
                        attacker.DefenseCurl = true;
                        break;
                    // Psych Up
                    case 144:
                        attacker.AttackStage = defender.AttackStage;
                        attacker.DefenseStage = defender.DefenseStage;
                        attacker.SpAtkStage = defender.SpAtkStage;
                        attacker.SpDefStage = defender.SpDefStage;
                        attacker.SpeedStage = defender.SpeedStage;
                        attacker.AccuracyStage = defender.AccuracyStage;
                        attacker.EvasionStage = defender.EvasionStage;
                        attacker.FocusEnergy = defender.FocusEnergy;
                        msg += "It psyched itself up!\n";
                        break;
                    // Conversion
                    case 31:
                    {
                        var t = attacker.Moves[0].Type;
                        if (!Enum.IsDefined(typeof(ElementType), t)) t = ElementType.NORMAL;

                        attacker.TypeIds = new List<ElementType> { t };
                        var typeName = t.ToString().ToLower();
                        msg += $"{attacker.Name} transformed into a {typeName} type!\n";
                        break;
                    }
                    // Conversion 2
                    case 94:
                    {
                        var t = GetConversion2(attacker, defender, battle);
                        if (t.HasValue)
                        {
                            attacker.TypeIds = new List<ElementType> { t.Value };
                            var typeName = t.Value.ToString().ToLower();
                            msg += $"{attacker.Name} transformed into a {typeName} type!\n";
                        }

                        break;
                    }
                    // Burn up
                    case 398:
                        attacker.TypeIds.Remove(ElementType.FIRE);
                        msg += $"{attacker.Name} lost its fire type!\n";
                        break;
                    // Double shock
                    case 481:
                        attacker.TypeIds.Remove(ElementType.ELECTRIC);
                        msg += $"{attacker.Name} lost its electric type!\n";
                        break;
                    // Forest's Curse
                    case 376:
                        defender.TypeIds.Add(ElementType.GRASS);
                        msg += $"{defender.Name} added grass type!\n";
                        break;
                    // Trick or Treat
                    case 343:
                        defender.TypeIds.Add(ElementType.GHOST);
                        msg += $"{defender.Name} added ghost type!\n";
                        break;
                    // Soak
                    case 295:
                        defender.TypeIds = new List<ElementType> { ElementType.WATER };
                        msg += $"{defender.Name} was transformed into a water type!\n";
                        break;
                    // Magic Powder
                    case 456:
                        defender.TypeIds = new List<ElementType> { ElementType.PSYCHIC };
                        msg += $"{defender.Name} was transformed into a psychic type!\n";
                        break;
                    // Camouflage
                    case 214:
                    {
                        switch (battle.Terrain.Item?.ToString())
                        {
                            case "grassy":
                                attacker.TypeIds = new List<ElementType> { ElementType.GRASS };
                                msg += $"{attacker.Name} was transformed into a grass type!\n";
                                break;
                            case "misty":
                                attacker.TypeIds = new List<ElementType> { ElementType.FAIRY };
                                msg += $"{attacker.Name} was transformed into a fairy type!\n";
                                break;
                            case "electric":
                                attacker.TypeIds = new List<ElementType> { ElementType.ELECTRIC };
                                msg += $"{attacker.Name} was transformed into an electric type!\n";
                                break;
                            case "psychic":
                                attacker.TypeIds = new List<ElementType> { ElementType.PSYCHIC };
                                msg += $"{attacker.Name} was transformed into a psychic type!\n";
                                break;
                            default:
                                attacker.TypeIds = new List<ElementType> { ElementType.NORMAL };
                                msg += $"{attacker.Name} was transformed into a normal type!\n";
                                break;
                        }

                        break;
                    }
                    // Role Play
                    case 179:
                    {
                        attacker.AbilityId = defender.AbilityId;
                        var abilityName = ((Ability)attacker.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{attacker.Name} acquired {abilityName}!\n";
                        msg += attacker.SendOutAbility(defender, battle);
                        break;
                    }
                    // Simple Beam
                    case 299:
                        defender.AbilityId = (int)Ability.SIMPLE;
                        msg += $"{defender.Name} acquired simple!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        break;
                    // Entrainment
                    case 300:
                    {
                        defender.AbilityId = attacker.AbilityId;
                        var abilityName = ((Ability)defender.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{defender.Name} acquired {abilityName}!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        break;
                    }
                    // Worry Seed
                    case 248:
                    {
                        defender.AbilityId = (int)Ability.INSOMNIA;
                        if (defender.NonVolatileEffect.Sleep()) defender.NonVolatileEffect.Reset();

                        msg += $"{defender.Name} acquired insomnia!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        break;
                    }
                    // Skill Swap
                    case 192:
                    {
                        (defender.AbilityId, attacker.AbilityId) = (attacker.AbilityId, defender.AbilityId);
                        var defenderAbilityName = ((Ability)defender.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{defender.Name} acquired {defenderAbilityName}!\n";
                        msg += defender.SendOutAbility(attacker, battle);
                        var attackerAbilityName = ((Ability)attacker.AbilityId).ToString().ToLower().Replace('_', ' ');
                        msg += $"{attacker.Name} acquired {attackerAbilityName}!\n";
                        msg += attacker.SendOutAbility(defender, battle);
                        break;
                    }
                    // Aurora Veil
                    case 407:
                    {
                        if (attacker.HeldItem == "light-clay")
                            attacker.Owner.AuroraVeil.SetTurns(8);
                        else
                            attacker.Owner.AuroraVeil.SetTurns(5);

                        msg += $"{attacker.Name} put up its aurora veil!\n";
                        break;
                    }
                    // Light Screen
                    case 36 or 421:
                    {
                        if (attacker.HeldItem == "light-clay")
                            attacker.Owner.LightScreen.SetTurns(8);
                        else
                            attacker.Owner.LightScreen.SetTurns(5);

                        msg += $"{attacker.Name} put up its light screen!\n";
                        break;
                    }
                    // Reflect
                    case 66 or 422:
                    {
                        if (attacker.HeldItem == "light-clay")
                            attacker.Owner.Reflect.SetTurns(8);
                        else
                            attacker.Owner.Reflect.SetTurns(5);

                        msg += $"{attacker.Name} put up its reflect!\n";
                        break;
                    }
                    // Mist
                    case 47:
                        attacker.Owner.Mist.SetTurns(5);
                        msg += $"{attacker.Name} gained the protection of mist!\n";
                        break;
                    // Bind
                    case 43:
                    case 262 when defender.Substitute == 0 && !defender.Bind.Active():
                    {
                        if (attacker.HeldItem == "grip-claw")
                            defender.Bind.SetTurns(7);
                        else
                            defender.Bind.SetTurns(new Random().Next(4, 6));

                        msg += $"{defender.Name} was squeezed!\n";
                        break;
                    }
                    // Sketch
                    case 96:
                    {
                        var m = defender.LastMove.Copy();
                        attacker.Moves[attacker.Moves.IndexOf(this)] = m;
                        msg += $"The move {m.PrettyName} was sketched!\n";
                        break;
                    }
                    // Transform
                    case 58:
                        msg += $"{attacker.Name} transformed into {defender.Name}!\n";
                        attacker.Transform(defender);
                        break;
                    // Substitute
                    case 80:
                    {
                        var hp = attacker.StartingHp / 4;
                        msg += attacker.Damage(hp, battle, attacker: attacker, source: "building a substitute");
                        attacker.Substitute = hp;
                        attacker.Bind = new ExpiringEffect(0);
                        msg += $"{attacker.Name} made a substitute!\n";
                        break;
                    }
                    // Shed Tail
                    case 493:
                    {
                        var hp = attacker.StartingHp / 4;
                        msg += attacker.Damage(attacker.StartingHp / 2, battle, attacker: attacker,
                            source: "building a substitute");
                        attacker.Owner.NextSubstitute = hp;
                        attacker.Bind = new ExpiringEffect(0);
                        msg += $"{attacker.Name} left behind a substitute!\n";
                        msg += attacker.Remove(battle);
                        // Force this pokemon to immediately return to be attacked
                        attacker.Owner.MidTurnRemove = true;
                        break;
                    }
                    // Throat Chop
                    case 393 when !defender.Silenced.Active():
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                        {
                            defender.Silenced.SetTurns(3);
                            msg += $"{defender.Name} was silenced!\n";
                        }

                        break;
                    }
                    // Speed Swap
                    case 399:
                    {
                        (attacker.Speed, defender.Speed) = (defender.Speed, attacker.Speed);
                        msg += "Both pokemon exchange speed!\n";
                        break;
                    }
                    // Mimic
                    case 83:
                    {
                        var m = defender.LastMove.Copy();
                        m.PP = m.StartingPP;
                        attacker.Moves[attacker.Moves.IndexOf(this)] = m;
                        msg += $"{attacker.Name} mimicked {m.PrettyName}!\n";
                        break;
                    }
                    // Rage
                    case 82:
                        attacker.Rage = true;
                        msg += $"{attacker.Name}'s rage is building!\n";
                        break;
                    // Mind Reader
                    case 95:
                        defender.MindReader.Set(attacker, 2);
                        msg += $"{attacker.Name} took aim at {defender.Name}!\n";
                        break;
                    // Destiny Bond
                    case 99:
                        attacker.DestinyBond = true;
                        attacker.DestinyBondCooldown.SetTurns(2);
                        msg += $"{attacker.Name} is trying to take its foe with it!\n";
                        break;
                    // Ingrain
                    case 182:
                        attacker.Ingrain = true;
                        msg += $"{attacker.Name} planted its roots!\n";
                        break;
                    // Attract
                    case 121:
                        msg += defender.Infatuate(attacker, this);
                        break;
                    // Heart Swap
                    case 251:
                    {
                        (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                        (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                        (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                        (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                        (attacker.SpeedStage, defender.SpeedStage) = (defender.SpeedStage, attacker.SpeedStage);

                        (attacker.AccuracyStage, defender.AccuracyStage) =
                            (defender.AccuracyStage, attacker.AccuracyStage);

                        (attacker.EvasionStage, defender.EvasionStage) = (defender.EvasionStage, attacker.EvasionStage);

                        msg += $"{attacker.Name} switched stat changes with {defender.Name}!\n";
                        break;
                    }
                    // Power Swap
                    case 244:
                    {
                        (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                        (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                        msg +=
                            $"{attacker.Name} switched attack and special attack stat changes with {defender.Name}!\n";
                        break;
                    }
                    // Guard Swap
                    case 245:
                    {
                        (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                        (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                        msg +=
                            $"{attacker.Name} switched defense and special defense stat changes with {defender.Name}!\n";
                        break;
                    }
                    // Aqua Ring
                    case 252:
                        attacker.AquaRing = true;
                        msg += $"{attacker.Name} surrounded itself with a veil of water!\n";
                        break;
                    // Magnet Rise
                    case 253:
                        attacker.MagnetRise.SetTurns(5);
                        msg += $"{attacker.Name} levitated with electromagnetism!\n";
                        break;
                    // Healing Wish
                    case 221:
                        attacker.Owner.HealingWish = true;
                        msg += $"{attacker.Name}'s replacement will be restored!\n";
                        break;
                    // Lunar Dance
                    case 271:
                        attacker.Owner.LunarDance = true;
                        msg += $"{attacker.Name}'s replacement will be restored!\n";
                        break;
                    // Gastro Acid
                    case 240:
                        defender.AbilityId = 0;
                        msg += $"{defender.Name}'s ability was disabled!\n";
                        break;
                    // Lucky Chant
                    case 241:
                        attacker.LuckyChant.SetTurns(5);
                        msg += $"{attacker.Name} is shielded from critical hits!\n";
                        break;
                    // Safeguard
                    case 125:
                        attacker.Owner.Safeguard.SetTurns(5);
                        msg += $"{attacker.Name} is protected from status effects!\n";
                        break;
                    // Guard Split
                    case 280:
                        attacker.DefenseSplit = defender.GetRawDefense();
                        attacker.SpDefSplit = defender.GetRawSpDef();
                        defender.DefenseSplit = attacker.GetRawDefense();
                        defender.SpDefSplit = attacker.GetRawSpDef();
                        msg += $"{attacker.Name} and {defender.Name} shared their guard!\n";
                        break;
                    // Power Split
                    case 281:
                        attacker.AttackSplit = defender.GetRawAttack();
                        attacker.SpAtkSplit = defender.GetRawSpAtk();
                        defender.AttackSplit = attacker.GetRawAttack();
                        defender.SpAtkSplit = attacker.GetRawSpAtk();
                        msg += $"{attacker.Name} and {defender.Name} shared their power!\n";
                        break;
                    // Smack Down/Thousand Arrows
                    case 288 or 373:
                    {
                        defender.Telekinesis.SetTurns(0);
                        if (defender.Fly)
                        {
                            defender.Fly = false;
                            defender.LockedMove = null;
                            defender.HasMoved = true;
                            msg += $"{defender.Name} was shot out of the air!\n";
                        }

                        if (!defender.Grounded(battle, attacker, this))
                        {
                            defender.GroundedByMove = true;
                            msg += $"{defender.Name} was grounded!\n";
                        }

                        break;
                    }
                    // Reflect Type
                    case 319:
                        attacker.TypeIds = new List<ElementType>(defender.TypeIds);
                        msg += $"{attacker.Name}'s type changed to match {defender.Name}!\n";
                        break;
                    // Charge
                    case 175:
                        // TODO: Gen 9 makes charge last until an electric move is used
                        attacker.Charge.SetTurns(2);
                        msg += $"{attacker.Name} charges up electric type moves!\n";
                        break;
                    // Magic Coat
                    case 184:
                        attacker.MagicCoat = true;
                        msg += $"{attacker.Name} shrouded itself with a magic coat!\n";
                        break;
                    // Tailwind
                    case 226:
                    {
                        attacker.Owner.Tailwind.SetTurns(4);
                        msg += $"{attacker.Owner.Name}'s team gets a tailwind!\n";
                        if (attacker.Ability() == Ability.WIND_RIDER)
                            msg += attacker.AppendAttack(1, attacker, source: "its wind rider");

                        break;
                    }
                }

                break;
            }
        }

        return msg;
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
}