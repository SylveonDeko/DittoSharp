namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Causes this Pokemon to faint, removing it from battle and triggering any faint-related effects.
    /// </summary>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="move">The move that caused the fainting (if any).</param>
    /// <param name="attacker">The Pokemon that caused the fainting (if any).</param>
    /// <param name="source">A description of what caused the fainting.</param>
    /// <returns>A formatted message describing the fainting and any triggered effects.</returns>
    public string Faint(Battle battle, Move.Move move = null, DuelPokemon attacker = null, string source = "")
    {
        var msg = "";
        Hp = 0;
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        msg += $"{Name} fainted{source}!\n";
        if (move != null && attacker != null && DestinyBond && Owner.HasAlivePokemon())
            msg += attacker.Faint(battle, source: $"{Name}'s destiny bond");
        if (move != null && attacker is { _name: "Greninja" } && attacker.Ability() == Impl.Ability.BATTLE_BOND)
            if (attacker.Form("Greninja-ash"))
                msg += $"{attacker.Name}'s bond with its trainer has strengthened it!\n";

        if (move != null && Grudge)
        {
            move.PP = 0;
            msg += $"{move.PrettyName}'s pp was depleted!\n";
        }

        if (attacker != null && (attacker.Ability() == Impl.Ability.CHILLING_NEIGH ||
                                 attacker.Ability() == Impl.Ability.AS_ONE_ICE))
            msg += attacker.AppendAttack(1, attacker, source: "its chilling neigh");
        if (attacker != null && (attacker.Ability() == Impl.Ability.GRIM_NEIGH ||
                                 attacker.Ability() == Impl.Ability.AS_ONE_SHADOW))
            msg += attacker.AppendSpAtk(1, attacker, source: "its grim neigh");
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
            if (poke != null && poke != this && poke.Ability() == Impl.Ability.SOUL_HEART)
                msg += poke.AppendSpAtk(1, poke, source: "its soul heart");

        Owner.Retaliate.SetTurns(2);
        Owner.NumFainted++;
        msg += Remove(battle, true);
        return msg;
    }

    /// <summary>
    ///     Applies a certain amount of damage to this pokemon.
    ///     Returns a formatted message.
    /// </summary>
    /// <param name="damage">The amount of damage to deal.</param>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="move">The move causing the damage (if any).</param>
    /// <param name="moveType">The type of the move causing damage.</param>
    /// <param name="attacker">The Pokemon dealing the damage.</param>
    /// <param name="critical">Whether this is a critical hit.</param>
    /// <param name="drainHealRatio">The ratio of damage to heal for drain moves.</param>
    /// <param name="source">A description of the damage source.</param>
    /// <returns>A formatted message describing the damage and any resulting effects.</returns>
    public string Damage(int damage, Battle battle, Move.Move move = null, ElementType? moveType = null,
        DuelPokemon attacker = null, bool critical = false, double? drainHealRatio = null, string source = "")
    {
        var result = _Damage(damage, battle, move, moveType, attacker, critical, drainHealRatio, source);
        return result.Item1;
    }

    /// <summary>
    ///     Applies a certain amount of damage to this pokemon.
    ///     Returns a formatted message and the amount of damage actually dealt.
    /// </summary>
    /// <param name="damage">The amount of damage to deal.</param>
    /// <param name="battle">The current battle instance.</param>
    /// <param name="move">The move causing the damage (if any).</param>
    /// <param name="moveType">The type of the move causing damage.</param>
    /// <param name="attacker">The Pokemon dealing the damage.</param>
    /// <param name="critical">Whether this is a critical hit.</param>
    /// <param name="drainHealRatio">The ratio of damage to heal for drain moves.</param>
    /// <param name="source">A description of the damage source.</param>
    /// <returns>A tuple containing the formatted message and actual damage dealt.</returns>
    public Tuple<string, int> _Damage(int damage, Battle battle, Move.Move move = null, ElementType? moveType = null,
        DuelPokemon attacker = null, bool critical = false, double? drainHealRatio = null, string source = "")
    {
        var msg = "";

        // Don't go through with an attack if the poke is already dead.
        // If this is a bad idea for *some* reason, make sure to add an `attacker is self` check to INNARDS_OUT.
        if (Hp <= 0) return new Tuple<string, int>("", 0);
        var previousHp = Hp;
        damage = Math.Max(1, damage);

        // Magic guard
        if (Ability(attacker, move) == Impl.Ability.MAGIC_GUARD && move == null && attacker != this)
            return new Tuple<string, int>($"{Name}'s magic guard protected it from damage!\n", 0);

        // Substitute
        if (Substitute > 0 && move != null && move.IsAffectedBySubstitute() && !move.IsSoundBased() &&
            (attacker == null || attacker.Ability() != Impl.Ability.INFILTRATOR))
        {
            // IsAffectedBySubstitute should be a superset of IsSoundBased, but it's better to check both to be sure.
            msg += $"{Name}'s substitute took {damage} damage{source}!\n";
            var max = Math.Max(0, Substitute - damage);
            var substitute = Substitute - max;
            Substitute = max;
            if (Substitute == 0) msg += $"{Name}'s substitute broke!\n";
            return new Tuple<string, int>(msg, substitute);
        }

        // Damage blocking forms / abilities
        if (move != null)
        {
            if (Ability(attacker, move) == Impl.Ability.DISGUISE && _name == "Mimikyu")
                if (Form("Mimikyu-busted"))
                {
                    msg += $"{Name}'s disguise was busted!\n";
                    msg += Damage(StartingHp / 8, battle, source: "losing its disguise");
                    return new Tuple<string, int>(msg, 0);
                }

            if (Ability(attacker, move) == Impl.Ability.ICE_FACE && _name == "Eiscue" &&
                move.DamageClass == DamageClass.PHYSICAL)
                if (Form("Eiscue-noice"))
                {
                    msg += $"{Name}'s ice face was busted!\n";
                    return new Tuple<string, int>(msg, 0);
                }
        }

        // OHKO protection
        DmgThisTurn = true;
        if (damage >= Hp && move != null)
        {
            if (Endure)
            {
                msg += $"{Name} endured the hit!\n";
                damage = Hp - 1;
            }
            else if (Hp == StartingHp && Ability(attacker, move) == Impl.Ability.STURDY)
            {
                msg += $"{Name} endured the hit with its Sturdy!\n";
                damage = Hp - 1;
            }
            else if (Hp == StartingHp && HeldItem.Get() == "focus-sash")
            {
                msg += $"{Name} held on using its focus sash!\n";
                damage = Hp - 1;
                HeldItem.Use();
            }
            else if (HeldItem.Get() == "focus-band" && new Random().Next(10) == 0)
            {
                msg += $"{Name} held on using its focus band!\n";
                damage = Hp - 1;
            }
        }

        // Apply the damage
        var droppedBelowHalf = Hp > StartingHp / 2;
        var droppedBelowQuarter = Hp > StartingHp / 4;

        var newHp = Math.Max(0, Hp - damage);
        var trueDamage = Hp - newHp;
        Hp = newHp;

        droppedBelowHalf = droppedBelowHalf && Hp <= StartingHp / 2;
        droppedBelowQuarter = droppedBelowQuarter && Hp <= StartingHp / 4;
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        msg += $"{Name} took {damage} damage{source}!\n";
        NumHits++;

        // Drain
        if (drainHealRatio != null && attacker != null)
        {
            var heal = (int)(trueDamage * drainHealRatio.Value);
            if (attacker.HeldItem.Get() == "big-root") heal = (int)(heal * 1.3);
            if (Ability() == Impl.Ability.LIQUID_OOZE)
            {
                msg += attacker.Damage(heal, battle, source: $"{Name}'s liquid ooze");
            }
            else
            {
                if (!attacker.HealBlock.Active()) msg += attacker.Heal(heal, source);
            }
        }

        if (Hp == 0)
        {
            msg += Faint(battle, move, attacker);
            if (Ability() == Impl.Ability.AFTERMATH && attacker != null && attacker != this &&
                attacker.Ability() != Impl.Ability.DAMP && move != null &&
                move.MakesContact(attacker))
                msg += attacker.Damage(attacker.StartingHp / 4, battle, source: $"{Name}'s aftermath");
            if (attacker != null && attacker.Ability() == Impl.Ability.MOXIE)
                msg += attacker.AppendAttack(1, attacker, source: "its moxie");
            if (attacker != null && attacker.Ability() == Impl.Ability.BEAST_BOOST)
            {
                var stats = new List<Tuple<int, Func<int, DuelPokemon, Move.Move, string, bool, string>>>
                {
                    new(attacker.GetRawAttack(), attacker.AppendAttack),
                    new(attacker.GetRawDefense(), attacker.AppendDefense),
                    new(attacker.GetRawSpAtk(), attacker.AppendSpAtk),
                    new(attacker.GetRawSpDef(), attacker.AppendSpDef),
                    new(attacker.GetRawSpeed(), attacker.AppendSpeed)
                };
                var appendFunc = stats.OrderByDescending(s => s.Item1).First().Item2;
                msg += appendFunc(1, attacker, null, "its beast boost", false);
            }

            if (attacker != null && Ability() == Impl.Ability.INNARDS_OUT)
                msg += attacker.Damage(previousHp, battle, attacker: this, source: $"{Name}'s innards out");
        }
        else if (move != null && moveType != null)
        {
            if (moveType == ElementType.FIRE && NonVolatileEffect.Freeze())
            {
                NonVolatileEffect.Reset();
                msg += $"{Name} thawed out!\n";
            }

            if (move.Effect is 458 or 500 && NonVolatileEffect.Freeze())
            {
                NonVolatileEffect.Reset();
                msg += $"{Name} thawed out!\n";
            }

            if (Ability() == Impl.Ability.COLOR_CHANGE && !TypeIds.Contains(moveType.Value))
            {
                TypeIds = [moveType.Value];
                var t = moveType.Value.ToString().ToLower();
                msg += $"{Name} changed its color, transforming into a {t} type!\n";
            }

            if (Ability() == Impl.Ability.ANGER_POINT && critical)
                msg += AppendAttack(6, this, source: "its anger point");
            if (Ability() == Impl.Ability.WEAK_ARMOR && move.DamageClass == DamageClass.PHYSICAL && attacker != this)
            {
                msg += AppendDefense(-1, this, source: "its weak armor");
                msg += AppendSpeed(2, this, source: "its weak armor");
            }

            if (Ability() == Impl.Ability.JUSTIFIED && moveType == ElementType.DARK)
                msg += AppendAttack(1, this, source: "justified");
            if (Ability() == Impl.Ability.RATTLED &&
                moveType is ElementType.BUG or ElementType.DARK or ElementType.GHOST)
                msg += AppendSpeed(1, this, source: "its rattled");
            if (Ability() == Impl.Ability.STAMINA) msg += AppendDefense(1, this, source: "its stamina");
            if (Ability() == Impl.Ability.WATER_COMPACTION && moveType == ElementType.WATER)
                msg += AppendDefense(2, this, source: "its water compaction");
            if (Ability() == Impl.Ability.BERSERK && droppedBelowHalf)
                msg += AppendSpAtk(1, this, source: "its berserk");
            if (Ability() == Impl.Ability.ANGER_SHELL && droppedBelowHalf)
            {
                msg += AppendAttack(1, this, source: "its anger shell");
                msg += AppendSpAtk(1, this, source: "its anger shell");
                msg += AppendSpeed(1, this, source: "its anger shell");
                msg += AppendDefense(-1, this, source: "its anger shell");
                msg += AppendSpDef(-1, this, source: "its anger shell");
            }

            if (Ability() == Impl.Ability.STEAM_ENGINE && moveType is ElementType.FIRE or ElementType.WATER)
                msg += AppendSpeed(6, this, source: "its steam engine");
            if (Ability() == Impl.Ability.THERMAL_EXCHANGE && moveType == ElementType.FIRE)
                msg += AppendAttack(1, this, source: "its thermal exchange");
            if (Ability() == Impl.Ability.WIND_RIDER && move.IsWind())
                msg += AppendAttack(1, this, source: "its wind rider");
            if (Ability() == Impl.Ability.COTTON_DOWN && attacker != null)
                msg += attacker.AppendSpeed(-1, this, source: $"{Name}'s cotton down");
            if (Ability() == Impl.Ability.SAND_SPIT) msg += battle.Weather.Set("sandstorm", this);
            if (Ability() == Impl.Ability.SEED_SOWER && battle.Terrain.Item == null)
                msg += battle.Terrain.Set("grassy", this);
            if (Ability() == Impl.Ability.ELECTROMORPHOSIS)
            {
                Charge.SetTurns(2);
                msg += $"{Name} became charged by its electromorphosis!\n";
            }

            if (Ability() == Impl.Ability.WIND_POWER && move.IsWind())
            {
                Charge.SetTurns(2);
                msg += $"{Name} became charged by its wind power!\n";
            }

            if (Ability() == Impl.Ability.TOXIC_DEBRIS && move.DamageClass == DamageClass.PHYSICAL)
                if (attacker != null && attacker != this && attacker.Owner.ToxicSpikes < 2)
                {
                    attacker.Owner.ToxicSpikes++;
                    msg +=
                        $"Toxic spikes were scattered around the feet of {attacker.Owner.Name}'s team because of {Name}'s toxic debris!\n";
                }

            if (_illusionDisplayName != null)
            {
                _name = _illusionName;
                Name = _illusionDisplayName;
                _illusionName = null;
                _illusionDisplayName = null;
                msg += $"{Name}'s illusion broke!\n";
            }

            if (HeldItem.Get() == "air-balloon")
            {
                HeldItem.Remove();
                msg += $"{Name}'s air balloon popped!\n";
            }
        }

        if (move != null)
        {
            LastMoveDamage = new Tuple<int, DamageClass>(Math.Max(1, damage), move.DamageClass);
            if (Bide != null) Bide += damage;
            if (Rage) msg += AppendAttack(1, this, source: "its rage");
            if (attacker != null)
            {
                if (Ability() == Impl.Ability.CURSED_BODY && !attacker.Disable.Active() &&
                    attacker.Moves.Contains(move) && new Random().Next(1, 101) <= 30)
                {
                    if (attacker.Ability() == Impl.Ability.AROMA_VEIL)
                    {
                        msg += $"{attacker.Name}'s aroma veil protects its move from being disabled!\n";
                    }
                    else
                    {
                        attacker.Disable.Set(move, 4);
                        msg += $"{attacker.Name}'s {move.PrettyName} was disabled by {Name}'s cursed body!\n";
                    }
                }

                if (attacker.Ability() == Impl.Ability.MAGICIAN && !attacker.HeldItem.HasItem() && HeldItem.CanRemove())
                {
                    HeldItem.Transfer(attacker.HeldItem);
                    msg += $"{attacker.Name} stole {attacker.HeldItem.Name} using its magician!\n";
                }

                if (attacker.Ability() == Impl.Ability.TOXIC_CHAIN && new Random().Next(1, 101) <= 30)
                    msg += NonVolatileEffect.ApplyStatus("b-poison", battle, attacker,
                        source: $"{attacker.Name}'s toxic chain");
                if (attacker.HeldItem.Get() == "shell-bell")
                    // Shell bell does not trigger when a move is buffed by sheer force.
                    if (attacker.Ability() != Impl.Ability.SHEER_FORCE || move.EffectChance == null)
                        msg += attacker.Heal(damage / 8, "its shell bell");
            }
        }

        // Retreat
        if (droppedBelowHalf && Owner.Party.Count(x => x.Hp > 0) > 1)
        {
            if (Ability() == Impl.Ability.WIMP_OUT)
            {
                msg += $"{Name} wimped out and retreated!\n";
                msg += Remove(battle);
            }
            else if (Ability() == Impl.Ability.EMERGENCY_EXIT)
            {
                msg += $"{Name} used the emergency exit and retreated!\n";
                msg += Remove(battle);
            }
        }

        // Gulp Missile
        if (attacker != null && _name is "Cramorant-gulping" or "Cramorant-gorging" && Owner.HasAlivePokemon())
        {
            var prey = _name == "Cramorant-gorging" ? "pikachu" : "arrokuda";
            if (Form("Cramorant"))
            {
                msg += attacker.Damage(attacker.StartingHp / 4, battle, source: $"{Name} spitting out its {prey}");
                switch (prey)
                {
                    case "arrokuda":
                        msg += attacker.AppendDefense(-1, this, source: $"{Name} spitting out its {prey}");
                        break;
                    case "pikachu":
                        msg += attacker.NonVolatileEffect.ApplyStatus("paralysis", battle, this,
                            source: $"{Name} spitting out its {prey}");
                        break;
                }
            }
        }

        // Berries
        if (HeldItem.ShouldEatBerryDamage(attacker)) msg += HeldItem.EatBerry(attacker: attacker, move: move);

        // Contact
        if (move != null && attacker != null && move.MakesContact(attacker))
        {
            // Affects ATTACKER
            if (attacker.HeldItem.Get() != "protective-pads")
            {
                if (BeakBlast)
                    msg += attacker.NonVolatileEffect.ApplyStatus("burn", battle, attacker,
                        source: $"{Name}'s charging beak blast");
                if (Ability() == Impl.Ability.STATIC)
                    if (new Random().Next(1, 101) <= 30)
                        msg += attacker.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker,
                            source: $"{Name}'s static");

                if (Ability() == Impl.Ability.POISON_POINT)
                    if (new Random().Next(1, 101) <= 30)
                        msg += attacker.NonVolatileEffect.ApplyStatus("poison", battle, attacker,
                            source: $"{Name}'s poison point");

                if (Ability() == Impl.Ability.FLAME_BODY)
                    if (new Random().Next(1, 101) <= 30)
                        msg += attacker.NonVolatileEffect.ApplyStatus("burn", battle, attacker,
                            source: $"{Name}'s flame body");

                if (Ability() == Impl.Ability.ROUGH_SKIN && Owner.HasAlivePokemon())
                    msg += attacker.Damage(attacker.StartingHp / 8, battle, source: $"{Name}'s rough skin");
                if (Ability() == Impl.Ability.IRON_BARBS && Owner.HasAlivePokemon())
                    msg += attacker.Damage(attacker.StartingHp / 8, battle, source: $"{Name}'s iron barbs");
                if (Ability() == Impl.Ability.EFFECT_SPORE)
                    if (attacker.Ability() != Impl.Ability.OVERCOAT && !attacker.TypeIds.Contains(ElementType.GRASS) &&
                        attacker.HeldItem.Get() != "safety-glasses" && new Random().Next(1, 101) <= 30)
                    {
                        string[] statuses = ["paralysis", "poison", "sleep"];
                        var status = statuses[new Random().Next(statuses.Length)];
                        msg += attacker.NonVolatileEffect.ApplyStatus(status, battle, attacker);
                    }

                if (Ability() == Impl.Ability.CUTE_CHARM && new Random().Next(1, 101) <= 30)
                    msg += attacker.Infatuate(this, source: $"{Name}'s cute charm");
                if (Ability() == Impl.Ability.MUMMY && attacker.Ability() != Impl.Ability.MUMMY &&
                    attacker.AbilityChangeable())
                {
                    attacker.AbilityId = (int)Impl.Ability.MUMMY;
                    msg += $"{attacker.Name} gained mummy from {Name}!\n";
                    msg += attacker.SendOutAbility(this, battle);
                }

                if (Ability() == Impl.Ability.LINGERING_AROMA && attacker.Ability() != Impl.Ability.LINGERING_AROMA &&
                    attacker.AbilityChangeable())
                {
                    attacker.AbilityId = (int)Impl.Ability.LINGERING_AROMA;
                    msg += $"{attacker.Name} gained lingering aroma from {Name}!\n";
                    msg += attacker.SendOutAbility(this, battle);
                }

                if (Ability() == Impl.Ability.GOOEY)
                    msg += attacker.AppendSpeed(-1, this, source: $"touching {Name}'s gooey body");
                if (Ability() == Impl.Ability.TANGLING_HAIR)
                    msg += attacker.AppendSpeed(-1, this, source: $"touching {Name}'s tangled hair");
                if (HeldItem.Get() == "rocky-helmet" && Owner.HasAlivePokemon())
                    msg += attacker.Damage(attacker.StartingHp / 6, battle, source: $"{Name}'s rocky helmet");
            }

            // Pickpocket is not included in the protective pads protection
            if (Ability() == Impl.Ability.PICKPOCKET && !HeldItem.HasItem() && attacker.HeldItem.HasItem() &&
                attacker.HeldItem.CanRemove())
            {
                if (attacker.Ability() == Impl.Ability.STICKY_HOLD)
                {
                    msg += $"{attacker.Name}'s sticky hand kept hold of its item!\n";
                }
                else
                {
                    attacker.HeldItem.Transfer(HeldItem);
                    msg += $"{attacker.Name}'s {HeldItem.Name} was stolen!\n";
                }
            }

            // Affects DEFENDER
            if (attacker.Ability() == Impl.Ability.POISON_TOUCH)
                if (new Random().Next(1, 101) <= 30)
                    msg += NonVolatileEffect.ApplyStatus("poison", battle, attacker, move,
                        source: $"{attacker.Name}'s poison touch");

            // Affects BOTH
            if (attacker.HeldItem.Get() != "protective-pads")
                if (Ability() == Impl.Ability.PERISH_BODY && !attacker.PerishSong.Active())
                {
                    attacker.PerishSong.SetTurns(4);
                    PerishSong.SetTurns(4);
                    msg += $"All pokemon will faint after 3 turns from {Name}'s perish body!\n";
                }

            if (Ability() == Impl.Ability.WANDERING_SPIRIT && attacker.AbilityChangeable() &&
                attacker.AbilityGiveable())
            {
                msg += $"{attacker.Name} swapped abilities with {Name} because of {Name}'s wandering spirit!\n";
                (AbilityId, attacker.AbilityId) = (attacker.AbilityId, AbilityId);
                var abilityName = ((Ability)AbilityId).GetPrettyName();
                msg += $"{Name} acquired {abilityName}!\n";
                msg += SendOutAbility(attacker, battle);
                abilityName = ((Ability)attacker.AbilityId).GetPrettyName();
                msg += $"{attacker.Name} acquired {abilityName}!\n";
                msg += attacker.SendOutAbility(this, battle);
            }
        }

        return new Tuple<string, int>(msg, trueDamage);
    }

    /// <summary>
    ///     Heals a pokemon by a certain amount.
    ///     Handles heal-affecting items and abilities, and keeping health within bounds.
    ///     Returns a formatted message.
    /// </summary>
    /// <param name="health">The amount of health to restore.</param>
    /// <param name="source">A description of the healing source.</param>
    /// <returns>A formatted message describing the healing.</returns>
    public string Heal(int health, string source = "")
    {
        var msg = "";
        health = Math.Max(1, health);
        // greater than is used to prevent zygarde's hp incresing form from losing its extra health
        if (Hp >= StartingHp) return msg;
        // safety to prevent errors from "reviving" pokes
        if (Hp == 0) return msg;
        if (HealBlock.Active()) return msg;
        health = Math.Min(StartingHp - Hp, health);
        Hp += health;
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        msg += $"{Name} healed {health} hp{source}!\n";
        return msg;
    }
}