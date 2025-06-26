using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlockmapFramework;
using TMPro;
using BlockmapFramework.WorldGeneration;
using BlockmapFramework.Defs;
using TheThoriumChallenge.WorldGeneration;
using TheThoriumChallenge;

namespace WorldEditor
{
    public class BlockEditor : GameLoop
    {
        [Header("Prefabs")]
        public UI_EditorToolButton EditorToolButtonPrefab;
        public GameObject ArrowPrefab;

        [Header("Elements")]
        public GameObject ToolButtonContainer;
        public GameObject ToolNamePanel;
        public TextMeshProUGUI ToolNameText;
        public Dictionary<EditorToolId, UI_EditorToolButton> ToolButtons;
        public TextMeshProUGUI TileInfoText;
        public UI_ToolWindow ToolWindow;
        public UI_WorldDisplayOptions DisplayOptions;
        public GameObject AltitudeHelperPlane;

        [Header("Tools")]
        public WorldGenTool WorldGenTool;
        public GroundSculptingTool GroundSculptingTool;
        public NodeShapingTool NodeShapingTool;
        public SurfacePaintTool SurfacePaintTool;
        public VoidTool VoidTool;
        public AirNodeTool AirNodeTool;
        public SpawnCharacterTool SpawnCharacterTool;
        public SelectAndMoveTool SelectAndMoveTool;
        public SpawnObjectTool SpawnObjectTool;
        public ProceduralEntityTool ProceduralEntityTool;
        public WaterTool WaterTool;
        public FenceTool FenceTool;
        public LadderTool LadderTool;
        public WallTool WallTool;
        public DoorTool DoorTool;
        public MapGenFeatureTool MapGenFeatureTool;
        public WorldModifierTool WorldModifierTool;

        // Editor
        private bool isInitialized = false;
        float deltaTime; // for fps
        public List<WorldGenerator> Generators;
        private Dictionary<EditorToolId, EditorTool> Tools;
        public EditorTool CurrentTool;

        // Colors
        public static Color ButtonSelectedColor = new Color(0.4f, 0.4f, 0.11f);
        public static Color ButtonUnselectedColor = new Color(0.27f, 0.27f, 0.27f);

        public World World { get; private set; }

        void Start()
        {
            DefDatabaseRegistry.ClearAllDatabases();

            // Load defs
            DefDatabaseRegistry.AddAllGlobalDefs();
            DefDatabase<WorldModifierDef>.AddDefs(EditorDefs.WorldModifierDefs);
            DefDatabase<EntityDef>.AddDefs(EditorDefs.EntityDefs);
            DefDatabase<SkillDef>.AddDefs(CaptureTheFlag.SkillDefs.GetDefs());
            DefDatabase<StatDef>.AddDefs(CaptureTheFlag.StatDefs.GetDefs());
            DefDatabase<EntityDef>.AddDefs(CaptureTheFlag.EntityDefs.GetObjectDefs());
            DefDatabase<EntityDef>.AddDefs(CaptureTheFlag.EntityDefs.GetCharacterDefs());
            DefDatabase<EntityDef>.AddDefs(CaptureTheFlag.ItemDefs.GetDefs());
            DefDatabaseRegistry.ResolveAllReferences();
            DefDatabaseRegistry.OnLoadingDone();

            // Init materials
            MaterialManager.InitializeBlendableSurfaceMaterial();

            // Init generators
            Generators = new List<WorldGenerator>()
            {
                new WorldGenerator_Flat(),
                new WorldGenerator_Void(),
                new WorldGenerator_SimplePerlin(),
                new WorldGenerator_Forest(),
                new WorldGenerator_Parcels(),
                new WorldGenerator_Desert(),
                new WorldGenerator_Exodus(),
                new TtcStageGenerator_Forest(),
            };

            // Init tools
            Tools = new Dictionary<EditorToolId, EditorTool>()
            {
                { EditorToolId.WorldGen, WorldGenTool },
                { EditorToolId.GroundSculpting, GroundSculptingTool },
                { EditorToolId.NodeShaping, NodeShapingTool },
                { EditorToolId.SurfacePaint, SurfacePaintTool },
                { EditorToolId.Void, VoidTool },
                { EditorToolId.AirNode, AirNodeTool },
                { EditorToolId.SpawnObject, SpawnObjectTool },
                { EditorToolId.ProceduralEntity, ProceduralEntityTool },
                { EditorToolId.Fence, FenceTool },
                { EditorToolId.Wall, WallTool },
                { EditorToolId.Door, DoorTool },
                { EditorToolId.Ladder, LadderTool },
                { EditorToolId.Water, WaterTool },
                { EditorToolId.MapGenFeature, MapGenFeatureTool },
                { EditorToolId.WorldModifier, WorldModifierTool },
                { EditorToolId.SpawnCharacter, SpawnCharacterTool },
                { EditorToolId.SelectAndMove, SelectAndMoveTool },
            };
            foreach (EditorTool tool in Tools.Values) tool.Init(this);

            // Init tool buttons
            ToolButtons = new Dictionary<EditorToolId, UI_EditorToolButton>();  
            foreach (EditorTool tool in Tools.Values)
            {
                UI_EditorToolButton btn = Instantiate(EditorToolButtonPrefab, ToolButtonContainer.transform);
                btn.Init(this, tool);
                ToolButtons.Add(tool.Id, btn);
            }

            SelectTool(EditorToolId.WorldGen);

            // Start world generation process
            WorldGenTool.GenerateButton_OnClick();

            // Init display options
            DisplayOptions.Init(this);

            // Set initialized to true if everything here did run through without throwing an error
            isInitialized = true;
        }
        /// <summary>
        /// Sets a world object as the new world of this game.
        /// <br/>Also starts the initialization of that world (drawing, navmesh, vision etc.).
        /// </summary>
        public virtual void SetAndInitializeWorld(World world, System.Action callback = null)
        {
            // Destroy GameObject of previous world
            if (World != null) World.Destroy();

            // Set new world
            World = world;

            // Set initial display settings
            World.DisplaySettings.ShowTextures(true);
            World.DisplaySettings.ShowTileBlending(true);
            World.DisplaySettings.SetVisionCutoffPerspectiveHeight(4);

            // Start world initialization
            World.Initialize(() => { callback?.Invoke(); DisplayOptions.OnNewWorld(); });

            // Init hooks
            World.OnHoveredGroundNodeChanged += OnHoveredSurfaceNodeChanged;
            World.OnHoveredNodeChanged += OnHoveredNodeChanged;
            World.OnHoveredChunkChanged += OnHoveredChunkChanged;
            World.OnHoveredEntityChanged += OnHoveredEntityChanged;

            // Feedback
            foreach (EditorTool tool in Tools.Values) tool.OnNewWorld();
        }

        public void DestroyWorld()
        {
            Destroy(World.WorldObject);
            World = null;
        }

        protected override void HandleInputs()
        {
            // Click
            bool isMouseOverUi = HelperFunctions.IsMouseOverUi();
            HelperFunctions.UnfocusNonInputUiElements();
            bool isUiElementFocussed = HelperFunctions.IsUiFocussed();

            if (Input.GetMouseButtonDown(0) && !isMouseOverUi) CurrentTool.HandleLeftClick();
            if (Input.GetMouseButton(0) && !isMouseOverUi) CurrentTool.HandleLeftDrag();

            if (Input.GetMouseButtonDown(1) && !isMouseOverUi) CurrentTool.HandleRightClick();
            if (Input.GetMouseButton(1) && !isMouseOverUi) CurrentTool.HandleRightDrag();

            if (Input.GetMouseButtonDown(2) && !isMouseOverUi) CurrentTool.HandleMiddleClick();

            if (isUiElementFocussed) return; // Don't check for keyboard inputs when a ui element is focussed

            DisplayOptions.HandleKeyboardInputs();
            CurrentTool.HandleKeyboardInputs();
        }
        protected override void Tick()
        {
            World?.Tick();
        }
        protected override void OnFrame()
        {
            CurrentTool.UpdateTool();
        }
        protected override void Render(float alpha)
        {
            if (!isInitialized) return;
            UpdateHoverInfoText();

            World?.Render(alpha);
        }
        protected override void OnFixedUpdate()
        {
            World?.FixedUpdate();
        }

        #region Controls

        private void UpdateHoverInfoText()
        {
            if (World == null) return;

            bool isShowingExpandedDebugInfo = Input.GetKey(KeyCode.LeftAlt);

            string text = "";
            if (World.IsHoveringWorld) text += World.HoveredWorldCoordinates.ToString();
            if (World.HoveredNode != null)
            {
                if (isShowingExpandedDebugInfo) text += "\n" + World.HoveredNode.DebugInfoLong();
                else text += "\n" + World.HoveredNode;
            }
            if (World.HoveredEntity != null) text += "\nEntity: " + World.HoveredEntity.ToString();
            if (World.HoveredWaterBody != null) text += "\nWaterbody";
            if (World.HoveredFence != null) text += "\nFence: " + World.HoveredFence.ToString();
            if (World.HoveredWall != null)
            {
                if (isShowingExpandedDebugInfo) text += "\n" + World.HoveredWall.DebugInfoLong();
                else text += "\nWall: " + World.HoveredWall.ToString();
            }

            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";
            text += "\n" + GameLoop.TPS.ToString() + " TPS";
            text += "\nHold alt for more info";

            text = text.TrimStart('\n');
            TileInfoText.text = text;
        }

        private void OnHoveredSurfaceNodeChanged(GroundNode oldNode, GroundNode newNode)
        {
            CurrentTool.OnHoveredGroundNodeChanged(oldNode, newNode);
        }
        private void OnHoveredNodeChanged(BlockmapNode oldNode, BlockmapNode newNode)
        {
            CurrentTool.OnHoveredNodeChanged(oldNode, newNode);
        }
        private void OnHoveredChunkChanged(Chunk oldChunk, Chunk newChunk)
        {
            CurrentTool.OnHoveredChunkChanged(oldChunk, newChunk);
        }
        private void OnHoveredEntityChanged(Entity oldEntity, Entity newEntity)
        {
            CurrentTool.OnHoveredEntityChanged(oldEntity, newEntity);
        }

        private KeyCode GetKeycodeForNumber(int hotkeyNumber) => (KeyCode)(48 + hotkeyNumber);

        #endregion

        #region Tools

        public void SelectTool(EditorToolId id)
        {
            EditorTool oldTool = CurrentTool;
            EditorTool newTool = Tools[id];

            // Handle de-delection of previous tool
            if (oldTool != null)
            {
                ToolButtons[oldTool.Id].SetSelected(false);
                oldTool.OnDeselect();
            }

            // Handle selection of new tool
            ToolButtons[newTool.Id].SetSelected(true);
            newTool.OnSelect();
            ToolWindow.SelectTool(newTool);

            // Set new tool as current
            CurrentTool = newTool;
        }

        #endregion

    }


}
