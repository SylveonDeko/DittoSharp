namespace EeveeCore.Modules.Duels.Impl.DuelPokemon;

public partial class DuelPokemon
{
    /// <summary>
    ///     Attempts to confuse this poke.
    ///     Returns a formatted message.
    /// </summary>
    /// <param name="attacker">The Pokemon causing the confusion.</param>
    /// <param name="move">The move causing the confusion.</param>
    /// <param name="source">A description of the confusion source.</param>
    /// <returns>A formatted message describing the confusion attempt.</returns>
    public string Confuse(DuelPokemon attacker = null, Move.Move move = null, string source = "")
    {
        if (Substitute > 0 && (move == null || move.IsAffectedBySubstitute())) return "";
        if (Confusion.Active()) return "";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.OWN_TEMPO) return "";
        Confusion.SetTurns(new Random().Next(2, 6));
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        var msg = $"{Name} is confused{source}!\n";
        if (HeldItem.ShouldEatBerryStatus(attacker)) msg += HeldItem.EatBerry(attacker: attacker, move: move);
        return msg;
    }

    /// <summary>
    ///     Attempts to flinch this poke.
    ///     Returns a formatted message.
    /// </summary>
    /// <param name="attacker">The Pokemon causing the flinch.</param>
    /// <param name="move">The move causing the flinch.</param>
    /// <param name="source">A description of the flinch source.</param>
    /// <returns>A formatted message describing the flinch attempt.</returns>
    public string Flinch(DuelPokemon attacker = null, Move.Move move = null, string source = "")
    {
        var msg = "";
        if (Substitute > 0 && (move == null || move.IsAffectedBySubstitute())) return "";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.INNER_FOCUS)
            return $"{Name} resisted the urge to flinch with its inner focus!\n";
        Flinched = true;
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        msg += $"{Name} flinched{source}!\n";
        if (Ability() == Impl.Ability.STEADFAST) msg += AppendSpeed(1, this, source: "its steadfast");
        return msg;
    }

    /// <summary>
    ///     Attempts to cause attacker to infatuate this poke.
    ///     Returns a formatted message.
    /// </summary>
    /// <param name="attacker">The Pokemon causing the infatuation.</param>
    /// <param name="move">The move causing the infatuation.</param>
    /// <param name="source">A description of the infatuation source.</param>
    /// <returns>A formatted message describing the infatuation attempt.</returns>
    public string Infatuate(DuelPokemon attacker, Move.Move move = null, string source = "")
    {
        var msg = "";
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";
        if (Gender.Contains("-x") || attacker.Gender.Contains("-x")) return "";
        if (Gender == attacker.Gender) return "";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.OBLIVIOUS)
            return $"{Name} is too oblivious to fall in love!\n";
        if (Ability(move: move, attacker: attacker) == Impl.Ability.AROMA_VEIL)
            return $"{Name}'s aroma veil protects it from being infatuated!\n";
        Infatuated = attacker;
        msg += $"{Name} fell in love{source}!\n";
        if (HeldItem.Get() == "destiny-knot") msg += attacker.Infatuate(this, source: $"{Name}'s destiny knot");
        return msg;
    }
}