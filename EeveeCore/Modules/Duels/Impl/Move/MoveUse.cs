namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Uses this move as attacker on defender.
    /// </summary>
    /// <returns>A string of formatted results of the move.</returns>
    public string Use(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle, bool usePP = true,
        bool overrideSleep = false, bool bounced = false)
    {
        if (attacker.HasMoved && usePP) return "";

        Used = true;
        if (usePP)
        {
            attacker.HasMoved = true;
            attacker.LastMove = this;
            attacker.BeakBlast = false;
            attacker.DestinyBond = false;
            attacker.Dive = false;
            attacker.Dig = false;
            attacker.Fly = false;
            attacker.ShadowForce = false;
        }

        var currentType = GetType(attacker, defender, battle);
        var effectChance = GetEffectChance(attacker, defender, battle);
        var msg = "";

        var (statusMsg, shouldAbortStatus) = CheckStatusConditions(attacker, defender, battle, usePP, overrideSleep);
        msg += statusMsg;
        if (shouldAbortStatus) return msg;

        if (!bounced)
        {
            msg += $"{attacker.Name} used {PrettyName}!\n";
            attacker.Metronome.Use(Name);
        }

        if (attacker.LockedMove == null && usePP)
        {
            PP -= 1;
            if (defender.Ability(attacker, this) == Ability.PRESSURE && PP != 0)
                if (TargetsOpponent() || new[] { 113, 193, 196, 250, 267 }.Contains(Effect))
                    PP -= 1;

            if (PP == 0) msg += "It ran out of PP!\n";
        }

        if (attacker.ChoiceMove == null && usePP)
        {
            if (attacker.HeldItem == "choice-scarf" || attacker.HeldItem == "choice-band" ||
                attacker.HeldItem == "choice-specs")
                attacker.ChoiceMove = this;
            else if (attacker.Ability() == Ability.GORILLA_TACTICS) attacker.ChoiceMove = this;
        }

        msg += HandleStanceChange(attacker);

        var (powderMsg, shouldAbortPowder) = HandlePowderEffects(attacker, defender, battle, currentType);
        msg += powderMsg;
        if (shouldAbortPowder) return msg;

        var (snatchMsg, shouldAbortSnatch) = HandleSnatch(attacker, defender, battle);
        msg += snatchMsg;
        if (shouldAbortSnatch) return msg;

        if (!CheckExecutable(attacker, defender, battle))
        {
            msg += "But it failed!\n";
            if (Effect is 28 or 118) attacker.LockedMove = null;

            attacker.LastMoveFailed = true;
            return msg;
        }

        msg += SetupMultiTurnMoves(attacker, defender, battle);

        msg += ProcessMoveEffects(attacker, defender, battle, currentType, effectChance, bounced);

        var numHits = 0;
        if (!attacker.LastMoveFailed)
        {
            msg += CalculateDamage(attacker, defender, battle, currentType, ref numHits);
        }

        battle.LastMoveEffect = Effect;

        msg += ApplyPostEffects(attacker, defender, battle, effectChance, numHits);

        msg += ApplyStatChanges(attacker, defender, battle, effectChance);

        msg += ApplyFlinchEffects(attacker, defender, battle, effectChance, numHits);

        msg += HandleSpecialEffects(attacker, defender, battle, effectChance);

        msg += HandleSwapEffects(attacker, defender, battle);

        msg += HandleLifeOrb(attacker, defender, battle);

        if (defender.Ability(attacker, this) != Ability.DANCER || !IsDance() || !usePP) return msg;
        var hm = defender.HasMoved;
        msg += Use(defender, attacker, battle, false);
        defender.HasMoved = hm;

        return msg;
    }
}