using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlockmapFramework;
using TMPro;

namespace WorldEditor
{
    public class BlockEditor : MonoBehaviour
    {
        [Header("Prefabs")]
        public UI_EditorToolButton EditorToolButtonPrefab;
        public EditorMovingEntity CharacterPrefab;
        public GameObject ArrowPrefab;
        public List<StaticEntity> StaticEntities;
        public List<MovingEntity> MovingEntityPresets;

        [Header("Elements")]
        public GameObject ToolButtonContainer;
        public Dictionary<EditorToolId, UI_EditorToolButton> ToolButtons;
        public TextMeshProUGUI TileInfoText;
        public UI_ToolWindow ToolWindow;
        public UI_WorldDisplayOptions DisplayOptions;

        [Header("Tools")]
        public WorldGenTool WorldGenTool;
        public TerrainTool TerrainTool;
        public SurfacePaintTool SurfacePaintTool;
        public AirNodeTool AirNodeTool;
        public AirSlopeNodeTool AirSlopeNodeTool;
        public SpawnCharacterTool SpawnCharacterTool;
        public MoveCharacterTool MoveCharacterTool;
        public SpawnObjectTool SpawnObjectTool;
        public ProceduralEntityTool ProceduralEntityTool;
        public WaterTool WaterTool;
        public WallTool WallTool;
        public LadderTool LadderTool;

        [Header("World")]
        public World World;

        // Editor
        public EditorEntityLibrary EntityLibrary { get; private set; }
        float deltaTime; // for fps
        public List<WorldGenerator> Generators;
        private Dictionary<EditorToolId, EditorTool> Tools;
        public EditorTool CurrentTool;

        void Start()
        {
            // Init editor content
            EntityLibrary = new EditorEntityLibrary();
            EntityLibrary.Init(this);

            // Init generators
            Generators = new List<WorldGenerator>()
            {
                new FlatWorldGenerator(),
                new PerlinWorldGenerator(),
                new CaptureTheFlag.CTFMapGenerator()
            };

            // Init tools
            Tools = new Dictionary<EditorToolId, EditorTool>()
            {
                { EditorToolId.WorldGen, WorldGenTool },
                { EditorToolId.Terrain, TerrainTool },
                { EditorToolId.SurfacePaint, SurfacePaintTool },
                { EditorToolId.AirNode, AirNodeTool },
                { EditorToolId.AirSlopeNode, AirSlopeNodeTool },
                { EditorToolId.SpawnObject, SpawnObjectTool },
                { EditorToolId.ProceduralEntity, ProceduralEntityTool },
                { EditorToolId.Wall, WallTool },
                { EditorToolId.Ladder, LadderTool },
                { EditorToolId.Water, WaterTool },
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

            // Generate world
            WorldGenTool.GenerateButton_OnClick();

            // Init display options
            DisplayOptions.Init(this);
        }

        public void SetWorld(WorldData data)
        {
            // Set new data
            SetWorld(World.Load(data, EntityLibrary));
        }
        public void SetWorld(World world)
        {
            // Clear old data
            if (World != null) Destroy(World.gameObject);

            // Assign new data
            World = world;

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

        void Update()
        {
            UpdateTileInfoText();

            CurrentTool.UpdateTool();

            // Click
            bool isMouseOverUi = HelperFunctions.IsMouseOverUi();
            HelperFunctions.UnfocusNonInputUiElements();
            bool isUiElementFocussed = HelperFunctions.IsUiFocussed();

            if (Input.GetMouseButtonDown(0) && !isMouseOverUi) CurrentTool.HandleLeftClick();
            if (Input.GetMouseButton(0) && !isMouseOverUi) CurrentTool.HandleLeftDrag();

            if (Input.GetMouseButtonDown(1) && !isMouseOverUi) CurrentTool.HandleRightClick();
            if (Input.GetMouseButton(1) && !isMouseOverUi) CurrentTool.HandleLeftDrag();

            if (Input.GetMouseButtonDown(2) && !isMouseOverUi) CurrentTool.HandleMiddleClick();


            if (isUiElementFocussed) return; // no input key checks when a ui element is focussed

            // G - Show/Hide Grid
            if (Input.GetKeyDown(KeyCode.G))
            {
                World.ToggleGridOverlay();
                DisplayOptions.UpdateValues();
            }

            // P - Visualize Pathfinding
            if (Input.GetKeyDown(KeyCode.N))
            {
                World.ToggleNavmesh();
                DisplayOptions.UpdateValues();
            }

            // T - Texture mode
            if (Input.GetKeyDown(KeyCode.T))
            {
                World.ToggleTextureMode();
                DisplayOptions.UpdateValues();
            }

            // B - Surface blending
            if (Input.GetKeyDown(KeyCode.B))
            {
                World.ToggleTileBlending();
                DisplayOptions.UpdateValues();
            }
        }

        private void UpdateTileInfoText()
        {
            if (World == null) return;

            string text = "";
            if (World.IsHoveringWorld) text += World.HoveredWorldCoordinates.ToString();
            if (World.HoveredNode != null)
            {
                text += "\n" + World.HoveredNode.ToString();
                text += "\nShape: " + World.HoveredNode.Shape;
                text += "\nRelHeight: " + World.HoveredNode.GetRelativeHeightAt(new Vector2(World.HoveredWorldPosition.x - World.HoveredWorldCoordinates.x, World.HoveredWorldPosition.z - World.HoveredWorldCoordinates.y));
            }
            if (World.HoveredEntity != null) text += "\nEntity:" + World.HoveredEntity.TypeId;
            if (World.HoveredWaterBody != null) text += "\nWaterbody";
            if (World.HoveredWall != null) text += "\nWall: " + World.HoveredWall.Node.WorldCoordinates.ToString() + " " + World.HoveredWall.Node.BaseHeight + " " + World.HoveredWall.Side.ToString();

            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";

            text = text.TrimStart('\n');
            TileInfoText.text = text;
        }

        private void OnHoveredSurfaceNodeChanged(GroundNode oldNode, GroundNode newNode)
        {
            CurrentTool.OnHoveredSurfaceNodeChanged(oldNode, newNode);
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
