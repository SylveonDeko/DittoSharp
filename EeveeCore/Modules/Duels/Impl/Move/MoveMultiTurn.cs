// File: MoveMultiTurn.cs

namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Sets up multi-turn moves.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string SetupMultiTurnMoves(DuelPokemon attacker, DuelPokemon defender, Battle battle)
    {
        var msg = "";

        // Setup for multi-turn moves
        if (attacker.LockedMove == null)
        {
            switch (Effect)
            {
                // 2 turn moves
                // During sun, this move does not need to charge
                case 152 when !new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()):
                    attacker.LockedMove = new LockedMove(this, 2);
                    break;
                // During rain, this move does not need to charge
                case 502:
                {
                    if (!new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()))
                        attacker.LockedMove = new LockedMove(this, 2);
                    else
                        // If this move isn't charging, the spatk increase has to happen manually
                        msg += attacker.AppendSpAtk(1, attacker, this);

                    break;
                }
            }

            if (new[] { 40, 76, 81, 146, 156, 256, 257, 264, 273, 332, 333, 366, 451 }.Contains(Effect))
                attacker.LockedMove = new LockedMove(this, 2);

            switch (Effect)
            {
                // 3 turn moves
                case 27:
                    attacker.LockedMove = new LockedMove(this, 3);
                    attacker.Bide = 0;
                    break;
                case 160:
                {
                    attacker.LockedMove = new LockedMove(this, 3);
                    attacker.Uproar.SetTurns(3);
                    if (attacker.NonVolatileEffect.Sleep())
                    {
                        attacker.NonVolatileEffect.Reset();
                        msg += $"{attacker.Name} woke up!\n";
                    }

                    if (defender.NonVolatileEffect.Sleep())
                    {
                        defender.NonVolatileEffect.Reset();
                        msg += $"{defender.Name} woke up!\n";
                    }

                    break;
                }
                // 5 turn moves
                case 118:
                    attacker.LockedMove = new LockedMove(this, 5);
                    break;
                // 2-3 turn moves
                case 28:
                    attacker.LockedMove = new LockedMove(this, new Random().Next(2, 4));
                    break;
            }

            switch (Effect)
            {
                // 2-5 turn moves
                case 160:
                    attacker.LockedMove = new LockedMove(this, new Random().Next(2, 6));
                    break;
                // Semi-invulnerable
                case 256:
                    attacker.Dive = true;
                    break;
                case 257:
                    attacker.Dig = true;
                    break;
                case 156 or 264:
                    attacker.Fly = true;
                    break;
                case 273:
                    attacker.ShadowForce = true;
                    break;
            }
        }

        // Early exits for moves that hit a certain turn when it is not that turn
        // Turn 1 hit moves
        if (Effect == 81 && attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn != 0)
            {
                msg += "It's recharging!\n";
                return msg;
            }
        }
        // Turn 2 hit moves
        else if (new[] { 40, 76, 146, 152, 156, 256, 257, 264, 273, 332, 333, 366, 451, 502 }.Contains(Effect) &&
                 attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn != 1)
            {
                switch (Effect)
                {
                    case 146:
                        msg += attacker.AppendDefense(1, attacker, this);
                        break;
                    case 451 or 502:
                        msg += attacker.AppendSpAtk(1, attacker, this);
                        break;
                    default:
                    {
                        msg += "It's charging up!\n";
                        // Gulp Missile
                        if (Effect == 256 && attacker.Ability() == Ability.GULP_MISSILE &&
                            attacker.Name == "Cramorant")
                        {
                            if (attacker.Hp > attacker.StartingHp / 2)
                            {
                                if (attacker.Form("Cramorant-gulping"))
                                    msg += $"{attacker.Name} gulped up an arrokuda!\n";
                            }
                            else
                            {
                                if (attacker.Form("Cramorant-gorging"))
                                    msg += $"{attacker.Name} gulped up a pikachu!\n";
                            }
                        }

                        break;
                    }
                }

                return msg;
            }
        }
        // Turn 3 hit moves
        else if (Effect == 27)
        {
            if (attacker.LockedMove.Turn != 2)
            {
                msg += "It's storing energy!\n";
                return msg;
            }
        }

        return msg;
    }
}