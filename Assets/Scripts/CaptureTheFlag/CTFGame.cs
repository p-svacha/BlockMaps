using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFGame : MonoBehaviour
    {
        public CTFUi UI;
        public LineRenderer PathPreview;

        public GameState State { get; private set; }
        private CTFMapGenerator MapGenerator;
        public World World { get; private set; }

        public Player Player { get; private set; }
        public Player Opponent { get; private set; }

        public Character SelectedCharacter { get; private set; }
        private HashSet<BlockmapNode> HighlightedNodes = new();

        #region Game Loop

        private void Start()
        {
            UI.Init(this);

            UI.LoadingScreenOverlay.SetActive(true);
            MapGenerator = new CTFMapGenerator();
            MapGenerator.InitGeneration(16, 4);
            State = GameState.Loading;
        }

        private void StartGame()
        {
            World = MapGenerator.GeneratedWorld;

            // Convert world actors to CTF Players
            Player = new Player(World.Actors[0]);
            Opponent = new Player(World.Actors[1]);

            // Hooks
            World.OnHoveredNodeChanged += OnHoveredNodeChanged;

            // Vision
            World.ShowTextures(true);
            World.SetActiveVisionActor(Player.Actor);
            UI.LoadingScreenOverlay.SetActive(false);

            // Start Game
            foreach (Character c in Player.Characters) c.OnStartGame(this);
            foreach (Character c in Opponent.Characters) c.OnStartGame(this);
            UI.OnStartGame();

            // Camera
            World.CameraJumpToFocusEntity(Player.Characters[0].Entity);

            StartYourTurn();
        }

        private void StartYourTurn()
        {
            foreach (Character c in Player.Characters) c.OnStartTurn();

            State = GameState.YourTurn;
        }

        #endregion

        #region Update

        private void Update()
        {
            HelperFunctions.UnfocusNonInputUiElements();

            switch (State)
            {
                case GameState.Loading:
                    if (MapGenerator.GenerationPhase == GenerationPhase.Done) StartGame();
                    else MapGenerator.UpdateGeneration();
                    break;

                case GameState.YourTurn:
                    UpdateYourTurn();
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

            // Left click
            if(Input.GetMouseButtonDown(0))
            {
                // De/Select character
                if (!HelperFunctions.IsMouseOverUi())
                    SelectCharacter(hoveredCharacter);
            }

            // Right click
            if(Input.GetMouseButtonDown(1))
            {
                // Move
                if(SelectedCharacter != null && 
                    World.HoveredNode != null &&
                    !SelectedCharacter.IsMoving
                    && SelectedCharacter.PossibleMoves.TryGetValue(World.HoveredNode, out Movement move))
                {
                    MoveCharacter(move);
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
            if (SelectedCharacter.IsMoving) return;

            PathPreview.gameObject.SetActive(false);
            UI.CharacterInfo.Init(SelectedCharacter);

            // Check if we hover a possible move
            BlockmapNode targetNode = World.HoveredNode;
            if (targetNode == null) return;
            if (!targetNode.IsExploredBy(Player.Actor)) return;


            // Can move there in this turn
            if (SelectedCharacter.PossibleMoves.ContainsKey(targetNode))
            {
                PathPreview.gameObject.SetActive(true);

                Movement move = SelectedCharacter.PossibleMoves[targetNode];
                UI.CharacterInfo.ShowActionPreview(move.Cost);
                Pathfinder.ShowPathPreview(PathPreview, move.Path, 0.1f, new Color(1f, 1f, 1f, 0.5f));
            }
            // Can not move there in this turn
            else
            {
                List<BlockmapNode> path = Pathfinder.GetPath(SelectedCharacter.Entity, SelectedCharacter.Entity.OriginNode, targetNode, ignoreUnexploredNodes: true);
                if (path == null) return; // no viable path there

                PathPreview.gameObject.SetActive(true);
                Pathfinder.ShowPathPreview(PathPreview, path, 0.1f, new Color(1f, 0f, 0f, 0.5f));
            }
        }

        public void OnMovementDone(Character c)
        {
            c.UpdatePossibleMoves();
            if (SelectedCharacter == c) SelectCharacter(c); // Reselect to update everything
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

        public void MoveCharacter(Movement move)
        {
            move.Character.Entity.Move(move.Path);
        }

        private void HighlightNodes(HashSet<BlockmapNode> nodes)
        {
            HighlightedNodes = nodes;
            foreach (BlockmapNode node in HighlightedNodes)
                node.ShowMultiOverlay(CTFResourceManager.Singleton.ReachableTileTexture, Color.green);
        }
        private void UnhighlightNodes()
        {
            foreach (BlockmapNode node in HighlightedNodes) node.HideMultiOverlay();
            HighlightedNodes.Clear();
        }

        #endregion
    }
}
