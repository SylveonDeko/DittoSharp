using EeveeCore.Modules.Duels.Impl.Helpers;

namespace EeveeCore.Modules.Duels.Impl;

/// <summary>
///     Represents a genereric pokemon trainer.
///     This class outlines the methods that Trainer objects
///     should have, but should not be used directly.
/// </summary>
public class Trainer
{
    public Trainer(string name, List<DuelPokemon> party)
    {
        Name = name;
        Party = party;
        CurrentPokemon = party.Count > 0 ? party[0] : null;
        foreach (var poke in Party) poke.Owner = this;
        Event = new TaskCompletionSource<bool>();
        SelectedAction = null;
        MidTurnRemove = false;
        BatonPass = null;
        Spikes = 0;
        ToxicSpikes = 0;
        StealthRock = false;
        StickyWeb = false;
        LastIdx = 0;
        Wish = new ExpiringWish();
        AuroraVeil = new ExpiringEffect(0);
        LightScreen = new ExpiringEffect(0);
        Reflect = new ExpiringEffect(0);
        Mist = new ExpiringEffect(0);
        Safeguard = new ExpiringEffect(0);
        HealingWish = false;
        LunarDance = false;
        Tailwind = new ExpiringEffect(0);
        MudSport = new ExpiringEffect(0);
        WaterSport = new ExpiringEffect(0);
        Retaliate = new ExpiringEffect(0);
        FutureSight = new ExpiringItem();
        HasMegaEvolved = false;
        NumFainted = 0;
        NextSubstitute = 0;
    }

    public string Name { get; set; }
    public List<DuelPokemon> Party { get; set; }
    public DuelPokemon CurrentPokemon { get; set; }
    public TaskCompletionSource<bool> Event { get; set; }
    public TrainerAction SelectedAction { get; set; }

    // Boolean - True if this trainer's pokemon was removed in such a way that it needs to return mid-turn.
    public bool MidTurnRemove { get; set; }

    // Optional[BatonPass] - Holds data baton passed from the previous pokemon to the next, if applicable.
    public BatonPass BatonPass { get; set; }

    // Int - Stacks of spikes on this trainer's side of the field
    public int Spikes { get; set; }

    // Int - Stacks of toxic spikes on this trainer's side of the field
    public int ToxicSpikes { get; set; }

    // Boolean - Whether stealth rocks are on this trainer's side of the field
    public bool StealthRock { get; set; }

    // Boolean - Whether a sticky web is on this trainer's side of the field
    public bool StickyWeb { get; set; }

    // Int - The last index of Party that was selected
    public int LastIdx { get; set; }

    public ExpiringWish Wish { get; set; }
    public ExpiringEffect AuroraVeil { get; set; }
    public ExpiringEffect LightScreen { get; set; }
    public ExpiringEffect Reflect { get; set; }
    public ExpiringEffect Mist { get; set; }

    // ExpiringEffect - Stores the number of turns that pokes are protected from NV effects
    public ExpiringEffect Safeguard { get; set; }

    // Boolean - Whether the next poke to swap in should be restored via healing wish
    public bool HealingWish { get; set; }

    // Boolean - Whether the next poke to swap in should be restored via lunar dance
    public bool LunarDance { get; set; }

    // ExpiringEffect - Stores the number of turns that pokes have doubled speed
    public ExpiringEffect Tailwind { get; set; }

    // ExpiringEffect - Stores the number of turns that electric moves have 1/3 power
    public ExpiringEffect MudSport { get; set; }

    // ExpiringEffect - Stores the number of turns that fire moves have 1/3 power
    public ExpiringEffect WaterSport { get; set; }

    // ExpiringEffect - Stores the fact that a party member recently fainted.
    public ExpiringEffect Retaliate { get; set; }

    // ExpiringItem - Stores the turns until future sight attacks this trainer's pokemon.
    public ExpiringItem FutureSight { get; set; }

    // Boolean - Whether or not any of this trainer's pokemon have mega evolved yet this battle.
    public bool HasMegaEvolved { get; set; }

    // Int - Stores the number of times a pokemon in this trainer's party has fainted, including after being revived.
    public int NumFainted { get; set; }

    // Int - Stores the HP of the subsitute this trainer's next pokemon on the field will receive.
    public int NextSubstitute { get; set; }


    /// <summary>
    ///     Returns True if this trainer still has at least one pokemon that is alive.
    /// </summary>
    public bool HasAlivePokemon()
    {
        return Party.Any(poke => poke.Hp > 0);
    }

    /// <summary>
    ///     Updates this trainer for a new turn.
    ///     Returns a formatted message.
    /// </summary>
    public string NextTurn(Battle battle)
    {
        var msg = "";
        SelectedAction = null;
        MidTurnRemove = false;
        var hp = Wish.NextTurn();
        if (hp > 0 && CurrentPokemon != null) msg += CurrentPokemon.Heal(hp, "its wish");
        if (AuroraVeil.NextTurn()) msg += $"{Name}'s aurora veil wore off!\n";
        if (LightScreen.NextTurn()) msg += $"{Name}'s light screen wore off!\n";
        if (Reflect.NextTurn()) msg += $"{Name}'s reflect wore off!\n";
        if (Mist.NextTurn()) msg += $"{Name}'s mist wore off!\n";
        if (Safeguard.NextTurn()) msg += $"{Name}'s safeguard wore off!\n";
        if (Tailwind.NextTurn()) msg += $"{Name}'s tailwind died down!\n";
        if (MudSport.NextTurn()) msg += $"{Name}'s mud sport wore off!\n";
        if (WaterSport.NextTurn()) msg += $"{Name}'s water sport evaporated!\n";
        Retaliate.NextTurn();

        var futureSightData = FutureSight.Item;
        if (FutureSight.NextTurn() && CurrentPokemon != null)
        {
            msg += $"{CurrentPokemon.Name} took the future sight attack!\n";
            var futureSightAttacker = ((Tuple<DuelPokemon, Move.Move>)futureSightData).Item1;
            var futureSightMove = ((Tuple<DuelPokemon, Move.Move>)futureSightData).Item2;
            var futureSightResult = futureSightMove.Attack(futureSightAttacker, CurrentPokemon, battle);
            msg += futureSightResult.Item1;
        }

        return msg;
    }

    /// <summary>
    ///     Switch the currently active poke to the given slot.
    /// </summary>
    public void SwitchPoke(int slot, bool midTurn = false)
    {
        if (slot < 0 || slot >= Party.Count) throw new ArgumentException("out of bounds");
        if (!(Party[slot].Hp > 0)) throw new ArgumentException("no hp");
        CurrentPokemon = Party[slot];
        MidTurnRemove = false;
        LastIdx = slot;
        if (midTurn) CurrentPokemon.SwappedIn = true;
    }

    /// <summary>
    ///     Returns True if this trainer is a human player, False if it is an AI.
    /// </summary>
    public virtual bool IsHuman()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Returns a list of indexes of pokes in the party that can be swapped to.
    /// </summary>
    public List<int> ValidSwaps(DuelPokemon defender, Battle battle, bool checkTrap = true)
    {
        if (CurrentPokemon != null)
        {
            if (CurrentPokemon.TypeIds.Contains(ElementType.GHOST)) checkTrap = false;
            if (CurrentPokemon.HeldItem.Get() == "shed-shell") checkTrap = false;

            if (checkTrap)
            {
                if (CurrentPokemon.Trapping) return new List<int>();
                if (CurrentPokemon.Ingrain) return new List<int>();
                if (CurrentPokemon.FairyLock.Active() || defender.FairyLock.Active()) return new List<int>();
                if (CurrentPokemon.NoRetreat) return new List<int>();
                if (CurrentPokemon.Bind.Active() && CurrentPokemon.Substitute == 0) return new List<int>();
                if (defender.Ability() == Ability.SHADOW_TAG && CurrentPokemon.Ability() != Ability.SHADOW_TAG)
                    return new List<int>();
                if (defender.Ability() == Ability.MAGNET_PULL && CurrentPokemon.TypeIds.Contains(ElementType.STEEL))
                    return new List<int>();
                if (defender.Ability() == Ability.ARENA_TRAP && CurrentPokemon.Grounded(battle)) return new List<int>();
            }
        }

        var result = new List<int>();
        for (var idx = 0; idx < Party.Count; idx++)
            if (Party[idx].Hp > 0)
                result.Add(idx);

        if (result.Contains(LastIdx)) result.Remove(LastIdx);

        return result;
    }

    /// <summary>
    ///     https://www.smogon.com/dp/articles/move_restrictions
    ///     Returns
    ///     - ("forced", Move) - The move-action this trainer is FORCED to use.
    ///     - ("idxs", List[int]) - The indexes of moves that are valid to CHOOSE to use.
    ///     - ("struggle", List[int]) - If the user attempts to use any move, use struggle instead (no valid moves).
    /// </summary>
    public ValidMovesResult ValidMoves(DuelPokemon defender)
    {
        // Check if they are FORCED to use a certain move
        if (CurrentPokemon.LockedMove != null) return ValidMovesResult.Forced(CurrentPokemon.LockedMove.Move);

        // Remove all moves not matching a restriction
        var result = new List<int>();
        for (var idx = 0; idx < CurrentPokemon.Moves.Count; idx++)
        {
            var move = CurrentPokemon.Moves[idx];

            if (move.PP <= 0) continue;
            switch (move.DamageClass)
            {
                case DamageClass.STATUS when CurrentPokemon.HeldItem.Get() == "assault-vest":
                case DamageClass.STATUS when CurrentPokemon.Taunt.Active():
                    continue;
            }

            if (move.Effect == 247 && !CurrentPokemon.Moves.Where(m => m.Effect != 247).All(m => m.Used)) continue;
            if (CurrentPokemon.Disable.Active() && move == CurrentPokemon.Disable.Item) continue;
            if ((CurrentPokemon.HeldItem.Get() == "choice-scarf" ||
                 CurrentPokemon.HeldItem.Get() == "choice-band" ||
                 CurrentPokemon.HeldItem.Get() == "choice-specs" ||
                 CurrentPokemon.Ability() == Ability.GORILLA_TACTICS) &&
                CurrentPokemon.ChoiceMove != null && move != CurrentPokemon.ChoiceMove)
                continue;
            if (CurrentPokemon.Torment && CurrentPokemon.LastMove == move) continue;
            if (CurrentPokemon.LastMove is { Effect: 492 } &&
                CurrentPokemon.LastMove.Id == move.Id &&
                !CurrentPokemon.LastMoveFailed)
                continue;
            if (defender.Imprison && defender.Moves.Any(x => x.Id == move.Id)) continue;
            if (CurrentPokemon.HealBlock.Active() && move.IsAffectedByHealBlock()) continue;
            if (CurrentPokemon.Silenced.Active() && move.IsSoundBased()) continue;
            switch (move.Effect)
            {
                case 339 when !CurrentPokemon.AteBerry:
                case 453 when !CurrentPokemon.HeldItem.IsBerry():
                    continue;
            }

            if (CurrentPokemon.Encore.Active() && move != CurrentPokemon.Encore.Item) continue;

            result.Add(idx);
        }

        if (result.Count == 0) return ValidMovesResult.Struggle();

        return ValidMovesResult.ValidIndexes(result);
    }

    public abstract class TrainerAction
    {
        public abstract bool IsSwitch { get; }
    }

    public class MoveAction(Move.Move move) : TrainerAction
    {
        public Move.Move Move { get; set; } = move;

        public override bool IsSwitch => false;
    }

    public class SwitchAction(int switchIndex) : TrainerAction
    {
        public int SwitchIndex { get; set; } = switchIndex;

        public override bool IsSwitch => true;
    }
}

/// <summary>
///     Represents a pokemon trainer that is a discord.Member.
/// </summary>
public class MemberTrainer(IUser member, List<DuelPokemon> party) : Trainer(member.Username, party)
{
    public ulong Id { get; set; } = member.Id;
    public IUser Member { get; set; } = member;

    /// <summary>
    ///     Returns True if this trainer is a human player, False if it is an AI.
    /// </summary>
    public override bool IsHuman()
    {
        return true;
    }
}

/// <summary>
///     Represents a pokemon trainer that is a NPC.
/// </summary>
public class NPCTrainer(List<DuelPokemon> party) : Trainer("Trainer John", party)
{
    //// <summary>
    /// Request a normal move from this trainer AI.
    /// </summary>
    public void Move(DuelPokemon defender, Battle battle)
    {
        var moveResult = ValidMoves(defender);

        switch (moveResult.Type)
        {
            case ValidMovesResult.ResultType.ForcedMove:
                SelectedAction = new MoveAction(moveResult.ForcedMove);
                break;

            case ValidMovesResult.ResultType.Struggle:
                SelectedAction = new MoveAction(Impl.Move.Move.Struggle());
                break;

            case ValidMovesResult.ResultType.ValidIndexes:
                var moveData = moveResult.ValidMoveIndexes;
                var randomMoveIdx = moveData[new Random().Next(moveData.Count)];
                SelectedAction = new MoveAction(CurrentPokemon.Moves[randomMoveIdx]);
                break;
        }

        Event.SetResult(true);
        // TODO: npc ai?
    }

    /// <summary>
    ///     Request a swap choice from this trainer AI.
    /// </summary>
    public void Swap(DuelPokemon defender, Battle battle, bool midTurn = false)
    {
        var validSwaps = ValidSwaps(defender, battle, false);
        var pokeIdx = validSwaps[new Random().Next(validSwaps.Count)];
        SwitchPoke(pokeIdx, midTurn);

        // Also set this as the selected action if it's not a mid-turn swap
        if (!midTurn) SelectedAction = new SwitchAction(pokeIdx);

        Event.SetResult(true);
        // TODO: npc ai?
    }

    /// <summary>
    ///     Returns True if this trainer is a human player, False if it is an AI.
    /// </summary>
    public override bool IsHuman()
    {
        return false;
    }
}