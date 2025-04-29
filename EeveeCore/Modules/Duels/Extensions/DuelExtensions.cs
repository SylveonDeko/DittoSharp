using EeveeCore.Modules.Duels.Impl;

namespace EeveeCore.Modules.Duels.Extensions;

/// <summary>
///     Extension methods for tracking and managing the state of Battle objects.
/// </summary>
public static class BattleExtensions
{
    /// <summary>
    ///     Dictionary that tracks the current interaction turn number for each Battle.
    /// </summary>
    private static readonly Dictionary<Battle, int> CurrentInteractionTurns = new();

    /// <summary>
    ///     Dictionary that tracks the current swap turn number for each Battle.
    /// </summary>
    private static readonly Dictionary<Battle, int> CurrentSwapTurns = new();

    /// <summary>
    ///     Dictionary that tracks whether each Battle is currently in a mid-turn state.
    /// </summary>
    private static readonly Dictionary<Battle, bool> CurrentMidTurns = new();

    /// <summary>
    ///     Gets the current interaction turn number for a Battle.
    /// </summary>
    /// <param name="battle">The Battle to get the interaction turn for.</param>
    /// <returns>The current interaction turn number.</returns>
    public static int CurrentInteractionTurn(this Battle battle)
    {
        if (!CurrentInteractionTurns.ContainsKey(battle))
            CurrentInteractionTurns[battle] = 0;
        return CurrentInteractionTurns[battle];
    }

    /// <summary>
    ///     Sets the current interaction turn number for a Battle.
    /// </summary>
    /// <param name="battle">The Battle to set the interaction turn for.</param>
    /// <param name="turn">The turn number to set.</param>
    public static void SetCurrentInteractionTurn(this Battle battle, int turn)
    {
        CurrentInteractionTurns[battle] = turn;
    }

    /// <summary>
    ///     Gets the current swap turn number for a Battle.
    /// </summary>
    /// <param name="battle">The Battle to get the swap turn for.</param>
    /// <returns>The current swap turn number.</returns>
    public static int CurrentSwapTurn(this Battle battle)
    {
        if (!CurrentSwapTurns.ContainsKey(battle))
            CurrentSwapTurns[battle] = 0;
        return CurrentSwapTurns[battle];
    }

    /// <summary>
    ///     Sets the current swap turn number for a Battle.
    /// </summary>
    /// <param name="battle">The Battle to set the swap turn for.</param>
    /// <param name="turn">The turn number to set.</param>
    public static void SetCurrentSwapTurn(this Battle battle, int turn)
    {
        CurrentSwapTurns[battle] = turn;
    }

    /// <summary>
    ///     Gets whether a Battle is currently in a mid-turn state.
    /// </summary>
    /// <param name="battle">The Battle to check.</param>
    /// <returns>True if the Battle is in a mid-turn state, false otherwise.</returns>
    public static bool CurrentMidTurn(this Battle battle)
    {
        if (!CurrentMidTurns.ContainsKey(battle))
            CurrentMidTurns[battle] = false;
        return CurrentMidTurns[battle];
    }

    /// <summary>
    ///     Sets whether a Battle is in a mid-turn state.
    /// </summary>
    /// <param name="battle">The Battle to update.</param>
    /// <param name="midTurn">The mid-turn state to set.</param>
    public static void SetCurrentMidTurn(this Battle battle, bool midTurn)
    {
        CurrentMidTurns[battle] = midTurn;
    }
}