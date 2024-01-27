using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CTFGame : MonoBehaviour
    {
        public GameState State { get; private set; }
        private CTFMapGenerator MapGenerator;
        private World World;

        private Player Player;
        private Player Opponent;

        private void Start()
        {
            MapGenerator = new CTFMapGenerator();
            MapGenerator.InitGeneration(16, 4);
            State = GameState.Loading;
        }

        private void Update()
        {
            switch(State)
            {
                case GameState.YourTurn:
                    break;
            }
        }

        private void FixedUpdate()
        {
            if(State == GameState.Loading)
            {
                if (MapGenerator.GenerationPhase == GenerationPhase.Done) StartGame();
                else MapGenerator.UpdateGeneration();
            }
        }

        private void StartGame()
        {
            World = MapGenerator.GeneratedWorld;
            Player = World.Players[0];
            Opponent = World.Players[1];
            World.SetActiveVisionPlayer(Player);

            State = GameState.YourTurn;
        }
    }
}
