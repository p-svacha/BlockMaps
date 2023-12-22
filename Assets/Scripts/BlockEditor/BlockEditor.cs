using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BlockmapFramework;

namespace WorldEditor
{
    public class BlockEditor : MonoBehaviour
    {
        [Header("Prefabs")]
        public MovingEntity CharacterPrefab;
        public GameObject ArrowPrefab;
        public List<StaticEntity> BuildingPrefabs;

        [Header("Textures")]
        public Texture2D TileSelector;
        public Texture2D TileSelectorN;
        public Texture2D TileSelectorE;
        public Texture2D TileSelectorS;
        public Texture2D TileSelectorW;
        public Texture2D TileSelectorNE;
        public Texture2D TileSelectorNW;
        public Texture2D TileSelectorSW;
        public Texture2D TileSelectorSE;

        [Header("UI")]
        public List<EditorToolButton> ToolButtons;
        public Text TileInfoText;

        [Header("World")]
        public World World;

        public List<Entity> Entities = new List<Entity>();

        

        public EditorTool CurrentTool;

        [Header("Build")]
        public int BuildHeight;
        public int BuildRotation;
        public int BuildingIndex;
        public Direction BuildRotationDirection;
        public GameObject PathPreview;
        public StaticEntity BuildingPreview;

        void Start()
        {
            int chunkSize = 12;

            WorldData data = BaseWorldGenerator.GenerateWorld("TestWorld", chunkSize, 3);
            World = World.Load(data);
            World.Draw();

            CurrentTool = EditorTool.Terrain;

            foreach (EditorToolButton btn in ToolButtons) btn.GetComponent<Button>().onClick.AddListener(() => SelectTool(btn.Tool));
            SelectTool(EditorTool.Terrain);

            BuildHeight = 3;
            BuildRotation = 0;
            BuildRotationDirection = GetDirectionFromRotation(BuildRotation);

            // Hooks
            World.OnHoveredChunkChanged += OnHoveredChunkChanged;
        }

        #region Controls

        void Update()
        {
            if (World.HoveredSurfaceNode != null) TileInfoText.text = World.HoveredWorldCoordinates.ToString() + " (" + World.HoveredSurfaceNode.Shape + ") " + World.HoveredSurfaceNode.Surface.Name;
            else TileInfoText.text = World.HoveredWorldCoordinates.ToString();

            // Hover
            HandleHoveredObjects();

            // Click
            if (Input.GetMouseButtonDown(0)) HandleLeftClick();
            if (Input.GetMouseButtonDown(1)) HandleRightClick();

            // New Pathfinding Entity
            if(Input.GetKeyDown(KeyCode.Space))
            {
                MovingEntity newCharacter = Instantiate(CharacterPrefab);
                newCharacter.gameObject.layer = World.Layer_Entity;
                Entities.Add(newCharacter);
                BlockmapNode node = World.GetRandomOwnedTerrainNode();
                newCharacter.Init(World, node, new bool[,,] { { { true } } });
            }

            // Show/Hide Grid
            if (Input.GetKeyDown(KeyCode.G)) World.ToggleGridOverlay();

            // Visualize Pathfinding
            if(Input.GetKeyDown(KeyCode.P)) World.TogglePathfindingVisualization();

            // Raise/Lower
            if (Input.GetKeyDown(KeyCode.R)) SetHeight(BuildHeight + 1);
            if (Input.GetKeyDown(KeyCode.F)) SetHeight(BuildHeight - 1);

            // Rotate
            if (Input.GetKeyDown(KeyCode.X))
            {
                BuildRotation = (BuildRotation + 90) % 360;
                BuildRotationDirection = GetDirectionFromRotation(BuildRotation);
            }

            // Next Building
            if (Input.GetKeyDown(KeyCode.B)) SwitchBuildingIndex();

            // Tool Selection
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectTool(EditorTool.Terrain);
            if(Input.GetKeyDown(KeyCode.Alpha2)) SelectTool(EditorTool.SurfacePath);
            if(Input.GetKeyDown(KeyCode.Alpha3)) SelectTool(EditorTool.AirPath);
            if(Input.GetKeyDown(KeyCode.Alpha4)) SelectTool(EditorTool.Stairs);
        }

        private void OnHoveredChunkChanged(Chunk oldChunk, Chunk newChunk)
        {
            switch (CurrentTool)
            {
                case EditorTool.Terrain:
                case EditorTool.SurfacePath:
                    if (oldChunk != null) oldChunk.ShowSurfaceTileOverlay(false);
                    if (newChunk != null) newChunk.ShowSurfaceTileOverlay(true);
                    break;
            }
        }

        /// <summary>
        /// Handles hovered objects based on current tool.
        /// </summary>
        private void HandleHoveredObjects()
        {
            switch(CurrentTool)
            {
                case EditorTool.Terrain:
                    if(World.HoveredChunk != null && World.HoveredSurfaceNode != null)
                    {
                        Texture2D overlayTexture = GetTextureForHoverMode(World.NodeHoverMode);
                        bool canIncrease = World.HoveredSurfaceNode.CanChangeHeight(World.HoveredSurfaceNode, increase: true, World.NodeHoverMode);
                        bool canDecrease = World.HoveredSurfaceNode.CanChangeHeight(World.HoveredSurfaceNode, increase: false, World.NodeHoverMode);
                        Color c = Color.white;
                        if (canIncrease && canDecrease) c = Color.white;
                        else if (canIncrease) c = Color.green;
                        else if (canDecrease) c = Color.yellow;
                        else c = Color.red;
                        World.HoveredChunk.ShowSurfaceTileOverlay(overlayTexture, World.HoveredSurfaceNode.LocalCoordinates, c);
                    }
                    break;

                case EditorTool.SurfacePath:
                    if (World.HoveredSurfaceNode != null)
                    {
                        Texture2D overlayTexture = GetTextureForHoverMode(Direction.None);
                        Color c = World.CanBuildSurfacePath(World.HoveredSurfaceNode) ? Color.white : Color.red;
                        World.HoveredChunk.ShowSurfaceTileOverlay(overlayTexture, World.HoveredSurfaceNode.LocalCoordinates, c);
                    }
                    break;

                case EditorTool.AirPath:
                    if(World.HoveredSurfaceNode != null)
                    {
                        Vector3 hoverPos = World.HoveredSurfaceNode.GetCenterWorldPosition();
                        PathPreview.transform.position = new Vector3(hoverPos.x, World.TILE_HEIGHT * BuildHeight + World.TILE_HEIGHT * 0.5f, hoverPos.z);
                        PathPreview.transform.rotation = Quaternion.Euler(0f, BuildRotation, 0f);
                        PathPreview.GetComponentInChildren<MeshRenderer>().material.color = World.CanBuildAirPath(World.HoveredWorldCoordinates, BuildHeight) ? Color.white : Color.red;
                    }
                    break;

                case EditorTool.Stairs:
                    if (World.HoveredSurfaceNode != null)
                    {
                        Vector3 hoverPos = World.HoveredSurfaceNode.GetCenterWorldPosition();
                        PathPreview.transform.position = new Vector3(hoverPos.x, World.TILE_HEIGHT * BuildHeight + World.TILE_HEIGHT * 0.5f, hoverPos.z);
                        PathPreview.transform.rotation = Quaternion.Euler(0f, BuildRotation, 0f);
                        PathPreview.GetComponentInChildren<MeshRenderer>().material.color = World.CanBuildAirSlope(World.HoveredWorldCoordinates, BuildHeight, BuildRotationDirection) ? Color.white : Color.red;
                    }
                    break;

            }
        }

        /// <summary>
        /// Called every frame when left mouse button is pressed down.
        /// </summary>
        private void HandleLeftClick()
        {
            bool isUiClick = EventSystem.current.IsPointerOverGameObject();
            switch(CurrentTool)
            {
                case EditorTool.Terrain:
                    if(!isUiClick && World.HoveredSurfaceNode != null && World.HoveredSurfaceNode.CanChangeHeight(World.HoveredSurfaceNode, increase: true, World.NodeHoverMode)) World.HoveredSurfaceNode.ChangeHeight(World.NodeHoverMode, isIncrease: true);
                    break;
                    
                case EditorTool.SurfacePath:
                    if (!isUiClick && World.HoveredSurfaceNode != null && World.CanBuildSurfacePath(World.HoveredSurfaceNode)) World.BuildSurfacePath(World.HoveredSurfaceNode);
                    break;

                case EditorTool.AirPath:
                    if (!isUiClick && World.HoveredSurfaceNode != null && World.CanBuildAirPath(World.HoveredWorldCoordinates, BuildHeight)) World.BuildAirPath(World.HoveredWorldCoordinates, BuildHeight);
                    break;

                case EditorTool.Stairs:
                    if (!isUiClick && World.HoveredSurfaceNode != null && World.CanBuildAirSlope(World.HoveredWorldCoordinates, BuildHeight, BuildRotationDirection)) World.BuildAirSlope(World.HoveredWorldCoordinates, BuildHeight, BuildRotationDirection);
                    break;
            }
        }

        /// <summary>
        /// Called every frame when right mouse button is pressed down.
        /// </summary>
        private void HandleRightClick()
        {
            bool isUiClick = EventSystem.current.IsPointerOverGameObject();
            switch (CurrentTool)
            {
                case EditorTool.Terrain:
                    if (World.HoveredSurfaceNode != null && !isUiClick && World.HoveredSurfaceNode.CanChangeHeight(World.HoveredSurfaceNode, increase: false, World.NodeHoverMode)) World.HoveredSurfaceNode.ChangeHeight(World.NodeHoverMode, isIncrease: false);
                    break;
            }
        }



        private Texture2D GetTextureForHoverMode(Direction mode)
        {
            if (mode == Direction.None) return TileSelector;
            if (mode == Direction.N) return TileSelectorN;
            if (mode == Direction.E) return TileSelectorE;
            if (mode == Direction.S) return TileSelectorS;
            if (mode == Direction.W) return TileSelectorW;
            if (mode == Direction.NE) return TileSelectorNE;
            if (mode == Direction.NW) return TileSelectorNW;
            if (mode == Direction.SW) return TileSelectorSW;
            if (mode == Direction.SE) return TileSelectorSE;
            return null;
        }

        private Direction GetDirectionFromRotation(int rotation)
        {
            if (rotation == 0) return Direction.N;
            if (rotation == 90) return Direction.E;
            if (rotation == 180) return Direction.S;
            if (rotation == 270) return Direction.W;
            return Direction.None;
        }

        #endregion

        #region Tools

        private void SetHeight(int value)
        {
            BuildHeight = value;
            BuildHeight = Mathf.Clamp(BuildHeight, 0, World.MAX_HEIGHT);
        }

        private void SwitchBuildingIndex()
        {
            BuildingIndex = (BuildingIndex + 1) % BuildingPrefabs.Count;
        }

        public void SelectTool(EditorTool tool)
        {
            EditorTool oldTool = CurrentTool;
            EditorTool newTool = tool;

            ToolButtons.First(x => x.Tool == oldTool).SetSelected(false);
            CurrentTool = newTool;
            ToolButtons.First(x => x.Tool == newTool).SetSelected(true);

            // Handle de-delection of previous tool
            switch(oldTool)
            {
                case EditorTool.Terrain:
                case EditorTool.SurfacePath:
                    if (World.HoveredSurfaceNode != null) World.HoveredChunk.ShowSurfaceTileOverlay(false);
                    break;

                case EditorTool.AirPath:
                case EditorTool.Stairs:
                    Destroy(PathPreview);
                    PathPreview = null;
                    break;
            }

            // Handle selection of new tool
            switch(newTool)
            {
                case EditorTool.Terrain:
                case EditorTool.SurfacePath:
                    break;

                case EditorTool.AirPath:
                    PathPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(PathPreview.GetComponent<BoxCollider>());
                    PathPreview.transform.localScale = new Vector3(1f, World.TILE_HEIGHT, 1f);
                    break;

                case EditorTool.Stairs:
                    PathPreview = new GameObject("ArrowContainer");
                    GameObject arrowObject = Instantiate(ArrowPrefab, PathPreview.transform);
                    arrowObject.transform.localPosition = new Vector3(-0.6f, -0.5f, -1.9f);
                    arrowObject.transform.localRotation = Quaternion.Euler(20f, 180f, 0f);
                    arrowObject.transform.localScale = new Vector3(1f, 1f, 1.8f);
                    PathPreview.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                    break;
            }

            
        }

        #endregion

    }


}
