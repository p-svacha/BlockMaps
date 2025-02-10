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
        public Level CurrentLevel { get; private set; }
        public List<CreatureInfo> PlayerCreatures { get; private set; }

        public Actor LocalPlayer => CurrentLevel.LocalPlayer;


        private List<TtcWorldGenerator> LevelGenerators = new List<TtcWorldGenerator>()
        {
            new TtcWorldGenerator_Forest()
        };

        private void Awake()
        {
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
            DefDatabase<SpeciesStatDef>.AddDefs(SpeciesStatDefs.Defs);
            DefDatabase<CreatureStatDef>.AddDefs(CreatureStatDefs.Defs);
            DefDatabase<AbilityDef>.AddDefs(AbilityDefs.Defs);
            DefDatabase<SpeciesDef>.AddDefs(SpeciesDefs.Defs);
            DefDatabaseRegistry.ResolveAllReferences();
            DefDatabaseRegistry.BindAllDefOfs();
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
                    Def = SpeciesDefOf.Needlegrub,
                    Level = 5,
                },
                new CreatureInfo()
                {
                    Def = SpeciesDefOf.Needlegrub,
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
            CurrentLevel = null;
            ActiveLevelGenerator = LevelGenerators.First(x => x.Biome == biome);
            ActiveLevelGenerator.StartLevelGeneration(PlayerCreatures, onDoneCallback: OnWorldGenerationDone);
        }

        private void OnWorldGenerationDone()
        {
            CurrentLevel = ActiveLevelGenerator.GetLevel(this);

            // Start world initialization
            CurrentLevel.World.Initialize(OnWorldInitializationDone);
            GameState = GameState.LoadingLevel_InitializingWorld;
        }

        private void OnWorldInitializationDone()
        {
            UI.OnGameStarting(this);
            CurrentLevel.Start();

            GameState = GameState.Running;
            UI.LoadingScreenOverlay.SetActive(false);
        }

        #region Udate Loop

        protected override void OnFixedUpdate() => CurrentLevel?.World?.FixedUpdate();
        protected override void Render(float alpha) => CurrentLevel?.World?.Render(alpha);
        
        protected override void OnFrame() { }

        protected override void Tick()
        {
            CurrentLevel?.World?.Tick();

            if (GameState == GameState.LoadingLevel_GeneratingWorld) ActiveLevelGenerator.UpdateGeneration();

            if (GameState == GameState.Running) CurrentLevel.Tick();
        }

        protected override void HandleInputs()
        {
            if (GameState == GameState.Running) CurrentLevel.HandleInputs();
        }

        #endregion


    }
}
