using Ditto.Modules.Duels.Impl;

namespace Ditto.Modules.Duels.Extensions;

public static class BattleExtensions
{
    private static readonly Dictionary<Battle, int> CurrentInteractionTurns = new();
    private static readonly Dictionary<Battle, int> CurrentSwapTurns = new();
    private static readonly Dictionary<Battle, bool> CurrentMidTurns = new();

    public static int CurrentInteractionTurn(this Battle battle)
    {
        if (!CurrentInteractionTurns.ContainsKey(battle))
            CurrentInteractionTurns[battle] = 0;
        return CurrentInteractionTurns[battle];
    }

    public static void SetCurrentInteractionTurn(this Battle battle, int turn)
    {
        CurrentInteractionTurns[battle] = turn;
    }

    public static int CurrentSwapTurn(this Battle battle)
    {
        if (!CurrentSwapTurns.ContainsKey(battle))
            CurrentSwapTurns[battle] = 0;
        return CurrentSwapTurns[battle];
    }

    public static void SetCurrentSwapTurn(this Battle battle, int turn)
    {
        CurrentSwapTurns[battle] = turn;
    }

    public static bool CurrentMidTurn(this Battle battle)
    {
        if (!CurrentMidTurns.ContainsKey(battle))
            CurrentMidTurns[battle] = false;
        return CurrentMidTurns[battle];
    }

    public static void SetCurrentMidTurn(this Battle battle, bool midTurn)
    {
        CurrentMidTurns[battle] = midTurn;
    }
}