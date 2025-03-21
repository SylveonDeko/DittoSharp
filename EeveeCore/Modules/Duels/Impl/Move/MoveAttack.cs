namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Attacks the defender using this move.
    /// </summary>
    /// <returns>A string of formatted results of this attack and the number of hits this move did.</returns>
    public (string, int) Attack(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        // https://bulbapedia.bulbagarden.net/wiki/Damage
        var msg = "";
        var currentType = GetType(attacker, defender, battle);

        // Move effectiveness
        var effectiveness = defender.Effectiveness(currentType, battle, attacker, this);
        if (Effect == 338) effectiveness *= defender.Effectiveness(ElementType.FLYING, battle, attacker, this);

        switch (effectiveness)
        {
            case <= 0:
                return ("The attack had no effect!\n", 0);
            case <= .5:
                msg += "It's not very effective...\n";
                break;
            case >= 2:
                msg += "It's super effective!\n";
                break;
        }

        // Calculate the number of hits for this move.
        var parentalBond = false;
        var minHits = MinHits;
        var maxHits = MaxHits;
        int hits;

        if (Effect == 361 && attacker.Name == "Greninja-ash")
        {
            hits = 3;
        }
        else if (minHits.HasValue && maxHits.HasValue)
        {
            // Handle hit range overrides
            if (attacker.Ability() == Ability.SKILL_LINK)
                minHits = maxHits;
            else if (attacker.HeldItem == "loaded-dice" && maxHits >= 4 && (minHits < 4 || Effect == 484)) minHits = 4;

            // Randomly select number of hits
            if (minHits == 2 && maxHits == 5)
            {
                // Weighted random distribution matching the Python implementation
                var hitChoices = new[]
                {
                    2, 2, 2, 2, 2, 2, 2,
                    3, 3, 3, 3, 3, 3, 3,
                    4, 4, 4,
                    5, 5, 5
                };
                hits = hitChoices[new Random().Next(hitChoices.Length)];
            }
            else
            {
                hits = new Random().Next(minHits.Value, maxHits.Value + 1);
            }
        }
        else
        {
            if (attacker.Ability() == Ability.PARENTAL_BOND)
            {
                hits = 2;
                parentalBond = true;
            }
            else
            {
                hits = 1;
            }
        }

        for (var hit = 0; hit < hits; hit++)
        {
            if (defender.Hp == 0) break;

            // Explosion faints the user first, but should still do damage after death.
            // Future sight still needs to hit after the attacker dies.
            // Mind blown still needs to hit after the attacker dies.
            if (attacker.Hp == 0 && !new[] { 8, 149, 420, 444 }.Contains(Effect)) break;

            // Critical hit chance
            var criticalStage = CritRate;
            if (attacker.HeldItem == "scope-lens" || attacker.HeldItem == "razor-claw") criticalStage += 1;

            if (attacker.Ability() == Ability.SUPER_LUCK) criticalStage += 1;

            if (attacker.FocusEnergy) criticalStage += 2;

            if (attacker.LansatBerryAte) criticalStage += 2;

            criticalStage = Math.Min(criticalStage, 3);

            var critMap = new[] { 24, 8, 2, 1 };
            var critical = new Random().Next(critMap[criticalStage]) == 0;

            if (attacker.Ability() == Ability.MERCILESS && defender.NonVolatileEffect.Poison()) critical = true;

            // Always scores a critical hit.
            if (Effect == 289) critical = true;

            if (attacker.LaserFocus.Active()) critical = true;

            if (defender.Ability(attacker, this) == Ability.SHELL_ARMOR ||
                defender.Ability(attacker, this) == Ability.BATTLE_ARMOR)
                critical = false;

            if (defender.LuckyChant.Active()) critical = false;

            // Confusion never crits
            if (Id == 0xCFCF) critical = false;

            // Stats
            int a, d;
            DamageClass damageClass;

            if (DamageClass == DamageClass.PHYSICAL)
            {
                damageClass = DamageClass.PHYSICAL;
                a = attacker.GetAttack(
                    battle,
                    critical,
                    defender.Ability(attacker, this) == Ability.UNAWARE
                );

                if (Effect == 304)
                    d = defender.GetRawDefense();
                else
                    d = defender.GetDefense(
                        battle,
                        critical,
                        attacker.Ability() == Ability.UNAWARE,
                        attacker,
                        this
                    );
            }
            else
            {
                damageClass = DamageClass.SPECIAL;
                a = attacker.GetSpAtk(
                    battle,
                    critical,
                    defender.Ability(attacker, this) == Ability.UNAWARE
                );

                if (Effect == 304)
                    d = defender.GetRawSpDef();
                else
                    d = defender.GetSpDef(
                        battle,
                        critical,
                        attacker.Ability() == Ability.UNAWARE,
                        attacker,
                        this
                    );
            }

            switch (Effect)
            {
                // Always uses defender's defense
                case 283:
                    d = defender.GetDefense(
                        battle,
                        critical,
                        attacker.Ability() == Ability.UNAWARE,
                        attacker,
                        this
                    );
                    break;
                // Use the user's defense instead of attack for the attack stat
                case 426:
                    // This does not pass critical, otherwise it would crop the wrong direction.
                    a = attacker.GetDefense(
                        battle,
                        ignoreStages: defender.Ability(attacker, this) == Ability.UNAWARE
                    );
                    break;
                // Use the defender's attacking stat
                case 298:
                {
                    if (DamageClass == DamageClass.PHYSICAL)
                        a = defender.GetAttack(
                            battle,
                            critical,
                            defender.Ability(attacker, this) == Ability.UNAWARE
                        );
                    else
                        a = defender.GetSpAtk(
                            battle,
                            critical,
                            defender.Ability(attacker, this) == Ability.UNAWARE
                        );

                    break;
                }
                // Use the higher of attack or special attack
                case 416:
                {
                    var ignoreStages = defender.Ability(attacker, this) == Ability.UNAWARE;
                    a = Math.Max(
                        attacker.GetAttack(battle, critical, ignoreStages),
                        attacker.GetSpAtk(battle, critical, ignoreStages)
                    );
                    break;
                }
            }

            if (attacker.FlashFire && currentType == ElementType.FIRE) a = (int)(a * 1.5);

            if (defender.Ability(attacker, this) == Ability.THICK_FAT &&
                currentType is ElementType.FIRE or ElementType.ICE)
                a = (int)(a * 0.5);

            var power = GetPower(attacker, defender, battle);
            if (power == null) throw new InvalidOperationException($"{Name} has no power and no override.");

            // Check accuracy on each hit
            // WARNING: If there is something BEFORE this in the loop which adds to msg (like "A critical hit")
            // it MUST be after this block, or it will appear even after "misses" from this move.
            if (hit > 0 && attacker.Ability() != Ability.SKILL_LINK)
            {
                // Increasing damage each hit
                if (Effect == 105)
                {
                    if (!CheckHit(attacker, defender, battle))
                    {
                        // Reset the number of hits to the number of ACTUAL hits
                        hits = hit;
                        break;
                    }

                    // x2 then x3
                    power = (int)(power * (1 + hit));
                }

                // Only checks if loaded dice did not activate
                if (Effect == 484 && attacker.HeldItem != "loaded-dice")
                    if (!CheckHit(attacker, defender, battle))
                    {
                        hits = hit;
                        break;
                    }
            }

            double damage = 2 * attacker.Level;
            damage /= 5;
            damage += 2;
            damage = damage * power.Value * ((double)a / d);
            damage /= 50;
            damage += 2;

            // Critical hit damage
            if (critical)
            {
                msg += "A critical hit!\n";
                damage *= 1.5;
            }

            switch (currentType)
            {
                // Type buffing weather
                case ElementType.WATER when new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()):
                    damage *= 1.5;
                    break;
                case ElementType.FIRE when new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()):
                    damage *= 0.5;
                    break;
                case ElementType.FIRE when battle.Weather.Get() == "sun":
                    damage *= 1.5;
                    break;
                case ElementType.WATER when battle.Weather.Get() == "sun":
                    damage *= 0.5;
                    break;
            }

            // Same type attack bonus - extra damage for using a move that is the same type as your poke's type.
            if (attacker.TypeIds.Contains(currentType))
            {
                if (attacker.Ability() == Ability.ADAPTABILITY)
                    damage *= 2;
                else
                    damage *= 1.5;
            }

            // Move effectiveness
            damage *= effectiveness;

            // Burn
            if (
                attacker.NonVolatileEffect.Burn() &&
                damageClass == DamageClass.PHYSICAL &&
                attacker.Ability() != Ability.GUTS &&
                Effect != 170
            )
                damage *= .5;

            // Aurora Veil, Light Screen, Reflect do not stack but all reduce incoming damage in some way
            if (!critical && attacker.Ability() != Ability.INFILTRATOR)
            {
                if (defender.Owner.AuroraVeil.Active())
                    damage *= .5;
                else if (defender.Owner.LightScreen.Active() && damageClass == DamageClass.SPECIAL)
                    damage *= .5;
                else if (defender.Owner.Reflect.Active() && damageClass == DamageClass.PHYSICAL) damage *= .5;
            }

            // Moves that do extra damage to minimized pokes
            if (defender.Minimized && Effect == 338) damage *= 2;

            // Fluffy
            if (defender.Ability(attacker, this) == Ability.FLUFFY)
            {
                if (MakesContact(attacker)) damage *= .5;

                if (currentType == ElementType.FIRE) damage *= 2;
            }

            // Abilities that change damage
            if (new[] { Ability.FILTER, Ability.PRISM_ARMOR, Ability.SOLID_ROCK }.Contains(
                    defender.Ability(attacker, this)) && effectiveness > 1)
                damage *= .75;

            if (attacker.Ability() == Ability.NEUROFORCE && effectiveness > 1) damage *= 1.25;

            if (defender.Ability(attacker, this) == Ability.ICE_SCALES &&
                damageClass == DamageClass.SPECIAL)
                damage *= .5;

            if (attacker.Ability() == Ability.SNIPER && critical) damage *= 1.5;

            if (attacker.Ability() == Ability.TINTED_LENS && effectiveness < 1) damage *= 2;

            if (attacker.Ability() == Ability.PUNK_ROCK && IsSoundBased()) damage *= 1.3;

            if (defender.Ability(attacker, this) == Ability.PUNK_ROCK && IsSoundBased()) damage *= .5;

            if (defender.Ability(attacker, this) == Ability.HEATPROOF &&
                currentType == ElementType.FIRE)
                damage *= .5;

            if (defender.Ability(attacker, this) == Ability.PURIFYING_SALT &&
                currentType == ElementType.GHOST)
                damage *= .5;

            // Aura abilities
            if ((attacker.Ability() == Ability.DARK_AURA ||
                 defender.Ability(attacker, this) == Ability.DARK_AURA) &&
                currentType == ElementType.DARK)
            {
                if (attacker.Ability() == Ability.AURA_BREAK ||
                    defender.Ability(attacker, this) == Ability.AURA_BREAK)
                    damage *= .75;
                else
                    damage *= 4.0 / 3.0;
            }

            if ((attacker.Ability() == Ability.FAIRY_AURA ||
                 defender.Ability(attacker, this) == Ability.FAIRY_AURA) &&
                currentType == ElementType.FAIRY)
            {
                if (attacker.Ability() == Ability.AURA_BREAK ||
                    defender.Ability(attacker, this) == Ability.AURA_BREAK)
                    damage *= .75;
                else
                    damage *= 4.0 / 3.0;
            }

            if (defender.Ability(attacker, this) == Ability.DRY_SKIN &&
                currentType == ElementType.FIRE)
                damage *= 1.25;

            // Items that change damage
            if (defender.HeldItem == "chilan-berry" && currentType == ElementType.NORMAL) damage *= .5;

            if (attacker.HeldItem == "expert-belt" && effectiveness > 1) damage *= 1.2;

            if (
                attacker.HeldItem == "life-orb" &&
                DamageClass != DamageClass.STATUS &&
                Effect != 149
            )
                damage *= 1.3;

            if (attacker.HeldItem == "metronome") damage *= attacker.Metronome.GetBuff(Name);

            // Parental bond - adds an extra low power hit
            if (parentalBond && hit > 0) damage *= .25;

            // Reduced damage while at full hp
            if (new[] { Ability.MULTISCALE, Ability.SHADOW_SHIELD }.Contains(defender.Ability(attacker,
                    this)) &&
                defender.Hp == defender.StartingHp)
                damage *= .5;

            // Random damage scaling
            damage *= new Random().NextDouble() * (1.0 - 0.85) + 0.85;
            damage = Math.Max(1, (int)damage);

            // Cannot lower the target's HP below 1.
            if (Effect == 102) damage = Math.Min(damage, defender.Hp - 1);

            // Drain ratios
            double? drainHealRatio = null;
            if (new[] { 4, 9, 346, 500 }.Contains(Effect))
                drainHealRatio = 1.0 / 2.0;
            else if (Effect == 349) drainHealRatio = 3.0 / 4.0;

            // Do the damage
            var (msgadd, actualDamage) = defender._Damage((int)damage, battle, this, currentType,
                attacker, critical, drainHealRatio);
            msg += msgadd;

            // Recoil
            if (attacker.Ability() != Ability.ROCK_HEAD && defender.Owner.HasAlivePokemon())
            {
                if (Effect == 49) msg += attacker.Damage(actualDamage / 4, battle, source: "recoil");

                if (new[] { 199, 254, 263, 469 }.Contains(Effect))
                    msg += attacker.Damage(actualDamage / 3, battle, source: "recoil");

                switch (Effect)
                {
                    case 270:
                        msg += attacker.Damage(actualDamage / 2, battle, source: "recoil");
                        break;
                    case 463:
                        msg += attacker.Damage(attacker.StartingHp / 2, battle, source: "recoil");
                        break;
                }
            }
        }

        // Weakness Policy
        if (effectiveness > 1 && defender.HeldItem == "weakness-policy" && defender.Substitute == 0)
        {
            msg += defender.AppendAttack(2, defender, this, "its weakness policy");
            msg += defender.AppendSpAtk(2, defender, this, "its weakness policy");
            defender.HeldItem.Use();
        }

        return (msg, hits);
    }

    /// <summary>
    ///     Get the power of this move.
    /// </summary>
    public int? GetPower(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        var currentType = GetType(attacker, defender, battle);
        int? power = null;

        switch (Effect)
        {
            // Inflicts damage equal to the user's level.
            case 88:
                power = attacker.Level;
                break;
            // Inflicts damage between 50% and 150% of the user's level.
            case 89:
                power = new Random().Next((int)(attacker.Level * 0.5), (int)(attacker.Level * 1.5) + 1);
                break;
            // Inflicts more damage to heavier targets, with a maximum of 120 power.
            case 197:
            {
                var defWeight = defender.Weight(attacker, this);
                switch (defWeight)
                {
                    case <= 100:
                        power = 20;
                        break;
                    case <= 250:
                        power = 40;
                        break;
                    case <= 500:
                        power = 60;
                        break;
                    case <= 1000:
                        power = 80;
                        break;
                    case <= 2000:
                        power = 100;
                        break;
                    default:
                        power = 120;
                        break;
                }

                break;
            }
            // Power is higher when the user weighs more than the target, up to a maximum of 120.
            case 292:
            {
                var weightDelta = (double)attacker.Weight() / defender.Weight(attacker, this);
                switch (weightDelta)
                {
                    case <= 2:
                        power = 40;
                        break;
                    case <= 3:
                        power = 60;
                        break;
                    case <= 4:
                        power = 80;
                        break;
                    case <= 5:
                        power = 100;
                        break;
                    default:
                        power = 120;
                        break;
                }

                break;
            }
            // Power increases with happiness, up to a maximum of 102.
            case 122:
            {
                power = (int)(attacker.Happiness / 2.5);
                switch (power)
                {
                    case > 102:
                        power = 102;
                        break;
                    case < 1:
                        power = 1;
                        break;
                }

                break;
            }
            // Power increases as happiness **decreases**, up to a maximum of 102.
            case 124:
            {
                power = (int)((255 - attacker.Happiness) / 2.5);
                switch (power)
                {
                    case > 102:
                        power = 102;
                        break;
                    case < 1:
                        power = 1;
                        break;
                }

                break;
            }
            // Power raises when the user has lower Speed, up to a maximum of 150.
            case 220:
                power = Math.Min(150, 1 + 25 * defender.GetSpeed(battle) / attacker.GetSpeed(battle));
                break;
            // Inflicts more damage when the user has more HP remaining, with a maximum of 150 power.
            case 191:
                power = (int)(150 * ((double)attacker.Hp / attacker.StartingHp));
                break;
            // Power is 100 times the amount of energy Stockpiled.
            case 162:
                power = 100 * attacker.Stockpile;
                break;
            // Inflicts more damage when the user has less HP remaining, with a maximum of 200 power.
            case 100:
            {
                var hpPercent = (int)(64 * ((double)attacker.Hp / attacker.StartingHp));
                switch (hpPercent)
                {
                    case <= 1:
                        power = 200;
                        break;
                    case <= 5:
                        power = 150;
                        break;
                    case <= 12:
                        power = 100;
                        break;
                    case <= 21:
                        power = 80;
                        break;
                    case <= 42:
                        power = 40;
                        break;
                    default:
                        power = 20;
                        break;
                }

                break;
            }
            // Power increases when this move has less PP, up to a maximum of 200.
            case 236:
            {
                switch (PP)
                {
                    case 0:
                        power = 200;
                        break;
                    case 1:
                        power = 80;
                        break;
                    case 2:
                        power = 60;
                        break;
                    case 3:
                        power = 50;
                        break;
                    default:
                        power = 40;
                        break;
                }

                break;
            }
            // Power increases against targets with more HP remaining, up to a maximum of 120|100 power.
            case 238:
                power = Math.Max(1, (int)(120 * ((double)defender.Hp / defender.StartingHp)));
                break;
            case 495:
                power = Math.Max(1, (int)(100 * ((double)defender.Hp / defender.StartingHp)));
                break;
            // Power increases against targets with more raised stats, up to a maximum of 200.
            case 246:
            {
                var delta = 0;
                delta += Math.Max(0, defender.AttackStage);
                delta += Math.Max(0, defender.DefenseStage);
                delta += Math.Max(0, defender.SpAtkStage);
                delta += Math.Max(0, defender.SpDefStage);
                delta += Math.Max(0, defender.SpeedStage);
                power = Math.Min(200, 60 + delta * 20);
                break;
            }
            // Power is higher when the user has greater Speed than the target, up to a maximum of 150.
            case 294:
            {
                var delta = attacker.GetSpeed(battle) / defender.GetSpeed(battle);
                switch (delta)
                {
                    case <= 0:
                        power = 40;
                        break;
                    case <= 1:
                        power = 60;
                        break;
                    case <= 2:
                        power = 80;
                        break;
                    case <= 3:
                        power = 120;
                        break;
                    default:
                        power = 150;
                        break;
                }

                break;
            }
            // Power is higher the more the user's stats have been raised.
            case 306:
            {
                var delta = 1;
                delta += Math.Max(0, attacker.AttackStage);
                delta += Math.Max(0, attacker.DefenseStage);
                delta += Math.Max(0, attacker.SpAtkStage);
                delta += Math.Max(0, attacker.SpDefStage);
                delta += Math.Max(0, attacker.SpeedStage);
                delta += Math.Max(0, attacker.AccuracyStage);
                delta += Math.Max(0, attacker.EvasionStage);
                power = 20 * delta;
                break;
            }
            // Power doubles every turn this move is used in succession after the first, maxing out after five turns.
            case 120:
                power = (int)(Math.Pow(2, attacker.FuryCutter) * 10);
                attacker.FuryCutter = Math.Min(4, attacker.FuryCutter + 1);
                break;
            // Power doubles every turn this move is used in succession after the first, resetting after five turns.
            case 118:
                power = (int)(Math.Pow(2, attacker.LockedMove.Turn) * Power.Value);
                break;
            // Power varies randomly from 10 to 150.
            case 127:
            {
                var percentile = new Random().Next(0, 101);
                switch (percentile)
                {
                    case <= 5:
                        power = 10;
                        break;
                    case <= 15:
                        power = 30;
                        break;
                    case <= 35:
                        power = 50;
                        break;
                    case <= 65:
                        power = 70;
                        break;
                    case <= 85:
                        power = 90;
                        break;
                    case <= 95:
                        power = 110;
                        break;
                    default:
                        power = 150;
                        break;
                }

                break;
            }
            // Power is based on the user's held item
            case 234:
                power = attacker.HeldItem.Power;
                break;
            // Power increases by 100% for each consecutive use by any friendly Pokémon, to a maximum of 200.
            case 303:
                power = attacker.EchoedVoicePower;
                break;
            // Power is dependent on the user's held berry.
            case 223:
            {
                var hi = attacker.HeldItem.Get();
                if (new[]
                    {
                        "enigma-berry", "rowap-berry", "maranga-berry", "jaboca-berry", "belue-berry", "kee-berry",
                        "salac-berry", "watmel-berry", "lansat-berry", "custap-berry", "liechi-berry", "apicot-berry",
                        "ganlon-berry", "petaya-berry", "starf-berry", "micle-berry", "durin-berry"
                    }.Contains(hi))
                    power = 100;
                else if (new[]
                         {
                             "cornn-berry", "spelon-berry", "nomel-berry", "wepear-berry", "kelpsy-berry", "bluk-berry",
                             "grepa-berry", "rabuta-berry", "pinap-berry", "hondew-berry", "pomeg-berry",
                             "qualot-berry",
                             "tamato-berry", "magost-berry", "pamtre-berry", "nanab-berry"
                         }.Contains(hi))
                    power = 90;
                else
                    power = 80;

                break;
            }
            case 361 when attacker.Name == "Greninja-ash":
                power = 20;
                break;
            // Power is based on the user's base attack. Only applies when not explicitly overridden.
            case 155 when Power == null:
                power = attacker.GetRawAttack() / 10 + 5;
                break;
            // No special changes to power, return its raw value.
            default:
                power = Power;
                break;
        }

        if (power == null) return null;

        // NOTE: this needs to be first as it only applies to raw power
        if (attacker.Ability() == Ability.TECHNICIAN && power <= 60) power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.TOUGH_CLAWS && MakesContact(attacker)) power = (int)(power * 1.3);

        if (attacker.Ability() == Ability.RIVALRY && "-x" != attacker.Gender && "-x" != defender.Gender)
        {
            if (attacker.Gender == defender.Gender)
                power = (int)(power * 1.25);
            else
                power = (int)(power * .75);
        }

        if (attacker.Ability() == Ability.IRON_FIST && IsPunching()) power = (int)(power * 1.2);

        if (attacker.Ability() == Ability.STRONG_JAW && IsBiting()) power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.MEGA_LAUNCHER && IsAuraOrPulse()) power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.SHARPNESS && IsSlicing()) power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.RECKLESS && new[] { 46, 49, 199, 254, 263, 270 }.Contains(Effect))
            power = (int)(power * 1.2);

        if (attacker.Ability() == Ability.TOXIC_BOOST && DamageClass == DamageClass.PHYSICAL &&
            attacker.NonVolatileEffect.Poison())
            power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.FLARE_BOOST && DamageClass == DamageClass.SPECIAL &&
            attacker.NonVolatileEffect.Burn())
            power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.ANALYTIC && defender.HasMoved) power = (int)(power * 1.3);

        if (attacker.Ability() == Ability.BATTERY && DamageClass == DamageClass.SPECIAL) power = (int)(power * 1.3);

        if (attacker.Ability() == Ability.SHEER_FORCE && EffectChance.HasValue) // Not *perfect* but good enough
            power = (int)(power * 1.3);

        if (attacker.Ability() == Ability.STAKEOUT && defender.SwappedIn) power = (int)(power * 2);

        if (attacker.Ability() == Ability.SUPREME_OVERLORD)
        {
            var fainted = attacker.Owner.Party.Count(poke => poke.Hp == 0);
            if (fainted > 0) power = (int)(power * (10 + fainted) / 10.0);
        }

        // Type buffing abilities - Some use naive type because the type is changed.
        if (attacker.Ability() == Ability.AERILATE && Type == ElementType.NORMAL) power = (int)(power * 1.2);

        if (attacker.Ability() == Ability.PIXILATE && Type == ElementType.NORMAL) power = (int)(power * 1.2);

        if (attacker.Ability() == Ability.GALVANIZE && Type == ElementType.NORMAL) power = (int)(power * 1.2);

        if (attacker.Ability() == Ability.REFRIGERATE && Type == ElementType.NORMAL) power = (int)(power * 1.2);

        if (attacker.Ability() == Ability.DRAGONS_MAW && currentType == ElementType.DRAGON) power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.TRANSISTOR && currentType == ElementType.ELECTRIC) power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.WATER_BUBBLE && currentType == ElementType.WATER) power = (int)(power * 2);

        if (defender.Ability(attacker, this) == Ability.WATER_BUBBLE &&
            currentType == ElementType.FIRE)
            power = (int)(power * .5);

        if (attacker.Ability() == Ability.OVERGROW && currentType == ElementType.GRASS &&
            attacker.Hp <= attacker.StartingHp / 3)
            power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.BLAZE && currentType == ElementType.FIRE &&
            attacker.Hp <= attacker.StartingHp / 3)
            power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.TORRENT && currentType == ElementType.WATER &&
            attacker.Hp <= attacker.StartingHp / 3)
            power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.SWARM && currentType == ElementType.BUG &&
            attacker.Hp <= attacker.StartingHp / 3)
            power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.NORMALIZE && currentType == ElementType.NORMAL) power = (int)(power * 1.2);

        if (attacker.Ability() == Ability.SAND_FORCE &&
            new[] { ElementType.ROCK, ElementType.GROUND, ElementType.STEEL }.Contains(currentType) &&
            battle.Weather.Get() == "sandstorm")
            power = (int)(power * 1.3);

        if (new[] { Ability.STEELWORKER, Ability.STEELY_SPIRIT }.Contains(attacker.Ability()) &&
            currentType == ElementType.STEEL)
            power = (int)(power * 1.5);

        if (attacker.Ability() == Ability.ROCKY_PAYLOAD && currentType == ElementType.ROCK) power = (int)(power * 1.5);

        // Type buffing items
        if (attacker.HeldItem == "black-glasses" && currentType == ElementType.DARK) power = (int)(power * 1.2);

        if (attacker.HeldItem == "black-belt" && currentType == ElementType.FIGHTING) power = (int)(power * 1.2);

        if (attacker.HeldItem == "hard-stone" && currentType == ElementType.ROCK) power = (int)(power * 1.2);

        if (attacker.HeldItem == "magnet" && currentType == ElementType.ELECTRIC) power = (int)(power * 1.2);

        if (attacker.HeldItem == "mystic-water" && currentType == ElementType.WATER) power = (int)(power * 1.2);

        if (attacker.HeldItem == "never-melt-ice" && currentType == ElementType.ICE) power = (int)(power * 1.2);

        if (attacker.HeldItem == "dragon-fang" && currentType == ElementType.DRAGON) power = (int)(power * 1.2);

        if (attacker.HeldItem == "poison-barb" && currentType == ElementType.POISON) power = (int)(power * 1.2);

        if (attacker.HeldItem == "charcoal" && currentType == ElementType.FIRE) power = (int)(power * 1.2);

        if (attacker.HeldItem == "silk-scarf" && currentType == ElementType.NORMAL) power = (int)(power * 1.2);

        if (attacker.HeldItem == "metal-coat" && currentType == ElementType.STEEL) power = (int)(power * 1.2);

        if (attacker.HeldItem == "sharp-beak" && currentType == ElementType.FLYING) power = (int)(power * 1.2);

        if (attacker.HeldItem == "draco-plate" && currentType == ElementType.DRAGON) power = (int)(power * 1.2);

        if (attacker.HeldItem == "dread-plate" && currentType == ElementType.DARK) power = (int)(power * 1.2);

        if (attacker.HeldItem == "earth-plate" && currentType == ElementType.GROUND) power = (int)(power * 1.2);

        if (attacker.HeldItem == "fist-plate" && currentType == ElementType.FIGHTING) power = (int)(power * 1.2);

        if (attacker.HeldItem == "flame-plate" && currentType == ElementType.FIRE) power = (int)(power * 1.2);

        if (attacker.HeldItem == "icicle-plate" && currentType == ElementType.ICE) power = (int)(power * 1.2);

        if (attacker.HeldItem == "insect-plate" && currentType == ElementType.BUG) power = (int)(power * 1.2);

        if (attacker.HeldItem == "iron-plate" && currentType == ElementType.STEEL) power = (int)(power * 1.2);

        if (attacker.HeldItem == "meadow-plate" && currentType == ElementType.GRASS) power = (int)(power * 1.2);

        if (attacker.HeldItem == "mind-plate" && currentType == ElementType.PSYCHIC) power = (int)(power * 1.2);

        if (attacker.HeldItem == "pixie-plate" && currentType == ElementType.FAIRY) power = (int)(power * 1.2);

        if (attacker.HeldItem == "sky-plate" && currentType == ElementType.FLYING) power = (int)(power * 1.2);

        if (attacker.HeldItem == "splash-plate" && currentType == ElementType.WATER) power = (int)(power * 1.2);

        if (attacker.HeldItem == "spooky-plate" && currentType == ElementType.GHOST) power = (int)(power * 1.2);

        if (attacker.HeldItem == "stone-plate" && currentType == ElementType.ROCK) power = (int)(power * 1.2);

        if (attacker.HeldItem == "toxic-plate" && currentType == ElementType.POISON) power = (int)(power * 1.2);

        if (attacker.HeldItem == "zap-plate" && currentType == ElementType.ELECTRIC) power = (int)(power * 1.2);

        if (attacker.HeldItem == "adamant-orb" &&
            new[] { ElementType.DRAGON, ElementType.STEEL }.Contains(currentType) && attacker.Name == "Dialga")
            power = (int)(power * 1.2);

        if (attacker.HeldItem == "griseous-orb" &&
            new[] { ElementType.DRAGON, ElementType.GHOST }.Contains(currentType) && attacker.Name == "Giratina")
            power = (int)(power * 1.2);

        if (attacker.HeldItem == "soul-dew" &&
            new[] { ElementType.DRAGON, ElementType.PSYCHIC }.Contains(currentType) &&
            new[] { "Latios", "Latias" }.Contains(attacker.Name))
            power = (int)(power * 1.2);

        if (attacker.HeldItem == "lustrous-orb" &&
            new[] { ElementType.DRAGON, ElementType.WATER }.Contains(currentType) && attacker.Name == "Palkia")
            power = (int)(power * 1.2);

        // Damage class buffing items
        if (attacker.HeldItem == "wise-glasses" && DamageClass == DamageClass.SPECIAL) power = (int)(power * 1.1);

        if (attacker.HeldItem == "muscle-band" && DamageClass == DamageClass.PHYSICAL) power = (int)(power * 1.1);

        switch (Effect)
        {
            // If there be weather, this move has doubled power and the weather's type.
            case 204 when
                new[] { "hail", "sandstorm", "rain", "h-rain", "sun", "h-sun" }.Contains(battle.Weather.Get()):
                power = (int)(power * 2);
                break;
            // During hail, rain-dance, or sandstorm, power is halved.
            case 152 when new[] { "rain", "hail" }.Contains(battle.Weather.Get()):
                power = (int)(power * 0.5);
                break;
            // Power doubles if user is burned, paralyzed, or poisoned.
            case 170 when attacker.NonVolatileEffect.Burn() || attacker.NonVolatileEffect.Poison() ||
                          attacker.NonVolatileEffect.Paralysis():
                power = (int)(power * 2);
                break;
            // If the target is paralyzed, power is doubled and cures the paralysis.
            case 172 when defender.NonVolatileEffect.Paralysis():
                power = (int)(power * 2);
                defender.NonVolatileEffect.Reset();
                break;
        }

        // If the target is poisoned, this move has double power.
        if (new[] { 284, 461 }.Contains(Effect) && defender.NonVolatileEffect.Poison()) power = (int)(power * 2);

        switch (Effect)
        {
            // If the target is sleeping, this move has double power, and the target wakes up.
            case 218 when defender.NonVolatileEffect.Sleep():
                power = (int)(power * 2);
                defender.NonVolatileEffect.Reset();
                break;
            // Has double power against Pokémon that have less than half their max HP remaining.
            case 222 when defender.Hp < defender.StartingHp / 2:
            // Power is doubled if the target has already moved this turn.
            case 231 when defender.HasMoved:
            // Has double power if the target has a major status ailment.
            case 311 when !string.IsNullOrEmpty(defender.NonVolatileEffect.Current):
            // If the user has used defense-curl since entering the field, this move has double power.
            case 118 when attacker.DefenseCurl:
            // Has double power if the user's last move failed.
            case 409 when attacker.LastMoveFailed:
                power = (int)(power * 2);
                break;
        }

        // Has double power if the target is in the first turn of dive.
        if (new[] { 258, 262 }.Contains(Effect) && defender.Dive) power = (int)(power * 2);

        // Has double power if the target is in the first turn of dig.
        if (new[] { 127, 148 }.Contains(Effect) && defender.Dig) power = (int)(power * 2);

        // Has double power if the target is in the first turn of bounce or fly.
        if (new[] { 147, 150 }.Contains(Effect) && defender.Fly) power = (int)(power * 2);

        switch (Effect)
        {
            // Has double power if the user takes damage before attacking this turn.
            case 186 when attacker.LastMoveDamage != null:
            // Has double power if the user has no held item.
            case 318 when !attacker.HeldItem.HasItem():
            // Has double power if a friendly Pokémon fainted last turn.
            case 320 when attacker.Owner.Retaliate.Active():
            // Has double power against, and can hit, Pokémon attempting to switch out.
            case 129 when defender.Owner.SelectedAction.IsSwitch ||
                          (defender.Owner.SelectedAction is Trainer.MoveAction move &&
                           SourceArray.Contains(move.Move.Effect)):
            // Power is doubled if the target has already received damage this turn.
            case 232 when defender.DmgThisTurn:
            // Power is doubled if the target is minimized.
            case 151 when defender.Minimized:
            // With Fusion Bolt, power is doubled.
            case 336 when battle.LastMoveEffect == 337:
            // With Fusion Flare, power is doubled.
            case 337 when battle.LastMoveEffect == 336:
                power = (int)(power * 2);
                break;
        }

        // Me first increases the power of the used move by 50%.
        if (attacker.Owner.SelectedAction.ToString() != null && attacker.Owner.SelectedAction is Trainer.MoveAction
            {
                Move.Effect: 242
            })
            power = (int)(power * 1.5);

        switch (Effect)
        {
            // Has 1.5x power during gravity.
            case 435 when battle.Gravity.Active():
                power = (int)(power * 1.5);
                break;
            // If the user attacks before the target, or if the target switched in this turn, its base power doubles.
            case 436 when !defender.HasMoved || defender.SwappedIn:
                power = (int)(power * 2);
                break;
            // If the terrain is psychic and the user is grounded, this move gets 1.5x power.
            case 440 when battle.Terrain.Item?.ToString() == "psychic" && attacker.Grounded(battle):
                power = (int)(power * 1.5);
                break;
            // Power is doubled if terrain is present.
            case 441 when battle.Terrain.Item != null && attacker.Grounded(battle):
                power = (int)(power * 2);
                break;
            // Power is boosted by 50% if used on a Pokémon that is holding an item that can be knocked off.
            case 189 when defender.HeldItem.HasItem() && defender.HeldItem.CanRemove():
                power = (int)(power * 1.5);
                break;
            // If the target is under the effect of electric terrain, this move has double power.
            case 443 when battle.Terrain.Item?.ToString() == "electric" &&
                          defender.Grounded(battle, attacker, this):
                power = (int)(power * 2);
                break;
            // Deals 1.5x damage if the user is under the effect of misty terrain.
            case 444 when battle.Terrain.Item?.ToString() == "misty" && attacker.Grounded(battle):
                power = (int)(power * 1.5);
                break;
            // Power is doubled if any of the user's stats were lowered this turn.
            case 450 when attacker.StatDecreased:
            // Power is doubled if the defender has a non volatile status effect.
            case 465 when !string.IsNullOrEmpty(defender.NonVolatileEffect.Current):
                power = (int)(power * 2);
                break;
            // Deals 4/3x damage if supereffective.
            case 482 when defender.Effectiveness(currentType, battle, attacker, this) > 1:
                power = (int)(power * 4.0 / 3.0);
                break;
            // Power is multiplied by (1 + number of fainted party members)x, capping at 101x (100 faints).
            case 490:
                power = (int)(power * (1 + Math.Min(attacker.Owner.NumFainted, 100)));
                break;
            // Power is multiplied by (1 + number of times hit)x, capping at 7x (6 hits).
            case 491:
                power = (int)(power * (1 + Math.Min(attacker.NumHits, 6)));
                break;
            // Has a 30% chance to double power
            case 498 when new Random().NextDouble() <= 0.3:
                power = (int)(power * 2);
                break;
        }

        switch (battle.Terrain.Item?.ToString())
        {
            // Terrains
            case "psychic" when attacker.Grounded(battle) &&
                                currentType == ElementType.PSYCHIC:
            case "grassy" when attacker.Grounded(battle) &&
                               currentType == ElementType.GRASS:
                power = (int)(power * 1.3);
                break;
        }

        switch (battle.Terrain.Item?.ToString())
        {
            case "grassy" when
                defender.Grounded(battle, attacker, this) && new[] { 89, 222, 523 }.Contains(Id):
                power = (int)(power * 0.5);
                break;
            case "electric" when attacker.Grounded(battle) &&
                                 currentType == ElementType.ELECTRIC:
                power = (int)(power * 1.3);
                break;
            case "misty" when
                defender.Grounded(battle, attacker, this) && currentType == ElementType.DRAGON:
                power = (int)(power * 0.5);
                break;
        }

        // Power buffing statuses
        if (attacker.Charge.Active() && currentType == ElementType.ELECTRIC) power = (int)(power * 2);

        if ((attacker.Owner.MudSport.Active() || defender.Owner.MudSport.Active()) &&
            currentType == ElementType.ELECTRIC)
            power = power.Value / 3;

        if ((attacker.Owner.WaterSport.Active() || defender.Owner.WaterSport.Active()) &&
            currentType == ElementType.FIRE)
            power = power.Value / 3;

        return power;
    }
}