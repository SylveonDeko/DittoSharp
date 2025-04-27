namespace EeveeCore.Modules.Duels.Impl.Helpers;

/// <summary>
///     Represents the result of checking valid moves for a Pokémon in battle.
///     Provides information about forced moves, valid move indexes, or struggle scenarios.
///     Used to determine which moves a trainer can select during battle.
/// </summary>
public class ValidMovesResult
{
    /// <summary>
    ///     Defines the possible types of move validation results.
    /// </summary>
    public enum ResultType
    {
        /// <summary>
        ///     Indicates the Pokémon is forced to use a specific move.
        ///     Occurs with locked moves like Outrage, Thrash, or Encore.
        /// </summary>
        ForcedMove,

        /// <summary>
        ///     Indicates the Pokémon has a specific set of valid moves to choose from.
        ///     Provides a list of move indexes that can be selected.
        /// </summary>
        ValidIndexes,

        /// <summary>
        ///     Indicates the Pokémon has no valid moves and must use Struggle.
        ///     Occurs when all moves are out of PP or otherwise unavailable.
        /// </summary>
        Struggle
    }

    /// <summary>
    ///     Private constructor to enforce factory method usage.
    /// </summary>
    private ValidMovesResult()
    {
    }

    /// <summary>
    ///     Gets the type of result represented by this instance.
    /// </summary>
    public ResultType Type { get; private set; }

    /// <summary>
    ///     Gets the forced move that must be used when Type is ForcedMove.
    ///     Null for other result types.
    /// </summary>
    public Move.Move ForcedMove { get; private set; }

    /// <summary>
    ///     Gets the list of valid move indexes when Type is ValidIndexes.
    ///     Null for other result types.
    /// </summary>
    public List<int> ValidMoveIndexes { get; private set; }

    /// <summary>
    ///     Creates a result indicating the Pokémon is forced to use a specific move.
    /// </summary>
    /// <param name="move">The move that the Pokémon is forced to use.</param>
    /// <returns>A ValidMovesResult with Type set to ForcedMove.</returns>
    public static ValidMovesResult Forced(Move.Move move)
    {
        return new ValidMovesResult
        {
            Type = ResultType.ForcedMove,
            ForcedMove = move
        };
    }

    /// <summary>
    ///     Creates a result with a list of valid move indexes that the Pokémon can use.
    /// </summary>
    /// <param name="indexes">The list of indexes of valid moves in the Pokémon's move list.</param>
    /// <returns>A ValidMovesResult with Type set to ValidIndexes.</returns>
    public static ValidMovesResult ValidIndexes(List<int> indexes)
    {
        return new ValidMovesResult
        {
            Type = ResultType.ValidIndexes,
            ValidMoveIndexes = indexes
        };
    }

    /// <summary>
    ///     Creates a result indicating the Pokémon has no valid moves and must use Struggle.
    /// </summary>
    /// <returns>A ValidMovesResult with Type set to Struggle.</returns>
    public static ValidMovesResult Struggle()
    {
        return new ValidMovesResult { Type = ResultType.Struggle };
    }
}