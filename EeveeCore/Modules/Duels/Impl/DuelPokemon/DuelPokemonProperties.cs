namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    #region Private Fields
    
    private readonly string _nickname;
    private readonly string? _startingName;
    private string _illusionDisplayName;
    private string? _illusionName;

    /// <summary>
    ///     The actual pokemon name.
    /// </summary>
    public string? _name;

    #endregion

    #region Core Properties
    
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