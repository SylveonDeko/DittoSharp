using Ditto.Services.Impl;

namespace Duels
{
    /// <summary>
    /// Represents a battle between two trainers and their pokemon.
    /// This object holds all necessary information for a battle & runs the battle.
    /// </summary>
    public class Battle
    {
        public IInteractionContext Context { get; private set; }
        public IMessageChannel Channel { get; private set; }
        public Trainer Trainer1 { get; private set; }
        public Trainer Trainer2 { get; private set; }
        public int BgNum { get; private set; }
        public ExpiringEffect TrickRoom { get; private set; }
        public ExpiringEffect MagicRoom { get; private set; }
        public ExpiringEffect WonderRoom { get; private set; }
        public ExpiringEffect Gravity { get; private set; }
        public Weather Weather { get; private set; }
        public Terrain Terrain { get; private set; }
        public bool PlasmaFists { get; set; }
        public int Turn { get; set; }
        public int? LastMoveEffect { get; set; }
        public List<dynamic> MetronomeMoves { get; private set; }
        public Dictionary<(ElementType, ElementType), int> TypeEffectiveness { get; private set; }
        public bool InverseBattle { get; private set; }
        public string Msg { get; set; }
        private readonly IMongoService _mongoService;

        public Battle(IInteractionContext context, IMessageChannel channel, Trainer trainer1, Trainer trainer2, IMongoService mongoService, bool inverseBattle = false)
        {
            Context = context;
            Channel = channel;
            Trainer1 = trainer1;
            Trainer2 = trainer2;
            _mongoService = mongoService;

            // Initialize items for each Pokemon
            foreach (var poke in trainer1.Party)
            {
                poke.HeldItem.Battle = this;
            }
            foreach (var poke in trainer2.Party)
            {
                poke.HeldItem.Battle = this;
            }

            BgNum = new Random().Next(1, 5);
            TrickRoom = new ExpiringEffect(0);
            MagicRoom = new ExpiringEffect(0);
            WonderRoom = new ExpiringEffect(0);
            Gravity = new ExpiringEffect(0);
            Weather = new Weather(this);
            Terrain = new Terrain(this);
            PlasmaFists = false;
            Turn = 0;
            LastMoveEffect = null;
            MetronomeMoves = new List<dynamic>();
            TypeEffectiveness = new Dictionary<(ElementType, ElementType), int>();
            InverseBattle = inverseBattle;
            Msg = "";
        }

        /// <summary>
        /// Runs the duel.
        /// </summary>
        public async Task<Trainer> Run()
        {
            Msg = "";

            // Moves which are immune to metronome (implementation pending)
            var immuneIds = new List<int>
            {
                68, 102, 119, 144, 165, 166, 168, 173, 182, 194, 197, 203, 214, 243, 264, 266,
                267, 270, 271, 274, 289, 343, 364, 382, 383, 415, 448, 469, 476, 495, 501, 511,
                516, 546, 547, 548, 553, 554, 555, 557, 561, 562, 578, 588, 591, 592, 593, 596,
                606, 607, 614, 615, 617, 621, 661, 671, 689, 690, 704, 705, 712, 720, 721, 722
            };

            // Moves which are not coded in the bot (implementation pending)
            var uncodedIds = new List<int>
            {
                266, 270, 476, 495, 502, 511, 597, 602, 603, 607, 622, 623, 624, 625, 626, 627,
                628, 629, 630, 631, 632, 633, 634, 635, 636, 637, 638, 639, 640, 641, 642, 643,
                644, 645, 646, 647, 648, 649, 650, 651, 652, 653, 654, 655, 656, 657, 658, 671,
                695, 696, 697, 698, 699, 700, 701, 702, 703, 719, 723, 724, 725, 726, 727, 728,
                811, 10001, 10002, 10003, 10004, 10005, 10006, 10007, 10008, 10009, 10010, 10011,
                10012, 10013, 10014, 10015, 10016, 10017, 10018
            };

            // Combine ignored move IDs
            var ignoredIds = immuneIds.Union(uncodedIds).ToList();

            // Load move data and type effectiveness
            MetronomeMoves = await DataHandler.Find(Context, "moves", new Dictionary<string, object>
            {
                ["id"] = new Dictionary<string, object> { ["$nin"] = ignoredIds }
            });

            var typeEffectivenessData = await DataHandler.Find(Context, "type_effectiveness", new Dictionary<string, object>());
            foreach (var te in typeEffectivenessData)
            {
                TypeEffectiveness[(
                    (ElementType)te["damage_type_id"],
                    (ElementType)te["target_type_id"]
                )] = te["damage_factor"];
            }

            // Initial Pokemon send-out
            if (Trainer1.CurrentPokemon.GetRawSpeed() > Trainer2.CurrentPokemon.GetRawSpeed())
            {
                Msg += Trainer1.CurrentPokemon.SendOut(Trainer2.CurrentPokemon, this);
                Msg += Trainer2.CurrentPokemon.SendOut(Trainer1.CurrentPokemon, this);
            }
            else
            {
                Msg += Trainer2.CurrentPokemon.SendOut(Trainer1.CurrentPokemon, this);
                Msg += Trainer1.CurrentPokemon.SendOut(Trainer2.CurrentPokemon, this);
            }

            await SendMsg();

            Trainer winner = null;

            while (true)
            {
                // Swap pokes for any users w/o an active poke
                while (Trainer1.CurrentPokemon == null || Trainer2.CurrentPokemon == null)
                {
                    var swapped1 = false;
                    var swapped2 = false;

                    if (Trainer1.CurrentPokemon == null)
                    {
                        swapped1 = true;
                        winner = await RunSwap(Trainer1, Trainer2);
                        if (winner != null)
                            break;
                    }

                    if (Trainer2.CurrentPokemon == null)
                    {
                        swapped2 = true;
                        winner = await RunSwap(Trainer2, Trainer1);
                        if (winner != null)
                            break;
                    }

                    // Send out the pokes that were just swapped to
                    if (swapped1 && swapped2)
                    {
                        if (Trainer1.CurrentPokemon.GetRawSpeed() > Trainer2.CurrentPokemon.GetRawSpeed())
                        {
                            Msg += Trainer1.CurrentPokemon.SendOut(Trainer2.CurrentPokemon, this);
                            if (!Trainer1.HasAlivePokemon())
                            {
                                Msg += $"{Trainer2.Name} wins!\n";
                                winner = Trainer2;
                                break;
                            }
                            Msg += Trainer2.CurrentPokemon.SendOut(Trainer1.CurrentPokemon, this);
                            if (!Trainer2.HasAlivePokemon())
                            {
                                Msg += $"{Trainer1.Name} wins!\n";
                                winner = Trainer1;
                                break;
                            }
                        }
                        else
                        {
                            Msg += Trainer2.CurrentPokemon.SendOut(Trainer1.CurrentPokemon, this);
                            if (!Trainer2.HasAlivePokemon())
                            {
                                Msg += $"{Trainer1.Name} wins!\n";
                                winner = Trainer1;
                                break;
                            }
                            Msg += Trainer1.CurrentPokemon.SendOut(Trainer2.CurrentPokemon, this);
                            if (!Trainer1.HasAlivePokemon())
                            {
                                Msg += $"{Trainer2.Name} wins!\n";
                                winner = Trainer2;
                                break;
                            }
                        }
                    }
                    else if (swapped1)
                    {
                        Msg += Trainer1.CurrentPokemon.SendOut(Trainer2.CurrentPokemon, this);
                        if (!Trainer1.HasAlivePokemon())
                        {
                            Msg += $"{Trainer2.Name} wins!\n";
                            winner = Trainer2;
                            break;
                        }
                    }
                    else if (swapped2)
                    {
                        Msg += Trainer2.CurrentPokemon.SendOut(Trainer1.CurrentPokemon, this);
                        if (!Trainer2.HasAlivePokemon())
                        {
                            Msg += $"{Trainer1.Name} wins!\n";
                            winner = Trainer1;
                            break;
                        }
                    }
                }

                // Handle breaking out of the main game loop when a winner happens in the poke select loop
                if (winner != null)
                    break;

                // Get trainer actions
                await SendMsg();

                Trainer1.Event = new TaskCompletionSource<bool>();
                Trainer2.Event = new TaskCompletionSource<bool>();

                if (!Trainer1.IsHuman())
                {
                    ((NPCTrainer)Trainer1).Move(Trainer2.CurrentPokemon, this);
                }
                if (!Trainer2.IsHuman())
                {
                    ((NPCTrainer)Trainer2).Move(Trainer1.CurrentPokemon, this);
                }

                var battleView = await DataHandler.GenerateMainBattleMessage(this);
                await Trainer1.Event.Task;
                await Trainer2.Event.Task;
                battleView.StopAsync();

                // Check for forfeits
                if (Trainer1.SelectedAction == null && Trainer2.SelectedAction == null)
                {
                    await Channel.SendMessageAsync("Both players forfeited...");
                    return null; // TODO: handle this case better
                }
                if (Trainer1.SelectedAction == null)
                {
                    Msg += $"{Trainer1.Name} forfeited, {Trainer2.Name} wins!\n";
                    winner = Trainer2;
                    break;
                }
                if (Trainer2.SelectedAction == null)
                {
                    Msg += $"{Trainer2.Name} forfeited, {Trainer1.Name} wins!\n";
                    winner = Trainer1;
                    break;
                }

                // Run setup for both pokemon
                var (t1, t2) = WhoFirst();
                if (t1.CurrentPokemon != null && t2.CurrentPokemon != null)
                {
                    if (!(t1.SelectedAction.IsSwitch))
                    {
                        Msg += ((Trainer.MoveAction)t1.SelectedAction).Move.Setup(t1.CurrentPokemon, t2.CurrentPokemon, this);
                    }
                }
                if (!t1.HasAlivePokemon())
                {
                    Msg += $"{t2.Name} wins!\n";
                    winner = t2;
                    break;
                }
                if (!t2.HasAlivePokemon())
                {
                    Msg += $"{t1.Name} wins!\n";
                    winner = t1;
                    break;
                }
                if (t1.CurrentPokemon != null && t2.CurrentPokemon != null)
                {
                    if (!(t2.SelectedAction.IsSwitch))
                    {
                        Msg += ((Trainer.MoveAction)t2.SelectedAction).Move.Setup(t2.CurrentPokemon, t1.CurrentPokemon, this);
                    }
                }
                if (!t2.HasAlivePokemon())
                {
                    Msg += $"{t1.Name} wins!\n";
                    winner = t1;
                    break;
                }
                if (!t1.HasAlivePokemon())
                {
                    Msg += $"{t2.Name} wins!\n";
                    winner = t2;
                    break;
                }

                // Run moves for both pokemon
                // Trainer 1's move
                var ranMegas = false;
                if (!(t1.SelectedAction.IsSwitch))
                {
                    HandleMegas(t1, t2);
                    ranMegas = true;
                }

                if (t1.CurrentPokemon != null && t2.CurrentPokemon != null)
                {
                    if (t1.SelectedAction is Trainer.SwitchAction action)
                    {
                        Msg += t1.CurrentPokemon.Remove(this);
                        t1.SwitchPoke(action.SwitchIndex, midTurn: true);
                        Msg += t1.CurrentPokemon.SendOut(t2.CurrentPokemon, this);
                        if (t1.CurrentPokemon != null)
                        {
                            t1.CurrentPokemon.HasMoved = true;
                        }
                    }
                    else
                    {
                        Msg += ((Trainer.MoveAction)t1.SelectedAction).Move.Use(t1.CurrentPokemon, t2.CurrentPokemon, this);
                    }
                }
                if (!t1.HasAlivePokemon())
                {
                    Msg += $"{t2.Name} wins!\n";
                    winner = t2;
                    break;
                }
                if (!t2.HasAlivePokemon())
                {
                    Msg += $"{t1.Name} wins!\n";
                    winner = t1;
                    break;
                }

                // Pokes who die do NOT get attacked, but pokes who retreat *do*
                if (t1.MidTurnRemove)
                {
                    winner = await RunSwap(t1, t2, midTurn: true);
                    if (winner != null)
                        break;
                }

                // EDGE CASE - Moves that DO NOT target the opponent (and swapping) SHOULD run
                // even if there is no other poke on the field.
                if (t1.CurrentPokemon == null && t2.CurrentPokemon != null &&
                    (t2.SelectedAction.IsSwitch || !(t2.SelectedAction as Trainer.MoveAction).Move.TargetsOpponent()))
                {
                    winner = await RunSwap(t1, t2, midTurn: true);
                    if (winner != null)
                        break;
                }

                Msg += "\n";

                // Trainer 2's move
                if (!ranMegas && !(t2.SelectedAction.IsSwitch))
                {
                    HandleMegas(t1, t2);
                    ranMegas = true;
                }

                if (t1.CurrentPokemon != null && t2.CurrentPokemon != null)
                {
                    if (t2.SelectedAction.IsSwitch)
                    {
                        Msg += t2.CurrentPokemon.Remove(this);
                        t2.SwitchPoke((t2.SelectedAction as Trainer.SwitchAction).SwitchIndex, midTurn: true);
                        Msg += t2.CurrentPokemon.SendOut(t1.CurrentPokemon, this);
                        if (t2.CurrentPokemon != null)
                        {
                            t2.CurrentPokemon.HasMoved = true;
                        }
                    }
                    else
                    {
                        Msg += ((Trainer.MoveAction)t2.SelectedAction).Move.Use(t2.CurrentPokemon, t1.CurrentPokemon, this);
                    }
                }
                if (!t2.HasAlivePokemon())
                {
                    Msg += $"{t1.Name} wins!\n";
                    winner = t1;
                    break;
                }
                if (!t1.HasAlivePokemon())
                {
                    Msg += $"{t2.Name} wins!\n";
                    winner = t2;
                    break;
                }

                Msg += "\n";
                if (t2.MidTurnRemove)
                {
                    // This DOES need to be here, otherwise end of turn effects aren't handled right
                    winner = await RunSwap(t2, t1, midTurn: true);
                    if (winner != null)
                        break;
                }

                if (!t2.HasAlivePokemon())
                {
                    Msg += $"{t1.Name} wins!\n";
                    winner = t1;
                    break;
                }
                if (!t1.HasAlivePokemon())
                {
                    Msg += $"{t2.Name} wins!\n";
                    winner = t2;
                    break;
                }

                if (!ranMegas)
                {
                    HandleMegas(t1, t2);
                }

                // Progress turns
                Turn += 1;
                PlasmaFists = false;
                if (Weather.NextTurn())
                {
                    Msg += "The weather cleared!\n";
                }
                if (Terrain.NextTurn())
                {
                    Msg += "The terrain cleared!\n";
                }
                LastMoveEffect = null;

                (t1, t2) = WhoFirst(checkMove: false);
                Msg += t1.NextTurn(this);
                if (t1.CurrentPokemon != null)
                {
                    Msg += t1.CurrentPokemon.NextTurn(t2.CurrentPokemon, this);
                }
                if (!t1.HasAlivePokemon())
                {
                    Msg += $"{t2.Name} wins!\n";
                    winner = t2;
                    break;
                }
                if (!t2.HasAlivePokemon())
                {
                    Msg += $"{t1.Name} wins!\n";
                    winner = t1;
                    break;
                }

                Msg += t2.NextTurn(this);
                if (t2.CurrentPokemon != null)
                {
                    Msg += t2.CurrentPokemon.NextTurn(t1.CurrentPokemon, this);
                }
                if (!t2.HasAlivePokemon())
                {
                    Msg += $"{t1.Name} wins!\n";
                    winner = t1;
                    break;
                }
                if (!t1.HasAlivePokemon())
                {
                    Msg += $"{t2.Name} wins!\n";
                    winner = t2;
                    break;
                }

                if (TrickRoom.NextTurn())
                {
                    Msg += "The Dimensions returned back to normal!\n";
                }
                if (Gravity.NextTurn())
                {
                    Msg += "Gravity returns to normal!\n";
                }
                if (MagicRoom.NextTurn())
                {
                    Msg += "The room returns to normal, and held items regain their effect!\n";
                }
                if (WonderRoom.NextTurn())
                {
                    Msg += "The room returns to normal, and stats swap back to what they were before!\n";
                }
            }

            // The game is over, and we broke out before sending, send the remaining cache
            await SendMsg();
            return winner;
        }

        /// <summary>
        /// Determines which move should go.
        /// Returns the two trainers and their moves, in the order they should go.
        /// </summary>
        public (Trainer, Trainer) WhoFirst(bool checkMove = true)
        {
            var T1FIRST = (Trainer1, Trainer2);
            var T2FIRST = (Trainer2, Trainer1);

            if (Trainer1.CurrentPokemon == null || Trainer2.CurrentPokemon == null)
            {
                return T1FIRST;
            }

            var speed1 = Trainer1.CurrentPokemon.GetSpeed(this);
            var speed2 = Trainer2.CurrentPokemon.GetSpeed(this);

            // Pokes that are switching go before pokes making other moves
            if (checkMove)
            {
                switch (Trainer1.SelectedAction)
                {
                    case Trainer.SwitchAction when Trainer2.SelectedAction is Trainer.SwitchAction:
                    {
                        if (Trainer1.CurrentPokemon.GetRawSpeed() > Trainer2.CurrentPokemon.GetRawSpeed())
                        {
                            return T1FIRST;
                        }
                        return T2FIRST;
                    }
                    case Trainer.SwitchAction:
                        return T1FIRST;
                }

                if (Trainer2.SelectedAction.IsSwitch)
                {
                    return T2FIRST;
                }
            }

            // Priority brackets & abilities
            if (checkMove)
            {
                var prio1 = (Trainer1.SelectedAction as Trainer.MoveAction).Move.GetPriority(Trainer1.CurrentPokemon, Trainer2.CurrentPokemon, this);
                var prio2 = (Trainer2.SelectedAction as Trainer.MoveAction).Move.GetPriority(Trainer2.CurrentPokemon, Trainer1.CurrentPokemon, this);

                if (prio1 > prio2)
                {
                    return T1FIRST;
                }
                if (prio2 > prio1)
                {
                    return T2FIRST;
                }

                var t1Quick = false;
                var t2Quick = false;

                // Quick draw/claw
                if (Trainer1.CurrentPokemon.Ability() == Ability.QUICK_DRAW &&
                    (Trainer1.SelectedAction as Trainer.MoveAction).Move.DamageClass != DamageClass.STATUS &&
                    new Random().Next(1, 101) <= 30)
                {
                    t1Quick = true;
                }
                if (Trainer2.CurrentPokemon.Ability() == Ability.QUICK_DRAW &&
                    (Trainer2.SelectedAction as Trainer.MoveAction).Move.DamageClass != DamageClass.STATUS &&
                    new Random().Next(1, 101) <= 30)
                {
                    t2Quick = true;
                }
                if (Trainer1.CurrentPokemon.HeldItem == "quick-claw" &&
                    new Random().Next(1, 101) <= 20)
                {
                    t1Quick = true;
                }
                if (Trainer2.CurrentPokemon.HeldItem == "quick-claw" &&
                    new Random().Next(1, 101) <= 20)
                {
                    t2Quick = true;
                }

                // If both pokemon activate a quick, priority bracket proceeds as normal
                if (t1Quick && !t2Quick)
                {
                    return T1FIRST;
                }
                if (t2Quick && !t1Quick)
                {
                    return T2FIRST;
                }

                // Move last in prio bracket
                var t1Slow = false;
                var t2Slow = false;

                if (Trainer1.CurrentPokemon.Ability() == Ability.STALL)
                {
                    t1Slow = true;
                }
                if (Trainer1.CurrentPokemon.Ability() == Ability.MYCELIUM_MIGHT &&
                    (Trainer1.SelectedAction as Trainer.MoveAction).Move.DamageClass == DamageClass.STATUS)
                {
                    t1Slow = true;
                }
                if (Trainer2.CurrentPokemon.Ability() == Ability.STALL)
                {
                    t2Slow = true;
                }
                if (Trainer2.CurrentPokemon.Ability() == Ability.MYCELIUM_MIGHT &&
                    (Trainer2.SelectedAction as Trainer.MoveAction).Move.DamageClass == DamageClass.STATUS)
                {
                    t2Slow = true;
                }

                switch (t1Slow)
                {
                    case true when t2Slow:
                    {
                        if (speed1 == speed2)
                        {
                            return new Random().Next(2) == 0 ? T1FIRST : T2FIRST;
                        }
                        if (speed1 > speed2)
                        {
                            return T2FIRST;
                        }
                        return T1FIRST;
                    }
                    case true:
                        return T2FIRST;
                }

                if (t2Slow)
                {
                    return T1FIRST;
                }
            }

            // Equal speed
            if (speed1 == speed2)
            {
                return new Random().Next(2) == 0 ? T1FIRST : T2FIRST;
            }

            // Trick room
            if (TrickRoom.Active())
            {
                if (speed1 > speed2)
                {
                    return T2FIRST;
                }
                return T1FIRST;
            }

            // Default handling
            if (speed1 > speed2)
            {
                return T1FIRST;
            }
            return T2FIRST;
        }

        /// <summary>
        /// Send the msg in a boilerplate embed.
        /// Handles the message being too long.
        /// </summary>
        public async Task SendMsg()
        {
            await DataHandler.GenerateTextBattleMessage(this);
        }

        /// <summary>
        /// Called when swapper does not have a pokemon selected, and needs a new one.
        /// Prompts the swapper to pick a pokemon.
        /// If midTurn is set to True, the pokemon is being swapped in the middle of a turn (NOT at the start of a turn).
        /// Returns null if the trainer swapped, and the trainer that won if they did not.
        /// </summary>
        public async Task<Trainer> RunSwap(Trainer swapper, Trainer otherTrainer, bool midTurn = false)
        {
            await SendMsg();

            swapper.Event = new TaskCompletionSource<bool>();
            if (swapper.IsHuman())
            {
                SwapPromptView swapView = new SwapPromptView(swapper, otherTrainer, this, midTurn);
                await Channel.SendMessageAsync(
                    $"{swapper.Name}, pick a pokemon to swap to!",
                    components: swapView.Build());
            }
            else
            {
                ((NPCTrainer)swapper).Swap(otherTrainer.CurrentPokemon, this, midTurn);
            }

            try
            {
                await swapper.Event.Task.WaitAsync(TimeSpan.FromSeconds(60)); // 60 second timeout
            }
            catch (TimeoutException)
            {
                Msg += $"{swapper.Name} did not select a poke, {otherTrainer.Name} wins!\n";
                return otherTrainer;
            }

            if (swapper.IsHuman())
            {
                // Stop the view (implementation pending)
            }

            if (swapper.CurrentPokemon == null)
            {
                Msg += $"{swapper.Name} did not select a poke, {otherTrainer.Name} wins!\n";
                return otherTrainer;
            }

            if (midTurn)
            {
                Msg += swapper.CurrentPokemon.SendOut(otherTrainer.CurrentPokemon, this);
                if (swapper.CurrentPokemon != null)
                {
                    swapper.CurrentPokemon.HasMoved = true;
                }
            }

            return null;
        }

        /// <summary>
        /// Handle mega evolving pokemon who mega evolve this turn.
        /// </summary>
        public void HandleMegas(Trainer t1, Trainer t2)
        {
            foreach (var (at, dt) in new[] { (t1, t2), (t2, t1) })
            {
                if (at.CurrentPokemon is { ShouldMegaEvolve: true })
                {
                    // Bit of a hack, since it is in its mega form and dashes are removed from `name`, it will show as "<poke> mega evolved!".
                    if ((at.CurrentPokemon.HeldItem == "mega-stone" || at.CurrentPokemon.Name == "Rayquaza") &&
                        at.CurrentPokemon.Form(at.CurrentPokemon.Name + "-mega"))
                    {
                        Msg += $"{at.CurrentPokemon.DisplayName} evolved!\n";
                    }
                    else if (at.CurrentPokemon.HeldItem == "mega-stone-x" &&
                             at.CurrentPokemon.Form(at.CurrentPokemon.Name + "-mega-x"))
                    {
                        Msg += $"{at.CurrentPokemon.DisplayName} evolved!\n";
                    }
                    else if (at.CurrentPokemon.HeldItem == "mega-stone-y" &&
                             at.CurrentPokemon.Form(at.CurrentPokemon.Name + "-mega-y"))
                    {
                        Msg += $"{at.CurrentPokemon.DisplayName} evolved!\n";
                    }
                    else
                    {
                        throw new InvalidOperationException("expected to mega evolve but no valid mega condition");
                    }

                    at.CurrentPokemon.AbilityId = at.CurrentPokemon.MegaAbilityId;
                    at.CurrentPokemon.StartingAbilityId = at.CurrentPokemon.MegaAbilityId;
                    at.CurrentPokemon.TypeIds = new List<ElementType>(at.CurrentPokemon.MegaTypeIds);
                    at.CurrentPokemon.StartingTypeIds = new List<ElementType>(at.CurrentPokemon.MegaTypeIds);
                    Msg += at.CurrentPokemon.SendOutAbility(dt.CurrentPokemon, this);
                    at.HasMegaEvolved = true;
                }
            }
        }
    }
}