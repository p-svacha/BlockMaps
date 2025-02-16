using BlockmapFramework;
using TheThoriumChallenge.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TheThoriumChallenge
{
    public class Game : GameLoop
    {
        public static Game Instance;

        public GameUI UI { get; private set; }
        public GameState GameState { get; private set; }
        public TtcWorldGenerator ActiveLevelGenerator { get; private set; }
        public Stage CurrentStage { get; private set; }
        public List<CreatureInfo> PlayerCreatures { get; private set; }

        public Actor LocalPlayer => CurrentStage.LocalPlayer;


        private List<TtcWorldGenerator> LevelGenerators = new List<TtcWorldGenerator>()
        {
            new TtcWorldGenerator_Forest()
        };

        private void Awake()
        {
            GameState = GameState.StartingGame;
            Instance = this;
            UI = GameObject.Find("GameUI").GetComponent<GameUI>();
        }

        void Start()
        {
            InitializeGame();
        }


        private void InitializeGame()
        {
            // Load defs
            DefDatabaseRegistry.AddAllGlobalDefs();
            DefDatabase<SkillDef>.AddDefs(SkillDefs.GetDefs());
            DefDatabase<StatDef>.AddDefs(StatDefs.GetDefs());
            DefDatabase<AbilityDef>.AddDefs(AbilityDefs.GetDefs());
            DefDatabase<EntityDef>.AddDefs(SpeciesDefs.GetDefs());
            DefDatabaseRegistry.ResolveAllReferences();
            DefDatabaseRegistry.OnLoadingDone();

            // Init materials
            MaterialManager.InitializeBlendableSurfaceMaterial();

            // Choose a random starting creatures
            SetStartingCreatures();

            // Start world generation of first level
            InitNextLevel(Biome.Forest);
        }

        private void SetStartingCreatures()
        {
            PlayerCreatures = new List<CreatureInfo>()
            {
                new CreatureInfo()
                {
                    SpeciesDef = SpeciesDefOf.Squishgrub,
                    Level = 5,
                },
                new CreatureInfo()
                {
                    SpeciesDef = SpeciesDefOf.Squishgrub,
                    Level = 6,
                },
            };
        }

        /// <summary>
        /// Starts generating the next level.
        /// </summary>
        private void InitNextLevel(Biome biome)
        {
            UI.LoadingScreenOverlay.SetActive(true);
            GameState = GameState.LoadingLevel_GeneratingWorld;
            CurrentStage = null;
            ActiveLevelGenerator = LevelGenerators.First(x => x.Biome == biome);
            ActiveLevelGenerator.StartLevelGeneration(PlayerCreatures, onDoneCallback: OnWorldGenerationDone);
        }

        private void OnWorldGenerationDone()
        {
            CurrentStage = ActiveLevelGenerator.GetLevel(this);

            // Start world initialization
            CurrentStage.World.Initialize(OnWorldInitializationDone);
            GameState = GameState.LoadingLevel_InitializingWorld;
        }

        private void OnWorldInitializationDone()
        {
            UI.OnGameStarting(this);
            CurrentStage.Start();

            GameState = GameState.Running;
            UI.LoadingScreenOverlay.SetActive(false);
        }

        #region Udate Loop

        protected override void OnFixedUpdate() => CurrentStage?.World?.FixedUpdate();
        protected override void Render(float alpha) => CurrentStage?.World?.Render(alpha);
        
        protected override void OnFrame() { }

        protected override void Tick()
        {
            CurrentStage?.World?.Tick();

            if (GameState == GameState.LoadingLevel_GeneratingWorld) ActiveLevelGenerator.UpdateGeneration();

            if (GameState == GameState.Running) CurrentStage.Tick();
        }

        protected override void HandleInputs()
        {
            if (GameState == GameState.Running) CurrentStage.HandleInputs();
        }

        #endregion


    }
}
