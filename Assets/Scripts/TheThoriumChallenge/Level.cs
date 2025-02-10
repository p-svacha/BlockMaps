using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Level
    {
        public Game Game { get; private set; }
        public World World { get; private set; }
        public Vector2Int EntryPoint;
        public Dictionary<Vector2Int, Biome> ExitPoints;
        public TimeStamp GlobalSimulationTime { get; private set; }
        public Actor LocalPlayer { get; private set; }

        public List<Creature> Creatures;
        private Creature ActiveTurnCreature;

        private PriorityQueue<Creature> ActionQueue;

        public Level(Game game, World world, Vector2Int entryPoint, Dictionary<Vector2Int, Biome> exitPoints)
        {
            Game = game;
            World = world;
            EntryPoint = entryPoint;
            ExitPoints = exitPoints;

            LocalPlayer = World.GetActor(1);

            // Fill entity lists
            Creatures = new List<Creature>();
            foreach (Entity e in World.GetAllEntities())
            {
                if (e is Creature creature)
                {
                    Creatures.Add(creature);
                }
            }

            // Display settings
            World.DisplaySettings.ShowTextures(true);
            World.DisplaySettings.ShowTileBlending(true);
            World.DisplaySettings.ShowGrid(false);
        }

        public void Start()
        {
            // Camera
            BlockmapCamera.Instance.SetAngle(0);
            BlockmapCamera.Instance.SetZoom(10);
            World.CameraJumpToFocusNode(World.GetGroundNode(EntryPoint));

            // Vision
            World.SetActiveVisionActor(LocalPlayer);

            // Action queue
            ActionQueue = new PriorityQueue<Creature>();
            foreach (Creature e in Creatures)
            {
                ActionQueue.Enqueue(e, e.NextActionTime.ValueInSeconds);
            }

            // Time
            GlobalSimulationTime = new TimeStamp();

            // Start first turn
            StartNextTurn();
        }

        public void Tick()
        {
            // Go to next turn
            if (!ActiveTurnCreature.IsInTurn)
            {
                ActionQueue.Enqueue(ActiveTurnCreature, ActiveTurnCreature.NextActionTime.ValueInSeconds);
                StartNextTurn();
            }
        }

        public void HandleInputs()
        {
            if (ActiveTurnCreature.IsPlayerControlled && !ActiveTurnCreature.IsInAction)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 180);
                    if (ActiveTurnCreature.MoveActions.ContainsKey(dir))
                        ActiveTurnCreature.MoveActions[dir].Perform();
                }
                else if (Input.GetKey(KeyCode.LeftArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 90);
                    if (ActiveTurnCreature.MoveActions.ContainsKey(dir))
                        ActiveTurnCreature.MoveActions[dir].Perform();
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 0);
                    if (ActiveTurnCreature.MoveActions.ContainsKey(dir))
                        ActiveTurnCreature.MoveActions[dir].Perform();
                }
                else if (Input.GetKey(KeyCode.RightArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 270);
                    if (ActiveTurnCreature.MoveActions.ContainsKey(dir))
                        ActiveTurnCreature.MoveActions[dir].Perform();
                }
            }
        }

        #region Turn Loop

        private void StartNextTurn()
        {
            
            SetGlobalSimulationTime(ActionQueue.ToSortedList()[0].NextActionTime.ValueInSeconds);
            Game.UI.ActionTimeline.Refresh(ActionQueue);
            ActiveTurnCreature = ActionQueue.Dequeue();
            ActiveTurnCreature.IsInTurn = true;

            ActiveTurnCreature.RefreshPossibleActions();

            if (ActiveTurnCreature.IsPlayerControlled)
            {
                World.CameraPanToFocusEntity(ActiveTurnCreature, duration: 0.6f, followAfterPan: false);
                Game.UI.ShowCreatureInfo(ActiveTurnCreature);
                ActiveTurnCreature.PerformNextAction();
            }
            else
            {
                Game.UI.HideCreatureInfo();
                if (ActiveTurnCreature.IsVisible) World.CameraPanToFocusEntity(ActiveTurnCreature, duration: 0.6f, followAfterPan: false, callback: () => ActiveTurnCreature.PerformNextAction());
                else ActiveTurnCreature.PerformNextAction();
            }
        }

        private void SetGlobalSimulationTime(int secondsAbsolute)
        {
            GlobalSimulationTime.SetTime(secondsAbsolute);
            Game.UI.RefreshTimeText();
        }


        #endregion

    }
}
