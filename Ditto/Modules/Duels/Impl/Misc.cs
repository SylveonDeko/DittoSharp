namespace Duels
{
    /// <summary>
    /// Some effect that has a specific amount of time it is active.
    /// Turns_to_expire can be null, in which case this effect never expires.
    /// </summary>
    public class ExpiringEffect(int? turnsToExpire)
    {
        public int? RemainingTurns = turnsToExpire;

        /// <summary>
        /// Returns True if this effect is still active, False otherwise.
        /// </summary>
        public bool Active()
        {
            if (RemainingTurns == null)
                return true;
            return RemainingTurns > 0;
        }

        /// <summary>
        /// Progresses this effect for a turn.
        /// Returns True if the effect just ended.
        /// </summary>
        public bool NextTurn()
        {
            if (RemainingTurns == null)
                return false;
            if (!Active()) return false;
            RemainingTurns--;
            return !Active();
        }

        /// <summary>
        /// Set the amount of turns until this effect expires.
        /// </summary>
        public void SetTurns(int? turnsToExpire)
        {
            RemainingTurns = turnsToExpire;
        }
    }

    /// <summary>
    /// The current weather of the battlefield.
    ///
    /// Options:
    /// -hail
    /// -sandstorm
    /// -h-rain
    /// -rain
    /// -h-sun
    /// -sun
    /// -h-wind
    /// </summary>
    public class Weather : ExpiringEffect
    {
        private string _weatherType = "";
        private Battle _battle;

        public Weather(Battle battle) : base(0)
        {
            _battle = battle;
        }

        /// <summary>
        /// Clear the current weather and update Castform forms.
        /// </summary>
        private void _ExpireWeather()
        {
            _weatherType = "";
            foreach (var poke in new[] { _battle.Trainer1.CurrentPokemon, _battle.Trainer2.CurrentPokemon })
            {
                if (poke == null)
                    continue;
                // Forecast
                if (poke.Ability() != Ability.FORECAST || !poke.Name.StartsWith("Castform") ||
                    poke.Name == "Castform") continue;
                if (poke.Form("Castform"))
                {
                    poke.TypeIds = new List<ElementType> { ElementType.NORMAL };
                }
            }
        }

        /// <summary>
        /// Progresses the weather a turn.
        /// </summary>
        public new bool NextTurn()
        {
            if (!base.NextTurn()) return false;
            _ExpireWeather();
            return true;
        }

        /// <summary>
        /// Checks if strong weather effects from a pokemon with a weather ability need to be removed.
        /// </summary>
        public bool RecheckAbilityWeather()
        {
            var maintainWeather = false;
            foreach (var poke in new[] { _battle.Trainer1.CurrentPokemon, _battle.Trainer2.CurrentPokemon })
            {
                if (poke == null)
                    continue;
                switch (_weatherType)
                {
                    case "h-wind" when poke.Ability() == Ability.DELTA_STREAM:
                    case "h-sun" when poke.Ability() == Ability.DESOLATE_LAND:
                    case "h-rain" when poke.Ability() == Ability.PRIMORDIAL_SEA:
                        maintainWeather = true;
                        break;
                }
            }

            if (new[] { "h-wind", "h-sun", "h-rain" }.Contains(_weatherType) && !maintainWeather)
            {
                _ExpireWeather();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the current weather type.
        /// </summary>
        public string Get()
        {
            foreach (var poke in new[] { _battle.Trainer1.CurrentPokemon, _battle.Trainer2.CurrentPokemon })
            {
                if (poke == null)
                    continue;
                if (poke.Ability() == Ability.CLOUD_NINE || poke.Ability() == Ability.AIR_LOCK)
                    return "";
            }
            return _weatherType;
        }

        /// <summary>
        /// Set the weather, lasting a certain number of turns.
        /// Returns a formatted message indicating any weather change.
        /// </summary>
        public string Set(string weather, DuelPokemon pokemon)
        {
            var msg = "";
            int? turns = null;
            ElementType? element = null;
            string castform = null;

            if (_weatherType == weather)
                return "";

            switch (weather)
            {
                case "hail":
                    if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(_weatherType))
                        return "";
                    turns = pokemon.HeldItem == "icy-rock" ? 8 : 5;
                    msg += "It starts to hail!\n";
                    element = ElementType.ICE;
                    castform = "Castform-snowy";
                    break;
                case "sandstorm":
                    if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(_weatherType))
                        return "";
                    turns = pokemon.HeldItem == "smooth-rock" ? 8 : 5;
                    msg += "A sandstorm is brewing up!\n";
                    element = ElementType.NORMAL;
                    castform = "Castform";
                    break;
                case "rain":
                    if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(_weatherType))
                        return "";
                    turns = pokemon.HeldItem == "damp-rock" ? 8 : 5;
                    msg += "It starts to rain!\n";
                    element = ElementType.WATER;
                    castform = "Castform-rainy";
                    break;
                case "sun":
                    if (new[] { "h-rain", "h-sun", "h-wind" }.Contains(_weatherType))
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
            foreach (var poke in new[] { _battle.Trainer1.CurrentPokemon, _battle.Trainer2.CurrentPokemon })
            {
                if (poke == null)
                    continue;
                if (poke.Ability() == Ability.FORECAST && poke.Name != castform)
                {
                    if (poke.Form(castform))
                    {
                        poke.TypeIds = new List<ElementType> { element.Value };
                        msg += $"{poke.Name} transformed into a {t} type using its forecast!\n";
                    }
                }
            }

            _weatherType = weather;
            SetTurns(turns);
            return msg;
        }
    }

    /// <summary>
    /// A multi-turn move that a pokemon is locked into.
    /// </summary>
    public class LockedMove : ExpiringEffect
    {
        public Move Move { get; private set; }
        public int Turn { get; private set; } = 0;

        public LockedMove(Move move, int turnsToExpire) : base(turnsToExpire)
        {
            Move = move;
        }

        /// <summary>
        /// Progresses the move a turn.
        /// </summary>
        public new bool NextTurn()
        {
            var expired = base.NextTurn();
            Turn++;
            return expired;
        }

        /// <summary>
        /// Returns True if this is the last turn this move will be used.
        /// </summary>
        public bool IsLastTurn()
        {
            return base.RemainingTurns == 1;
        }
    }

    /// <summary>
    /// An expiration timer with some data.
    /// </summary>
    public class ExpiringItem : ExpiringEffect
    {
        public object Item { get; private set; }

        public ExpiringItem() : base(0)
        {
        }

        /// <summary>
        /// Progresses the effect a turn.
        /// </summary>
        public new bool NextTurn()
        {
            var expired = base.NextTurn();
            if (expired)
            {
                Item = null;
            }
            return expired;
        }

        /// <summary>
        /// Set the item and turns until expiration.
        /// </summary>
        public void Set(object item, int turns)
        {
            Item = item;
            SetTurns(turns);
        }

        /// <summary>
        /// Ends this expiring item.
        /// </summary>
        public void End()
        {
            Item = null;
            SetTurns(0);
        }
    }

    /// <summary>
    /// The terrain of the battle
    /// </summary>
    public class Terrain : ExpiringItem
    {
        private Battle _battle;

        public Terrain(Battle battle) : base()
        {
            _battle = battle;
        }

        /// <summary>
        /// Progresses the effect a turn.
        /// </summary>
        public new bool NextTurn()
        {
            var expired = base.NextTurn();
            if (expired)
            {
                End();
            }
            return expired;
        }

        /// <summary>
        /// Set the terrain and turns until expiration.
        /// Returns a formatted string.
        /// </summary>
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

            foreach (var poke in new[] { _battle.Trainer1.CurrentPokemon, _battle.Trainer2.CurrentPokemon })
            {
                if (poke == null)
                    continue;
                if (poke.Ability() == Ability.MIMICRY)
                {
                    poke.TypeIds = new List<ElementType> { element.Value };
                    var t = element.Value.ToString().ToLower();
                    msg += $"{poke.Name} became a {t} type using its mimicry!\n";
                }
                if (poke.HeldItem == "electric-seed" && item == "electric")
                {
                    msg += poke.AppendDefense(1, attacker: poke, source: "its electric seed");
                    poke.HeldItem.Use();
                }
                if (poke.HeldItem == "psychic-seed" && item == "psychic")
                {
                    msg += poke.AppendSpDef(1, attacker: poke, source: "its psychic seed");
                    poke.HeldItem.Use();
                }
                if (poke.HeldItem == "misty-seed" && item == "misty")
                {
                    msg += poke.AppendSpDef(1, attacker: poke, source: "its misty seed");
                    poke.HeldItem.Use();
                }
                if (poke.HeldItem == "grassy-seed" && item == "grassy")
                {
                    msg += poke.AppendDefense(1, attacker: poke, source: "its grassy seed");
                    poke.HeldItem.Use();
                }
            }
            return msg;
        }

        /// <summary>
        /// Ends the terrain.
        /// </summary>
        public new void End()
        {
            base.End();
            // Mimicry
            foreach (var poke in new[] { _battle.Trainer1.CurrentPokemon, _battle.Trainer2.CurrentPokemon })
            {
                if (poke == null)
                    continue;
                if (poke.Ability() == Ability.MIMICRY)
                {
                    poke.TypeIds = new List<ElementType>(poke.StartingTypeIds);
                }
            }
        }
    }

    /// <summary>
    /// Stores the HP and when to heal for the move Wish.
    /// </summary>
    public class ExpiringWish : ExpiringEffect
    {
        public int? Hp { get; private set; }

        public ExpiringWish() : base(0)
        {
        }

        /// <summary>
        /// Progresses the effect a turn.
        /// </summary>
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
        /// Set the move and turns until expiration.
        /// </summary>
        public void Set(int hp)
        {
            Hp = hp;
            SetTurns(2);
        }
    }

    /// <summary>
    /// The current non volatile effect status.
    /// </summary>
    public class NonVolatileEffect
    {
        public string Current { get; set; } = "";
        private DuelPokemon _pokemon;
        public ExpiringEffect SleepTimer { get; private set; }
        public int BadlyPoisonedTurn { get; set; } = 0;

        public NonVolatileEffect(DuelPokemon pokemon)
        {
            _pokemon = pokemon;
            SleepTimer = new ExpiringEffect(0);
        }

        /// <summary>
        /// Progresses this status by a turn.
        /// Returns a formatted string if a status wore off.
        /// </summary>
        public string NextTurn(Battle battle)
        {
            if (string.IsNullOrEmpty(Current))
                return "";

            if (Current == "b-poison")
            {
                BadlyPoisonedTurn++;
            }

            if (_pokemon.Ability() == Ability.HYDRATION && new[] { "rain", "h-rain" }.Contains(battle.Weather.Get()))
            {
                var removed = Current;
                Reset();
                return $"{_pokemon.Name}'s hydration cured its {removed}!\n";
            }

            if (_pokemon.Ability() == Ability.SHED_SKIN && new Random().Next(3) == 0)
            {
                var removed = Current;
                Reset();
                return $"{_pokemon.Name}'s shed skin cured its {removed}!\n";
            }

            switch (Current)
            {
                // The poke still has a status effect, apply damage
                case "burn":
                {
                    var damage = Math.Max(1, _pokemon.StartingHp / 16);
                    if (_pokemon.Ability() == Ability.HEATPROOF)
                    {
                        damage /= 2;
                    }
                    return _pokemon.Damage(damage, battle, source: "its burn");
                }
                case "b-poison":
                {
                    if (_pokemon.Ability() == Ability.POISON_HEAL)
                    {
                        return _pokemon.Heal(_pokemon.StartingHp / 8, source: "its poison heal");
                    }
                    var damage = Math.Max(1, _pokemon.StartingHp / 16 * Math.Min(15, BadlyPoisonedTurn));
                    return _pokemon.Damage(damage, battle, source: "its bad poison");
                }
                case "poison":
                {
                    if (_pokemon.Ability() == Ability.POISON_HEAL)
                    {
                        return _pokemon.Heal(_pokemon.StartingHp / 8, source: "its poison heal");
                    }
                    var damage = Math.Max(1, _pokemon.StartingHp / 8);
                    return _pokemon.Damage(damage, battle, source: "its poison");
                }
                case "sleep" when _pokemon.Nightmare:
                    return _pokemon.Damage(_pokemon.StartingHp / 4, battle, source: "its nightmare");
                default:
                    return "";
            }
        }

        /// <summary>
        /// Returns True if the pokemon is burned.
        /// </summary>
        public bool Burn()
        {
            return Current == "burn";
        }

        /// <summary>
        /// Returns True if the pokemon is asleep.
        /// </summary>
        public bool Sleep()
        {
            if (_pokemon.Ability() == Ability.COMATOSE)
            {
                return true;
            }
            return Current == "sleep";
        }

        /// <summary>
        /// Returns True if the pokemon is poisoned.
        /// </summary>
        public bool Poison()
        {
            return Current is "poison" or "b-poison";
        }

        /// <summary>
        /// Returns True if the pokemon is paralyzed.
        /// </summary>
        public bool Paralysis()
        {
            return Current == "paralysis";
        }

        /// <summary>
        /// Returns True if the pokemon is frozen.
        /// </summary>
        public bool Freeze()
        {
            return Current == "freeze";
        }

        /// <summary>
        /// Apply a non volatile status to a pokemon.
        /// Returns a formatted message.
        /// </summary>
        public string ApplyStatus(string status, Battle battle, DuelPokemon attacker = null, Move move = null, int? turns = null, bool force = false, string source = "")
        {
            var msg = "";
            if (!string.IsNullOrEmpty(source))
            {
                source = $" from {source}";
            }

            if (!string.IsNullOrEmpty(Current) && !force)
            {
                return $"{_pokemon.Name} already has a status, it can't get {status} too!\n";
            }

            if (_pokemon.Ability(attacker: attacker, move: move) == Ability.COMATOSE)
            {
                return $"{_pokemon.Name} already has a status, it can't get {status} too!\n";
            }

            if (_pokemon.Ability(attacker: attacker, move: move) == Ability.PURIFYING_SALT)
            {
                return $"{_pokemon.Name}'s purifying salt protects it from being inflicted with {status}!\n";
            }

            if (_pokemon.Ability(attacker: attacker, move: move) == Ability.LEAF_GUARD && new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
            {
                return $"{_pokemon.Name}'s leaf guard protects it from being inflicted with {status}!\n";
            }

            if (_pokemon.Substitute > 0 && attacker != _pokemon && (move == null || move.IsAffectedBySubstitute()))
            {
                return $"{_pokemon.Name}'s substitute protects it from being inflicted with {status}!\n";
            }

            if (_pokemon.Owner.Safeguard.Active() && attacker != _pokemon && (attacker == null || attacker.Ability() != Ability.INFILTRATOR))
            {
                return $"{_pokemon.Name}'s safeguard protects it from being inflicted with {status}!\n";
            }

            if (_pokemon.Grounded(battle, attacker: attacker, move: move) && battle.Terrain.Item?.ToString() == "misty")
            {
                return $"The misty terrain protects {_pokemon.Name} from being inflicted with {status}!\n";
            }

            if (_pokemon.Ability(attacker: attacker, move: move) == Ability.FLOWER_VEIL && _pokemon.TypeIds.Contains(ElementType.GRASS))
            {
                return $"{_pokemon.Name}'s flower veil protects it from being inflicted with {status}!\n";
            }

            if (_pokemon.Name == "Minior")
            {
                return "Minior's hard shell protects it from status effects!\n";
            }

            switch (status)
            {
                case "burn":
                    if (_pokemon.TypeIds.Contains(ElementType.FIRE))
                    {
                        return $"{_pokemon.Name} is a fire type and can't be burned!\n";
                    }
                    if (_pokemon.Ability(attacker: attacker, move: move) == Ability.WATER_VEIL ||
                        _pokemon.Ability(attacker: attacker, move: move) == Ability.WATER_BUBBLE)
                    {
                        var abilityName = ((Ability)_pokemon.AbilityId).GetPrettyName();
                        return $"{_pokemon.Name}'s {abilityName} prevents it from getting burned!\n";
                    }
                    Current = status;
                    msg += $"{_pokemon.Name} was burned{source}!\n";
                    break;

                case "sleep":
                    var sleepImmunities = new[] { Ability.INSOMNIA, Ability.VITAL_SPIRIT, Ability.SWEET_VEIL };
                    if (sleepImmunities.Contains(_pokemon.Ability(attacker: attacker, move: move)))
                    {
                        var abilityName = ((Ability)_pokemon.AbilityId).GetPrettyName();
                        return $"{_pokemon.Name}'s {abilityName} keeps it awake!\n";
                    }
                    if (_pokemon.Grounded(battle, attacker: attacker, move: move) && battle.Terrain.Item?.ToString() == "electric")
                    {
                        return $"The terrain is too electric for {_pokemon.Name} to fall asleep!\n";
                    }
                    if (battle.Trainer1.CurrentPokemon != null && battle.Trainer1.CurrentPokemon.Uproar.Active())
                    {
                        return $"An uproar keeps {_pokemon.Name} from falling asleep!\n";
                    }
                    if (battle.Trainer2.CurrentPokemon != null && battle.Trainer2.CurrentPokemon.Uproar.Active())
                    {
                        return $"An uproar keeps {_pokemon.Name} from falling asleep!\n";
                    }

                    if (turns == null)
                    {
                        turns = new Random().Next(2, 5);
                    }
                    if (_pokemon.Ability(attacker: attacker, move: move) == Ability.EARLY_BIRD)
                    {
                        turns /= 2;
                    }
                    Current = status;
                    SleepTimer.SetTurns(turns);
                    msg += $"{_pokemon.Name} fell asleep{source}!\n";
                    break;

                case "poison":
                case "b-poison":
                    if (attacker == null || attacker.Ability() != Ability.CORROSION)
                    {
                        if (_pokemon.TypeIds.Contains(ElementType.STEEL))
                        {
                            return $"{_pokemon.Name} is a steel type and can't be poisoned!\n";
                        }
                        if (_pokemon.TypeIds.Contains(ElementType.POISON))
                        {
                            return $"{_pokemon.Name} is a poison type and can't be poisoned!\n";
                        }
                    }
                    if (_pokemon.Ability(attacker: attacker, move: move) == Ability.IMMUNITY ||
                        _pokemon.Ability(attacker: attacker, move: move) == Ability.PASTEL_VEIL)
                    {
                        var abilityName = ((Ability)_pokemon.AbilityId).GetPrettyName();
                        return $"{_pokemon.Name}'s {abilityName} keeps it from being poisoned!\n";
                    }
                    Current = status;
                    var bad = status == "b-poison" ? " badly" : "";
                    msg += $"{_pokemon.Name} was{bad} poisoned{source}!\n";

                    if (move != null && attacker != null && attacker.Ability() == Ability.POISON_PUPPETEER)
                    {
                        msg += _pokemon.Confuse(attacker: attacker, source: $"{attacker.Name}'s poison puppeteer");
                    }
                    break;

                case "paralysis":
                    if (_pokemon.TypeIds.Contains(ElementType.ELECTRIC))
                    {
                        return $"{_pokemon.Name} is an electric type and can't be paralyzed!\n";
                    }
                    if (_pokemon.Ability(attacker: attacker, move: move) == Ability.LIMBER)
                    {
                        return $"{_pokemon.Name}'s limber keeps it from being paralyzed!\n";
                    }
                    Current = status;
                    msg += $"{_pokemon.Name} was paralyzed{source}!\n";
                    break;

                case "freeze":
                    if (_pokemon.TypeIds.Contains(ElementType.ICE))
                    {
                        return $"{_pokemon.Name} is an ice type and can't be frozen!\n";
                    }
                    if (_pokemon.Ability(attacker: attacker, move: move) == Ability.MAGMA_ARMOR)
                    {
                        return $"{_pokemon.Name}'s magma armor keeps it from being frozen!\n";
                    }
                    if (new[] { "sun", "h-sun" }.Contains(battle.Weather.Get()))
                    {
                        return $"It's too sunny to freeze {_pokemon.Name}!\n";
                    }
                    Current = status;
                    msg += $"{_pokemon.Name} was frozen solid{source}!\n";
                    break;
            }

            if (_pokemon.Ability(attacker: attacker, move: move) == Ability.SYNCHRONIZE && attacker != null)
            {
                msg += attacker.NonVolatileEffect.ApplyStatus(status, battle, attacker: _pokemon, source: $"{_pokemon.Name}'s synchronize");
            }

            if (_pokemon.HeldItem.ShouldEatBerryStatus(attacker))
            {
                msg += _pokemon.HeldItem.EatBerry(attacker: attacker, move: move);
            }

            return msg;
        }

        /// <summary>
        /// Remove a non volatile status from a pokemon.
        /// </summary>
        public void Reset()
        {
            Current = "";
            BadlyPoisonedTurn = 0;
            SleepTimer.SetTurns(0);
            _pokemon.Nightmare = false;
        }
    }

    /// <summary>
    /// Holds recent move status for the held item metronome.
    /// </summary>
    public class Metronome
    {
        public string Move { get; private set; } = "";
        public int Count { get; private set; } = 0;

        /// <summary>
        /// A move failed or a non-move action was done.
        /// </summary>
        public void Reset()
        {
            Move = "";
            Count = 0;
        }

        /// <summary>
        /// Updates the metronome based on a used move.
        /// </summary>
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
        /// Get the buff multiplier for this metronome.
        /// </summary>
        public double GetBuff(string moveName)
        {
            if (Move != moveName)
            {
                return 1;
            }
            return Math.Min(2, 1 + 0.2 * Count);
        }
    }

    /// <summary>
    /// Stores information about an item.
    /// </summary>
    public class Item
    {
        public string Name { get; }
        public int Id { get; }
        public int? Power { get; }
        public int? Effect { get; }

        public Item(IDictionary<string, object> itemData)
        {
            Name = (string)itemData["identifier"];
            Id = Convert.ToInt32(itemData["id"]);
            Power = itemData["fling_power"] as int?;
            Effect = itemData["fling_effect_id"] as int?;
        }
    }

    /// <summary>
    /// Stores information about the current held item for a particualar poke.
    /// </summary>
    public class HeldItem
    {
        private Item _item;
        private DuelPokemon _owner;
        public Battle Battle { get; set; }
        public Item LastUsed { get; set; }
        public bool EverHadItem { get; set; }

        public HeldItem(Item itemData, DuelPokemon owner)
        {
            _item = itemData;
            _owner = owner;
            EverHadItem = _item != null;
        }

        /// <summary>
        /// Get the current held item identifier.
        /// </summary>
        public string Get()
        {
            if (_item == null)
            {
                return null;
            }
            if (!CanRemove())
            {
                return _item.Name;
            }
            if (_owner.Embargo.Active())
            {
                return null;
            }
            if (Battle != null && Battle.MagicRoom.Active())
            {
                return null;
            }
            if (_owner.Ability() == Ability.KLUTZ)
            {
                return null;
            }
            if (_owner.CorrosiveGas)
            {
                return null;
            }
            return _item.Name;
        }

        /// <summary>
        /// Helper method to prevent attempting to acquire a new item if the poke already has one.
        /// </summary>
        public bool HasItem()
        {
            return _item != null;
        }

        /// <summary>
        /// Returns a boolean indicating whether this held item can be removed.
        /// </summary>
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
                "mega-stone", "mega-stone-x", "mega-stone-y",
            };

            return _item == null || !nonRemovableItems.Contains(_item.Name);
        }

        /// <summary>
        /// Returns a boolean indicating whether this held item is a berry.
        /// The optional param onlyActive determines if this method should only return True if the berry is active and usable.
        /// </summary>
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
        /// Remove this held item, setting it to None.
        /// </summary>
        public void Remove()
        {
            if (!CanRemove())
            {
                throw new InvalidOperationException($"{Name} cannot be removed.");
            }
            _item = null;
        }

        /// <summary>
        /// Uses this item, setting it to None but also recording that it was used.
        /// </summary>
        public void Use()
        {
            if (!CanRemove())
            {
                throw new InvalidOperationException($"{Name} cannot be removed.");
            }
            LastUsed = _item;
            _owner.ChoiceMove = null;
            Remove();
        }

        /// <summary>
        /// Transfer the data of this held item to other, and clear this item.
        /// </summary>
        public void Transfer(HeldItem other)
        {
            if (!CanRemove())
            {
                throw new InvalidOperationException($"{Name} cannot be removed.");
            }
            if (!other.CanRemove())
            {
                throw new InvalidOperationException($"{other.Name} cannot be removed.");
            }
            other._item = _item;
            Remove();
        }

        /// <summary>
        /// Swap the date between this held item and other.
        /// </summary>
        public void Swap(HeldItem other)
        {
            if (!CanRemove())
            {
                throw new InvalidOperationException($"{Name} cannot be removed.");
            }
            if (!other.CanRemove())
            {
                throw new InvalidOperationException($"{other.Name} cannot be removed.");
            }
            (_item, other._item) = (other._item, _item);
            _owner.ChoiceMove = null;
            other._owner.ChoiceMove = null;
            EverHadItem = EverHadItem || _item != null;
        }

        /// <summary>
        /// Recover & claim the last_used item from other.
        /// </summary>
        public void Recover(HeldItem other)
        {
            _item = other.LastUsed;
            other.LastUsed = null;
            EverHadItem = EverHadItem || _item != null;
        }

        private bool _ShouldEatBerryUtil(DuelPokemon otherpoke = null)
        {
            if (_owner.Hp == 0)
            {
                return false;
            }
            if (otherpoke != null && (
                otherpoke.Ability() == Ability.UNNERVE ||
                otherpoke.Ability() == Ability.AS_ONE_SHADOW ||
                otherpoke.Ability() == Ability.AS_ONE_ICE))
            {
                return false;
            }
            if (!IsBerry())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns True if the pokemon meets the criteria to eat its held berry after being damaged.
        /// </summary>
        public bool ShouldEatBerryDamage(DuelPokemon otherpoke = null)
        {
            if (!_ShouldEatBerryUtil(otherpoke))
            {
                return false;
            }
            if (_owner.Hp <= _owner.StartingHp / 4)
            {
                var hpBerries = new[] {
                    "figy-berry", "wiki-berry", "mago-berry", "aguav-berry", "iapapa-berry",
                    "apicot-berry", "ganlon-berry", "lansat-berry", "liechi-berry", "micle-berry",
                    "petaya-berry", "salac-berry", "starf-berry"
                };
                if (hpBerries.Contains(Get()))
                {
                    return true;
                }
            }
            if (_owner.Hp <= _owner.StartingHp / 2)
            {
                if (_owner.Ability() == Ability.GLUTTONY)
                {
                    return true;
                }
                if (Get() == "sitrus-berry")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns True if the pokemon meets the criteria to eat its held berry after getting a status.
        /// </summary>
        public bool ShouldEatBerryStatus(DuelPokemon otherpoke = null)
        {
            if (!_ShouldEatBerryUtil(otherpoke))
            {
                return false;
            }
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
        /// Returns True if the pokemon meets the criteria to eat its held berry.
        /// </summary>
        public bool ShouldEatBerry(DuelPokemon otherpoke = null)
        {
            return ShouldEatBerryDamage(otherpoke) || ShouldEatBerryStatus(otherpoke);
        }

        /// <summary>
        /// Eat this held item berry.
        /// Returns a formatted message.
        /// </summary>
        public string EatBerry(DuelPokemon consumer = null, DuelPokemon attacker = null, Move move = null)
        {
            var msg = "";
            if (!IsBerry())
            {
                return "";
            }
            if (consumer == null)
            {
                consumer = _owner;
            }
            else
            {
                msg += $"{consumer.Name} eats {_owner.Name}'s berry!\n";
            }

            // 2x or 1x
            var ripe = Convert.ToInt32(consumer.Ability(attacker: attacker, move: move) == Ability.RIPEN) + 1;
            string flavor = null;

            var item = Get();
            switch (item)
            {
                case "sitrus-berry":
                    msg += consumer.Heal(ripe * consumer.StartingHp / 4, source: "eating its berry");
                    break;
                case "figy-berry":
                    msg += consumer.Heal(ripe * consumer.StartingHp / 3, source: "eating its berry");
                    flavor = "spicy";
                    break;
                case "wiki-berry":
                    msg += consumer.Heal(ripe * consumer.StartingHp / 3, source: "eating its berry");
                    flavor = "dry";
                    break;
                case "mago-berry":
                    msg += consumer.Heal(ripe * consumer.StartingHp / 3, source: "eating its berry");
                    flavor = "sweet";
                    break;
                case "aguav-berry":
                    msg += consumer.Heal(ripe * consumer.StartingHp / 3, source: "eating its berry");
                    flavor = "bitter";
                    break;
                case "iapapa-berry":
                    msg += consumer.Heal(ripe * consumer.StartingHp / 3, source: "eating its berry");
                    flavor = "sour";
                    break;
                case "apicot-berry":
                    msg += consumer.AppendSpDef(ripe * 1, attacker: attacker, move: move, source: "eating its berry");
                    break;
                case "ganlon-berry":
                    msg += consumer.AppendDefense(ripe * 1, attacker: attacker, move: move, source: "eating its berry");
                    break;
                case "lansat-berry":
                    consumer.LansatBerryAte = true;
                    msg += $"{consumer.Name} is powered up by eating its berry.\n";
                    break;
                case "liechi-berry":
                    msg += consumer.AppendAttack(ripe * 1, attacker: attacker, move: move, source: "eating its berry");
                    break;
                case "micle-berry":
                    consumer.MicleBerryAte = true;
                    msg += $"{consumer.Name} is powered up by eating its berry.\n";
                    break;
                case "petaya-berry":
                    msg += consumer.AppendSpAtk(ripe * 1, attacker: attacker, move: move, source: "eating its berry");
                    break;
                case "salac-berry":
                    msg += consumer.AppendSpeed(ripe * 1, attacker: attacker, move: move, source: "eating its berry");
                    break;
                case "starf-berry":
                    var funcs = new Func<int, DuelPokemon, Move, string, bool, string>[] {
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
            {
                msg += consumer.Confuse(attacker: attacker, move: move, source: "disliking its berry's flavor");
            }
            if (consumer.Ability(attacker: attacker, move: move) == Ability.CHEEK_POUCH)
            {
                msg += consumer.Heal(consumer.StartingHp / 3, source: "its cheek pouch");
            }

            consumer.LastBerry = _item;
            consumer.AteBerry = true;
            // TODO: right now HeldItem does not support `recover`ing/setting from anything other than another HeldItem object.
            //       this should probably be modified to be an `ExpiringItem` w/ that item for cases where `last_item` gets reset.
            if (consumer.Ability(attacker: attacker, move: move) == Ability.CUD_CHEW)
            {
                consumer.CudChew.SetTurns(2);
            }
            if (consumer == _owner)
            {
                Use();
            }
            else
            {
                Remove();
            }

            return msg;
        }

        // Properties to access the item's properties
        public string Name => _item?.Name;
        public int? Power => _item?.Power;
        public int? Id => _item?.Id;
        public int? Effect => _item?.Effect;

        // Operator overloads for comparison
        public static bool operator ==(HeldItem item, string other)
        {
            return item?.Get() == other;
        }

        public static bool operator !=(HeldItem item, string other)
        {
            return item?.Get() != other;
        }

        public override bool Equals(object obj)
        {
            if (obj is string str)
            {
                return Get() == str;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Get()?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Stores the necessary data from a pokemon to baton pass to another pokemon.
    /// </summary>
    public class BatonPass
    {
        public int AttackStage { get; }
        public int DefenseStage { get; }
        public int SpAtkStage { get; }
        public int SpDefStage { get; }
        public int SpeedStage { get; }
        public int EvasionStage { get; }
        public int AccuracyStage { get; }
        public ExpiringEffect Confusion { get; }
        public bool FocusEnergy { get; }
        public ExpiringItem MindReader { get; }
        public bool LeechSeed { get; }
        public bool Curse { get; }
        public int Substitute { get; }
        public bool Ingrain { get; }
        public bool PowerTrick { get; }
        public bool PowerShift { get; }
        public ExpiringEffect HealBlock { get; }
        public ExpiringEffect Embargo { get; }
        public ExpiringEffect PerishSong { get; }
        public ExpiringEffect MagnetRise { get; }
        public bool AquaRing { get; }
        public ExpiringEffect Telekinesis { get; }

        public BatonPass(DuelPokemon poke)
        {
            AttackStage = poke.AttackStage;
            DefenseStage = poke.DefenseStage;
            SpAtkStage = poke.SpAtkStage;
            SpDefStage = poke.SpDefStage;
            SpeedStage = poke.SpeedStage;
            EvasionStage = poke.EvasionStage;
            AccuracyStage = poke.AccuracyStage;
            Confusion = poke.Confusion;
            FocusEnergy = poke.FocusEnergy;
            MindReader = poke.MindReader;
            LeechSeed = poke.LeechSeed;
            Curse = poke.Curse;
            Substitute = poke.Substitute;
            Ingrain = poke.Ingrain;
            PowerTrick = poke.PowerTrick;
            PowerShift = poke.PowerShift;
            HealBlock = poke.HealBlock;
            Embargo = poke.Embargo;
            PerishSong = poke.PerishSong;
            MagnetRise = poke.MagnetRise;
            AquaRing = poke.AquaRing;
            Telekinesis = poke.Telekinesis;
        }

        /// <summary>
        /// Push this objects data to a poke.
        /// </summary>
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
}