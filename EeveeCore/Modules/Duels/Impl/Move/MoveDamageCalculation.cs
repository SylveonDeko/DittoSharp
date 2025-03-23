namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Calculates and applies damage from the move.
    /// </summary>
    /// <returns>A string of formatted results and the number of hits.</returns>
    private string CalculateDamage(DuelPokemon attacker, DuelPokemon defender, Battle battle, ElementType currentType, ref int numHits)
    {
        var msg = "";

        var i = 0;
        // Turn 1 hit moves
        if (Effect == 81 && attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn == 0)
            {
                (var msgadd, i) = Attack(attacker, defender, battle);
                msg += msgadd;
            }
        }
        // Turn 2 hit moves
        else if (new[] { 40, 76, 146, 152, 156, 256, 257, 264, 273, 332, 333, 366, 451, 502 }.Contains(Effect) &&
                 attacker.LockedMove != null)
        {
            if (attacker.LockedMove.Turn == 1)
                if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                {
                    (var msgadd, i) = Attack(attacker, defender, battle);
                    msg += msgadd;
                }
        }
        else
        {
            switch (Effect)
            {
                // Turn 3 hit moves
                case 27:
                {
                    if (attacker.LockedMove.Turn == 2)
                    {
                        msg += defender.Damage(attacker.Bide.Value * 2, battle, this, currentType,
                            attacker);
                        attacker.Bide = null;
                        i = 1;
                    }

                    break;
                }
                // Counter attack moves
                case 228:
                    msg += defender.Damage((int)(1.5 * attacker.LastMoveDamage.Item1), battle, this,
                        currentType, attacker);
                    i = 1;
                    break;
                case 145:
                    msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                case 90:
                    msg += defender.Damage(2 * attacker.LastMoveDamage.Item1, battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                // Static-damage moves
                case 41:
                    msg += defender.Damage(defender.Hp / 2, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 42:
                    msg += defender.Damage(40, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 88:
                    msg += defender.Damage(attacker.Level, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 89:
                {
                    // 0.5-1.5, increments of .1
                    var scale = new Random().Next(0, 11) / 10.0 + 0.5;
                    msg += defender.Damage((int)(attacker.Level * scale), battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                }
                case 131:
                    msg += defender.Damage(20, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 190:
                    msg += defender.Damage(Math.Max(0, defender.Hp - attacker.Hp), battle, this,
                        currentType, attacker);
                    i = 1;
                    break;
                case 39:
                    msg += defender.Damage(defender.Hp, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 321:
                    msg += defender.Damage(attacker.Hp, battle, this, currentType, attacker);
                    i = 1;
                    break;
                case 413:
                    msg += defender.Damage(3 * (defender.Hp / 4), battle, this, currentType,
                        attacker);
                    i = 1;
                    break;
                // Beat up, a stupid move
                case 155:
                {
                    foreach (var poke in attacker.Owner.Party)
                    {
                        if (defender.Hp == 0) break;

                        if (poke.Hp == 0) continue;

                        if (poke == attacker)
                        {
                            var (msgadd, nh) = Attack(attacker, defender, battle);
                            msg += msgadd;
                            i += nh;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(poke.NonVolatileEffect.Current)) continue;

                            var moveData = new Dictionary<string, object>
                            {
                                ["id"] = 251,
                                ["identifier"] = "beat-up",
                                ["power"] = poke.GetRawAttack() / 10 + 5,
                                ["pp"] = 100,
                                ["accuracy"] = 100,
                                ["priority"] = 0,
                                ["type_id"] = (int)ElementType.DARK,
                                ["damage_class_id"] = (int)DamageClass.PHYSICAL,
                                ["effect_id"] = 1,
                                ["effect_chance"] = null,
                                ["target_id"] = 10,
                                ["crit_rate"] = 0,
                                ["min_hits"] = null,
                                ["max_hits"] = null
                            };
                            var fakeMove = new Move(moveData);
                            var (msgadd, nh) = fakeMove.Attack(attacker, defender, battle);
                            msg += msgadd;
                            i += nh;
                        }
                    }

                    break;
                }
                // Other damaging moves
                default:
                {
                    if (DamageClass is DamageClass.PHYSICAL or DamageClass.SPECIAL)
                    {
                        (var msgadd, i) = Attack(attacker, defender, battle);
                        msg += msgadd;
                    }

                    break;
                }
            }
        }

        numHits = i;
        return msg;
    }
}