using EeveeCore.Modules.Games.Common;
using EeveeCore.Modules.Games.Models;

namespace EeveeCore.Modules.Games.Services;

/// <summary>
///     Service for handling slot machine game functionality.
///     Handles spinning logic and win calculation.
/// </summary>
public class SlotMachineService : INService
{
    private static readonly Random Random = new();

    /// <summary>
    ///     Performs a slot machine spin and calculates the result.
    /// </summary>
    /// <returns>The result of the slot machine spin.</returns>
    public SlotMachineResult Spin()
    {
        var reel1 = GetWeightedRandomSymbol();
        var reel2 = GetWeightedRandomSymbol();
        var reel3 = GetWeightedRandomSymbol();

        var result = new SlotMachineResult
        {
            Reel1 = reel1,
            Reel2 = reel2,
            Reel3 = reel3
        };

        CalculateWinResult(result);

        return result;
    }

    /// <summary>
    ///     Gets a weighted random symbol from the slot machine symbols.
    /// </summary>
    /// <returns>A slot machine symbol emoji.</returns>
    private static string GetWeightedRandomSymbol()
    {
        var totalWeight = GameConstants.SlotMachine.Weights.Sum();
        var randomValue = Random.Next(totalWeight);
        var currentWeight = 0;

        for (var i = 0; i < GameConstants.SlotMachine.AllSymbols.Length; i++)
        {
            currentWeight += GameConstants.SlotMachine.Weights[i];
            if (randomValue < currentWeight)
                return GameConstants.SlotMachine.AllSymbols[i];
        }

        return GameConstants.SlotMachine.AllSymbols[0];
    }

    /// <summary>
    ///     Calculates the win result and sets the appropriate message and payout.
    /// </summary>
    /// <param name="result">The slot machine result to evaluate.</param>
    private static void CalculateWinResult(SlotMachineResult result)
    {
        var reels = new[] { result.Reel1, result.Reel2, result.Reel3 };

        if (reels.All(r => r == GameConstants.SlotMachine.Seven))
        {
            result.IsWin = true;
            result.WinType = "JACKPOT";
            result.PayoutMultiplier = 1000m;
            result.ResultMessage = $"🎉 **JACKPOT!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.All(r => r == GameConstants.SlotMachine.Star))
        {
            result.IsWin = true;
            result.WinType = "TRIPLE_STAR";
            result.PayoutMultiplier = 500m;
            result.ResultMessage = $"🎉 **X3 STAR WIN!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.All(r => r == GameConstants.SlotMachine.Coin))
        {
            result.IsWin = true;
            result.WinType = "TRIPLE_COIN";
            result.PayoutMultiplier = 250m;
            result.ResultMessage = $"🎉 **X3 COIN WIN!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.All(r => r == GameConstants.SlotMachine.Cherry))
        {
            result.IsWin = true;
            result.WinType = "TRIPLE_CHERRY";
            result.PayoutMultiplier = 100m;
            result.ResultMessage = $"🎉 **X3 CHERRY WIN!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.All(r => r == GameConstants.SlotMachine.Pikachu))
        {
            result.IsWin = true;
            result.WinType = "TRIPLE_PIKACHU";
            result.PayoutMultiplier = 75m;
            result.ResultMessage = $"🎉 **X3 PIKACHU WIN!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.All(r => r == GameConstants.SlotMachine.Present))
        {
            result.IsWin = true;
            result.WinType = "TRIPLE_PRESENT";
            result.PayoutMultiplier = 50m;
            result.ResultMessage = $"🎉 **X3 PRESENT WIN!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.All(r => r == GameConstants.SlotMachine.Purple))
        {
            result.IsWin = true;
            result.WinType = "TRIPLE_PURPLE";
            result.PayoutMultiplier = 25m;
            result.ResultMessage = $"🎉 **PURPLE WIN!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.Contains(GameConstants.SlotMachine.Seven))
        {
            result.IsWin = true;
            result.WinType = "LUCKY_SEVEN";
            result.PayoutMultiplier = 10m;
            result.ResultMessage = $"🎉 **LUCKY SEVEN!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        if (reels.All(r => r == reels[0]))
        {
            result.IsWin = true;
            result.WinType = "TRIPLE_MATCH";
            result.PayoutMultiplier = 5m;
            result.ResultMessage = $"🎉 **TRIPLE MATCH!!!** 🎉\n{result.Reel1} {result.Reel2} {result.Reel3}";
            return;
        }

        result.IsWin = false;
        result.WinType = "NO_WIN";
        result.PayoutMultiplier = 0m;
        result.ResultMessage = $"💔 **TRY AGAIN!** 💔\n{result.Reel1} {result.Reel2} {result.Reel3}";
    }

    /// <summary>
    ///     Gets the probability of each win type for display purposes.
    /// </summary>
    /// <returns>A dictionary mapping win types to their approximate probabilities.</returns>
    public Dictionary<string, double> GetWinProbabilities()
    {
        var totalWeight = GameConstants.SlotMachine.Weights.Sum();
        var probabilities = new Dictionary<string, double>();

        for (var i = 0; i < GameConstants.SlotMachine.AllSymbols.Length; i++)
        {
            var symbolProb = (double)GameConstants.SlotMachine.Weights[i] / totalWeight;
            var tripleProb = Math.Pow(symbolProb, 3) * 100;
            
            var symbolName = GetSymbolName(GameConstants.SlotMachine.AllSymbols[i]);
            probabilities[$"Triple {symbolName}"] = tripleProb;
        }

        var sevenProb = (double)GameConstants.SlotMachine.Weights[6] / totalWeight;
        var luckySevenProb = (1 - Math.Pow(1 - sevenProb, 3)) * 100;
        probabilities["Lucky Seven"] = luckySevenProb;

        return probabilities;
    }

    /// <summary>
    ///     Gets a human-readable name for a symbol emoji.
    /// </summary>
    /// <param name="symbol">The symbol emoji.</param>
    /// <returns>A human-readable name for the symbol.</returns>
    private static string GetSymbolName(string symbol)
    {
        return symbol switch
        {
            GameConstants.SlotMachine.Cherry => "Cherry",
            GameConstants.SlotMachine.Pikachu => "Pikachu",
            GameConstants.SlotMachine.Present => "Present",
            GameConstants.SlotMachine.Purple => "Purple",
            GameConstants.SlotMachine.Coin => "Coin",
            GameConstants.SlotMachine.Star => "Star",
            GameConstants.SlotMachine.Seven => "Seven",
            _ => "Unknown"
        };
    }
}