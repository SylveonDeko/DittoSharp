namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Handles special effects (move locking, protection, weather, terrain, etc.).
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string HandleSpecialEffects(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle, int? effectChance)
    {
        var msg = "";

        switch (Effect)
        {
            // OldMove locking
            case 87 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects its move from being disabled!\n";
                break;
            case 87:
                defender.Disable.Set(defender.LastMove, new Random().Next(4, 8));
                msg += $"{defender.Name}'s {defender.LastMove.PrettyName} was disabled!\n";
                break;
            case 176 when defender.Ability(attacker, this) == Ability.OBLIVIOUS:
                msg += $"{defender.Name} is too oblivious to be taunted!\n";
                break;
            case 176 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being taunted!\n";
                break;
            case 176:
            {
                if (defender.HasMoved)
                    defender.Taunt.SetTurns(4);
                else
                    defender.Taunt.SetTurns(3);

                msg += $"{defender.Name} is being taunted!\n";
                break;
            }
            case 91 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being encored!\n";
                break;
            case 91:
            {
                defender.Encore.Set(defender.LastMove, 4);
                if (!defender.HasMoved) defender.Owner.SelectedAction = new Trainer.MoveAction(defender.LastMove);

                msg += $"{defender.Name} is giving an encore!\n";
                break;
            }
            case 166 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being tormented!\n";
                break;
            case 166:
                defender.Torment = true;
                msg += $"{defender.Name} is tormented!\n";
                break;
            case 193:
                attacker.Imprison = true;
                msg += $"{attacker.Name} imprisons!\n";
                break;
            case 237 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                break;
            case 237:
                defender.HealBlock.SetTurns(5);
                msg += $"{defender.Name} is blocked from healing!\n";
                break;
            case 496 when defender.Ability(attacker, this) == Ability.AROMA_VEIL:
                msg += $"{defender.Name}'s aroma veil protects it from being heal blocked!\n";
                break;
            case 496:
                defender.HealBlock.SetTurns(2);
                msg += $"{defender.Name} is blocked from healing!\n";
                break;
            // Weather changing
            case 116:
                msg += battle.Weather.Set("sandstorm", attacker);
                break;
            case 137:
                msg += battle.Weather.Set("rain", attacker);
                break;
            case 138:
                msg += battle.Weather.Set("sun", attacker);
                break;
            case 165:
                msg += battle.Weather.Set("hail", attacker);
                break;
            // Terrain changing
            case 352:
                msg += battle.Terrain.Set("grassy", attacker);
                break;
            case 353:
                msg += battle.Terrain.Set("misty", attacker);
                break;
            case 369:
                msg += battle.Terrain.Set("electric", attacker);
                break;
            case 395:
                msg += battle.Terrain.Set("psychic", attacker);
                break;
        }

        // Protection
        if (new[] { 112, 117, 279, 356, 362, 384, 454, 488, 499 }.Contains(Effect))
        {
            attacker.ProtectionUsed = true;
            attacker.ProtectionChance *= 3;
        }

        switch (Effect)
        {
            case 112:
                attacker.Protect = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
            case 117:
                attacker.Endure = true;
                msg += $"{attacker.Name} braced itself!\n";
                break;
            case 279:
                attacker.WideGuard = true;
                msg += $"Wide guard protects {attacker.Name}!\n";
                break;
            case 350:
                attacker.CraftyShield = true;
                msg += $"A crafty shield protects {attacker.Name} from status moves!\n";
                break;
            case 356:
                attacker.KingShield = true;
                msg += $"{attacker.Name} shields itself!\n";
                break;
            case 362:
                attacker.SpikyShield = true;
                msg += $"{attacker.Name} shields itself!\n";
                break;
            case 377:
                attacker.MatBlock = true;
                msg += $"{attacker.Name} shields itself!\n";
                break;
            case 384:
                attacker.BanefulBunker = true;
                msg += $"{attacker.Name} bunkers down!\n";
                break;
            case 307:
                attacker.QuickGuard = true;
                msg += $"{attacker.Name} guards itself!\n";
                break;
            case 454:
                attacker.Obstruct = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
            case 488:
                attacker.SilkTrap = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
            case 499:
                attacker.BurningBulwark = true;
                msg += $"{attacker.Name} protected itself!\n";
                break;
        }

        // Sound-based move with throat spray
        if (IsSoundBased() && attacker.HeldItem == "throat-spray")
        {
            msg += attacker.AppendSpAtk(1, attacker, source: "its throat spray");
            attacker.HeldItem.Use();
        }

        switch (Effect)
        {
            // Tar Shot
            case 477 when !defender.TarShot:
                defender.TarShot = true;
                msg += $"{defender.Name} is covered in sticky tar!\n";
                break;
            // Tidy Up
            case 487:
                defender.Owner.Spikes = 0;
                defender.Owner.ToxicSpikes = 0;
                defender.Owner.StealthRock = false;
                defender.Owner.StickyWeb = false;
                defender.Substitute = 0;
                attacker.Owner.Spikes = 0;
                attacker.Owner.ToxicSpikes = 0;
                attacker.Owner.StealthRock = false;
                attacker.Owner.StickyWeb = false;
                attacker.Substitute = 0;
                msg += $"{attacker.Name} tidied up!\n";
                break;
            // Syrup Bomb
            case 503:
                defender.SyrupBomb.SetTurns(4);
                msg += $"{defender.Name} got covered in sticky candy syrup!\n";
                break;
        }

        return msg;
    }

    /// <summary>
    ///     Handles swap effects.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string HandleSwapEffects(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
    {
        var msg = "";

        // Swap outs
        // A poke is force-swapped out before activating red-card
        if (Effect is 29 or 314)
        {
            var swaps = defender.Owner.ValidSwaps(attacker, battle, false);
            if (swaps.Count == 0)
            {
                // Do nothing
            }
            else if (defender.Ability(attacker, this) == Ability.SUCTION_CUPS)
            {
                msg += $"{defender.Name}'s suction cups kept it in place!\n";
            }
            else if (defender.Ability(attacker, this) == Ability.GUARD_DOG)
            {
                msg += $"{defender.Name}'s guard dog kept it in place!\n";
            }
            else if (defender.Ingrain)
            {
                msg += $"{defender.Name} is ingrained in the ground!\n";
            }
            else
            {
                msg += $"{defender.Name} fled in fear!\n";
                msg += defender.Remove(battle);
                var idx = swaps[new Random().Next(swaps.Count)];
                defender.Owner.SwitchPoke(idx, true);
                msg += defender.Owner.CurrentPokemon.SendOut(attacker, battle);
                // Safety in case the poke dies on send out.
                if (defender.Owner.CurrentPokemon != null) defender.Owner.CurrentPokemon.HasMoved = true;
            }
        }
        // A red-card forces the attacker to swap to a random poke, even if they used a switch out move
        else if (defender.HeldItem == "red-card" && defender.Hp > 0 && DamageClass != DamageClass.STATUS)
        {
            var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
            if (swaps.Count == 0)
            {
                // Do nothing
            }
            else if (attacker.Ability(defender, this) == Ability.SUCTION_CUPS)
            {
                msg += $"{attacker.Name}'s suction cups kept it in place from {defender.Name}'s red card!\n";
                defender.HeldItem.Use();
            }
            else if (attacker.Ability(defender, this) == Ability.GUARD_DOG)
            {
                msg += $"{attacker.Name}'s guard dog kept it in place from {defender.Name}'s red card!\n";
                defender.HeldItem.Use();
            }
            else if (attacker.Ingrain)
            {
                msg += $"{attacker.Name} is ingrained in the ground from {defender.Name}'s red card!\n";
                defender.HeldItem.Use();
            }
            else
            {
                msg += $"{defender.Name} held up its red card against {attacker.Name}!\n";
                defender.HeldItem.Use();
                msg += attacker.Remove(battle);
                var idx = swaps[new Random().Next(swaps.Count)];
                attacker.Owner.SwitchPoke(idx, true);
                msg += attacker.Owner.CurrentPokemon.SendOut(defender, battle);
                // Safety in case the poke dies on send out.
                if (attacker.Owner.CurrentPokemon != null) attacker.Owner.CurrentPokemon.HasMoved = true;
            }
        }
        else if (new[] { 128, 154, 229, 347 }.Contains(Effect))
        {
            var swaps = attacker.Owner.ValidSwaps(defender, battle, false);
            if (swaps.Count > 0)
            {
                msg += $"{attacker.Name} went back!\n";
                if (Effect == 128) attacker.Owner.BatonPass = new BatonPass(attacker);

                msg += attacker.Remove(battle);
                // Force this pokemon to immediately return to be attacked
                attacker.Owner.MidTurnRemove = true;
            }
        }

        // Trapping
        if (new[] { 107, 374, 385, 449, 452 }.Contains(Effect) && !defender.Trapping)
        {
            defender.Trapping = true;
            msg += $"{defender.Name} can't escape!\n";
        }

        if (Effect == 449 && !attacker.Trapping)
        {
            attacker.Trapping = true;
            msg += $"{attacker.Name} can't escape!\n";
        }

        // Attacker faints
        if (new[] { 169, 221, 271, 321 }.Contains(Effect)) msg += attacker.Faint(battle);

        return msg;
    }

    /// <summary>
    ///     Handles life orb damage.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string HandleLifeOrb(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
    {
        var msg = "";

        // Life orb
        if (
            attacker.HeldItem == "life-orb" &&
            defender.Owner.HasAlivePokemon() &&
            DamageClass != DamageClass.STATUS &&
            (attacker.Ability() != Ability.SHEER_FORCE || EffectChance == null) &&
            Effect != 149
        )
            msg += attacker.Damage(attacker.StartingHp / 10, battle, source: "its life orb");

        return msg;
    }
}