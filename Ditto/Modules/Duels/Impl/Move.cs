namespace Ditto.Modules.Duels.Impl
{
    /// <summary>
    /// Represents an instance of a move.
    /// </summary>
    public class Move
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string PrettyName { get; private set; }
        public int? Power { get; private set; }
        public int PP { get; set; }
        public int StartingPP { get; private set; }
        public int? Accuracy { get; private set; }
        public int Priority { get; private set; }
        public ElementType Type { get; set; }
        public DamageClass DamageClass { get; private set; }
        public int Effect { get; private set; }
        public int? EffectChance { get; private set; }
        public MoveTarget Target { get; private set; }
        public int CritRate { get; private set; }
        public int? MinHits { get; private set; }
        public int? MaxHits { get; private set; }
        public bool Used { get; set; }

        private static readonly int[] SourceArray = new[] { 128, 154, 229, 347, 493 };

        public Move(IDictionary<string, object> moveData)
        {
            Id = Convert.ToInt32(moveData["id"]);
            Name = (string)moveData["identifier"];
            PrettyName = Name[..1].ToUpper() + Name[1..].Replace("-", " ");
            Power = moveData["power"] as int?;
            PP = Convert.ToInt32(moveData["pp"]);
            StartingPP = PP;
            Accuracy = moveData["accuracy"] as int?;
            Priority = Convert.ToInt32(moveData["priority"]);
            Type = (ElementType)Convert.ToInt32(moveData["type_id"]);
            DamageClass = (DamageClass)Convert.ToInt32(moveData["damage_class_id"]);
            Effect = Convert.ToInt32(moveData["effect_id"]);
            EffectChance = moveData["effect_chance"] as int?;
            Target = (MoveTarget)Convert.ToInt32(moveData["target_id"]);
            CritRate = Convert.ToInt32(moveData["crit_rate"]);
            MinHits = moveData["min_hits"] as int?;
            MaxHits = moveData["max_hits"] as int?;
            Used = false;
        }

        /// <summary>
        /// Constructor that takes a MongoDB Move model and converts it to a game Move object
        /// </summary>
        public Move(Database.Models.Mongo.Pokemon.Move moveData)
        {
            Id = moveData.MoveId;
            Name = moveData.Identifier;
            PrettyName = Name[..1].ToUpper() + Name[1..].Replace("-", " ");
            Power = moveData.Power;
            PP = moveData.PP;
            StartingPP = PP;
            Accuracy = moveData.Accuracy;
            Priority = moveData.Priority;
            Type = (ElementType)moveData.TypeId;
            DamageClass = (DamageClass)moveData.DamageClassId;
            Effect = moveData.EffectId;
            EffectChance = moveData.EffectChance;
            Target = (MoveTarget)moveData.TargetId;
            CritRate = moveData.CritRate;
            MinHits = moveData.MinHits;
            MaxHits = moveData.MaxHits;
            Used = false;
        }

        /// <summary>
        /// Copy constructor that can also override the Type property
        /// </summary>
        public Move(Move other)
        {
            Id = other.Id;
            Name = other.Name;
            PrettyName = other.PrettyName;
            Power = other.Power;
            PP = other.PP;
            StartingPP = other.StartingPP;
            Accuracy = other.Accuracy;
            Priority = other.Priority;
            Type = other.Type;
            DamageClass = other.DamageClass;
            Effect = other.Effect;
            EffectChance = other.EffectChance;
            Target = other.Target;
            CritRate = other.CritRate;
            MinHits = other.MinHits;
            MaxHits = other.MaxHits;
            Used = other.Used;
        }


        /// <summary>
        /// Sets up anything this move needs to do prior to normal move execution.
        /// </summary>
        /// <returns>A formatted message.</returns>
        public string Setup(DuelPokemon attacker, DuelPokemon defender, Battle battle)
        {
            var msg = "";
            if (Effect == 129 && (defender.Owner.SelectedAction.IsSwitch ||
                                  (defender.Owner.SelectedAction as Trainer.MoveAction)?.Move.Effect is 128 or 154 or 229 or 347 or 493))
            {
                msg += Use(attacker, defender, battle);
            }

            switch (Effect)
            {
                case 171:
                    msg += $"{attacker.Name} is focusing on its attack!\n";
                    break;
                case 404:
                    attacker.BeakBlast = true;
                    break;
            }

            return msg;
        }

        /// <summary>
        /// Uses this move as attacker on defender.
        /// </summary>
        /// <returns>A string of formatted results of the move.</returns>
        public string Use(DuelPokemon attacker, DuelPokemon defender, Battle battle, bool usePP = true,
            bool overrideSleep = false, bool bounced = false)
        {
            // This handles an edge case for moves that cause the target to swap out
            if (attacker.HasMoved && usePP)
            {
                return "";
            }

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
                    if (Effect == 28)
                    {
                        attacker.LockedMove = null;
                    }

                    return msg;
                }
            }

            if (attacker.NonVolatileEffect.Paralysis() && new Random().Next(0, 4) == 0)
            {
                msg += $"{attacker.Name} is paralyzed! It can't move!\n";
                if (Effect == 28)
                {
                    attacker.LockedMove = null;
                }

                return msg;
            }

            if (attacker.Infatuated == defender && new Random().Next(0, 2) == 0)
            {
                msg += $"{attacker.Name} is in love with {defender.Name} and can't bare to hurt them!\n";
                if (Effect == 28)
                {
                    attacker.LockedMove = null;
                }

                return msg;
            }

            if (attacker.Flinched)
            {
                msg += $"{attacker.Name} flinched! It can't move!\n";
                if (Effect == 28)
                {
                    attacker.LockedMove = null;
                }

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
                    if (Effect == 28)
                    {
                        attacker.LockedMove = null;
                    }

                    return msg;
                }
            }

            if (attacker.Confusion.NextTurn())
            {
                msg += $"{attacker.Name} is no longer confused!\n";
            }

            if (attacker.Confusion.Active() && new Random().Next(0, 3) == 0)
            {
                msg += $"{attacker.Name} hurt itself in its confusion!\n";
                var (msgadd, numhits) = Confusion().Attack(attacker, attacker, battle);
                msg += msgadd;
                if (Effect == 28)
                {
                    attacker.LockedMove = null;
                }

                return msg;
            }

            if (attacker.Ability() == Ability.TRUANT && attacker.TruantTurn % 2 == 1)
            {
                msg += $"{attacker.Name} is loafing around!\n";
                if (Effect == 28)
                {
                    attacker.LockedMove = null;
                }

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
                if (defender.Ability(attacker: attacker, move: this) == Ability.PRESSURE && PP != 0)
                {
                    if (TargetsOpponent() || new[] { 113, 193, 196, 250, 267 }.Contains(Effect))
                    {
                        PP -= 1;
                    }
                }

                if (PP == 0)
                {
                    msg += "It ran out of PP!\n";
                }
            }

            // User is using a choice item and had not used a move yet, set that as their only move.
            if (attacker.ChoiceMove == null && usePP)
            {
                if (attacker.HeldItem == "choice-scarf" || attacker.HeldItem == "choice-band" ||
                    attacker.HeldItem == "choice-specs")
                {
                    attacker.ChoiceMove = this;
                }
                else if (attacker.Ability() == Ability.GORILLA_TACTICS)
                {
                    attacker.ChoiceMove = this;
                }
            }

            // Stance change
            if (attacker.Ability() == Ability.STANCE_CHANGE)
            {
                if (attacker.Name == "Aegislash" &&
                    DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                {
                    if (attacker.Form("Aegislash-blade"))
                    {
                        msg += $"{attacker.Name} draws its blade!\n";
                    }
                }

                if (attacker.Name == "Aegislash-blade" && Effect == 356)
                {
                    if (attacker.Form("Aegislash"))
                    {
                        msg += $"{attacker.Name} readies its shield!\n";
                    }
                }
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
                msg += Use(defender, attacker, battle, usePP: false);
                return msg;
            }

            // Check Fail
            if (!CheckExecutable(attacker, defender, battle))
            {
                msg += "But it failed!\n";
                if (Effect is 28 or 118)
                {
                    attacker.LockedMove = null;
                }

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
                        {
                            attacker.LockedMove = new LockedMove(this, 2);
                        }
                        else
                        {
                            // If this move isn't charging, the spatk increase has to happen manually
                            msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                        }

                        break;
                    }
                }

                if (new[] { 40, 76, 81, 146, 156, 256, 257, 264, 273, 332, 333, 366, 451 }.Contains(Effect))
                {
                    attacker.LockedMove = new LockedMove(this, 2);
                }

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
                            msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                            break;
                        case 451 or 502:
                            msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
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
                                    {
                                        msg += $"{attacker.Name} gulped up an arrokuda!\n";
                                    }
                                }
                                else
                                {
                                    if (attacker.Form("Cramorant-gorging"))
                                    {
                                        msg += $"{attacker.Name} gulped up a pikachu!\n";
                                    }
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
                (defender.Ability(attacker: attacker, move: this) == Ability.MAGIC_BOUNCE || defender.MagicCoat) &&
                !bounced)
            {
                msg += $"It was reflected by {defender.Name}'s magic bounce!\n";
                var hm = defender.HasMoved;
                msg += Use(defender, attacker, battle, usePP: false, bounced: true);
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
            {
                switch (currentType)
                {
                    // Heal
                    case ElementType.ELECTRIC when
                        defender.Ability(attacker: attacker, move: this) == Ability.VOLT_ABSORB:
                        msg += $"{defender.Name}'s volt absorb absorbed the move!\n";
                        msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                        return msg;
                    case ElementType.WATER when
                        defender.Ability(attacker: attacker, move: this) == Ability.WATER_ABSORB:
                        msg += $"{defender.Name}'s water absorb absorbed the move!\n";
                        msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                        return msg;
                    case ElementType.WATER when
                        defender.Ability(attacker: attacker, move: this) == Ability.DRY_SKIN:
                        msg += $"{defender.Name}'s dry skin absorbed the move!\n";
                        msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                        return msg;
                    case ElementType.GROUND when
                        defender.Ability(attacker: attacker, move: this) == Ability.EARTH_EATER:
                        msg += $"{defender.Name}'s earth eater absorbed the move!\n";
                        msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                        return msg;
                    // Stat stage changes
                    case ElementType.ELECTRIC when
                        defender.Ability(attacker: attacker, move: this) == Ability.LIGHTNING_ROD:
                        msg += $"{defender.Name}'s lightning rod absorbed the move!\n";
                        msg += defender.AppendSpAtk(1, attacker: defender, move: this);
                        return msg;
                    case ElementType.ELECTRIC when
                        defender.Ability(attacker: attacker, move: this) == Ability.MOTOR_DRIVE:
                        msg += $"{defender.Name}'s motor drive absorbed the move!\n";
                        msg += defender.AppendSpeed(1, attacker: defender, move: this);
                        return msg;
                    case ElementType.WATER when
                        defender.Ability(attacker: attacker, move: this) == Ability.STORM_DRAIN:
                        msg += $"{defender.Name}'s storm drain absorbed the move!\n";
                        msg += defender.AppendSpAtk(1, attacker: defender, move: this);
                        return msg;
                    case ElementType.GRASS when
                        defender.Ability(attacker: attacker, move: this) == Ability.SAP_SIPPER:
                        msg += $"{defender.Name}'s sap sipper absorbed the move!\n";
                        msg += defender.AppendAttack(1, attacker: defender, move: this);
                        return msg;
                    case ElementType.FIRE when
                        defender.Ability(attacker: attacker, move: this) == Ability.WELL_BAKED_BODY:
                        msg += $"{defender.Name}'s well baked body absorbed the move!\n";
                        msg += defender.AppendDefense(2, attacker: defender, move: this);
                        return msg;
                    // Other
                    case ElementType.FIRE when
                        defender.Ability(attacker: attacker, move: this) == Ability.FLASH_FIRE:
                        defender.FlashFire = true;
                        msg += $"{defender.Name} used its flash fire to buff its fire type moves!\n";
                        return msg;
                }
            }

            // Stat stage from type items
            if (defender.Substitute == 0)
            {
                switch (currentType)
                {
                    case ElementType.WATER when defender.HeldItem == "absorb-bulb":
                        msg += defender.AppendSpAtk(1, attacker: defender, move: this, source: "its absorb bulb");
                        defender.HeldItem.Use();
                        break;
                    case ElementType.ELECTRIC when defender.HeldItem == "cell-battery":
                        msg += defender.AppendAttack(1, attacker: defender, move: this, source: "its cell battery");
                        defender.HeldItem.Use();
                        break;
                }

                switch (currentType)
                {
                    case ElementType.WATER when defender.HeldItem == "luminous-moss":
                        msg += defender.AppendSpDef(1, attacker: defender, move: this, source: "its luminous moss");
                        defender.HeldItem.Use();
                        break;
                    case ElementType.ICE when defender.HeldItem == "snowball":
                        msg += defender.AppendAttack(1, attacker: defender, move: this, source: "its snowball");
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
                        msg += move.Use(attacker, defender, battle, usePP: false, overrideSleep: true);
                        return msg;
                    }
                    else
                    {
                        msg += "But it failed!\n";
                        return msg;
                    }
                }
                // Mirror Move/Copy Cat
                case 10 or 243:
                {
                    if (defender.LastMove != null)
                    {
                        msg += defender.LastMove.Use(attacker, defender, battle, usePP: false);
                        return msg;
                    }
                    else
                    {
                        msg += "But it failed!\n";
                        return msg;
                    }
                }
                // Me First
                case 242:
                {
                    if (defender.Owner.SelectedAction is Trainer.MoveAction move)
                    {
                        msg += move.Move.Use(attacker, defender, battle, usePP: false);
                        return msg;
                    }
                    else
                    {
                        msg += "But it failed!\n";
                        return msg;
                    }
                }
                // Assist
                case 181:
                {
                    var assistMove = attacker.GetAssistMove();
                    if (assistMove != null)
                    {
                        msg += assistMove.Use(attacker, defender, battle, usePP: false);
                        return msg;
                    }
                    else
                    {
                        msg += "But it failed!\n";
                        return msg;
                    }
                }
                // Spectral Thief
                case 410:
                {
                    if (defender.AttackStage > 0)
                    {
                        var stage = defender.AttackStage;
                        defender.AttackStage = 0;
                        msg += $"{defender.Name}'s attack stage was reset!\n";
                        msg += attacker.AppendAttack(stage, attacker: attacker, move: this);
                    }

                    if (defender.DefenseStage > 0)
                    {
                        var stage = defender.DefenseStage;
                        defender.DefenseStage = 0;
                        msg += $"{defender.Name}'s defense stage was reset!\n";
                        msg += attacker.AppendDefense(stage, attacker: attacker, move: this);
                    }

                    if (defender.SpAtkStage > 0)
                    {
                        var stage = defender.SpAtkStage;
                        defender.SpAtkStage = 0;
                        msg += $"{defender.Name}'s special attack stage was reset!\n";
                        msg += attacker.AppendSpAtk(stage, attacker: attacker, move: this);
                    }

                    if (defender.SpDefStage > 0)
                    {
                        var stage = defender.SpDefStage;
                        defender.SpDefStage = 0;
                        msg += $"{defender.Name}'s special defense stage was reset!\n";
                        msg += attacker.AppendSpDef(stage, attacker: attacker, move: this);
                    }

                    if (defender.SpeedStage > 0)
                    {
                        var stage = defender.SpeedStage;
                        defender.SpeedStage = 0;
                        msg += $"{defender.Name}'s speed stage was reset!\n";
                        msg += attacker.AppendSpeed(stage, attacker: attacker, move: this);
                    }

                    if (defender.EvasionStage > 0)
                    {
                        var stage = defender.EvasionStage;
                        defender.EvasionStage = 0;
                        msg += $"{defender.Name}'s evasion stage was reset!\n";
                        msg += attacker.AppendEvasion(stage, attacker: attacker, move: this);
                    }

                    if (defender.AccuracyStage > 0)
                    {
                        var stage = defender.AccuracyStage;
                        defender.AccuracyStage = 0;
                        msg += $"{defender.Name}'s accuracy stage was reset!\n";
                        msg += attacker.AppendAccuracy(stage, attacker: attacker, move: this);
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
                        {
                            msg += "It had no effect!\n";
                        }
                        else
                        {
                            msg += defender.Heal(defender.StartingHp / 4, source: $"{attacker.Name}'s present");
                        }

                        return msg;
                    }

                    var presentPower = action == 2 ? 40 : action == 3 ? 80 : 120;
                    var presentMove = Present(presentPower);
                    var (msgadd, hits) = presentMove.Attack(attacker, defender, battle);
                    msg += msgadd;
                    return msg;
                }
                // Incinerate
                case 315 when defender.HeldItem.IsBerry(onlyActive: false):
                {
                    if (defender.Ability(attacker: attacker, move: this) == Ability.STICKY_HOLD)
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
                {
                    if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                    {
                        (var msgadd, i) = Attack(attacker, defender, battle);
                        msg += msgadd;
                    }
                }
            }
            else switch (Effect)
            {
                // Turn 3 hit moves
                case 27:
                {
                    if (attacker.LockedMove.Turn == 2)
                    {
                        msg += defender.Damage(attacker.Bide.Value * 2, battle, move: this, moveType: currentType,
                            attacker: attacker);
                        attacker.Bide = null;
                        i = 1;
                    }

                    break;
                }
                // Counter attack moves
                case 228:
                    msg += defender.Damage((int)(1.5 * attacker.LastMoveDamage.Item1), battle, move: this,
                        moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 145:
                    msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, move: this, moveType: currentType,
                        attacker: attacker);
                    i = 1;
                    break;
                case 90:
                    msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, move: this, moveType: currentType,
                        attacker: attacker);
                    i = 1;
                    break;
                // Static-damage moves
                case 41:
                    msg += defender.Damage(defender.Hp / 2, battle, move: this, moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 42:
                    msg += defender.Damage(40, battle, move: this, moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 88:
                    msg += defender.Damage(attacker.Level, battle, move: this, moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 89:
                {
                    // 0.5-1.5, increments of .1
                    var scale = new Random().Next(0, 11) / 10.0 + 0.5;
                    msg += defender.Damage((int)(attacker.Level * scale), battle, move: this, moveType: currentType,
                        attacker: attacker);
                    i = 1;
                    break;
                }
                case 131:
                    msg += defender.Damage(20, battle, move: this, moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 190:
                    msg += defender.Damage(Math.Max(0, defender.Hp - attacker.Hp), battle, move: this,
                        moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 39:
                    msg += defender.Damage(defender.Hp, battle, move: this, moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 321:
                    msg += defender.Damage(attacker.Hp, battle, move: this, moveType: currentType, attacker: attacker);
                    i = 1;
                    break;
                case 413:
                    msg += defender.Damage(3 * (defender.Hp / 4), battle, move: this, moveType: currentType,
                        attacker: attacker);
                    i = 1;
                    break;
                // Beat up, a stupid move
                case 155:
                {
                    foreach (var poke in attacker.Owner.Party)
                    {
                        if (defender.Hp == 0)
                        {
                            break;
                        }

                        if (poke.Hp == 0)
                        {
                            continue;
                        }

                        if (poke == attacker)
                        {
                            var (msgadd, nh) = Attack(attacker, defender, battle);
                            msg += msgadd;
                            i += nh;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(poke.NonVolatileEffect.Current))
                            {
                                continue;
                            }

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
                    msg += attacker.AppendDefense(-attacker.Stockpile, attacker: attacker, move: this);
                    msg += attacker.AppendSpDef(-attacker.Stockpile, attacker: attacker, move: this);
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

                    msg += attacker.Heal(attacker.StartingHp / healFactor, source: "stockpiled energy");
                    msg += attacker.AppendDefense(-attacker.Stockpile, attacker: attacker, move: this);
                    msg += attacker.AppendSpDef(-attacker.Stockpile, attacker: attacker, move: this);
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
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 168:
                case 429 when defender.StatIncreased:
                    msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker: attacker, move: this);
                    break;
                case 37 when effectChance.HasValue:
                {
                    var statuses = new[] { "burn", "freeze", "paralysis" };
                    var status = statuses[new Random().Next(statuses.Length)];
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 464 when effectChance.HasValue:
                {
                    string[] statuses = new[] { "poison", "paralysis", "sleep" };
                    var status = statuses[new Random().Next(statuses.Length)];
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 6 or 261 or 275 or 380 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus("freeze", battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 7 or 153 or 263 or 264 or 276 or 332 or 372 or 396 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 68:
                    msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker, move: this);
                    break;
                case 3 or 78 or 210 or 447 or 461 when
                    effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 67:
                case 390:
                case 486:
                    msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker: attacker, move: this);
                    break;
                case 203 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 34:
                    msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker: attacker, move: this);
                    break;
                case 2 when Id == 464 && attacker.Name != "Darkrai":
                    msg += $"{attacker.Name} can't use the move!\n";
                    break;
                case 2:
                    msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this);
                    break;
                case 330 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this);
                    }

                    break;
                }
                case 38:
                {
                    msg += attacker.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this, turns: 3,
                        force: true);
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
                    msg += defender.Confuse(attacker: attacker, move: this);
                    break;
                // This checks if attacker.LockedMove is not null as locked_move is cleared if the poke dies to rocky helmet or similar items
                case 28 when attacker.LockedMove != null && attacker.LockedMove.IsLastTurn():
                    msg += attacker.Confuse();
                    break;
                case 77 or 268 or 334 or 478 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.Confuse(attacker: attacker, move: this);
                    }

                    break;
                }
                case 497 when defender.StatIncreased:
                    msg += defender.Confuse(attacker: attacker, move: this);
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
            {
                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
            }

            if (Effect is 12 or 157 or 161 or 207 or 209 or 323 or 367 or 414 or 427 or 467 or 468 or 472)
            {
                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
            }

            if (Effect is 14 or 212 or 291 or 328 or 392 or 414 or 427 or 472)
            {
                msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
            }

            if (Effect is 161 or 175 or 207 or 212 or 291 or 367 or 414 or 427 or 472)
            {
                msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
            }

            switch (Effect)
            {
                case 130 or 213 or 291 or 296 or 414 or 427 or 442 or 468 or 469 or 487:
                    msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                    break;
                case 17 or 467:
                    msg += attacker.AppendEvasion(1, attacker: attacker, move: this);
                    break;
                case 278 or 323:
                    msg += attacker.AppendAccuracy(1, attacker: attacker, move: this);
                    break;
                case 139 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 140 or 375 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 277 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 433 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 167:
                    msg += defender.AppendSpAtk(1, attacker: attacker, move: this);
                    break;
                // +2
                case 51 or 309:
                    msg += attacker.AppendAttack(2, attacker: attacker, move: this);
                    break;
                case 52 or 453:
                    msg += attacker.AppendDefense(2, attacker: attacker, move: this);
                    break;
            }

            if (Effect is 53 or 285 or 309 or 313 or 366)
            {
                msg += attacker.AppendSpeed(2, attacker: attacker, move: this);
            }

            if (Effect is 54 or 309 or 366)
            {
                msg += attacker.AppendSpAtk(2, attacker: attacker, move: this);
            }

            switch (Effect)
            {
                case 55 or 366:
                    msg += attacker.AppendSpDef(2, attacker: attacker, move: this);
                    break;
                case 109:
                    msg += attacker.AppendEvasion(2, attacker: attacker, move: this);
                    break;
                case 119 or 432 or 483:
                    msg += defender.AppendAttack(2, attacker: attacker, move: this);
                    break;
            }

            switch (Effect)
            {
                case 432:
                    msg += defender.AppendSpAtk(2, attacker: attacker, move: this);
                    break;
                case 359 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += attacker.AppendDefense(2, attacker: attacker, move: this);
                    }

                    break;
                }
                // -1
                case 19 or 206 or 344 or 347 or 357 or 365 or 388 or 412:
                    msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                    break;
            }

            switch (Effect)
            {
                case 20 or 206:
                    msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                    break;
                case 344 or 347 or 358 or 412:
                    msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                    break;
                case 428:
                    msg += defender.AppendSpDef(-1, attacker: attacker, move: this);
                    break;
                case 331 or 390:
                    msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                    break;
                case 24:
                    msg += defender.AppendAccuracy(-1, attacker: attacker, move: this);
                    break;
                case 25 or 259:
                    msg += defender.AppendEvasion(-1, attacker: attacker, move: this);
                    break;
                case 69 or 396 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 70 or 397 or 435 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 475:
                {
                    // This one has two different chance percents, one has to be hardcoded
                    if (new Random().Next(1, 101) <= 50)
                    {
                        msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 21 or 71 or 357 or 477 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 72 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 73 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.AppendSpDef(-1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 74 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.AppendAccuracy(-1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 183:
                    msg += attacker.AppendAttack(-1, attacker: attacker, move: this);
                    break;
            }

            switch (Effect)
            {
                case 183 or 230 or 309 or 335 or 405 or 438 or 442:
                    msg += attacker.AppendDefense(-1, attacker: attacker, move: this);
                    break;
                case 480:
                    msg += attacker.AppendSpAtk(-1, attacker: attacker, move: this);
                    break;
            }

            if (Effect is 230 or 309 or 335)
            {
                msg += attacker.AppendSpDef(-1, attacker: attacker, move: this);
            }

            switch (Effect)
            {
                case 219 or 335:
                    msg += attacker.AppendSpeed(-1, attacker: attacker, move: this);
                    break;
                // -2
                case 59 or 169:
                    msg += defender.AppendAttack(-2, attacker: attacker, move: this);
                    break;
                case 60 or 483:
                    msg += defender.AppendDefense(-2, attacker: attacker, move: this);
                    break;
                case 61:
                    msg += defender.AppendSpeed(-2, attacker: attacker, move: this);
                    break;
            }

            switch (Effect)
            {
                case 62 or 169 or 266:
                    msg += defender.AppendSpAtk(-2, attacker: attacker, move: this);
                    break;
                case 63:
                    msg += defender.AppendSpDef(-2, attacker: attacker, move: this);
                    break;
                case 272 or 297 when effectChance.HasValue:
                {
                    if (new Random().Next(1, 101) <= effectChance)
                    {
                        msg += defender.AppendSpDef(-2, attacker: attacker, move: this);
                    }

                    break;
                }
                case 205:
                    msg += attacker.AppendSpAtk(-2, attacker: attacker, move: this);
                    break;
                case 479:
                    msg += attacker.AppendSpeed(-2, attacker: attacker, move: this);
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
                        msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                        msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                        msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                        msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                        msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 143:
                    msg += attacker.Damage(attacker.StartingHp / 2, battle);
                    msg += attacker.AppendAttack(12, attacker: attacker, move: this);
                    break;
                case 317:
                {
                    var amount = 1;
                    if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                    {
                        amount = 2;
                    }

                    msg += attacker.AppendAttack(amount, attacker: attacker, move: this);
                    msg += attacker.AppendSpAtk(amount, attacker: attacker, move: this);
                    break;
                }
                case 364 when defender.NonVolatileEffect.Poison():
                    msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                    msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                    msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                    break;
                case 329:
                    msg += attacker.AppendDefense(3, attacker: attacker, move: this);
                    break;
                case 322:
                    msg += attacker.AppendSpAtk(3, attacker: attacker, move: this);
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
                        msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                        msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                    }
                    else
                    {
                        msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                        msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                    }

                    break;
                }
                case 485:
                    msg += attacker.Damage(attacker.StartingHp / 2, battle);
                    msg += attacker.AppendAttack(2, attacker: attacker, move: this);
                    msg += attacker.AppendSpAtk(2, attacker: attacker, move: this);
                    msg += attacker.AppendSpeed(2, attacker: attacker, move: this);
                    break;
            }

            // Flinch
            if (!defender.HasMoved)
            {
                for (var hit = 0; hit < i; hit++)
                {
                    if (defender.Flinched)
                    {
                        break;
                    }

                    if (new[] { 32, 76, 93, 147, 151, 159, 274, 275, 276, 425, 475, 501 }.Contains(Effect) &&
                        effectChance.HasValue)
                    {
                        if (new Random().Next(1, 101) <= effectChance)
                        {
                            msg += defender.Flinch(move: this, attacker: attacker);
                        }
                    }
                    else if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                    {
                        if (attacker.Ability() == Ability.STENCH)
                        {
                            if (new Random().Next(1, 101) <= 10)
                            {
                                msg += defender.Flinch(move: this, attacker: attacker, source: "its stench");
                            }
                        }
                        else if (attacker.HeldItem == "kings-rock")
                        {
                            if (new Random().Next(1, 101) <= 10)
                            {
                                msg += defender.Flinch(move: this, attacker: attacker, source: "its kings rock");
                            }
                        }
                        else if (attacker.HeldItem == "razor-fang")
                        {
                            if (new Random().Next(1, 101) <= 10)
                            {
                                msg += defender.Flinch(move: this, attacker: attacker, source: "its razor fang");
                            }
                        }
                    }
                }
            }

            switch (Effect)
            {
                // Move locking
                case 87 when defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL:
                    msg += $"{defender.Name}'s aroma veil protects its move from being disabled!\n";
                    break;
                case 87:
                    defender.Disable.Set(defender.LastMove, new Random().Next(4, 8));
                    msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was disabled!\n";
                    break;
                case 176 when defender.Ability(attacker: attacker, move: this) == Ability.OBLIVIOUS:
                    msg += $"{defender.Name} is too oblivious to be taunted!\n";
                    break;
                case 176 when defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL:
                    msg += $"{defender.Name}'s aroma veil protects it from being taunted!\n";
                    break;
                case 176:
                {
                    if (defender.HasMoved)
                    {
                        defender.Taunt.SetTurns(4);
                    }
                    else
                    {
                        defender.Taunt.SetTurns(3);
                    }

                    msg += $"{defender.Name} is being taunted!\n";
                    break;
                }
                case 91 when defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL:
                    msg += $"{defender.Name}'s aroma veil protects it from being encored!\n";
                    break;
                case 91:
                {
                    defender.Encore.Set(defender.LastMove, 4);
                    if (!defender.HasMoved)
                    {
                        defender.Owner.SelectedAction = new Trainer.MoveAction(defender.LastMove);
                    }

                    msg += $"{defender.Name} is giving an encore!\n";
                    break;
                }
                case 166 when defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL:
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
                case 237 when defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL:
                    msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                    break;
                case 237:
                    defender.HealBlock.SetTurns(5);
                    msg += $"{defender.Name} is blocked from healing!\n";
                    break;
                case 496 when defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL:
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
            {
                msg += attacker.Damage(attacker.StartingHp / 10, battle, source: "its life orb");
            }

            // Swap outs
            // A poke is force-swapped out before activating red-card
            if (Effect is 29 or 314)
            {
                var swaps = defender.Owner.ValidSwaps(attacker, battle, checkTrap: false);
                if (swaps.Count == 0)
                {
                    // Do nothing
                }
                else if (defender.Ability(attacker: attacker, move: this) == Ability.SUCTION_CUPS)
                {
                    msg += $"{defender.Name}'s suction cups kept it in place!\n";
                }
                else if (defender.Ability(attacker: attacker, move: this) == Ability.GUARD_DOG)
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
                    defender.Owner.SwitchPoke(idx, midTurn: true);
                    msg += defender.Owner.CurrentPokemon.SendOut(attacker, battle);
                    // Safety in case the poke dies on send out.
                    if (defender.Owner.CurrentPokemon != null)
                    {
                        defender.Owner.CurrentPokemon.HasMoved = true;
                    }
                }
            }
            // A red-card forces the attacker to swap to a random poke, even if they used a switch out move
            else if (defender.HeldItem == "red-card" && defender.Hp > 0 && DamageClass != DamageClass.STATUS)
            {
                var swaps = attacker.Owner.ValidSwaps(defender, battle, checkTrap: false);
                if (swaps.Count == 0)
                {
                    // Do nothing
                }
                else if (attacker.Ability(attacker: defender, move: this) == Ability.SUCTION_CUPS)
                {
                    msg += $"{attacker.Name}'s suction cups kept it in place from {defender.Name}'s red card!\n";
                    defender.HeldItem.Use();
                }
                else if (attacker.Ability(attacker: defender, move: this) == Ability.GUARD_DOG)
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
                    attacker.Owner.SwitchPoke(idx, midTurn: true);
                    msg += attacker.Owner.CurrentPokemon.SendOut(defender, battle);
                    // Safety in case the poke dies on send out.
                    if (attacker.Owner.CurrentPokemon != null)
                    {
                        attacker.Owner.CurrentPokemon.HasMoved = true;
                    }
                }
            }
            else if (new[] { 128, 154, 229, 347 }.Contains(Effect))
            {
                var swaps = attacker.Owner.ValidSwaps(defender, battle, checkTrap: false);
                if (swaps.Count > 0)
                {
                    msg += $"{attacker.Name} went back!\n";
                    if (Effect == 128)
                    {
                        attacker.Owner.BatonPass = new BatonPass(attacker);
                    }

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
            if (new[] { 169, 221, 271, 321 }.Contains(Effect))
            {
                msg += attacker.Faint(battle);
            }

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
                    foreach (var poke in attacker.Owner.Party)
                    {
                        poke.NonVolatileEffect.Reset();
                    }

                    msg += $"A bell chimed, and all of {attacker.Owner.Name}'s pokemon had status conditions removed!\n";
                    break;
                }
                // Psycho Shift
                case 235:
                {
                    var transferedStatus = attacker.NonVolatileEffect.Current;
                    msg += defender.NonVolatileEffect.ApplyStatus(transferedStatus, battle, attacker: attacker, move: this);
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
                    {
                        msg += $"{attacker.Name} is already under the effect of perish song!\n";
                    }
                    else
                    {
                        attacker.PerishSong.SetTurns(4);
                    }

                    if (defender.PerishSong.Active())
                    {
                        msg += $"{defender.Name} is already under the effect of perish song!\n";
                    }
                    else if (defender.Ability(attacker: attacker, move: this) == Ability.SOUNDPROOF)
                    {
                        msg += $"{defender.Name}'s soundproof protects it from hearing the song!\n";
                    }
                    else
                    {
                        defender.PerishSong.SetTurns(4);
                    }

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
                    if (!Enum.IsDefined(typeof(ElementType), t))
                    {
                        t = ElementType.NORMAL;
                    }

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
                        msg += Use(defender, attacker, battle, usePP: false, bounced: true);
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
                    {
                        switch (currentType)
                        {
                            // Heal
                            case ElementType.ELECTRIC when
                                defender.Ability(attacker: attacker, move: this) == Ability.VOLT_ABSORB:
                                msg += $"{defender.Name}'s volt absorb absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            case ElementType.WATER when
                                defender.Ability(attacker: attacker, move: this) == Ability.WATER_ABSORB:
                                msg += $"{defender.Name}'s water absorb absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            case ElementType.WATER when
                                defender.Ability(attacker: attacker, move: this) == Ability.DRY_SKIN:
                                msg += $"{defender.Name}'s dry skin absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            case ElementType.GROUND when
                                defender.Ability(attacker: attacker, move: this) == Ability.EARTH_EATER:
                                msg += $"{defender.Name}'s earth eater absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            // Stat stage changes
                            case ElementType.ELECTRIC when
                                defender.Ability(attacker: attacker, move: this) == Ability.LIGHTNING_ROD:
                                msg += $"{defender.Name}'s lightning rod absorbed the move!\n";
                                msg += defender.AppendSpAtk(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.ELECTRIC when
                                defender.Ability(attacker: attacker, move: this) == Ability.MOTOR_DRIVE:
                                msg += $"{defender.Name}'s motor drive absorbed the move!\n";
                                msg += defender.AppendSpeed(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.WATER when
                                defender.Ability(attacker: attacker, move: this) == Ability.STORM_DRAIN:
                                msg += $"{defender.Name}'s storm drain absorbed the move!\n";
                                msg += defender.AppendSpAtk(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.GRASS when
                                defender.Ability(attacker: attacker, move: this) == Ability.SAP_SIPPER:
                                msg += $"{defender.Name}'s sap sipper absorbed the move!\n";
                                msg += defender.AppendAttack(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.FIRE when
                                defender.Ability(attacker: attacker, move: this) == Ability.WELL_BAKED_BODY:
                                msg += $"{defender.Name}'s well baked body absorbed the move!\n";
                                msg += defender.AppendDefense(2, attacker: defender, move: this);
                                return msg;
                            // Other
                            case ElementType.FIRE when
                                defender.Ability(attacker: attacker, move: this) == Ability.FLASH_FIRE:
                                defender.FlashFire = true;
                                msg += $"{defender.Name} used its flash fire to buff its fire type moves!\n";
                                return msg;
                        }
                    }

                    // Stat stage from type items
                    if (defender.Substitute == 0)
                    {
                        switch (currentType)
                        {
                            case ElementType.WATER when defender.HeldItem == "absorb-bulb":
                                msg += defender.AppendSpAtk(1, attacker: defender, move: this, source: "its absorb bulb");
                                defender.HeldItem.Use();
                                break;
                            case ElementType.ELECTRIC when defender.HeldItem == "cell-battery":
                                msg += defender.AppendAttack(1, attacker: defender, move: this, source: "its cell battery");
                                defender.HeldItem.Use();
                                break;
                        }

                        switch (currentType)
                        {
                            case ElementType.WATER when defender.HeldItem == "luminous-moss":
                                msg += defender.AppendSpDef(1, attacker: defender, move: this, source: "its luminous moss");
                                defender.HeldItem.Use();
                                break;
                            case ElementType.ICE when defender.HeldItem == "snowball":
                                msg += defender.AppendAttack(1, attacker: defender, move: this, source: "its snowball");
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
                                msg += move.Use(attacker, defender, battle, usePP: false, overrideSleep: true);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Mirror Move/Copy Cat
                        case 10 or 243:
                        {
                            if (defender.LastMove != null)
                            {
                                msg += defender.LastMove.Use(attacker, defender, battle, usePP: false);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Me First
                        case 242:
                        {
                            if (defender.Owner.SelectedAction is Trainer.MoveAction move)
                            {
                                msg += move.Move.Use(attacker, defender, battle, usePP: false);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Assist
                        case 181:
                        {
                            var assistMove = attacker.GetAssistMove();
                            if (assistMove != null)
                            {
                                msg += assistMove.Use(attacker, defender, battle, usePP: false);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Spectral Thief
                        case 410:
                        {
                            if (defender.AttackStage > 0)
                            {
                                var stage = defender.AttackStage;
                                defender.AttackStage = 0;
                                msg += $"{defender.Name}'s attack stage was reset!\n";
                                msg += attacker.AppendAttack(stage, attacker: attacker, move: this);
                            }

                            if (defender.DefenseStage > 0)
                            {
                                var stage = defender.DefenseStage;
                                defender.DefenseStage = 0;
                                msg += $"{defender.Name}'s defense stage was reset!\n";
                                msg += attacker.AppendDefense(stage, attacker: attacker, move: this);
                            }

                            if (defender.SpAtkStage > 0)
                            {
                                var stage = defender.SpAtkStage;
                                defender.SpAtkStage = 0;
                                msg += $"{defender.Name}'s special attack stage was reset!\n";
                                msg += attacker.AppendSpAtk(stage, attacker: attacker, move: this);
                            }

                            if (defender.SpDefStage > 0)
                            {
                                var stage = defender.SpDefStage;
                                defender.SpDefStage = 0;
                                msg += $"{defender.Name}'s special defense stage was reset!\n";
                                msg += attacker.AppendSpDef(stage, attacker: attacker, move: this);
                            }

                            if (defender.SpeedStage > 0)
                            {
                                var stage = defender.SpeedStage;
                                defender.SpeedStage = 0;
                                msg += $"{defender.Name}'s speed stage was reset!\n";
                                msg += attacker.AppendSpeed(stage, attacker: attacker, move: this);
                            }

                            if (defender.EvasionStage > 0)
                            {
                                var stage = defender.EvasionStage;
                                defender.EvasionStage = 0;
                                msg += $"{defender.Name}'s evasion stage was reset!\n";
                                msg += attacker.AppendEvasion(stage, attacker: attacker, move: this);
                            }

                            if (defender.AccuracyStage > 0)
                            {
                                var stage = defender.AccuracyStage;
                                defender.AccuracyStage = 0;
                                msg += $"{defender.Name}'s accuracy stage was reset!\n";
                                msg += attacker.AppendAccuracy(stage, attacker: attacker, move: this);
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
                                {
                                    msg += "It had no effect!\n";
                                }
                                else
                                {
                                    msg += defender.Heal(defender.StartingHp / 4, source: $"{attacker.Name}'s present");
                                }

                                return msg;
                            }

                            var presentPower = action == 2 ? 40 : action == 3 ? 80 : 120;
                            var presentMove = Present(presentPower);
                            var (msgadd, hits) = presentMove.Attack(attacker, defender, battle);
                            msg += msgadd;
                            return msg;
                        }
                        // Incinerate
                        case 315 when defender.HeldItem.IsBerry(onlyActive: false):
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.STICKY_HOLD)
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
                        {
                            if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                            {
                                (var msgadd, numHits1) = Attack(attacker, defender, battle);
                                msg += msgadd;
                            }
                        }
                    }
                    else switch (Effect)
                    {
                        // Turn 3 hit moves
                        case 27:
                        {
                            if (attacker.LockedMove.Turn == 2)
                            {
                                msg += defender.Damage(attacker.Bide.Value * 2, battle, move: this, moveType: currentType,
                                    attacker: attacker);
                                attacker.Bide = null;
                                numHits1 = 1;
                            }

                            break;
                        }
                        // Counter attack moves
                        case 228:
                            msg += defender.Damage((int)(1.5 * attacker.LastMoveDamage.Item1), battle, move: this,
                                moveType: currentType, attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 145:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 90:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits1 = 1;
                            break;
                        // Static-damage moves
                        case 41:
                            msg += defender.Damage(defender.Hp / 2, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 42:
                            msg += defender.Damage(40, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 88:
                            msg += defender.Damage(attacker.Level, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 89:
                        {
                            // 0.5-1.5, increments of .1
                            var scale = new Random().Next(0, 11) / 10.0 + 0.5;
                            msg += defender.Damage((int)(attacker.Level * scale), battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits1 = 1;
                            break;
                        }
                        case 131:
                            msg += defender.Damage(20, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 190:
                            msg += defender.Damage(Math.Max(0, defender.Hp - attacker.Hp), battle, move: this,
                                moveType: currentType, attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 39:
                            msg += defender.Damage(defender.Hp, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 321:
                            msg += defender.Damage(attacker.Hp, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits1 = 1;
                            break;
                        case 413:
                            msg += defender.Damage(3 * (defender.Hp / 4), battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits1 = 1;
                            break;
                        // Beat up, a stupid move
                        case 155:
                        {
                            foreach (var poke in attacker.Owner.Party)
                            {
                                if (defender.Hp == 0)
                                {
                                    break;
                                }

                                if (poke.Hp == 0)
                                {
                                    continue;
                                }

                                if (poke == attacker)
                                {
                                    var (msgadd, nh) = Attack(attacker, defender, battle);
                                    msg += msgadd;
                                    numHits1 += nh;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(poke.NonVolatileEffect.Current))
                                    {
                                        continue;
                                    }

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
                            msg += attacker.AppendDefense(-attacker.Stockpile, attacker: attacker, move: this);
                            msg += attacker.AppendSpDef(-attacker.Stockpile, attacker: attacker, move: this);
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
                            {
                                msg += defender.Heal(defender.StartingHp * 3 / 4);
                            }
                            else
                            {
                                msg += defender.Heal(defender.StartingHp / 2);
                            }

                            break;
                        }
                        case 133:
                        {
                            if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                            {
                                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                            }
                            else if (battle.Weather.Get() == "h-wind")
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }
                            else if (!string.IsNullOrEmpty(battle.Weather.Get()))
                            {
                                msg += attacker.Heal(attacker.StartingHp / 4);
                            }
                            else
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }

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

                            msg += attacker.Heal(attacker.StartingHp / healFactor, source: "stockpiled energy");
                            msg += attacker.AppendDefense(-attacker.Stockpile, attacker: attacker, move: this);
                            msg += attacker.AppendSpDef(-attacker.Stockpile, attacker: attacker, move: this);
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
                            {
                                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                            }
                            else
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }

                            break;
                        }
                        case 387:
                        {
                            if (battle.Terrain.Item?.ToString() == "grassy")
                            {
                                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                            }
                            else
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }

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
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 168:
                        case 429 when defender.StatIncreased:
                            msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker: attacker, move: this);
                            break;
                        case 37 when effectChance.HasValue:
                        {
                            string[] statuses = new[] { "burn", "freeze", "paralysis" };
                            var status = statuses[new Random().Next(statuses.Length)];
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 464 when effectChance.HasValue:
                        {
                            string[] statuses = new[] { "poison", "paralysis", "sleep" };
                            var status = statuses[new Random().Next(statuses.Length)];
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 6 or 261 or 275 or 380 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("freeze", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 7 or 153 or 263 or 264 or 276 or 332 or 372 or 396 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker,
                                    move: this);
                            }

                            break;
                        }
                        case 68:
                            msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker, move: this);
                            break;
                        case 3 or 78 or 210 or 447 or 461 when
                            effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 67 or 390 or 486:
                            msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker: attacker, move: this);
                            break;
                        case 203 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker: attacker,
                                    move: this);
                            }

                            break;
                        }
                        case 34:
                            msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker: attacker, move: this);
                            break;
                        case 2:
                        {
                            if (Id == 464 && attacker.Name != "Darkrai")
                            {
                                msg += $"{attacker.Name} can't use the move!\n";
                            }
                            else
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 330 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 38:
                        {
                            msg += attacker.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this,
                                turns: 3, force: true);
                            if (attacker.NonVolatileEffect.Sleep())
                            {
                                msg += $"{attacker.Name}'s slumber restores its health back to full!\n";
                                attacker.Hp = attacker.StartingHp;
                            }

                            break;
                        }
                        case 50 or 119 or 167 or 200:
                            msg += defender.Confuse(attacker: attacker, move: this);
                            break;
                        // This checks if attacker.LockedMove is not null as locked_move is cleared if the poke dies to rocky helmet or similar items
                        case 28 when attacker.LockedMove != null && attacker.LockedMove.IsLastTurn():
                            msg += attacker.Confuse();
                            break;
                        case 77 or 268 or 334 or 478 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.Confuse(attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 497 when defender.StatIncreased:
                            msg += defender.Confuse(attacker: attacker, move: this);
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
                    {
                        msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                    }

                    if (Effect is 12 or 157 or 161 or 207 or 209 or 323 or 367 or 414 or 427 or 467 or 468 or 472)
                    {
                        msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                    }

                    if (Effect is 14 or 212 or 291 or 328 or 392 or 414 or 427 or 472)
                    {
                        msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                    }

                    if (Effect is 161 or 175 or 207 or 212 or 291 or 367 or 414 or 427 or 472)
                    {
                        msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                    }

                    switch (Effect)
                    {
                        case 130 or 213 or 291 or 296 or 414 or 427 or 442 or 468 or 469 or 487:
                            msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                            break;
                        case 17 or 467:
                            msg += attacker.AppendEvasion(1, attacker: attacker, move: this);
                            break;
                        case 278 or 323:
                            msg += attacker.AppendAccuracy(1, attacker: attacker, move: this);
                            break;
                        case 139 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 140 or 375 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 277 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 433 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 167:
                            msg += defender.AppendSpAtk(1, attacker: attacker, move: this);
                            break;
                        // +2
                        case 51 or 309:
                            msg += attacker.AppendAttack(2, attacker: attacker, move: this);
                            break;
                        case 52 or 453:
                            msg += attacker.AppendDefense(2, attacker: attacker, move: this);
                            break;
                    }

                    if (Effect is 53 or 285 or 309 or 313 or 366)
                    {
                        msg += attacker.AppendSpeed(2, attacker: attacker, move: this);
                    }

                    if (Effect is 54 or 309 or 366)
                    {
                        msg += attacker.AppendSpAtk(2, attacker: attacker, move: this);
                    }

                    switch (Effect)
                    {
                        case 55 or 366:
                            msg += attacker.AppendSpDef(2, attacker: attacker, move: this);
                            break;
                        case 109:
                            msg += attacker.AppendEvasion(2, attacker: attacker, move: this);
                            break;
                        case 119 or 432 or 483:
                            msg += defender.AppendAttack(2, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 432:
                            msg += defender.AppendSpAtk(2, attacker: attacker, move: this);
                            break;
                        case 359 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendDefense(2, attacker: attacker, move: this);
                            }

                            break;
                        }
                        // -1
                        case 19 or 206 or 344 or 347 or 357 or 365 or 388 or 412:
                            msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 20 or 206:
                            msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                            break;
                        case 344 or 347 or 358 or 412:
                            msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                            break;
                        case 428:
                            msg += defender.AppendSpDef(-1, attacker: attacker, move: this);
                            break;
                        case 331 or 390:
                            msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                            break;
                        case 24:
                            msg += defender.AppendAccuracy(-1, attacker: attacker, move: this);
                            break;
                        case 25 or 259:
                            msg += defender.AppendEvasion(-1, attacker: attacker, move: this);
                            break;
                        case 69 or 396 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 70 or 397 or 435 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 475:
                        {
                            // This one has two different chance percents, one has to be hardcoded
                            if (new Random().Next(1, 101) <= 50)
                            {
                                msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 21 or 71 or 357 or 477 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 72 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 73 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpDef(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 74 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendAccuracy(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 183:
                            msg += attacker.AppendAttack(-1, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 183 or 230 or 309 or 335 or 405 or 438 or 442:
                            msg += attacker.AppendDefense(-1, attacker: attacker, move: this);
                            break;
                        case 480:
                            msg += attacker.AppendSpAtk(-1, attacker: attacker, move: this);
                            break;
                    }

                    if (Effect is 230 or 309 or 335)
                    {
                        msg += attacker.AppendSpDef(-1, attacker: attacker, move: this);
                    }

                    switch (Effect)
                    {
                        case 219 or 335:
                            msg += attacker.AppendSpeed(-1, attacker: attacker, move: this);
                            break;
                        // -2
                        case 59 or 169:
                            msg += defender.AppendAttack(-2, attacker: attacker, move: this);
                            break;
                        case 60 or 483:
                            msg += defender.AppendDefense(-2, attacker: attacker, move: this);
                            break;
                        case 61:
                            msg += defender.AppendSpeed(-2, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 62 or 169 or 266:
                            msg += defender.AppendSpAtk(-2, attacker: attacker, move: this);
                            break;
                        case 63:
                            msg += defender.AppendSpDef(-2, attacker: attacker, move: this);
                            break;
                        case 272 or 297 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpDef(-2, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 205:
                            msg += attacker.AppendSpAtk(-2, attacker: attacker, move: this);
                            break;
                        case 479:
                            msg += attacker.AppendSpeed(-2, attacker: attacker, move: this);
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
                                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 143:
                            msg += attacker.Damage(attacker.StartingHp / 2, battle);
                            msg += attacker.AppendAttack(12, attacker: attacker, move: this);
                            break;
                        case 317:
                        {
                            var amount = 1;
                            if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                            {
                                amount = 2;
                            }

                            msg += attacker.AppendAttack(amount, attacker: attacker, move: this);
                            msg += attacker.AppendSpAtk(amount, attacker: attacker, move: this);
                            break;
                        }
                        case 364 when defender.NonVolatileEffect.Poison():
                            msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                            msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                            msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                            break;
                        case 329:
                            msg += attacker.AppendDefense(3, attacker: attacker, move: this);
                            break;
                        case 322:
                            msg += attacker.AppendSpAtk(3, attacker: attacker, move: this);
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
                                Func<int, DuelPokemon, Move, string, bool, string> statRaiseFunc =
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
                                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                            }
                            else
                            {
                                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 485:
                            msg += attacker.Damage(attacker.StartingHp / 2, battle);
                            msg += attacker.AppendAttack(2, attacker: attacker, move: this);
                            msg += attacker.AppendSpAtk(2, attacker: attacker, move: this);
                            msg += attacker.AppendSpeed(2, attacker: attacker, move: this);
                            break;
                    }

                    // Flinch
                    if (!defender.HasMoved)
                    {
                        for (var hit = 0; hit < numHits1; hit++)
                        {
                            if (defender.Flinched)
                            {
                                break;
                            }

                            if (new[] { 32, 76, 93, 147, 151, 159, 274, 275, 276, 425, 475, 501 }.Contains(Effect) &&
                                effectChance.HasValue)
                            {
                                if (new Random().Next(1, 101) <= effectChance)
                                {
                                    msg += defender.Flinch(move: this, attacker: attacker);
                                }
                            }
                            else if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                            {
                                if (attacker.Ability() == Ability.STENCH)
                                {
                                    if (new Random().Next(1, 101) <= 10)
                                    {
                                        msg += defender.Flinch(move: this, attacker: attacker, source: "its stench");
                                    }
                                }
                                else if (attacker.HeldItem == "kings-rock")
                                {
                                    if (new Random().Next(1, 101) <= 10)
                                    {
                                        msg += defender.Flinch(move: this, attacker: attacker, source: "its kings rock");
                                    }
                                }
                                else if (attacker.HeldItem == "razor-fang")
                                {
                                    if (new Random().Next(1, 101) <= 10)
                                    {
                                        msg += defender.Flinch(move: this, attacker: attacker, source: "its razor fang");
                                    }
                                }
                            }
                        }
                    }

                    switch (Effect)
                    {
                        // Move locking
                        case 87:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                            if (defender.Ability(attacker: attacker, move: this) == Ability.OBLIVIOUS)
                            {
                                msg += $"{defender.Name} is too oblivious to be taunted!\n";
                            }
                            else if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
                            {
                                msg += $"{defender.Name}'s aroma veil protects it from being taunted!\n";
                            }
                            else
                            {
                                if (defender.HasMoved)
                                {
                                    defender.Taunt.SetTurns(4);
                                }
                                else
                                {
                                    defender.Taunt.SetTurns(3);
                                }

                                msg += $"{defender.Name} is being taunted!\n";
                            }

                            break;
                        }
                        case 91:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
                            {
                                msg += $"{defender.Name}'s aroma veil protects it from being encored!\n";
                            }
                            else
                            {
                                defender.Encore.Set(defender.LastMove, 4);
                                if (!defender.HasMoved)
                                {
                                    defender.Owner.SelectedAction = new Trainer.MoveAction(defender.LastMove);
                                }

                                msg += $"{defender.Name} is giving an encore!\n";
                            }

                            break;
                        }
                        case 166:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                    {
                        msg += attacker.Damage(attacker.StartingHp / 10, battle, source: "its life orb");
                    }

                    // Swap outs
                    // A poke is force-swapped out before activating red-card
                    if (Effect is 29 or 314)
                    {
                        var swaps = defender.Owner.ValidSwaps(attacker, battle, checkTrap: false);
                        if (swaps.Count == 0)
                        {
                            // Do nothing
                        }
                        else if (defender.Ability(attacker: attacker, move: this) == Ability.SUCTION_CUPS)
                        {
                            msg += $"{defender.Name}'s suction cups kept it in place!\n";
                        }
                        else if (defender.Ability(attacker: attacker, move: this) == Ability.GUARD_DOG)
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
                            defender.Owner.SwitchPoke(idx, midTurn: true);
                            msg += defender.Owner.CurrentPokemon.SendOut(attacker, battle);
                            // Safety in case the poke dies on send out.
                            if (defender.Owner.CurrentPokemon != null)
                            {
                                defender.Owner.CurrentPokemon.HasMoved = true;
                            }
                        }
                    }
                    // A red-card forces the attacker to swap to a random poke, even if they used a switch out move
                    else if (defender.HeldItem == "red-card" && defender.Hp > 0 && DamageClass != DamageClass.STATUS)
                    {
                        var swaps = attacker.Owner.ValidSwaps(defender, battle, checkTrap: false);
                        if (swaps.Count == 0)
                        {
                            // Do nothing
                        }
                        else if (attacker.Ability(attacker: defender, move: this) == Ability.SUCTION_CUPS)
                        {
                            msg += $"{attacker.Name}'s suction cups kept it in place from {defender.Name}'s red card!\n";
                            defender.HeldItem.Use();
                        }
                        else if (attacker.Ability(attacker: defender, move: this) == Ability.GUARD_DOG)
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
                            attacker.Owner.SwitchPoke(idx, midTurn: true);
                            msg += attacker.Owner.CurrentPokemon.SendOut(defender, battle);
                            // Safety in case the poke dies on send out.
                            if (attacker.Owner.CurrentPokemon != null)
                            {
                                attacker.Owner.CurrentPokemon.HasMoved = true;
                            }
                        }
                    }
                    else if (new[] { 128, 154, 229, 347 }.Contains(Effect))
                    {
                        var swaps = attacker.Owner.ValidSwaps(defender, battle, checkTrap: false);
                        if (swaps.Count > 0)
                        {
                            msg += $"{attacker.Name} went back!\n";
                            if (Effect == 128)
                            {
                                attacker.Owner.BatonPass = new BatonPass(attacker);
                            }

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
                    if (new[] { 169, 221, 271, 321 }.Contains(Effect))
                    {
                        msg += attacker.Faint(battle);
                    }

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
                            foreach (var poke in attacker.Owner.Party)
                            {
                                poke.NonVolatileEffect.Reset();
                            }

                            msg +=
                                $"A bell chimed, and all of {attacker.Owner.Name}'s pokemon had status conditions removed!\n";
                            break;
                        }
                        // Psycho Shift
                        case 235:
                        {
                            var transferedStatus = attacker.NonVolatileEffect.Current;
                            msg += defender.NonVolatileEffect.ApplyStatus(transferedStatus, battle, attacker: attacker,
                                move: this);
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
                            {
                                msg += $"{attacker.Name} is already under the effect of perish song!\n";
                            }
                            else
                            {
                                attacker.PerishSong.SetTurns(4);
                            }

                            if (defender.PerishSong.Active())
                            {
                                msg += $"{defender.Name} is already under the effect of perish song!\n";
                            }
                            else if (defender.Ability(attacker: attacker, move: this) == Ability.SOUNDPROOF)
                            {
                                msg += $"{defender.Name}'s soundproof protects it from hearing the song!\n";
                            }
                            else
                            {
                                defender.PerishSong.SetTurns(4);
                            }

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
                            if (!Enum.IsDefined(typeof(ElementType), t))
                            {
                                t = ElementType.NORMAL;
                            }

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
                            if (defender.NonVolatileEffect.Sleep())
                            {
                                defender.NonVolatileEffect.Reset();
                            }

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
                            {
                                attacker.Owner.AuroraVeil.SetTurns(8);
                            }
                            else
                            {
                                attacker.Owner.AuroraVeil.SetTurns(5);
                            }

                            msg += $"{attacker.Name} put up its aurora veil!\n";
                            break;
                        }
                        // Light Screen
                        case 36 or 421:
                        {
                            if (attacker.HeldItem == "light-clay")
                            {
                                attacker.Owner.LightScreen.SetTurns(8);
                            }
                            else
                            {
                                attacker.Owner.LightScreen.SetTurns(5);
                            }

                            msg += $"{attacker.Name} put up its light screen!\n";
                            break;
                        }
                        // Reflect
                        case 66 or 422:
                        {
                            if (attacker.HeldItem == "light-clay")
                            {
                                attacker.Owner.Reflect.SetTurns(8);
                            }
                            else
                            {
                                attacker.Owner.Reflect.SetTurns(5);
                            }

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
                            {
                                defender.Bind.SetTurns(7);
                            }
                            else
                            {
                                defender.Bind.SetTurns(new Random().Next(4, 6));
                            }

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
                            msg += defender.Infatuate(attacker, move: this);
                            break;
                        // Heart Swap
                        case 251:
                        {
                            (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                            (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                            (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                            (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                            (attacker.SpeedStage, defender.SpeedStage) = (defender.SpeedStage, attacker.SpeedStage);

                            (attacker.AccuracyStage, defender.AccuracyStage) = (defender.AccuracyStage, attacker.AccuracyStage);

                            (attacker.EvasionStage, defender.EvasionStage) = (defender.EvasionStage, attacker.EvasionStage);

                            msg += $"{attacker.Name} switched stat changes with {defender.Name}!\n";
                            break;
                        }
                        // Power Swap
                        case 244:
                        {
                            (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                            (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                            msg += $"{attacker.Name} switched attack and special attack stat changes with {defender.Name}!\n";
                            break;
                        }
                        // Guard Swap
                        case 245:
                        {
                            (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                            (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                            msg += $"{attacker.Name} switched defense and special defense stat changes with {defender.Name}!\n";
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

                            if (!defender.Grounded(battle, attacker: attacker, move: this))
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
                            msg += Use(defender, attacker, battle, usePP: false, bounced: true);
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
                                msg += attacker.HeldItem.EatBerry(consumer: defender, attacker: attacker, move: this);
                            }
                            else
                            {
                                attacker.HeldItem.Use();
                                switch (item)
                                {
                                    case "flame-orb":
                                        msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker: attacker,
                                            move: this);
                                        break;
                                    case "kings-rock" or "razor-fang":
                                        msg += defender.Flinch(attacker: attacker, move: this);
                                        break;
                                    case "light-ball":
                                        msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker,
                                            move: this);
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
                                        msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker: attacker,
                                            move: this);
                                        break;
                                    case "toxic-orb":
                                        msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker: attacker,
                                            move: this);
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
                            if (defender.Ability(attacker: attacker, move: this) == Ability.STICKY_HOLD)
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
                            {
                                msg += $"{attacker.Name} gained {attacker.HeldItem.Name}!\n";
                            }

                            if (defender.HeldItem.Name != null)
                            {
                                msg += $"{defender.Name} gained {defender.HeldItem.Name}!\n";
                            }

                            break;
                        }
                        // Knock off
                        case 189 when defender.HeldItem.HasItem() && defender.HeldItem.CanRemove() &&
                                      defender.Substitute == 0 && attacker.Hp > 0:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.STICKY_HOLD)
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
                            {
                                msgadd += poke.HeldItem.EatBerry(attacker: attacker, move: this);
                            }

                            msg += msgadd;
                            if (string.IsNullOrEmpty(msgadd))
                            {
                                msg += "But nothing happened...";
                            }

                            break;
                        }
                        // Corrosive Gas
                        case 430:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.STICKY_HOLD)
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
                                defender.Grounded(battle, attacker: attacker, move: this))
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
                                if (!p.TypeIds.Contains(ElementType.GRASS))
                                {
                                    continue;
                                }

                                if (!p.Grounded(battle))
                                {
                                    continue;
                                }

                                if (p.Dive || p.Dig || p.Fly || p.ShadowForce)
                                {
                                    continue;
                                }

                                msg += p.AppendAttack(1, attacker: attacker, move: this);
                                msg += p.AppendSpAtk(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        // Flower Shield
                        case 351:
                        {
                            foreach (var p in new[] { attacker, defender })
                            {
                                if (!p.TypeIds.Contains(ElementType.GRASS))
                                {
                                    continue;
                                }

                                if (!p.Grounded(battle))
                                {
                                    continue;
                                }

                                if (p.Dive || p.Dig || p.Fly || p.ShadowForce)
                                {
                                    continue;
                                }

                                msg += p.AppendDefense(1, attacker: attacker, move: this);
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
                                msg += attacker.AppendSpeed(-1, attacker: attacker, move: this);
                                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
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
                            msg += attacker.AppendAttack(3, attacker: attacker, move: this);
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
                            {
                                msg += attacker.HeldItem.EatBerry(attacker: defender, move: this);
                            }

                            break;
                        }
                        // Court Change
                        case 431:
                        {
                            // Swap spikes
                            (attacker.Owner.Spikes, defender.Owner.Spikes) = (defender.Owner.Spikes, attacker.Owner.Spikes);

                            // Swap toxic spikes
                            (attacker.Owner.ToxicSpikes, defender.Owner.ToxicSpikes) = (defender.Owner.ToxicSpikes, attacker.Owner.ToxicSpikes);

                            // Swap stealth rock
                            (attacker.Owner.StealthRock, defender.Owner.StealthRock) = (defender.Owner.StealthRock, attacker.Owner.StealthRock);

                            // Swap sticky web
                            (attacker.Owner.StickyWeb, defender.Owner.StickyWeb) = (defender.Owner.StickyWeb, attacker.Owner.StickyWeb);

                            // Swap aurora veil
                            (attacker.Owner.AuroraVeil, defender.Owner.AuroraVeil) = (defender.Owner.AuroraVeil, attacker.Owner.AuroraVeil);

                            // Swap light screen
                            (attacker.Owner.LightScreen, defender.Owner.LightScreen) = (defender.Owner.LightScreen, attacker.Owner.LightScreen);

                            // Swap reflect
                            (attacker.Owner.Reflect, defender.Owner.Reflect) = (defender.Owner.Reflect, attacker.Owner.Reflect);

                            // Swap mist
                            (attacker.Owner.Mist, defender.Owner.Mist) = (defender.Owner.Mist, attacker.Owner.Mist);

                            // Swap safeguard
                            (attacker.Owner.Safeguard, defender.Owner.Safeguard) = (defender.Owner.Safeguard, attacker.Owner.Safeguard);

                            // Swap tailwind
                            (attacker.Owner.Tailwind, defender.Owner.Tailwind) = (defender.Owner.Tailwind, attacker.Owner.Tailwind);

                            msg += "Active battle effects swapped sides!\n";
                            break;
                        }
                        // Roost
                        case 215:
                        {
                            attacker.Roost = true;
                            if (attacker.TypeIds.Contains(ElementType.FLYING))
                            {
                                msg += $"{attacker.Name}'s flying type is suppressed!\n";
                            }

                            break;
                        }
                        // Pluck
                        case 225 when defender.Ability(attacker: attacker, move: this) != Ability.STICKY_HOLD:
                            msg += defender.HeldItem.EatBerry(consumer: attacker);
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
                                if (attacker.Form("Cramorant-gulping"))
                                {
                                    msg += $"{attacker.Name} gulped up an arrokuda!\n";
                                }
                            }
                            else
                            {
                                if (attacker.Form("Cramorant-gorging"))
                                {
                                    msg += $"{attacker.Name} gulped up a pikachu!\n";
                                }
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
                            {
                                switch (battle.Terrain.Item?.ToString())
                                {
                                    case "grassy":
                                        msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker,
                                            move: this);
                                        break;
                                    case "misty":
                                        msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                                        break;
                                    case "psychic":
                                        msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                                        break;
                                    default:
                                        msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker,
                                            move: this);
                                        break;
                                }
                            }

                            break;
                        }
                    }


                    if (IsSoundBased() && attacker.HeldItem == "throat-spray")
                    {
                        msg += attacker.AppendSpAtk(1, attacker: attacker, source: "its throat spray");
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
                    if (defender.Ability(attacker: attacker, move: this) == Ability.DANCER && IsDance() && usePP)
                    {
                        var hm = defender.HasMoved;
                        msg += Use(defender, attacker, battle, usePP: false);
                        defender.HasMoved = hm;
                    }

                    return msg;

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
                    {
                        switch (currentType)
                        {
                            // Heal
                            case ElementType.ELECTRIC when
                                defender.Ability(attacker: attacker, move: this) == Ability.VOLT_ABSORB:
                                msg += $"{defender.Name}'s volt absorb absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            case ElementType.WATER when
                                defender.Ability(attacker: attacker, move: this) == Ability.WATER_ABSORB:
                                msg += $"{defender.Name}'s water absorb absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            case ElementType.WATER when
                                defender.Ability(attacker: attacker, move: this) == Ability.DRY_SKIN:
                                msg += $"{defender.Name}'s dry skin absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            case ElementType.GROUND when
                                defender.Ability(attacker: attacker, move: this) == Ability.EARTH_EATER:
                                msg += $"{defender.Name}'s earth eater absorbed the move!\n";
                                msg += defender.Heal(defender.StartingHp / 4, source: "absorbing the move");
                                return msg;
                            // Stat stage changes
                            case ElementType.ELECTRIC when
                                defender.Ability(attacker: attacker, move: this) == Ability.LIGHTNING_ROD:
                                msg += $"{defender.Name}'s lightning rod absorbed the move!\n";
                                msg += defender.AppendSpAtk(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.ELECTRIC when
                                defender.Ability(attacker: attacker, move: this) == Ability.MOTOR_DRIVE:
                                msg += $"{defender.Name}'s motor drive absorbed the move!\n";
                                msg += defender.AppendSpeed(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.WATER when
                                defender.Ability(attacker: attacker, move: this) == Ability.STORM_DRAIN:
                                msg += $"{defender.Name}'s storm drain absorbed the move!\n";
                                msg += defender.AppendSpAtk(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.GRASS when
                                defender.Ability(attacker: attacker, move: this) == Ability.SAP_SIPPER:
                                msg += $"{defender.Name}'s sap sipper absorbed the move!\n";
                                msg += defender.AppendAttack(1, attacker: defender, move: this);
                                return msg;
                            case ElementType.FIRE when
                                defender.Ability(attacker: attacker, move: this) == Ability.WELL_BAKED_BODY:
                                msg += $"{defender.Name}'s well baked body absorbed the move!\n";
                                msg += defender.AppendDefense(2, attacker: defender, move: this);
                                return msg;
                            // Other
                            case ElementType.FIRE when
                                defender.Ability(attacker: attacker, move: this) == Ability.FLASH_FIRE:
                                defender.FlashFire = true;
                                msg += $"{defender.Name} used its flash fire to buff its fire type moves!\n";
                                return msg;
                        }
                    }

                    // Stat stage from type items
                    if (defender.Substitute == 0)
                    {
                        switch (currentType)
                        {
                            case ElementType.WATER when defender.HeldItem == "absorb-bulb":
                                msg += defender.AppendSpAtk(1, attacker: defender, move: this, source: "its absorb bulb");
                                defender.HeldItem.Use();
                                break;
                            case ElementType.ELECTRIC when defender.HeldItem == "cell-battery":
                                msg += defender.AppendAttack(1, attacker: defender, move: this, source: "its cell battery");
                                defender.HeldItem.Use();
                                break;
                        }

                        switch (currentType)
                        {
                            case ElementType.WATER when defender.HeldItem == "luminous-moss":
                                msg += defender.AppendSpDef(1, attacker: defender, move: this, source: "its luminous moss");
                                defender.HeldItem.Use();
                                break;
                            case ElementType.ICE when defender.HeldItem == "snowball":
                                msg += defender.AppendAttack(1, attacker: defender, move: this, source: "its snowball");
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
                                msg += move.Use(attacker, defender, battle, usePP: false, overrideSleep: true);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Mirror Move/Copy Cat
                        case 10 or 243:
                        {
                            if (defender.LastMove != null)
                            {
                                msg += defender.LastMove.Use(attacker, defender, battle, usePP: false);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Me First
                        case 242:
                        {
                            if (defender.Owner.SelectedAction is Trainer.MoveAction move)
                            {
                                msg += move.Move.Use(attacker, defender, battle, usePP: false);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Assist
                        case 181:
                        {
                            var assistMove = attacker.GetAssistMove();
                            if (assistMove != null)
                            {
                                msg += assistMove.Use(attacker, defender, battle, usePP: false);
                                return msg;
                            }
                            else
                            {
                                msg += "But it failed!\n";
                                return msg;
                            }
                        }
                        // Spectral Thief
                        case 410:
                        {
                            if (defender.AttackStage > 0)
                            {
                                var stage = defender.AttackStage;
                                defender.AttackStage = 0;
                                msg += $"{defender.Name}'s attack stage was reset!\n";
                                msg += attacker.AppendAttack(stage, attacker: attacker, move: this);
                            }

                            if (defender.DefenseStage > 0)
                            {
                                var stage = defender.DefenseStage;
                                defender.DefenseStage = 0;
                                msg += $"{defender.Name}'s defense stage was reset!\n";
                                msg += attacker.AppendDefense(stage, attacker: attacker, move: this);
                            }

                            if (defender.SpAtkStage > 0)
                            {
                                var stage = defender.SpAtkStage;
                                defender.SpAtkStage = 0;
                                msg += $"{defender.Name}'s special attack stage was reset!\n";
                                msg += attacker.AppendSpAtk(stage, attacker: attacker, move: this);
                            }

                            if (defender.SpDefStage > 0)
                            {
                                var stage = defender.SpDefStage;
                                defender.SpDefStage = 0;
                                msg += $"{defender.Name}'s special defense stage was reset!\n";
                                msg += attacker.AppendSpDef(stage, attacker: attacker, move: this);
                            }

                            if (defender.SpeedStage > 0)
                            {
                                var stage = defender.SpeedStage;
                                defender.SpeedStage = 0;
                                msg += $"{defender.Name}'s speed stage was reset!\n";
                                msg += attacker.AppendSpeed(stage, attacker: attacker, move: this);
                            }

                            if (defender.EvasionStage > 0)
                            {
                                var stage = defender.EvasionStage;
                                defender.EvasionStage = 0;
                                msg += $"{defender.Name}'s evasion stage was reset!\n";
                                msg += attacker.AppendEvasion(stage, attacker: attacker, move: this);
                            }

                            if (defender.AccuracyStage > 0)
                            {
                                var stage = defender.AccuracyStage;
                                defender.AccuracyStage = 0;
                                msg += $"{defender.Name}'s accuracy stage was reset!\n";
                                msg += attacker.AppendAccuracy(stage, attacker: attacker, move: this);
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
                                {
                                    msg += "It had no effect!\n";
                                }
                                else
                                {
                                    msg += defender.Heal(defender.StartingHp / 4, source: $"{attacker.Name}'s present");
                                }

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
                        case 315 when defender.HeldItem.IsBerry(onlyActive: false):
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.STICKY_HOLD)
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
                        {
                            if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                            {
                                string msgadd;
                                (msgadd, numHits) = Attack(attacker, defender, battle);
                                msg += msgadd;
                            }
                        }
                    }
                    else switch (Effect)
                    {
                        // Turn 3 hit moves
                        case 27:
                        {
                            if (attacker.LockedMove.Turn == 2)
                            {
                                msg += defender.Damage(attacker.Bide.Value * 2, battle, move: this, moveType: currentType,
                                    attacker: attacker);
                                attacker.Bide = null;
                                numHits = 1;
                            }

                            break;
                        }
                        // Counter attack moves
                        case 228:
                            msg += defender.Damage((int)(1.5 * attacker.LastMoveDamage.Item1), battle, move: this,
                                moveType: currentType, attacker: attacker);
                            numHits = 1;
                            break;
                        case 145:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits = 1;
                            break;
                        case 90:
                            msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits = 1;
                            break;
                        // Static-damage moves
                        case 41:
                            msg += defender.Damage(defender.Hp / 2, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits = 1;
                            break;
                        case 42:
                            msg += defender.Damage(40, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits = 1;
                            break;
                        case 88:
                            msg += defender.Damage(attacker.Level, battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits = 1;
                            break;
                        case 89:
                        {
                            // 0.5-1.5, increments of .1
                            var scale = new Random().Next(0, 11) / 10.0 + 0.5;
                            msg += defender.Damage((int)(attacker.Level * scale), battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits = 1;
                            break;
                        }
                        case 131:
                            msg += defender.Damage(20, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits = 1;
                            break;
                        case 190:
                            msg += defender.Damage(Math.Max(0, defender.Hp - attacker.Hp), battle, move: this,
                                moveType: currentType, attacker: attacker);
                            numHits = 1;
                            break;
                        case 39:
                            msg += defender.Damage(defender.Hp, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits = 1;
                            break;
                        case 321:
                            msg += defender.Damage(attacker.Hp, battle, move: this, moveType: currentType, attacker: attacker);
                            numHits = 1;
                            break;
                        case 413:
                            msg += defender.Damage(3 * (defender.Hp / 4), battle, move: this, moveType: currentType,
                                attacker: attacker);
                            numHits = 1;
                            break;
                        // Beat up, a stupid move
                        case 155:
                        {
                            foreach (var poke in attacker.Owner.Party)
                            {
                                if (defender.Hp == 0)
                                {
                                    break;
                                }

                                if (poke.Hp == 0)
                                {
                                    continue;
                                }

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
                                    if (!string.IsNullOrEmpty(poke.NonVolatileEffect.Current))
                                    {
                                        continue;
                                    }

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
                            msg += attacker.AppendDefense(-attacker.Stockpile, attacker: attacker, move: this);
                            msg += attacker.AppendSpDef(-attacker.Stockpile, attacker: attacker, move: this);
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
                            {
                                msg += defender.Heal(defender.StartingHp * 3 / 4);
                            }
                            else
                            {
                                msg += defender.Heal(defender.StartingHp / 2);
                            }

                            break;
                        }
                        case 133:
                        {
                            if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                            {
                                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                            }
                            else if (battle.Weather.Get() == "h-wind")
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }
                            else if (!string.IsNullOrEmpty(battle.Weather.Get()))
                            {
                                msg += attacker.Heal(attacker.StartingHp / 4);
                            }
                            else
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }

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

                            msg += attacker.Heal(attacker.StartingHp / healFactor, source: "stockpiled energy");
                            msg += attacker.AppendDefense(-attacker.Stockpile, attacker: attacker, move: this);
                            msg += attacker.AppendSpDef(-attacker.Stockpile, attacker: attacker, move: this);
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
                            {
                                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                            }
                            else
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }

                            break;
                        }
                        case 387:
                        {
                            if (battle.Terrain.Item?.ToString() == "grassy")
                            {
                                msg += attacker.Heal(attacker.StartingHp * 2 / 3);
                            }
                            else
                            {
                                msg += attacker.Heal(attacker.StartingHp / 2);
                            }

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
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 168:
                        case 429 when defender.StatIncreased:
                            msg += defender.NonVolatileEffect.ApplyStatus("burn", battle, attacker: attacker, move: this);
                            break;
                        case 37 when effectChance.HasValue:
                        {
                            string[] statuses = new[] { "burn", "freeze", "paralysis" };
                            var status = statuses[new Random().Next(statuses.Length)];
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 464 when effectChance.HasValue:
                        {
                            string[] statuses = new[] { "poison", "paralysis", "sleep" };
                            var status = statuses[new Random().Next(statuses.Length)];
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus(status, battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 6 or 261 or 275 or 380 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("freeze", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 7 or 153 or 263 or 264 or 276 or 332 or 372 or 396 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker,
                                    move: this);
                            }

                            break;
                        }
                        case 68:
                            msg += defender.NonVolatileEffect.ApplyStatus("paralysis", battle, attacker: attacker, move: this);
                            break;
                        case 3 or 78 or 210 or 447 or 461 when
                            effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 67 or 390 or 486:
                            msg += defender.NonVolatileEffect.ApplyStatus("poison", battle, attacker: attacker, move: this);
                            break;
                        case 203 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker: attacker,
                                    move: this);
                            }

                            break;
                        }
                        case 34:
                            msg += defender.NonVolatileEffect.ApplyStatus("b-poison", battle, attacker: attacker, move: this);
                            break;
                        case 2:
                        {
                            if (Id == 464 && attacker.Name != "Darkrai")
                            {
                                msg += $"{attacker.Name} can't use the move!\n";
                            }
                            else
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 330 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 38:
                        {
                            msg += attacker.NonVolatileEffect.ApplyStatus("sleep", battle, attacker: attacker, move: this,
                                turns: 3, force: true);
                            if (attacker.NonVolatileEffect.Sleep())
                            {
                                msg += $"{attacker.Name}'s slumber restores its health back to full!\n";
                                attacker.Hp = attacker.StartingHp;
                            }

                            break;
                        }
                        case 50 or 119 or 167 or 200:
                            msg += defender.Confuse(attacker: attacker, move: this);
                            break;
                        // This checks if attacker.LockedMove is not null as locked_move is cleared if the poke dies to rocky helmet or similar items
                        case 28 when attacker.LockedMove != null && attacker.LockedMove.IsLastTurn():
                            msg += attacker.Confuse();
                            break;
                        case 77 or 268 or 334 or 478 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.Confuse(attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 497 when defender.StatIncreased:
                            msg += defender.Confuse(attacker: attacker, move: this);
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
                    {
                        msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                    }

                    if (Effect is 12 or 157 or 161 or 207 or 209 or 323 or 367 or 414 or 427 or 467 or 468 or 472)
                    {
                        msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                    }

                    if (Effect is 14 or 212 or 291 or 328 or 392 or 414 or 427 or 472)
                    {
                        msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                    }

                    if (Effect is 161 or 175 or 207 or 212 or 291 or 367 or 414 or 427 or 472)
                    {
                        msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                    }

                    switch (Effect)
                    {
                        case 130 or 213 or 291 or 296 or 414 or 427 or 442 or 468 or 469 or 487:
                            msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                            break;
                        case 17 or 467:
                            msg += attacker.AppendEvasion(1, attacker: attacker, move: this);
                            break;
                        case 278 or 323:
                            msg += attacker.AppendAccuracy(1, attacker: attacker, move: this);
                            break;
                        case 139 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 140 or 375 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 277 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 433 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 167:
                            msg += defender.AppendSpAtk(1, attacker: attacker, move: this);
                            break;
                        // +2
                        case 51 or 309:
                            msg += attacker.AppendAttack(2, attacker: attacker, move: this);
                            break;
                        case 52 or 453:
                            msg += attacker.AppendDefense(2, attacker: attacker, move: this);
                            break;
                    }

                    if (Effect is 53 or 285 or 309 or 313 or 366)
                    {
                        msg += attacker.AppendSpeed(2, attacker: attacker, move: this);
                    }

                    if (Effect is 54 or 309 or 366)
                    {
                        msg += attacker.AppendSpAtk(2, attacker: attacker, move: this);
                    }

                    switch (Effect)
                    {
                        case 55 or 366:
                            msg += attacker.AppendSpDef(2, attacker: attacker, move: this);
                            break;
                        case 109:
                            msg += attacker.AppendEvasion(2, attacker: attacker, move: this);
                            break;
                        case 119 or 432 or 483:
                            msg += defender.AppendAttack(2, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 432:
                            msg += defender.AppendSpAtk(2, attacker: attacker, move: this);
                            break;
                        case 359 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += attacker.AppendDefense(2, attacker: attacker, move: this);
                            }

                            break;
                        }
                        // -1
                        case 19 or 206 or 344 or 347 or 357 or 365 or 388 or 412:
                            msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 20 or 206:
                            msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                            break;
                        case 344 or 347 or 358 or 412:
                            msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                            break;
                        case 428:
                            msg += defender.AppendSpDef(-1, attacker: attacker, move: this);
                            break;
                        case 331 or 390:
                            msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                            break;
                        case 24:
                            msg += defender.AppendAccuracy(-1, attacker: attacker, move: this);
                            break;
                        case 25 or 259:
                            msg += defender.AppendEvasion(-1, attacker: attacker, move: this);
                            break;
                        case 69 or 396 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 70 or 397 or 435 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 475:
                        {
                            // This one has two different chance percents, one has to be hardcoded
                            if (new Random().Next(1, 101) <= 50)
                            {
                                msg += defender.AppendDefense(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 21 or 71 or 357 or 477 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 72 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 73 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpDef(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 74 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendAccuracy(-1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 183:
                            msg += attacker.AppendAttack(-1, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 183 or 230 or 309 or 335 or 405 or 438 or 442:
                            msg += attacker.AppendDefense(-1, attacker: attacker, move: this);
                            break;
                        case 480:
                            msg += attacker.AppendSpAtk(-1, attacker: attacker, move: this);
                            break;
                    }

                    if (Effect is 230 or 309 or 335)
                    {
                        msg += attacker.AppendSpDef(-1, attacker: attacker, move: this);
                    }

                    switch (Effect)
                    {
                        case 219 or 335:
                            msg += attacker.AppendSpeed(-1, attacker: attacker, move: this);
                            break;
                        // -2
                        case 59 or 169:
                            msg += defender.AppendAttack(-2, attacker: attacker, move: this);
                            break;
                        case 60 or 483:
                            msg += defender.AppendDefense(-2, attacker: attacker, move: this);
                            break;
                        case 61:
                            msg += defender.AppendSpeed(-2, attacker: attacker, move: this);
                            break;
                    }

                    switch (Effect)
                    {
                        case 62 or 169 or 266:
                            msg += defender.AppendSpAtk(-2, attacker: attacker, move: this);
                            break;
                        case 63:
                            msg += defender.AppendSpDef(-2, attacker: attacker, move: this);
                            break;
                        case 272 or 297 when effectChance.HasValue:
                        {
                            if (new Random().Next(1, 101) <= effectChance)
                            {
                                msg += defender.AppendSpDef(-2, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 205:
                            msg += attacker.AppendSpAtk(-2, attacker: attacker, move: this);
                            break;
                        case 479:
                            msg += attacker.AppendSpeed(-2, attacker: attacker, move: this);
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
                                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpeed(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 143:
                            msg += attacker.Damage(attacker.StartingHp / 2, battle);
                            msg += attacker.AppendAttack(12, attacker: attacker, move: this);
                            break;
                        case 317:
                        {
                            var amount = 1;
                            if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                            {
                                amount = 2;
                            }

                            msg += attacker.AppendAttack(amount, attacker: attacker, move: this);
                            msg += attacker.AppendSpAtk(amount, attacker: attacker, move: this);
                            break;
                        }
                        case 364 when defender.NonVolatileEffect.Poison():
                            msg += defender.AppendAttack(-1, attacker: attacker, move: this);
                            msg += defender.AppendSpAtk(-1, attacker: attacker, move: this);
                            msg += defender.AppendSpeed(-1, attacker: attacker, move: this);
                            break;
                        case 329:
                            msg += attacker.AppendDefense(3, attacker: attacker, move: this);
                            break;
                        case 322:
                            msg += attacker.AppendSpAtk(3, attacker: attacker, move: this);
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
                                Func<int, DuelPokemon, Move, string, bool, string> statRaiseFunc =
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
                                msg += attacker.AppendAttack(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpAtk(1, attacker: attacker, move: this);
                            }
                            else
                            {
                                msg += attacker.AppendDefense(1, attacker: attacker, move: this);
                                msg += attacker.AppendSpDef(1, attacker: attacker, move: this);
                            }

                            break;
                        }
                        case 485:
                            msg += attacker.Damage(attacker.StartingHp / 2, battle);
                            msg += attacker.AppendAttack(2, attacker: attacker, move: this);
                            msg += attacker.AppendSpAtk(2, attacker: attacker, move: this);
                            msg += attacker.AppendSpeed(2, attacker: attacker, move: this);
                            break;
                    }

                    // Flinch
                    if (!defender.HasMoved)
                    {
                        for (var hit = 0; hit < numHits; hit++)
                        {
                            if (defender.Flinched)
                            {
                                break;
                            }

                            if (new[] { 32, 76, 93, 147, 151, 159, 274, 275, 276, 425, 475, 501 }.Contains(Effect) &&
                                effectChance.HasValue)
                            {
                                if (new Random().Next(1, 101) <= effectChance)
                                {
                                    msg += defender.Flinch(move: this, attacker: attacker);
                                }
                            }
                            else if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                            {
                                if (attacker.Ability() == Ability.STENCH)
                                {
                                    if (new Random().Next(1, 101) <= 10)
                                    {
                                        msg += defender.Flinch(move: this, attacker: attacker, source: "its stench");
                                    }
                                }
                                else if (attacker.HeldItem == "kings-rock")
                                {
                                    if (new Random().Next(1, 101) <= 10)
                                    {
                                        msg += defender.Flinch(move: this, attacker: attacker, source: "its kings rock");
                                    }
                                }
                                else if (attacker.HeldItem == "razor-fang")
                                {
                                    if (new Random().Next(1, 101) <= 10)
                                    {
                                        msg += defender.Flinch(move: this, attacker: attacker, source: "its razor fang");
                                    }
                                }
                            }
                        }
                    }

                    switch (Effect)
                    {
                        // Move locking
                        case 87:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                            if (defender.Ability(attacker: attacker, move: this) == Ability.OBLIVIOUS)
                            {
                                msg += $"{defender.Name} is too oblivious to be taunted!\n";
                            }
                            else if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
                            {
                                msg += $"{defender.Name}'s aroma veil protects it from being taunted!\n";
                            }
                            else
                            {
                                if (defender.HasMoved)
                                {
                                    defender.Taunt.SetTurns(4);
                                }
                                else
                                {
                                    defender.Taunt.SetTurns(3);
                                }

                                msg += $"{defender.Name} is being taunted!\n";
                            }

                            break;
                        }
                        case 91:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
                            {
                                msg += $"{defender.Name}'s aroma veil protects it from being encored!\n";
                            }
                            else
                            {
                                defender.Encore.Set(defender.LastMove, 4);
                                if (!defender.HasMoved)
                                {
                                    defender.Owner.SelectedAction = new Trainer.MoveAction(defender.LastMove);
                                }

                                msg += $"{defender.Name} is giving an encore!\n";
                            }

                            break;
                        }
                        case 166:
                        {
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                            if (defender.Ability(attacker: attacker, move: this) == Ability.AROMA_VEIL)
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
                    {
                        msg += attacker.Damage(attacker.StartingHp / 10, battle, source: "its life orb");
                    }

                    // Swap outs
                    // A poke is force-swapped out before activating red-card
                    if (Effect is 29 or 314)
                    {
                        var swaps = defender.Owner.ValidSwaps(attacker, battle, checkTrap: false);
                        if (swaps.Count == 0)
                        {
                            // Do nothing
                        }
                        else if (defender.Ability(attacker: attacker, move: this) == Ability.SUCTION_CUPS)
                        {
                            msg += $"{defender.Name}'s suction cups kept it in place!\n";
                        }
                        else if (defender.Ability(attacker: attacker, move: this) == Ability.GUARD_DOG)
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
                            defender.Owner.SwitchPoke(idx, midTurn: true);
                            msg += defender.Owner.CurrentPokemon.SendOut(attacker, battle);
                            // Safety in case the poke dies on send out.
                            if (defender.Owner.CurrentPokemon != null)
                            {
                                defender.Owner.CurrentPokemon.HasMoved = true;
                            }
                        }
                    }
                    // A red-card forces the attacker to swap to a random poke, even if they used a switch out move
                    else if (defender.HeldItem == "red-card" && defender.Hp > 0 && DamageClass != DamageClass.STATUS)
                    {
                        var swaps = attacker.Owner.ValidSwaps(defender, battle, checkTrap: false);
                        if (swaps.Count == 0)
                        {
                            // Do nothing
                        }
                        else if (attacker.Ability(attacker: defender, move: this) == Ability.SUCTION_CUPS)
                        {
                            msg += $"{attacker.Name}'s suction cups kept it in place from {defender.Name}'s red card!\n";
                            defender.HeldItem.Use();
                        }
                        else if (attacker.Ability(attacker: defender, move: this) == Ability.GUARD_DOG)
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
                            attacker.Owner.SwitchPoke(idx, midTurn: true);
                            msg += attacker.Owner.CurrentPokemon.SendOut(defender, battle);
                            // Safety in case the poke dies on send out.
                            if (attacker.Owner.CurrentPokemon != null)
                            {
                                attacker.Owner.CurrentPokemon.HasMoved = true;
                            }
                        }
                    }
                    else if (new[] { 128, 154, 229, 347 }.Contains(Effect))
                    {
                        var swaps = attacker.Owner.ValidSwaps(defender, battle, checkTrap: false);
                        if (swaps.Count > 0)
                        {
                            msg += $"{attacker.Name} went back!\n";
                            if (Effect == 128)
                            {
                                attacker.Owner.BatonPass = new BatonPass(attacker);
                            }

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
                    if (new[] { 169, 221, 271, 321 }.Contains(Effect))
                    {
                        msg += attacker.Faint(battle);
                    }

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
                            foreach (var poke in attacker.Owner.Party)
                            {
                                poke.NonVolatileEffect.Reset();
                            }

                            msg +=
                                $"A bell chimed, and all of {attacker.Owner.Name}'s pokemon had status conditions removed!\n";
                            break;
                        }
                        // Psycho Shift
                        case 235:
                        {
                            var transferedStatus = attacker.NonVolatileEffect.Current;
                            msg += defender.NonVolatileEffect.ApplyStatus(transferedStatus, battle, attacker: attacker,
                                move: this);
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
                            {
                                msg += $"{attacker.Name} is already under the effect of perish song!\n";
                            }
                            else
                            {
                                attacker.PerishSong.SetTurns(4);
                            }

                            if (defender.PerishSong.Active())
                            {
                                msg += $"{defender.Name} is already under the effect of perish song!\n";
                            }
                            else if (defender.Ability(attacker: attacker, move: this) == Ability.SOUNDPROOF)
                            {
                                msg += $"{defender.Name}'s soundproof protects it from hearing the song!\n";
                            }
                            else
                            {
                                defender.PerishSong.SetTurns(4);
                            }

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
                            if (!Enum.IsDefined(typeof(ElementType), t))
                            {
                                t = ElementType.NORMAL;
                            }

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
                            if (defender.NonVolatileEffect.Sleep())
                            {
                                defender.NonVolatileEffect.Reset();
                            }

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
                            {
                                attacker.Owner.AuroraVeil.SetTurns(8);
                            }
                            else
                            {
                                attacker.Owner.AuroraVeil.SetTurns(5);
                            }

                            msg += $"{attacker.Name} put up its aurora veil!\n";
                            break;
                        }
                        // Light Screen
                        case 36 or 421:
                        {
                            if (attacker.HeldItem == "light-clay")
                            {
                                attacker.Owner.LightScreen.SetTurns(8);
                            }
                            else
                            {
                                attacker.Owner.LightScreen.SetTurns(5);
                            }

                            msg += $"{attacker.Name} put up its light screen!\n";
                            break;
                        }
                        // Reflect
                        case 66 or 422:
                        {
                            if (attacker.HeldItem == "light-clay")
                            {
                                attacker.Owner.Reflect.SetTurns(8);
                            }
                            else
                            {
                                attacker.Owner.Reflect.SetTurns(5);
                            }

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
                            {
                                defender.Bind.SetTurns(7);
                            }
                            else
                            {
                                defender.Bind.SetTurns(new Random().Next(4, 6));
                            }

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
                            msg += defender.Infatuate(attacker, move: this);
                            break;
                        // Heart Swap
                        case 251:
                        {
                            (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                            (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                            (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                            (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                            (attacker.SpeedStage, defender.SpeedStage) = (defender.SpeedStage, attacker.SpeedStage);

                            (attacker.AccuracyStage, defender.AccuracyStage) = (defender.AccuracyStage, attacker.AccuracyStage);

                            (attacker.EvasionStage, defender.EvasionStage) = (defender.EvasionStage, attacker.EvasionStage);

                            msg += $"{attacker.Name} switched stat changes with {defender.Name}!\n";
                            break;
                        }
                        // Power Swap
                        case 244:
                        {
                            (attacker.AttackStage, defender.AttackStage) = (defender.AttackStage, attacker.AttackStage);

                            (attacker.SpAtkStage, defender.SpAtkStage) = (defender.SpAtkStage, attacker.SpAtkStage);

                            msg += $"{attacker.Name} switched attack and special attack stat changes with {defender.Name}!\n";
                            break;
                        }
                        // Guard Swap
                        case 245:
                        {
                            (attacker.DefenseStage, defender.DefenseStage) = (defender.DefenseStage, attacker.DefenseStage);

                            (attacker.SpDefStage, defender.SpDefStage) = (defender.SpDefStage, attacker.SpDefStage);

                            msg += $"{attacker.Name} switched defense and special defense stat changes with {defender.Name}!\n";
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

                            if (!defender.Grounded(battle, attacker: attacker, move: this))
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
                            {
                                msg += attacker.AppendAttack(1, attacker: attacker, source: "its wind rider");
                            }

                            break;
                        }
                    }

                    break;
                }
            }

            return msg;
        }

        /// <summary>
        /// Calculates the element type this move will be.
        /// </summary>
        /// <returns>The element type of this move, taking into account abilities and effects.</returns>
        public ElementType GetType(DuelPokemon attacker, DuelPokemon defender, Battle battle)
        {
            // Abilities are first because those are intrinsic to the poke and would "apply" to the move first
            if (attacker.Ability() == Ability.REFRIGERATE && Type == ElementType.NORMAL)
            {
                return ElementType.ICE;
            }

            if (attacker.Ability() == Ability.PIXILATE && Type == ElementType.NORMAL)
            {
                return ElementType.FAIRY;
            }

            if (attacker.Ability() == Ability.AERILATE && Type == ElementType.NORMAL)
            {
                return ElementType.FLYING;
            }

            if (attacker.Ability() == Ability.GALVANIZE && Type == ElementType.NORMAL)
            {
                return ElementType.ELECTRIC;
            }

            if (attacker.Ability() == Ability.NORMALIZE)
            {
                return ElementType.NORMAL;
            }

            if (attacker.Ability() == Ability.LIQUID_VOICE && IsSoundBased())
            {
                return ElementType.WATER;
            }

            if (Type == ElementType.NORMAL && (attacker.IonDeluge || defender.IonDeluge || battle.PlasmaFists))
            {
                return ElementType.ELECTRIC;
            }

            if (attacker.Electrify)
            {
                return ElementType.ELECTRIC;
            }

            switch (Effect)
            {
                case 204:
                {
                    if (battle.Weather.Get() == "hail")
                    {
                        return ElementType.ICE;
                    }

                    if (battle.Weather.Get() == "sandstorm")
                    {
                        return ElementType.ROCK;
                    }

                    if (new[] { "h-sun", "sun" }.Contains(battle.Weather.Get()))
                    {
                        return ElementType.FIRE;
                    }

                    if (new[] { "h-rain", "rain" }.Contains(battle.Weather.Get()))
                    {
                        return ElementType.WATER;
                    }

                    break;
                }
                case 136:
                {
                    // Uses starting IVs as its own IVs should be used even if transformed
                    var typeIdx = attacker.StartingHpIV % 2;
                    typeIdx += 2 * (attacker.StartingAtkIV % 2);
                    typeIdx += 4 * (attacker.StartingDefIV % 2);
                    typeIdx += 8 * (attacker.StartingSpeedIV % 2);
                    typeIdx += 16 * (attacker.StartingSpAtkIV % 2);
                    typeIdx += 32 * (attacker.StartingSpDefIV % 2);
                    typeIdx = typeIdx * 15 / 63;
                    var typeOptions = new[]
                    {
                        ElementType.FIGHTING,
                        ElementType.FLYING,
                        ElementType.POISON,
                        ElementType.GROUND,
                        ElementType.ROCK,
                        ElementType.BUG,
                        ElementType.GHOST,
                        ElementType.STEEL,
                        ElementType.FIRE,
                        ElementType.WATER,
                        ElementType.GRASS,
                        ElementType.ELECTRIC,
                        ElementType.PSYCHIC,
                        ElementType.ICE,
                        ElementType.DRAGON,
                        ElementType.DARK
                    };
                    return typeOptions[typeIdx];
                }
                case 401:
                {
                    if (attacker.TypeIds.Count == 0)
                    {
                        return ElementType.TYPELESS;
                    }

                    return attacker.TypeIds[0];
                }
                case 269:
                {
                    if (attacker.HeldItem == "draco-plate" || attacker.HeldItem == "dragon-memory")
                    {
                        return ElementType.DRAGON;
                    }

                    if (attacker.HeldItem == "dread-plate" || attacker.HeldItem == "dark-memory")
                    {
                        return ElementType.DARK;
                    }

                    if (attacker.HeldItem == "earth-plate" || attacker.HeldItem == "ground-memory")
                    {
                        return ElementType.GROUND;
                    }

                    if (attacker.HeldItem == "fist-plate" || attacker.HeldItem == "fighting-memory")
                    {
                        return ElementType.FIGHTING;
                    }

                    if (attacker.HeldItem == "flame-plate" || attacker.HeldItem == "burn-drive" ||
                        attacker.HeldItem == "fire-memory")
                    {
                        return ElementType.FIRE;
                    }

                    if (attacker.HeldItem == "icicle-plate" || attacker.HeldItem == "chill-drive" ||
                        attacker.HeldItem == "ice-memory")
                    {
                        return ElementType.ICE;
                    }

                    if (attacker.HeldItem == "insect-plate" || attacker.HeldItem == "bug-memory")
                    {
                        return ElementType.BUG;
                    }

                    if (attacker.HeldItem == "iron-plate" || attacker.HeldItem == "steel-memory")
                    {
                        return ElementType.STEEL;
                    }

                    if (attacker.HeldItem == "meadow-plate" || attacker.HeldItem == "grass-memory")
                    {
                        return ElementType.GRASS;
                    }

                    if (attacker.HeldItem == "mind-plate" || attacker.HeldItem == "psychic-memory")
                    {
                        return ElementType.PSYCHIC;
                    }

                    if (attacker.HeldItem == "pixie-plate" || attacker.HeldItem == "fairy-memory")
                    {
                        return ElementType.FAIRY;
                    }

                    if (attacker.HeldItem == "sky-plate" || attacker.HeldItem == "flying-memory")
                    {
                        return ElementType.FLYING;
                    }

                    if (attacker.HeldItem == "splash-plate" || attacker.HeldItem == "douse-drive" ||
                        attacker.HeldItem == "water-memory")
                    {
                        return ElementType.WATER;
                    }

                    if (attacker.HeldItem == "spooky-plate" || attacker.HeldItem == "ghost-memory")
                    {
                        return ElementType.GHOST;
                    }

                    if (attacker.HeldItem == "stone-plate" || attacker.HeldItem == "rock-memory")
                    {
                        return ElementType.ROCK;
                    }

                    if (attacker.HeldItem == "toxic-plate" || attacker.HeldItem == "poison-memory")
                    {
                        return ElementType.POISON;
                    }

                    if (attacker.HeldItem == "zap-plate" || attacker.HeldItem == "shock-drive" ||
                        attacker.HeldItem == "electric-memory")
                    {
                        return ElementType.ELECTRIC;
                    }

                    break;
                }
                case 223:
                {
                    var hi = attacker.HeldItem.Get();
                    if (new[] { "figy-berry", "tanga-berry", "cornn-berry", "enigma-berry" }.Contains(hi))
                    {
                        return ElementType.BUG;
                    }

                    if (new[] { "iapapa-berry", "colbur-berry", "spelon-berry", "rowap-berry", "maranga-berry" }
                        .Contains(hi))
                    {
                        return ElementType.DARK;
                    }

                    if (new[] { "aguav-berry", "haban-berry", "nomel-berry", "jaboca-berry" }.Contains(hi))
                    {
                        return ElementType.DRAGON;
                    }

                    if (new[] { "pecha-berry", "wacan-berry", "wepear-berry", "belue-berry" }.Contains(hi))
                    {
                        return ElementType.ELECTRIC;
                    }

                    if (new[] { "roseli-berry", "kee-berry" }.Contains(hi))
                    {
                        return ElementType.FAIRY;
                    }

                    if (new[] { "leppa-berry", "chople-berry", "kelpsy-berry", "salac-berry" }.Contains(hi))
                    {
                        return ElementType.FIGHTING;
                    }

                    if (new[] { "cheri-berry", "occa-berry", "bluk-berry", "watmel-berry" }.Contains(hi))
                    {
                        return ElementType.FIRE;
                    }

                    if (new[] { "lum-berry", "coba-berry", "grepa-berry", "lansat-berry" }.Contains(hi))
                    {
                        return ElementType.FLYING;
                    }

                    if (new[] { "mago-berry", "kasib-berry", "rabuta-berry", "custap-berry" }.Contains(hi))
                    {
                        return ElementType.GHOST;
                    }

                    if (new[] { "rawst-berry", "rindo-berry", "pinap-berry", "liechi-berry" }.Contains(hi))
                    {
                        return ElementType.GRASS;
                    }

                    if (new[] { "persim-berry", "shuca-berry", "hondew-berry", "apicot-berry" }.Contains(hi))
                    {
                        return ElementType.GROUND;
                    }

                    if (new[] { "aspear-berry", "yache-berry", "pomeg-berry", "ganlon-berry" }.Contains(hi))
                    {
                        return ElementType.ICE;
                    }

                    if (new[] { "oran-berry", "kebia-berry", "qualot-berry", "petaya-berry" }.Contains(hi))
                    {
                        return ElementType.POISON;
                    }

                    if (new[] { "sitrus-berry", "payapa-berry", "tamato-berry", "starf-berry" }.Contains(hi))
                    {
                        return ElementType.PSYCHIC;
                    }

                    if (new[] { "wiki-berry", "charti-berry", "magost-berry", "micle-berry" }.Contains(hi))
                    {
                        return ElementType.ROCK;
                    }

                    if (new[] { "razz-berry", "babiri-berry", "pamtre-berry" }.Contains(hi))
                    {
                        return ElementType.STEEL;
                    }

                    if (new[] { "chesto-berry", "passho-berry", "nanab-berry", "durin-berry" }.Contains(hi))
                    {
                        return ElementType.WATER;
                    }

                    if (hi == "chilan-berry")
                    {
                        return ElementType.NORMAL;
                    }

                    break;
                }
                case 433 when attacker.Name == "Morpeko-hangry":
                    return ElementType.DARK;
                case 441 when attacker.Grounded(battle):
                {
                    switch (battle.Terrain.Item?.ToString())
                    {
                        case "electric":
                            return ElementType.ELECTRIC;
                        case "grass":
                            return ElementType.GRASS;
                        case "misty":
                            return ElementType.FAIRY;
                        case "psychic":
                            return ElementType.PSYCHIC;
                    }

                    break;
                }
            }

            if (Id == 873)
            {
                switch (attacker.Name)
                {
                    case "Tauros-paldea":
                        return ElementType.FIGHTING;
                    case "Tauros-aqua-paldea":
                        return ElementType.WATER;
                    case "Tauros-blaze-paldea":
                        return ElementType.FIRE;
                }
            }

            return Type;
        }

        /// <summary>
        /// Calculates the priority value for this move.
        /// </summary>
        /// <returns>An int priority from -7 to 5.</returns>
        public int GetPriority(DuelPokemon attacker, DuelPokemon defender, Battle battle)
        {
            var priority = Priority;
            var currentType = GetType(attacker, defender, battle);

            if (Effect == 437 && attacker.Grounded(battle) && battle.Terrain.Item?.ToString() == "grassy")
            {
                priority += 1;
            }

            if (attacker.Ability() == Ability.GALE_WINGS && currentType == ElementType.FLYING &&
                attacker.Hp == attacker.StartingHp)
            {
                priority += 1;
            }

            if (attacker.Ability() == Ability.PRANKSTER && DamageClass == DamageClass.STATUS)
            {
                priority += 1;
            }

            if (attacker.Ability() == Ability.TRIAGE && IsAffectedByHealBlock())
            {
                priority += 3;
            }

            return priority;
        }

        /// <summary>
        /// Gets the chance for secondary effects to occur.
        /// </summary>
        /// <returns>An int from 0-100.</returns>
        public int? GetEffectChance(DuelPokemon attacker, DuelPokemon defender, Battle battle)
        {
            if (EffectChance == null)
            {
                return 100;
            }

            if (defender.Ability(attacker: attacker, move: this) == Ability.SHIELD_DUST)
            {
                return 0;
            }

            if (defender.HeldItem == "covert-cloak")
            {
                return 0;
            }

            if (attacker.Ability() == Ability.SHEER_FORCE)
            {
                return 0;
            }

            if (attacker.Ability() == Ability.SERENE_GRACE)
            {
                return Math.Min(100, EffectChance.Value * 2);
            }

            return EffectChance;
        }

        /// <summary>
        /// Returns True if the move can be executed, False otherwise.
        /// Checks different requirements for moves that can make them fail.
        /// </summary>
        public bool CheckExecutable(DuelPokemon attacker, DuelPokemon defender, Battle battle)
        {
            if (attacker.Taunt.Active() && DamageClass == DamageClass.STATUS)
            {
                return false;
            }

            if (attacker.Silenced.Active() && IsSoundBased())
            {
                return false;
            }

            if (IsAffectedByHealBlock() && attacker.HealBlock.Active())
            {
                return false;
            }

            if (IsPowderOrSpore() && (defender.TypeIds.Contains(ElementType.GRASS) ||
                                      defender.Ability(attacker: attacker, move: this) == Ability.OVERCOAT ||
                                      defender.HeldItem == "safety-goggles"))
            {
                return false;
            }

            if (battle.Weather.Get() == "h-sun" && GetType(attacker, defender, battle) == ElementType.WATER &&
                DamageClass != DamageClass.STATUS)
            {
                return false;
            }

            if (battle.Weather.Get() == "h-rain" && GetType(attacker, defender, battle) == ElementType.FIRE &&
                DamageClass != DamageClass.STATUS)
            {
                return false;
            }

            if (attacker.Disable.Active() && attacker.Disable.Item == this)
            {
                return false;
            }

            if (attacker != defender && defender.Imprison && defender.Moves.Any(m => m.Id == Id))
            {
                return false;
            }

            // Since we only have single battles, these moves always fail
            if (new[] { 173, 301, 308, 316, 363, 445, 494 }.Contains(Effect))
            {
                return false;
            }

            if (new[] { 93, 98 }.Contains(Effect) && !attacker.NonVolatileEffect.Sleep())
            {
                return false;
            }

            if (new[] { 9, 108 }.Contains(Effect) && !defender.NonVolatileEffect.Sleep())
            {
                return false;
            }

            if (Effect == 364 && !defender.NonVolatileEffect.Poison())
            {
                return false;
            }

            if (new[] { 162, 163 }.Contains(Effect) && attacker.Stockpile == 0)
            {
                return false;
            }

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
            {
                return false;
            }

            switch (Effect)
            {
                case 176 when defender.Taunt.Active():
                case 29 when !defender.Owner.ValidSwaps(attacker, battle, checkTrap: false).Any():
                    return false;
            }

            if (new[] { 128, 154, 493 }.Contains(Effect) &&
                !attacker.Owner.ValidSwaps(defender, battle, checkTrap: false).Any())
            {
                return false;
            }

            if (Effect == 161 && attacker.Stockpile >= 3)
            {
                return false;
            }

            if (new[] { 90, 145, 228, 408 }.Contains(Effect) && attacker.LastMoveDamage == null)
            {
                return false;
            }

            if (Effect == 145 && attacker.LastMoveDamage.Item2 != DamageClass.SPECIAL)
            {
                return false;
            }

            if (new[] { 90, 408 }.Contains(Effect) && attacker.LastMoveDamage.Item2 != DamageClass.PHYSICAL)
            {
                return false;
            }

            if (new[] { 10, 243 }.Contains(Effect) &&
                (defender.LastMove == null || !defender.LastMove.SelectableByMirrorMove()))
            {
                return false;
            }

            switch (Effect)
            {
                case 83 when defender.LastMove == null || !defender.LastMove.SelectableByMimic():
                case 180 when attacker.Owner.Wish.Active():
                case 388 when defender.AttackStage == -6:
                    return false;
            }

            if (new[] { 143, 485, 493 }.Contains(Effect) && attacker.Hp <= attacker.StartingHp / 2)
            {
                return false;
            }

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
                                                        defender.Ability(attacker: attacker, move: this) ==
                                                        Ability.OBLIVIOUS))
            {
                return false;
            }

            if (new[] { 367, 392 }.Contains(Effect) &&
                !new[] { Ability.PLUS, Ability.MINUS }.Contains(attacker.Ability()))
            {
                return false;
            }

            if (Effect == 39 && attacker.Level < defender.Level)
            {
                return false;
            }

            if (new[] { 46, 86, 156, 264, 286 }.Contains(Effect) && battle.Gravity.Active())
            {
                return false;
            }

            switch (Effect)
            {
                case 113 when defender.Owner.Spikes == 3:
                case 250 when defender.Owner.ToxicSpikes == 2:
                    return false;
            }

            if (new[] { 159, 377, 383 }.Contains(Effect) && attacker.ActiveTurns != 0)
            {
                return false;
            }

            switch (Effect)
            {
                case 98 when !attacker.Moves.Any(m => m.SelectableBySleepTalk()):
                case 407 when battle.Weather.Get() != "hail":
                case 407 when attacker.Owner.AuroraVeil.Active():
                case 47 when attacker.Owner.Mist.Active():
                    return false;
            }

            if (new[] { 80, 493 }.Contains(Effect) && attacker.Substitute > 0)
            {
                return false;
            }

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
                case 248 when defender.Ability(attacker: attacker, move: this) == Ability.INSOMNIA:
                    return false;
            }

            if (new[] { 242, 249 }.Contains(Effect) && (defender.HasMoved || defender.Owner.SelectedAction.IsSwitch ||
                                                        defender.Owner.SelectedAction is Trainer.MoveAction
                                                        {
                                                            Move.DamageClass: DamageClass.STATUS
                                                        }))
            {
                return false;
            }

            switch (Effect)
            {
                case 252 when attacker.AquaRing:
                case 253 when attacker.MagnetRise.Active():
                case 221 when attacker.Owner.HealingWish:
                case 271 when attacker.Owner.LunarDance:
                    return false;
            }

            if (new[] { 240, 248, 299, 300 }.Contains(Effect) && !defender.AbilityChangeable())
            {
                return false;
            }

            switch (Effect)
            {
                case 300 when !attacker.AbilityGiveable():
                case 241 when attacker.LuckyChant.Active():
                case 125 when attacker.Owner.Safeguard.Active():
                case 293 when !attacker.TypeIds.Intersect(defender.TypeIds).Any():
                case 295 when defender.Ability(attacker: attacker, move: this) == Ability.MULTITYPE:
                case 319 when defender.TypeIds.Count == 0:
                case 171 when attacker.LastMoveDamage != null:
                case 179 when !(attacker.AbilityChangeable() && defender.AbilityGiveable()):
                case 181 when attacker.GetAssistMove() == null:
                    return false;
            }

            if (new[] { 112, 117, 184, 195, 196, 279, 307, 345, 350, 354, 356, 362, 378, 384, 454, 488, 499 }
                    .Contains(Effect) &&
                defender.HasMoved)
            {
                return false;
            }

            switch (Effect)
            {
                case 192 when !(attacker.AbilityChangeable() && attacker.AbilityGiveable() &&
                                defender.AbilityChangeable() && defender.AbilityGiveable()):
                case 226 when attacker.Owner.Tailwind.Active():
                    return false;
            }

            if (new[] { 90, 92, 145 }.Contains(Effect) && attacker.Substitute > 0)
            {
                return false;
            }

            if (new[] { 85, 92, 169, 178, 188, 206, 388 }.Contains(Effect) && defender.Substitute > 0)
            {
                return false;
            }

            switch (Effect)
            {
                case 234 when attacker.HeldItem.Power == null || attacker.Ability() == Ability.STICKY_HOLD:
                case 178 when new[] { Ability.STICKY_HOLD }.Contains(attacker.Ability()) ||
                              defender.Ability(attacker: attacker, move: this) == Ability.STICKY_HOLD ||
                              !attacker.HeldItem.CanRemove() || !defender.HeldItem.CanRemove():
                case 202 when attacker.Owner.MudSport.Active():
                case 211 when attacker.Owner.WaterSport.Active():
                case 149 when defender.Owner.FutureSight.Active():
                case 188 when defender.NonVolatileEffect.Current.Any() ||
                              new[] { Ability.INSOMNIA, Ability.VITAL_SPIRIT, Ability.SWEET_VEIL }.Contains(
                                  defender.Ability(attacker: attacker, move: this)) ||
                              defender.Yawn.Active():
                case 188 when battle.Terrain.Item?.ToString() == "electric" && attacker.Grounded(battle):
                    return false;
            }

            if (new[] { 340, 351 }.Contains(Effect) && !new[] { attacker, defender }.Any(p =>
                    p.TypeIds.Contains(ElementType.GRASS) && p.Grounded(battle) &&
                    p is { Dive: false, Dig: false, Fly: false, ShadowForce: false }))
            {
                return false;
            }

            if (Effect == 341 && defender.Owner.StickyWeb)
            {
                return false;
            }

            if (new[] { 112, 117, 356, 362, 384, 454, 488, 499 }.Contains(Effect) &&
                new Random().Next(1, attacker.ProtectionChance + 1) != 1)
            {
                return false;
            }

            switch (Effect)
            {
                case 403 when defender.LastMove == null || defender.LastMove.PP == 0 ||
                              !defender.LastMove.SelectableByInstruct() || defender.LockedMove != null:
                case 378 when defender.TypeIds.Contains(ElementType.GRASS) ||
                              defender.Ability(attacker: attacker, move: this) == Ability.OVERCOAT ||
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
            {
                return false;
            }

            if (new[] { 8, 420, 444 }.Contains(Effect) && new[] { Ability.DAMP }.Contains(attacker.Ability()) ||
                defender.Ability(attacker: attacker, move: this) == Ability.DAMP)
            {
                return false;
            }

            if (new[] { 223, 453 }.Contains(Effect) && !attacker.HeldItem.IsBerry())
            {
                return false;
            }

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
                              defender.Ability(attacker: attacker, move: this) == Ability.RKS_SYSTEM:
                case 83 when !attacker.Moves.Contains(this):
                case 501 when defender.HasMoved || defender.Owner.SelectedAction.IsSwitch ||
                              (defender.Owner.SelectedAction is Trainer.MoveAction selectedAction &&
                               selectedAction.Move.GetPriority(defender, attacker, battle) <= 0):
                    return false;
            }

            if (new[] { Ability.QUEENLY_MAJESTY, Ability.DAZZLING, Ability.ARMOR_TAIL }
                    .Contains(defender.Ability(attacker: attacker, move: this)) &&
                GetPriority(attacker, defender, battle) > 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attacks the defender using this move.
        /// </summary>
        /// <returns>A string of formatted results of this attack and the number of hits this move did.</returns>
        public (string, int) Attack(DuelPokemon attacker, DuelPokemon defender, Battle battle)
        {
            // https://bulbapedia.bulbagarden.net/wiki/Damage
            var msg = "";
            var currentType = GetType(attacker, defender, battle);

            // Move effectiveness
            var effectiveness = defender.Effectiveness(currentType, battle, attacker: attacker, move: this);
            if (Effect == 338)
            {
                effectiveness *= defender.Effectiveness(ElementType.FLYING, battle, attacker: attacker, move: this);
            }

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
                {
                    minHits = maxHits;
                }
                else if (attacker.HeldItem == "loaded-dice" && maxHits >= 4 && (minHits < 4 || Effect == 484))
                {
                    minHits = 4;
                }

                // Randomly select number of hits
                if (minHits == 2 && maxHits == 5)
                {
                    // Weighted random distribution matching the Python implementation
                    var hitChoices = new int[]
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
                if (defender.Hp == 0)
                {
                    break;
                }

                // Explosion faints the user first, but should still do damage after death.
                // Future sight still needs to hit after the attacker dies.
                // Mind blown still needs to hit after the attacker dies.
                if (attacker.Hp == 0 && !new[] { 8, 149, 420, 444 }.Contains(Effect))
                {
                    break;
                }

                // Critical hit chance
                var criticalStage = CritRate;
                if (attacker.HeldItem == "scope-lens" || attacker.HeldItem == "razor-claw")
                {
                    criticalStage += 1;
                }

                if (attacker.Ability() == Ability.SUPER_LUCK)
                {
                    criticalStage += 1;
                }

                if (attacker.FocusEnergy)
                {
                    criticalStage += 2;
                }

                if (attacker.LansatBerryAte)
                {
                    criticalStage += 2;
                }

                criticalStage = Math.Min(criticalStage, 3);

                var critMap = new int[] { 24, 8, 2, 1 };
                var critical = new Random().Next(critMap[criticalStage]) == 0;

                if (attacker.Ability() == Ability.MERCILESS && defender.NonVolatileEffect.Poison())
                {
                    critical = true;
                }

                // Always scores a critical hit.
                if (Effect == 289)
                {
                    critical = true;
                }

                if (attacker.LaserFocus.Active())
                {
                    critical = true;
                }

                if (defender.Ability(attacker: attacker, move: this) == Ability.SHELL_ARMOR ||
                    defender.Ability(attacker: attacker, move: this) == Ability.BATTLE_ARMOR)
                {
                    critical = false;
                }

                if (defender.LuckyChant.Active())
                {
                    critical = false;
                }

                // Confusion never crits
                if (Id == 0xCFCF)
                {
                    critical = false;
                }

                // Stats
                int a, d;
                DamageClass damageClass;

                if (DamageClass == DamageClass.PHYSICAL)
                {
                    damageClass = DamageClass.PHYSICAL;
                    a = attacker.GetAttack(
                        battle,
                        critical: critical,
                        ignoreStages: defender.Ability(attacker: attacker, move: this) == Ability.UNAWARE
                    );

                    if (Effect == 304)
                    {
                        d = defender.GetRawDefense();
                    }
                    else
                    {
                        d = defender.GetDefense(
                            battle,
                            critical: critical,
                            ignoreStages: attacker.Ability() == Ability.UNAWARE,
                            attacker: attacker,
                            move: this
                        );
                    }
                }
                else
                {
                    damageClass = DamageClass.SPECIAL;
                    a = attacker.GetSpAtk(
                        battle,
                        critical: critical,
                        ignoreStages: defender.Ability(attacker: attacker, move: this) == Ability.UNAWARE
                    );

                    if (Effect == 304)
                    {
                        d = defender.GetRawSpDef();
                    }
                    else
                    {
                        d = defender.GetSpDef(
                            battle,
                            critical: critical,
                            ignoreStages: attacker.Ability() == Ability.UNAWARE,
                            attacker: attacker,
                            move: this
                        );
                    }
                }

                switch (Effect)
                {
                    // Always uses defender's defense
                    case 283:
                        d = defender.GetDefense(
                            battle,
                            critical: critical,
                            ignoreStages: attacker.Ability() == Ability.UNAWARE,
                            attacker: attacker,
                            move: this
                        );
                        break;
                    // Use the user's defense instead of attack for the attack stat
                    case 426:
                        // This does not pass critical, otherwise it would crop the wrong direction.
                        a = attacker.GetDefense(
                            battle,
                            ignoreStages: defender.Ability(attacker: attacker, move: this) == Ability.UNAWARE
                        );
                        break;
                    // Use the defender's attacking stat
                    case 298:
                    {
                        if (DamageClass == DamageClass.PHYSICAL)
                        {
                            a = defender.GetAttack(
                                battle,
                                critical: critical,
                                ignoreStages: defender.Ability(attacker: attacker, move: this) == Ability.UNAWARE
                            );
                        }
                        else
                        {
                            a = defender.GetSpAtk(
                                battle,
                                critical: critical,
                                ignoreStages: defender.Ability(attacker: attacker, move: this) == Ability.UNAWARE
                            );
                        }

                        break;
                    }
                    // Use the higher of attack or special attack
                    case 416:
                    {
                        var ignoreStages = defender.Ability(attacker: attacker, move: this) == Ability.UNAWARE;
                        a = Math.Max(
                            attacker.GetAttack(battle, critical: critical, ignoreStages: ignoreStages),
                            attacker.GetSpAtk(battle, critical: critical, ignoreStages: ignoreStages)
                        );
                        break;
                    }
                }

                if (attacker.FlashFire && currentType == ElementType.FIRE)
                {
                    a = (int)(a * 1.5);
                }

                if (defender.Ability(attacker: attacker, move: this) == Ability.THICK_FAT &&
                    currentType is ElementType.FIRE or ElementType.ICE)
                {
                    a = (int)(a * 0.5);
                }

                var power = GetPower(attacker, defender, battle);
                if (power == null)
                {
                    throw new InvalidOperationException($"{Name} has no power and no override.");
                }

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
                    {
                        if (!CheckHit(attacker, defender, battle))
                        {
                            hits = hit;
                            break;
                        }
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
                    {
                        damage *= 2;
                    }
                    else
                    {
                        damage *= 1.5;
                    }
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
                {
                    damage *= .5;
                }

                // Aurora Veil, Light Screen, Reflect do not stack but all reduce incoming damage in some way
                if (!critical && attacker.Ability() != Ability.INFILTRATOR)
                {
                    if (defender.Owner.AuroraVeil.Active())
                    {
                        damage *= .5;
                    }
                    else if (defender.Owner.LightScreen.Active() && damageClass == DamageClass.SPECIAL)
                    {
                        damage *= .5;
                    }
                    else if (defender.Owner.Reflect.Active() && damageClass == DamageClass.PHYSICAL)
                    {
                        damage *= .5;
                    }
                }

                // Moves that do extra damage to minimized pokes
                if (defender.Minimized && Effect == 338)
                {
                    damage *= 2;
                }

                // Fluffy
                if (defender.Ability(attacker: attacker, move: this) == Ability.FLUFFY)
                {
                    if (MakesContact(attacker))
                    {
                        damage *= .5;
                    }

                    if (currentType == ElementType.FIRE)
                    {
                        damage *= 2;
                    }
                }

                // Abilities that change damage
                if (new[] { Ability.FILTER, Ability.PRISM_ARMOR, Ability.SOLID_ROCK }.Contains(
                        defender.Ability(attacker: attacker, move: this)) && effectiveness > 1)
                {
                    damage *= .75;
                }

                if (attacker.Ability() == Ability.NEUROFORCE && effectiveness > 1)
                {
                    damage *= 1.25;
                }

                if (defender.Ability(attacker: attacker, move: this) == Ability.ICE_SCALES &&
                    damageClass == DamageClass.SPECIAL)
                {
                    damage *= .5;
                }

                if (attacker.Ability() == Ability.SNIPER && critical)
                {
                    damage *= 1.5;
                }

                if (attacker.Ability() == Ability.TINTED_LENS && effectiveness < 1)
                {
                    damage *= 2;
                }

                if (attacker.Ability() == Ability.PUNK_ROCK && IsSoundBased())
                {
                    damage *= 1.3;
                }

                if (defender.Ability(attacker: attacker, move: this) == Ability.PUNK_ROCK && IsSoundBased())
                {
                    damage *= .5;
                }

                if (defender.Ability(attacker: attacker, move: this) == Ability.HEATPROOF &&
                    currentType == ElementType.FIRE)
                {
                    damage *= .5;
                }

                if (defender.Ability(attacker: attacker, move: this) == Ability.PURIFYING_SALT &&
                    currentType == ElementType.GHOST)
                {
                    damage *= .5;
                }

                // Aura abilities
                if ((attacker.Ability() == Ability.DARK_AURA ||
                     defender.Ability(attacker: attacker, move: this) == Ability.DARK_AURA) &&
                    currentType == ElementType.DARK)
                {
                    if (attacker.Ability() == Ability.AURA_BREAK ||
                        defender.Ability(attacker: attacker, move: this) == Ability.AURA_BREAK)
                    {
                        damage *= .75;
                    }
                    else
                    {
                        damage *= 4.0 / 3.0;
                    }
                }

                if ((attacker.Ability() == Ability.FAIRY_AURA ||
                     defender.Ability(attacker: attacker, move: this) == Ability.FAIRY_AURA) &&
                    currentType == ElementType.FAIRY)
                {
                    if (attacker.Ability() == Ability.AURA_BREAK ||
                        defender.Ability(attacker: attacker, move: this) == Ability.AURA_BREAK)
                    {
                        damage *= .75;
                    }
                    else
                    {
                        damage *= 4.0 / 3.0;
                    }
                }

                if (defender.Ability(attacker: attacker, move: this) == Ability.DRY_SKIN &&
                    currentType == ElementType.FIRE)
                {
                    damage *= 1.25;
                }

                // Items that change damage
                if (defender.HeldItem == "chilan-berry" && currentType == ElementType.NORMAL)
                {
                    damage *= .5;
                }

                if (attacker.HeldItem == "expert-belt" && effectiveness > 1)
                {
                    damage *= 1.2;
                }

                if (
                    attacker.HeldItem == "life-orb" &&
                    DamageClass != DamageClass.STATUS &&
                    Effect != 149
                )
                {
                    damage *= 1.3;
                }

                if (attacker.HeldItem == "metronome")
                {
                    damage *= attacker.Metronome.GetBuff(Name);
                }

                // Parental bond - adds an extra low power hit
                if (parentalBond && hit > 0)
                {
                    damage *= .25;
                }

                // Reduced damage while at full hp
                if (new[] { Ability.MULTISCALE, Ability.SHADOW_SHIELD }.Contains(defender.Ability(attacker: attacker,
                        move: this)) &&
                    defender.Hp == defender.StartingHp)
                {
                    damage *= .5;
                }

                // Random damage scaling
                damage *= new Random().NextDouble() * (1.0 - 0.85) + 0.85;
                damage = Math.Max(1, (int)damage);

                // Cannot lower the target's HP below 1.
                if (Effect == 102)
                {
                    damage = Math.Min(damage, defender.Hp - 1);
                }

                // Drain ratios
                double? drainHealRatio = null;
                if (new[] { 4, 9, 346, 500 }.Contains(Effect))
                {
                    drainHealRatio = 1.0 / 2.0;
                }
                else if (Effect == 349)
                {
                    drainHealRatio = 3.0 / 4.0;
                }

                // Do the damage
                var (msgadd, actualDamage) = defender._Damage((int)damage, battle, move: this, moveType: currentType,
                    attacker: attacker, critical: critical, drainHealRatio: drainHealRatio);
                msg += msgadd;

                // Recoil
                if (attacker.Ability() != Ability.ROCK_HEAD && defender.Owner.HasAlivePokemon())
                {
                    if (Effect == 49)
                    {
                        msg += attacker.Damage(actualDamage / 4, battle, source: "recoil");
                    }

                    if (new[] { 199, 254, 263, 469 }.Contains(Effect))
                    {
                        msg += attacker.Damage(actualDamage / 3, battle, source: "recoil");
                    }

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
                msg += defender.AppendAttack(2, attacker: defender, move: this, source: "its weakness policy");
                msg += defender.AppendSpAtk(2, attacker: defender, move: this, source: "its weakness policy");
                defender.HeldItem.Use();
            }

            return (msg, hits);
        }

        /// <summary>
        /// Get the power of this move.
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
                    var defWeight = defender.Weight(attacker: attacker, move: this);
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
                    var weightDelta = (double)attacker.Weight() / defender.Weight(attacker: attacker, move: this);
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
                    {
                        power = 100;
                    }
                    else if (new[]
                             {
                                 "cornn-berry", "spelon-berry", "nomel-berry", "wepear-berry", "kelpsy-berry", "bluk-berry",
                                 "grepa-berry", "rabuta-berry", "pinap-berry", "hondew-berry", "pomeg-berry",
                                 "qualot-berry",
                                 "tamato-berry", "magost-berry", "pamtre-berry", "nanab-berry"
                             }.Contains(hi))
                    {
                        power = 90;
                    }
                    else
                    {
                        power = 80;
                    }

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

            if (power == null)
            {
                return null;
            }

            // NOTE: this needs to be first as it only applies to raw power
            if (attacker.Ability() == Ability.TECHNICIAN && power <= 60)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.TOUGH_CLAWS && MakesContact(attacker))
            {
                power = (int)(power * 1.3);
            }

            if (attacker.Ability() == Ability.RIVALRY && "-x" != attacker.Gender && "-x" != defender.Gender)
            {
                if (attacker.Gender == defender.Gender)
                {
                    power = (int)(power * 1.25);
                }
                else
                {
                    power = (int)(power * .75);
                }
            }

            if (attacker.Ability() == Ability.IRON_FIST && IsPunching())
            {
                power = (int)(power * 1.2);
            }

            if (attacker.Ability() == Ability.STRONG_JAW && IsBiting())
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.MEGA_LAUNCHER && IsAuraOrPulse())
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.SHARPNESS && IsSlicing())
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.RECKLESS && new[] { 46, 49, 199, 254, 263, 270 }.Contains(Effect))
            {
                power = (int)(power * 1.2);
            }

            if (attacker.Ability() == Ability.TOXIC_BOOST && DamageClass == DamageClass.PHYSICAL &&
                attacker.NonVolatileEffect.Poison())
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.FLARE_BOOST && DamageClass == DamageClass.SPECIAL &&
                attacker.NonVolatileEffect.Burn())
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.ANALYTIC && defender.HasMoved)
            {
                power = (int)(power * 1.3);
            }

            if (attacker.Ability() == Ability.BATTERY && DamageClass == DamageClass.SPECIAL)
            {
                power = (int)(power * 1.3);
            }

            if (attacker.Ability() == Ability.SHEER_FORCE && EffectChance.HasValue) // Not *perfect* but good enough
            {
                power = (int)(power * 1.3);
            }

            if (attacker.Ability() == Ability.STAKEOUT && defender.SwappedIn)
            {
                power = (int)(power * 2);
            }

            if (attacker.Ability() == Ability.SUPREME_OVERLORD)
            {
                var fainted = attacker.Owner.Party.Count(poke => poke.Hp == 0);
                if (fainted > 0)
                {
                    power = (int)(power * (10 + fainted) / 10.0);
                }
            }

            // Type buffing abilities - Some use naive type because the type is changed.
            if (attacker.Ability() == Ability.AERILATE && Type == ElementType.NORMAL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.Ability() == Ability.PIXILATE && Type == ElementType.NORMAL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.Ability() == Ability.GALVANIZE && Type == ElementType.NORMAL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.Ability() == Ability.REFRIGERATE && Type == ElementType.NORMAL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.Ability() == Ability.DRAGONS_MAW && currentType == ElementType.DRAGON)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.TRANSISTOR && currentType == ElementType.ELECTRIC)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.WATER_BUBBLE && currentType == ElementType.WATER)
            {
                power = (int)(power * 2);
            }

            if (defender.Ability(attacker: attacker, move: this) == Ability.WATER_BUBBLE &&
                currentType == ElementType.FIRE)
            {
                power = (int)(power * .5);
            }

            if (attacker.Ability() == Ability.OVERGROW && currentType == ElementType.GRASS &&
                attacker.Hp <= attacker.StartingHp / 3)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.BLAZE && currentType == ElementType.FIRE &&
                attacker.Hp <= attacker.StartingHp / 3)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.TORRENT && currentType == ElementType.WATER &&
                attacker.Hp <= attacker.StartingHp / 3)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.SWARM && currentType == ElementType.BUG &&
                attacker.Hp <= attacker.StartingHp / 3)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.NORMALIZE && currentType == ElementType.NORMAL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.Ability() == Ability.SAND_FORCE &&
                new[] { ElementType.ROCK, ElementType.GROUND, ElementType.STEEL }.Contains(currentType) &&
                battle.Weather.Get() == "sandstorm")
            {
                power = (int)(power * 1.3);
            }

            if (new[] { Ability.STEELWORKER, Ability.STEELY_SPIRIT }.Contains(attacker.Ability()) &&
                currentType == ElementType.STEEL)
            {
                power = (int)(power * 1.5);
            }

            if (attacker.Ability() == Ability.ROCKY_PAYLOAD && currentType == ElementType.ROCK)
            {
                power = (int)(power * 1.5);
            }

            // Type buffing items
            if (attacker.HeldItem == "black-glasses" && currentType == ElementType.DARK)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "black-belt" && currentType == ElementType.FIGHTING)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "hard-stone" && currentType == ElementType.ROCK)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "magnet" && currentType == ElementType.ELECTRIC)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "mystic-water" && currentType == ElementType.WATER)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "never-melt-ice" && currentType == ElementType.ICE)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "dragon-fang" && currentType == ElementType.DRAGON)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "poison-barb" && currentType == ElementType.POISON)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "charcoal" && currentType == ElementType.FIRE)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "silk-scarf" && currentType == ElementType.NORMAL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "metal-coat" && currentType == ElementType.STEEL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "sharp-beak" && currentType == ElementType.FLYING)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "draco-plate" && currentType == ElementType.DRAGON)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "dread-plate" && currentType == ElementType.DARK)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "earth-plate" && currentType == ElementType.GROUND)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "fist-plate" && currentType == ElementType.FIGHTING)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "flame-plate" && currentType == ElementType.FIRE)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "icicle-plate" && currentType == ElementType.ICE)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "insect-plate" && currentType == ElementType.BUG)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "iron-plate" && currentType == ElementType.STEEL)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "meadow-plate" && currentType == ElementType.GRASS)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "mind-plate" && currentType == ElementType.PSYCHIC)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "pixie-plate" && currentType == ElementType.FAIRY)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "sky-plate" && currentType == ElementType.FLYING)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "splash-plate" && currentType == ElementType.WATER)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "spooky-plate" && currentType == ElementType.GHOST)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "stone-plate" && currentType == ElementType.ROCK)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "toxic-plate" && currentType == ElementType.POISON)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "zap-plate" && currentType == ElementType.ELECTRIC)
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "adamant-orb" &&
                new[] { ElementType.DRAGON, ElementType.STEEL }.Contains(currentType) && attacker.Name == "Dialga")
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "griseous-orb" &&
                new[] { ElementType.DRAGON, ElementType.GHOST }.Contains(currentType) && attacker.Name == "Giratina")
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "soul-dew" &&
                new[] { ElementType.DRAGON, ElementType.PSYCHIC }.Contains(currentType) &&
                new[] { "Latios", "Latias" }.Contains(attacker.Name))
            {
                power = (int)(power * 1.2);
            }

            if (attacker.HeldItem == "lustrous-orb" &&
                new[] { ElementType.DRAGON, ElementType.WATER }.Contains(currentType) && attacker.Name == "Palkia")
            {
                power = (int)(power * 1.2);
            }

            // Damage class buffing items
            if (attacker.HeldItem == "wise-glasses" && DamageClass == DamageClass.SPECIAL)
            {
                power = (int)(power * 1.1);
            }

            if (attacker.HeldItem == "muscle-band" && DamageClass == DamageClass.PHYSICAL)
            {
                power = (int)(power * 1.1);
            }

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
            if (new[] { 284, 461 }.Contains(Effect) && defender.NonVolatileEffect.Poison())
            {
                power = (int)(power * 2);
            }

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
            if (new[] { 258, 262 }.Contains(Effect) && defender.Dive)
            {
                power = (int)(power * 2);
            }

            // Has double power if the target is in the first turn of dig.
            if (new[] { 127, 148 }.Contains(Effect) && defender.Dig)
            {
                power = (int)(power * 2);
            }

            // Has double power if the target is in the first turn of bounce or fly.
            if (new[] { 147, 150 }.Contains(Effect) && defender.Fly)
            {
                power = (int)(power * 2);
            }

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
            {
                power = (int)(power * 1.5);
            }

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
                              defender.Grounded(battle, attacker: attacker, move: this):
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
                case 482 when defender.Effectiveness(currentType, battle, attacker: attacker, move: this) > 1:
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
                    defender.Grounded(battle, attacker: attacker, move: this) && new[] { 89, 222, 523 }.Contains(Id):
                    power = (int)(power * 0.5);
                    break;
                case "electric" when attacker.Grounded(battle) &&
                                     currentType == ElementType.ELECTRIC:
                    power = (int)(power * 1.3);
                    break;
                case "misty" when
                    defender.Grounded(battle, attacker: attacker, move: this) && currentType == ElementType.DRAGON:
                    power = (int)(power * 0.5);
                    break;
            }

            // Power buffing statuses
            if (attacker.Charge.Active() && currentType == ElementType.ELECTRIC)
            {
                power = (int)(power * 2);
            }

            if ((attacker.Owner.MudSport.Active() || defender.Owner.MudSport.Active()) &&
                currentType == ElementType.ELECTRIC)
            {
                power = power.Value / 3;
            }

            if ((attacker.Owner.WaterSport.Active() || defender.Owner.WaterSport.Active()) &&
                currentType == ElementType.FIRE)
            {
                power = power.Value / 3;
            }

            return power;
        }

        /// <summary>
        /// Checks if this move hits a semi-invulnerable pokemon.
        /// </summary>
        /// <returns>True if this move hits, False otherwise.</returns>
        public bool CheckSemiInvulnerable(DuelPokemon attacker, DuelPokemon defender, Battle battle)
        {
            if (!TargetsOpponent())
            {
                return true;
            }

            if (attacker.Ability() == Ability.NO_GUARD ||
                defender.Ability(attacker: attacker, move: this) == Ability.NO_GUARD)
            {
                return true;
            }

            if (defender.MindReader.Active() && defender.MindReader.Item == attacker)
            {
                return true;
            }

            if (defender.Dive && !new[] { 258, 262 }.Contains(Effect))
            {
                return false;
            }

            if (defender.Dig && !new[] { 127, 148 }.Contains(Effect))
            {
                return false;
            }

            if (defender.Fly && !new[] { 147, 150, 153, 208, 288, 334, 373 }.Contains(Effect))
            {
                return false;
            }

            if (defender.ShadowForce)
            {
                return false;
            }

            return true;
        }

    /// <summary>
    /// Checks if the move hits through protection effects.
    /// </summary>
    /// <returns>A tuple (boolean, string) where the boolean indicates if the move hits, and the string is a message to add to the battle log.</returns>
    public (bool, string) CheckProtect(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        var msg = "";
        // Moves that don't target the opponent can't be protected by the target.
        if (!TargetsOpponent())
        {
            return (true, msg);
        }
        // Moves which bypass all protection.
        if (new[] { 149, 224, 273, 360, 438, 489 }.Contains(Effect))
        {
            return (true, msg);
        }
        if (attacker.Ability() == Ability.UNSEEN_FIST && MakesContact(attacker))
        {
            return (true, msg);
        }
        if (defender.CraftyShield && DamageClass == DamageClass.STATUS)
        {
            return (false, msg);
        }
        // Moves which bypass all protection except for crafty shield.
        if (new[] { 29, 107, 179, 412 }.Contains(Effect))
        {
            return (true, msg);
        }
        if (defender.Protect)
        {
            return (false, msg);
        }
        if (defender.SpikyShield)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
            {
                msg += attacker.Damage(attacker.StartingHp / 8, battle, source: $"{defender.Name}'s spiky shield");
            }
            return (false, msg);
        }
        if (defender.BanefulBunker)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
            {
                msg += attacker.NonVolatileEffect.ApplyStatus("poison", battle, attacker: defender, source: $"{defender.Name}'s baneful bunker");
            }
            return (false, msg);
        }
        if (defender.WideGuard && TargetsMultiple())
        {
            return (false, msg);
        }
        if (GetPriority(attacker, defender, battle) > 0 && battle.Terrain.Item?.ToString() == "psychic" &&
            defender.Grounded(battle, attacker: attacker, move: this))
        {
            return (false, msg);
        }
        if (defender.MatBlock && DamageClass != DamageClass.STATUS)
        {
            return (false, msg);
        }
        if (defender.KingShield && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
            {
                msg += attacker.AppendAttack(-1, attacker: defender, move: this, source: $"{defender.Name}'s king shield");
            }
            return (false, msg);
        }
        if (defender.Obstruct && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
            {
                msg += attacker.AppendDefense(-2, attacker: defender, move: this, source: $"{defender.Name}'s obstruct");
            }
            return (false, msg);
        }
        if (defender.SilkTrap && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
            {
                msg += attacker.AppendSpeed(-1, attacker: defender, move: this, source: $"{defender.Name}'s silk trap");
            }
            return (false, msg);
        }
        if (defender.BurningBulwark && DamageClass != DamageClass.STATUS)
        {
            if (MakesContact(attacker) && attacker.HeldItem != "protective-pads")
            {
                msg += attacker.NonVolatileEffect.ApplyStatus("burn", battle, attacker: defender, source: $"{defender.Name}'s burning bulwark");
            }
            return (false, msg);
        }
        if (defender.QuickGuard && GetPriority(attacker, defender, battle) > 0)
        {
            return (false, msg);
        }

        return (true, msg);
    }

    /// <summary>
    /// Checks if this move hits based on accuracy.
    /// </summary>
    /// <returns>True if this move hits, False otherwise.</returns>
    public bool CheckHit(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        var micleBerryUsed = attacker.MicleBerryAte;
        attacker.MicleBerryAte = false;

        // Moves that have a null accuracy always hit.
        if (Accuracy == null)
        {
            return true;
        }

        // During hail, this bypasses accuracy checks
        if (Effect == 261 && battle.Weather.Get() == "hail")
        {
            return true;
        }
        // During rain, this bypasses accuracy checks
        if (new[] { 153, 334, 357, 365, 396 }.Contains(Effect) && new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()))
        {
            return true;
        }

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
            if (defender.MindReader.Active() && defender.MindReader.Item == attacker)
            {
                return true;
            }
            if (attacker.Ability() == Ability.NO_GUARD)
            {
                return true;
            }
            if (defender.Ability(attacker: attacker, move: this) == Ability.NO_GUARD)
            {
                return true;
            }
        }

        // OHKO moves
        if (Effect == 39)
        {
            var attackerLevel = 30 + (attacker.Level - defender.Level);
            return new Random().NextDouble() * 100 <= attackerLevel;
        }

        // This does NOT allow OHKO moves to bypass accuracy checks
        if (attacker.Telekinesis.Active())
        {
            return true;
        }

        double accuracy = Accuracy.Value;
        // When used during harsh sunlight, this has an accuracy of 50%
        if (new[] { 153, 334 }.Contains(Effect) && new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
        {
            accuracy = 50;
        }
        if (TargetsOpponent())
        {
            if (defender.Ability(attacker: attacker, move: this) == Ability.WONDER_SKIN && DamageClass == DamageClass.STATUS)
            {
                accuracy = 50;
            }
        }

        var stage = defender.Ability(attacker: attacker, move: this) == Ability.UNAWARE
            ? 0
            : attacker.GetAccuracy(battle);

        if (!(
            Effect == 304 ||
            defender.Foresight ||
            defender.MiracleEye ||
            new[] { Ability.UNAWARE, Ability.KEEN_EYE, Ability.MINDS_EYE }.Contains(attacker.Ability())
        ))
        {
            stage -= defender.GetEvasion(battle);
        }

        stage = Math.Min(6, Math.Max(-6, stage));
        var stageMultiplier = new double[]
        {
            3.0/9.0, 3.0/8.0, 3.0/7.0, 3.0/6.0, 3.0/5.0, 3.0/4.0,
            1.0,
            4.0/3.0, 5.0/3.0, 2.0, 7.0/3.0, 8.0/3.0, 3.0
        };

        accuracy *= stageMultiplier[stage + 6];

        if (TargetsOpponent())
        {
            if (defender.Ability(attacker: attacker, move: this) == Ability.TANGLED_FEET && defender.Confusion.Active())
            {
                accuracy *= 0.5;
            }
            if (defender.Ability(attacker: attacker, move: this) == Ability.SAND_VEIL && battle.Weather.Get() == "sandstorm")
            {
                accuracy *= 0.8;
            }
            if (defender.Ability(attacker: attacker, move: this) == Ability.SNOW_CLOAK && battle.Weather.Get() == "hail")
            {
                accuracy *= 0.8;
            }
        }

        if (attacker.Ability() == Ability.COMPOUND_EYES)
        {
            accuracy *= 1.3;
        }
        if (attacker.Ability() == Ability.HUSTLE && DamageClass == DamageClass.PHYSICAL)
        {
            accuracy *= 0.8;
        }
        if (attacker.Ability() == Ability.VICTORY_STAR)
        {
            accuracy *= 1.1;
        }

        if (battle.Gravity.Active())
        {
            accuracy *= 5.0 / 3.0;
        }

        if (attacker.HeldItem == "wide-lens")
        {
            accuracy *= 1.1;
        }
        if (attacker.HeldItem == "zoom-lens" && defender.HasMoved)
        {
            accuracy *= 1.2;
        }
        if (defender.HeldItem == "bright-powder")
        {
            accuracy *= 0.9;
        }
        if (micleBerryUsed)
        {
            accuracy *= 1.2;
        }

        return new Random().NextDouble() * 100 <= accuracy;
    }

    /// <summary>
    /// Checks if a move has an effect on a pokemon.
    /// </summary>
    /// <returns>True if a move has an effect on a pokemon.</returns>
    public bool CheckEffective(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        // What if I :flushed: used Hold Hands :flushed: in a double battle :flushed: with you? :flushed:
        // (and you weren't protected by Crafty Shield or in the semi-invulnerable turn of a move like Fly or Dig)
        if (new[] { 86, 174, 368, 370, 371, 389 }.Contains(Effect))
        {
            return false;
        }

        if (!TargetsOpponent())
        {
            return true;
        }

        switch (Effect)
        {
            case 266 when defender.Ability(attacker: attacker, move: this) == Ability.OBLIVIOUS:
            case 39 when defender.Ability(attacker: attacker, move: this) == Ability.STURDY:
            case 39 when Id == 329 && defender.TypeIds.Contains(ElementType.ICE):
            case 400 when string.IsNullOrEmpty(defender.NonVolatileEffect.Current):
                return false;
        }

        if (IsSoundBased() && defender.Ability(attacker: attacker, move: this) == Ability.SOUNDPROOF)
        {
            return false;
        }
        if (IsBallOrBomb() && defender.Ability(attacker: attacker, move: this) == Ability.BULLETPROOF)
        {
            return false;
        }
        if (attacker.Ability() == Ability.PRANKSTER && defender.TypeIds.Contains(ElementType.DARK))
        {
            if (DamageClass == DamageClass.STATUS)
            {
                return false;
            }
            // If the attacker used a status move that called this move, even if this move is not a status move then it should still be considered affected by prankster.
            if (attacker.Owner.SelectedAction is Trainer.MoveAction { Move.DamageClass: DamageClass.STATUS })
            {
                return false;
            }
        }
        if (defender.Ability(attacker: attacker, move: this) == Ability.GOOD_AS_GOLD && DamageClass == DamageClass.STATUS)
        {
            return false;
        }

        // Status moves do not care about type effectiveness - except for thunder wave FOR SOME REASON...
        if (DamageClass == DamageClass.STATUS && Id != 86)
        {
            return true;
        }

        var currentType = GetType(attacker, defender, battle);
        if (currentType == ElementType.TYPELESS)
        {
            return true;
        }

        var effectiveness = defender.Effectiveness(currentType, battle, attacker: attacker, move: this);
        if (Effect == 338)
        {
            effectiveness *= defender.Effectiveness(ElementType.FLYING, battle, attacker: attacker, move: this);
        }
        if (effectiveness == 0)
        {
            return false;
        }

        if (currentType == ElementType.GROUND && !defender.Grounded(battle, attacker: attacker, move: this) &&
            Effect != 373 && !battle.InverseBattle)
        {
            return false;
        }

        if (Effect != 459)
        {
            switch (currentType)
            {
                case ElementType.ELECTRIC when
                    defender.Ability(attacker: attacker, move: this) == Ability.VOLT_ABSORB &&
                    defender.Hp == defender.StartingHp:
                case ElementType.WATER when
                    (defender.Ability(attacker: attacker, move: this) == Ability.WATER_ABSORB ||
                     defender.Ability(attacker: attacker, move: this) == Ability.DRY_SKIN) &&
                    defender.Hp == defender.StartingHp:
                    return false;
            }
        }

        if (currentType == ElementType.FIRE &&
            defender.Ability(attacker: attacker, move: this) == Ability.FLASH_FIRE &&
            defender.FlashFire)
        {
            return false;
        }

        if (effectiveness <= 1 && defender.Ability(attacker: attacker, move: this) == Ability.WONDER_GUARD)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether or not this move is sound based.
    /// </summary>
    public bool IsSoundBased()
    {
        return new[] {
            45, 46, 47, 48, 103, 173, 195, 215, 253, 304, 319, 320, 336, 405, 448, 496, 497, 547,
            555, 568, 574, 575, 586, 590, 664, 691, 728, 744, 753, 826, 871, 1005, 1006
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is a punching move.
    /// </summary>
    public bool IsPunching()
    {
        return new[] {
            4, 5, 7, 8, 9, 146, 183, 223, 264, 309, 325, 327, 359, 409, 418, 612, 665, 721, 729,
            764, 765, 834, 857, 889
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is a biting move.
    /// </summary>
    public bool IsBiting()
    {
        return new[] { 44, 158, 242, 305, 422, 423, 424, 706, 733, 742 }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is a ball or bomb move.
    /// </summary>
    public bool IsBallOrBomb()
    {
        return new[] {
            121, 140, 188, 190, 192, 247, 296, 301, 311, 331, 350, 360, 396, 402, 411, 412, 426,
            439, 443, 486, 491, 545, 676, 690, 748, 1017
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is an aura or pulse move.
    /// </summary>
    public bool IsAuraOrPulse()
    {
        return new[] { 352, 396, 399, 406, 505, 618, 805 }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is a powder or spore move.
    /// </summary>
    public bool IsPowderOrSpore()
    {
        return new[] { 77, 78, 79, 147, 178, 476, 600, 737 }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is a dance move.
    /// </summary>
    public bool IsDance()
    {
        return new[] { 14, 80, 297, 298, 349, 461, 483, 552, 686, 744, 846, 872 }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is a slicing move.
    /// </summary>
    public bool IsSlicing()
    {
        return new[] {
            15, 75, 163, 210, 314, 332, 348, 400, 403, 404, 427, 440, 533, 534, 669, 749, 830, 845,
            860, 869, 891, 895, 1013, 1014
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is a wind move.
    /// </summary>
    public bool IsWind()
    {
        return new[] { 16, 18, 59, 196, 201, 239, 257, 314, 366, 542, 572, 584, 829, 842, 844, 849 }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move can be reflected by magic coat and magic bounce.
    /// </summary>
    public bool IsAffectedByMagicCoat()
    {
        return new[] {
            18, 28, 39, 43, 45, 46, 47, 48, 50, 73, 77, 78, 79, 81, 86, 92, 95, 103, 108, 109, 134,
            137, 139, 142, 147, 148, 169, 178, 180, 184, 186, 191, 193, 204, 207, 212, 213, 227, 230,
            259, 260, 261, 269, 281, 297, 313, 316, 319, 320, 321, 335, 357, 373, 377, 380, 388, 390,
            432, 445, 446, 464, 477, 487, 493, 494, 505, 564, 567, 568, 571, 575, 576, 589, 590, 598,
            599, 600, 608, 666, 668, 671, 672, 685, 715, 736, 737, 810
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move cannot be selected during heal block.
    /// </summary>
    public bool IsAffectedByHealBlock()
    {
        return new[] {
            71, 72, 105, 135, 138, 141, 156, 202, 208, 234, 235, 236, 256, 273, 303, 355, 361, 409,
            456, 461, 505, 532, 570, 577, 613, 659, 666, 668, 685
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move is able to bypass a substitute.
    /// </summary>
    public bool IsAffectedBySubstitute()
    {
        return !new[] {
            18, 45, 46, 47, 48, 50, 102, 103, 114, 166, 173, 174, 176, 180, 193, 195, 213, 215, 227,
            244, 253, 259, 269, 270, 272, 285, 286, 304, 312, 316, 319, 320, 357, 367, 382, 384, 385,
            391, 405, 448, 495, 496, 497, 513, 516, 547, 555, 568, 574, 575, 586, 587, 589, 590, 593,
            597, 600, 602, 607, 621, 664, 674, 683, 689, 691, 712, 728, 753, 826, 871, 1005, 1006
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move targets the opponent.
    /// </summary>
    public bool TargetsOpponent()
    {
        // Moves which don't follow normal targeting protocols, ignore them unless they are damaging.
        if (Target == MoveTarget.SPECIFIC_MOVE && DamageClass == DamageClass.STATUS)
        {
            return false;
        }
        // Moves which do not target the opponent Pokemon.
        return !new[] {
            MoveTarget.SELECTED_POKEMON_ME_FIRST,
            MoveTarget.ALLY,
            MoveTarget.USERS_FIELD,
            MoveTarget.USER_OR_ALLY,
            MoveTarget.OPPONENTS_FIELD,
            MoveTarget.USER,
            MoveTarget.ENTIRE_FIELD,
            MoveTarget.USER_AND_ALLIES,
            MoveTarget.ALL_ALLIES
        }.Contains(Target);
    }

    /// <summary>
    /// Whether or not this move targets multiple Pokemon.
    /// </summary>
    public bool TargetsMultiple()
    {
        return new[] {
            MoveTarget.ALL_OTHER_POKEMON,
            MoveTarget.ALL_OPPONENTS,
            MoveTarget.USER_AND_ALLIES,
            MoveTarget.ALL_POKEMON,
            MoveTarget.ALL_ALLIES
        }.Contains(Target);
    }

    /// <summary>
    /// Whether or not this move makes contact.
    /// </summary>
    public bool MakesContact(DuelPokemon attacker)
    {
        var makesContact = new[] {
            1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 15, 17, 19, 20, 21, 22, 23, 24, 25, 26, 27, 29,
            30, 31, 32, 33, 34, 35, 36, 37, 38, 44, 64, 65, 66, 67, 68, 69, 70, 80, 91, 98, 99,
            117, 122, 127, 128, 130, 132, 136, 141, 146, 152, 154, 158, 162, 163, 165, 167, 168,
            172, 175, 179, 183, 185, 200, 205, 206, 209, 210, 211, 216, 218, 223, 224, 228, 229,
            231, 232, 233, 238, 242, 245, 249, 252, 263, 264, 265, 276, 279, 280, 282, 283, 291,
            292, 299, 301, 302, 305, 306, 309, 310, 325, 327, 332, 337, 340, 342, 343, 344, 348,
            358, 359, 360, 365, 369, 370, 371, 372, 376, 378, 386, 387, 389, 394, 395, 398, 400,
            401, 404, 407, 409, 413, 416, 418, 419, 421, 422, 423, 424, 425, 428, 431, 438, 440,
            442, 447, 450, 452, 453, 457, 458, 462, 467, 480, 484, 488, 490, 492, 498, 507, 509,
            512, 514, 525, 528, 529, 530, 531, 532, 533, 534, 535, 537, 541, 543, 544, 550, 557,
            560, 565, 566, 577, 583, 609, 610, 611, 612, 620, 658, 660, 663, 665, 667, 669, 675,
            677, 679, 680, 681, 684, 688, 692, 693, 696, 699, 701, 706, 707, 709, 710, 712, 713,
            716, 718, 721, 724, 729, 730, 733, 741, 742, 745, 747, 749, 750, 752, 756, 760, 764,
            765, 766, 779, 799, 803, 806, 812, 813, 821, 830, 832, 834, 840, 845, 848, 853, 857,
            859, 860, 861, 862, 866, 869, 872, 873, 878, 879, 884, 885, 887, 889, 891, 892, 894,
            1003, 1010, 1012, 1013
        }.Contains(Id);

        return makesContact && attacker.Ability() != Ability.LONG_REACH;
    }

    /// <summary>
    /// Whether or not this move can be selected by mirror move.
    /// </summary>
    public bool SelectableByMirrorMove()
    {
        return TargetsOpponent();
    }

    /// <summary>
    /// Whether or not this move can be selected by sleep talk.
    /// </summary>
    public bool SelectableBySleepTalk()
    {
        return !new[] {
            13, 19, 76, 91, 102, 117, 118, 119, 130, 143, 166, 253, 264, 274, 291, 340, 382, 383,
            467, 507, 553, 554, 562, 566, 601, 669, 690, 704, 731
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move can be selected by assist.
    /// </summary>
    public bool SelectableByAssist()
    {
        return !new[] {
            18, 19, 46, 68, 91, 102, 118, 119, 144, 165, 166, 168, 182, 194, 197, 203, 214, 243,
            264, 266, 267, 270, 271, 289, 291, 340, 343, 364, 382, 383, 415, 448, 467, 476, 507,
            509, 516, 525, 561, 562, 566, 588, 596, 606, 607, 661, 671, 690, 704
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move can be selected by mimic.
    /// </summary>
    public bool SelectableByMimic()
    {
        return !new[] { 102, 118, 165, 166, 448, 896 }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move can be selected by instruct.
    /// </summary>
    public bool SelectableByInstruct()
    {
        return !new[] {
            13, 19, 63, 76, 91, 102, 117, 118, 119, 130, 143, 144, 165, 166, 214, 264, 267, 274,
            289, 291, 307, 308, 338, 340, 382, 383, 408, 416, 439, 459, 467, 507, 553, 554, 566,
            588, 601, 669, 689, 690, 704, 711, 761, 762, 896
        }.Contains(Id);
    }

    /// <summary>
    /// Whether or not this move can be selected by snatch.
    /// </summary>
    public bool SelectableBySnatch()
    {
        return new[] {
            14, 54, 74, 96, 97, 104, 105, 106, 107, 110, 111, 112, 113, 115, 116, 133, 135, 151,
            156, 159, 160, 164, 187, 208, 215, 219, 234, 235, 236, 254, 256, 268, 273, 275, 278,
            286, 287, 293, 294, 303, 312, 322, 334, 336, 339, 347, 349, 355, 361, 366, 379, 381,
            392, 393, 397, 417, 455, 456, 461, 468, 469, 475, 483, 489, 501, 504, 508, 526, 538,
            561, 602, 659, 673, 674, 694, 0xCFCF
        }.Contains(Id);
    }

    /// <summary>
    /// Gets a random new type for attacker that is resistant to defender's last move type.
    /// </summary>
    /// <returns>A random possible type id, or null if there is no valid type.</returns>
    public static ElementType? GetConversion2(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        if (defender.LastMove == null)
        {
            return null;
        }

        var moveType = defender.LastMove.GetType(attacker, defender, battle);
        var newTypes = new HashSet<ElementType>();

        foreach (ElementType e in Enum.GetValues(typeof(ElementType)))
        {
            if (e == ElementType.TYPELESS)
            {
                continue;
            }

            if (battle.InverseBattle)
            {
                if (battle.TypeEffectiveness[(moveType, e)] > 100)
                {
                    newTypes.Add(e);
                }
            }
            else
            {
                if (battle.TypeEffectiveness[(moveType, e)] < 100)
                {
                    newTypes.Add(e);
                }
            }
        }

        // Remove existing types
        foreach (var t in attacker.TypeIds)
        {
            newTypes.Remove(t);
        }

        if (newTypes.Count == 0)
        {
            return null;
        }

        return newTypes.ElementAt(new Random().Next(newTypes.Count));
    }

    /// <summary>
    /// Generate a copy of this move.
    /// </summary>
    public Move Copy()
    {
        return new Move(new Dictionary<string, object>
        {
            ["id"] = Id,
            ["identifier"] = Name,
            ["power"] = Power,
            ["pp"] = PP,
            ["accuracy"] = Accuracy,
            ["priority"] = Priority,
            ["type_id"] = Type,
            ["damage_class_id"] = DamageClass,
            ["effect_id"] = Effect,
            ["effect_chance"] = EffectChance,
            ["target_id"] = Target,
            ["crit_rate"] = CritRate,
            ["min_hits"] = MinHits,
            ["max_hits"] = MaxHits
        });
    }

    /// <summary>
    /// Generate an instance of the move struggle.
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
    /// Generate an instance of the move confusion.
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
    /// Generate an instance of the move present.
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

    public override string ToString()
    {
        return $"Move(name={Name}, power={Power}, effect_id={Effect})";
    }
}
}
