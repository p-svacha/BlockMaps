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

namespace WorldEditor
{
    public class BlockEditor : BlockmapGame
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
        public MoveCharacterTool MoveCharacterTool;
        public SpawnObjectTool SpawnObjectTool;
        public ProceduralEntityTool ProceduralEntityTool;
        public WaterTool WaterTool;
        public FenceTool FenceTool;
        public LadderTool LadderTool;
        public WallTool WallTool;
        public DoorTool DoorTool;
        public MapGenFeatureTool MapGenFeatureTool;

        // Editor
        private bool isInitialized = false;
        float deltaTime; // for fps
        public List<WorldGenerator> Generators;
        private Dictionary<EditorToolId, EditorTool> Tools;
        public EditorTool CurrentTool;

        void Start()
        {
            // Load defs
            DefDatabaseRegistry.AddAllGlobalDefs();
            DefDatabase<EntityDef>.AddDefs(EditorDefs.EntityDefs);
            DefDatabase<EntityDef>.AddDefs(CaptureTheFlag.EntityDefs.Defs);
            DefDatabaseRegistry.ResolveAllReferences();
            DefDatabaseRegistry.OnLoadingDone();
            DefDatabaseRegistry.BindAllDefOfs();

            // Init materials
            MaterialManager.InitializeBlendableSurfaceMaterial();

            // Init generators
            Generators = new List<WorldGenerator>()
            {
                new FlatWorldGenerator(),
                new PerlinWorldGenerator(),
                new CaptureTheFlag.CTFMapGenerator_Forest(),
                new RegionMapGenerator(),
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
                { EditorToolId.SpawnCharacter, SpawnCharacterTool },
                { EditorToolId.MoveCharacter, MoveCharacterTool },
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

        public override void SetAndInitializeWorld(World world, System.Action onInitializationDoneCallback = null)
        {
            base.SetAndInitializeWorld(world, onInitializationDoneCallback);

            // Init hooks
            World.OnHoveredGroundNodeChanged += OnHoveredSurfaceNodeChanged;
            World.OnHoveredNodeChanged += OnHoveredNodeChanged;
            World.OnHoveredChunkChanged += OnHoveredChunkChanged;
            World.OnHoveredEntityChanged += OnHoveredEntityChanged;

            // Feedback
            foreach (EditorTool tool in Tools.Values) tool.OnNewWorld();
            DisplayOptions.OnNewWorld();
        }

        #region Controls

        protected override void Update()
        {
            if (!isInitialized) return;
            base.Update();

            UpdateHoverInfoText();

            CurrentTool.UpdateTool();

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

        private void UpdateHoverInfoText()
        {
            if (World == null) return;

            string text = "";
            if (World.IsHoveringWorld) text += World.HoveredWorldCoordinates.ToString();
            if (World.HoveredNode != null)
            {
                if(Input.GetKey(KeyCode.LeftAlt)) text += "\n" + World.HoveredNode.DebugInfoLong();
                else text += "\n" + World.HoveredNode.DebugInfoShort();
            }
            if (World.HoveredEntity != null) text += "\nEntity: " + World.HoveredEntity.ToString();
            if (World.HoveredWaterBody != null) text += "\nWaterbody";
            if (World.HoveredFence != null) text += "\nFence: " + World.HoveredFence.ToString();
            if (World.HoveredWall != null) text += "\nWall: " + World.HoveredWall.ToString();

            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";
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
