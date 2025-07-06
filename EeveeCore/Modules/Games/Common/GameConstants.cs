namespace EeveeCore.Modules.Games.Common;

/// <summary>
///     Contains constants used across various game modules.
/// </summary>
public static class GameConstants
{
    /// <summary>
    ///     The maximum length allowed for words in word-based games.
    /// </summary>
    public const int MaxWordLength = 15;

    /// <summary>
    ///     The number of rows in a word search grid.
    /// </summary>
    public const int WordSearchRows = 15;

    /// <summary>
    ///     The number of columns in a word search grid.
    /// </summary>
    public const int WordSearchColumns = 15;

    /// <summary>
    ///     The left offset for drawing word search grids.
    /// </summary>
    public const int WordSearchLeftOffset = 37;

    /// <summary>
    ///     The top offset for drawing word search grids.
    /// </summary>
    public const int WordSearchTopOffset = 36;

    /// <summary>
    ///     The size of each box in the word search grid.
    /// </summary>
    public const int WordSearchBoxSize = 25;

    /// <summary>
    ///     The duration of a word search game in seconds.
    /// </summary>
    public const int WordSearchGameTimeSeconds = 200;

    /// <summary>
    ///     The number of words to include in a word search game.
    /// </summary>
    public const int WordSearchWordCount = 8;

    /// <summary>
    ///     The number of progress bar blocks to display.
    /// </summary>
    public const int ProgressBarBlocks = 10;

    /// <summary>
    ///     Character used for filled progress bar blocks.
    /// </summary>
    public const string ProgressBarFilledBlock = "█";

    /// <summary>
    ///     Character used for unfilled progress bar blocks.
    /// </summary>
    public const string ProgressBarUnfilledBlock = "░";

    /// <summary>
    ///     Slot machine symbols and their emojis.
    /// </summary>
    public static class SlotMachine
    {
        /// <summary>
        ///     Cherry reel symbol emoji.
        /// </summary>
        public const string Cherry = "<:cherryreel:1180361727330766858>";

        /// <summary>
        ///     Pikachu reel symbol emoji.
        /// </summary>
        public const string Pikachu = "<:pikareel:1180362895113060436>";

        /// <summary>
        ///     Present reel symbol emoji.
        /// </summary>
        public const string Present = "<:presentreel:1180361805462241322>";

        /// <summary>
        ///     Purple reel symbol emoji.
        /// </summary>
        public const string Purple = "<:purpreel:1180372108061192272>";

        /// <summary>
        ///     Coin reel symbol emoji.
        /// </summary>
        public const string Coin = "<:coinreel:1180361707906940959>";

        /// <summary>
        ///     Star reel symbol emoji.
        /// </summary>
        public const string Star = "<:starreel:1180372080236179496>";

        /// <summary>
        ///     Seven reel symbol emoji.
        /// </summary>
        public const string Seven = "<:7reel:1180361763498246204>";

        /// <summary>
        ///     All slot machine symbols.
        /// </summary>
        public static readonly string[] AllSymbols = { Cherry, Pikachu, Present, Purple, Coin, Star, Seven };

        /// <summary>
        ///     Weights for slot machine symbols (higher = more likely).
        /// </summary>
        public static readonly int[] Weights = { 2, 3, 4, 5, 6, 7, 8 };
    }
}