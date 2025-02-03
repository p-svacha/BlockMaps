using BlockmapFramework;
using ExodusOutposAlpha.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExodusOutposAlpha
{
    public class EoaGame : GameLoop
    {
        public static EoaGame Instance;

        public UI_EoaGame UI { get; private set; }
        public EoaGameState GameState { get; private set; }
        public World World { get; private set; }
        private WorldGenerator_Exodus MapGenerator;

        public EoaTime GlobalSimulationTime { get; private set; }
        public Actor LocalPlayer { get; private set; }

        public List<EoaEntity> Crew;
        public List<EoaEntity> Entities;

        private PriorityQueue<EoaEntity> ActionQueue;
        private EoaEntity CurrentTurnEntity;

        private void Awake()
        {
            Instance = this;
            UI = GameObject.Find("GameUI").GetComponent<UI_EoaGame>();
        }

        void Start()
        {
            InitializeGame();
        }


        private void InitializeGame()
        {
            // Load defs
            DefDatabaseRegistry.AddAllGlobalDefs();
            DefDatabase<EntityDef>.AddDefs(CrewDefs.Defs);
            DefDatabase<EntityDef>.AddDefs(RobotDefs.Defs);
            DefDatabaseRegistry.ResolveAllReferences();
            DefDatabaseRegistry.OnLoadingDone();
            DefDatabaseRegistry.BindAllDefOfs();

            // Init materials
            MaterialManager.InitializeBlendableSurfaceMaterial();

            // Start world generation
            UI.LoadingScreenOverlay.SetActive(true);
            MapGenerator = new WorldGenerator_Exodus();
            MapGenerator.StartGeneration(2, WorldGenerator.GetRandomSeed(), onDoneCallback: OnWorldGenerationDone);
            GameState = EoaGameState.GeneratingWorld;
        }

        private void OnWorldGenerationDone()
        {
            World = MapGenerator.World;
            LocalPlayer = World.GetActor(1);

            // Fill entity lists
            Crew = new List<EoaEntity>();
            Entities = new List<EoaEntity>();
            foreach(Entity e in World.GetAllEntities())
            {
                if(e is EoaEntity eoaEntity)
                {
                    if (eoaEntity.Actor == LocalPlayer)
                    {
                        eoaEntity.IsPlayerControlled = true;
                        Crew.Add(eoaEntity);
                    }
                    else eoaEntity.NextActionTime.IncreaseTime(1);
                    Entities.Add(eoaEntity);
                }
            }

            // Display settings
            World.DisplaySettings.ShowTextures(true);
            World.DisplaySettings.ShowTileBlending(true);
            World.DisplaySettings.ShowGrid(false);

            // Start world initialization
            World.Initialize(OnWorldInitializationDone);
            GameState = EoaGameState.InitializingWorld;
        }

        private void OnWorldInitializationDone()
        {
            // Camera
            BlockmapCamera.Instance.SetAngle(0);
            BlockmapCamera.Instance.SetZoom(10);
            World.CameraJumpToFocusEntity(Crew[0]);

            // Vision
            World.DisplaySettings.SetVisionCutoffMode(VisionCutoffMode.RoomPerspectiveCutoff);
            World.DisplaySettings.SetVisionCutoffAltitude(WorldGenerator_Exodus.GROUND_FLOOR_ALTITUDE);
            World.DisplaySettings.SetVisionCutoffPerspectiveHeight(WorldGenerator_Exodus.FLOOR_HEIGHT - 1);
            World.DisplaySettings.SetVisionCutoffPerspectiveTarget(Crew[0]);
            World.SetActiveVisionActor(LocalPlayer);
            UI.LoadingScreenOverlay.SetActive(false);

            // Action queue
            ActionQueue = new PriorityQueue<EoaEntity>();
            foreach (EoaEntity e in Entities)
            {
                ActionQueue.Enqueue(e, e.NextActionTime.ValueInSeconds);
            }

            // Objects
            GlobalSimulationTime = new EoaTime();

            // Notify match readiness
            UI.OnGameStarting(this);

            GameState = EoaGameState.Running;

            StartNextTurn();
        }

        #region Udate Loop

        protected override void OnFixedUpdate() => World?.FixedUpdate();
        protected override void Render(float alpha) => World?.Render(alpha);
        
        protected override void OnFrame() { }

        protected override void Tick()
        {
            World?.Tick();

            if (GameState == EoaGameState.GeneratingWorld) MapGenerator.UpdateGeneration();

            if(GameState == EoaGameState.Running)
            {
                // Go to next turn
                if (!CurrentTurnEntity.IsInTurn)
                {
                    ActionQueue.Enqueue(CurrentTurnEntity, CurrentTurnEntity.NextActionTime.ValueInSeconds);
                    StartNextTurn();
                }
            }
        }

        protected override void HandleInputs()
        {
            if (GameState == EoaGameState.Running)
            {
                // Player turn
                if (CurrentTurnEntity.IsPlayerControlled && !CurrentTurnEntity.IsInAction)
                {
                    if (Input.GetKey(KeyCode.UpArrow))
                    {
                        Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 180);
                        if (CurrentTurnEntity.MoveActions.ContainsKey(dir))
                            CurrentTurnEntity.MoveActions[dir].Perform();
                    }
                    else if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 90);
                        if (CurrentTurnEntity.MoveActions.ContainsKey(dir))
                            CurrentTurnEntity.MoveActions[dir].Perform();
                    }
                    else if(Input.GetKey(KeyCode.DownArrow))
                    {
                        Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 0);
                        if (CurrentTurnEntity.MoveActions.ContainsKey(dir))
                            CurrentTurnEntity.MoveActions[dir].Perform();
                    }
                    else if(Input.GetKey(KeyCode.RightArrow))
                    {
                        Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 270);
                        if (CurrentTurnEntity.MoveActions.ContainsKey(dir))
                            CurrentTurnEntity.MoveActions[dir].Perform();
                    }
                }
            }
        }

        #endregion

        #region Turn Loop

        private void StartNextTurn()
        {
            CurrentTurnEntity = ActionQueue.Dequeue();
            SetGlobalSimulationTime(CurrentTurnEntity.NextActionTime.ValueInSeconds);
            CurrentTurnEntity.RefreshPossibleActions();
            CurrentTurnEntity.PerformNextAction();
        }

        private void SetGlobalSimulationTime(int secondsAbsolute)
        {
            GlobalSimulationTime.SetTime(CurrentTurnEntity.NextActionTime.ValueInSeconds);
            UI.RefreshTimeText();
        }


        #endregion
    }
}
