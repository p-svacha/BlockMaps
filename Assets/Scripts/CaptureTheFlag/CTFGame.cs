using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFGame : MonoBehaviour
    {
        // Rules
        public const float NEUTRAL_ZONE_SIZE = 0.1f; // size of neutral zone strip in %
        public const int JAIL_ZONE_RADIUS = 3;
        public const int JAIL_ZONE_MIN_FLAG_DISTANCE = 9; // minimum distance from jail zone center to flag
        public const int JAIL_ZONE_MAX_FLAG_DISTANCE = 11; // maximum distance from jail zone center to flag
        public const int JAIL_TIME = 5; // Amount of turns a character spends in jail after being tagged
        public const float FLAG_ZONE_RADIUS = 7.5f;  // Amount of tiles around flag that can't be entered by own team

        // Elements
        public CTFUi UI;
        public LineRenderer PathPreview;
        private CTFMapGenerator MapGenerator;

        public Character SelectedCharacter { get; private set; }
        private HashSet<BlockmapNode> HighlightedNodes = new();

        // Game attributes
        public Zone LocalPlayerZone { get; private set; }
        public Zone NeutralZone { get; private set; }
        public Zone OpponentZone { get; private set; }

        public GameState State { get; private set; }
        public World World { get; private set; }

        public Player LocalPlayer { get; private set; }
        public AIPlayer Opponent { get; private set; }

        // Options
        public bool DevMode { get; private set; }


        #region Game Loop

        private void Start()
        {
            UI.Init(this);

            UI.LoadingScreenOverlay.SetActive(true);
            MapGenerator = new CTFMapGenerator_Forest();
            MapGenerator.InitGeneration(16, 4);// Random.Range(4, 8 + 1));
            State = GameState.Loading;
        }

        private void StartGame()
        {
            // Set world
            World = MapGenerator.World;

            // Map zones
            LocalPlayerZone = World.GetZone(id: 0);
            NeutralZone = World.GetZone(id: 1);
            OpponentZone = World.GetZone(id: 2);

            // Convert world actors to CTF Players
            LocalPlayer = new Player(World.GetActor(id: 1), LocalPlayerZone, World.GetZone(id: 3), World.GetZone(id: 4));
            Opponent = new AIPlayer(World.GetActor(id: 2), OpponentZone, World.GetZone(id: 5), World.GetZone(id: 6));
            LocalPlayer.Opponent = Opponent;
            Opponent.Opponent = LocalPlayer;

            // Hooks
            World.OnHoveredNodeChanged += OnHoveredNodeChanged;

            // Vision
            World.ShowTextures(true);
            World.ShowGridOverlay(true);
            World.ShowTileBlending(true);
            World.SetActiveVisionActor(LocalPlayer.Actor);
            UI.LoadingScreenOverlay.SetActive(false);

            // Start Game
            LocalPlayer.OnStartGame(this);
            Opponent.OnStartGame(this);
            UI.OnStartGame();

            // Camera
            World.CameraJumpToFocusEntity(LocalPlayer.Characters[0].Entity);

            StartYourTurn();
        }

        public void OnActionDone(CharacterAction action)
        {
            // Check game over
            if (IsGameOver(out Player winner)) EndGame(won: winner == LocalPlayer);

            // Reduce action/stamina based on action cost
            action.Character.ReduceActionAndStamina(action.Cost);

            // Send all colliding characters to jail (depending on map half)
            BlockmapNode node = action.Character.Entity.OriginNode;
            List<Character> characters = GetCharacters(node);
            if(LocalPlayerZone.ContainsNode(node) && characters.Any(x => x.Owner == LocalPlayer)) // send opponent characters to their jail
            {
                foreach (Character opponentCharacter in characters.Where(x => x.Owner == Opponent))
                    SendToJail(opponentCharacter);
            }
            if (OpponentZone.ContainsNode(node) && characters.Any(x => x.Owner == Opponent)) // send own characters to own jail
            {
                foreach (Character ownCharacter in characters.Where(x => x.Owner == LocalPlayer))
                    SendToJail(ownCharacter);
            }

            // Update possible moves for all characters
            foreach (Character teamCharacter in action.Character.Owner.Characters) teamCharacter.UpdatePossibleActions();

            // Update UI
            if (action.Character.Owner == LocalPlayer) UI.UpdateSelectionPanel(action.Character);
            if (SelectedCharacter == action.Character) SelectCharacter(SelectedCharacter); // Reselect character to update highlighted nodes and character info
        }

        private void StartYourTurn()
        {
            State = GameState.YourTurn;

            foreach (Character c in LocalPlayer.Characters) c.OnStartTurn();
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
            foreach (Character c in Opponent.Characters) c.OnStartTurn();
            Opponent.StartTurn();
        }

        public void EndOpponentTurn()
        {
            UI.HideTurnIndicator();
            StartYourTurn();
        }

        public void EndGame(bool won)
        {
            State = GameState.GameFinished;
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

        #region Update

        private void Update()
        {
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
                case GameState.Loading:
                    if (MapGenerator.IsDone) StartGame();
                    else MapGenerator.UpdateGeneration();
                    break;

                case GameState.YourTurn:
                    UpdateYourTurn();
                    break;

                case GameState.OpponentTurn:
                    Opponent.UpdateTurn();
                    if (Opponent.TurnFinished) EndOpponentTurn();
                    break;
            }
        }

        /// <summary>
        /// Gets called every frame during your turn.
        /// </summary>
        private void UpdateYourTurn()
        {
            // Update hovered character
            Character hoveredCharacter = null;
            if(World.HoveredEntity != null) World.HoveredEntity.TryGetComponent(out hoveredCharacter);

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
                List<BlockmapNode> path = Pathfinder.GetPath(SelectedCharacter.Entity, SelectedCharacter.Entity.OriginNode, targetNode, considerUnexploredNodes: true);
                if (path == null) return; // no viable path there

                PathPreview.gameObject.SetActive(true);
                Pathfinder.ShowPathPreview(PathPreview, path, 0.1f, new Color(1f, 0f, 0f, 0.5f));
            }
        }

        #endregion

        #region Actions

        public void SelectCharacter(Character c)
        {
            // Deselect previous
            DeselectCharacter();

            // Select new
            SelectedCharacter = c;
            if (SelectedCharacter != null)
            {
                UI.SelectCharacter(c);
                c.Entity.SetSelected(true);
               
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
                SelectedCharacter.Entity.SetSelected(false);
            }
            SelectedCharacter = null;
        }

        private void HighlightNodes(HashSet<BlockmapNode> nodes)
        {
            HighlightedNodes = nodes;
            HashSet<BlockmapNode> addedNodes = new HashSet<BlockmapNode>();
            foreach (BlockmapNode node in HighlightedNodes)
            {
                node.ShowMultiOverlay(CTFResourceManager.Singleton.ReachableTileTexture, Color.green);
                if (node is GroundNode surfaceNode && surfaceNode.WaterNode != null) // Also highlight waternodes on top of surface nodes
                {
                    surfaceNode.WaterNode.ShowMultiOverlay(CTFResourceManager.Singleton.ReachableTileTexture, Color.green);
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

        public void SendToJail(Character character)
        {
            // Get a random free node within the characters jail zone
            List<BlockmapNode> candidateNodes = character.Owner.JailZone.Nodes.Where(x => x.IsPassable(character.Entity)).ToList();
            BlockmapNode targetNode = candidateNodes[Random.Range(0, candidateNodes.Count)];

            // Teleport character to target node
            character.Entity.Teleport(targetNode);

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
            Opponent.OnSetDevMode(DevMode);

            if (!DevMode) World.SetActiveVisionActor(LocalPlayer.Actor);
        }

        #endregion

        #region Getters

        public List<Character> GetCharacters(BlockmapNode node)
        {
            return node.Entities.Where(x => x.TryGetComponent(out Character character)).Select(x => x.GetComponent<Character>()).ToList();
        }

        #endregion
    }
}
