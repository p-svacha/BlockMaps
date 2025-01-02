using BlockmapFramework;
using CaptureTheFlag.AI;
using CaptureTheFlag.Network;
using CaptureTheFlag.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CtfMatch
    {
        public CtfGame Game { get; private set; }
        public World World { get; private set; }
        public CtfMatchLobby MatchInfo { get; private set; }
        public CtfMatchType MatchType => MatchInfo.MatchType;

        // Rules
        public const float NEUTRAL_ZONE_SIZE = 0.1f; // size of neutral zone strip in %
        public const int JAIL_ZONE_RADIUS = 3;
        public const int JAIL_ZONE_MIN_FLAG_DISTANCE = 9; // minimum distance from jail zone center to flag
        public const int JAIL_ZONE_MAX_FLAG_DISTANCE = 11; // maximum distance from jail zone center to flag
        public const int JAIL_TIME = 6; // Amount of turns a character spends in jail after being tagged
        public const float FLAG_ZONE_RADIUS = 7.5f;  // Amount of tiles around flag that can't be entered by own team
        public static List<Color> PlayerColors = new List<Color>()
        {
            Color.blue,
            Color.red
        };

        // Match setting option
        public static List<CTFMapGenerator> WorldGenerators = new List<CTFMapGenerator>()
        {
            new CTFMapGenerator_Forest(),
        };
        public static Dictionary<string, int> MapSizes = new Dictionary<string, int>()
        {
            { "Tiny", 4 },
            { "Small", 6 },
            { "Medium", 8 },
            { "Big", 10 },
            { "Large", 12 },
        };

        // Elements
        public CtfMatchUi UI => Game.MatchUI;
        public LineRenderer PathPreview => Game.PathPreviewRenderer;
        private CTFMapGenerator MapGenerator;

        public List<CtfCharacter> Characters;
        public CtfCharacter SelectedCharacter { get; private set; }
        public CharacterAction HoveredAction { get; set; }
        private HashSet<BlockmapNode> HighlightedNodes = new();

        // Game attributes
        public Zone LocalPlayerZone => LocalPlayer.Territory;
        public Zone NeutralZone { get; private set; }
        public Zone OpponentZone => Opponent.Territory;

        public List<Player> Players;
        private bool LocalPlayerIsPlayer1;
        public Player LocalPlayer { get; private set; }
        public Player Opponent { get; private set; }

        // Match state
        public MatchState State { get; private set; }
        public bool IsMatchRunning => (State != MatchState.GeneratingWorld && State != MatchState.InitializingWorld && State != MatchState.MatchReadyToStart && State != MatchState.GameFinished);
        public int CurrentTick { get; private set; }

        // Display options
        public bool IsVisionCutoffEnabled { get; private set; }

        // Options
        public bool DevMode { get; private set; }

        // Cache
        private Texture2D ReachableTileOverlay;

        // Multiplayer
        protected const int MultiplayerActionTickOffset = 10;
        List<System.Tuple<CharacterAction, int>> QueuedCharacterActions = new List<System.Tuple<CharacterAction, int>>(); // Stores which actions should be performed at what tick

        // Double click
        private float DoubleClickTimeThreshold = 0.5f;
        private float LastSelectClickTime = 0f;

        #region Initialize

        public CtfMatch(CtfGame game)
        {
            Game = game;

            // Load textures
            ReachableTileOverlay = MaterialManager.LoadTexture("CaptureTheFlag/Textures/ReachableTileOverlay");
        }

        public void InitializeGame(CtfMatchLobby matchInfo)
        {
            MatchInfo = matchInfo;
            LocalPlayerIsPlayer1 = (MatchType == CtfMatchType.Singleplayer) || (matchInfo.Clients[0].ClientId == NetworkClient.Instance.ClientId);

            // Network
            if (MatchType == CtfMatchType.Multiplayer) NetworkClient.Instance.Match = this;

            // Create players
            Players = new List<Player>();
            Players.Add(new Player(matchInfo.Clients[0]));
            if (MatchType == CtfMatchType.Singleplayer) Players.Add(new AIPlayer(matchInfo.Clients[1]));
            if (MatchType == CtfMatchType.Multiplayer) Players.Add(new Player(matchInfo.Clients[1]));

            // Start world generation
            Game.LoadingScreenOverlay.SetActive(true);
            MapGenerator = WorldGenerators[matchInfo.Settings.WorldGeneratorIndex];
            MapGenerator.StartGeneration(matchInfo.Settings.WorldSize, matchInfo.Settings.Seed, onDoneCallback: OnWorldGenerationDone);
            State = MatchState.GeneratingWorld;
        }

        private void OnWorldGenerationDone()
        {
            // Start world initialization
            if (World != null) GameObject.Destroy(World.WorldObject);
            World = MapGenerator.World;
            World.Initialize(OnWorldInitializationDone);

            // Set map zones
            NeutralZone = World.GetZone(id: 1);

            // Initialize players
            Players[0].OnWorldGenerationDone(World.GetActor(id: 1), World.GetZone(id: 0), World.GetZone(id: 3), World.GetZone(id: 4));
            Players[1].OnWorldGenerationDone(World.GetActor(id: 2), World.GetZone(id: 2), World.GetZone(id: 5), World.GetZone(id: 6));

            if (LocalPlayerIsPlayer1)
            {
                LocalPlayer = Players[0];
                Opponent = Players[1];
            }
            else
            {
                LocalPlayer = Players[1];
                Opponent = Players[0];
            }

            LocalPlayer.Opponent = Opponent;
            Opponent.Opponent = LocalPlayer;

            // Register all characters
            Characters = new List<CtfCharacter>();
            foreach (Player p in Players) Characters.AddRange(p.Characters);

            State = MatchState.InitializingWorld;
        }

        private void OnWorldInitializationDone()
        {
            FinalizeMatchInitialization();

            if (MatchType == CtfMatchType.Singleplayer) StartGame(); // Instantly start the match in singleplayer
            if (MatchType == CtfMatchType.Multiplayer)
            {
                UI.ShowRedNotificationText("Waiting for other player");
                NetworkClient.Instance.SendMessage(new NetworkMessage("PlayerReadyToStartMatch")); // Inform the server that we are ready
            }
        }

        /// <summary>
        /// Gets called when the world is fully generated and initialized. After this function the match should be immediately ready to start.
        /// </summary>
        private void FinalizeMatchInitialization()
        {
            // Vision
            World.ShowTextures(true);
            World.ShowGridOverlay(false);
            World.ShowTileBlending(true);
            World.SetActiveVisionActor(LocalPlayer.Actor);
            Game.LoadingScreenOverlay.SetActive(false);

            // Camera
            World.CameraJumpToFocusEntity(LocalPlayer.Flag);

            // Notify match readiness
            foreach (Player p in Players) p.OnMatchReady(this);
            UI.OnMatchReady();

            State = MatchState.MatchReadyToStart;
        }

        private void StartGame()
        {
            UI.HideRedNotificationText();
            CurrentTick = 0;
            if (MatchType == CtfMatchType.Multiplayer && LocalPlayerIsPlayer1) CurrentTick = -30; // Offset for server host to counteract ping
            StartPlayerTurn();
        }

        #endregion

        #region Game Loop

        public void OnActionDone(CharacterAction action)
        {
            // Check game over
            if (IsGameOver(out Player winner)) EndGame(won: winner == LocalPlayer);

            // Reduce action/stamina based on action cost
            action.Character.ReduceActionAndStamina(action.Cost);

            // Send all colliding characters to jail (depending on map half)
            BlockmapNode node = action.Character.OriginNode;
            List<CtfCharacter> characters = GetCharacters(node);
            if(LocalPlayerZone.ContainsNode(node) && characters.Any(x => x.Owner == LocalPlayer)) // send opponent characters to their jail
            {
                foreach (CtfCharacter opponentCharacter in characters.Where(x => x.Owner == Opponent))
                    SendToJail(opponentCharacter);
            }
            if (OpponentZone.ContainsNode(node) && characters.Any(x => x.Owner == Opponent)) // send own characters to own jail
            {
                foreach (CtfCharacter ownCharacter in characters.Where(x => x.Owner == LocalPlayer))
                    SendToJail(ownCharacter);
            }

            // Update possible actions for all characters
            foreach (CtfCharacter character in Characters) character.UpdatePossibleActions();


            // Update UI
            if (action.Character.Owner == LocalPlayer) UI.UpdateSelectionPanel(action.Character);
            if (SelectedCharacter == action.Character) RefreshSelectedCharacter(); // Reselect character to update highlighted nodes and actions
        }

        private void StartPlayerTurn()
        {
            State = MatchState.PlayerTurn;
            foreach (Player p in Players) p.OnStartTurn();
            UI.UpdateSelectionPanels();
        }

        public void EndPlayerTurn()
        {
            if (State != MatchState.PlayerTurn) return;
            if (LocalPlayer.Characters.Any(x => x.IsInAction)) return;
            DeselectCharacter();

            if (MatchType == CtfMatchType.Singleplayer) StartNpcTurn();
            if (MatchType == CtfMatchType.Multiplayer)
            {
                NetworkClient.Instance.SendMessage(new NetworkMessage("TurnEnded"));
                UI.ShowRedNotificationText("Waiting for other player");
                State = MatchState.WaitingForOtherPlayerTurn;
            }
        }

        /// <summary>
        /// Starts the turn where all bots and neutral characters make their move.
        /// </summary>
        public void StartNpcTurn()
        {
            State = MatchState.NpcTurn;

            UI.ShowRedNotificationText("NPC Turn");

            if (MatchType == CtfMatchType.Singleplayer) ((AIPlayer)Opponent).StartTurn();
        }
        /// <summary>
        /// Ends the turn where all bots and neutral characters make their move.
        /// </summary>

        public void EndNpcTurn()
        {
            UI.HideRedNotificationText();
            StartPlayerTurn();
        }

        public void EndGame(bool won)
        {
            State = MatchState.GameFinished;
            World.Camera.Unfollow();
            World.SetActiveVisionActor(null);
            UI.ShowEndGameScreen(won ? "You won!" : "You lost.");
        }

        private bool IsGameOver(out Player winner)
        {
            winner = null;
            if(LocalPlayer.Characters.Any(x => x.Node == Opponent.Flag.OriginNode))
            {
                winner = LocalPlayer;
                return true;
            }
            if (Opponent.Characters.Any(x => x.Node == LocalPlayer.Flag.OriginNode))
            {
                winner = Opponent;
                return true;
            }
            return false;
        }

        #endregion

        #region Update / Tick

        // Called every frame: Use this for detecting and handling client-side stuff like inputs etc.
        public void Update()
        {
            World?.Update();

            HelperFunctions.UnfocusNonInputUiElements();

            // V - Vision (debug)
            if (DevMode && Input.GetKeyDown(KeyCode.V))
            {
                if (World.ActiveVisionActor == LocalPlayer.Actor) World.SetActiveVisionActor(null);
                else if (World.ActiveVisionActor == null) World.SetActiveVisionActor(LocalPlayer.Opponent.Actor);
                else World.SetActiveVisionActor(LocalPlayer.Actor);
            }

            switch (State)
            {
                case MatchState.GeneratingWorld:
                    MapGenerator.UpdateGeneration();
                    break;

                case MatchState.InitializingWorld:
                    // Handled in World.Update()
                    break;

                case MatchState.MatchReadyToStart: // Multiplayer only
                    if (Players.All(p => p.ReadyToStartMultiplayerMatch)) StartGame();
                    break;

                case MatchState.PlayerTurn:
                    UpdatePlayerTurn();
                    break;

                case MatchState.WaitingForOtherPlayerTurn:
                    if (Players.All(p => p.TurnEnded)) StartNpcTurn();
                    break;

                case MatchState.NpcTurn:
                    if (MatchType == CtfMatchType.Singleplayer)
                    {
                        AIPlayer opp = (AIPlayer)Opponent;
                        opp.UpdateTurn();
                        if (opp.TurnFinished) EndNpcTurn();
                    }
                    else EndNpcTurn(); // no NPCs in multiplayer (yet)
                    break;
            }
        }

        // Called every tick (exactly 60 TPS): Use this for game simulation logic so it's deterministic across all players
        public void Tick()
        {
            if(!IsMatchRunning) return;
            CurrentTick++;

            if(MatchType == CtfMatchType.Multiplayer)
            {
                foreach(System.Tuple<CharacterAction, int> queuedAction in QueuedCharacterActions)
                {
                    if (queuedAction.Item2 == CurrentTick)
                    {
                        queuedAction.Item1.Perform();
                    }
                }
                QueuedCharacterActions = QueuedCharacterActions.Where(x => x.Item2 > CurrentTick).ToList(); // Remove actions that were performed this tick
            }
        }

        /// <summary>
        /// Gets called every frame during your turn.
        /// </summary>
        private void UpdatePlayerTurn()
        {
            // Update hovered character
            CtfCharacter hoveredCharacter = null;
            if (World.HoveredEntity != null && World.HoveredEntity is CtfCharacter c) hoveredCharacter = c;

            // Hovered move
            UpdateHoveredMove();

            // Vision cutoff
            UpdateVisionCutoff();

            // Left click - Select character
            if (Input.GetMouseButtonDown(0) && !HelperFunctions.IsMouseOverUi())
            {
                // Deselect character
                if (hoveredCharacter == null) DeselectCharacter();

                // Select character (or pan to when double clicking)
                else if (hoveredCharacter.Owner == LocalPlayer)
                {
                    if (Time.time - LastSelectClickTime < DoubleClickTimeThreshold) // Double click detected
                        World.CameraPanToFocusEntity(hoveredCharacter, duration: 0.5f, false);

                    else SelectCharacter(hoveredCharacter);

                    LastSelectClickTime = Time.time;
                }
            }

            // Right click - Move
            if(Input.GetMouseButtonDown(1) && !HelperFunctions.IsMouseOverUiExcept(UI.CharacterLabelsContainer))
            {
                // Move
                if(SelectedCharacter != null && 
                    World.HoveredNode != null &&
                    !SelectedCharacter.IsInAction &&
                    SelectedCharacter.PossibleMoves.TryGetValue(World.HoveredNode, out Action_Movement move) &&
                    move.CanPerformNow())
                {
                    if (MatchType == CtfMatchType.Singleplayer) move.Perform();
                    if (MatchType == CtfMatchType.Multiplayer) PerformMultiplayerAction(move);

                    UnhighlightNodes(); // Unhighlight nodes
                }
            }

            // Update character info UI
            if (SelectedCharacter != null) UI.CharacterInfo.ShowCharacter(SelectedCharacter, HoveredAction);

            // Notifications UI
            if (Opponent.TurnEnded && !LocalPlayer.TurnEnded) UI.ShowRedNotificationText("Opponent has ended their turn");
        }

        /// <summary>
        /// Shows the path preview line to the currently hovered node and shows the cost in the character info.
        /// </summary>
        private void UpdateHoveredMove()
        {
            if (SelectedCharacter == null) return;
            if (SelectedCharacter.IsInAction) return;

            PathPreview.gameObject.SetActive(false);

            if (HelperFunctions.IsMouseOverUiExcept(UI.CharacterLabelsContainer)) return;
            HoveredAction = null;

            // Check if we hover a possible move
            BlockmapNode targetNode = World.HoveredNode;
            if (targetNode == null) return;
            if (!targetNode.IsExploredBy(LocalPlayer.Actor)) return;

            // Can move there in this turn
            if (SelectedCharacter.PossibleMoves.ContainsKey(targetNode))
            {
                PathPreview.gameObject.SetActive(true);

                Action_Movement move = SelectedCharacter.PossibleMoves[targetNode];
                HoveredAction = move;
                Pathfinder.ShowPathPreview(PathPreview, move.Path, 0.1f, new Color(1f, 1f, 1f, 0.5f));
            }
            // Can not move there in this turn
            else
            {
                NavigationPath path = Pathfinder.GetPath(SelectedCharacter, SelectedCharacter.OriginNode, targetNode, considerUnexploredNodes: true);
                if (path == null) return; // no viable path there
                
                PathPreview.gameObject.SetActive(true);
                Pathfinder.ShowPathPreview(PathPreview, path, 0.1f, new Color(1f, 0f, 0f, 0.5f));
            }
        }

        private void UpdateVisionCutoff()
        {
            if(SelectedCharacter == null)
            {
                if (World.IsVisionCutoffEnabled) World.ShowVisionCutoff(false);
            }

            else
            {
                if (!IsVisionCutoffEnabled && World.IsVisionCutoffEnabled) World.ShowVisionCutoff(false);
                else if (IsVisionCutoffEnabled)
                {
                    int currentCutoffAltitude = World.VisionCutoffAltitude;
                    int targetCutoffAltitude = SelectedCharacter.OriginNode.MaxAltitude + 3;
                    if (currentCutoffAltitude != targetCutoffAltitude || !World.IsVisionCutoffEnabled)
                    {
                        World.ShowVisionCutoffAt(targetCutoffAltitude);
                    }
                }
            }
        }

        #endregion

        #region Actions

        public void SelectCharacter(CtfCharacter c)
        {
            if (State != MatchState.PlayerTurn) return;
            if (SelectedCharacter == c) return;
            if (c.Owner != LocalPlayer) return;

            // Deselect previous
            DeselectCharacter();

            // Select new
            SelectedCharacter = c;

            RefreshSelectedCharacter();
        }
        private void DeselectCharacter()
        {
            UnhighlightNodes();
            PathPreview.gameObject.SetActive(false);
            IsVisionCutoffEnabled = false;
            if (SelectedCharacter != null)
            {
                UI.DeselectCharacter(SelectedCharacter);
                SelectedCharacter.ShowSelectionIndicator(false);
            }
            SelectedCharacter = null;
        }

        public void RefreshSelectedCharacter()
        {
            if (SelectedCharacter == null) return;

            UI.SelectCharacter(SelectedCharacter);
            SelectedCharacter.ShowSelectionIndicator(true);

            // Enable vision cutoff if below air node, else disable it
            bool isBelowRoof = (World.GetNodes(SelectedCharacter.OriginNode.WorldCoordinates).Any(n => n.BaseAltitude > SelectedCharacter.OriginNode.MaxAltitude));
            IsVisionCutoffEnabled = isBelowRoof;

            // Highlight nodes
            UnhighlightNodes();
            if (!SelectedCharacter.IsInAction)
            {
                HighlightNodes(SelectedCharacter.PossibleMoves.Select(x => x.Key).ToHashSet());
            }
        }

        private void HighlightNodes(HashSet<BlockmapNode> nodes)
        {
            HighlightedNodes = nodes;
            HashSet<BlockmapNode> addedNodes = new HashSet<BlockmapNode>();
            foreach (BlockmapNode node in HighlightedNodes)
            {
                node.ShowMultiOverlay(ReachableTileOverlay, Color.green);
                if (node is GroundNode surfaceNode && surfaceNode.WaterNode != null) // Also highlight waternodes on top of surface nodes
                {
                    surfaceNode.WaterNode.ShowMultiOverlay(ReachableTileOverlay, Color.green);
                    addedNodes.Add(surfaceNode.WaterNode);
                }
            }

            foreach (BlockmapNode node in addedNodes) HighlightedNodes.Add(node);
        }
        private void UnhighlightNodes()
        {
            foreach (BlockmapNode node in HighlightedNodes) node.HideMultiOverlay();
            HighlightedNodes.Clear();
        }

        public void SendToJail(CtfCharacter character)
        {
            // Get a node within the characters jail zone
            BlockmapNode targetNode = character.Owner.GetNextJailPosition();
            while (!targetNode.IsPassable(character)) targetNode = character.Owner.GetNextJailPosition();

            // Teleport character to target node
            character.Teleport(targetNode);

            // Instantly remove all action points (needed if it happens during own turn)
            character.SetActionPointsToZero();

            // Set jail time so character can't move
            character.SetJailTime(JAIL_TIME);

            // Inform player
            character.Owner.OnCharacterGotSentToJail(character);

            // Update selection panel UI
            if (character.Owner == LocalPlayer) UI.UpdateSelectionPanel(character);
        }

        public void ToggleDevMode() => SetDevMode(!DevMode);
        public void SetDevMode(bool active)
        {
            DevMode = active;

            UI.OnSetDevMode(DevMode);
            foreach (Player p in Players) p.OnSetDevMode(DevMode);

            if (!DevMode) World.SetActiveVisionActor(LocalPlayer.Actor);
        }

        public void ToggleVisionCutoff()
        {
            IsVisionCutoffEnabled = !IsVisionCutoffEnabled;
        }

        #endregion

        #region Getters

        public List<CtfCharacter> GetCharacters(BlockmapNode node)
        {
            return node.Entities.Where(x => x is CtfCharacter).Select(x => (CtfCharacter)x).ToList();
        }

        public CtfCharacter GetCharacterById(int id) => Characters.First(c => c.Id == id);

        public Player GetPlayerByClientId(string clientId) => Players.First(p => p.ClientId == clientId);

        /// <summary>
        /// Checks and returns a character can move on a given node.
        public bool CanCharacterMoveOn(CtfCharacter sourceCharacter, BlockmapNode target)
        {
            // Can't be on same node as any other friendly character
            foreach (CtfCharacter otherCharacter in sourceCharacter.Owner.Characters)
            {
                if (IsCharacterOnOrMovingTowards(otherCharacter, target)) return false;
            }

            // Additionally, can't be on same node as opponent character in neutral zone
            if (NeutralZone.ContainsNode(target))
            {
                foreach (CtfCharacter otherCharacter in sourceCharacter.Opponent.Characters)
                {
                    if (IsCharacterOnOrMovingTowards(otherCharacter, target)) return false;
                }
            }

            return true;
        }

        private bool IsCharacterOnOrMovingTowards(CtfCharacter character, BlockmapNode node)
        {
            if (character.OriginNode == node) return true;
            if (character.IsInAction && character.CurrentAction is Action_Movement otherMove && otherMove.Target == node) return true;
            if (character.IsInAction && character.CurrentAction is Action_UseLadder ladderMove && ladderMove.Transition.To == node) return true;
            return false;
        }

        #endregion

        #region Network

        /// <summary>
        /// All actions that players take in a multiplayer game need to be sent to this function.
        /// </summary>
        public void PerformMultiplayerAction(CharacterAction action)
        {
            NetworkMessage_CharacterAction characterActionMessage = action.GetNetworkAction();
            characterActionMessage.Tick = CurrentTick + MultiplayerActionTickOffset;

            NetworkClient.Instance.SendMessage(action.GetNetworkAction());
        }

        /// <summary>
        /// Gets called by the NetworkClient when actions come in through the network.
        /// <br/>THIS RUNS IN ANOTHER THREAD, do not directly call game simulation logic from here, but set flags so that the main thread can then handle it.
        /// </summary>
        /// <param name="baseMessage"></param>
        public void OnNetworkMessageReceived(NetworkMessage baseMessage)
        {
            try
            {
                switch (baseMessage.MessageType)
                {
                    // Game loop
                    case "PlayerReadyToStartMatch":
                        GetPlayerByClientId(baseMessage.SenderId).ReadyToStartMultiplayerMatch = true;
                        Debug.Log($"Player with clientId {baseMessage.SenderId} is ready. {Players.Count(x => x.ReadyToStartMultiplayerMatch)}/{Players.Count} players are ready now.");
                        break;

                    case "TurnEnded":
                        GetPlayerByClientId(baseMessage.SenderId).TurnEnded = true;
                        Debug.Log($"Player with clientId {baseMessage.SenderId} has ended their turn. {Players.Count(x => x.TurnEnded)}/{Players.Count} players have ended their now.");
                        break;

                    // Character actions
                    case "CharacterAction_MoveCharacter":
                        var moveMessage = (NetworkMessage_CharacterAction)baseMessage;
                        Action_Movement move = GetCharacterById(moveMessage.CharacterId).PossibleMoves.First(m => m.Key.Id == moveMessage.TargetId).Value;
                        QueueActionToPerform(move, moveMessage.Tick);
                        break;

                    case "CharacterAction_GoToJail":
                        var jailMessage = (NetworkMessage_CharacterAction)baseMessage;
                        Action_GoToJail jailAction = (Action_GoToJail)(GetCharacterById(jailMessage.CharacterId).PossibleSpecialActions.First(x => x is Action_GoToJail));
                        QueueActionToPerform(jailAction, jailMessage.Tick);
                        break;

                    case "CharacterAction_InteractWithDoor":
                        var doorMessage = (NetworkMessage_CharacterAction)baseMessage;
                        Action_InteractWithDoor doorAction = (Action_InteractWithDoor)(GetCharacterById(doorMessage.CharacterId).PossibleSpecialActions.First(x => x is Action_InteractWithDoor a && a.TargetDoor.Id == doorMessage.TargetId));
                        QueueActionToPerform(doorAction, doorMessage.Tick);
                        break;

                    case "CharacterAction_UseLadder":
                        var ladderMessage = (NetworkMessage_CharacterAction)baseMessage;
                        Action_UseLadder ladderAction = (Action_UseLadder)(GetCharacterById(ladderMessage.CharacterId).PossibleSpecialActions.First(x => x is Action_UseLadder a && a.Transition.To.Id == ladderMessage.TargetId));
                        QueueActionToPerform(ladderAction, ladderMessage.Tick);
                        break;

                    default:
                        throw new System.Exception($"NetworkMessage type '{baseMessage.MessageType}' not handled.");
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"[Client] Error in OnNetworkMessageReceived(): {e.Message}");
            }
        }

        private void QueueActionToPerform(CharacterAction action, int tick)
        {
            if (tick <= CurrentTick)
            {
                Debug.Log($"Delaying action from tick {tick} to {CurrentTick + 1} because else it would be in the past.");
                tick = CurrentTick + 1; // Ensure that action comes through
            }
            QueuedCharacterActions.Add(new System.Tuple<CharacterAction, int>(action, tick));
        }

        #endregion
    }

    public enum CtfMatchType
    {
        Singleplayer,
        Multiplayer
    }
}
