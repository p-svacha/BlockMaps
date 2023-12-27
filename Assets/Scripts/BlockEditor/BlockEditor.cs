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
        public EditorToolButton EditorToolButtonPrefab;
        public MovingEntity CharacterPrefab;
        public GameObject ArrowPrefab;

        [Header("Elements")]
        public GameObject ToolButtonContainer;
        public Dictionary<EditorToolId, EditorToolButton> ToolButtons;
        public TextMeshProUGUI TileInfoText;
        public UI_ToolWindow ToolWindow;

        [Header("Tools")]
        public WorldGenTool WorldGenTool;
        public TerrainTool TerrainTool;
        public SurfacePaintTool SurfacePaintTool;
        public SurfacePathTool SurfacePathTool;
        public AirNodeTool AirNodeTool;
        public AirSlopeNodeTool AirSlopeNodeTool;

        [Header("World")]
        public World World;

        // Editor
        float deltaTime; // for fps
        private Dictionary<EditorToolId, EditorTool> Tools;
        public EditorTool CurrentTool;

        void Start()
        {
            // Init world
            WorldData data = BaseWorldGenerator.GenerateWorld("TestWorld", 16, 4);
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
            };
            foreach (EditorTool tool in Tools.Values) tool.Init(this);

            // Init tool buttons
            ToolButtons = new Dictionary<EditorToolId, EditorToolButton>();  
            foreach (EditorTool tool in Tools.Values)
            {
                EditorToolButton btn = Instantiate(EditorToolButtonPrefab, ToolButtonContainer.transform);
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
            World = World.Load(data);
            World.Draw();

            // Init hooks
            World.OnHoveredSurfaceNodeChanged += OnHoveredSurfaceNodeChanged;
            World.OnHoveredNodeChanged += OnHoveredNodeChanged;
            World.OnHoveredChunkChanged += OnHoveredChunkChanged;
        }

        #region Controls

        void Update()
        {
            UpdateTileInfoText();

            CurrentTool.UpdateTool();

            // Click
            bool isMouseOverUi = EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && !isMouseOverUi) CurrentTool.HandleLeftClick();
            if (Input.GetMouseButtonDown(1) && !isMouseOverUi) CurrentTool.HandleRightClick();

            // New Pathfinding Entity
            if(Input.GetKeyDown(KeyCode.Space))
            {
                BlockmapNode spawnNode = World.GetRandomOwnedTerrainNode();

                GameObject characterContainer = new GameObject("Character");
                characterContainer.transform.SetParent(World.transform);

                MovingEntity newCharacter = Instantiate(CharacterPrefab, characterContainer.transform);
                newCharacter.Init(World, spawnNode, new bool[,,] { { { true } } }, World.Gaia, visionRange: 10.6f);
                World.AddEntity(newCharacter);
            }

            // Show/Hide Grid
            if (Input.GetKeyDown(KeyCode.G)) World.ToggleGridOverlay();

            // Visualize Pathfinding
            if(Input.GetKeyDown(KeyCode.P)) World.TogglePathfindingVisualization();

            // Texture mode
            if (Input.GetKeyDown(KeyCode.T)) World.ToggleTextureMode();

            // Surface blending
            if (Input.GetKeyDown(KeyCode.B)) World.ToggleTileBlending();

            // Visibility
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (World.IsAllVisible) World.SetActiveVisionPlayer(World.Gaia);
                else World.SetActiveVisionPlayer(null);
            }

            // Tool Selection
            foreach (EditorTool tool in Tools.Values)
            {
                if (Input.GetKeyDown(GetKeycodeForNumber(tool.HotkeyNumber)) && EventSystem.current.currentSelectedGameObject == null)
                    SelectTool(tool.Id);
            }
        }

        private void UpdateTileInfoText()
        {
            string text = "";
            if(World.IsHoveringWorld) text += World.HoveredWorldCoordinates.ToString();
            if(World.HoveredNode != null) text += "\n" + World.HoveredNode.Type.ToString() + " | " + World.HoveredNode.Surface.Name;

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
