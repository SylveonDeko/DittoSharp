namespace EeveeCore.Modules.Duels.Impl.Move;

public partial class Move
{
    /// <summary>
    ///     Sets up anything this move needs to do prior to normal move execution.
    /// </summary>
    /// <returns>A formatted message.</returns>
    public string Setup(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
    {
        var msg = "";
        if (Effect == 129 && (defender.Owner.SelectedAction.IsSwitch ||
                              (defender.Owner.SelectedAction as Trainer.MoveAction)?.Move.Effect is 128 or 154 or 229
                              or 347 or 493))
            msg += Use(attacker, defender, battle);

        switch (Effect)
        {
            case 171:
                msg += $"{attacker.Name} is focusing on its attack!\n";
                break;
            case 404:
                attacker.BeakBlast = true;
                break;
        }

        return msg;
    }

    /// <summary>
    ///     Calculates the element type this move will be.
    /// </summary>
    /// <returns>The element type of this move, taking into account abilities and effects.</returns>
    public ElementType GetType(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
    {
        // Abilities are first because those are intrinsic to the poke and would "apply" to the move first
        if (attacker.Ability() == Ability.REFRIGERATE && Type == ElementType.NORMAL) return ElementType.ICE;

        if (attacker.Ability() == Ability.PIXILATE && Type == ElementType.NORMAL) return ElementType.FAIRY;

        if (attacker.Ability() == Ability.AERILATE && Type == ElementType.NORMAL) return ElementType.FLYING;

        if (attacker.Ability() == Ability.GALVANIZE && Type == ElementType.NORMAL) return ElementType.ELECTRIC;

        if (attacker.Ability() == Ability.NORMALIZE) return ElementType.NORMAL;

        if (attacker.Ability() == Ability.LIQUID_VOICE && IsSoundBased()) return ElementType.WATER;

        if (Type == ElementType.NORMAL && (attacker.IonDeluge || defender.IonDeluge || battle.PlasmaFists))
            return ElementType.ELECTRIC;

        if (attacker.Electrify) return ElementType.ELECTRIC;

        switch (Effect)
        {
            case 204:
            {
                if (battle.Weather.Get() == "hail") return ElementType.ICE;

                if (battle.Weather.Get() == "sandstorm") return ElementType.ROCK;

                if (new[] { "h-sun", "sun" }.Contains(battle.Weather.Get())) return ElementType.FIRE;

                if (new[] { "h-rain", "rain" }.Contains(battle.Weather.Get())) return ElementType.WATER;

                break;
            }
            case 136:
            {
                // Uses starting IVs as its own IVs should be used even if transformed
                var typeIdx = attacker.StartingHpIV % 2;
                typeIdx += 2 * (attacker.StartingAtkIV % 2);
                typeIdx += 4 * (attacker.StartingDefIV % 2);
                typeIdx += 8 * (attacker.StartingSpeedIV % 2);
                typeIdx += 16 * (attacker.StartingSpAtkIV % 2);
                typeIdx += 32 * (attacker.StartingSpDefIV % 2);
                typeIdx = typeIdx * 15 / 63;
                var typeOptions = new[]
                {
                    ElementType.FIGHTING,
                    ElementType.FLYING,
                    ElementType.POISON,
                    ElementType.GROUND,
                    ElementType.ROCK,
                    ElementType.BUG,
                    ElementType.GHOST,
                    ElementType.STEEL,
                    ElementType.FIRE,
                    ElementType.WATER,
                    ElementType.GRASS,
                    ElementType.ELECTRIC,
                    ElementType.PSYCHIC,
                    ElementType.ICE,
                    ElementType.DRAGON,
                    ElementType.DARK
                };
                return typeOptions[typeIdx];
            }
            case 401:
            {
                if (attacker.TypeIds.Count == 0) return ElementType.TYPELESS;

                return attacker.TypeIds[0];
            }
            case 269:
            {
                if (attacker.HeldItem == "draco-plate" || attacker.HeldItem == "dragon-memory")
                    return ElementType.DRAGON;

                if (attacker.HeldItem == "dread-plate" || attacker.HeldItem == "dark-memory") return ElementType.DARK;

                if (attacker.HeldItem == "earth-plate" || attacker.HeldItem == "ground-memory")
                    return ElementType.GROUND;

                if (attacker.HeldItem == "fist-plate" || attacker.HeldItem == "fighting-memory")
                    return ElementType.FIGHTING;

                if (attacker.HeldItem == "flame-plate" || attacker.HeldItem == "burn-drive" ||
                    attacker.HeldItem == "fire-memory")
                    return ElementType.FIRE;

                if (attacker.HeldItem == "icicle-plate" || attacker.HeldItem == "chill-drive" ||
                    attacker.HeldItem == "ice-memory")
                    return ElementType.ICE;

                if (attacker.HeldItem == "insect-plate" || attacker.HeldItem == "bug-memory") return ElementType.BUG;

                if (attacker.HeldItem == "iron-plate" || attacker.HeldItem == "steel-memory") return ElementType.STEEL;

                if (attacker.HeldItem == "meadow-plate" || attacker.HeldItem == "grass-memory")
                    return ElementType.GRASS;

                if (attacker.HeldItem == "mind-plate" || attacker.HeldItem == "psychic-memory")
                    return ElementType.PSYCHIC;

                if (attacker.HeldItem == "pixie-plate" || attacker.HeldItem == "fairy-memory") return ElementType.FAIRY;

                if (attacker.HeldItem == "sky-plate" || attacker.HeldItem == "flying-memory") return ElementType.FLYING;

                if (attacker.HeldItem == "splash-plate" || attacker.HeldItem == "douse-drive" ||
                    attacker.HeldItem == "water-memory")
                    return ElementType.WATER;

                if (attacker.HeldItem == "spooky-plate" || attacker.HeldItem == "ghost-memory")
                    return ElementType.GHOST;

                if (attacker.HeldItem == "stone-plate" || attacker.HeldItem == "rock-memory") return ElementType.ROCK;

                if (attacker.HeldItem == "toxic-plate" || attacker.HeldItem == "poison-memory")
                    return ElementType.POISON;

                if (attacker.HeldItem == "zap-plate" || attacker.HeldItem == "shock-drive" ||
                    attacker.HeldItem == "electric-memory")
                    return ElementType.ELECTRIC;

                break;
            }
            case 223:
            {
                var hi = attacker.HeldItem.Get();
                if (new[] { "figy-berry", "tanga-berry", "cornn-berry", "enigma-berry" }.Contains(hi))
                    return ElementType.BUG;

                if (new[] { "iapapa-berry", "colbur-berry", "spelon-berry", "rowap-berry", "maranga-berry" }
                    .Contains(hi))
                    return ElementType.DARK;

                if (new[] { "aguav-berry", "haban-berry", "nomel-berry", "jaboca-berry" }.Contains(hi))
                    return ElementType.DRAGON;

                if (new[] { "pecha-berry", "wacan-berry", "wepear-berry", "belue-berry" }.Contains(hi))
                    return ElementType.ELECTRIC;

                if (new[] { "roseli-berry", "kee-berry" }.Contains(hi)) return ElementType.FAIRY;

                if (new[] { "leppa-berry", "chople-berry", "kelpsy-berry", "salac-berry" }.Contains(hi))
                    return ElementType.FIGHTING;

                if (new[] { "cheri-berry", "occa-berry", "bluk-berry", "watmel-berry" }.Contains(hi))
                    return ElementType.FIRE;

                if (new[] { "lum-berry", "coba-berry", "grepa-berry", "lansat-berry" }.Contains(hi))
                    return ElementType.FLYING;

                if (new[] { "mago-berry", "kasib-berry", "rabuta-berry", "custap-berry" }.Contains(hi))
                    return ElementType.GHOST;

                if (new[] { "rawst-berry", "rindo-berry", "pinap-berry", "liechi-berry" }.Contains(hi))
                    return ElementType.GRASS;

                if (new[] { "persim-berry", "shuca-berry", "hondew-berry", "apicot-berry" }.Contains(hi))
                    return ElementType.GROUND;

                if (new[] { "aspear-berry", "yache-berry", "pomeg-berry", "ganlon-berry" }.Contains(hi))
                    return ElementType.ICE;

                if (new[] { "oran-berry", "kebia-berry", "qualot-berry", "petaya-berry" }.Contains(hi))
                    return ElementType.POISON;

                if (new[] { "sitrus-berry", "payapa-berry", "tamato-berry", "starf-berry" }.Contains(hi))
                    return ElementType.PSYCHIC;

                if (new[] { "wiki-berry", "charti-berry", "magost-berry", "micle-berry" }.Contains(hi))
                    return ElementType.ROCK;

                if (new[] { "razz-berry", "babiri-berry", "pamtre-berry" }.Contains(hi)) return ElementType.STEEL;

                if (new[] { "chesto-berry", "passho-berry", "nanab-berry", "durin-berry" }.Contains(hi))
                    return ElementType.WATER;

                if (hi == "chilan-berry") return ElementType.NORMAL;

                break;
            }
            case 433 when attacker.Name == "Morpeko-hangry":
                return ElementType.DARK;
            case 441 when attacker.Grounded(battle):
            {
                switch (battle.Terrain.Item?.ToString())
                {
                    case "electric":
                        return ElementType.ELECTRIC;
                    case "grass":
                        return ElementType.GRASS;
                    case "misty":
                        return ElementType.FAIRY;
                    case "psychic":
                        return ElementType.PSYCHIC;
                }

                break;
            }
        }

        if (Id == 873)
            switch (attacker.Name)
            {
                case "Tauros-paldea":
                    return ElementType.FIGHTING;
                case "Tauros-aqua-paldea":
                    return ElementType.WATER;
                case "Tauros-blaze-paldea":
                    return ElementType.FIRE;
            }

        return Type;
    }

    /// <summary>
    ///     Calculates the priority value for this move.
    /// </summary>
    /// <returns>An int priority from -7 to 5.</returns>
    public int GetPriority(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
    {
        var priority = Priority;
        var currentType = GetType(attacker, defender, battle);

        if (Effect == 437 && attacker.Grounded(battle) && battle.Terrain.Item?.ToString() == "grassy") priority += 1;

        if (attacker.Ability() == Ability.GALE_WINGS && currentType == ElementType.FLYING &&
            attacker.Hp == attacker.StartingHp)
            priority += 1;

        if (attacker.Ability() == Ability.PRANKSTER && DamageClass == DamageClass.STATUS) priority += 1;

        if (attacker.Ability() == Ability.TRIAGE && IsAffectedByHealBlock()) priority += 3;

        return priority;
    }

    /// <summary>
    ///     Gets the chance for secondary effects to occur.
    /// </summary>
    /// <returns>An int from 0-100.</returns>
    public int? GetEffectChance(DuelPokemon.DuelPokemon attacker, DuelPokemon.DuelPokemon defender, Battle battle)
    {
        if (EffectChance == null) return 100;

        if (defender.Ability(attacker, this) == Ability.SHIELD_DUST) return 0;

        if (defender.HeldItem == "covert-cloak") return 0;

        if (attacker.Ability() == Ability.SHEER_FORCE) return 0;

        if (attacker.Ability() == Ability.SERENE_GRACE) return Math.Min(100, EffectChance.Value * 2);

        return EffectChance;
    }
}