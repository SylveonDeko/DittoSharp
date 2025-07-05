
namespace EeveeCore.Modules.Fishing.Common;

/// <summary>
///     Constants for the fishing system.
/// </summary>
public static class FishingConstants
{
    // Experience and level calculations
    /// <summary>
    ///     Base experience required for level calculations.
    /// </summary>
    public const int BaseExp = 100;
    
    /// <summary>
    ///     Exponent used in level experience calculations.
    /// </summary>
    public const double LevelExponent = 1.75;
    
    /// <summary>
    ///     Multiplier applied to experience calculations after level 100.
    /// </summary>
    public const double LevelMultiplierAfter100 = 1.00015;
    
    /// <summary>
    ///     Divisor used in high-level experience calculations.
    /// </summary>
    public const int LevelDivisor = 8;
    
    /// <summary>
    ///     Adjustment factor for high-level experience calculations.
    /// </summary>
    public const double LevelAdjustment = 1.011;
    
    /// <summary>
    ///     Value subtracted from high-level experience calculations.
    /// </summary>
    public const int ExpSubtraction = 2800000;
    
    /// <summary>
    ///     Maximum fishing level achievable.
    /// </summary>
    public const int MaxLevel = 401;
    
    // Credits system
    /// <summary>
    ///     Credits earned per level above the minimum.
    /// </summary>
    public const int CreditsPerLevel = 50;
    
    /// <summary>
    ///     Base credits earned at minimum level.
    /// </summary>
    public const int CreditsBase = 2000;
    
    /// <summary>
    ///     Maximum credits that can be earned per fishing attempt.
    /// </summary>
    public const int CreditsMax = 20000;
    
    /// <summary>
    ///     Minimum level required to earn credits.
    /// </summary>
    public const int CreditsMinLevel = 101;
    
    /// <summary>
    ///     Maximum level for linear credit progression.
    /// </summary>
    public const int CreditsMaxLevel = 301;
    
    /// <summary>
    ///     Level at which maximum credits are always earned.
    /// </summary>
    public const int CreditsCapLevel = 400;
    
    // Fishing mechanics
    /// <summary>
    ///     Minimum base time for fishing attempts in seconds.
    /// </summary>
    public const int BaseTimeMin = 15;
    
    /// <summary>
    ///     Maximum base time for fishing attempts in seconds.
    /// </summary>
    public const int BaseTimeMax = 20;
    
    /// <summary>
    ///     Base threshold for shiny Pokemon encounters.
    /// </summary>
    public const int ShinyThreshold = 5000;
    
    /// <summary>
    ///     Energy consumed per fishing attempt.
    /// </summary>
    public const int EnergyPerFish = 1;
    
    /// <summary>
    ///     Divisor for calculating experience gain from rod prices.
    /// </summary>
    public const double ExpGainDivisor = 250.0;
    
    // Rod level requirements
    /// <summary>
    ///     Minimum level required to use the Supreme Rod.
    /// </summary>
    public const int SupremeRodMinLevel = 105;
    
    /// <summary>
    ///     Minimum level required to use the Epic Rod.
    /// </summary>
    public const int EpicRodMinLevel = 150;
    
    /// <summary>
    ///     Minimum level required to use the Master Rod.
    /// </summary>
    public const int MasterRodMinLevel = 200;
    
    // Chest chances
    /// <summary>
    ///     Chance denominator for common chest drops at levels under 150.
    /// </summary>
    public const int CommonChestChanceUnder150 = 50;
    
    /// <summary>
    ///     Chance denominator for common chest drops at levels 150 and above.
    /// </summary>
    public const int CommonChestChanceOver150 = 40;
    
    /// <summary>
    ///     Chance denominator for rare chest drops at levels under 150.
    /// </summary>
    public const int RareChestChanceUnder150 = 400;
    
    /// <summary>
    ///     Chance denominator for rare chest drops at levels 150 and above.
    /// </summary>
    public const int RareChestChanceOver150 = 300;
    
    // Ultra rare item chance
    /// <summary>
    ///     Base chance for ultra rare item drops.
    /// </summary>
    public const double UltraRareBaseChance = 2.0;
    
    /// <summary>
    ///     Experience bonus applied to ultra rare item chances.
    /// </summary>
    public const double UltraRareExpBonus = 100000.0 / 8333.0;
    
    // Scatter mechanics
    /// <summary>
    ///     Chance denominator for scattering block characters in Pokemon names.
    /// </summary>
    public const int ScatterBlockChance = 2;
    
    /// <summary>
    ///     Character used to scatter/hide letters in Pokemon names.
    /// </summary>
    public const string ScatterBlock = "▫️";
    
    // Multi-box chance
    /// <summary>
    ///     Chance denominator for multi-box events.
    /// </summary>
    public const int MultiBoxChance = 10;
    
    /// <summary>
    ///     Chance denominator for getting items from multi-boxes.
    /// </summary>
    public const int MultiBoxItemChance = 3;
    
    /// <summary>
    ///     Maximum number of battle multipliers a user can have.
    /// </summary>
    public const int BattleMultiplierCap = 50;
    
    // Rod time bonuses
    /// <summary>
    ///     Dictionary mapping rod types to their time bonus ranges in seconds.
    /// </summary>
    public static readonly Dictionary<string, (int Min, int Max)> RodBonuses = new()
    {
        { "old-rod", (0, 1) },
        { "new-rod", (1, 1) },
        { "good-rod", (1, 2) },
        { "super-rod", (1, 3) },
        { "ultra-rod", (2, 5) },
        { "supreme-rod", (4, 7) },
        { "master-rod", (6, 9) }
    };
    
    // Rod second chance items
    /// <summary>
    ///     Rods that provide a second chance to catch Pokemon after timeout.
    /// </summary>
    public static readonly string[] SecondChanceRods = ["supreme-rod", "master-rod"];
    
    /// <summary>
    ///     Chance denominator for second chance attempts with premium rods.
    /// </summary>
    public const int SecondChanceChance = 3;
    
    // Sellable items
    /// <summary>
    ///     Array of items that are considered sellable/useless.
    /// </summary>
    public static readonly string[] SellableItems =
    [
        "nugget", 
        "big-nugget", 
        "big-pearl", 
        "pearl", 
        "comet-shard"
    ];
    
    // Ultra rare items
    /// <summary>
    ///     Array of ultra rare items that can be obtained from fishing.
    /// </summary>
    public static readonly string[] UltraRareItems = ["supreme-rod", "rusty-shield"];
    
    // Chest items
    /// <summary>
    ///     Array of chest items that go into inventory rather than items.
    /// </summary>
    public static readonly string[] ChestItems = ["common-chest", "rare-chest", "mystery-box"];
    
    // Funny fail messages for empty boxes
    /// <summary>
    ///     Array of humorous messages displayed when multi-box events fail.
    /// </summary>
    public static readonly string[] FunnyFails =
    [
        "The box explodes leaving behind a cloud of glitter and sadness",
        "Looks like we accidentally sent you a box of disappointment instead of a box of fun!",
        "Sorry, the contents got lost in translation - literally!",
        "Congratulations, you've won the world's lightest mystery box!",
        "It's like magic, but without the trick!",
        "Looks like the contents are playing a game of hide-and-seek - they're just really good at hiding!",
        "Don't worry, we've got a whole team of detectives on the case to solve this mystery of nothing in the box!",
        "The contents were actually in there, at some point... swear it!",
        "Sorry, the contents are out for lunch - try again later!",
        "This box is like a metaphor for life - sometimes you get nothing. Thats all.",
        "Looks like we accidentally sent you a box of dreams instead of a box of reality!",
        "Looks like we accidentally sent you a box of fresh air!",
        "We were going for a minimalist vibe with this one.",
        "The contents must have escaped during shipping...",
        "Congratulations, you just won an all-expenses-paid trip to Disappointment Island! Nothing is in the box!",
        "This is what happens when we let the new staff members pack the boxes. Blame them.",
        "We were going for the 'surprise without the prize' experience with this one.",
        "Sorry, the contents got stage fright and didn't want to come out.",
        "This is what happens when you leave a box unattended in general chat.",
        "Looks like someone forgot to hit the 'fill contents' button on the assembly line.",
        "Don't worry, we're working on a new 'invisible items' collection - this box is just a sneak peek!",
        "Sorry, the contents got stuck in traffic and didn't make it on time!",
        "This box is like a metaphor for life - empty.",
        "Looks like we forgot to fill this one up - oops!",
        "Don't worry, the contents are just taking a vacation somewhere else!",
        "We've officially reached peak disappointment!",
        "Congratulations, you found the world's rarest item - nothing!",
        "Looks like a big nothing...",
        "Sorry, we couldn't afford to put anything in this one.",
        "This box is the ultimate test of your optimism - it's either empty or full of air. ",
        "Looks like this box got mugged along the way, better luck next time!",
        "The contents must be invisible, because they've vanished!",
        "There's nothing in here except a really good opportunity for you to use your imagination!",
        "The box was on a diet and lost everything inside! Oh well.",
        "Someone stole the contents and replaced them with a note saying 'better luck next time'!",
        "It's the world's first zero-calorie box!... awesome!",
        "Looks like you got the 'empty surprise' option! Congrats!",
        "The contents were probably abducted by aliens...right?",
        "Congratulations, you just won an empty box!"
    ];
    
    // Energy phrases
    /// <summary>
    ///     Array of phrases to display remaining energy to users.
    /// </summary>
    public static readonly string[] EnergyPhrases =
    [
        "You have used up a point of energy. Only {0} remain.",
        "You have used up all but {0} of your energy points.",
        "You have {0} more energy to spend.",
        "You have spent one Energy Point. {0} Remaining.",
        "You have used up one of your Energy Points. You have {0} left."
    ];

    // Rarity chances (out of 10000)
    /// <summary>
    ///     Dictionary mapping rarity names to their chance thresholds out of 10000.
    /// </summary>
    public static readonly Dictionary<string, double> RarityChances = new()
    {
        { "common", 8000 },      // 80%
        { "uncommon", 9500 },    // 15%
        { "rare", 9900 },        // 4%
        { "extremely_rare", 9999 }, // 0.99%
        { "ultra_rare", 10000 }  // 0.01%
    };

    // Shop price tiers
    /// <summary>
    ///     Dictionary mapping tier names to their price ranges for shop items.
    /// </summary>
    public static readonly Dictionary<string, (ulong Min, ulong Max)> ShopTiers = new()
    {
        { "cheap", (0, 3500) },
        { "mid", (3500, 5000) },
        { "expensive", (5000, 8000) },
        { "super", (8000, 999999999) }
    };

    // Excluded items from fishing rewards
    /// <summary>
    ///     Array of items excluded from fishing reward pools.
    /// </summary>
    public static readonly string[] ExcludedItems = ["old-rod", "master-rod", "epic-rod"];
    
    // Fishing image
    /// <summary>
    ///     URL or filename for the fishing animation image.
    /// </summary>
    public const string FishingImageUrl = "fishing.gif";
}

/// <summary>
///     Pokemon lists for different water types and rarities.
/// </summary>
public static class WaterPokemonLists
{
    /// <summary>
    ///     Common water-type Pokemon that can be caught while fishing.
    /// </summary>
    public static readonly string[] CommonWater =
    [
        "magikarp", "goldeen", "psyduck", "slowpoke", "tentacool", "seel", "shellder",
        "krabby", "horsea", "staryu", "poliwag", "squirtle", "totodile", "mudkip",
        "piplup", "oshawott", "froakie", "popplio", "sobble"
    ];

    /// <summary>
    ///     Uncommon water-type Pokemon that can be caught while fishing.
    /// </summary>
    public static readonly string[] UncommonWater =
    [
        "seaking", "golduck", "slowbro", "tentacruel", "dewgong", "cloyster", "kingler",
        "seadra", "starmie", "poliwhirl", "wartortle", "croconaw", "marshtomp", "prinplup",
        "dewott", "frogadier", "brionne", "drizzile"
    ];

    /// <summary>
    ///     Rare water-type Pokemon that can be caught while fishing.
    /// </summary>
    public static readonly string[] RareWater =
    [
        "poliwrath", "politoed", "blastoise", "feraligatr", "swampert", "empoleon",
        "samurott", "greninja", "primarina", "inteleon", "gyarados", "kingdra",
        "vaporeon", "lapras", "wailmer", "wailord", "relicanth", "luvdisc"
    ];

    /// <summary>
    ///     Extremely rare water-type Pokemon that can be caught while fishing.
    /// </summary>
    public static readonly string[] ExtremelyRareWater =
    [
        "articuno", "suicune", "lugia", "kyogre", "palkia", "phione", "manaphy",
        "keldeo", "volcanion", "tapu-fini", "primarina"
    ];

    /// <summary>
    ///     Ultra rare water-type Pokemon that can be caught while fishing.
    /// </summary>
    public static readonly string[] UltraRareWater =
    [
        "arceus", "dialga", "giratina", "reshiram", "zekrom", "kyurem", "xerneas",
        "yveltal", "zygarde", "solgaleo", "lunala", "necrozma", "zacian", "zamazenta",
        "eternatus", "calyrex"
    ];
}