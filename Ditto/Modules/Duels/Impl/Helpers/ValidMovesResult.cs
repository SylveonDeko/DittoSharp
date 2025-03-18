namespace Duels.Helpers;

public class ValidMovesResult
{
    public enum ResultType
    {
        ForcedMove,
        ValidIndexes,
        Struggle
    }

    public ResultType Type { get; private set; }
    public Move ForcedMove { get; private set; }
    public List<int> ValidMoveIndexes { get; private set; }

    private ValidMovesResult() { }

    public static ValidMovesResult Forced(Move move)
    {
        return new ValidMovesResult
        {
            Type = ResultType.ForcedMove,
            ForcedMove = move
        };
    }

    public static ValidMovesResult ValidIndexes(List<int> indexes)
    {
        return new ValidMovesResult
        {
            Type = ResultType.ValidIndexes,
            ValidMoveIndexes = indexes
        };
    }

    public static ValidMovesResult Struggle()
    {
        return new ValidMovesResult { Type = ResultType.Struggle };
    }
}