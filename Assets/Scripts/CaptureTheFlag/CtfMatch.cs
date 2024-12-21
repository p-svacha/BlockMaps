using BlockmapFramework;
using CaptureTheFlag.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CtfMatch
    {
        private CtfGame Game;
        public World World { get; private set; }
        public CtfMatchType MatchType { get; private set; }

        // Rules
        public const float NEUTRAL_ZONE_SIZE = 0.1f; // size of neutral zone strip in %
        public const int JAIL_ZONE_RADIUS = 3;
        public const int JAIL_ZONE_MIN_FLAG_DISTANCE = 9; // minimum distance from jail zone center to flag
        public const int JAIL_ZONE_MAX_FLAG_DISTANCE = 11; // maximum distance from jail zone center to flag
        public const int JAIL_TIME = 5; // Amount of turns a character spends in jail after being tagged
        public const float FLAG_ZONE_RADIUS = 7.5f;  // Amount of tiles around flag that can't be entered by own team

        // Elements
        public CtfMatchUi UI => Game.MatchUI;
        public LineRenderer PathPreview => Game.PathPreviewRenderer;
        private CTFMapGenerator MapGenerator;

        private List<CTFCharacter> Characters;
        public CTFCharacter SelectedCharacter { get; private set; }
        private HashSet<BlockmapNode> HighlightedNodes = new();

        // Game attributes
        public Zone LocalPlayerZone => LocalPlayer.Territory;
        public Zone NeutralZone { get; private set; }
        public Zone OpponentZone => Opponent.Territory;

        public GameState State { get; private set; }

        public List<Player> Players;
        private bool IsPlayingAsBlue;
        public Player LocalPlayer { get; private set; }
        public Player Opponent { get; private set; }

        // Options
        public bool DevMode { get; private set; }

        // Cache
        private Texture2D ReachableTileOverlay;


        #region Game Loop

        public CtfMatch(CtfGame game)
        {
            Game = game;
        }

        public void InitializeGame(CtfMatchType matchType, int mapSize, int seed = -1, bool playAsBlue = true)
        {
            MatchType = matchType;
            IsPlayingAsBlue = playAsBlue;

            // Load textures
            ReachableTileOverlay = MaterialManager.LoadTexture("CaptureTheFlag/Textures/ReachableTileOverlay");

            // Start world generation
            Game.LoadingScreenOverlay.SetActive(true);
            MapGenerator = new CTFMapGenerator_Forest();
            MapGenerator.StartGeneration(mapSize, seed, onDoneCallback: OnWorldGenerationDone);
            State = GameState.GeneratingWorld;
        }

        private void OnWorldGenerationDone()
        {
            // Start world initialization
            if (World != null) GameObject.Destroy(World.WorldObject);
            World = MapGenerator.World;
            World.Initialize(OnWorldInitializationDone);

            // Set map zones
            NeutralZone = World.GetZone(id: 1);

            // Create players
            Players = new List<Player>();

            if(MatchType == CtfMatchType.Singleplayer)
            {
                if (IsPlayingAsBlue)
                {
                    LocalPlayer = new Player(World.GetActor(id: 1), World.GetZone(id: 0), World.GetZone(id: 3), World.GetZone(id: 4));
                    Players.Add(LocalPlayer);

                    Opponent = new AIPlayer(World.GetActor(id: 2), World.GetZone(id: 2), World.GetZone(id: 5), World.GetZone(id: 6));
                    Players.Add(Opponent);
                }
                else
                {
                    Opponent = new AIPlayer(World.GetActor(id: 1), World.GetZone(id: 0), World.GetZone(id: 3), World.GetZone(id: 4));
                    Players.Add(Opponent);

                    LocalPlayer = new Player(World.GetActor(id: 2), World.GetZone(id: 2), World.GetZone(id: 5), World.GetZone(id: 6));
                    Players.Add(LocalPlayer);
                }
            }
            else if(MatchType == CtfMatchType.Multiplayer)
            {
                Players.Add(new Player(World.GetActor(id: 1), World.GetZone(id: 0), World.GetZone(id: 3), World.GetZone(id: 4)));
                Players.Add(new Player(World.GetActor(id: 2), World.GetZone(id: 2), World.GetZone(id: 5), World.GetZone(id: 6)));

                if (IsPlayingAsBlue)
                {
                    LocalPlayer = Players[0];
                    Opponent = Players[1];
                }
                else
                {
                    LocalPlayer = Players[1];
                    Opponent = Players[0];
                }
            }

            LocalPlayer.Opponent = Opponent;
            Opponent.Opponent = LocalPlayer;

            // Register all characters
            Characters = new List<CTFCharacter>();
            Characters.AddRange(LocalPlayer.Characters);
            Characters.AddRange(Opponent.Characters);

            State = GameState.InitializingWorld;
        }

        private void OnWorldInitializationDone()
        {
            // Hooks
            World.OnHoveredNodeChanged += OnHoveredNodeChanged;

            StartGame();
        }

        private void StartGame()
        {
            // Vision
            World.ShowTextures(true);
            World.ShowGridOverlay(true);
            World.ShowTileBlending(true);
            World.SetActiveVisionActor(LocalPlayer.Actor);
            Game.LoadingScreenOverlay.SetActive(false);

            // Start Game
            LocalPlayer.OnStartGame(this);
            Opponent.OnStartGame(this);
            UI.OnStartGame();

            // Camera
            World.CameraJumpToFocusEntity(LocalPlayer.Characters[0]);

            StartYourTurn();
        }

        public void OnActionDone(CharacterAction action)
        {
            // Check game over
            if (IsGameOver(out Player winner)) EndGame(won: winner == LocalPlayer);

            // Reduce action/stamina based on action cost
            action.Character.ReduceActionAndStamina(action.Cost);

            // Send all colliding characters to jail (depending on map half)
            BlockmapNode node = action.Character.OriginNode;
            List<CTFCharacter> characters = GetCharacters(node);
            if(LocalPlayerZone.ContainsNode(node) && characters.Any(x => x.Owner == LocalPlayer)) // send opponent characters to their jail
            {
                foreach (CTFCharacter opponentCharacter in characters.Where(x => x.Owner == Opponent))
                    SendToJail(opponentCharacter);
            }
            if (OpponentZone.ContainsNode(node) && characters.Any(x => x.Owner == Opponent)) // send own characters to own jail
            {
                foreach (CTFCharacter ownCharacter in characters.Where(x => x.Owner == LocalPlayer))
                    SendToJail(ownCharacter);
            }

            // Update possible moves for all characters
            foreach (CTFCharacter teamCharacter in action.Character.Owner.Characters) teamCharacter.UpdatePossibleActions();

            // Update UI
            if (action.Character.Owner == LocalPlayer) UI.UpdateSelectionPanel(action.Character);
            if (SelectedCharacter == action.Character) SelectCharacter(SelectedCharacter); // Reselect character to update highlighted nodes and character info
        }

        private void StartYourTurn()
        {
            State = GameState.YourTurn;

            foreach (CTFCharacter c in LocalPlayer.Characters) c.OnStartTurn();
            UI.UpdateSelectionPanels();
        }

        public void EndYourTurn()
        {
            if (State != GameState.YourTurn) return;
            if (LocalPlayer.Characters.Any(x => x.IsInAction)) return;
            DeselectCharacter();

            StartOpponentTurn();
        }

        public void StartOpponentTurn()
        {
            State = GameState.OpponentTurn;

            UI.ShowTurnIndicator("Opponent Turn");
            foreach (CTFCharacter c in Opponent.Characters) c.OnStartTurn();

            if (MatchType == CtfMatchType.Singleplayer) ((AIPlayer)Opponent).StartTurn();
        }

        public void EndOpponentTurn()
        {
            UI.HideTurnIndicator();
            StartYourTurn();
        }

        public void EndGame(bool won)
        {
            State = GameState.GameFinished;
            Game.ShowEndGameScreen(won ? "You won!" : "You lost.");
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

        #region Update

        public void Tick()
        {
            World?.Tick();
            
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
                case GameState.GeneratingWorld:
                    MapGenerator.UpdateGeneration();
                    break;

                case GameState.YourTurn:
                    UpdateYourTurn();
                    break;

                case GameState.OpponentTurn:
                    if (MatchType == CtfMatchType.Singleplayer)
                    {
                        AIPlayer opp = (AIPlayer)Opponent;
                        opp.UpdateTurn();
                        if (opp.TurnFinished) EndOpponentTurn();
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets called every frame during your turn.
        /// </summary>
        private void UpdateYourTurn()
        {
            // Update hovered character
            CTFCharacter hoveredCharacter = null;
            if (World.HoveredEntity != null && World.HoveredEntity is CTFCharacter c) hoveredCharacter = c;

            // Left click - Select character
            if(Input.GetMouseButtonDown(0) && !HelperFunctions.IsMouseOverUi())
            {
                // Deselect character
                if (hoveredCharacter == null) DeselectCharacter();

                // Select character
                else if(hoveredCharacter.Owner == LocalPlayer) SelectCharacter(hoveredCharacter);
            }

            // Right click - Move
            if(Input.GetMouseButtonDown(1))
            {
                // Move
                if(SelectedCharacter != null && 
                    World.HoveredNode != null &&
                    !SelectedCharacter.IsInAction
                    && SelectedCharacter.PossibleMoves.TryGetValue(World.HoveredNode, out Action_Movement move))
                {
                    if (LocalPlayer.CanPerformMovement(move))
                    {
                        LocalPlayer.Actions[SelectedCharacter] = move;
                        move.Perform(); // Start movement action
                        UnhighlightNodes(); // Unhighlight nodes
                    }
                }
            }
        }

        private void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            UpdateHoveredMove();
        }

        /// <summary>
        /// Shows the path preview line to the currently hovered node and shows the cost in the character info.
        /// </summary>
        private void UpdateHoveredMove()
        {
            if (SelectedCharacter == null) return;
            if (SelectedCharacter.IsInAction) return;

            PathPreview.gameObject.SetActive(false);
            UI.CharacterInfo.Init(SelectedCharacter);

            // Check if we hover a possible move
            BlockmapNode targetNode = World.HoveredNode;
            if (targetNode == null) return;
            if (!targetNode.IsExploredBy(LocalPlayer.Actor)) return;


            // Can move there in this turn
            if (SelectedCharacter.PossibleMoves.ContainsKey(targetNode))
            {
                PathPreview.gameObject.SetActive(true);

                Action_Movement move = SelectedCharacter.PossibleMoves[targetNode];
                UI.CharacterInfo.ShowActionPreview(move.Cost);
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

        #endregion

        #region Actions

        public void SelectCharacter(CTFCharacter c)
        {
            // Deselect previous
            DeselectCharacter();

            // Select new
            SelectedCharacter = c;
            if (SelectedCharacter != null)
            {
                UI.SelectCharacter(c);
                c.SetSelected(true);
               
                HighlightNodes(c.PossibleMoves.Select(x => x.Key).ToHashSet()); // Highlight reachable nodes
            }
        }
        private void DeselectCharacter()
        {
            UnhighlightNodes();
            PathPreview.gameObject.SetActive(false);
            if (SelectedCharacter != null)
            {
                UI.DeselectCharacter(SelectedCharacter);
                SelectedCharacter.SetSelected(false);
            }
            SelectedCharacter = null;
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

        public void SendToJail(CTFCharacter character)
        {
            // Get a random free node within the characters jail zone
            List<BlockmapNode> candidateNodes = character.Owner.JailZone.Nodes.Where(x => x.IsPassable(character)).ToList();
            BlockmapNode targetNode = candidateNodes[Random.Range(0, candidateNodes.Count)];

            // Teleport character to target node
            character.Teleport(targetNode);

            // Instantly remove all action points (needed if it happens during own turn)
            character.SetActionPointsToZero();

            // Set jail time so character can't move
            character.SetJailTime(JAIL_TIME);

            // Update selection panel UI
            if (character.Owner == LocalPlayer) UI.UpdateSelectionPanel(character);
        }

        public void ToggleDevMode() => SetDevMode(!DevMode);
        public void SetDevMode(bool active)
        {
            DevMode = active;

            UI.OnSetDevMode(DevMode);
            if (MatchType == CtfMatchType.Singleplayer) ((AIPlayer)Opponent).OnSetDevMode(DevMode);

            if (!DevMode) World.SetActiveVisionActor(LocalPlayer.Actor);
        }

        #endregion

        #region Getters

        public List<CTFCharacter> GetCharacters(BlockmapNode node)
        {
            return node.Entities.Where(x => x is CTFCharacter).Select(x => (CTFCharacter)x).ToList();
        }

        #endregion

        #region Network

        public void OnNetworkActionReceived(NetworkAction action)
        {

        }

        #endregion
    }

    public enum CtfMatchType
    {
        Singleplayer,
        Multiplayer
    }
}
