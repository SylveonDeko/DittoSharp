namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Uses this move as attacker on defender.
    /// </summary>
    /// <returns>A string of formatted results of the move.</returns>
    public string Use(DuelPokemon attacker, DuelPokemon defender, Battle battle, bool usePP = true,
        bool overrideSleep = false, bool bounced = false)
    {
        // This handles an edge case for moves that cause the target to swap out
        if (attacker.HasMoved && usePP) return "";

        Used = true;
        if (usePP)
        {
            attacker.HasMoved = true;
            attacker.LastMove = this;
            attacker.BeakBlast = false;
            attacker.DestinyBond = false;
            // Reset semi-invulnerable status in case this is turn 2
            attacker.Dive = false;
            attacker.Dig = false;
            attacker.Fly = false;
            attacker.ShadowForce = false;
        }

        var currentType = GetType(attacker, defender, battle);
        var effectChance = GetEffectChance(attacker, defender, battle);
        var msg = "";

        // Check status conditions that may prevent move usage
        msg += CheckStatusConditions(attacker, defender, battle, usePP, overrideSleep);
        if (msg.Contains("return msg;")) return msg;

        // OldMove announcement and PP management
        if (!bounced)
        {
            msg += $"{attacker.Name} used {PrettyName}!\n";
            attacker.Metronome.Use(Name);
        }

        // PP
        if (attacker.LockedMove == null && usePP)
        {
            PP -= 1;
            if (defender.Ability(attacker, this) == Ability.PRESSURE && PP != 0)
                if (TargetsOpponent() || new[] { 113, 193, 196, 250, 267 }.Contains(Effect))
                    PP -= 1;

            if (PP == 0) msg += "It ran out of PP!\n";
        }

        // User is using a choice item and had not used a move yet, set that as their only move.
        if (attacker.ChoiceMove == null && usePP)
        {
            if (attacker.HeldItem == "choice-scarf" || attacker.HeldItem == "choice-band" ||
                attacker.HeldItem == "choice-specs")
                attacker.ChoiceMove = this;
            else if (attacker.Ability() == Ability.GORILLA_TACTICS) attacker.ChoiceMove = this;
        }

        // Stance change
        msg += HandleStanceChange(attacker);

        // Powder damage
        msg += HandlePowderEffects(attacker, defender, battle, currentType);
        if (msg.Contains("return msg;")) return msg;

        // Snatch steal
        msg += HandleSnatch(attacker, defender, battle);
        if (msg.Contains("return msg;")) return msg;

        // Check Fail
        if (!CheckExecutable(attacker, defender, battle))
        {
            msg += "But it failed!\n";
            if (Effect is 28 or 118) attacker.LockedMove = null;

            attacker.LastMoveFailed = true;
            return msg;
        }

        // Setup for multi-turn moves
        msg += SetupMultiTurnMoves(attacker, defender, battle);

        // Process move effects
        msg += ProcessMoveEffects(attacker, defender, battle, currentType, effectChance, bounced);
        if (msg.Contains("return msg;")) return msg;

        // Calculate damage if applicable
        int numHits = 0;
        msg += CalculateDamage(attacker, defender, battle, currentType, ref numHits);

        // Fusion Flare/Bolt effect tracking
        battle.LastMoveEffect = Effect;

        // Apply post-damage effects
        msg += ApplyPostEffects(attacker, defender, battle, effectChance, numHits);

        // Apply stat changes
        msg += ApplyStatChanges(attacker, defender, battle, effectChance);

        // Apply flinch effects
        msg += ApplyFlinchEffects(attacker, defender, battle, effectChance, numHits);

        // Handle move locking, protection, weather, terrain
        msg += HandleSpecialEffects(attacker, defender, battle, effectChance);

        // Handle swap-outs
        msg += HandleSwapEffects(attacker, defender, battle);

        // Handle life orb
        msg += HandleLifeOrb(attacker, defender, battle);

        // Dancer Ability - Runs at the end of move usage
        if (defender.Ability(attacker, this) == Ability.DANCER && IsDance() && usePP)
        {
            var hm = defender.HasMoved;
            msg += Use(defender, attacker, battle, false);
            defender.HasMoved = hm;
        }

        return msg;
    }
}