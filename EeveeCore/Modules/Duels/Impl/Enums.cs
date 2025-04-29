namespace EeveeCore.Modules.Duels.Impl;

/// <summary>
///     Helper enum for ability IDs to make them more readable in code.
///     Maps numeric ability IDs from the game data to named constants.
///     Represents the special powers and characteristics that determine how Pokémon interact with moves and battle
///     conditions.
/// </summary>
public enum Ability
{
    /// <summary>
    ///     May cause the opponent to flinch when the Pokémon with this ability is hit by a contact move.
    /// </summary>
    STENCH = 1,

    /// <summary>
    ///     Summons rain when the Pokémon enters battle.
    /// </summary>
    DRIZZLE = 2,

    /// <summary>
    ///     Increases the Pokémon's Speed stat by one stage at the end of each turn.
    /// </summary>
    SPEED_BOOST = 3,

    /// <summary>
    ///     Prevents critical hits against the Pokémon.
    /// </summary>
    BATTLE_ARMOR = 4,

    /// <summary>
    ///     Ensures the Pokémon survives one-hit KO moves with 1 HP remaining.
    ///     In later generations, also ensures survival from full HP against any attack.
    /// </summary>
    STURDY = 5,

    /// <summary>
    ///     Prevents Pokémon from using self-destructing moves like Explosion.
    /// </summary>
    DAMP = 6,

    /// <summary>
    ///     Prevents the Pokémon from being paralyzed.
    /// </summary>
    LIMBER = 7,

    /// <summary>
    ///     Increases evasion during sandstorms.
    /// </summary>
    SAND_VEIL = 8,

    /// <summary>
    ///     May paralyze the attacker when hit by a contact move.
    /// </summary>
    STATIC = 9,

    /// <summary>
    ///     Absorbs Electric-type moves, healing the Pokémon for 1/4 of its maximum HP.
    /// </summary>
    VOLT_ABSORB = 10,

    /// <summary>
    ///     Absorbs Water-type moves, healing the Pokémon for 1/4 of its maximum HP.
    /// </summary>
    WATER_ABSORB = 11,

    /// <summary>
    ///     Prevents the Pokémon from being infatuated or affected by Taunt.
    /// </summary>
    OBLIVIOUS = 12,

    /// <summary>
    ///     Negates all weather effects in battle.
    /// </summary>
    CLOUD_NINE = 13,

    /// <summary>
    ///     Increases the Pokémon's accuracy by 30%.
    /// </summary>
    COMPOUND_EYES = 14,

    /// <summary>
    ///     Prevents the Pokémon from falling asleep.
    /// </summary>
    INSOMNIA = 15,

    /// <summary>
    ///     Changes the Pokémon's type to match the type of the move it was hit with.
    /// </summary>
    COLOR_CHANGE = 16,

    /// <summary>
    ///     Prevents the Pokémon from being poisoned.
    /// </summary>
    IMMUNITY = 17,

    /// <summary>
    ///     Absorbs Fire-type moves, increasing the power of the Pokémon's own Fire-type moves.
    /// </summary>
    FLASH_FIRE = 18,

    /// <summary>
    ///     Blocks the additional effects of damaging moves.
    /// </summary>
    SHIELD_DUST = 19,

    /// <summary>
    ///     Prevents the Pokémon from being confused.
    /// </summary>
    OWN_TEMPO = 20,

    /// <summary>
    ///     Prevents the Pokémon from being forced out of battle by moves like Whirlwind.
    /// </summary>
    SUCTION_CUPS = 21,

    /// <summary>
    ///     Lowers the opponent's Attack stat by one stage upon entering battle.
    /// </summary>
    INTIMIDATE = 22,

    /// <summary>
    ///     Prevents opposing Pokémon from fleeing or switching out.
    /// </summary>
    SHADOW_TAG = 23,

    /// <summary>
    ///     Damages attacking Pokémon when they make contact, dealing 1/8 of their maximum HP.
    /// </summary>
    ROUGH_SKIN = 24,

    /// <summary>
    ///     Only super effective moves will hit the Pokémon.
    /// </summary>
    WONDER_GUARD = 25,

    /// <summary>
    ///     Makes the Pokémon immune to Ground-type moves.
    /// </summary>
    LEVITATE = 26,

    /// <summary>
    ///     Contact with the Pokémon may cause poison, paralysis, or sleep in the attacker.
    /// </summary>
    EFFECT_SPORE = 27,

    /// <summary>
    ///     Passes burns, poison, or paralysis to the Pokémon that inflicted them.
    /// </summary>
    SYNCHRONIZE = 28,

    /// <summary>
    ///     Prevents other Pokémon from lowering this Pokémon's stats.
    /// </summary>
    CLEAR_BODY = 29,

    /// <summary>
    ///     Cures the Pokémon's status conditions when it switches out.
    /// </summary>
    NATURAL_CURE = 30,

    /// <summary>
    ///     Draws in Electric-type moves, raising the Pokémon's Special Attack.
    /// </summary>
    LIGHTNING_ROD = 31,

    /// <summary>
    ///     Doubles the chance of additional effects from the Pokémon's moves.
    /// </summary>
    SERENE_GRACE = 32,

    /// <summary>
    ///     Doubles the Pokémon's Speed in rain.
    /// </summary>
    SWIFT_SWIM = 33,

    /// <summary>
    ///     Doubles the Pokémon's Speed in sunshine.
    /// </summary>
    CHLOROPHYLL = 34,

    /// <summary>
    ///     Increases the encounter rate of wild Pokémon.
    /// </summary>
    ILLUMINATE = 35,

    /// <summary>
    ///     Copies the ability of the opposing Pokémon upon entering battle.
    /// </summary>
    TRACE = 36,

    /// <summary>
    ///     Doubles the Pokémon's Attack stat.
    /// </summary>
    HUGE_POWER = 37,

    /// <summary>
    ///     May poison attackers that make contact with the Pokémon.
    /// </summary>
    POISON_POINT = 38,

    /// <summary>
    ///     Prevents the Pokémon from flinching.
    /// </summary>
    INNER_FOCUS = 39,

    /// <summary>
    ///     Prevents the Pokémon from being frozen.
    /// </summary>
    MAGMA_ARMOR = 40,

    /// <summary>
    ///     Prevents the Pokémon from being burned.
    /// </summary>
    WATER_VEIL = 41,

    /// <summary>
    ///     Prevents Steel-type Pokémon from switching out.
    /// </summary>
    MAGNET_PULL = 42,

    /// <summary>
    ///     Protects the Pokémon from sound-based moves.
    /// </summary>
    SOUNDPROOF = 43,

    /// <summary>
    ///     Gradually restores HP in rain.
    /// </summary>
    RAIN_DISH = 44,

    /// <summary>
    ///     Summons a sandstorm when the Pokémon enters battle.
    /// </summary>
    SAND_STREAM = 45,

    /// <summary>
    ///     Makes opposing Pokémon use more PP for their moves.
    /// </summary>
    PRESSURE = 46,

    /// <summary>
    ///     Halves damage from Fire and Ice-type moves.
    /// </summary>
    THICK_FAT = 47,

    /// <summary>
    ///     Reduces the amount of time the Pokémon spends asleep.
    /// </summary>
    EARLY_BIRD = 48,

    /// <summary>
    ///     May burn attackers that make contact with the Pokémon.
    /// </summary>
    FLAME_BODY = 49,

    /// <summary>
    ///     Increases the chance of successfully fleeing from wild Pokémon battles.
    /// </summary>
    RUN_AWAY = 50,

    /// <summary>
    ///     Prevents the Pokémon's accuracy from being lowered.
    /// </summary>
    KEEN_EYE = 51,

    /// <summary>
    ///     Prevents the Pokémon's Attack stat from being lowered.
    /// </summary>
    HYPER_CUTTER = 52,

    /// <summary>
    ///     May find items after battle.
    /// </summary>
    PICKUP = 53,

    /// <summary>
    ///     The Pokémon skips every other turn.
    /// </summary>
    TRUANT = 54,

    /// <summary>
    ///     Boosts Attack but lowers accuracy of physical moves.
    /// </summary>
    HUSTLE = 55,

    /// <summary>
    ///     May infatuate Pokémon of the opposite gender that make contact.
    /// </summary>
    CUTE_CHARM = 56,

    /// <summary>
    ///     Increases Special Attack when another Pokémon has Minus.
    /// </summary>
    PLUS = 57,

    /// <summary>
    ///     Increases Special Attack when another Pokémon has Plus.
    /// </summary>
    MINUS = 58,

    /// <summary>
    ///     Changes Castform's type and appearance based on the weather.
    /// </summary>
    FORECAST = 59,

    /// <summary>
    ///     Prevents the Pokémon's held item from being removed.
    /// </summary>
    STICKY_HOLD = 60,

    /// <summary>
    ///     Has a chance of curing the Pokémon's status condition after each turn.
    /// </summary>
    SHED_SKIN = 61,

    /// <summary>
    ///     Increases Attack when the Pokémon has a status condition.
    /// </summary>
    GUTS = 62,

    /// <summary>
    ///     Increases Defense when the Pokémon has a status condition.
    /// </summary>
    MARVEL_SCALE = 63,

    /// <summary>
    ///     Damages Pokémon that try to drain HP from this Pokémon.
    /// </summary>
    LIQUID_OOZE = 64,

    /// <summary>
    ///     Powers up Grass-type moves when the Pokémon's HP is low.
    /// </summary>
    OVERGROW = 65,

    /// <summary>
    ///     Powers up Fire-type moves when the Pokémon's HP is low.
    /// </summary>
    BLAZE = 66,

    /// <summary>
    ///     Powers up Water-type moves when the Pokémon's HP is low.
    /// </summary>
    TORRENT = 67,

    /// <summary>
    ///     Powers up Bug-type moves when the Pokémon's HP is low.
    /// </summary>
    SWARM = 68,

    /// <summary>
    ///     Protects the Pokémon from recoil damage.
    /// </summary>
    ROCK_HEAD = 69,

    /// <summary>
    ///     Summons strong sunlight when the Pokémon enters battle.
    /// </summary>
    DROUGHT = 70,

    /// <summary>
    ///     Prevents opposing Pokémon from fleeing.
    /// </summary>
    ARENA_TRAP = 71,

    /// <summary>
    ///     Prevents the Pokémon from falling asleep.
    /// </summary>
    VITAL_SPIRIT = 72,

    /// <summary>
    ///     Prevents other Pokémon from lowering this Pokémon's stats.
    /// </summary>
    WHITE_SMOKE = 73,

    /// <summary>
    ///     Doubles the Pokémon's Attack stat.
    /// </summary>
    PURE_POWER = 74,

    /// <summary>
    ///     Protects the Pokémon from critical hits.
    /// </summary>
    SHELL_ARMOR = 75,

    /// <summary>
    ///     Negates all weather effects in battle.
    /// </summary>
    AIR_LOCK = 76,

    /// <summary>
    ///     Increases evasion when the Pokémon is confused.
    /// </summary>
    TANGLED_FEET = 77,

    /// <summary>
    ///     Raises Speed when hit by an Electric-type move.
    /// </summary>
    MOTOR_DRIVE = 78,

    /// <summary>
    ///     Increases damage against Pokémon of the same gender, decreases damage against Pokémon of the opposite gender.
    /// </summary>
    RIVALRY = 79,

    /// <summary>
    ///     Raises Speed when the Pokémon flinches.
    /// </summary>
    STEADFAST = 80,

    /// <summary>
    ///     Increases evasion during hailstorms.
    /// </summary>
    SNOW_CLOAK = 81,

    /// <summary>
    ///     Encourages early use of held berries.
    /// </summary>
    GLUTTONY = 82,

    /// <summary>
    ///     Maximizes Attack after taking a critical hit.
    /// </summary>
    ANGER_POINT = 83,

    /// <summary>
    ///     Increases Speed when the Pokémon's held item is used or lost.
    /// </summary>
    UNBURDEN = 84,

    /// <summary>
    ///     Reduces damage from Fire-type moves.
    /// </summary>
    HEATPROOF = 85,

    /// <summary>
    ///     Doubles the effect of stat changes.
    /// </summary>
    SIMPLE = 86,

    /// <summary>
    ///     Reduces HP in sunshine and increases it in rain. Vulnerable to Fire-type moves.
    /// </summary>
    DRY_SKIN = 87,

    /// <summary>
    ///     Adjusts power based on the opponent's higher defensive stat.
    /// </summary>
    DOWNLOAD = 88,

    /// <summary>
    ///     Increases the power of punching moves.
    /// </summary>
    IRON_FIST = 89,

    /// <summary>
    ///     Restores HP when poisoned instead of taking damage.
    /// </summary>
    POISON_HEAL = 90,

    /// <summary>
    ///     Increases the power of moves that match the Pokémon's type.
    /// </summary>
    ADAPTABILITY = 91,

    /// <summary>
    ///     Ensures multi-hit moves hit the maximum number of times.
    /// </summary>
    SKILL_LINK = 92,

    /// <summary>
    ///     Heals status conditions in rain.
    /// </summary>
    HYDRATION = 93,

    /// <summary>
    ///     Increases Special Attack in sunshine but damages the Pokémon each turn.
    /// </summary>
    SOLAR_POWER = 94,

    /// <summary>
    ///     Increases Speed when the Pokémon has a status condition.
    /// </summary>
    QUICK_FEET = 95,

    /// <summary>
    ///     Changes all of the Pokémon's moves to Normal type.
    /// </summary>
    NORMALIZE = 96,

    /// <summary>
    ///     Increases damage dealt by critical hits.
    /// </summary>
    SNIPER = 97,

    /// <summary>
    ///     Protects the Pokémon from indirect damage.
    /// </summary>
    MAGIC_GUARD = 98,

    /// <summary>
    ///     Ensures all moves used by or against the Pokémon will hit.
    /// </summary>
    NO_GUARD = 99,

    /// <summary>
    ///     The Pokémon always moves last within its priority bracket.
    /// </summary>
    STALL = 100,

    /// <summary>
    ///     Increases the power of weak moves.
    /// </summary>
    TECHNICIAN = 101,

    /// <summary>
    ///     Prevents status conditions in sunshine.
    /// </summary>
    LEAF_GUARD = 102,

    /// <summary>
    ///     Prevents the Pokémon from using its held item.
    /// </summary>
    KLUTZ = 103,

    /// <summary>
    ///     Ignores the effects of abilities that hinder attacking moves.
    /// </summary>
    MOLD_BREAKER = 104,

    /// <summary>
    ///     Increases the critical hit ratio.
    /// </summary>
    SUPER_LUCK = 105,

    /// <summary>
    ///     Damages the attacker when the Pokémon is knocked out.
    /// </summary>
    AFTERMATH = 106,

    /// <summary>
    ///     Alerts the Pokémon when the opponent has a super effective move.
    /// </summary>
    ANTICIPATION = 107,

    /// <summary>
    ///     Reveals the opponent's move with the highest power upon entering battle.
    /// </summary>
    FOREWARN = 108,

    /// <summary>
    ///     Ignores the opponent's stat changes when taking or dealing damage.
    /// </summary>
    UNAWARE = 109,

    /// <summary>
    ///     Increases the power of not very effective moves.
    /// </summary>
    TINTED_LENS = 110,

    /// <summary>
    ///     Reduces damage from super effective attacks.
    /// </summary>
    FILTER = 111,

    /// <summary>
    ///     Halves the Pokémon's Attack and Speed for five turns after entering battle.
    /// </summary>
    SLOW_START = 112,

    /// <summary>
    ///     Enables the Pokémon to hit Ghost-type Pokémon with Normal and Fighting-type moves.
    /// </summary>
    SCRAPPY = 113,

    /// <summary>
    ///     Draws in Water-type moves, raising the Pokémon's Special Attack.
    /// </summary>
    STORM_DRAIN = 114,

    /// <summary>
    ///     Restores HP in hailstorms.
    /// </summary>
    ICE_BODY = 115,

    /// <summary>
    ///     Reduces damage from super effective attacks.
    /// </summary>
    SOLID_ROCK = 116,

    /// <summary>
    ///     Summons a hailstorm when the Pokémon enters battle.
    /// </summary>
    SNOW_WARNING = 117,

    /// <summary>
    ///     May find honey after battle.
    /// </summary>
    HONEY_GATHER = 118,

    /// <summary>
    ///     Reveals the opponent's held item upon entering battle.
    /// </summary>
    FRISK = 119,

    /// <summary>
    ///     Increases the power of moves with recoil damage.
    /// </summary>
    RECKLESS = 120,

    /// <summary>
    ///     Changes Arceus's type based on its held plate.
    /// </summary>
    MULTITYPE = 121,

    /// <summary>
    ///     Powers up allies' moves in sunshine.
    /// </summary>
    FLOWER_GIFT = 122,

    /// <summary>
    ///     Damages sleeping opponents each turn.
    /// </summary>
    BAD_DREAMS = 123,

    /// <summary>
    ///     Steals the attacker's held item when hit by a contact move.
    /// </summary>
    PICKPOCKET = 124,

    /// <summary>
    ///     Increases move power but removes additional effects.
    /// </summary>
    SHEER_FORCE = 125,

    /// <summary>
    ///     Inverts the effect of stat changes.
    /// </summary>
    CONTRARY = 126,

    /// <summary>
    ///     Prevents opponents from eating berries.
    /// </summary>
    UNNERVE = 127,

    /// <summary>
    ///     Raises Attack by two stages when a stat is lowered.
    /// </summary>
    DEFIANT = 128,

    /// <summary>
    ///     Halves the Pokémon's Attack and Special Attack when its HP drops below half.
    /// </summary>
    DEFEATIST = 129,

    /// <summary>
    ///     May disable a move used on the Pokémon.
    /// </summary>
    CURSED_BODY = 130,

    /// <summary>
    ///     May heal allies' status conditions.
    /// </summary>
    HEALER = 131,

    /// <summary>
    ///     Reduces damage taken by allies.
    /// </summary>
    FRIEND_GUARD = 132,

    /// <summary>
    ///     Lowers Defense but raises Speed when hit by a physical move.
    /// </summary>
    WEAK_ARMOR = 133,

    /// <summary>
    ///     Doubles the Pokémon's weight.
    /// </summary>
    HEAVY_METAL = 134,

    /// <summary>
    ///     Halves the Pokémon's weight.
    /// </summary>
    LIGHT_METAL = 135,

    /// <summary>
    ///     Reduces damage taken when at full HP.
    /// </summary>
    MULTISCALE = 136,

    /// <summary>
    ///     Increases Attack when poisoned.
    /// </summary>
    TOXIC_BOOST = 137,

    /// <summary>
    ///     Increases Special Attack when burned.
    /// </summary>
    FLARE_BOOST = 138,

    /// <summary>
    ///     May restore a used berry after each turn.
    /// </summary>
    HARVEST = 139,

    /// <summary>
    ///     Protects the Pokémon from allies' damaging moves.
    /// </summary>
    TELEPATHY = 140,

    /// <summary>
    ///     Raises one random stat and lowers another after each turn.
    /// </summary>
    MOODY = 141,

    /// <summary>
    ///     Protects the Pokémon from damage from weather and powder moves.
    /// </summary>
    OVERCOAT = 142,

    /// <summary>
    ///     May poison the opponent when the Pokémon makes contact.
    /// </summary>
    POISON_TOUCH = 143,

    /// <summary>
    ///     Restores a portion of HP when switched out.
    /// </summary>
    REGENERATOR = 144,

    /// <summary>
    ///     Prevents the Pokémon's Defense from being lowered.
    /// </summary>
    BIG_PECKS = 145,

    /// <summary>
    ///     Doubles Speed during a sandstorm.
    /// </summary>
    SAND_RUSH = 146,

    /// <summary>
    ///     Lowers the accuracy of status moves used against the Pokémon.
    /// </summary>
    WONDER_SKIN = 147,

    /// <summary>
    ///     Increases move power when the Pokémon moves last.
    /// </summary>
    ANALYTIC = 148,

    /// <summary>
    ///     Enters battle disguised as the last Pokémon in the party.
    /// </summary>
    ILLUSION = 149,

    /// <summary>
    ///     Transforms into the opposing Pokémon upon entering battle.
    /// </summary>
    IMPOSTER = 150,

    /// <summary>
    ///     Bypasses Light Screen, Reflect, and Substitute.
    /// </summary>
    INFILTRATOR = 151,

    /// <summary>
    ///     Changes the opponent's ability to Mummy when hit by a contact move.
    /// </summary>
    MUMMY = 152,

    /// <summary>
    ///     Raises Attack by one stage after knocking out a Pokémon.
    /// </summary>
    MOXIE = 153,

    /// <summary>
    ///     Raises Attack by one stage when hit by a Dark-type move.
    /// </summary>
    JUSTIFIED = 154,

    /// <summary>
    ///     Raises Speed by one stage when hit by a Dark, Ghost, or Bug-type move.
    /// </summary>
    RATTLED = 155,

    /// <summary>
    ///     Reflects status moves back at the user.
    /// </summary>
    MAGIC_BOUNCE = 156,

    /// <summary>
    ///     Absorbs Grass-type moves, raising Attack by one stage.
    /// </summary>
    SAP_SIPPER = 157,

    /// <summary>
    ///     Gives status moves priority.
    /// </summary>
    PRANKSTER = 158,

    /// <summary>
    ///     Increases the power of Rock, Ground, and Steel-type moves during a sandstorm.
    /// </summary>
    SAND_FORCE = 159,

    /// <summary>
    ///     Damages attackers when they make contact.
    /// </summary>
    IRON_BARBS = 160,

    /// <summary>
    ///     Changes Darmanitan's form when its HP drops below half.
    /// </summary>
    ZEN_MODE = 161,

    /// <summary>
    ///     Increases the accuracy of allies.
    /// </summary>
    VICTORY_STAR = 162,

    /// <summary>
    ///     Ignores the effects of abilities that hinder attacking moves.
    /// </summary>
    TURBOBLAZE = 163,

    /// <summary>
    ///     Ignores the effects of abilities that hinder attacking moves.
    /// </summary>
    TERAVOLT = 164,

    /// <summary>
    ///     Protects allies from moves that affect their mental state.
    /// </summary>
    AROMA_VEIL = 165,

    /// <summary>
    ///     Protects Grass-type allies from status conditions and stat reductions.
    /// </summary>
    FLOWER_VEIL = 166,

    /// <summary>
    ///     Restores HP when the Pokémon eats a berry.
    /// </summary>
    CHEEK_POUCH = 167,

    /// <summary>
    ///     Changes the Pokémon's type to match the type of the move it uses.
    /// </summary>
    PROTEAN = 168,

    /// <summary>
    ///     Halves damage from physical moves.
    /// </summary>
    FUR_COAT = 169,

    /// <summary>
    ///     Steals the target's held item when the Pokémon uses a damaging move.
    /// </summary>
    MAGICIAN = 170,

    /// <summary>
    ///     Protects the Pokémon from bullet, ball, and bomb-based moves.
    /// </summary>
    BULLETPROOF = 171,

    /// <summary>
    ///     Raises Special Attack by two stages when a stat is lowered.
    /// </summary>
    COMPETITIVE = 172,

    /// <summary>
    ///     Increases the power of biting moves.
    /// </summary>
    STRONG_JAW = 173,

    /// <summary>
    ///     Turns Normal-type moves into Ice-type moves with a 30% power boost.
    /// </summary>
    REFRIGERATE = 174,

    /// <summary>
    ///     Prevents allies from falling asleep.
    /// </summary>
    SWEET_VEIL = 175,

    /// <summary>
    ///     Changes Aegislash between Blade and Shield forms based on the moves it uses.
    /// </summary>
    STANCE_CHANGE = 176,

    /// <summary>
    ///     Gives Flying-type moves priority when at full HP.
    /// </summary>
    GALE_WINGS = 177,

    /// <summary>
    ///     Increases the power of pulse and aura moves.
    /// </summary>
    MEGA_LAUNCHER = 178,

    /// <summary>
    ///     Increases Defense on Grassy Terrain.
    /// </summary>
    GRASS_PELT = 179,

    /// <summary>
    ///     Passes the Pokémon's held item to an ally when its HP drops below half.
    /// </summary>
    SYMBIOSIS = 180,

    /// <summary>
    ///     Increases the power of contact moves.
    /// </summary>
    TOUGH_CLAWS = 181,

    /// <summary>
    ///     Turns Normal-type moves into Fairy-type moves with a 30% power boost.
    /// </summary>
    PIXILATE = 182,

    /// <summary>
    ///     Lowers the Speed of attackers that make contact.
    /// </summary>
    GOOEY = 183,

    /// <summary>
    ///     Turns Normal-type moves into Flying-type moves with a 30% power boost.
    /// </summary>
    AERILATE = 184,

    /// <summary>
    ///     Causes damaging moves to hit twice, with the second hit dealing 25% damage.
    /// </summary>
    PARENTAL_BOND = 185,

    /// <summary>
    ///     Increases the power of Dark-type moves for all Pokémon in battle.
    /// </summary>
    DARK_AURA = 186,

    /// <summary>
    ///     Increases the power of Fairy-type moves for all Pokémon in battle.
    /// </summary>
    FAIRY_AURA = 187,

    /// <summary>
    ///     Reverses the effects of Dark Aura and Fairy Aura.
    /// </summary>
    AURA_BREAK = 188,

    /// <summary>
    ///     Creates heavy rain that prevents Fire-type moves from working.
    /// </summary>
    PRIMORDIAL_SEA = 189,

    /// <summary>
    ///     Creates extremely harsh sunlight that prevents Water-type moves from working.
    /// </summary>
    DESOLATE_LAND = 190,

    /// <summary>
    ///     Creates strong winds that remove Flying-type Pokémon's weaknesses.
    /// </summary>
    DELTA_STREAM = 191,

    /// <summary>
    ///     Raises Defense by one stage when hit by an attack.
    /// </summary>
    STAMINA = 192,

    /// <summary>
    ///     Switches the Pokémon out when its HP drops below half.
    /// </summary>
    WIMP_OUT = 193,

    /// <summary>
    ///     Switches the Pokémon out when its HP drops below half.
    /// </summary>
    EMERGENCY_EXIT = 194,

    /// <summary>
    ///     Raises Defense by two stages when hit by a Water-type move.
    /// </summary>
    WATER_COMPACTION = 195,

    /// <summary>
    ///     Guarantees critical hits against poisoned targets.
    /// </summary>
    MERCILESS = 196,

    /// <summary>
    ///     Changes Minior's form when its HP drops below half.
    /// </summary>
    SHIELDS_DOWN = 197,

    /// <summary>
    ///     Doubles damage against Pokémon that switched in during this turn.
    /// </summary>
    STAKEOUT = 198,

    /// <summary>
    ///     Halves damage from Fire-type moves and prevents burns.
    /// </summary>
    WATER_BUBBLE = 199,

    /// <summary>
    ///     Increases the power of Steel-type moves.
    /// </summary>
    STEELWORKER = 200,

    /// <summary>
    ///     Raises Special Attack by one stage when HP drops below half.
    /// </summary>
    BERSERK = 201,

    /// <summary>
    ///     Doubles Speed during hailstorms.
    /// </summary>
    SLUSH_RUSH = 202,

    /// <summary>
    ///     Prevents making contact with the target when attacking.
    /// </summary>
    LONG_REACH = 203,

    /// <summary>
    ///     Sound-based moves become Water-type.
    /// </summary>
    LIQUID_VOICE = 204,

    /// <summary>
    ///     Gives healing moves priority.
    /// </summary>
    TRIAGE = 205,

    /// <summary>
    ///     Turns Normal-type moves into Electric-type moves with a 30% power boost.
    /// </summary>
    GALVANIZE = 206,

    /// <summary>
    ///     Doubles Speed on Electric Terrain.
    /// </summary>
    SURGE_SURFER = 207,

    /// <summary>
    ///     Changes Wishiwashi's form when its HP drops below 25%.
    /// </summary>
    SCHOOLING = 208,

    /// <summary>
    ///     Prevents damage for the first hit, then reduces Mimikyu to Disguised form.
    /// </summary>
    DISGUISE = 209,

    /// <summary>
    ///     Changes Greninja's form when it defeats a Pokémon, increasing its power.
    /// </summary>
    BATTLE_BOND = 210,

    /// <summary>
    ///     Changes Zygarde's form at 50% HP or less.
    /// </summary>
    POWER_CONSTRUCT = 211,

    /// <summary>
    ///     Allows the Pokémon to poison Steel and Poison-type Pokémon.
    /// </summary>
    CORROSION = 212,

    /// <summary>
    ///     The Pokémon is permanently asleep but can still attack.
    /// </summary>
    COMATOSE = 213,

    /// <summary>
    ///     Prevents the opponent from using priority moves.
    /// </summary>
    QUEENLY_MAJESTY = 214,

    /// <summary>
    ///     Damages the attacker when the Pokémon is knocked out.
    /// </summary>
    INNARDS_OUT = 215,

    /// <summary>
    ///     Copies dance moves used by other Pokémon.
    /// </summary>
    DANCER = 216,

    /// <summary>
    ///     Increases the power of allies' special moves.
    /// </summary>
    BATTERY = 217,

    /// <summary>
    ///     Halves damage from contact moves but doubles damage from Fire-type moves.
    /// </summary>
    FLUFFY = 218,

    /// <summary>
    ///     Prevents the opponent from using priority moves.
    /// </summary>
    DAZZLING = 219,

    /// <summary>
    ///     Raises Special Attack by one stage when a Pokémon faints.
    /// </summary>
    SOUL_HEART = 220,

    /// <summary>
    ///     Lowers the Speed of attackers that make contact.
    /// </summary>
    TANGLING_HAIR = 221,

    /// <summary>
    ///     Inherits the ability of a defeated ally.
    /// </summary>
    RECEIVER = 222,

    /// <summary>
    ///     Inherits the ability of a defeated ally.
    /// </summary>
    POWER_OF_ALCHEMY = 223,

    /// <summary>
    ///     Raises the highest stat by one stage after knocking out a Pokémon.
    /// </summary>
    BEAST_BOOST = 224,

    /// <summary>
    ///     Changes Silvally's type based on its held memory.
    /// </summary>
    RKS_SYSTEM = 225,

    /// <summary>
    ///     Creates Electric Terrain when the Pokémon enters battle.
    /// </summary>
    ELECTRIC_SURGE = 226,

    /// <summary>
    ///     Creates Psychic Terrain when the Pokémon enters battle.
    /// </summary>
    PSYCHIC_SURGE = 227,

    /// <summary>
    ///     Creates Misty Terrain when the Pokémon enters battle.
    /// </summary>
    MISTY_SURGE = 228,

    /// <summary>
    ///     Creates Grassy Terrain when the Pokémon enters battle.
    /// </summary>
    GRASSY_SURGE = 229,

    /// <summary>
    ///     Prevents other Pokémon from lowering this Pokémon's stats.
    /// </summary>
    FULL_METAL_BODY = 230,

    /// <summary>
    ///     Reduces damage from full-HP Pokémon.
    /// </summary>
    SHADOW_SHIELD = 231,

    /// <summary>
    ///     Reduces damage from super effective attacks.
    /// </summary>
    PRISM_ARMOR = 232,

    /// <summary>
    ///     Increases damage dealt with super effective moves.
    /// </summary>
    NEUROFORCE = 233,

    /// <summary>
    ///     Changes the Pokémon's type to match the type of the move it uses.
    /// </summary>
    LIBERO = 234,

    /// <summary>
    ///     Reflects stat-lowering effects back to the attacker.
    /// </summary>
    MIRROR_ARMOR = 235,

    /// <summary>
    ///     Lowers the Speed of the opponent when hit.
    /// </summary>
    COTTON_DOWN = 236,

    /// <summary>
    ///     Retrieves the first thrown Poké Ball if it fails.
    /// </summary>
    BALL_FETCH = 237,

    /// <summary>
    ///     Raises Speed by six stages when hit by a Fire or Water-type move.
    /// </summary>
    STEAM_ENGINE = 238,

    /// <summary>
    ///     Doubles the effect of berries.
    /// </summary>
    RIPEN = 239,

    /// <summary>
    ///     Summons a sandstorm when hit by an attack.
    /// </summary>
    SAND_SPIT = 240,

    /// <summary>
    ///     Changes form and attacks opponents with aquatic prey when using Surf or Dive.
    /// </summary>
    GULP_MISSILE = 241,

    /// <summary>
    ///     Ignores the effects of opponent's abilities when attacking.
    /// </summary>
    PROPELLER_TAIL = 242,

    /// <summary>
    ///     Reduces the power of sound-based moves and prevents damage from them.
    /// </summary>
    PUNK_ROCK = 243,

    /// <summary>
    ///     Increases the power of Steel-type moves for allies.
    /// </summary>
    STEELY_SPIRIT = 244,

    /// <summary>
    ///     Causes both Pokémon to faint when the opponent makes contact.
    /// </summary>
    PERISH_BODY = 245,

    /// <summary>
    ///     Removes all active screens from the field.
    /// </summary>
    SCREEN_CLEANER = 246,

    /// <summary>
    ///     Swaps abilities with Pokémon that make contact.
    /// </summary>
    WANDERING_SPIRIT = 247,

    /// <summary>
    ///     Neutralizes the effects of all other Pokémon's abilities.
    /// </summary>
    NEUTRALIZING_GAS = 248,

    /// <summary>
    ///     Halves damage from special moves.
    /// </summary>
    ICE_SCALES = 249,

    /// <summary>
    ///     Increases the power of allies' moves.
    /// </summary>
    POWER_SPOT = 250,

    /// <summary>
    ///     Protects from one physical attack, then changes form.
    /// </summary>
    ICE_FACE = 251,

    /// <summary>
    ///     Changes Morpeko's form after each turn, altering its signature move.
    /// </summary>
    HUNGER_SWITCH = 252,

    /// <summary>
    ///     Ignores redirection effects from opponent abilities.
    /// </summary>
    STALWART = 253,

    /// <summary>
    ///     Raises Attack by one stage upon entering battle.
    /// </summary>
    INTREPID_SWORD = 254,

    /// <summary>
    ///     Raises Defense by one stage upon entering battle.
    /// </summary>
    DAUNTLESS_SHIELD = 255,

    /// <summary>
    ///     Prevents the Pokémon and its allies from being poisoned.
    /// </summary>
    PASTEL_VEIL = 256,

    /// <summary>
    ///     Increases Attack but restricts the Pokémon to only the first move it uses.
    /// </summary>
    GORILLA_TACTICS = 257,

    /// <summary>
    ///     Gives the Pokémon a chance to move first with damaging moves.
    /// </summary>
    QUICK_DRAW = 258,

    /// <summary>
    ///     Changes the Pokémon's type to match the current terrain.
    /// </summary>
    MIMICRY = 259,

    /// <summary>
    ///     Increases the power of Electric-type moves.
    /// </summary>
    TRANSISTOR = 260,

    /// <summary>
    ///     Increases the power of Dragon-type moves.
    /// </summary>
    DRAGONS_MAW = 261,

    /// <summary>
    ///     Raises Attack by one stage after knocking out a Pokémon.
    /// </summary>
    CHILLING_NEIGH = 262,

    /// <summary>
    ///     Raises Special Attack by one stage after knocking out a Pokémon.
    /// </summary>
    GRIM_NEIGH = 263,

    /// <summary>
    ///     Combines Unnerve and Chilling Neigh abilities.
    /// </summary>
    AS_ONE_SHADOW = 264,

    /// <summary>
    ///     Combines Unnerve and Grim Neigh abilities.
    /// </summary>
    AS_ONE_ICE = 265,

    /// <summary>
    ///     Resets all allies' stat changes when the Pokémon enters battle.
    /// </summary>
    CURIOUS_MEDICINE = 266,

    /// <summary>
    ///     Bypasses the effects of protecting moves.
    /// </summary>
    UNSEEN_FIST = 267,

    /// <summary>
    ///     Changes the ability of Pokémon making contact to this ability.
    /// </summary>
    LINGERING_AROMA = 268,

    /// <summary>
    ///     Turns the terrain to Grassy Terrain when hit by an attack.
    /// </summary>
    SEED_SOWER = 269,

    /// <summary>
    ///     Raises Attack when hit by a Fire-type move and prevents burns.
    /// </summary>
    THERMAL_EXCHANGE = 270,

    /// <summary>
    ///     Increases Attack and Special Attack but decreases defenses when HP drops below half.
    /// </summary>
    ANGER_SHELL = 271,

    /// <summary>
    ///     Protects against status conditions and halves damage from Ghost-type moves.
    /// </summary>
    PURIFYING_SALT = 272,

    /// <summary>
    ///     Protects against Fire-type moves and doubles Defense when hit by one.
    /// </summary>
    WELL_BAKED_BODY = 273,

    /// <summary>
    ///     Raises Attack when hit by a Wind move and grants immunity to them.
    /// </summary>
    WIND_RIDER = 274,

    /// <summary>
    ///     Raises Attack when intimidated and prevents being forced to switch out.
    /// </summary>
    GUARD_DOG = 275,

    /// <summary>
    ///     Increases the power of Rock-type moves.
    /// </summary>
    ROCKY_PAYLOAD = 276,

    /// <summary>
    ///     Charges the Pokémon when hit by a Wind move.
    /// </summary>
    WIND_POWER = 277,

    /// <summary>
    ///     Transforms Palafin when switched out after reaching Lv. 60+.
    /// </summary>
    ZERO_TO_HERO = 278,

    /// <summary>
    ///     Powers up allies and directs all single-target attacks toward this Pokémon.
    /// </summary>
    COMMANDER = 279,

    /// <summary>
    ///     Charges the Pokémon when hit by an attack.
    /// </summary>
    ELECTROMORPHOSIS = 280,

    /// <summary>
    ///     Boosts the highest stat in strong sunlight.
    /// </summary>
    PROTOSYNTHESIS = 281,

    /// <summary>
    ///     Boosts the highest stat on Electric Terrain.
    /// </summary>
    QUARK_DRIVE = 282,

    /// <summary>
    ///     Grants immunity to status moves.
    /// </summary>
    GOOD_AS_GOLD = 283,

    /// <summary>
    ///     Reduces opponents' Special Attack stats.
    /// </summary>
    VESSEL_OF_RUIN = 284,

    /// <summary>
    ///     Reduces opponents' Attack stats.
    /// </summary>
    SWORD_OF_RUIN = 285,

    /// <summary>
    ///     Reduces opponents' Defense stats.
    /// </summary>
    TABLETS_OF_RUIN = 286,

    /// <summary>
    ///     Reduces opponents' Special Defense stats.
    /// </summary>
    BEADS_OF_RUIN = 287,

    /// <summary>
    ///     Powers up Electric-type moves in strong sunlight.
    /// </summary>
    ORICHALCUM_PULSE = 288,

    /// <summary>
    ///     Powers up Electric-type moves on Electric Terrain.
    /// </summary>
    HADRON_ENGINE = 289,

    /// <summary>
    ///     Steals stat boosts from other Pokémon.
    /// </summary>
    OPPORTUNIST = 290,

    /// <summary>
    ///     Re-eats the used berry after a few turns.
    /// </summary>
    CUD_CHEW = 291,

    /// <summary>
    ///     Increases the power of slicing moves.
    /// </summary>
    SHARPNESS = 292,

    /// <summary>
    ///     Increases power as allies faint.
    /// </summary>
    SUPREME_OVERLORD = 293,

    /// <summary>
    ///     Copies the stat changes of the Pokémon it replaces.
    /// </summary>
    COSTAR = 294,

    /// <summary>
    ///     Scatters poison spikes when hit by a physical attack.
    /// </summary>
    TOXIC_DEBRIS = 295,

    /// <summary>
    ///     Prevents opponents from using priority moves.
    /// </summary>
    ARMOR_TAIL = 296,

    /// <summary>
    ///     Absorbs Ground-type moves, healing the Pokémon for 1/4 of its maximum HP.
    /// </summary>
    EARTH_EATER = 297,

    /// <summary>
    ///     Status moves go last but ignore protection and substitute.
    /// </summary>
    MYCELIUM_MIGHT = 298,

    /// <summary>
    ///     Heals allies that switch in.
    /// </summary>
    HOSPITALITY = 299,

    /// <summary>
    ///     Ignores target's evasion and ability when attacking.
    /// </summary>
    MINDS_EYE = 300,

    /// <summary>
    ///     Boosts special moves and changes them to Fairy type.
    /// </summary>
    EMBODY_ASPECT_TEAL = 301,

    /// <summary>
    ///     Boosts special moves and changes them to Fire type.
    /// </summary>
    EMBODY_ASPECT_HEARTHFLAME = 302,

    /// <summary>
    ///     Boosts special moves and changes them to Water type.
    /// </summary>
    EMBODY_ASPECT_WELLSPRING = 303,

    /// <summary>
    ///     Boosts special moves and changes them to Rock type.
    /// </summary>
    EMBODY_ASPECT_CORNERSTONE = 304,

    /// <summary>
    ///     Spreads poison to opponents on either side when one is poisoned.
    /// </summary>
    TOXIC_CHAIN = 305,

    /// <summary>
    ///     Lowers the evasion of the opponent when the Pokémon enters battle.
    /// </summary>
    SUPERSWEET_SYRUP = 306,

    /// <summary>
    ///     Changes the Pokémon's Tera Type when Terastallized.
    /// </summary>
    TERA_SHIFT = 307,

    /// <summary>
    ///     Reduces damage when untransformed; becomes pure Tera Type when Terastallized.
    /// </summary>
    TERA_SHELL = 308,

    /// <summary>
    ///     Technical overwrite feature for Terapagos. Changes form when Terastallized.
    /// </summary>
    TERAFORM_ZERO = 309,

    /// <summary>
    ///     Forces opponents to use status moves when poisoned.
    /// </summary>
    POISON_PUPPETEER = 310
}

/// <summary>
///     Helper enum for damage classes to make them more readable in code.
///     Defines the three categories of moves in the battle system.
/// </summary>
public enum DamageClass
{
    /// <summary>
    ///     Moves that don't directly deal damage but instead cause status effects or stat changes.
    ///     Examples include Thunder Wave, Swords Dance, and Toxic.
    /// </summary>
    STATUS = 1,

    /// <summary>
    ///     Damaging moves that use the Attack and Defense stats for damage calculation.
    ///     Examples include Tackle, Earthquake, and Close Combat.
    /// </summary>
    PHYSICAL = 2,

    /// <summary>
    ///     Damaging moves that use the Special Attack and Special Defense stats for damage calculation.
    ///     Examples include Flamethrower, Thunderbolt, and Psychic.
    /// </summary>
    SPECIAL = 3
}

/// <summary>
///     Helper enum for element types to make them more readable in code.
///     Defines the types that Pokémon and moves can have, determining type effectiveness relationships.
/// </summary>
public enum ElementType
{
    /// <summary>
    ///     Normal type. Weak to Fighting. Immune to Ghost.
    /// </summary>
    NORMAL = 1,

    /// <summary>
    ///     Fighting type. Strong against Normal, Rock, Steel, Ice, Dark.
    ///     Weak to Flying, Psychic, Fairy. Resisted by Bug, Ghost, Poison.
    /// </summary>
    FIGHTING = 2,

    /// <summary>
    ///     Flying type. Strong against Fighting, Bug, Grass.
    ///     Weak to Rock, Electric, Ice. Immune to Ground.
    /// </summary>
    FLYING = 3,

    /// <summary>
    ///     Poison type. Strong against Grass, Fairy.
    ///     Weak to Ground, Psychic. Resisted by Poison, Ground, Rock, Ghost.
    /// </summary>
    POISON = 4,

    /// <summary>
    ///     Ground type. Strong against Poison, Rock, Steel, Fire, Electric.
    ///     Weak to Water, Grass, Ice. Immune to Electric.
    /// </summary>
    GROUND = 5,

    /// <summary>
    ///     Rock type. Strong against Flying, Bug, Fire, Ice.
    ///     Weak to Fighting, Ground, Steel, Water, Grass.
    /// </summary>
    ROCK = 6,

    /// <summary>
    ///     Bug type. Strong against Grass, Psychic, Dark.
    ///     Weak to Flying, Rock, Fire. Resisted by Fighting, Poison, Ghost, Steel, Fairy.
    /// </summary>
    BUG = 7,

    /// <summary>
    ///     Ghost type. Strong against Ghost, Psychic.
    ///     Weak to Ghost, Dark. Immune to Normal, Fighting. Resisted by Dark.
    /// </summary>
    GHOST = 8,

    /// <summary>
    ///     Steel type. Strong against Rock, Ice, Fairy.
    ///     Weak to Fighting, Ground, Fire. Resists many types and immune to Poison.
    /// </summary>
    STEEL = 9,

    /// <summary>
    ///     Fire type. Strong against Bug, Steel, Grass, Ice.
    ///     Weak to Ground, Rock, Water. Resisted by Fire, Water, Rock, Dragon.
    /// </summary>
    FIRE = 10,

    /// <summary>
    ///     Water type. Strong against Ground, Rock, Fire.
    ///     Weak to Grass, Electric. Resisted by Water, Grass, Dragon.
    /// </summary>
    WATER = 11,

    /// <summary>
    ///     Grass type. Strong against Ground, Rock, Water.
    ///     Weak to Flying, Poison, Bug, Fire, Ice. Resisted by Flying, Poison, Bug, Steel, Fire, Grass, Dragon.
    /// </summary>
    GRASS = 12,

    /// <summary>
    ///     Electric type. Strong against Flying, Water.
    ///     Weak to Ground. Resisted by Grass, Electric, Dragon. No effect on Ground.
    /// </summary>
    ELECTRIC = 13,

    /// <summary>
    ///     Psychic type. Strong against Fighting, Poison.
    ///     Weak to Bug, Ghost, Dark. Resisted by Steel, Psychic. No effect on Dark.
    /// </summary>
    PSYCHIC = 14,

    /// <summary>
    ///     Ice type. Strong against Flying, Ground, Grass, Dragon.
    ///     Weak to Fighting, Rock, Steel, Fire. Resisted by Steel, Fire, Water, Ice.
    /// </summary>
    ICE = 15,

    /// <summary>
    ///     Dragon type. Strong against Dragon.
    ///     Weak to Ice, Dragon, Fairy. Resisted by Steel. No effect on Fairy.
    /// </summary>
    DRAGON = 16,

    /// <summary>
    ///     Dark type. Strong against Ghost, Psychic.
    ///     Weak to Fighting, Bug, Fairy. Resisted by Fighting, Dark, Fairy. Immune to Psychic.
    /// </summary>
    DARK = 17,

    /// <summary>
    ///     Fairy type. Strong against Fighting, Dragon, Dark.
    ///     Weak to Poison, Steel. Resisted by Fire, Poison, Steel. Immune to Dragon.
    /// </summary>
    FAIRY = 18,

    /// <summary>
    ///     Special type with no strengths or weaknesses. Used for moves like Struggle.
    /// </summary>
    TYPELESS = 19
}

/// <summary>
///     Helper enum for move targets to make them more readable in code.
///     Defines the possible targeting patterns for moves in battle.
/// </summary>
public enum MoveTarget
{
    /// <summary>
    ///     Targets a specific move rather than a Pokémon. Used by Counter, Mirror Coat, etc.
    /// </summary>
    SPECIFIC_MOVE = 1,

    /// <summary>
    ///     Targets a selected Pokémon, but only works if used before the target. Used by Me First.
    /// </summary>
    SELECTED_POKEMON_ME_FIRST = 2,

    /// <summary>
    ///     Targets an ally Pokémon. Used by Helping Hand, Aromatherapy, etc.
    /// </summary>
    ALLY = 3,

    /// <summary>
    ///     Targets the user's side of the field. Used by Light Screen, Reflect, etc.
    /// </summary>
    USERS_FIELD = 4,

    /// <summary>
    ///     Targets either the user or an ally. Used by Acupressure.
    /// </summary>
    USER_OR_ALLY = 5,

    /// <summary>
    ///     Targets the opponent's side of the field. Used by Spikes, Toxic Spikes, etc.
    /// </summary>
    OPPONENTS_FIELD = 6,

    /// <summary>
    ///     Targets the user. Used by Swords Dance, Recover, etc.
    /// </summary>
    USER = 7,

    /// <summary>
    ///     Targets a random opponent. Used by Outrage in multi-battles.
    /// </summary>
    RANDOM_OPPONENT = 8,

    /// <summary>
    ///     Targets all Pokémon except the user. Used by Earthquake, Surf, etc. in multi-battles.
    /// </summary>
    ALL_OTHER_POKEMON = 9,

    /// <summary>
    ///     Targets a selected Pokémon. Used by most direct attacks like Tackle, Flamethrower, etc.
    /// </summary>
    SELECTED_POKEMON = 10,

    /// <summary>
    ///     Targets all opponents. Used by Leer, Growl, etc. in multi-battles.
    /// </summary>
    ALL_OPPONENTS = 11,

    /// <summary>
    ///     Targets the entire field. Used by weather moves, Gravity, etc.
    /// </summary>
    ENTIRE_FIELD = 12,

    /// <summary>
    ///     Targets the user and its allies. Used by Heal Bell, Safeguard, etc.
    /// </summary>
    USER_AND_ALLIES = 13,

    /// <summary>
    ///     Targets all Pokémon in battle. Used by Perish Song, Haze, etc.
    /// </summary>
    ALL_POKEMON = 14,

    /// <summary>
    ///     Targets all allies including the user. Used by Heal Pulse in multi-battles.
    /// </summary>
    ALL_ALLIES = 15
}

/// <summary>
///     Provides extension methods for enums used in the battle system.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    ///     Converts an Ability enum value to a formatted display name.
    ///     Replaces underscores with spaces and converts to lowercase.
    /// </summary>
    /// <param name="ability">The ability to format.</param>
    /// <returns>A user-friendly name for the ability.</returns>
    /// <example>
    ///     Ability.FLASH_FIRE.GetPrettyName() returns "flash fire"
    /// </example>
    public static string GetPrettyName(this Ability ability)
    {
        return ability.ToString().ToLower().Replace("_", " ");
    }
}