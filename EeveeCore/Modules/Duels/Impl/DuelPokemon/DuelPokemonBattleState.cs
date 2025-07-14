namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Sends this Pokemon out to battle, handling illusion setup, entry hazards, abilities, and items.
    ///     Returns a formatted message describing what happened when the Pokemon was sent out.
    /// </summary>
    /// <param name="otherpoke">The opposing Pokemon that may be affected by this Pokemon's entry.</param>
    /// <param name="battle">The current battle instance.</param>
    /// <returns>A formatted message describing the send-out process and any triggered effects.</returns>
    public string SendOut(DuelPokemon otherpoke, Battle battle)
    {
        EverSentOut = true;

        // Emergency exit `remove`s the pokemon *in the middle of the turn* in a somewhat unsafe way.
        // `remove` may need to be called here, but that seems like it may have side effects.
        Flinched = false;

        // This has to go BEFORE the send out message, and not in send_out_ability as it only
        // applies on send out, not when abilities are changed, and it changes the send out msg.
        var illusionOptions = Owner.Party.Where(x => x != this && x.Hp > 0).ToList();
        if (Ability() == Impl.Ability.ILLUSION && illusionOptions.Count > 0)
        {
            _illusionName = _name;
            _illusionDisplayName = Name;
            _name = illusionOptions.Last()._name;
            Name = illusionOptions.Last().Name;
        }

        string msg;
        if (_name == "Pikachu")
            msg = $"{Name}, I choose you!\n";
        else
            msg = $"{Owner.Name} sent out {Name}!\n";

        // Any time a poke switches out, certain effects it had put on its opponent end
        if (otherpoke != null)
        {
            otherpoke.Trapping = false;
            otherpoke.Octolock = false;
            otherpoke.Bind.SetTurns(0);
        }

        // Baton Pass
        if (Owner.BatonPass != null)
        {
            msg += $"{Name} carries on the baton!\n";
            Owner.BatonPass.Apply(this);
            Owner.BatonPass = null;
        }

        // Shed Tail
        if (Owner.NextSubstitute > 0)
        {
            Substitute = Owner.NextSubstitute;
            Owner.NextSubstitute = 0;
        }

        // Entry hazards
        // Special case for clearing toxic spikes, still happens even with heavy duty boots
        if (Owner.ToxicSpikes > 0 && Grounded(battle) && TypeIds.Contains(ElementType.POISON))
        {
            Owner.ToxicSpikes = 0;
            msg += $"{Name} absorbed the toxic spikes!\n";
        }

        if (HeldItem.Get() != "heavy-duty-boots")
        {
            // Grounded entry hazards
            if (Grounded(battle))
            {
                // Spikes
                if (Owner.Spikes > 0)
                {
                    // 1/8 -> 1/4
                    var damage = StartingHp / (10 - 2 * Owner.Spikes);
                    msg += Damage(damage, battle, source: "spikes");
                }

                switch (Owner.ToxicSpikes)
                {
                    // Toxic spikes
                    case 1:
                        msg += NonVolatileEffect.ApplyStatus("poison", battle, source: "toxic spikes");
                        break;
                    case 2:
                        msg += NonVolatileEffect.ApplyStatus("b-poison", battle, source: "toxic spikes");
                        break;
                }

                // Sticky web
                if (Owner.StickyWeb) msg += AppendSpeed(-1, source: "the sticky web");
            }

            // Non-grounded entry hazards
            if (Owner.StealthRock)
            {
                var effective = Effectiveness(ElementType.ROCK, battle);
                if (effective > 0)
                {
                    // damage = 1/8 max hp * effectiveness
                    var damage = StartingHp / (32 / (int)(4 * effective));
                    msg += Damage(damage, battle, source: "stealth rock");
                }
            }
        }

        if (Hp > 0) msg += SendOutAbility(otherpoke, battle);

        // Restoration
        if (Owner.HealingWish)
        {
            var used = false;
            if (Hp != StartingHp)
            {
                used = true;
                Hp = StartingHp;
            }

            if (!string.IsNullOrEmpty(NonVolatileEffect.Current))
            {
                used = true;
                NonVolatileEffect.Reset();
            }

            if (used)
            {
                Owner.HealingWish = false;
                msg += $"{Name} was restored by healing wish!\n";
            }
        }

        if (Owner.LunarDance)
        {
            var used = false;
            if (Hp != StartingHp)
            {
                used = true;
                Hp = StartingHp;
            }

            if (!string.IsNullOrEmpty(NonVolatileEffect.Current))
            {
                used = true;
                NonVolatileEffect.Reset();
            }

            var notAtFullPP = Moves.Where(move => move.PP != move.StartingPP).ToList();
            if (notAtFullPP.Count > 0)
            {
                used = true;
                foreach (var move in notAtFullPP) move.PP = move.StartingPP;
            }

            if (used)
            {
                Owner.LunarDance = false;
                msg += $"{Name} was restored by lunar dance\n";
            }
        }

        // Items
        if (HeldItem.Get() == "air-balloon" && !Grounded(battle))
            msg += $"{Name} floats in the air with its air balloon!\n";
        if (HeldItem.Get() == "electric-seed" && battle.Terrain.Item?.ToString() == "electric")
        {
            msg += AppendDefense(1, this, source: "its electric seed");
            HeldItem.Use();
        }

        if (HeldItem.Get() == "psychic-seed" && battle.Terrain.Item?.ToString() == "psychic")
        {
            msg += AppendSpDef(1, this, source: "its psychic seed");
            HeldItem.Use();
        }

        if (HeldItem.Get() == "misty-seed" && battle.Terrain.Item?.ToString() == "misty")
        {
            msg += AppendSpDef(1, this, source: "its misty seed");
            HeldItem.Use();
        }

        if (HeldItem.Get() == "grassy-seed" && battle.Terrain.Item?.ToString() == "grassy")
        {
            msg += AppendDefense(1, this, source: "its grassy seed");
            HeldItem.Use();
        }

        return msg;
    }

    /// <summary>
    ///     Initialize this poke's Duels.Ability.
    ///     otherpoke may be null.
    ///     Returns a formatted message.
    /// </summary>
    public string SendOutAbility(DuelPokemon otherpoke, Battle battle)
    {
        var msg = "";

        // Imposter (sus)
        if (Ability() == Impl.Ability.IMPOSTER && otherpoke is { Substitute: 0, _illusionDisplayName: null })
        {
            msg += $"{Name} transformed into {otherpoke._name}!\n";
            Transform(otherpoke);
        }

        // Weather
        if (Ability() == Impl.Ability.DRIZZLE) msg += battle.Weather.Set("rain", this);
        if (Ability() == Impl.Ability.PRIMORDIAL_SEA) msg += battle.Weather.Set("h-rain", this);
        if (Ability() == Impl.Ability.SAND_STREAM) msg += battle.Weather.Set("sandstorm", this);
        if (Ability() == Impl.Ability.SNOW_WARNING) msg += battle.Weather.Set("hail", this);
        if (Ability() == Impl.Ability.DROUGHT || Ability() == Impl.Ability.ORICHALCUM_PULSE)
            msg += battle.Weather.Set("sun", this);
        if (Ability() == Impl.Ability.DESOLATE_LAND) msg += battle.Weather.Set("h-sun", this);
        if (Ability() == Impl.Ability.DELTA_STREAM) msg += battle.Weather.Set("h-wind", this);

        // Terrain
        if (Ability() == Impl.Ability.GRASSY_SURGE) msg += battle.Terrain.Set("grassy", this);
        if (Ability() == Impl.Ability.MISTY_SURGE) msg += battle.Terrain.Set("misty", this);
        if (Ability() == Impl.Ability.ELECTRIC_SURGE || Ability() == Impl.Ability.HADRON_ENGINE)
            msg += battle.Terrain.Set("electric", this);
        if (Ability() == Impl.Ability.PSYCHIC_SURGE) msg += battle.Terrain.Set("psychic", this);

        // Message only
        if (Ability() == Impl.Ability.MOLD_BREAKER) msg += $"{Name} breaks the mold!\n";
        if (Ability() == Impl.Ability.TURBOBLAZE) msg += $"{Name} is radiating a blazing aura!\n";
        if (Ability() == Impl.Ability.TERAVOLT) msg += $"{Name} is radiating a bursting aura!\n";

        if (Ability() == Impl.Ability.INTIMIDATE && otherpoke != null)
        {
            if (otherpoke.Ability() == Impl.Ability.OBLIVIOUS)
            {
                msg += $"{otherpoke.Name} is too oblivious to be intimidated!\n";
            }
            else if (otherpoke.Ability() == Impl.Ability.OWN_TEMPO)
            {
                msg += $"{otherpoke.Name} keeps walking on its own tempo, and is not intimidated!\n";
            }
            else if (otherpoke.Ability() == Impl.Ability.INNER_FOCUS)
            {
                msg += $"{otherpoke.Name} is too focused to be intimidated!\n";
            }
            else if (otherpoke.Ability() == Impl.Ability.SCRAPPY)
            {
                msg += $"{otherpoke.Name} is too scrappy to be intimidated!\n";
            }
            else if (otherpoke.Ability() == Impl.Ability.GUARD_DOG)
            {
                msg += $"{otherpoke.Name}'s guard dog keeps it from being intimidated!\n";
                msg += otherpoke.AppendAttack(1, otherpoke, source: "its guard dog");
            }
            else
            {
                msg += otherpoke.AppendAttack(-1, this, source: $"{Name}'s Intimidate");
                if (otherpoke.HeldItem.Get() == "adrenaline-orb")
                    msg += otherpoke.AppendSpeed(1, otherpoke, source: "its adrenaline orb");
                if (otherpoke.Ability() == Impl.Ability.RATTLED)
                    msg += otherpoke.AppendSpeed(1, otherpoke, source: "its rattled");
            }
        }

        if (Ability() == Impl.Ability.SCREEN_CLEANER)
        {
            battle.Trainer1.AuroraVeil.SetTurns(0);
            battle.Trainer1.LightScreen.SetTurns(0);
            battle.Trainer1.Reflect.SetTurns(0);
            battle.Trainer2.AuroraVeil.SetTurns(0);
            battle.Trainer2.LightScreen.SetTurns(0);
            battle.Trainer2.Reflect.SetTurns(0);
            msg += $"{Name}'s screen cleaner removed barriers from both sides of the field!\n";
        }

        if (Ability() == Impl.Ability.INTREPID_SWORD) msg += AppendAttack(1, this, source: "its intrepid sword");
        if (Ability() == Impl.Ability.DAUNTLESS_SHIELD) msg += AppendDefense(1, this, source: "its dauntless shield");
        if (Ability() == Impl.Ability.TRACE && otherpoke != null && otherpoke.AbilityGiveable())
        {
            AbilityId = otherpoke.AbilityId;
            msg += $"{Name} traced {otherpoke.Name}'s ability!\n";
            msg += SendOutAbility(otherpoke, battle);
            return msg;
        }

        if (Ability() == Impl.Ability.DOWNLOAD && otherpoke != null)
        {
            if (otherpoke.GetSpDef(battle) > otherpoke.GetDefense(battle))
                msg += AppendAttack(1, this, source: "its download");
            else
                msg += AppendSpAtk(1, this, source: "its download");
        }

        if (Ability() == Impl.Ability.ANTICIPATION && otherpoke != null)
        {
            var shuddered = false;
            foreach (var move in otherpoke.Moves)
            {
                if (move.Effect == 39)
                {
                    msg += $"{Name} shuddered in anticipation!\n";
                    shuddered = true;
                    break;
                }

                if (Effectiveness(move.Type, battle) > 1)
                {
                    msg += $"{Name} shuddered in anticipation!\n";
                    shuddered = true;
                    break;
                }
            }
        }

        if (Ability() == Impl.Ability.FOREWARN && otherpoke != null)
        {
            var bestMoves = new List<Move.Move>();
            var bestPower = 0;
            foreach (var move in otherpoke.Moves)
            {
                int power;
                if (move.DamageClass == DamageClass.STATUS)
                    power = 0;
                else if (move.Effect == 39)
                    power = 150;
                else if (move.Power == null) // Good enough
                    power = 80;
                else
                    power = move.Power.Value;
                if (power > bestPower)
                {
                    bestPower = power;
                    bestMoves = [move];
                }
                else if (power == bestPower)
                {
                    bestMoves.Add(move);
                }
            }

            if (bestMoves.Count > 0)
            {
                var move = bestMoves[new Random().Next(bestMoves.Count)];
                msg += $"{Name} is forewarned about {otherpoke.Name}'s {move.PrettyName}!\n";
            }
        }

        if (Ability() == Impl.Ability.FRISK && otherpoke != null && otherpoke.HeldItem.HasItem())
            msg += $"{Name} senses that {otherpoke.Name} is holding a {otherpoke.HeldItem.Name} using its frisk!\n";
        if (Ability() == Impl.Ability.MULTITYPE)
        {
            ElementType? e = null;
            string? f = null;
            if (HeldItem.Get() == "draco-plate")
            {
                e = ElementType.DRAGON;
                f = "Arceus-dragon";
            }
            else if (HeldItem.Get() == "dread-plate")
            {
                e = ElementType.DARK;
                f = "Arceus-dark";
            }
            else if (HeldItem.Get() == "earth-plate")
            {
                e = ElementType.GROUND;
                f = "Arceus-ground";
            }
            else if (HeldItem.Get() == "fist-plate")
            {
                e = ElementType.FIGHTING;
                f = "Arceus-fighting";
            }
            else if (HeldItem.Get() == "flame-plate")
            {
                e = ElementType.FIRE;
                f = "Arceus-fire";
            }
            else if (HeldItem.Get() == "icicle-plate")
            {
                e = ElementType.ICE;
                f = "Arceus-ice";
            }
            else if (HeldItem.Get() == "insect-plate")
            {
                e = ElementType.BUG;
                f = "Arceus-bug";
            }
            else if (HeldItem.Get() == "iron-plate")
            {
                e = ElementType.STEEL;
                f = "Arceus-steel";
            }
            else if (HeldItem.Get() == "meadow-plate")
            {
                e = ElementType.GRASS;
                f = "Arceus-grass";
            }
            else if (HeldItem.Get() == "mind-plate")
            {
                e = ElementType.PSYCHIC;
                f = "Arceus-psychic";
            }
            else if (HeldItem.Get() == "pixie-plate")
            {
                e = ElementType.FAIRY;
                f = "Arceus-fairy";
            }
            else if (HeldItem.Get() == "sky-plate")
            {
                e = ElementType.FLYING;
                f = "Arceus-flying";
            }
            else if (HeldItem.Get() == "splash-plate")
            {
                e = ElementType.WATER;
                f = "Arceus-water";
            }
            else if (HeldItem.Get() == "spooky-plate")
            {
                e = ElementType.GHOST;
                f = "Arceus-ghost";
            }
            else if (HeldItem.Get() == "stone-plate")
            {
                e = ElementType.ROCK;
                f = "Arceus-rock";
            }
            else if (HeldItem.Get() == "toxic-plate")
            {
                e = ElementType.POISON;
                f = "Arceus-poison";
            }
            else if (HeldItem.Get() == "zap-plate")
            {
                e = ElementType.ELECTRIC;
                f = "Arceus-electric";
            }

            if (e != null && Form(f))
            {
                TypeIds = [e.Value];
                var t = e.Value.ToString().ToLower();
                msg += $"{Name} transformed into a {t} type using its multitype!\n";
            }
        }

        if (Ability() == Impl.Ability.RKS_SYSTEM && _name == "Silvally")
        {
            ElementType? e = null;
            if (HeldItem.Get() == "dragon-memory")
            {
                if (Form("Silvally-dragon")) e = ElementType.DRAGON;
            }
            else if (HeldItem.Get() == "dark-memory")
            {
                if (Form("Silvally-dark")) e = ElementType.DARK;
            }
            else if (HeldItem.Get() == "ground-memory")
            {
                if (Form("Silvally-ground")) e = ElementType.GROUND;
            }
            else if (HeldItem.Get() == "fighting-memory")
            {
                if (Form("Silvally-fighting")) e = ElementType.FIGHTING;
            }
            else if (HeldItem.Get() == "fire-memory")
            {
                if (Form("Silvally-fire")) e = ElementType.FIRE;
            }
            else if (HeldItem.Get() == "ice-memory")
            {
                if (Form("Silvally-ice")) e = ElementType.ICE;
            }
            else if (HeldItem.Get() == "bug-memory")
            {
                if (Form("Silvally-bug")) e = ElementType.BUG;
            }
            else if (HeldItem.Get() == "steel-memory")
            {
                if (Form("Silvally-steel")) e = ElementType.STEEL;
            }
            else if (HeldItem.Get() == "grass-memory")
            {
                if (Form("Silvally-grass")) e = ElementType.GRASS;
            }
            else if (HeldItem.Get() == "psychic-memory")
            {
                if (Form("Silvally-psychic")) e = ElementType.PSYCHIC;
            }
            else if (HeldItem.Get() == "fairy-memory")
            {
                if (Form("Silvally-fairy")) e = ElementType.FAIRY;
            }
            else if (HeldItem.Get() == "flying-memory")
            {
                if (Form("Silvally-flying")) e = ElementType.FLYING;
            }
            else if (HeldItem.Get() == "water-memory")
            {
                if (Form("Silvally-water")) e = ElementType.WATER;
            }
            else if (HeldItem.Get() == "ghost-memory")
            {
                if (Form("Silvally-ghost")) e = ElementType.GHOST;
            }
            else if (HeldItem.Get() == "rock-memory")
            {
                if (Form("Silvally-rock")) e = ElementType.ROCK;
            }
            else if (HeldItem.Get() == "poison-memory")
            {
                if (Form("Silvally-poison")) e = ElementType.POISON;
            }
            else if (HeldItem.Get() == "electric-memory")
            {
                if (Form("Silvally-electric")) e = ElementType.ELECTRIC;
            }

            if (e != null)
            {
                TypeIds = [e.Value];
                var t = e.Value.ToString().ToLower();
                msg += $"{Name} transformed into a {t} type using its rks system!\n";
            }
        }

        if (Ability() == Impl.Ability.TRUANT) TruantTurn = 0;
        if (Ability() == Impl.Ability.FORECAST &&
            _name is "Castform" or "Castform-snowy" or "Castform-rainy" or "Castform-sunny")
        {
            var weather = battle.Weather.Get();
            ElementType? element = null;
            switch (weather)
            {
                case "hail" when _name != "Castform-snowy":
                {
                    if (Form("Castform-snowy")) element = ElementType.ICE;

                    break;
                }
                case "sandstorm" or "h-wind" or null when _name != "Castform":
                {
                    if (Form("Castform")) element = ElementType.NORMAL;

                    break;
                }
                case "rain" or "h-rain" when _name != "Castform-rainy":
                {
                    if (Form("Castform-rainy")) element = ElementType.WATER;

                    break;
                }
                case "sun" or "h-sun" when _name != "Castform-sunny":
                {
                    if (Form("Castform-sunny")) element = ElementType.FIRE;

                    break;
                }
            }

            if (element != null)
            {
                TypeIds = [element.Value];
                var t = element.Value.ToString().ToLower();
                msg += $"{Name} transformed into a {t} type using its forecast!\n";
            }
        }

        if (Ability() == Impl.Ability.MIMICRY && battle.Terrain.Item != null)
        {
            ElementType element;
            var terrain = battle.Terrain.Item.ToString();
            switch (terrain)
            {
                case "electric":
                    element = ElementType.ELECTRIC;
                    break;
                case "grassy":
                    element = ElementType.GRASS;
                    break;
                case "misty":
                    element = ElementType.FAIRY;
                    break;
                // terrain == "psychic"
                default:
                    element = ElementType.PSYCHIC;
                    break;
            }

            TypeIds = [element];
            var t = element.ToString().ToLower();
            msg += $"{Name} transformed into a {t} type using its mimicry!\n";
        }

        if (Ability() == Impl.Ability.WIND_RIDER && Owner.Tailwind.Active())
            msg += AppendAttack(1, this, source: "its wind rider");
        if (Ability() == Impl.Ability.SUPERSWEET_SYRUP && !SupersweetSyrup && otherpoke != null)
            msg += otherpoke.AppendEvasion(-1, this, source: $"{Name}'s supersweet syrup");

        return msg;
    }

    /// <summary>
    ///     Clean up a poke when it is removed from battle.
    ///     Returns a formatted message of anything that happened while switching out.
    /// </summary>
    public string Remove(Battle battle, bool fainted = false)
    {
        var msg = "";
        if (!fainted)
        {
            if (Ability() == Impl.Ability.NATURAL_CURE && !string.IsNullOrEmpty(NonVolatileEffect.Current))
            {
                msg += $"{Name}'s {NonVolatileEffect.Current} was cured by its natural cure!\n";
                NonVolatileEffect.Reset();
            }

            if (Ability() == Impl.Ability.REGENERATOR) msg += Heal(StartingHp / 3, "its regenerator");
            if (Ability() == Impl.Ability.ZERO_TO_HERO)
                if (Form("Palafin-hero"))
                    msg += $"{Name} is ready to be a hero!\n";
        }

        NonVolatileEffect.BadlyPoisonedTurn = 0;
        Minimized = false;
        HasMoved = false;
        ChoiceMove = null;
        LastMove = null;
        ShouldMegaEvolve = false;
        SwappedIn = false;
        ActiveTurns = 0;
        if (_illusionName != null)
        {
            _name = _illusionName;
            Name = _illusionDisplayName;
        }

        _illusionName = null;
        _illusionDisplayName = null;
        if (_startingName is "Ditto" or "Smeargle" or "Mew" or "Aegislash")
        {
            _name = _startingName;
            if (_nickname != "None")
                Name = $"{_nickname} ({_name.Replace('-', ' ')})";
            else
                Name = _name.Replace("-", " ");
        }

        if (Owner.CurrentPokemon == this) Owner.CurrentPokemon = null;
        if (battle.Weather.RecheckAbilityWeather()) msg += "The weather cleared!\n";
        Attack = BaseStats[_name][1];
        Defense = BaseStats[_name][2];
        SpAtk = BaseStats[_name][3];
        SpDef = BaseStats[_name][4];
        Speed = BaseStats[_name][5];
        HpIV = StartingHpIV;
        AtkIV = StartingAtkIV;
        DefIV = StartingDefIV;
        SpAtkIV = StartingSpAtkIV;
        SpDefIV = StartingSpDefIV;
        SpeedIV = StartingSpeedIV;
        HpEV = StartingHpEV;
        AtkEV = StartingAtkEV;
        DefEV = StartingDefEV;
        SpAtkEV = StartingSpAtkEV;
        SpDefEV = StartingSpDefEV;
        SpeedEV = StartingSpeedEV;
        Moves = new List<Move.Move>(StartingMoves);
        AbilityId = StartingAbilityId;
        TypeIds = new List<ElementType>(StartingTypeIds);
        AttackStage = 0;
        DefenseStage = 0;
        SpAtkStage = 0;
        SpDefStage = 0;
        SpeedStage = 0;
        AccuracyStage = 0;
        EvasionStage = 0;
        Metronome = new Metronome();
        LeechSeed = false;
        Stockpile = 0;
        Flinched = false;
        Confusion = new ExpiringEffect(0);
        LastMoveDamage = null;
        LockedMove = null;
        Bide = null;
        Torment = false;
        Imprison = false;
        Disable = new ExpiringItem();
        Taunt = new ExpiringEffect(0);
        Encore = new ExpiringItem();
        HealBlock = new ExpiringEffect(0);
        FocusEnergy = false;
        PerishSong = new ExpiringEffect(0);
        Nightmare = false;
        DefenseCurl = false;
        FuryCutter = 0;
        Bind = new ExpiringEffect(0);
        Substitute = 0;
        Silenced = new ExpiringEffect(0);
        LastMoveFailed = false;
        Rage = false;
        MindReader = new ExpiringItem();
        DestinyBond = false;
        DestinyBondCooldown = new ExpiringEffect(0);
        Trapping = false;
        Ingrain = false;
        Infatuated = null;
        AquaRing = false;
        MagnetRise = new ExpiringEffect(0);
        Dive = false;
        Dig = false;
        Fly = false;
        ShadowForce = false;
        LuckyChant = new ExpiringEffect(0);
        GroundedByMove = false;
        Charge = new ExpiringEffect(0);
        Uproar = new ExpiringEffect(0);
        MagicCoat = false;
        PowerTrick = false;
        PowerShift = false;
        Yawn = new ExpiringEffect(0);
        IonDeluge = false;
        Electrify = false;
        ProtectionUsed = false;
        ProtectionChance = 1;
        Protect = false;
        Endure = false;
        WideGuard = false;
        CraftyShield = false;
        KingShield = false;
        SpikyShield = false;
        MatBlock = false;
        BanefulBunker = false;
        QuickGuard = false;
        Obstruct = false;
        SilkTrap = false;
        BurningBulwark = false;
        LaserFocus = new ExpiringEffect(0);
        Powdered = false;
        Snatching = false;
        Telekinesis = new ExpiringEffect(0);
        Embargo = new ExpiringEffect(0);
        EchoedVoicePower = 40;
        EchoedVoiceUsed = false;
        Curse = false;
        FairyLock = new ExpiringEffect(0);
        Grudge = false;
        Foresight = false;
        MiracleEye = false;
        BeakBlast = false;
        NoRetreat = false;
        DmgThisTurn = false;
        Autotomize = 0;
        LansatBerryAte = false;
        MicleBerryAte = false;
        FlashFire = false;
        TruantTurn = 0;
        CudChew = new ExpiringEffect(0);
        BoosterEnergy = false;
        StatIncreased = false;
        StatDecreased = false;
        Roost = false;
        Octolock = false;
        AttackSplit = null;
        SpAtkSplit = null;
        DefenseSplit = null;
        SpDefSplit = null;
        TarShot = false;
        SyrupBomb = new ExpiringEffect(0);
        HeldItem.EverHadItem = HeldItem.HasItem();

        return msg;
    }

    /// <summary>
    ///     Updates this pokemon for a new turn.
    ///     `otherpoke` may be null if the opponent fainted the previous turn.
    ///     Returns a formatted message.
    /// </summary>
    public string NextTurn(DuelPokemon otherpoke, Battle battle)
    {
        var msg = "";
        // This needs to be here, as swapping sets this value explicitly
        HasMoved = false;
        if (!SwappedIn) ActiveTurns++;
        LastMoveDamage = null;
        LastMoveFailed = false;
        ShouldMegaEvolve = false;
        Rage = false;
        MindReader.NextTurn();
        Charge.NextTurn();
        DestinyBondCooldown.NextTurn();
        MagicCoat = false;
        IonDeluge = false;
        Electrify = false;
        if (!ProtectionUsed) ProtectionChance = 1;
        ProtectionUsed = false;
        Protect = false;
        Endure = false;
        WideGuard = false;
        CraftyShield = false;
        KingShield = false;
        SpikyShield = false;
        MatBlock = false;
        BanefulBunker = false;
        QuickGuard = false;
        Obstruct = false;
        SilkTrap = false;
        BurningBulwark = false;
        LaserFocus.NextTurn();
        Powdered = false;
        Snatching = false;
        if (!EchoedVoiceUsed) EchoedVoicePower = 40;
        Grudge = false;
        BeakBlast = false;
        DmgThisTurn = false;
        if (LockedMove != null)
            if (LockedMove.NextTurn())
            {
                LockedMove = null;
                // Just in case they never actually used the move to remove it
                Dive = false;
                Dig = false;
                Fly = false;
                ShadowForce = false;
            }

        FairyLock.NextTurn();
        Flinched = false;
        TruantTurn++;
        StatIncreased = false;
        StatDecreased = false;
        Roost = false;
        SyrupBomb.NextTurn();

        msg += NonVolatileEffect.NextTurn(battle);

        // Volatile status turn progression
        var prevDisabMove = (Move.Move)Disable.Item;
        if (Disable.NextTurn()) msg += $"{Name}'s {prevDisabMove.PrettyName} is no longer disabled!\n";
        if (Taunt.NextTurn()) msg += $"{Name}'s taunt has ended!\n";
        if (HealBlock.NextTurn()) msg += $"{Name}'s heal block has ended!\n";
        if (Silenced.NextTurn()) msg += $"{Name}'s voice returned!\n";
        if (MagnetRise.NextTurn()) msg += $"{Name}'s magnet rise has ended!\n";
        if (LuckyChant.NextTurn()) msg += $"{Name} is no longer shielded by lucky chant!\n";
        if (Uproar.NextTurn()) msg += $"{Name} calms down!\n";
        if (Telekinesis.NextTurn()) msg += $"{Name} was released from telekinesis!\n";
        if (Embargo.NextTurn()) msg += $"{Name}'s embargo was lifted!\n";
        if (Yawn.NextTurn()) msg += NonVolatileEffect.ApplyStatus("sleep", battle, this, source: "drowsiness");
        if (Encore.NextTurn()) msg += $"{Name}'s encore is over!\n";
        if (PerishSong.NextTurn()) msg += Faint(battle, source: "perish song");
        if (Encore.Active() && ((Move.Move)Encore.Item).PP == 0)
        {
            Encore.End();
            msg += $"{Name}'s encore is over!\n";
        }

        if (CudChew.NextTurn() && HeldItem.LastUsed != null && HeldItem.LastUsed.Identifier.EndsWith("-berry"))
        {
            HeldItem.Recover(HeldItem);
            msg += HeldItem.EatBerry();
            HeldItem.LastUsed = null;
        }

        // Held Items
        if (HeldItem.Get() == "white-herb")
        {
            var changed = false;
            if (AttackStage < 0)
            {
                AttackStage = 0;
                changed = true;
            }

            if (DefenseStage < 0)
            {
                DefenseStage = 0;
                changed = true;
            }

            if (SpAtkStage < 0)
            {
                SpAtkStage = 0;
                changed = true;
            }

            if (SpDefStage < 0)
            {
                SpDefStage = 0;
                changed = true;
            }

            if (SpeedStage < 0)
            {
                SpeedStage = 0;
                changed = true;
            }

            if (AccuracyStage < 0)
            {
                AccuracyStage = 0;
                changed = true;
            }

            if (EvasionStage < 0)
            {
                EvasionStage = 0;
                changed = true;
            }

            if (changed)
            {
                msg += $"{Name}'s white herb reset all negative stat stage changes.\n";
                HeldItem.Use();
            }
        }

        if (HeldItem.Get() == "toxic-orb")
            msg += NonVolatileEffect.ApplyStatus("b-poison", battle, this, source: "its toxic orb");
        if (HeldItem.Get() == "flame-orb")
            msg += NonVolatileEffect.ApplyStatus("burn", battle, this, source: "its flame orb");
        if (HeldItem.Get() == "leftovers") msg += Heal(StartingHp / 16, "its leftovers");
        if (HeldItem.Get() == "black-sludge")
        {
            if (TypeIds.Contains(ElementType.POISON))
                msg += Heal(StartingHp / 16, "its black sludge");
            else
                msg += Damage(StartingHp / 8, battle, source: "its black sludge");
        }

        // Abilities
        if (Ability() == Impl.Ability.SPEED_BOOST && !SwappedIn) msg += AppendSpeed(1, this, source: "its Speed boost");
        if (Ability() == Impl.Ability.LIMBER && NonVolatileEffect.Paralysis())
        {
            NonVolatileEffect.Reset();
            msg += $"{Name}'s limber cured it of its paralysis!\n";
        }

        if (Ability() == Impl.Ability.INSOMNIA && NonVolatileEffect.Sleep())
        {
            NonVolatileEffect.Reset();
            msg += $"{Name}'s insomnia woke it up!\n";
        }

        if (Ability() == Impl.Ability.VITAL_SPIRIT && NonVolatileEffect.Sleep())
        {
            NonVolatileEffect.Reset();
            msg += $"{Name}'s vital spirit woke it up!\n";
        }

        if (Ability() == Impl.Ability.IMMUNITY && NonVolatileEffect.Poison())
        {
            NonVolatileEffect.Reset();
            msg += $"{Name}'s immunity cured it of its poison!\n";
        }

        if (Ability() == Impl.Ability.MAGMA_ARMOR && NonVolatileEffect.Freeze())
        {
            NonVolatileEffect.Reset();
            msg += $"{Name}'s magma armor cured it of thawed it!\n";
        }

        if ((Ability() == Impl.Ability.WATER_VEIL || Ability() == Impl.Ability.WATER_BUBBLE) &&
            NonVolatileEffect.Burn())
        {
            NonVolatileEffect.Reset();
            var abilityName = ((Ability)AbilityId).GetPrettyName();
            msg += $"{Name}'s {abilityName} cured it of its burn!\n";
        }

        if (Ability() == Impl.Ability.OWN_TEMPO && Confusion.Active())
        {
            Confusion.SetTurns(0);
            msg += $"{Name}'s tempo cured it of its confusion!\n";
        }

        if (Ability() == Impl.Ability.OBLIVIOUS)
        {
            if (Infatuated != null)
            {
                Infatuated = null;
                msg += $"{Name} fell out of love because of its obliviousness!\n";
            }

            if (Taunt.Active())
            {
                Taunt.SetTurns(0);
                msg += $"{Name} stopped caring about being taunted because of its obliviousness!\n";
            }
        }

        if (Ability() == Impl.Ability.RAIN_DISH && (battle.Weather.Get() == "rain" || battle.Weather.Get() == "h-rain"))
            msg += Heal(StartingHp / 16, "its rain dish");
        if (Ability() == Impl.Ability.ICE_BODY && battle.Weather.Get() == "hail")
            msg += Heal(StartingHp / 16, "its ice body");
        if (Ability() == Impl.Ability.DRY_SKIN)
        {
            if (battle.Weather.Get() == "rain" || battle.Weather.Get() == "h-rain")
                msg += Heal(StartingHp / 8, "its dry skin");
            else if (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun")
                msg += Damage(StartingHp / 8, battle, source: "its dry skin");
        }

        if (Ability() == Impl.Ability.SOLAR_POWER && (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun"))
            msg += Damage(StartingHp / 8, battle, source: "its solar power");
        if (Ability() == Impl.Ability.MOODY)
        {
            var stats = new List<Tuple<int, string>>
            {
                new(AttackStage, "attack"),
                new(DefenseStage, "defense"),
                new(SpAtkStage, "special attack"),
                new(SpDefStage, "special defense"),
                new(SpeedStage, "speed")
            };
            var addStats = new List<Tuple<int, string>>(stats);
            var removeStats = new List<Tuple<int, string>>(stats);
            for (var i = stats.Count - 1; i >= 0; i--)
                if (stats[i].Item1 == 6)
                    addStats.RemoveAt(i);

            Tuple<int, string> addStat = null;
            if (addStats.Count > 0)
            {
                addStat = addStats[new Random().Next(addStats.Count)];
                msg += AppendStat(2, this, null, addStat.Item2, "its moodiness");
            }

            for (var i = removeStats.Count - 1; i >= 0; i--)
                if (removeStats[i] == addStat)
                    removeStats.RemoveAt(i);
                else if (removeStats[i].Item1 == -6) removeStats.RemoveAt(i);

            if (removeStats.Count > 0)
            {
                var removeStat = removeStats[new Random().Next(removeStats.Count)];
                msg += AppendStat(-1, this, null, removeStat.Item2, "its moodiness");
            }
        }

        if (Ability() == Impl.Ability.PICKUP && !HeldItem.HasItem() && otherpoke is { HeldItem.LastUsed: not null })
        {
            HeldItem.Recover(otherpoke.HeldItem);
            msg += $"{Name} picked up a {HeldItem.Name}!\n";
        }

        if (Ability() == Impl.Ability.ICE_FACE && !IceRepaired && _name == "Eiscue-noice" &&
            battle.Weather.Get() == "hail")
            if (Form("Eiscue"))
            {
                IceRepaired = true;
                msg += $"{Name}'s ice face was restored by the hail!\n";
            }

        if (Ability() == Impl.Ability.HARVEST && LastBerry != null && !HeldItem.HasItem())
            if (new Random().Next(2) == 0)
            {
                HeldItem = new HeldItem(LastBerry, this);
                LastBerry = null;
                msg += $"{Name} harvested a {HeldItem.Name}!\n";
            }

        if (Ability() == Impl.Ability.ZEN_MODE && _name == "Darmanitan" && Hp < StartingHp / 2)
            if (Form("Darmanitan-zen"))
            {
                if (!TypeIds.Contains(ElementType.PSYCHIC)) TypeIds.Add(ElementType.PSYCHIC);
                msg += $"{Name} enters a zen state.\n";
            }

        if (Ability() == Impl.Ability.ZEN_MODE && _name == "Darmanitan-galar" && Hp < StartingHp / 2)
            if (Form("Darmanitan-zen-galar"))
            {
                if (!TypeIds.Contains(ElementType.FIRE)) TypeIds.Add(ElementType.FIRE);
                msg += $"{Name} enters a zen state.\n";
            }

        if (Ability() == Impl.Ability.ZEN_MODE && _name == "Darmanitan-zen" && Hp >= StartingHp / 2)
            if (Form("Darmanitan"))
            {
                if (TypeIds.Contains(ElementType.PSYCHIC)) TypeIds.Remove(ElementType.PSYCHIC);
                msg += $"{Name}'s zen state ends!\n";
            }

        if (Ability() == Impl.Ability.ZEN_MODE && _name == "Darmanitan-zen-galar" && Hp >= StartingHp / 2)
            if (Form("Darmanitan-galar"))
            {
                if (TypeIds.Contains(ElementType.FIRE)) TypeIds.Remove(ElementType.FIRE);
                msg += $"{Name}'s zen state ends!\n";
            }

        if (Ability() == Impl.Ability.SHIELDS_DOWN && _name == "Minior" && Hp < StartingHp / 2)
        {
            string? newForm;
            switch (Id % 7)
            {
                case 0:
                    newForm = "Minior-red";
                    break;
                case 1:
                    newForm = "Minior-orange";
                    break;
                case 2:
                    newForm = "Minior-yellow";
                    break;
                case 3:
                    newForm = "Minior-green";
                    break;
                case 4:
                    newForm = "Minior-blue";
                    break;
                case 5:
                    newForm = "Minior-indigo";
                    break;
                default:
                    newForm = "Minior-violet";
                    break;
            }

            if (Form(newForm)) msg += $"{Name}'s core was exposed!\n";
        }

        if (Ability() == Impl.Ability.SHIELDS_DOWN && _name.StartsWith("Minior-") && _name != "Minior" &&
            Hp >= StartingHp / 2)
            if (Form("Minior"))
                msg += $"{Name}'s shell returned!\n";

        if (Ability() == Impl.Ability.SCHOOLING && _name == "Wishiwashi-school" && Hp < StartingHp / 4)
            if (Form("Wishiwashi"))
                msg += $"{Name}'s school is gone!\n";

        if (Ability() == Impl.Ability.SCHOOLING && _name == "Wishiwashi" && Hp >= StartingHp / 4 && Level >= 20)
            if (Form("Wishiwashi-school"))
                msg += $"{Name} schools together!\n";

        if (Ability() == Impl.Ability.POWER_CONSTRUCT && _name is "Zygarde" or "Zygarde-10" && Hp < StartingHp / 2 &&
            Hp > 0)
            if (Form("Zygarde-complete"))
            {
                msg += $"{Name} is at full power!\n";
                // Janky way to raise the current HP of this poke, as it's new form has a higher HP stat. Note, this is NOT healing.
                var newHp = (int)Math.Round((2 * BaseStats["Zygarde-complete"][0] + HpIV + HpEV / 4.0) * Level / 100 +
                                            Level + 10);
                Hp = newHp - (StartingHp - Hp);
                StartingHp = newHp;
            }

        if (Ability() == Impl.Ability.HUNGER_SWITCH)
            switch (_name)
            {
                case "Morpeko":
                    Form("Morpeko-hangry");
                    break;
                case "Morpeko-hangry":
                    Form("Morpeko");
                    break;
            }

        if (Ability() == Impl.Ability.FLOWER_GIFT && _name == "Cherrim" &&
            (battle.Weather.Get() == "sun" || battle.Weather.Get() == "h-sun")) Form("Cherrim-sunshine");
        if (Ability() == Impl.Ability.FLOWER_GIFT && _name == "Cherrim-sunshine" && battle.Weather.Get() != "sun" &&
            battle.Weather.Get() != "h-sun") Form("Cherrim");

        // Bad Dreams
        if (otherpoke != null && otherpoke.Ability() == Impl.Ability.BAD_DREAMS && NonVolatileEffect.Sleep())
            msg += Damage(StartingHp / 8, battle, source: $"{otherpoke.Name}'s bad dreams");
        // Leech seed
        if (LeechSeed && otherpoke != null)
        {
            var damage = StartingHp / 8;
            msg += Damage(damage, battle, attacker: otherpoke, drainHealRatio: 1, source: "leech seed");
        }

        // Curse
        if (Curse) msg += Damage(StartingHp / 4, battle, source: "its curse");
        // Syrup bomb
        if (SyrupBomb.Active() && otherpoke != null) msg += AppendSpeed(-1, otherpoke, source: "its syrup coating");

        // Weather damages
        if (Ability() == Impl.Ability.OVERCOAT)
        {
            // No damage from weather
        }
        else if (HeldItem.Get() == "safety-goggles")
        {
            // No damage from weather
        }
        else if (battle.Weather.Get() == "sandstorm")
        {
            if (!TypeIds.Contains(ElementType.ROCK) && !TypeIds.Contains(ElementType.GROUND) &&
                !TypeIds.Contains(ElementType.STEEL)
                && Ability() != Impl.Ability.SAND_RUSH && Ability() != Impl.Ability.SAND_VEIL &&
                Ability() != Impl.Ability.SAND_FORCE)
                msg += Damage(StartingHp / 16, battle, source: "the sandstorm");
        }
        else if (battle.Weather.Get() == "hail")
        {
            if (!TypeIds.Contains(ElementType.ICE) && Ability() != Impl.Ability.SNOW_CLOAK &&
                Ability() != Impl.Ability.ICE_BODY) msg += Damage(StartingHp / 16, battle, source: "the hail");
        }

        // Bind
        if (Bind.NextTurn())
        {
            msg += $"{Name} is no longer bound!\n";
        }
        else if (Bind.Active() && otherpoke != null)
        {
            if (otherpoke.HeldItem.Get() == "binding-band")
                msg += Damage(StartingHp / 6, battle, source: $"{otherpoke.Name}'s bind");
            else
                msg += Damage(StartingHp / 8, battle, source: $"{otherpoke.Name}'s bind");
        }

        // Ingrain
        if (Ingrain)
        {
            var heal = StartingHp / 16;
            if (HeldItem.Get() == "big_root") heal = (int)(heal * 1.3);
            msg += Heal(heal, "ingrain");
        }

        // Aqua Ring
        if (AquaRing)
        {
            var heal = StartingHp / 16;
            if (HeldItem.Get() == "big_root") heal = (int)(heal * 1.3);
            msg += Heal(heal, "aqua ring");
        }

        // Octolock
        if (Octolock && otherpoke != null)
        {
            msg += AppendDefense(-1, this, source: $"{otherpoke.Name}'s octolock");
            msg += AppendSpDef(-1, this, source: $"{otherpoke.Name}'s octolock");
        }

        // Grassy Terrain
        if (battle.Terrain.Item?.ToString() == "grassy" && Grounded(battle) && !HealBlock.Active())
            msg += Heal(StartingHp / 16, "grassy terrain");

        // Goes at the end so everything in this func that checks it handles it correctly
        SwappedIn = false;

        return msg;
    }
}