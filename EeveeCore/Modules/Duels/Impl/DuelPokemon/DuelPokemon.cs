namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

/// <summary>
///     Represents an instance of a Pokémon in a battle (duel).
///     Contains battle-specific attributes, status conditions, and temporary effects that only exist during combat.
///     Manages all state changes, move effects, and ability interactions that happen during battle.
/// </summary>
public partial class DuelPokemon
{
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
        Dictionary<string, List<int>> baseStats, int hp,
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
}