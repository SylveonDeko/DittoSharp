namespace EeveeCore.Modules.Duels.Impl.Helpers;

public class ValidMovesResult
{
    public enum ResultType
    {
        ForcedMove,
        ValidIndexes,
        Struggle
    }

    private ValidMovesResult()
    {
    }

    public ResultType Type { get; private set; }
    public Move.Move ForcedMove { get; private set; }
    public List<int> ValidMoveIndexes { get; private set; }

    public static ValidMovesResult Forced(Move.Move move)
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