namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Checks status conditions that may prevent move usage.
    /// </summary>
    /// <returns>A tuple containing the formatted message and whether move execution should be aborted.</returns>
    private (string message, bool shouldAbort) CheckStatusConditions(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender,
        Battle battle, bool usePP,
        bool overrideSleep)
    {
        var msg = "";

        if (Effect is 5 or 126 or 168 or 254 or 336 or 398 or 458 or 500 && attacker.NonVolatileEffect.Freeze())
        {
            attacker.NonVolatileEffect.Reset();
            msg += $"{attacker.Name} thawed out!\n";
        }

        if (attacker.NonVolatileEffect.Freeze())
        {
            if (usePP && new Random().Next(0, 5) == 0)
            {
                attacker.NonVolatileEffect.Reset();
                msg += $"{attacker.Name} is no longer frozen!\n";
            }
            else
            {
                msg += $"{attacker.Name} is frozen solid!\n";
                if (Effect == 28) attacker.LockedMove = null;

                return (msg, true); // Abort move execution
            }
        }

        if (attacker.NonVolatileEffect.Paralysis() && new Random().Next(0, 4) == 0)
        {
            msg += $"{attacker.Name} is paralyzed! It can't move!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return (msg, true); // Abort move execution
        }

        if (attacker.Infatuated == defender && new Random().Next(0, 2) == 0)
        {
            msg += $"{attacker.Name} is in love with {defender.Name} and can't bare to hurt them!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return (msg, true); // Abort move execution
        }

        if (attacker.Flinched)
        {
            msg += $"{attacker.Name} flinched! It can't move!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return (msg, true); // Abort move execution
        }

        if (attacker.NonVolatileEffect.Sleep())
        {
            if (usePP && attacker.NonVolatileEffect.SleepTimer.NextTurn())
            {
                attacker.NonVolatileEffect.Reset();
                msg += $"{attacker.Name} woke up!\n";
            }
            else if (Effect != 93 && Effect != 98 && attacker.Ability() != Ability.COMATOSE && !overrideSleep)
            {
                msg += $"{attacker.Name} is fast asleep!\n";
                if (Effect == 28) attacker.LockedMove = null;

                return (msg, true); // Abort move execution
            }
        }

        if (attacker.Confusion.NextTurn()) msg += $"{attacker.Name} is no longer confused!\n";

        if (attacker.Confusion.Active() && new Random().Next(0, 3) == 0)
        {
            msg += $"{attacker.Name} hurt itself in its confusion!\n";
            var (msgadd, numhits) = Confusion().Attack(attacker, attacker, battle);
            msg += msgadd;
            if (Effect == 28) attacker.LockedMove = null;

            return (msg, true); // Abort move execution
        }

        if (attacker.Ability() == Ability.TRUANT && attacker.TruantTurn % 2 == 1)
        {
            msg += $"{attacker.Name} is loafing around!\n";
            if (Effect == 28) attacker.LockedMove = null;

            return (msg, true); // Abort move execution
        }

        return (msg, false); // Continue move execution
    }

    /// <summary>
    ///     Handles stance change abilities like Aegislash's Form Change.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string HandleStanceChange(DuelPokemon.DuelPokemon attacker)
    {
        var msg = "";

        // Stance change
        if (attacker.Ability() == Ability.STANCE_CHANGE)
        {
            if (attacker.Name == "Aegislash" &&
                DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                if (attacker.Form("Aegislash-blade"))
                    msg += $"{attacker.Name} draws its blade!\n";

            if (attacker.Name == "Aegislash-blade" && Effect == 356)
                if (attacker.Form("Aegislash"))
                    msg += $"{attacker.Name} readies its shield!\n";
        }

        return msg;
    }

    /// <summary>
    ///     Handles powder effects for fire-type moves.
    /// </summary>
    /// <returns>A tuple containing the formatted message and whether move execution should be aborted.</returns>
    private (string message, bool shouldAbort) HandlePowderEffects(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender,
        Battle battle, ElementType currentType)
    {
        var msg = "";

        // Powder damage
        if (attacker.Powdered && currentType == ElementType.FIRE && battle.Weather.Get() != "h-rain")
        {
            msg += attacker.Damage(attacker.StartingHp / 4, battle, source: "its powder exploding");
            return (msg, true); // Abort move execution
        }

        return (msg, false); // Continue move execution
    }

    /// <summary>
    ///     Handles snatch effects for status moves.
    /// </summary>
    /// <returns>A tuple containing the formatted message and whether move execution should be aborted.</returns>
    private (string message, bool shouldAbort) HandleSnatch(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
    {
        var msg = "";

        // Snatch steal
        if (defender.Snatching && SelectableBySnatch())
        {
            msg += $"{defender.Name} snatched the move!\n";
            msg += Use(defender, attacker, battle, false);
            return (msg, true); // Abort move execution
        }

        return (msg, false); // Continue move execution
    }
}