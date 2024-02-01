using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFGame : MonoBehaviour
    {
        public CTFUi UI;
        public GameState State { get; private set; }
        private CTFMapGenerator MapGenerator;
        private World World;

        public Player Player { get; private set; }
        public Player Opponent { get; private set; }

        public Character SelectedCharacter { get; private set; }

        #region Initialize

        private void Start()
        {
            UI.Init(this);

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

            World.SetActiveVisionActor(Player.Actor);

            UI.OnStartGame();

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
                    break;
            }
        }

        #endregion

        #region Actions

        public void SelectCharacter(Character c)
        {
            SelectedCharacter = c;
        }

        #endregion
    }
}
