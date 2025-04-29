using EeveeCore.Services.Impl;
using MongoDB.Driver;

namespace EeveeCore.Modules.Duels.Impl;

/// <summary>
///     Represents an instance of a Pokémon in a battle (duel).
///     Contains battle-specific attributes, status conditions, and temporary effects that only exist during combat.
///     Manages all state changes, move effects, and ability interactions that happen during battle.
/// </summary>
public class DuelPokemon
{
    private readonly string _nickname;
    private readonly string? _startingName;
    private string _illusionDisplayName;
    private string? _illusionName;

    /// <summary>
    ///     The actual pokemon name.
    /// </summary>
    public string? _name;


    /// <summary>
    ///     Initializes a new instance of the DuelPokemon class with comprehensive battle-relevant data.
    ///     Sets up the Pokémon's base attributes, stats, moves, and battle state.
    /// </summary>
    /// <param name="pokemonId">The database ID of the Pokémon species.</param>
    /// <param name="name">The name of the Pokémon species.</param>
    /// <param name="fullname">The fully qualified name of the Pokémon.</param>
    /// <param name="nickname">The user-given nickname of the Pokémon.</param>
    /// <param name="baseStats">A dictionary mapping Pokémon names to their base stat arrays.</param>
    /// <param name="hp">The current hit points of the Pokémon.</param>
    /// <param name="hpIV">The Individual Value for HP.</param>
    /// <param name="atkIV">The Individual Value for Attack.</param>
    /// <param name="defIV">The Individual Value for Defense.</param>
    /// <param name="spatkIV">The Individual Value for Special Attack.</param>
    /// <param name="spdefIV">The Individual Value for Special Defense.</param>
    /// <param name="speedIV">The Individual Value for Speed.</param>
    /// <param name="hpEV">The Effort Value for HP.</param>
    /// <param name="atkEV">The Effort Value for Attack.</param>
    /// <param name="defEV">The Effort Value for Defense.</param>
    /// <param name="spatkEV">The Effort Value for Special Attack.</param>
    /// <param name="spdefEV">The Effort Value for Special Defense.</param>
    /// <param name="speedEV">The Effort Value for Speed.</param>
    /// <param name="level">The level of the Pokémon.</param>
    /// <param name="natureStatDeltas">The stat modifiers applied by the Pokémon's nature.</param>
    /// <param name="shiny">Whether the Pokémon is shiny.</param>
    /// <param name="radiant">Whether the Pokémon is radiant (a rarity tier above shiny).</param>
    /// <param name="skin">The visual skin variant of the Pokémon.</param>
    /// <param name="typeIds">The elemental types of the Pokémon.</param>
    /// <param name="megaTypeIds">The elemental types of the Pokémon when Mega Evolved.</param>
    /// <param name="id">The unique identifier for this Pokémon instance.</param>
    /// <param name="heldItemData">The item the Pokémon is holding.</param>
    /// <param name="happiness">The happiness level of the Pokémon.</param>
    /// <param name="moves">The list of moves the Pokémon knows.</param>
    /// <param name="abilityId">The ID of the Pokémon's ability.</param>
    /// <param name="megaAbilityId">The ID of the Pokémon's ability when Mega Evolved.</param>
    /// <param name="weight">The weight of the Pokémon.</param>
    /// <param name="gender">The gender of the Pokémon.</param>
    /// <param name="canStillEvolve">Whether the Pokémon is capable of evolving further.</param>
    /// <param name="dislikedFlavor">The flavor the Pokémon dislikes, affecting berry effects.</param>
    public DuelPokemon(int pokemonId, string? name, string? fullname, string nickname,
        Dictionary<string?, List<int>> baseStats, int hp,
        int hpIV, int atkIV, int defIV, int spatkIV, int spdefIV, int speedIV,
        int hpEV, int atkEV, int defEV, int spatkEV, int spdefEV, int speedEV,
        int level, Dictionary<string, double> natureStatDeltas, bool shiny, bool radiant, string skin,
        List<ElementType> typeIds, List<ElementType> megaTypeIds, ulong id,
        Database.Models.Mongo.Pokemon.Item heldItemData,
        int happiness, List<Move.Move> moves, int abilityId, int megaAbilityId, int weight,
        string gender, bool canStillEvolve, string dislikedFlavor)
    {
        PokemonId = pokemonId;
        _name = name;
        _nickname = nickname;
        if (_nickname != "None")
            DisplayName = $"{_nickname} ({_name.Replace("-", " ")})";
        else
            Name = _name.Replace("-", " ");
        _illusionName = null;
        _illusionDisplayName = null;

        // Stats
        FullName = fullname;
        BaseStats = baseStats;
        Hp = hp;
        Attack = baseStats[_name][1];
        Defense = baseStats[_name][2];
        SpAtk = baseStats[_name][3];
        SpDef = baseStats[_name][4];
        Speed = baseStats[_name][5];
        HpIV = Math.Min(31, hpIV);
        AtkIV = Math.Min(31, atkIV);
        DefIV = Math.Min(31, defIV);
        SpAtkIV = Math.Min(31, spatkIV);
        SpDefIV = Math.Min(31, spdefIV);
        SpeedIV = Math.Min(31, speedIV);
        HpEV = hpEV;
        AtkEV = atkEV;
        DefEV = defEV;
        SpAtkEV = spatkEV;
        SpDefEV = spdefEV;
        SpeedEV = speedEV;
        NatureStatDeltas = natureStatDeltas;
        Moves = moves;
        AbilityId = abilityId;
        MegaAbilityId = megaAbilityId;
        TypeIds = typeIds;
        MegaTypeIds = megaTypeIds;
        _startingName = _name;
        StartingHp = hp;
        StartingHpIV = hpIV;
        StartingAtkIV = atkIV;
        StartingDefIV = defIV;
        StartingSpAtkIV = spatkIV;
        StartingSpDefIV = spdefIV;
        StartingSpeedIV = speedIV;
        StartingHpEV = hpEV;
        StartingAtkEV = atkEV;
        StartingDefEV = defEV;
        StartingSpAtkEV = spatkEV;
        StartingSpDefEV = spdefEV;
        StartingSpeedEV = speedEV;
        StartingMoves = new List<Move.Move>(moves); // Shallow copy to keep the objects but not the list itself
        StartingAbilityId = abilityId;
        StartingTypeIds = new List<ElementType>(typeIds);
        StartingWeight = Math.Max(1, weight);
        AttackStage = 0;
        DefenseStage = 0;
        SpAtkStage = 0;
        SpDefStage = 0;
        SpeedStage = 0;
        AccuracyStage = 0;
        EvasionStage = 0;
        Level = level;
        Shiny = shiny;
        Radiant = radiant;
        Skin = skin;
        Id = id;
        HeldItem = new HeldItem(heldItemData, this);
        Happiness = happiness;
        Gender = gender;
        CanStillEvolve = canStillEvolve;
        DislikedFlavor = dislikedFlavor;

        Owner = null;
        ActiveTurns = 0;

        Cursed = false;
        Switched = false;
        Minimized = false;
        NonVolatileEffect = new NonVolatileEffect(this);
        Metronome = new Metronome();
        LeechSeed = false;
        Stockpile = 0;
        Flinched = false;
        Confusion = new ExpiringEffect(0);

        CanMove = true;
        HasMoved = false;
        SwappedIn = false;
        EverSentOut = false;
        ShouldMegaEvolve = false;

        // Moves
        LastMove = null;
        LastMoveDamage = null;
        LastMoveFailed = false;
        LockedMove = null;
        ChoiceMove = null;
        Disable = new ExpiringItem();
        Taunt = new ExpiringEffect(0);
        Encore = new ExpiringItem();
        Torment = false;
        Imprison = false;
        HealBlock = new ExpiringEffect(0);
        Bide = null;
        FocusEnergy = false;
        PerishSong = new ExpiringEffect(0);
        Nightmare = false;
        DefenseCurl = false;
        FuryCutter = 0;
        Bind = new ExpiringEffect(0);
        Substitute = 0;
        Silenced = new ExpiringEffect(0);
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
        AteBerry = false;
        CorrosiveGas = false;
        StatIncreased = false;
        StatDecreased = false;
        Roost = false;
        Octolock = false;
        AttackSplit = null;
        SpAtkSplit = null;
        DefenseSplit = null;
        SpDefSplit = null;
        Autotomize = 0;
        LansatBerryAte = false;
        MicleBerryAte = false;
        TarShot = false;
        SyrupBomb = new ExpiringEffect(0);
        NumHits = 0;

        // Abilities
        FlashFire = false;
        TruantTurn = 0;
        IceRepaired = false;
        LastBerry = null;
        CudChew = new ExpiringEffect(0);
        BoosterEnergy = false;
        SupersweetSyrup = false;
    }

    /// <summary>
    ///     Gets or sets the database ID of the Pokémon species.
    /// </summary>
    public int PokemonId { get; private set; }

    /// <summary>
    ///     Gets or sets the fully qualified name of the Pokémon, which may include form information.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    ///     Gets or sets the name of the Pokémon as displayed in battle.
    ///     This may be the species name or include a nickname depending on configuration.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    ///     Gets or sets the display name of the Pokémon in battle.
    ///     May include nickname and species information.
    /// </summary>
    public string DisplayName
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>
    ///     Initialize a poke upon first sending it out.
    ///     otherpoke may be null, if two pokes are sent out at the same time and the first is killed in the send out process.
    ///     Returns a formatted message.
    /// </summary>
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

    /// <summary>
    ///     Sets a pokemon's HP to zero and cleans it up.
    ///     If a pokemon takes damage equal to its HP, use damage instead.
    ///     This method ignores focus sash and sturdy, forcing the pokemon to faint.
    ///     Returns a formatted message.
    /// </summary>
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

    /// <summary>
    ///     Attempts to confuse this poke.
    ///     Returns a formatted message.
    /// </summary>
    public string Confuse(DuelPokemon attacker = null, Move.Move move = null, string source = "")
    {
        if (Substitute > 0 && (move == null || move.IsAffectedBySubstitute())) return "";
        if (Confusion.Active()) return "";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.OWN_TEMPO) return "";
        Confusion.SetTurns(new Random().Next(2, 6));
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        var msg = $"{Name} is confused{source}!\n";
        if (HeldItem.ShouldEatBerryStatus(attacker)) msg += HeldItem.EatBerry(attacker: attacker, move: move);
        return msg;
    }

    /// <summary>
    ///     Attepts to flinch this poke.
    ///     Returns a formatted message.
    /// </summary>
    public string Flinch(DuelPokemon attacker = null, Move.Move move = null, string source = "")
    {
        var msg = "";
        if (Substitute > 0 && (move == null || move.IsAffectedBySubstitute())) return "";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.INNER_FOCUS)
            return $"{Name} resisted the urge to flinch with its inner focus!\n";
        Flinched = true;
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        msg += $"{Name} flinched{source}!\n";
        if (Ability() == Impl.Ability.STEADFAST) msg += AppendSpeed(1, this, source: "its steadfast");
        return msg;
    }

    /// <summary>
    ///     Attepts to cause attacker to infatuate this poke.
    ///     Returns a formatted message.
    /// </summary>
    public string Infatuate(DuelPokemon attacker, Move.Move move = null, string source = "")
    {
        var msg = "";
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        if (Gender.Contains("-x") || attacker.Gender.Contains("-x")) return "";
        if (Gender == attacker.Gender) return "";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.OBLIVIOUS)
            return $"{Name} is too oblivious to fall in love!\n";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.AROMA_VEIL)
            return $"{Name}'s aroma veil protects it from being infatuated!\n";
        Infatuated = attacker;
        msg += $"{Name} fell in love{source}!\n";
        if (HeldItem.Get() == "destiny-knot") msg += attacker.Infatuate(this, source: $"{Name}'s destiny knot");
        return msg;
    }

    /// <summary>
    ///     Helper function to calculate a raw stat using the base, IV, EV, level, and nature.
    ///     https://bulbapedia.bulbagarden.net/wiki/Stat#Determination_of_stats "In Generation III onward"
    /// </summary>
    public int CalculateRawStat(int baseStat, int iv, int ev, double nature)
    {
        return (int)((int)Math.Round((2 * baseStat + iv + ev / 4.0) * Level / 100 + 5) * nature);
    }

    /// <summary>
    ///     Calculates a stat based on that stat's stage changes.
    /// </summary>
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
    public int GetRawSpeed()
    {
        return CalculateRawStat(Speed, SpeedIV, SpeedEV, NatureStatDeltas["Speed"]);
    }

    /// <summary>
    ///     Helper method to call calculate_stat for attack.
    /// </summary>
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
    public int GetAccuracy(Battle battle)
    {
        return AccuracyStage;
    }

    /// <summary>
    ///     Helper method to calculate evasion stage.
    /// </summary>
    public int GetEvasion(Battle battle)
    {
        return EvasionStage;
    }

    /// <summary>
    ///     Helper method to call append_stat for attack.
    /// </summary>
    public string AppendAttack(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "attack", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for defense.
    /// </summary>
    public string AppendDefense(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "defense", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for special attack.
    /// </summary>
    public string AppendSpAtk(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "special attack", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for special defense.
    /// </summary>
    public string AppendSpDef(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "special defense", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for speed.
    /// </summary>
    public string AppendSpeed(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "speed", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for accuracy.
    /// </summary>
    public string AppendAccuracy(int stageChange, DuelPokemon attacker = null, Move.Move move = null,
        string source = "", bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "accuracy", source, checkLooping);
    }

    /// <summary>
    ///     Helper method to call append_stat for evasion.
    /// </summary>
    public string AppendEvasion(int stageChange, DuelPokemon attacker = null, Move.Move move = null, string source = "",
        bool checkLooping = true)
    {
        return AppendStat(stageChange, attacker, move, "evasion", source, checkLooping);
    }

    /// <summary>
    ///     Adds a stat stage change to this pokemon.
    ///     Returns a formatted string describing the stat change.
    /// </summary>
    public string AppendStat(int stageChange, DuelPokemon attacker, Move.Move move, string stat, string source,
        bool checkLooping = true)
    {
        var msg = "";
        if (Substitute > 0 && attacker != this && attacker != null &&
            (move == null || move.IsAffectedBySubstitute())) return "";
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        var deltaMessages = new Dictionary<int, string>
        {
            { -3, $"{Name}'s {stat} severely fell{source}!\n" },
            { -2, $"{Name}'s {stat} harshly fell{source}!\n" },
            { -1, $"{Name}'s {stat} fell{source}!\n" },
            { 1, $"{Name}'s {stat} rose{source}!\n" },
            { 2, $"{Name}'s {stat} rose sharply{source}!\n" },
            { 3, $"{Name}'s {stat} rose drastically{source}!\n" }
        };
        var delta = stageChange;
        if (Ability(attacker, move) == Impl.Ability.SIMPLE) delta *= 2;
        if (Ability(attacker, move) == Impl.Ability.CONTRARY) delta *= -1;

        int currentStage;
        switch (stat)
        {
            case "attack":
                currentStage = AttackStage;
                break;
            case "defense":
                currentStage = DefenseStage;
                break;
            case "special attack":
                currentStage = SpAtkStage;
                break;
            case "special defense":
                currentStage = SpDefStage;
                break;
            case "speed":
                currentStage = SpeedStage;
                break;
            case "accuracy":
                currentStage = AccuracyStage;
                break;
            case "evasion":
                currentStage = EvasionStage;
                break;
            default:
                throw new ArgumentException($"invalid stat {stat}");
        }

        // Cap stat stages within -6 to 6
        if (delta < 0)
        {
            //-6 -5 -4 ..  2
            // 0 -1 -2 .. -8
            var cap = currentStage * -1 - 6;
            delta = Math.Max(delta, cap);
            if (delta == 0) return $"{Name}'s {stat} won't go any lower!\n";
        }
        else
        {
            // 6  5  4 .. -2
            // 0  1  2 ..  8
            var cap = currentStage * -1 + 6;
            delta = Math.Min(delta, cap);
            if (delta == 0) return $"{Name}'s {stat} won't go any higher!\n";
        }

        // Prevent stat changes
        if (delta < 0 && attacker != this)
        {
            if (Ability(attacker, move) == Impl.Ability.CLEAR_BODY ||
                Ability(attacker, move) == Impl.Ability.WHITE_SMOKE ||
                Ability(attacker, move) == Impl.Ability.FULL_METAL_BODY)
            {
                var abilityName = ((Ability)AbilityId).GetPrettyName();
                return $"{Name}'s {abilityName} prevented its {stat} from being lowered!\n";
            }

            if (Ability(attacker, move) == Impl.Ability.HYPER_CUTTER && stat == "attack")
                return $"{Name}'s claws stayed sharp because of its hyper cutter!\n";
            if (Ability(attacker, move) == Impl.Ability.KEEN_EYE && stat == "accuracy")
                return $"{Name}'s aim stayed true because of its keen eye!\n";
            if (Ability(attacker, move) == Impl.Ability.MINDS_EYE && stat == "accuracy")
                return $"{Name}'s aim stayed true because of its mind's eye!\n";
            if (Ability(attacker, move) == Impl.Ability.BIG_PECKS && stat == "defense")
                return $"{Name}'s defense stayed strong because of its big pecks!\n";
            if (Owner.Mist.Active() && (attacker == null || attacker.Ability() != Impl.Ability.INFILTRATOR))
                return $"The mist around {Name}'s feet prevented its {stat} from being lowered!\n";
            if (Ability(attacker, move) == Impl.Ability.FLOWER_VEIL && TypeIds.Contains(ElementType.GRASS)) return "";
            if (Ability(attacker, move) == Impl.Ability.MIRROR_ARMOR && attacker != null && checkLooping)
            {
                msg += $"{Name} reflected the stat change with its mirror armor!\n";
                msg += attacker.AppendStat(delta, this, null, stat, "", false);
                return msg;
            }
        }

        switch (delta)
        {
            // Remember if stats were changed for certain moves
            case > 0:
                StatIncreased = true;
                break;
            case < 0:
                StatDecreased = true;
                break;
        }

        switch (stat)
        {
            case "attack":
                AttackStage += delta;
                break;
            case "defense":
                DefenseStage += delta;
                break;
            case "special attack":
                SpAtkStage += delta;
                break;
            case "special defense":
                SpDefStage += delta;
                break;
            case "speed":
                SpeedStage += delta;
                break;
            case "accuracy":
                AccuracyStage += delta;
                break;
            case "evasion":
                EvasionStage += delta;
                break;
            default:
                throw new ArgumentException($"invalid stat {stat}");
        }

        var formattedDelta = Math.Min(Math.Max(delta, -3), 3);
        msg += deltaMessages[formattedDelta];

        // TODO: fix this hacky way of doing this, but probably not until multi battles...
        var battle = HeldItem.Battle;

        switch (delta)
        {
            // Effects that happen after a pokemon gains stats
            case < 0:
            {
                if (attacker != this)
                {
                    if (Ability(attacker, move) == Impl.Ability.DEFIANT)
                        msg += AppendAttack(2, this, source: "its defiance");
                    if (Ability(attacker, move) == Impl.Ability.COMPETITIVE)
                        msg += AppendSpAtk(2, this, source: "its competitiveness");
                }

                if (HeldItem.Get() == "eject-pack")
                {
                    // This assumes that neither attacker or poke are needed if not checking traps
                    var swaps = Owner.ValidSwaps(null, null, false);
                    if (swaps.Count > 0)
                    {
                        msg += $"{Name} is switched out by its eject pack!\n";
                        HeldItem.Use();
                        msg += Remove(battle);
                        // Force this pokemon to immediately return to be attacked
                        Owner.MidTurnRemove = true;
                    }
                }

                break;
            }
            case > 0:
            {
                foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
                    if (poke != null && poke != this && poke.Ability() == Impl.Ability.OPPORTUNIST && checkLooping)
                    {
                        msg += $"{poke.Name} seizes the opportunity to boost its stat with its opportunist!\n";
                        msg += poke.AppendStat(delta, poke, null, stat, "", false);
                    }

                break;
            }
        }

        return msg;
    }

    /// <summary>
    ///     Returns True if this pokemon is considered "grounded".
    ///     Explicit grounding applies first, then explicit ungrounding, then implicit grounding.
    /// </summary>
    public bool Grounded(Battle battle, DuelPokemon attacker = null, Move.Move move = null)
    {
        if (battle.Gravity.Active()) return true;
        if (HeldItem.Get() == "iron-ball") return true;
        if (GroundedByMove) return true;
        if (TypeIds.Contains(ElementType.FLYING) && !Roost) return false;
        if (Ability(attacker, move) == Impl.Ability.LEVITATE) return false;
        if (HeldItem.Get() == "air-balloon") return false;
        if (MagnetRise.Active()) return false;
        if (Telekinesis.Active()) return false;
        return true;
    }

    /// <summary>
    ///     Transforms this poke into otherpoke.
    /// </summary>
    public void Transform(DuelPokemon otherpoke)
    {
        ChoiceMove = null;
        _name = otherpoke._name;
        if (_nickname != "None")
            Name = $"{_nickname} ({_name.Replace('-', ' ')})";
        else
            Name = _name.Replace("-", " ");
        Attack = otherpoke.Attack;
        Defense = otherpoke.Defense;
        SpAtk = otherpoke.SpAtk;
        SpDef = otherpoke.SpDef;
        Speed = otherpoke.Speed;
        HpIV = otherpoke.HpIV;
        AtkIV = otherpoke.AtkIV;
        DefIV = otherpoke.DefIV;
        SpAtkIV = otherpoke.SpAtkIV;
        SpDefIV = otherpoke.SpDefIV;
        SpeedIV = otherpoke.SpeedIV;
        HpEV = otherpoke.HpEV;
        AtkEV = otherpoke.AtkEV;
        DefEV = otherpoke.DefEV;
        SpAtkEV = otherpoke.SpAtkEV;
        SpDefEV = otherpoke.SpDefEV;
        SpeedEV = otherpoke.SpeedEV;
        Moves = otherpoke.Moves.Select(move => move.Copy()).ToList();
        foreach (var m in Moves) m.PP = 5;
        AbilityId = otherpoke.AbilityId;
        TypeIds = new List<ElementType>(otherpoke.TypeIds);
        AttackStage = otherpoke.AttackStage;
        DefenseStage = otherpoke.DefenseStage;
        SpAtkStage = otherpoke.SpAtkStage;
        SpDefStage = otherpoke.SpDefStage;
        SpeedStage = otherpoke.SpeedStage;
        AccuracyStage = otherpoke.AccuracyStage;
        EvasionStage = otherpoke.EvasionStage;
    }

    /// <summary>
    ///     Changes this poke's form to the provided form.
    ///     This changes its name and base stats, and may affect moves, abilities, and items.
    ///     Returns True if the poke successfully reformed.
    /// </summary>
    public bool Form(string? form)
    {
        if (!BaseStats.ContainsKey(form)) return false;
        _name = form;
        if (_nickname != "None")
            DisplayName = $"{_nickname} ({_name.Replace('-', ' ')})";
        else
            DisplayName = _name.Replace("-", " ");
        Attack = BaseStats[_name][1];
        Defense = BaseStats[_name][2];
        SpAtk = BaseStats[_name][3];
        SpDef = BaseStats[_name][4];
        Speed = BaseStats[_name][5];
        AttackSplit = null;
        SpAtkSplit = null;
        DefenseSplit = null;
        SpDefSplit = null;
        Autotomize = 0;
        return true;
    }

    /// <summary>
    ///     Calculates a double representing the effectiveness of `attacker_type` damage on this poke.
    /// </summary>
    public double Effectiveness(ElementType attackerType, Battle battle, DuelPokemon attacker = null,
        Move.Move move = null)
    {
        if (attackerType == ElementType.TYPELESS) return 1;
        double effectiveness = 1;
        foreach (var defenderType in TypeIds)
        {
            if (defenderType == ElementType.TYPELESS) continue;
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
            if (!battle.TypeEffectiveness.ContainsKey(key)) continue;
            var e = battle.TypeEffectiveness[key] / 100.0;
            if (defenderType == ElementType.FLYING && e > 1 && move != null && battle.Weather.Get() == "h-wind") e = 1;
            if (battle.InverseBattle)
                switch (e)
                {
                    case < 1:
                        e = 2;
                        break;
                    case > 1:
                        e = 0.5;
                        break;
                }

            effectiveness *= e;
        }

        if (attackerType == ElementType.FIRE && TarShot) effectiveness *= 2;
        if (effectiveness >= 1 && Hp == StartingHp && Ability(attacker, move) == Impl.Ability.TERA_SHELL)
            effectiveness = 0.5;
        return effectiveness;
    }

    /// <summary>
    ///     Returns this pokemon's current weight.
    ///     Dynamically modifies the weight based on the ability of this pokemon.
    /// </summary>
    public int Weight(DuelPokemon attacker = null, Move.Move move = null)
    {
        var curAbility = Ability(attacker, move);
        var curWeight = StartingWeight;
        switch (curAbility)
        {
            case Impl.Ability.HEAVY_METAL:
                curWeight *= 2;
                break;
            case Impl.Ability.LIGHT_METAL:
                curWeight /= 2;
                curWeight = Math.Max(1, curWeight);
                break;
        }

        curWeight -= Autotomize * 1000;
        curWeight = Math.Max(1, curWeight);
        return curWeight;
    }

    /// <summary>
    ///     Returns this pokemon's current Duels.Ability.
    ///     Returns 0 in cases where the ability is blocked or nullified.
    /// </summary>
    public Ability Ability(DuelPokemon attacker = null, Move.Move move = null)
    {
        // Currently there are two categories of ability ignores, and both only apply when a move is used.
        // Since this could change, the method signature is flexible. However, without both present, it
        // should not consider the existing options.
        if (move == null || attacker == null || attacker == this) return (Ability)AbilityId;
        if (!AbilityIgnorable()) return (Ability)AbilityId;
        if (move.Effect is 411 or 460) return 0;
        switch (attacker.AbilityId)
        {
            case (int)Impl.Ability.MOLD_BREAKER or (int)Impl.Ability.TURBOBLAZE or (int)Impl.Ability.TERAVOLT
                or (int)Impl.Ability.NEUTRALIZING_GAS:
            case (int)Impl.Ability.MYCELIUM_MIGHT when move.DamageClass == DamageClass.STATUS:
                return 0;
            default:
                return (Ability)AbilityId;
        }
    }

    /// <summary>
    ///     Returns True if this pokemon's current ability can be changed.
    /// </summary>
    public bool AbilityChangeable()
    {
        return AbilityId != (int)Impl.Ability.MULTITYPE &&
               AbilityId != (int)Impl.Ability.STANCE_CHANGE &&
               AbilityId != (int)Impl.Ability.SCHOOLING &&
               AbilityId != (int)Impl.Ability.COMATOSE &&
               AbilityId != (int)Impl.Ability.SHIELDS_DOWN &&
               AbilityId != (int)Impl.Ability.DISGUISE &&
               AbilityId != (int)Impl.Ability.RKS_SYSTEM &&
               AbilityId != (int)Impl.Ability.BATTLE_BOND &&
               AbilityId != (int)Impl.Ability.POWER_CONSTRUCT &&
               AbilityId != (int)Impl.Ability.ICE_FACE &&
               AbilityId != (int)Impl.Ability.GULP_MISSILE &&
               AbilityId != (int)Impl.Ability.ZERO_TO_HERO;
    }

    /// <summary>
    ///     Returns True if this pokemon's current ability can be given to another pokemon.
    /// </summary>
    public bool AbilityGiveable()
    {
        return AbilityId != (int)Impl.Ability.TRACE &&
               AbilityId != (int)Impl.Ability.FORECAST &&
               AbilityId != (int)Impl.Ability.FLOWER_GIFT &&
               AbilityId != (int)Impl.Ability.ZEN_MODE &&
               AbilityId != (int)Impl.Ability.ILLUSION &&
               AbilityId != (int)Impl.Ability.IMPOSTER &&
               AbilityId != (int)Impl.Ability.POWER_OF_ALCHEMY &&
               AbilityId != (int)Impl.Ability.RECEIVER &&
               AbilityId != (int)Impl.Ability.DISGUISE &&
               AbilityId != (int)Impl.Ability.STANCE_CHANGE &&
               AbilityId != (int)Impl.Ability.POWER_CONSTRUCT &&
               AbilityId != (int)Impl.Ability.ICE_FACE &&
               AbilityId != (int)Impl.Ability.HUNGER_SWITCH &&
               AbilityId != (int)Impl.Ability.GULP_MISSILE &&
               AbilityId != (int)Impl.Ability.ZERO_TO_HERO;
    }

    /// <summary>
    ///     Returns True if this pokemon's current ability can be ignored.
    /// </summary>
    public bool AbilityIgnorable()
    {
        return AbilityId is (int)Impl.Ability.AROMA_VEIL or (int)Impl.Ability.BATTLE_ARMOR
            or (int)Impl.Ability.BIG_PECKS or (int)Impl.Ability.BULLETPROOF or (int)Impl.Ability.CLEAR_BODY
            or (int)Impl.Ability.CONTRARY or (int)Impl.Ability.DAMP or (int)Impl.Ability.DAZZLING
            or (int)Impl.Ability.DISGUISE or (int)Impl.Ability.DRY_SKIN or (int)Impl.Ability.FILTER
            or (int)Impl.Ability.FLASH_FIRE or (int)Impl.Ability.FLOWER_GIFT or (int)Impl.Ability.FLOWER_VEIL
            or (int)Impl.Ability.FLUFFY or (int)Impl.Ability.FRIEND_GUARD or (int)Impl.Ability.FUR_COAT
            or (int)Impl.Ability.HEATPROOF or (int)Impl.Ability.HEAVY_METAL or (int)Impl.Ability.HYPER_CUTTER
            or (int)Impl.Ability.ICE_FACE or (int)Impl.Ability.ICE_SCALES or (int)Impl.Ability.IMMUNITY
            or (int)Impl.Ability.INNER_FOCUS or (int)Impl.Ability.INSOMNIA or (int)Impl.Ability.KEEN_EYE
            or (int)Impl.Ability.LEAF_GUARD or (int)Impl.Ability.LEVITATE or (int)Impl.Ability.LIGHT_METAL
            or (int)Impl.Ability.LIGHTNING_ROD or (int)Impl.Ability.LIMBER or (int)Impl.Ability.MAGIC_BOUNCE
            or (int)Impl.Ability.MAGMA_ARMOR or (int)Impl.Ability.MARVEL_SCALE or (int)Impl.Ability.MIRROR_ARMOR
            or (int)Impl.Ability.MOTOR_DRIVE or (int)Impl.Ability.MULTISCALE or (int)Impl.Ability.OBLIVIOUS
            or (int)Impl.Ability.OVERCOAT or (int)Impl.Ability.OWN_TEMPO or (int)Impl.Ability.PASTEL_VEIL
            or (int)Impl.Ability.PUNK_ROCK or (int)Impl.Ability.QUEENLY_MAJESTY or (int)Impl.Ability.SAND_VEIL
            or (int)Impl.Ability.SAP_SIPPER or (int)Impl.Ability.SHELL_ARMOR or (int)Impl.Ability.SHIELD_DUST
            or (int)Impl.Ability.SIMPLE or (int)Impl.Ability.SNOW_CLOAK or (int)Impl.Ability.SOLID_ROCK
            or (int)Impl.Ability.SOUNDPROOF or (int)Impl.Ability.STICKY_HOLD or (int)Impl.Ability.STORM_DRAIN
            or (int)Impl.Ability.STURDY or (int)Impl.Ability.SUCTION_CUPS or (int)Impl.Ability.SWEET_VEIL
            or (int)Impl.Ability.TANGLED_FEET or (int)Impl.Ability.TELEPATHY or (int)Impl.Ability.THICK_FAT
            or (int)Impl.Ability.UNAWARE or (int)Impl.Ability.VITAL_SPIRIT or (int)Impl.Ability.VOLT_ABSORB
            or (int)Impl.Ability.WATER_ABSORB or (int)Impl.Ability.WATER_BUBBLE or (int)Impl.Ability.WATER_VEIL
            or (int)Impl.Ability.WHITE_SMOKE or (int)Impl.Ability.WONDER_GUARD or (int)Impl.Ability.WONDER_SKIN
            or (int)Impl.Ability.ARMOR_TAIL or (int)Impl.Ability.EARTH_EATER or (int)Impl.Ability.GOOD_AS_GOLD
            or (int)Impl.Ability.PURIFYING_SALT or (int)Impl.Ability.WELL_BAKED_BODY;
    }

    /// <summary>
    ///     Returns a OldMove that can be used with assist, or null if none exists.
    ///     This selects a random move from the pool of moves from pokes in the user's party that are eligable.
    /// </summary>
    public Move.Move? GetAssistMove()
    {
        var moves = (from t in Owner.Party.Where((t, idx) => idx != Owner.LastIdx)
            from move in t.Moves
            where move.SelectableByAssist()
            select move).ToList();

        return moves.Count == 0 ? null : moves[new Random().Next(moves.Count)];
    }

    /// <summary>
    ///     Creates a new DuelPokemon object asynchronously using the raw data provided.
    /// </summary>
    public static async Task<DuelPokemon> Create(IInteractionContext ctx,
        Database.Models.PostgreSQL.Pokemon.Pokemon pokemon, IMongoService mongoService)
    {
        // Initialize local variables from pokemon object
        var pn = pokemon.PokemonName;
        var nick = pokemon.Nickname;
        var hpiv = Math.Min(31, pokemon.HpIv);
        var atkiv = Math.Min(31, pokemon.AttackIv);
        var defiv = Math.Min(31, pokemon.DefenseIv);
        var spatkiv = Math.Min(31, pokemon.SpecialAttackIv);
        var spdefiv = Math.Min(31, pokemon.SpecialDefenseIv);
        var speediv = Math.Min(31, pokemon.SpeedIv);
        var happiness = pokemon.Happiness;
        var hitem = pokemon.HeldItem;
        var plevel = pokemon.Level;
        var id = pokemon.Id;
        var gender = pokemon.Gender;

        // Validate IVs
        var totalIvs = hpiv + atkiv + defiv + spatkiv + spdefiv + speediv;
        var ivPercentage = Math.Round(totalIvs / 186.0 * 100, 2);
        if (ivPercentage > 100.0) throw new ArgumentException($"IVs must be 100.0% or less, but got {ivPercentage}%");

        // Normalize form name - check if it's a battle form that shouldn't start this way
        pn = NormalizeFormName(pn, plevel);

        // Prepare for potential mega evolution
        string megaForm = null;
        if (pn != "Rayquaza")
            megaForm = hitem switch
            {
                "mega-stone" => pn + "-mega",
                "mega-stone-x" => pn + "-mega-x",
                "mega-stone-y" => pn + "-mega-y",
                _ => null
            };
        else if (pokemon.Moves.Contains("dragon-ascent")) megaForm = pn + "-mega";

        // Determine forms we need data for
        var extraForms = GetExtraForms(pn);
        if (megaForm != null)
            extraForms.Add(megaForm);

        // Create a list of tasks for parallel execution
        var tasks = new List<Task>();

        // Get form information
        var formInfoTask = mongoService.Forms.Find(f => f.Identifier == pn.ToLower()).FirstOrDefaultAsync();
        tasks.Add(formInfoTask);

        // Get nature data
        var natureTask = mongoService.Natures.Find(n => n.Identifier == pokemon.Nature.ToLower()).FirstOrDefaultAsync();
        tasks.Add(natureTask);

        // Get item data
        var itemTask = mongoService.Items.Find(i => i.Identifier == hitem).FirstOrDefaultAsync();
        tasks.Add(itemTask);

        // Wait for these initial tasks to complete
        await Task.WhenAll(tasks);

        var formInfo = await formInfoTask;
        var natureData = await natureTask;
        var hitemData = await itemTask;

        tasks.Clear();

        // Get stat types for nature
        var decStatTask = mongoService.StatTypes.Find(s => s.StatId == natureData.DecreasedStatId)
            .FirstOrDefaultAsync();
        var incStatTask = mongoService.StatTypes.Find(s => s.StatId == natureData.IncreasedStatId)
            .FirstOrDefaultAsync();

        // Get type data
        var typeDataTask = mongoService.PokemonTypes.Find(pt => pt.PokemonId == formInfo.PokemonId)
            .FirstOrDefaultAsync();

        // Get stats data
        var statsDataTask = mongoService.PokemonStats.Find(ps => ps.PokemonId == formInfo.PokemonId)
            .FirstOrDefaultAsync();

        // Get ability data
        var abilityRecordsTask =
            mongoService.PokeAbilities.Find(pa => pa.PokemonId == formInfo.PokemonId).ToListAsync();

        // Check evolution
        var pid = await GetBasePokemonId(pn, formInfo, mongoService);
        var evoCheckTask = mongoService.PFile.Find(pf => pf.EvolvesFromSpeciesId == pid).FirstOrDefaultAsync();

        // Wait for second batch of tasks
        await Task.WhenAll(decStatTask, incStatTask, typeDataTask, statsDataTask, abilityRecordsTask, evoCheckTask);

        var decStat = await decStatTask;
        var incStat = await incStatTask;
        var typeData = await typeDataTask;
        var statsData = await statsDataTask;
        var abilityRecords = await abilityRecordsTask;
        var evoCheck = await evoCheckTask;

        // Process stats
        var stats = statsData.Stats.ToList();
        var pokemonHp = stats[0];

        // Process nature
        var decStatName = decStat.Identifier.Capitalize().Replace("-", " ");
        var incStatName = incStat.Identifier.Capitalize().Replace("-", " ");

        // Initialize nature stat modifiers
        var natureStatDeltas = new Dictionary<string, double>
        {
            { "Attack", 1.0 },
            { "Defense", 1.0 },
            { "Special Attack", 1.0 },
            { "Special attack", 1.0 },
            { "Special Defense", 1.0 },
            { "Special defense", 1.0 },
            { "Speed", 1.0 }
        };

        var flavorMap = new Dictionary<string, string>
        {
            { "Attack", "spicy" },
            { "Defense", "sour" },
            { "Speed", "sweet" },
            { "Special Attack", "dry" },
            { "Special attack", "dry" },
            { "Special Defense", "bitter" },
            { "Special defense", "bitter" }
        };

        var dislikedFlavor = "";
        if (decStatName != incStatName)
        {
            natureStatDeltas[decStatName] = 0.9;
            natureStatDeltas[incStatName] = 1.1;
            dislikedFlavor = flavorMap[decStatName];
        }

        // Store base stats
        var baseStats = new Dictionary<string?, List<int>>
        {
            { pn, stats }
        };

        // Get type IDs
        var typeIds = typeData.Types.Select(t => (ElementType)t).ToList();

        // Process abilities
        var abIds = abilityRecords.Select(record => record.AbilityId).ToList();
        var abId = abIds.Intersect(abilityRecords.Select(x => x.AbilityId)).FirstOrDefault();

        // Process evolution
        var canStillEvolve = evoCheck != null;
        if (pn == "Floette-eternal") canStillEvolve = false;

        // Handle Shedinja special case
        if (pn == "Shedinja")
            pokemonHp = 1;
        else
            pokemonHp = (int)Math.Round((2 * pokemonHp + hpiv + pokemon.HpEv / 4.0) * plevel / 100 + plevel + 10);

        // Process mega evolution data if applicable
        var megaAbilityId = 0;
        List<ElementType> megaTypeIds = null;

        if (megaForm != null)
        {
            var megaDataTasks = await GetMegaData(megaForm, mongoService);
            if (megaDataTasks.megaAbilityId != 0)
            {
                megaAbilityId = megaDataTasks.megaAbilityId;
                megaTypeIds = megaDataTasks.megaTypeIds;
            }
        }

        // Process form stats
        if (extraForms.Count > 0) await LoadFormStats(extraForms, baseStats, mongoService);

        // Process moves
        var objectMoves = await ProcessMoves(pokemon.Moves.ToList(), mongoService);

        return new DuelPokemon(
            pid,
            pn,
            pokemon.PokemonName,
            nick,
            baseStats,
            pokemonHp,
            hpiv,
            atkiv,
            defiv,
            spatkiv,
            spdefiv,
            speediv,
            pokemon.HpEv,
            pokemon.AttackEv,
            pokemon.DefenseEv,
            pokemon.SpecialAttackEv,
            pokemon.SpecialDefenseEv,
            pokemon.SpeedEv,
            plevel,
            natureStatDeltas,
            pokemon.Shiny.GetValueOrDefault(),
            pokemon.Radiant.GetValueOrDefault(),
            pokemon.Skin,
            typeIds,
            megaTypeIds,
            id,
            hitemData,
            happiness,
            objectMoves.ToList(),
            abId,
            megaAbilityId,
            formInfo.Weight ?? 20,
            gender,
            canStillEvolve,
            dislikedFlavor);
    }

// Helper methods
    private static string? NormalizeFormName(string? pn, int plevel)
    {
        return pn switch
        {
            "Mimikyu-busted" => "Mimikyu",
            "Cramorant-gorging" or "Cramorant-gulping" => "Cramorant",
            "Eiscue-noice" => "Eiscue",
            "Darmanitan-zen" => "Darmanitan",
            "Darmanitan-zen-galar" => "Darmanitan-galar",
            "Aegislash-blade" => "Aegislash",
            not null when pn.StartsWith("Minior-") && (pn.EndsWith("red") || pn.EndsWith("orange") ||
                                                       pn.EndsWith("yellow") || pn.EndsWith("green") ||
                                                       pn.EndsWith("blue") || pn.EndsWith("indigo") ||
                                                       pn.EndsWith("violet")) => "Minior",
            "Wishiwashi" when plevel >= 20 => "Wishiwashi-school",
            "Wishiwashi-school" when plevel < 20 => "Wishiwashi",
            "Greninja-ash" => "Greninja",
            "Zygarde-complete" => "Zygarde",
            "Morpeko-hangry" => "Morpeko",
            "Cherrim-sunshine" => "Cherrim",
            not null when pn.StartsWith("Castform-") => "Castform",
            not null when pn.StartsWith("Arceus-") => "Arceus",
            not null when pn.StartsWith("Silvally-") => "Silvally",
            "Palafin-hero" => "Palafin",
            not null when pn.EndsWith("-mega-x") || pn.EndsWith("-mega-y") => pn[..^7],
            not null when pn.EndsWith("-mega") => pn[..^5],
            _ => pn
        };
    }

    private static List<string> GetExtraForms(string? pn)
    {
        return pn switch
        {
            "Mimikyu" => ["Mimikyu-busted"],
            "Cramorant" => ["Cramorant-gorging", "Cramorant-gulping"],
            "Eiscue" => ["Eiscue-noice"],
            "Darmanitan" => ["Darmanitan-zen"],
            "Darmanitan-galar" => ["Darmanitan-zen-galar"],
            "Aegislash" => ["Aegislash-blade"],
            "Minior" =>
            [
                "Minior-red", "Minior-orange", "Minior-yellow", "Minior-green",
                "Minior-blue", "Minior-indigo", "Minior-violet"
            ],
            "Wishiwashi" => ["Wishiwashi-school"],
            "Wishiwashi-school" => ["Wishiwashi"],
            "Greninja" => ["Greninja-ash"],
            "Zygarde" or "Zygarde-10" => ["Zygarde-complete"],
            "Morpeko" => ["Morpeko-hangry"],
            "Cherrim" => ["Cherrim-sunshine"],
            "Castform" => ["Castform-snowy", "Castform-rainy", "Castform-sunny"],
            "Arceus" => GetArceusFormsList(),
            "Silvally" => GetSilvallyFormsList(),
            "Palafin" => ["Palafin-hero"],
            _ => []
        };
    }

    private static List<string> GetArceusFormsList()
    {
        return
        [
            "Arceus-dragon", "Arceus-dark", "Arceus-ground", "Arceus-fighting",
            "Arceus-fire", "Arceus-ice", "Arceus-bug", "Arceus-steel",
            "Arceus-grass", "Arceus-psychic", "Arceus-fairy", "Arceus-flying",
            "Arceus-water", "Arceus-ghost", "Arceus-rock", "Arceus-poison",
            "Arceus-electric"
        ];
    }

    private static List<string> GetSilvallyFormsList()
    {
        return
        [
            "Silvally-psychic", "Silvally-fairy", "Silvally-flying", "Silvally-water",
            "Silvally-ghost", "Silvally-rock", "Silvally-poison", "Silvally-electric",
            "Silvally-dragon", "Silvally-dark", "Silvally-ground", "Silvally-fighting",
            "Silvally-fire", "Silvally-ice", "Silvally-bug", "Silvally-steel",
            "Silvally-grass"
        ];
    }

    private static async Task<int> GetBasePokemonId(string? pn, dynamic formInfo, IMongoService mongoService)
    {
        if (!IsFormVariant(pn)) return formInfo.PokemonId;
        var name = pn.ToLower().Split('-')[0];
        var originalFormInfo = await mongoService.Forms.Find(f => f.Identifier == name).FirstOrDefaultAsync();
        return originalFormInfo.PokemonId;
    }

    private static bool IsFormVariant(string? pn)
    {
        return pn.Contains('-');
    }

    private static async Task<(int megaAbilityId, List<ElementType> megaTypeIds)> GetMegaData(string megaForm,
        IMongoService mongoService)
    {
        var megaFormInfo = await mongoService.Forms.Find(f => f.Identifier == megaForm.ToLower())
            .FirstOrDefaultAsync();

        if (megaFormInfo == null)
            return (0, null);

        var megaAbilityTask = mongoService.PokeAbilities.Find(pa => pa.PokemonId == megaFormInfo.PokemonId)
            .FirstOrDefaultAsync();
        var megaTypesTask = mongoService.PokemonTypes.Find(pt => pt.PokemonId == megaFormInfo.PokemonId)
            .FirstOrDefaultAsync();

        await Task.WhenAll(megaAbilityTask, megaTypesTask);

        var megaAbility = await megaAbilityTask;
        var megaTypes = await megaTypesTask;

        if (megaAbility == null)
            throw new InvalidOperationException("mega form missing ability in `poke_abilities`");

        if (megaTypes == null)
            throw new InvalidOperationException("mega form missing types in `ptypes`");

        return (megaAbility.AbilityId, megaTypes.Types.Select(x => (ElementType)x).ToList());
    }

    private static async Task LoadFormStats(List<string> forms, Dictionary<string?, List<int>> baseStats,
        IMongoService mongoService)
    {
        var formTasks = forms.Select(formName => GetFormStats(formName, mongoService)).ToList();

        (string? formName, List<int> stats)[] results = await Task.WhenAll(formTasks);

        foreach (var (formName, stats) in results) baseStats[formName] = stats;
    }

    private static async Task<(string formName, List<int> stats)> GetFormStats(string formName,
        IMongoService mongoService)
    {
        var formData = await mongoService.Forms.Find(f => f.Identifier == formName.ToLower()).FirstOrDefaultAsync();
        var formStats = await mongoService.PokemonStats.Find(ps => ps.PokemonId == formData.PokemonId)
            .FirstOrDefaultAsync();

        return (formName, formStats.Stats.ToList());
    }

    private static async Task<Move.Move[]> ProcessMoves(List<string> moves, IMongoService mongoService)
    {
        var moveTasks = moves.Select(moveName => CreateMove(moveName, mongoService)).ToList();

        return await Task.WhenAll(moveTasks);
    }

    private static async Task<Move.Move> CreateMove(string moveName, IMongoService mongoService)
    {
        ElementType? typeOverride = null;
        var moveIdentifier = moveName;

        if (moveName.StartsWith("hidden-power-"))
        {
            var element = moveName.Split('-')[2];
            moveIdentifier = "hidden-power";
            typeOverride = (ElementType)Enum.Parse(typeof(ElementType), element.ToUpper());
        }

        var dbMove = await mongoService.Moves.Find(m => m.Identifier == moveIdentifier).FirstOrDefaultAsync() ??
                     await mongoService.Moves.Find(m => m.Identifier == "tackle").FirstOrDefaultAsync();

        // Create game move from database move
        var gameMove = new Move.Move(dbMove);

        // Apply type override if needed
        if (typeOverride.HasValue) gameMove.Type = typeOverride.Value;

        return gameMove;
    }

    #region Stat Properties

    /// <summary>
    ///     Gets the dictionary of base stats for each Pokémon species.
    /// </summary>
    public Dictionary<string?, List<int>> BaseStats { get; }

    /// <summary>
    ///     Gets or sets the current hit points of the Pokémon.
    /// </summary>
    public int Hp { get; set; }

    /// <summary>
    ///     Gets the base Attack stat of the Pokémon.
    /// </summary>
    public int Attack { get; private set; }

    /// <summary>
    ///     Gets the base Defense stat of the Pokémon.
    /// </summary>
    public int Defense { get; private set; }

    /// <summary>
    ///     Gets the base Special Attack stat of the Pokémon.
    /// </summary>
    public int SpAtk { get; private set; }

    /// <summary>
    ///     Gets the base Special Defense stat of the Pokémon.
    /// </summary>
    public int SpDef { get; private set; }

    /// <summary>
    ///     Gets or sets the base Speed stat of the Pokémon.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    ///     Gets the Individual Value (IV) for the HP stat, capped at 31.
    /// </summary>
    public int HpIV { get; private set; }

    /// <summary>
    ///     Gets the Individual Value (IV) for the Attack stat, capped at 31.
    /// </summary>
    public int AtkIV { get; private set; }

    /// <summary>
    ///     Gets the Individual Value (IV) for the Defense stat, capped at 31.
    /// </summary>
    public int DefIV { get; private set; }

    /// <summary>
    ///     Gets the Individual Value (IV) for the Special Attack stat, capped at 31.
    /// </summary>
    public int SpAtkIV { get; private set; }

    /// <summary>
    ///     Gets the Individual Value (IV) for the Special Defense stat, capped at 31.
    /// </summary>
    public int SpDefIV { get; private set; }

    /// <summary>
    ///     Gets the Individual Value (IV) for the Speed stat, capped at 31.
    /// </summary>
    public int SpeedIV { get; private set; }

    /// <summary>
    ///     Gets the Effort Value (EV) for the HP stat.
    /// </summary>
    public int HpEV { get; private set; }

    /// <summary>
    ///     Gets the Effort Value (EV) for the Attack stat.
    /// </summary>
    public int AtkEV { get; private set; }

    /// <summary>
    ///     Gets the Effort Value (EV) for the Defense stat.
    /// </summary>
    public int DefEV { get; private set; }

    /// <summary>
    ///     Gets the Effort Value (EV) for the Special Attack stat.
    /// </summary>
    public int SpAtkEV { get; private set; }

    /// <summary>
    ///     Gets the Effort Value (EV) for the Special Defense stat.
    /// </summary>
    public int SpDefEV { get; private set; }

    /// <summary>
    ///     Gets the Effort Value (EV) for the Speed stat.
    /// </summary>
    public int SpeedEV { get; private set; }

    /// <summary>
    ///     Gets the stat modifiers applied by the Pokémon's nature.
    ///     Maps stat names to multipliers (e.g., "attack" to 1.1 for a 10% boost).
    /// </summary>
    public Dictionary<string, double> NatureStatDeltas { get; }

    /// <summary>
    ///     Gets or sets the moves the Pokémon knows.
    /// </summary>
    public List<Move.Move> Moves { get; private set; }

    /// <summary>
    ///     Gets or sets the ID of the Pokémon's ability.
    /// </summary>
    public int AbilityId { get; set; }

    /// <summary>
    ///     Gets the ID of the Pokémon's ability when Mega Evolved.
    /// </summary>
    public int MegaAbilityId { get; private set; }

    /// <summary>
    ///     Gets or sets the elemental types of the Pokémon.
    /// </summary>
    public List<ElementType> TypeIds { get; set; }

    /// <summary>
    ///     Gets the elemental types of the Pokémon when Mega Evolved.
    /// </summary>
    public List<ElementType> MegaTypeIds { get; private set; }

    /// <summary>
    ///     Gets the original hit points of the Pokémon at the start of battle.
    /// </summary>
    public int StartingHp { get; private set; }

    /// <summary>
    ///     Gets the original HP IV value of the Pokémon.
    /// </summary>
    public int StartingHpIV { get; }

    /// <summary>
    ///     Gets the original Attack IV value of the Pokémon.
    /// </summary>
    public int StartingAtkIV { get; }

    /// <summary>
    ///     Gets the original Defense IV value of the Pokémon.
    /// </summary>
    public int StartingDefIV { get; }

    /// <summary>
    ///     Gets the original Special Attack IV value of the Pokémon.
    /// </summary>
    public int StartingSpAtkIV { get; }

    /// <summary>
    ///     Gets the original Special Defense IV value of the Pokémon.
    /// </summary>
    public int StartingSpDefIV { get; }

    /// <summary>
    ///     Gets the original Speed IV value of the Pokémon.
    /// </summary>
    public int StartingSpeedIV { get; }

    /// <summary>
    ///     Gets the original HP EV value of the Pokémon.
    /// </summary>
    public int StartingHpEV { get; }

    /// <summary>
    ///     Gets the original Attack EV value of the Pokémon.
    /// </summary>
    public int StartingAtkEV { get; }

    /// <summary>
    ///     Gets the original Defense EV value of the Pokémon.
    /// </summary>
    public int StartingDefEV { get; }

    /// <summary>
    ///     Gets the original Special Attack EV value of the Pokémon.
    /// </summary>
    public int StartingSpAtkEV { get; }

    /// <summary>
    ///     Gets the original Special Defense EV value of the Pokémon.
    /// </summary>
    public int StartingSpDefEV { get; }

    /// <summary>
    ///     Gets the original Speed EV value of the Pokémon.
    /// </summary>
    public int StartingSpeedEV { get; }

    /// <summary>
    ///     Gets the original moves the Pokémon knew at the start of battle.
    /// </summary>
    public List<Move.Move> StartingMoves { get; }

    /// <summary>
    ///     Gets or sets the original ability ID of the Pokémon.
    /// </summary>
    public int StartingAbilityId { get; set; }

    /// <summary>
    ///     Gets or sets the original elemental types of the Pokémon.
    /// </summary>
    public List<ElementType> StartingTypeIds { get; set; }

    /// <summary>
    ///     Gets the original weight of the Pokémon in tenths of kilograms (10 weight = 1 kg).
    ///     Minimum weight is 1.
    /// </summary>
    public int StartingWeight { get; }

    /// <summary>
    ///     Gets or sets the Attack stat stage modifier (-6 to +6).
    /// </summary>
    public int AttackStage { get; set; }

    /// <summary>
    ///     Gets or sets the Defense stat stage modifier (-6 to +6).
    /// </summary>
    public int DefenseStage { get; set; }

    /// <summary>
    ///     Gets or sets the Special Attack stat stage modifier (-6 to +6).
    /// </summary>
    public int SpAtkStage { get; set; }

    /// <summary>
    ///     Gets or sets the Special Defense stat stage modifier (-6 to +6).
    /// </summary>
    public int SpDefStage { get; set; }

    /// <summary>
    ///     Gets or sets the Speed stat stage modifier (-6 to +6).
    /// </summary>
    public int SpeedStage { get; set; }

    /// <summary>
    ///     Gets or sets the Accuracy stat stage modifier (-6 to +6).
    /// </summary>
    public int AccuracyStage { get; set; }

    /// <summary>
    ///     Gets or sets the Evasion stat stage modifier (-6 to +6).
    /// </summary>
    public int EvasionStage { get; set; }

    /// <summary>
    ///     Gets the level of the Pokémon.
    /// </summary>
    public int Level { get; }

    /// <summary>
    ///     Gets whether the Pokémon is shiny.
    /// </summary>
    public bool Shiny { get; private set; }

    /// <summary>
    ///     Gets whether the Pokémon is radiant (a rarity tier above shiny).
    /// </summary>
    public bool Radiant { get; private set; }

    /// <summary>
    ///     Gets the visual skin variant of the Pokémon.
    /// </summary>
    public string Skin { get; private set; }

    /// <summary>
    ///     Gets the unique identifier for this Pokémon instance.
    /// </summary>
    public ulong Id { get; }

    /// <summary>
    ///     Gets the item the Pokémon is holding.
    /// </summary>
    public HeldItem HeldItem { get; private set; }

    /// <summary>
    ///     Gets the happiness level of the Pokémon.
    /// </summary>
    public int Happiness { get; private set; }

    /// <summary>
    ///     Gets the gender of the Pokémon.
    /// </summary>
    public string Gender { get; }

    /// <summary>
    ///     Gets whether the Pokémon is capable of evolving further.
    /// </summary>
    public bool CanStillEvolve { get; }

    /// <summary>
    ///     Gets the flavor the Pokémon dislikes, affecting berry effects.
    /// </summary>
    public string DislikedFlavor { get; private set; }

    #endregion

    #region Battle State Properties

    /// <summary>
    ///     Gets or sets the trainer that owns this Pokémon in battle.
    /// </summary>
    public Trainer Owner { get; set; }

    /// <summary>
    ///     Gets or sets the number of turns this Pokémon has been active in battle.
    /// </summary>
    public int ActiveTurns { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by the Curse status effect.
    /// </summary>
    public bool Cursed { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has switched out this turn.
    /// </summary>
    public bool Switched { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has used the Minimize move.
    ///     Affects certain moves that deal double damage to minimized targets.
    /// </summary>
    public bool Minimized { get; set; }

    /// <summary>
    ///     Gets the non-volatile status effect affecting this Pokémon (e.g., Burn, Poison).
    /// </summary>
    public NonVolatileEffect NonVolatileEffect { get; }

    /// <summary>
    ///     Gets or sets the Metronome effect for this Pokémon.
    ///     Tracks consecutive use of the same move for power increase.
    /// </summary>
    public Metronome Metronome { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Leech Seed.
    /// </summary>
    public bool LeechSeed { get; set; }

    /// <summary>
    ///     Gets or sets the number of Stockpile charges this Pokémon has accumulated.
    /// </summary>
    public int Stockpile { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon flinched this turn.
    /// </summary>
    public bool Flinched { get; set; }

    /// <summary>
    ///     Gets or sets the confusion effect on this Pokémon.
    ///     Tracks duration of confusion.
    /// </summary>
    public ExpiringEffect Confusion { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon can move this turn.
    /// </summary>
    public bool CanMove { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has already moved this turn.
    /// </summary>
    public bool HasMoved { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon was swapped in this turn.
    ///     Affects whether next_turn function should be called.
    /// </summary>
    public bool SwappedIn { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has ever been sent into battle.
    /// </summary>
    public bool EverSentOut { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon should attempt to Mega Evolve this turn.
    /// </summary>
    public bool ShouldMegaEvolve { get; set; }

    #endregion

    #region Move-Related Properties

    /// <summary>
    ///     Gets or sets the last move used by this Pokémon.
    /// </summary>
    public Move.Move? LastMove { get; set; }

    /// <summary>
    ///     Gets or sets the damage taken and damage class of the last move this Pokémon was hit by.
    ///     Resets at the end of a turn.
    /// </summary>
    public Tuple<int, DamageClass>? LastMoveDamage { get; set; }

    /// <summary>
    ///     Gets or sets whether the last move used by this Pokémon failed.
    /// </summary>
    public bool LastMoveFailed { get; set; }

    /// <summary>
    ///     Gets or sets the move this Pokémon is locked into using due to multi-turn moves.
    /// </summary>
    public LockedMove? LockedMove { get; set; }

    /// <summary>
    ///     Gets or sets the move this Pokémon is locked into using due to a Choice item.
    /// </summary>
    public Move.Move? ChoiceMove { get; set; }

    /// <summary>
    ///     Gets or sets the disabled move effect on this Pokémon.
    ///     Tracks which move is disabled and for how many turns.
    /// </summary>
    public ExpiringItem Disable { get; set; }

    /// <summary>
    ///     Gets or sets the taunt effect on this Pokémon.
    ///     Prevents the use of status moves.
    /// </summary>
    public ExpiringEffect Taunt { get; set; }

    /// <summary>
    ///     Gets or sets the encore effect on this Pokémon.
    ///     Forces the Pokémon to repeat its last move.
    /// </summary>
    public ExpiringItem Encore { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Torment.
    ///     Prevents using the same move twice in a row.
    /// </summary>
    public bool Torment { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has Imprison active.
    ///     Prevents opponents from using moves known by this Pokémon.
    /// </summary>
    public bool Imprison { get; set; }

    /// <summary>
    ///     Gets or sets the Heal Block effect on this Pokémon.
    ///     Prevents the use of healing moves.
    /// </summary>
    public ExpiringEffect HealBlock { get; set; }

    /// <summary>
    ///     Gets or sets the accumulated damage for the Bide move.
    ///     Null when not using Bide.
    /// </summary>
    public int? Bide { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has used Focus Energy.
    ///     Increases critical hit ratio.
    /// </summary>
    public bool FocusEnergy { get; set; }

    /// <summary>
    ///     Gets or sets the Perish Song effect on this Pokémon.
    ///     Tracks turns until fainting.
    /// </summary>
    public ExpiringEffect PerishSong { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Nightmare.
    ///     Causes damage each turn while asleep.
    /// </summary>
    public bool Nightmare { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has used Defense Curl since entering battle.
    ///     Affects the power of certain moves like Rollout.
    /// </summary>
    public bool DefenseCurl { get; set; }

    /// <summary>
    ///     Gets or sets how many times Fury Cutter has been consecutively used.
    ///     Increases the move's power with each use.
    /// </summary>
    public int FuryCutter { get; set; }

    /// <summary>
    ///     Gets or sets the Bind effect on this Pokémon.
    ///     Prevents switching and causes damage each turn.
    /// </summary>
    public ExpiringEffect Bind { get; set; }

    /// <summary>
    ///     Gets or sets the remaining HP of this Pokémon's Substitute.
    ///     0 indicates no active Substitute.
    /// </summary>
    public int Substitute { get; set; }

    /// <summary>
    ///     Gets or sets the Silenced effect on this Pokémon.
    ///     Prevents the use of sound-based moves.
    /// </summary>
    public ExpiringEffect Silenced { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is in Rage state.
    ///     Increases Attack when hit by moves.
    /// </summary>
    public bool Rage { get; set; }

    /// <summary>
    ///     Gets or sets the Mind Reader effect on this Pokémon.
    ///     Tracks which Pokémon is guaranteed to hit this Pokémon.
    /// </summary>
    public ExpiringItem MindReader { get; set; }

    /// <summary>
    ///     Gets or sets whether Destiny Bond is active on this Pokémon.
    ///     Causes the attacker to faint if this Pokémon faints.
    /// </summary>
    public bool DestinyBond { get; set; }

    /// <summary>
    ///     Gets or sets the cooldown for using Destiny Bond again.
    /// </summary>
    public ExpiringEffect DestinyBondCooldown { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is trapped and prevented from switching out.
    ///     Does not affect moves that swap the target or user.
    /// </summary>
    public bool Trapping { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is under the effect of Ingrain.
    ///     Restores HP each turn but prevents switching out.
    /// </summary>
    public bool Ingrain { get; set; }

    /// <summary>
    ///     Gets or sets the Pokémon that this Pokémon is infatuated with due to Attract.
    ///     Null when not infatuated.
    /// </summary>
    public DuelPokemon Infatuated { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is under the effect of Aqua Ring.
    ///     Restores HP each turn.
    /// </summary>
    public bool AquaRing { get; set; }

    /// <summary>
    ///     Gets or sets the Magnet Rise effect on this Pokémon.
    ///     Makes the Pokémon immune to Ground-type moves.
    /// </summary>
    public ExpiringEffect MagnetRise { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is in the semi-invulnerable state of Dive.
    /// </summary>
    public bool Dive { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is in the semi-invulnerable state of Dig.
    /// </summary>
    public bool Dig { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is in the semi-invulnerable state of Fly or Bounce.
    /// </summary>
    public bool Fly { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is in the semi-invulnerable state of Shadow Force or Phantom Force.
    /// </summary>
    public bool ShadowForce { get; set; }

    /// <summary>
    ///     Gets or sets the Lucky Chant effect on this Pokémon.
    ///     Prevents critical hits against the Pokémon.
    /// </summary>
    public ExpiringEffect LuckyChant { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is grounded by moves like Smack Down or Thousand Arrows.
    ///     Removes Flying-type immunity to Ground-type moves.
    /// </summary>
    public bool GroundedByMove { get; set; }

    /// <summary>
    ///     Gets or sets the Charge effect on this Pokémon.
    ///     Doubles the power of the next Electric-type move.
    /// </summary>
    public ExpiringEffect Charge { get; set; }

    /// <summary>
    ///     Gets or sets the Uproar effect on this Pokémon.
    ///     Prevents sleep and causes damage each turn.
    /// </summary>
    public ExpiringEffect Uproar { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has Magic Coat active.
    ///     Reflects status moves back at the attacker.
    /// </summary>
    public bool MagicCoat { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has used Power Trick.
    ///     Swaps Attack and Defense stats.
    /// </summary>
    public bool PowerTrick { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has used Power Shift.
    ///     Swaps offensive and defensive stats.
    /// </summary>
    public bool PowerShift { get; set; }

    /// <summary>
    ///     Gets or sets the Yawn effect on this Pokémon.
    ///     Causes the Pokémon to fall asleep after a delay.
    /// </summary>
    public ExpiringEffect Yawn { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has Ion Deluge active.
    ///     Turns Normal-type moves into Electric-type this turn.
    /// </summary>
    public bool IonDeluge { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Electrify.
    ///     Changes the type of the Pokémon's move to Electric.
    /// </summary>
    public bool Electrify { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon used a protection move this turn.
    /// </summary>
    public bool ProtectionUsed { get; set; }

    /// <summary>
    ///     Gets or sets the current chance (1/x) that a protection move will succeed.
    ///     The chance decreases with consecutive uses.
    /// </summary>
    public int ProtectionChance { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Protect this turn.
    /// </summary>
    public bool Protect { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Endure this turn.
    ///     Guarantees survival with 1 HP from any attack.
    /// </summary>
    public bool Endure { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Wide Guard this turn.
    ///     Blocks multi-target moves.
    /// </summary>
    public bool WideGuard { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Crafty Shield this turn.
    ///     Blocks status moves.
    /// </summary>
    public bool CraftyShield { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by King's Shield this turn.
    ///     Blocks damage and lowers attacker's Attack if contacted.
    /// </summary>
    public bool KingShield { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Spiky Shield this turn.
    ///     Blocks damage and damages attacker if contacted.
    /// </summary>
    public bool SpikyShield { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Mat Block this turn.
    ///     Blocks damaging moves.
    /// </summary>
    public bool MatBlock { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Baneful Bunker this turn.
    ///     Blocks moves and poisons attacker if contacted.
    /// </summary>
    public bool BanefulBunker { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Quick Guard this turn.
    ///     Blocks priority moves.
    /// </summary>
    public bool QuickGuard { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Obstruct this turn.
    ///     Blocks damage and severely lowers attacker's Defense if contacted.
    /// </summary>
    public bool Obstruct { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Silk Trap this turn.
    ///     Blocks damage and lowers attacker's Speed if contacted.
    /// </summary>
    public bool SilkTrap { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is protected by Burning Bulwark this turn.
    ///     Blocks damage and burns attacker if contacted.
    /// </summary>
    public bool BurningBulwark { get; set; }

    /// <summary>
    ///     Gets or sets the Laser Focus effect on this Pokémon.
    ///     Guarantees critical hits for the Pokémon's next move.
    /// </summary>
    public ExpiringEffect LaserFocus { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Powder.
    ///     Causes damage if the Pokémon uses a Fire-type move.
    /// </summary>
    public bool Powdered { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has Snatch active.
    ///     Steals certain self-targeted moves used by other Pokémon.
    /// </summary>
    public bool Snatching { get; set; }

    /// <summary>
    ///     Gets or sets the Telekinesis effect on this Pokémon.
    ///     Makes the Pokémon immune to Ground-type moves and easier to hit.
    /// </summary>
    public ExpiringEffect Telekinesis { get; set; }

    /// <summary>
    ///     Gets or sets the Embargo effect on this Pokémon.
    ///     Prevents the Pokémon from using held items.
    /// </summary>
    public ExpiringEffect Embargo { get; set; }

    /// <summary>
    ///     Gets or sets the current power of Echoed Voice.
    ///     Increases with consecutive uses.
    /// </summary>
    public int EchoedVoicePower { get; set; }

    /// <summary>
    ///     Gets or sets whether Echoed Voice was used this turn.
    /// </summary>
    public bool EchoedVoiceUsed { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Curse.
    ///     Causes damage each turn.
    /// </summary>
    public bool Curse { get; set; }

    /// <summary>
    ///     Gets or sets the Fairy Lock effect preventing switching.
    ///     Prevents all Pokémon from switching out.
    /// </summary>
    public ExpiringEffect FairyLock { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has Grudge active.
    ///     Depletes PP from the move that causes this Pokémon to faint.
    /// </summary>
    public bool Grudge { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Foresight.
    ///     Negates type immunities for Normal and Fighting-type moves against Ghost-types.
    /// </summary>
    public bool Foresight { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Miracle Eye.
    ///     Negates type immunities for Psychic-type moves against Dark-types.
    /// </summary>
    public bool MiracleEye { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is charging Beak Blast.
    ///     Burns Pokémon that make contact during the charging turn.
    /// </summary>
    public bool BeakBlast { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by No Retreat.
    ///     Prevents switching out but boosts all stats.
    /// </summary>
    public bool NoRetreat { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has taken any damage this turn.
    /// </summary>
    public bool DmgThisTurn { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has eaten a berry during this battle.
    /// </summary>
    public bool AteBerry { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Corrosive Gas.
    ///     Prevents the use of held items for the rest of the battle.
    /// </summary>
    public bool CorrosiveGas { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has had a stat increase this turn.
    /// </summary>
    public bool StatIncreased { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has had a stat decrease this turn.
    /// </summary>
    public bool StatDecreased { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon has used Roost this turn.
    ///     Temporarily removes the Flying type.
    /// </summary>
    public bool Roost { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Octolock.
    ///     Lowers Defense and Special Defense each turn and prevents switching.
    /// </summary>
    public bool Octolock { get; set; }

    /// <summary>
    ///     Gets or sets the Attack value that this Pokémon's Attack is split with due to Power Split.
    ///     Null when not affected.
    /// </summary>
    public int? AttackSplit { get; set; }

    /// <summary>
    ///     Gets or sets the Special Attack value that this Pokémon's Special Attack is split with due to Power Split.
    ///     Null when not affected.
    /// </summary>
    public int? SpAtkSplit { get; set; }

    /// <summary>
    ///     Gets or sets the Defense value that this Pokémon's Defense is split with due to Guard Split.
    ///     Null when not affected.
    /// </summary>
    public int? DefenseSplit { get; set; }

    /// <summary>
    ///     Gets or sets the Special Defense value that this Pokémon's Special Defense is split with due to Guard Split.
    ///     Null when not affected.
    /// </summary>
    public int? SpDefSplit { get; set; }

    /// <summary>
    ///     Gets or sets the number of times Autotomize has been used since switching out.
    ///     Each use reduces weight and increases Speed.
    /// </summary>
    public int Autotomize { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon's critical hit ratio is increased from eating a Lansat Berry.
    /// </summary>
    public bool LansatBerryAte { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon's next move has increased accuracy from eating a Micle Berry.
    /// </summary>
    public bool MicleBerryAte { get; set; }

    /// <summary>
    ///     Gets or sets whether this Pokémon is affected by Tar Shot.
    ///     Makes Fire-type moves super effective against the Pokémon and lowers Speed.
    /// </summary>
    public bool TarShot { get; set; }

    /// <summary>
    ///     Gets or sets the Syrup Bomb effect on this Pokémon.
    ///     Lowers the opponent's Speed each turn.
    /// </summary>
    public ExpiringEffect SyrupBomb { get; set; }

    /// <summary>
    ///     Gets or sets the number of times this Pokémon has been hit in this battle.
    ///     Does not reset when switching out.
    /// </summary>
    public int NumHits { get; set; }

    #endregion

    #region Ability-Related Properties

    /// <summary>
    ///     Gets or sets whether this Pokémon's Fire moves are boosted by Flash Fire.
    ///     Activated when hit by a Fire-type move.
    /// </summary>
    public bool FlashFire { get; set; }

    /// <summary>
    ///     Gets or sets the turn number relative to the Truant ability.
    ///     When % 2 == 1, the Pokémon loafs around and cannot move.
    /// </summary>
    public int TruantTurn { get; set; }

    /// <summary>
    ///     Gets or sets whether the Ice Face ability has been repaired.
    ///     Tracks whether Ice Face can activate again after being broken.
    /// </summary>
    public bool IceRepaired { get; set; }

    /// <summary>
    ///     Gets or sets the last berry eaten by this Pokémon.
    ///     Used for abilities like Cheek Pouch and items like Shell Bell.
    /// </summary>
    public Database.Models.Mongo.Pokemon.Item LastBerry { get; set; }

    /// <summary>
    ///     Gets or sets the Cud Chew effect on this Pokémon.
    ///     Allows re-eating the last berry consumed.
    /// </summary>
    public ExpiringEffect CudChew { get; set; }

    /// <summary>
    ///     Gets or sets whether Booster Energy has been consumed this battle.
    ///     Activates the effects of Protosynthesis or Quark Drive.
    /// </summary>
    public bool BoosterEnergy { get; set; }

    /// <summary>
    ///     Gets or sets whether Supersweet Syrup has been consumed.
    ///     Lowers the opponent's evasion and is not reset on switch out.
    /// </summary>
    public bool SupersweetSyrup { get; set; }

    #endregion
}