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

        [Header("Elements")]
        public GameObject ToolButtonContainer;
        public Dictionary<EditorToolId, UI_EditorToolButton> ToolButtons;
        public TextMeshProUGUI TileInfoText;
        public UI_ToolWindow ToolWindow;

        [Header("Tools")]
        public WorldGenTool WorldGenTool;
        public TerrainTool TerrainTool;
        public SurfacePaintTool SurfacePaintTool;
        public SurfacePathTool SurfacePathTool;
        public AirNodeTool AirNodeTool;
        public AirSlopeNodeTool AirSlopeNodeTool;
        public SpawnCharacterTool SpawnEntityTool;
        public SpawnObjectTool SpawnObjectTool;
        public MoveEntityTool MoveEntityTool;
        public WaterTool WaterTool;
        public WallTool WallTool;

        [Header("World")]
        public World World;

        // Editor
        private EditorEntityLibrary ContentLibrary;
        float deltaTime; // for fps
        private Dictionary<EditorToolId, EditorTool> Tools;
        public EditorTool CurrentTool;
        public Player EditorPlayer { get; private set; }

        void Start()
        {
            // Init editor content
            ContentLibrary = new EditorEntityLibrary();
            ContentLibrary.Init(this);

            // Init world
            WorldData data = BaseWorldGenerator.GenerateWorld("TestWorld", 16, 2);
            SetWorld(data);

            // Init tools
            Tools = new Dictionary<EditorToolId, EditorTool>()
            {
                { EditorToolId.WorldGen, WorldGenTool },
                { EditorToolId.Terrain, TerrainTool },
                { EditorToolId.SurfacePaint, SurfacePaintTool },
                { EditorToolId.SurfacePath, SurfacePathTool },
                { EditorToolId.AirNode, AirNodeTool },
                { EditorToolId.AirSlopeNode, AirSlopeNodeTool },
                { EditorToolId.SpawnCharacter, SpawnEntityTool },
                { EditorToolId.SpawnObject, SpawnObjectTool },
                { EditorToolId.MoveEntity, MoveEntityTool },
                { EditorToolId.Water, WaterTool },
                { EditorToolId.Wall, WallTool },
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
        }

        public void SetWorld(WorldData data)
        {
            // Clear old data
            if (World != null) Destroy(World.gameObject);

            // Set new data
            World = World.Load(data, ContentLibrary);

            // Add editor player
            if (!World.Players.ContainsKey(0))
            {
                EditorPlayer = new Player(World, 0, "Player");
                World.AddPlayer(EditorPlayer);
            }

            // Init hooks
            World.OnHoveredSurfaceNodeChanged += OnHoveredSurfaceNodeChanged;
            World.OnHoveredNodeChanged += OnHoveredNodeChanged;
            World.OnHoveredChunkChanged += OnHoveredChunkChanged;
            World.OnHoveredEntityChanged += OnHoveredEntityChanged;
        }

        #region Controls

        void Update()
        {
            UpdateTileInfoText();

            CurrentTool.UpdateTool();

            // Click
            bool isMouseOverUi = EventSystem.current.IsPointerOverGameObject();

            if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<Button>() != null)
                EventSystem.current.SetSelectedGameObject(null); // Unfocus any focussed button so it doesn't block input controls

            bool isUiElementFocussed = EventSystem.current.currentSelectedGameObject != null;

            if (Input.GetMouseButtonDown(0) && !isMouseOverUi) CurrentTool.HandleLeftClick();
            if (Input.GetMouseButton(0) && !isMouseOverUi) CurrentTool.HandleLeftDrag();

            if (Input.GetMouseButtonDown(1) && !isMouseOverUi) CurrentTool.HandleRightClick();
            if (Input.GetMouseButton(1) && !isMouseOverUi) CurrentTool.HandleLeftDrag();


            if (isUiElementFocussed) return; // no input key checks when a ui element is focussed

            // G - Show/Hide Grid
            if (Input.GetKeyDown(KeyCode.G)) World.ToggleGridOverlay();

            // P - Visualize Pathfinding
            if(Input.GetKeyDown(KeyCode.P)) World.TogglePathfindingVisualization();

            // T - Texture mode
            if (Input.GetKeyDown(KeyCode.T)) World.ToggleTextureMode();

            // B - Surface blending
            if (Input.GetKeyDown(KeyCode.B)) World.ToggleTileBlending();

            // V - Visibility
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (World.IsAllVisible) World.SetActiveVisionPlayer(EditorPlayer);
                else World.SetActiveVisionPlayer(null);
            }

            // R - Reset explored nodes
            if (Input.GetKeyDown(KeyCode.R)) World.ResetExploration(EditorPlayer);
        }

        private void UpdateTileInfoText()
        {
            string text = "";
            if (World.IsHoveringWorld) text += World.HoveredWorldCoordinates.ToString();
            if (World.HoveredNode != null) text += "\n" + World.HoveredNode.Type.ToString() + " | " + World.HoveredNode.Surface.Name;
            if (World.HoveredEntity != null) text += "\nEntity:" + World.HoveredEntity.TypeId;
            if (World.HoveredWaterBody != null) text += "\nWaterbody";
            if (World.HoveredWall != null) text += "\nWall: " + World.HoveredWall.Node.WorldCoordinates.ToString() + " " + World.HoveredWall.Node.BaseHeight + " " + World.HoveredWall.Side.ToString();

            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text += "\n" + Mathf.Ceil(fps).ToString() + " FPS";

            text = text.TrimStart('\n');
            TileInfoText.text = text;
        }

        private void OnHoveredSurfaceNodeChanged(SurfaceNode oldNode, SurfaceNode newNode)
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
