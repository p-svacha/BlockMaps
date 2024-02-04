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

            // Vision
            World.ShowTextures(true);
            World.SetActiveVisionActor(Player.Actor);
            UI.LoadingScreenOverlay.SetActive(false);

            // Start Game
            foreach (Character c in Player.Characters) c.OnStartGame();
            foreach (Character c in Opponent.Characters) c.OnStartGame();
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
