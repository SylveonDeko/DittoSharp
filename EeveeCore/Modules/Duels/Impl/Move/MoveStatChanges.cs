// File: MoveStatChanges.cs

namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Applies stat changes from moves.
    /// </summary>
    /// <returns>A string of formatted results.</returns>
    private string ApplyStatChanges(DuelPokemon attacker, DuelPokemon defender, Battle battle, int? effectChance)
    {
        var msg = "";

        // Stage changes
        // +1
        if (Effect is 11 or 209 or 213 or 278 or 313 or 323 or 328 or 392 or 414 or 427 or 468 or 472 or 487)
            msg += attacker.AppendAttack(1, attacker, this);

        if (Effect is 12 or 157 or 161 or 207 or 209 or 323 or 367 or 414 or 427 or 467 or 468 or 472)
            msg += attacker.AppendDefense(1, attacker, this);

        if (Effect is 14 or 212 or 291 or 328 or 392 or 414 or 427 or 472)
            msg += attacker.AppendSpAtk(1, attacker, this);

        if (Effect is 161 or 175 or 207 or 212 or 291 or 367 or 414 or 427 or 472)
            msg += attacker.AppendSpDef(1, attacker, this);

        switch (Effect)
        {
            case 130 or 213 or 291 or 296 or 414 or 427 or 442 or 468 or 469 or 487:
                msg += attacker.AppendSpeed(1, attacker, this);
                break;
            case 17 or 467:
                msg += attacker.AppendEvasion(1, attacker, this);
                break;
            case 278 or 323:
                msg += attacker.AppendAccuracy(1, attacker, this);
                break;
            case 139 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(1, attacker, this);

                break;
            }
            case 140 or 375 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendAttack(1, attacker, this);

                break;
            }
            case 277 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpAtk(1, attacker, this);

                break;
            }
            case 433 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendSpeed(1, attacker, this);

                break;
            }
            case 167:
                msg += defender.AppendSpAtk(1, attacker, this);
                break;
            // +2
            case 51 or 309:
                msg += attacker.AppendAttack(2, attacker, this);
                break;
            case 52 or 453:
                msg += attacker.AppendDefense(2, attacker, this);
                break;
        }

        if (Effect is 53 or 285 or 309 or 313 or 366) msg += attacker.AppendSpeed(2, attacker, this);

        if (Effect is 54 or 309 or 366) msg += attacker.AppendSpAtk(2, attacker, this);

        switch (Effect)
        {
            case 55 or 366:
                msg += attacker.AppendSpDef(2, attacker, this);
                break;
            case 109:
                msg += attacker.AppendEvasion(2, attacker, this);
                break;
            case 119 or 432 or 483:
                msg += defender.AppendAttack(2, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 432:
                msg += defender.AppendSpAtk(2, attacker, this);
                break;
            case 359 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += attacker.AppendDefense(2, attacker, this);

                break;
            }
            // -1
            case 19 or 206 or 344 or 347 or 357 or 365 or 388 or 412:
                msg += defender.AppendAttack(-1, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 20 or 206:
                msg += defender.AppendDefense(-1, attacker, this);
                break;
            case 344 or 347 or 358 or 412:
                msg += defender.AppendSpAtk(-1, attacker, this);
                break;
            case 428:
                msg += defender.AppendSpDef(-1, attacker, this);
                break;
            case 331 or 390:
                msg += defender.AppendSpeed(-1, attacker, this);
                break;
            case 24:
                msg += defender.AppendAccuracy(-1, attacker, this);
                break;
            case 25 or 259:
                msg += defender.AppendEvasion(-1, attacker, this);
                break;
            case 69 or 396 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendAttack(-1, attacker, this);

                break;
            }
            case 70 or 397 or 435 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendDefense(-1, attacker, this);

                break;
            }
            case 475:
            {
                // This one has two different chance percents, one has to be hardcoded
                if (new Random().Next(1, 101) <= 50) msg += defender.AppendDefense(-1, attacker, this);

                break;
            }
            case 21 or 71 or 357 or 477 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpeed(-1, attacker, this);

                break;
            }
            case 72 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpAtk(-1, attacker, this);

                break;
            }
            case 73 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-1, attacker, this);

                break;
            }
            case 74 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendAccuracy(-1, attacker, this);

                break;
            }
            case 183:
                msg += attacker.AppendAttack(-1, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 183 or 230 or 309 or 335 or 405 or 438 or 442:
                msg += attacker.AppendDefense(-1, attacker, this);
                break;
            case 480:
                msg += attacker.AppendSpAtk(-1, attacker, this);
                break;
        }

        if (Effect is 230 or 309 or 335) msg += attacker.AppendSpDef(-1, attacker, this);

        switch (Effect)
        {
            case 219 or 335:
                msg += attacker.AppendSpeed(-1, attacker, this);
                break;
            // -2
            case 59 or 169:
                msg += defender.AppendAttack(-2, attacker, this);
                break;
            case 60 or 483:
                msg += defender.AppendDefense(-2, attacker, this);
                break;
            case 61:
                msg += defender.AppendSpeed(-2, attacker, this);
                break;
        }

        switch (Effect)
        {
            case 62 or 169 or 266:
                msg += defender.AppendSpAtk(-2, attacker, this);
                break;
            case 63:
                msg += defender.AppendSpDef(-2, attacker, this);
                break;
            case 272 or 297 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance) msg += defender.AppendSpDef(-2, attacker, this);

                break;
            }
            case 205:
                msg += attacker.AppendSpAtk(-2, attacker, this);
                break;
            case 479:
                msg += attacker.AppendSpeed(-2, attacker, this);
                break;
            // Other
            case 26:
                attacker.AttackStage = 0;
                attacker.DefenseStage = 0;
                attacker.SpAtkStage = 0;
                attacker.SpDefStage = 0;
                attacker.SpeedStage = 0;
                attacker.AccuracyStage = 0;
                attacker.EvasionStage = 0;
                defender.AttackStage = 0;
                defender.DefenseStage = 0;
                defender.SpAtkStage = 0;
                defender.SpDefStage = 0;
                defender.SpeedStage = 0;
                defender.AccuracyStage = 0;
                defender.EvasionStage = 0;
                msg += "All pokemon had their stat stages reset!\n";
                break;
            case 305:
                defender.AttackStage = 0;
                defender.DefenseStage = 0;
                defender.SpAtkStage = 0;
                defender.SpDefStage = 0;
                defender.SpeedStage = 0;
                defender.AccuracyStage = 0;
                defender.EvasionStage = 0;
                msg += $"{defender.Name} had their stat stages reset!\n";
                break;
            case 141 when effectChance.HasValue:
            {
                if (new Random().Next(1, 101) <= effectChance)
                {
                    msg += attacker.AppendAttack(1, attacker, this);
                    msg += attacker.AppendDefense(1, attacker, this);
                    msg += attacker.AppendSpAtk(1, attacker, this);
                    msg += attacker.AppendSpDef(1, attacker, this);
                    msg += attacker.AppendSpeed(1, attacker, this);
                }

                break;
            }
            case 143:
                msg += attacker.Damage(attacker.StartingHp / 2, battle);
                msg += attacker.AppendAttack(12, attacker, this);
                break;
            case 317:
            {
                var amount = 1;
                if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get())) amount = 2;

                msg += attacker.AppendAttack(amount, attacker, this);
                msg += attacker.AppendSpAtk(amount, attacker, this);
                break;
            }
            case 364 when defender.NonVolatileEffect.Poison():
                msg += defender.AppendAttack(-1, attacker, this);
                msg += defender.AppendSpAtk(-1, attacker, this);
                msg += defender.AppendSpeed(-1, attacker, this);
                break;
            case 329:
                msg += attacker.AppendDefense(3, attacker, this);
                break;
            case 322:
                msg += attacker.AppendSpAtk(3, attacker, this);
                break;
            case 227:
            {
                var validStats = new List<Func<int, DuelPokemon, Move, string, bool, string>>();

                if (attacker.AttackStage < 6)
                    validStats.Add(attacker.AppendAttack);
                if (attacker.DefenseStage < 6)
                    validStats.Add(attacker.AppendDefense);
                if (attacker.SpAtkStage < 6)
                    validStats.Add(attacker.AppendSpAtk);
                if (attacker.SpDefStage < 6)
                    validStats.Add(attacker.AppendSpDef);
                if (attacker.SpeedStage < 6)
                    validStats.Add(attacker.AppendSpeed);
                if (attacker.EvasionStage < 6)
                    validStats.Add(attacker.AppendEvasion);
                if (attacker.AccuracyStage < 6)
                    validStats.Add(attacker.AppendAccuracy);

                if (validStats.Count > 0)
                {
                    var statRaiseFunc =
                        validStats[new Random().Next(validStats.Count)];
                    msg += statRaiseFunc(2, attacker, this, "", false);
                }
                else
                {
                    msg += $"None of {attacker.Name}'s stats can go any higher!\n";
                }

                break;
            }
            case 473:
            {
                var rawAtk = attacker.GetRawAttack() + attacker.GetRawSpAtk();
                var rawDef = attacker.GetRawDefense() + attacker.GetRawSpDef();
                if (rawAtk > rawDef)
                {
                    msg += attacker.AppendAttack(1, attacker, this);
                    msg += attacker.AppendSpAtk(1, attacker, this);
                }
                else
                {
                    msg += attacker.AppendDefense(1, attacker, this);
                    msg += attacker.AppendSpDef(1, attacker, this);
                }

                break;
            }
            case 485:
                msg += attacker.Damage(attacker.StartingHp / 2, battle);
                msg += attacker.AppendAttack(2, attacker, this);
                msg += attacker.AppendSpAtk(2, attacker, this);
                msg += attacker.AppendSpeed(2, attacker, this);
                break;
        }

        return msg;
    }
}