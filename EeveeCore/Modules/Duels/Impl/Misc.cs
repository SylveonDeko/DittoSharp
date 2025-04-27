namespace EeveeCore.Modules.Duels.Impl;

/// <summary>
///     Represents an effect that lasts for a specific number of turns in battle.
///     Used as a base class for various time-limited battle effects such as status conditions,
///     weather effects, and move-induced states.
/// </summary>
public class ExpiringEffect(int? turnsToExpire)
{
    /// <summary>
    ///     Gets or sets the number of turns remaining before this effect expires.
    ///     A null value indicates the effect never expires unless manually removed.
    /// </summary>
    public int? RemainingTurns = turnsToExpire;

    /// <summary>
    ///     Determines whether this effect is currently active.
    /// </summary>
    /// <returns>
    ///     True if the effect is active (either has remaining turns or is permanent),
    ///     False if the effect has expired (remaining turns is 0 or less).
    /// </returns>
    public bool Active()
    {
        if (RemainingTurns == null)
            return true;
        return RemainingTurns > 0;
    }

    /// <summary>
    ///     Progresses this effect by one turn, decrementing the remaining turns counter.
    /// </summary>
    /// <returns>
    ///     True if the effect just expired this turn (went from active to inactive),
    ///     False if the effect remains active or was already inactive.
    /// </returns>
    public bool NextTurn()
    {
        if (RemainingTurns == null)
            return false;
        if (!Active()) return false;
        RemainingTurns--;
        return !Active();
    }

    /// <summary>
    ///     Sets or resets the duration of this effect.
    /// </summary>
    /// <param name="turnsToExpire">
    ///     The new number of turns until this effect expires.
    ///     Null makes the effect permanent until manually removed.
    /// </param>
    public void SetTurns(int? turnsToExpire)
    {
        RemainingTurns = turnsToExpire;
    }
}

/// <summary>
///     Represents the current weather condition in a battle.
///     Manages various weather effects like rain, sunshine, hail, and sandstorm,
///     along with their impacts on Pokémon abilities and form changes.
/// </summary>
public class Weather(Battle battle) : ExpiringEffect(0)
{
    /// <summary>
    ///     Gets or sets the current weather type as a string identifier.
    ///     Valid values include: "hail", "sandstorm", "rain", "sun", "h-rain", "h-sun", and "h-wind",
    ///     where the "h-" prefix indicates "heavy" or extreme versions of these weather conditions.
    /// </summary>
    public string? WeatherType = "";

    /// <summary>
    ///     Clears the current weather and updates Pokémon forms that depend on weather.
    ///     Primarily affects Castform's form which changes based on weather conditions.
    /// </summary>
    private void _ExpireWeather()
    {
        WeatherType = "";
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
        {
            if (poke == null)
                continue;
            // Forecast
            if (poke.Ability() != Ability.FORECAST || !poke.Name.StartsWith("Castform") ||
                poke.Name == "Castform") continue;
            if (poke.Form("Castform")) poke.TypeIds = [ElementType.NORMAL];
        }
    }

    /// <summary>
    ///     Progresses the weather condition by one turn, potentially clearing it if expired.
    /// </summary>
    /// <returns>
    ///     True if the weather condition expired this turn, False otherwise.
    /// </returns>
    public new bool NextTurn()
    {
        if (!base.NextTurn()) return false;
        _ExpireWeather();
        return true;
    }

    /// <summary>
    ///     Checks if extreme weather effects from abilities (Desolate Land, Primordial Sea, Delta Stream)
    ///     should be maintained based on the Pokémon currently in battle.
    /// </summary>
    /// <returns>
    ///     True if extreme weather was cleared due to no supporting ability being present,
    ///     False if weather remains unchanged.
    /// </returns>
    public bool RecheckAbilityWeather()
    {
        var maintainWeather = false;
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
        {
            if (poke == null)
                continue;
            switch (WeatherType)
            {
                case "h-wind" when poke.Ability() == Ability.DELTA_STREAM:
                case "h-sun" when poke.Ability() == Ability.DESOLATE_LAND:
                case "h-rain" when poke.Ability() == Ability.PRIMORDIAL_SEA:
                    maintainWeather = true;
                    break;
            }
        }

        if (new[] { "h-wind", "h-sun", "h-rain" }.Contains(WeatherType) && !maintainWeather)
        {
            _ExpireWeather();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets the current effective weather type, accounting for abilities that negate weather.
    /// </summary>
    /// <returns>
    ///     The current weather type as a string, or an empty string if weather is being suppressed
    ///     by abilities like Cloud Nine or Air Lock.
    /// </returns>
    public string? Get()
    {
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
        {
            if (poke == null)
                continue;
            if (poke.Ability() == Ability.CLOUD_NINE || poke.Ability() == Ability.AIR_LOCK)
                return "";
        }

        return WeatherType;
    }

    /// <summary>
    ///     Sets a new weather condition, applying appropriate duration and effects.
    ///     Handles form changes for Pokémon like Castform that react to weather.
    /// </summary>
    /// <param name="weather">The weather type to set.</param>
    /// <param name="pokemon">The Pokémon initiating the weather change.</param>
    /// <returns>A formatted message describing the weather change and its effects.</returns>
    public string Set(string weather, DuelPokemon pokemon)
    {
        var msg = "";
        int? turns = null;
        ElementType? element = null;
        string? castform = null;

        if (WeatherType == weather)
            return "";

        switch (weather)
        {
            case "hail":
                if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(WeatherType))
                    return "";
                turns = pokemon.HeldItem == "icy-rock" ? 8 : 5;
                msg += "It starts to hail!\n";
                element = ElementType.ICE;
                castform = "Castform-snowy";
                break;
            case "sandstorm":
                if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(WeatherType))
                    return "";
                turns = pokemon.HeldItem == "smooth-rock" ? 8 : 5;
                msg += "A sandstorm is brewing up!\n";
                element = ElementType.NORMAL;
                castform = "Castform";
                break;
            case "rain":
                if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(WeatherType))
                    return "";
                turns = pokemon.HeldItem == "damp-rock" ? 8 : 5;
                msg += "It starts to rain!\n";
                element = ElementType.WATER;
                castform = "Castform-rainy";
                break;
            case "sun":
                if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(WeatherType))
                    return "";
                turns = pokemon.HeldItem == "heat-rock" ? 8 : 5;
                msg += "The sunlight is strong!\n";
                element = ElementType.FIRE;
                castform = "Castform-sunny";
                break;
            case "h-rain":
                msg += "Heavy rain begins to fall!\n";
                element = ElementType.WATER;
                castform = "Castform-rainy";
                break;
            case "h-sun":
                msg += "The sunlight is extremely harsh!\n";
                element = ElementType.FIRE;
                castform = "Castform-sunny";
                break;
            case "h-wind":
                msg += "The winds are extremely strong!\n";
                element = ElementType.NORMAL;
                castform = "Castform";
                break;
            default:
                throw new ArgumentException("unexpected weather");
        }

        // Forecast
        var t = element.ToString().ToLower();
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
        {
            if (poke == null)
                continue;
            if (poke.Ability() == Ability.FORECAST && poke.Name != castform)
                if (poke.Form(castform))
                {
                    poke.TypeIds = [element.Value];
                    msg += $"{poke.Name} transformed into a {t} type using its forecast!\n";
                }
        }

        WeatherType = weather;
        SetTurns(turns);
        return msg;
    }
}

/// <summary>
///     Represents a multi-turn move that a Pokémon is locked into using for several consecutive turns.
///     Examples include Outrage, Petal Dance, Thrash, and charging moves like Fly and Dig.
/// </summary>
public class LockedMove(Move.Move move, int turnsToExpire) : ExpiringEffect(turnsToExpire)
{
    /// <summary>
    ///     Gets the Move object that the Pokémon is locked into using.
    /// </summary>
    public Move.Move Move { get; private set; } = move;

    /// <summary>
    ///     Gets or sets the current turn number within the locked move sequence.
    ///     Starts at 0 and increments with each use of the move.
    /// </summary>
    public int Turn { get; private set; }

    /// <summary>
    ///     Progresses the locked move state by one turn.
    /// </summary>
    /// <returns>
    ///     True if the Pokémon is no longer locked into the move after this turn,
    ///     False if the Pokémon must continue using the move next turn.
    /// </returns>
    public new bool NextTurn()
    {
        var expired = base.NextTurn();
        Turn++;
        return expired;
    }

    /// <summary>
    ///     Determines if this is the final turn of the locked move sequence.
    /// </summary>
    /// <returns>
    ///     True if there is exactly one turn remaining, indicating this is the last turn
    ///     the Pokémon will be forced to use this move.
    /// </returns>
    public bool IsLastTurn()
    {
        return RemainingTurns == 1;
    }
}

/// <summary>
///     Represents a time-limited effect that also stores an associated item or object.
///     Used for effects like Disable, Encore, and Mind Reader that track both
///     duration and a specific move or Pokémon.
/// </summary>
public class ExpiringItem() : ExpiringEffect(0)
{
    /// <summary>
    ///     Gets or sets the object associated with this expiring effect.
    ///     This can be a move, a Pokémon, or any other reference type depending on the context.
    /// </summary>
    public object Item { get; private set; }

    /// <summary>
    ///     Progresses the effect by one turn, clearing the item if expired.
    /// </summary>
    /// <returns>
    ///     True if the effect expired this turn, False otherwise.
    /// </returns>
    public new bool NextTurn()
    {
        var expired = base.NextTurn();
        if (expired) Item = null;
        return expired;
    }

    /// <summary>
    ///     Sets both the associated item and duration of this effect.
    /// </summary>
    /// <param name="item">The object to associate with this effect.</param>
    /// <param name="turns">The number of turns until this effect expires.</param>
    public void Set(object item, int turns)
    {
        Item = item;
        SetTurns(turns);
    }

    /// <summary>
    ///     Immediately ends this effect, clearing both the item and duration.
    /// </summary>
    public void End()
    {
        Item = null;
        SetTurns(0);
    }
}

/// <summary>
///     Represents the terrain condition of the battlefield in battle.
///     Manages terrain effects like Electric, Grassy, Misty, and Psychic terrain
///     and their impacts on Pokémon, moves, and items.
/// </summary>
public class Terrain(Battle battle) : ExpiringItem
{
    /// <summary>
    ///     Progresses the terrain effect by one turn, potentially clearing it if expired.
    /// </summary>
    /// <returns>
    ///     True if the terrain expired this turn, False otherwise.
    /// </returns>
    public new bool NextTurn()
    {
        var expired = base.NextTurn();
        if (expired) End();
        return expired;
    }

    /// <summary>
    ///     Sets a new terrain effect, applying appropriate duration and handling Pokémon
    ///     abilities and items that interact with terrain.
    /// </summary>
    /// <param name="item">The terrain type to set ("electric", "grassy", "misty", or "psychic").</param>
    /// <param name="attacker">The Pokémon creating the terrain.</param>
    /// <returns>A formatted message describing the terrain change and its effects.</returns>
    public string Set(string item, DuelPokemon attacker)
    {
        if (item == Item?.ToString())
            return $"There's already a {item} terrain!\n";

        var turns = attacker.HeldItem == "terrain-extender" ? 8 : 5;
        base.Set(item, turns);
        var msg = $"{attacker.Name} creates a{(item == "electric" ? "n" : "")} {item} terrain!\n";

        // Mimicry
        ElementType? element = null;
        switch (item)
        {
            case "electric":
                element = ElementType.ELECTRIC;
                break;
            case "grassy":
                element = ElementType.GRASS;
                break;
            case "misty":
                element = ElementType.FAIRY;
                break;
            case "psychic":
                element = ElementType.PSYCHIC;
                break;
        }

        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
        {
            if (poke == null)
                continue;
            if (poke.Ability() == Ability.MIMICRY)
            {
                poke.TypeIds = [element.Value];
                var t = element.Value.ToString().ToLower();
                msg += $"{poke.Name} became a {t} type using its mimicry!\n";
            }

            if (poke.HeldItem == "electric-seed" && item == "electric")
            {
                msg += poke.AppendDefense(1, poke, source: "its electric seed");
                poke.HeldItem.Use();
            }

            if (poke.HeldItem == "psychic-seed" && item == "psychic")
            {
                msg += poke.AppendSpDef(1, poke, source: "its psychic seed");
                poke.HeldItem.Use();
            }

            if (poke.HeldItem == "misty-seed" && item == "misty")
            {
                msg += poke.AppendSpDef(1, poke, source: "its misty seed");
                poke.HeldItem.Use();
            }

            if (poke.HeldItem == "grassy-seed" && item == "grassy")
            {
                msg += poke.AppendDefense(1, poke, source: "its grassy seed");
                poke.HeldItem.Use();
            }
        }

        return msg;
    }

    /// <summary>
    ///     Ends the current terrain effect and reverts Pokémon with the Mimicry ability
    ///     back to their original types.
    /// </summary>
    public new void End()
    {
        base.End();
        // Mimicry
        foreach (var poke in new[] { battle.Trainer1.CurrentPokemon, battle.Trainer2.CurrentPokemon })
        {
            if (poke == null)
                continue;
            if (poke.Ability() == Ability.MIMICRY) poke.TypeIds = [..poke.StartingTypeIds];
        }
    }
}

/// <summary>
///     Represents the delayed healing effect of the move Wish.
///     Tracks both the turn count until healing occurs and the amount of HP to restore.
/// </summary>
public class ExpiringWish() : ExpiringEffect(0)
{
    /// <summary>
    ///     Gets or sets the amount of HP to restore when the Wish activates.
    ///     Null when no Wish is active.
    /// </summary>
    public int? Hp { get; private set; }

    /// <summary>
    ///     Progresses the Wish effect by one turn, returning the healing amount if it activates.
    /// </summary>
    /// <returns>
    ///     The amount of HP to restore if the Wish activates this turn, or 0 if not.
    /// </returns>
    public int NextTurn()
    {
        var expired = base.NextTurn();
        var hp = 0;
        if (expired)
        {
            hp = Hp ?? 0;
            Hp = null;
        }

        return hp;
    }

    /// <summary>
    ///     Sets up a new Wish with the specified healing amount.
    /// </summary>
    /// <param name="hp">The amount of HP to restore when the Wish activates.</param>
    public void Set(int hp)
    {
        Hp = hp;
        SetTurns(2);
    }
}

/// <summary>
///     Represents a Pokémon's non-volatile status condition (burn, poison, paralysis, sleep, or freeze).
///     Manages application, removal, and turn-by-turn effects of status conditions.
/// </summary>
public class NonVolatileEffect(DuelPokemon pokemon)
{
    /// <summary>
    ///     Gets or sets the current status condition.
    ///     Valid values: "burn", "poison", "b-poison" (badly poisoned), "paralysis", "sleep", "freeze", or "" (none).
    /// </summary>
    public string Current { get; set; } = "";

    /// <summary>
    ///     Gets the timer that tracks the duration of sleep status.
    /// </summary>
    public ExpiringEffect SleepTimer { get; } = new(0);

    /// <summary>
    ///     Gets or sets the number of turns a Pokémon has been badly poisoned.
    ///     Used to calculate increasing damage each turn.
    /// </summary>
    public int BadlyPoisonedTurn { get; set; }

    /// <summary>
    ///     Applies the turn-by-turn effects of the current status condition.
    ///     This includes damage from poison/burn, potential status healing from abilities,
    ///     and nightmare damage during sleep.
    /// </summary>
    /// <param name="battle">The current battle context.</param>
    /// <returns>A formatted message describing the status effects applied this turn.</returns>
    public string NextTurn(Battle battle)
    {
        if (string.IsNullOrEmpty(Current))
            return "";

        if (Current == "b-poison") BadlyPoisonedTurn++;

        if (pokemon.Ability() == Ability.HYDRATION && new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()))
        {
            var removed = Current;
            Reset();
            return $"{pokemon.Name}'s hydration cured its {removed}!\n";
        }

        if (pokemon.Ability() == Ability.SHED_SKIN && new Random().Next(3) == 0)
        {
            var removed = Current;
            Reset();
            return $"{pokemon.Name}'s shed skin cured its {removed}!\n";
        }

        switch (Current)
        {
            // The poke still has a status effect, apply damage
            case "burn":
            {
                var damage = Math.Max(1, pokemon.StartingHp / 16);
                if (pokemon.Ability() == Ability.HEATPROOF) damage /= 2;
                return pokemon.Damage(damage, battle, source: "its burn");
            }
            case "b-poison":
            {
                if (pokemon.Ability() == Ability.POISON_HEAL)
                    return pokemon.Heal(pokemon.StartingHp / 8, "its poison heal");
                var damage = Math.Max(1, pokemon.StartingHp / 16 * Math.Min(15, BadlyPoisonedTurn));
                return pokemon.Damage(damage, battle, source: "its bad poison");
            }
            case "poison":
            {
                if (pokemon.Ability() == Ability.POISON_HEAL)
                    return pokemon.Heal(pokemon.StartingHp / 8, "its poison heal");
                var damage = Math.Max(1, pokemon.StartingHp / 8);
                return pokemon.Damage(damage, battle, source: "its poison");
            }
            case "sleep" when pokemon.Nightmare:
                return pokemon.Damage(pokemon.StartingHp / 4, battle, source: "its nightmare");
            default:
                return "";
        }
    }

    /// <summary>
    ///     Checks if the Pokémon is currently burned.
    /// </summary>
    /// <returns>True if the Pokémon has the burn status condition.</returns>
    public bool Burn()
    {
        return Current == "burn";
    }

    /// <summary>
    ///     Checks if the Pokémon is currently asleep.
    /// </summary>
    /// <returns>
    ///     True if the Pokémon has the sleep status condition or has the Comatose ability.
    /// </returns>
    public bool Sleep()
    {
        if (pokemon.Ability() == Ability.COMATOSE) return true;
        return Current == "sleep";
    }

    /// <summary>
    ///     Checks if the Pokémon is currently poisoned.
    /// </summary>
    /// <returns>True if the Pokémon has either regular or badly poisoned status condition.</returns>
    public bool Poison()
    {
        return Current is "poison" or "b-poison";
    }

    /// <summary>
    ///     Checks if the Pokémon is currently paralyzed.
    /// </summary>
    /// <returns>True if the Pokémon has the paralysis status condition.</returns>
    public bool Paralysis()
    {
        return Current == "paralysis";
    }

    /// <summary>
    ///     Checks if the Pokémon is currently frozen.
    /// </summary>
    /// <returns>True if the Pokémon has the freeze status condition.</returns>
    public bool Freeze()
    {
        return Current == "freeze";
    }

    /// <summary>
    ///     Attempts to apply a status condition to the Pokémon, handling immunities,
    ///     type-based protections, ability-based protections, and other battle conditions
    ///     that might prevent the status from being applied.
    /// </summary>
    /// <param name="status">The status condition to apply.</param>
    /// <param name="battle">The current battle context.</param>
    /// <param name="attacker">The Pokémon inflicting the status, if any.</param>
    /// <param name="move">The move used to inflict the status, if any.</param>
    /// <param name="turns">The number of turns for sleep status, if applicable.</param>
    /// <param name="force">Whether to force the status application, bypassing some protections.</param>
    /// <param name="source">The source of the status condition for message formatting.</param>
    /// <returns>A formatted message describing the result of the status application attempt.</returns>
    public string ApplyStatus(string status, Battle battle, DuelPokemon attacker = null, Move.Move move = null,
        int? turns = null, bool force = false, string source = "")
    {
        var msg = "";
        if (!string.IsNullOrEmpty(source)) source = $" from {source}";

        if (!string.IsNullOrEmpty(Current) && !force)
            return $"{pokemon.Name} already has a status, it can't get {status} too!\n";

        if (pokemon.Ability(attacker, move) == Ability.COMATOSE)
            return $"{pokemon.Name} already has a status, it can't get {status} too!\n";

        if (pokemon.Ability(attacker, move) == Ability.PURIFYING_SALT)
            return $"{pokemon.Name}'s purifying salt protects it from being inflicted with {status}!\n";

        if (pokemon.Ability(attacker, move) == Ability.LEAF_GUARD &&
            new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
            return $"{pokemon.Name}'s leaf guard protects it from being inflicted with {status}!\n";

        if (pokemon.Substitute > 0 && attacker != pokemon && (move == null || move.IsAffectedBySubstitute()))
            return $"{pokemon.Name}'s substitute protects it from being inflicted with {status}!\n";

        if (pokemon.Owner.Safeguard.Active() && attacker != pokemon &&
            (attacker == null || attacker.Ability() != Ability.INFILTRATOR))
            return $"{pokemon.Name}'s safeguard protects it from being inflicted with {status}!\n";

        if (pokemon.Grounded(battle, attacker, move) && battle.Terrain.Item?.ToString() == "misty")
            return $"The misty terrain protects {pokemon.Name} from being inflicted with {status}!\n";

        if (pokemon.Ability(attacker, move) == Ability.FLOWER_VEIL && pokemon.TypeIds.Contains(ElementType.GRASS))
            return $"{pokemon.Name}'s flower veil protects it from being inflicted with {status}!\n";

        if (pokemon.Name == "Minior") return "Minior's hard shell protects it from status effects!\n";

        switch (status)
        {
            case "burn":
                if (pokemon.TypeIds.Contains(ElementType.FIRE))
                    return $"{pokemon.Name} is a fire type and can't be burned!\n";
                if (pokemon.Ability(attacker, move) == Ability.WATER_VEIL ||
                    pokemon.Ability(attacker, move) == Ability.WATER_BUBBLE)
                {
                    var abilityName = ((Ability)pokemon.AbilityId).GetPrettyName();
                    return $"{pokemon.Name}'s {abilityName} prevents it from getting burned!\n";
                }

                Current = status;
                msg += $"{pokemon.Name} was burned{source}!\n";
                break;

            case "sleep":
                var sleepImmunities = new[] { Ability.INSOMNIA, Ability.VITAL_SPIRIT, Ability.SWEET_VEIL };
                if (sleepImmunities.Contains(pokemon.Ability(attacker, move)))
                {
                    var abilityName = ((Ability)pokemon.AbilityId).GetPrettyName();
                    return $"{pokemon.Name}'s {abilityName} keeps it awake!\n";
                }

                if (pokemon.Grounded(battle, attacker, move) && battle.Terrain.Item?.ToString() == "electric")
                    return $"The terrain is too electric for {pokemon.Name} to fall asleep!\n";
                if (battle.Trainer1.CurrentPokemon != null && battle.Trainer1.CurrentPokemon.Uproar.Active())
                    return $"An uproar keeps {pokemon.Name} from falling asleep!\n";
                if (battle.Trainer2.CurrentPokemon != null && battle.Trainer2.CurrentPokemon.Uproar.Active())
                    return $"An uproar keeps {pokemon.Name} from falling asleep!\n";

                if (turns == null) turns = new Random().Next(2, 5);
                if (pokemon.Ability(attacker, move) == Ability.EARLY_BIRD) turns /= 2;
                Current = status;
                SleepTimer.SetTurns(turns);
                msg += $"{pokemon.Name} fell asleep{source}!\n";
                break;

            case "poison":
            case "b-poison":
                if (attacker == null || attacker.Ability() != Ability.CORROSION)
                {
                    if (pokemon.TypeIds.Contains(ElementType.STEEL))
                        return $"{pokemon.Name} is a steel type and can't be poisoned!\n";
                    if (pokemon.TypeIds.Contains(ElementType.POISON))
                        return $"{pokemon.Name} is a poison type and can't be poisoned!\n";
                }

                if (pokemon.Ability(attacker, move) == Ability.IMMUNITY ||
                    pokemon.Ability(attacker, move) == Ability.PASTEL_VEIL)
                {
                    var abilityName = ((Ability)pokemon.AbilityId).GetPrettyName();
                    return $"{pokemon.Name}'s {abilityName} keeps it from being poisoned!\n";
                }

                Current = status;
                var bad = status == "b-poison" ? " badly" : "";
                msg += $"{pokemon.Name} was{bad} poisoned{source}!\n";

                if (move != null && attacker != null && attacker.Ability() == Ability.POISON_PUPPETEER)
                    msg += pokemon.Confuse(attacker, source: $"{attacker.Name}'s poison puppeteer");
                break;

            case "paralysis":
                if (pokemon.TypeIds.Contains(ElementType.ELECTRIC))
                    return $"{pokemon.Name} is an electric type and can't be paralyzed!\n";
                if (pokemon.Ability(attacker, move) == Ability.LIMBER)
                    return $"{pokemon.Name}'s limber keeps it from being paralyzed!\n";
                Current = status;
                msg += $"{pokemon.Name} was paralyzed{source}!\n";
                break;

            case "freeze":
                if (pokemon.TypeIds.Contains(ElementType.ICE))
                    return $"{pokemon.Name} is an ice type and can't be frozen!\n";
                if (pokemon.Ability(attacker, move) == Ability.MAGMA_ARMOR)
                    return $"{pokemon.Name}'s magma armor keeps it from being frozen!\n";
                if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                    return $"It's too sunny to freeze {pokemon.Name}!\n";
                Current = status;
                msg += $"{pokemon.Name} was frozen solid{source}!\n";
                break;
        }

        if (pokemon.Ability(attacker, move) == Ability.SYNCHRONIZE && attacker != null)
            msg += attacker.NonVolatileEffect.ApplyStatus(status, battle, pokemon,
                source: $"{pokemon.Name}'s synchronize");

        if (pokemon.HeldItem.ShouldEatBerryStatus(attacker))
            msg += pokemon.HeldItem.EatBerry(attacker: attacker, move: move);

        return msg;
    }

    /// <summary>
    ///     Removes all non-volatile status conditions from the Pokémon.
    ///     Clears the current status, resets bad poison counter, and removes any nightmare effect.
    /// </summary>
    public void Reset()
    {
        Current = "";
        BadlyPoisonedTurn = 0;
        SleepTimer.SetTurns(0);
        pokemon.Nightmare = false;
    }
}

/// <summary>
///     Tracks consecutive uses of the same move for the Metronome held item,
///     which increases move power with repeated use of the same move.
/// </summary>
public class Metronome
{
    /// <summary>
    ///     Gets or sets the name of the most recently used move.
    /// </summary>
    public string Move { get; private set; } = "";

    /// <summary>
    ///     Gets or sets the number of consecutive times the same move has been used.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    ///     Resets the move tracking when a move fails or a non-move action is taken.
    /// </summary>
    public void Reset()
    {
        Move = "";
        Count = 0;
    }

    /// <summary>
    ///     Updates the tracking based on the most recently used move.
    ///     Increments the counter if the same move is used consecutively.
    /// </summary>
    /// <param name="moveName">The name of the move that was just used.</param>
    public void Use(string moveName)
    {
        if (Move == moveName)
        {
            Count++;
        }
        else
        {
            Move = moveName;
            Count = 1;
        }
    }

    /// <summary>
    ///     Calculates the power multiplier for a move based on consecutive usage.
    ///     The power increases by 20% for each consecutive use, up to a maximum of 2x.
    /// </summary>
    /// <param name="moveName">The name of the move to get the buff for.</param>
    /// <returns>The power multiplier to apply to the move.</returns>
    public double GetBuff(string moveName)
    {
        if (Move != moveName) return 1;
        return Math.Min(2, 1 + 0.2 * Count);
    }
}

/// <summary>
///     Represents a basic item in the game with its core properties.
///     Provides a simplified view of an item's data without battle-specific functionality.
/// </summary>
public class Item(IDictionary<string, object> itemData)
{
    /// <summary>
    ///     Gets the internal identifier name of the item.
    /// </summary>
    public string Name { get; } = (string)itemData["identifier"];

    /// <summary>
    ///     Gets the unique numeric ID of the item in the game database.
    /// </summary>
    public int Id { get; } = Convert.ToInt32(itemData["id"]);

    /// <summary>
    ///     Gets the power of the item when used with the move Fling.
    ///     Null for items that cannot be used with Fling.
    /// </summary>
    public int? Power { get; } = itemData["fling_power"] as int?;

    /// <summary>
    ///     Gets the effect ID that occurs when the item is used with the move Fling.
    ///     Null for items that have no special effect when Flung.
    /// </summary>
    public int? Effect { get; } = itemData["fling_effect_id"] as int?;
}

/// <summary>
///     Manages a Pokémon's held item in battle, handling item effects, usage, and interactions.
///     Provides methods for item manipulation such as consuming, transferring, and swapping items.
/// </summary>
public class HeldItem
{
    private readonly DuelPokemon _owner;
    private Database.Models.Mongo.Pokemon.Item _item;

    /// <summary>
    ///     Initializes a new instance of the HeldItem class for a specific Pokémon.
    /// </summary>
    /// <param name="itemData">The database item data for the held item.</param>
    /// <param name="owner">The Pokémon holding the item.</param>
    public HeldItem(Database.Models.Mongo.Pokemon.Item itemData, DuelPokemon owner)
    {
        _item = itemData;
        _owner = owner;
        EverHadItem = _item != null;
    }

    /// <summary>
    ///     Gets or sets the battle context this held item is being used in.
    /// </summary>
    public Battle Battle { get; set; }

    /// <summary>
    ///     Gets or sets the last item used or consumed by the Pokémon.
    ///     Used for effects that can restore consumed items.
    /// </summary>
    public Database.Models.Mongo.Pokemon.Item? LastUsed { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this Pokémon has ever held an item.
    ///     Used by abilities like Unburden that activate when an item is lost.
    /// </summary>
    public bool EverHadItem { get; set; }

    /// <summary>
    ///     Gets the internal identifier name of the currently held item.
    ///     Null if no item is held.
    /// </summary>
    public string? Name => _item?.Identifier;

    /// <summary>
    ///     Gets the power of the currently held item when used with the move Fling.
    ///     Null if no item is held or the item cannot be used with Fling.
    /// </summary>
    public int? Power => _item?.FlingPower;

    /// <summary>
    ///     Gets the unique numeric ID of the currently held item.
    ///     Null if no item is held.
    /// </summary>
    public int? Id => _item?.ItemId;

    /// <summary>
    ///     Gets the effect ID that occurs when the currently held item is used with Fling.
    ///     Null if no item is held or the item has no special effect when Flung.
    /// </summary>
    public int? Effect => _item?.FlingEffectId;

    /// <summary>
    ///     Gets the effective held item identifier, accounting for battle conditions and abilities
    ///     that might prevent the item from being used.
    /// </summary>
    /// <returns>
    ///     The item identifier if an item is held and usable, or null if no item is held or
    ///     the item cannot be used due to effects like Magic Room, Klutz, Embargo, etc.
    /// </returns>
    public string? Get()
    {
        if (_item == null) return null;
        if (!CanRemove()) return _item.Identifier;
        if (_owner.Embargo.Active()) return null;
        if (Battle != null && Battle.MagicRoom.Active()) return null;
        if (_owner.Ability() == Ability.KLUTZ) return null;
        if (_owner.CorrosiveGas) return null;
        return _item.Identifier;
    }

    /// <summary>
    ///     Checks if the Pokémon currently has an item.
    /// </summary>
    /// <returns>True if an item is held, False otherwise.</returns>
    public bool HasItem()
    {
        return _item != null;
    }

    /// <summary>
    ///     Determines whether the current held item can be removed or replaced.
    ///     Some items like Mega Stones, Z-Crystals, and type-specific items cannot be removed.
    /// </summary>
    /// <returns>
    ///     True if the item can be removed or if no item is held,
    ///     False if the item is of a type that cannot be removed.
    /// </returns>
    public bool CanRemove()
    {
        var nonRemovableItems = new[]
        {
            // Plates
            "draco-plate", "dread-plate", "earth-plate", "fist-plate", "flame-plate", "icicle-plate",
            "insect-plate", "iron-plate", "meadow-plate", "mind-plate", "pixie-plate", "sky-plate",
            "splash-plate", "spooky-plate", "stone-plate", "toxic-plate", "zap-plate",
            // Memories
            "dragon-memory", "dark-memory", "ground-memory", "fighting-memory", "fire-memory",
            "ice-memory", "bug-memory", "steel-memory", "grass-memory", "psychic-memory",
            "fairy-memory", "flying-memory", "water-memory", "ghost-memory", "rock-memory",
            "poison-memory", "electric-memory",
            // Misc
            "primal-orb", "griseous-orb", "blue-orb", "red-orb", "rusty-sword", "rusty-shield",
            // Mega Stones
            "mega-stone", "mega-stone-x", "mega-stone-y"
        };

        return _item == null || !nonRemovableItems.Contains(_item.Identifier);
    }

    /// <summary>
    ///     Checks if the currently held item is a berry.
    /// </summary>
    /// <param name="onlyActive">
    ///     If true, only returns true when the berry is actually usable considering battle conditions.
    ///     If false, simply checks if the item is a berry regardless of usability.
    /// </param>
    /// <returns>True if the held item is a usable berry, False otherwise.</returns>
    public bool IsBerry(bool onlyActive = true)
    {
        if (onlyActive)
        {
            var item = Get();
            return !string.IsNullOrEmpty(item) && item.EndsWith("-berry");
        }

        return !string.IsNullOrEmpty(Name) && Name.EndsWith("-berry");
    }

    /// <summary>
    ///     Removes the currently held item without preserving it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the item cannot be removed.</exception>
    public void Remove()
    {
        if (!CanRemove()) throw new InvalidOperationException($"{Name} cannot be removed.");
        _item = null;
    }

    /// <summary>
    ///     Uses (consumes) the held item, storing it as the last used item for potential recovery.
    ///     Also clears any Choice item move restrictions.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the item cannot be removed.</exception>
    public void Use()
    {
        if (!CanRemove()) throw new InvalidOperationException($"{Name} cannot be removed.");
        LastUsed = _item;
        _owner.ChoiceMove = null;
        Remove();
    }

    /// <summary>
    ///     Transfers this Pokémon's held item to another Pokémon's held item slot.
    /// </summary>
    /// <param name="other">The destination HeldItem to transfer to.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if either this item or the target's current item cannot be removed.
    /// </exception>
    public void Transfer(HeldItem other)
    {
        if (!CanRemove()) throw new InvalidOperationException($"{Name} cannot be removed.");
        if (!other.CanRemove()) throw new InvalidOperationException($"{other.Name} cannot be removed.");
        other._item = _item;
        Remove();
    }

    /// <summary>
    ///     Swaps this Pokémon's held item with another Pokémon's held item.
    /// </summary>
    /// <param name="other">The HeldItem to swap with.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if either this item or the other item cannot be removed.
    /// </exception>
    public void Swap(HeldItem other)
    {
        if (!CanRemove()) throw new InvalidOperationException($"{Name} cannot be removed.");
        if (!other.CanRemove()) throw new InvalidOperationException($"{other.Name} cannot be removed.");
        (_item, other._item) = (other._item, _item);
        _owner.ChoiceMove = null;
        other._owner.ChoiceMove = null;
        EverHadItem = EverHadItem || _item != null;
    }

    /// <summary>
    ///     Recovers and claims the last used item from another Pokémon.
    ///     Used by moves like Recycle or abilities like Harvest.
    /// </summary>
    /// <param name="other">The HeldItem to recover the last used item from.</param>
    public void Recover(HeldItem other)
    {
        _item = other.LastUsed;
        other.LastUsed = null;
        EverHadItem = EverHadItem || _item != null;
    }

    private bool _ShouldEatBerryUtil(DuelPokemon otherpoke = null)
    {
        if (_owner.Hp == 0) return false;
        if (otherpoke != null && (
                otherpoke.Ability() == Ability.UNNERVE ||
                otherpoke.Ability() == Ability.AS_ONE_SHADOW ||
                otherpoke.Ability() == Ability.AS_ONE_ICE))
            return false;
        if (!IsBerry()) return false;
        return true;
    }

    /// <summary>
    ///     Checks if the Pokémon should automatically eat its held berry after taking damage.
    ///     Various berries activate at different HP thresholds or with the Gluttony ability.
    /// </summary>
    /// <param name="otherpoke">The opposing Pokémon, used to check for anti-berry abilities.</param>
    /// <returns>True if conditions are met for the berry to be eaten, False otherwise.</returns>
    public bool ShouldEatBerryDamage(DuelPokemon otherpoke = null)
    {
        if (!_ShouldEatBerryUtil(otherpoke)) return false;
        if (_owner.Hp <= _owner.StartingHp / 4)
        {
            var hpBerries = new[]
            {
                "figy-berry", "wiki-berry", "mago-berry", "aguav-berry", "iapapa-berry",
                "apicot-berry", "ganlon-berry", "lansat-berry", "liechi-berry", "micle-berry",
                "petaya-berry", "salac-berry", "starf-berry"
            };
            if (hpBerries.Contains(Get())) return true;
        }

        if (_owner.Hp <= _owner.StartingHp / 2)
        {
            if (_owner.Ability() == Ability.GLUTTONY) return true;
            if (Get() == "sitrus-berry") return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if the Pokémon should automatically eat its held berry after being inflicted
    ///     with a status condition. Certain berries cure specific status conditions.
    /// </summary>
    /// <param name="otherpoke">The opposing Pokémon, used to check for anti-berry abilities.</param>
    /// <returns>True if conditions are met for the berry to be eaten, False otherwise.</returns>
    public bool ShouldEatBerryStatus(DuelPokemon otherpoke = null)
    {
        if (!_ShouldEatBerryUtil(otherpoke)) return false;
        var item = Get();
        switch (item)
        {
            case "aspear-berry" or "lum-berry" when _owner.NonVolatileEffect.Freeze():
            case "cheri-berry" or "lum-berry" when _owner.NonVolatileEffect.Paralysis():
            case "chesto-berry" or "lum-berry" when _owner.NonVolatileEffect.Sleep():
            case "pecha-berry" or "lum-berry" when _owner.NonVolatileEffect.Poison():
            case "rawst-berry" or "lum-berry" when _owner.NonVolatileEffect.Burn():
            case "persim-berry" or "lum-berry" when _owner.Confusion.Active():
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Checks if the Pokémon should automatically eat its held berry due to
    ///     either damage or status conditions.
    /// </summary>
    /// <param name="otherpoke">The opposing Pokémon, used to check for anti-berry abilities.</param>
    /// <returns>True if conditions are met for the berry to be eaten, False otherwise.</returns>
    public bool ShouldEatBerry(DuelPokemon otherpoke = null)
    {
        return ShouldEatBerryDamage(otherpoke) || ShouldEatBerryStatus(otherpoke);
    }

    /// <summary>
    ///     Consumes the held berry and applies its effects.
    ///     Handles healing, stat boosts, status cures, and other berry effects.
    /// </summary>
    /// <param name="consumer">
    ///     The Pokémon consuming the berry. If null, defaults to the item's owner.
    ///     Used for moves like Bug Bite that consume another Pokémon's berry.
    /// </param>
    /// <param name="attacker">The attacking Pokémon, if relevant to berry effects.</param>
    /// <param name="move">The move being used, if relevant to berry effects.</param>
    /// <returns>A formatted message describing the berry's effects.</returns>
    public string EatBerry(DuelPokemon consumer = null, DuelPokemon attacker = null, Move.Move move = null)
    {
        var msg = "";
        if (!IsBerry()) return "";
        if (consumer == null)
            consumer = _owner;
        else
            msg += $"{consumer.Name} eats {_owner.Name}'s berry!\n";

        // 2x or 1x
        var ripe = Convert.ToInt32(consumer.Ability(attacker, move) == Ability.RIPEN) + 1;
        string flavor = null;

        var item = Get();
        switch (item)
        {
            case "sitrus-berry":
                msg += consumer.Heal(ripe * consumer.StartingHp / 4, "eating its berry");
                break;
            case "figy-berry":
                msg += consumer.Heal(ripe * consumer.StartingHp / 3, "eating its berry");
                flavor = "spicy";
                break;
            case "wiki-berry":
                msg += consumer.Heal(ripe * consumer.StartingHp / 3, "eating its berry");
                flavor = "dry";
                break;
            case "mago-berry":
                msg += consumer.Heal(ripe * consumer.StartingHp / 3, "eating its berry");
                flavor = "sweet";
                break;
            case "aguav-berry":
                msg += consumer.Heal(ripe * consumer.StartingHp / 3, "eating its berry");
                flavor = "bitter";
                break;
            case "iapapa-berry":
                msg += consumer.Heal(ripe * consumer.StartingHp / 3, "eating its berry");
                flavor = "sour";
                break;
            case "apicot-berry":
                msg += consumer.AppendSpDef(ripe * 1, attacker, move, "eating its berry");
                break;
            case "ganlon-berry":
                msg += consumer.AppendDefense(ripe * 1, attacker, move, "eating its berry");
                break;
            case "lansat-berry":
                consumer.LansatBerryAte = true;
                msg += $"{consumer.Name} is powered up by eating its berry.\n";
                break;
            case "liechi-berry":
                msg += consumer.AppendAttack(ripe * 1, attacker, move, "eating its berry");
                break;
            case "micle-berry":
                consumer.MicleBerryAte = true;
                msg += $"{consumer.Name} is powered up by eating its berry.\n";
                break;
            case "petaya-berry":
                msg += consumer.AppendSpAtk(ripe * 1, attacker, move, "eating its berry");
                break;
            case "salac-berry":
                msg += consumer.AppendSpeed(ripe * 1, attacker, move, "eating its berry");
                break;
            case "starf-berry":
                var funcs = new Func<int, DuelPokemon, Move.Move, string, bool, string>[]
                {
                    consumer.AppendAttack,
                    consumer.AppendDefense,
                    consumer.AppendSpAtk,
                    consumer.AppendSpDef,
                    consumer.AppendSpeed
                };
                var func = funcs[new Random().Next(funcs.Length)];
                msg += func(ripe * 2, attacker, move, "eating its berry", false);
                break;
            case "aspear-berry":
                if (consumer.NonVolatileEffect.Freeze())
                {
                    consumer.NonVolatileEffect.Reset();
                    msg += $"{consumer.Name} is no longer frozen after eating its berry!\n";
                }
                else
                {
                    msg += $"{consumer.Name}'s berry had no effect!\n";
                }

                break;
            case "cheri-berry":
                if (consumer.NonVolatileEffect.Paralysis())
                {
                    consumer.NonVolatileEffect.Reset();
                    msg += $"{consumer.Name} is no longer paralyzed after eating its berry!\n";
                }
                else
                {
                    msg += $"{consumer.Name}'s berry had no effect!\n";
                }

                break;
            case "chesto-berry":
                if (consumer.NonVolatileEffect.Sleep())
                {
                    consumer.NonVolatileEffect.Reset();
                    msg += $"{consumer.Name} woke up after eating its berry!\n";
                }
                else
                {
                    msg += $"{consumer.Name}'s berry had no effect!\n";
                }

                break;
            case "pecha-berry":
                if (consumer.NonVolatileEffect.Poison())
                {
                    consumer.NonVolatileEffect.Reset();
                    msg += $"{consumer.Name} is no longer poisoned after eating its berry!\n";
                }
                else
                {
                    msg += $"{consumer.Name}'s berry had no effect!\n";
                }

                break;
            case "rawst-berry":
                if (consumer.NonVolatileEffect.Burn())
                {
                    consumer.NonVolatileEffect.Reset();
                    msg += $"{consumer.Name} is no longer burned after eating its berry!\n";
                }
                else
                {
                    msg += $"{consumer.Name}'s berry had no effect!\n";
                }

                break;
            case "persim-berry":
                if (consumer.Confusion.Active())
                {
                    consumer.Confusion.SetTurns(0);
                    msg += $"{consumer.Name} is no longer confused after eating its berry!\n";
                }
                else
                {
                    msg += $"{consumer.Name}'s berry had no effect!\n";
                }

                break;
            case "lum-berry":
                consumer.NonVolatileEffect.Reset();
                consumer.Confusion.SetTurns(0);
                msg += $"{consumer.Name}'s statuses were cleared from eating its berry!\n";
                break;
        }

        if (flavor != null && consumer.DislikedFlavor == flavor)
            msg += consumer.Confuse(attacker, move, "disliking its berry's flavor");
        if (consumer.Ability(attacker, move) == Ability.CHEEK_POUCH)
            msg += consumer.Heal(consumer.StartingHp / 3, "its cheek pouch");

        consumer.LastBerry = _item;
        consumer.AteBerry = true;
        // TODO: right now HeldItem does not support `recover`ing/setting from anything other than another HeldItem object.
        //       this should probably be modified to be an `ExpiringItem` w/ that item for cases where `last_item` gets reset.
        if (consumer.Ability(attacker, move) == Ability.CUD_CHEW) consumer.CudChew.SetTurns(2);
        if (consumer == _owner)
            Use();
        else
            Remove();

        return msg;
    }

    /// <summary>
    ///     Compares a HeldItem with a string identifier.
    /// </summary>
    /// <param name="item">The HeldItem to compare.</param>
    /// <param name="other">The string identifier to compare with.</param>
    /// <returns>True if the HeldItem's identifier matches the string, False otherwise.</returns>
    public static bool operator ==(HeldItem item, string other)
    {
        return item?.Get() == other;
    }

    /// <summary>
    ///     Compares a HeldItem with a string identifier for inequality.
    /// </summary>
    /// <param name="item">The HeldItem to compare.</param>
    /// <param name="other">The string identifier to compare with.</param>
    /// <returns>True if the HeldItem's identifier doesn't match the string, False otherwise.</returns>
    public static bool operator !=(HeldItem item, string other)
    {
        return item?.Get() != other;
    }

    /// <summary>
    ///     Determines whether the specified object is equal to the current HeldItem.
    /// </summary>
    /// <param name="obj">The object to compare with the current HeldItem.</param>
    /// <returns>
    ///     True if the specified object is a string matching the item's identifier,
    ///     or if it is equal according to the base class's implementation.
    /// </returns>
    public override bool Equals(object obj)
    {
        if (obj is string str) return Get() == str;
        return base.Equals(obj);
    }

    /// <summary>
    ///     Returns a hash code for this HeldItem.
    /// </summary>
    /// <returns>
    ///     A hash code based on the item's identifier, or 0 if no item is held.
    /// </returns>
    public override int GetHashCode()
    {
        return Get()?.GetHashCode() ?? 0;
    }
}

/// <summary>
///     Represents the collection of data that can be transferred between Pokémon
///     via the move Baton Pass. Stores stat changes, volatile status conditions, and
///     other transferable battle effects.
/// </summary>
public class BatonPass(DuelPokemon poke)
{
    /// <summary>
    ///     Gets the Attack stage modifier (-6 to +6) from the original Pokémon.
    /// </summary>
    public int AttackStage { get; } = poke.AttackStage;

    /// <summary>
    ///     Gets the Defense stage modifier (-6 to +6) from the original Pokémon.
    /// </summary>
    public int DefenseStage { get; } = poke.DefenseStage;

    /// <summary>
    ///     Gets the Special Attack stage modifier (-6 to +6) from the original Pokémon.
    /// </summary>
    public int SpAtkStage { get; } = poke.SpAtkStage;

    /// <summary>
    ///     Gets the Special Defense stage modifier (-6 to +6) from the original Pokémon.
    /// </summary>
    public int SpDefStage { get; } = poke.SpDefStage;

    /// <summary>
    ///     Gets the Speed stage modifier (-6 to +6) from the original Pokémon.
    /// </summary>
    public int SpeedStage { get; } = poke.SpeedStage;

    /// <summary>
    ///     Gets the Evasion stage modifier (-6 to +6) from the original Pokémon.
    /// </summary>
    public int EvasionStage { get; } = poke.EvasionStage;

    /// <summary>
    ///     Gets the Accuracy stage modifier (-6 to +6) from the original Pokémon.
    /// </summary>
    public int AccuracyStage { get; } = poke.AccuracyStage;

    /// <summary>
    ///     Gets the Confusion effect from the original Pokémon.
    /// </summary>
    public ExpiringEffect Confusion { get; } = poke.Confusion;

    /// <summary>
    ///     Gets the Focus Energy status from the original Pokémon.
    /// </summary>
    public bool FocusEnergy { get; } = poke.FocusEnergy;

    /// <summary>
    ///     Gets the Mind Reader effect from the original Pokémon.
    /// </summary>
    public ExpiringItem MindReader { get; } = poke.MindReader;

    /// <summary>
    ///     Gets the Leech Seed status from the original Pokémon.
    /// </summary>
    public bool LeechSeed { get; } = poke.LeechSeed;

    /// <summary>
    ///     Gets the Curse effect from the original Pokémon.
    /// </summary>
    public bool Curse { get; } = poke.Curse;

    /// <summary>
    ///     Gets the Substitute HP from the original Pokémon.
    /// </summary>
    public int Substitute { get; } = poke.Substitute;

    /// <summary>
    ///     Gets the Ingrain status from the original Pokémon.
    /// </summary>
    public bool Ingrain { get; } = poke.Ingrain;

    /// <summary>
    ///     Gets the Power Trick status from the original Pokémon.
    /// </summary>
    public bool PowerTrick { get; } = poke.PowerTrick;

    /// <summary>
    ///     Gets the Power Shift status from the original Pokémon.
    /// </summary>
    public bool PowerShift { get; } = poke.PowerShift;

    /// <summary>
    ///     Gets the Heal Block effect from the original Pokémon.
    /// </summary>
    public ExpiringEffect HealBlock { get; } = poke.HealBlock;

    /// <summary>
    ///     Gets the Embargo effect from the original Pokémon.
    /// </summary>
    public ExpiringEffect Embargo { get; } = poke.Embargo;

    /// <summary>
    ///     Gets the Perish Song effect from the original Pokémon.
    /// </summary>
    public ExpiringEffect PerishSong { get; } = poke.PerishSong;

    /// <summary>
    ///     Gets the Magnet Rise effect from the original Pokémon.
    /// </summary>
    public ExpiringEffect MagnetRise { get; } = poke.MagnetRise;

    /// <summary>
    ///     Gets the Aqua Ring status from the original Pokémon.
    /// </summary>
    public bool AquaRing { get; } = poke.AquaRing;

    /// <summary>
    ///     Gets the Telekinesis effect from the original Pokémon.
    /// </summary>
    public ExpiringEffect Telekinesis { get; } = poke.Telekinesis;

    /// <summary>
    ///     Applies all stored effects to a new Pokémon.
    ///     Transfers stat changes, volatile status effects, and other battle conditions
    ///     that can be passed via Baton Pass.
    /// </summary>
    /// <param name="poke">The Pokémon to apply the stored effects to.</param>
    public void Apply(DuelPokemon poke)
    {
        if (poke.Ability() != Ability.CURIOUS_MEDICINE)
        {
            poke.AttackStage = AttackStage;
            poke.DefenseStage = DefenseStage;
            poke.SpAtkStage = SpAtkStage;
            poke.SpDefStage = SpDefStage;
            poke.SpeedStage = SpeedStage;
            poke.EvasionStage = EvasionStage;
            poke.AccuracyStage = AccuracyStage;
        }

        poke.Confusion = Confusion;
        poke.FocusEnergy = FocusEnergy;
        poke.MindReader = MindReader;
        poke.LeechSeed = LeechSeed;
        poke.Curse = Curse;
        poke.Substitute = Substitute;
        poke.Ingrain = Ingrain;
        poke.PowerTrick = PowerTrick;
        poke.PowerShift = PowerShift;
        poke.HealBlock = HealBlock;
        poke.Embargo = Embargo;
        poke.PerishSong = PerishSong;
        poke.MagnetRise = MagnetRise;
        poke.AquaRing = AquaRing;
        poke.Telekinesis = Telekinesis;
    }
}