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

        [Header("UI")]
        public List<EditorToolButton> ToolButtons;
        public Text TileInfoText;

        public EditorCamera Camera;

        [Header("World")]
        public World World;
        public SurfaceNode HoveredSurfaceNode;

        private float HoverEdgeSensitivity = 0.3f; // how close to an edge you have to go for edge selection
        public Vector2Int HoveredWorldCoordinates;
        public Vector2Int HoveredLocalCoordinates;
        private Direction TileHoverMode;

        public List<Entity> Entities = new List<Entity>();

        private bool IsShowingGrid = true;
        private bool IsShowingPathfindingGraph;

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

            Camera.SetPosition(new Vector2(chunkSize * 0.75f, chunkSize * 0.75f));
            Camera.SetZoom(10f);
            Camera.SetAngle(45);

            WorldData data = BaseWorldGenerator.GenerateWorld("TestWorld", chunkSize, 3);
            World = World.Load(data);
            World.Draw();

            CurrentTool = EditorTool.Terrain;

            foreach (EditorToolButton btn in ToolButtons) btn.GetComponent<Button>().onClick.AddListener(() => SelectTool(btn.Tool));
            SelectTool(EditorTool.Terrain);

            BuildHeight = 3;
            BuildRotation = 0;
            BuildRotationDirection = GetDirectionFromRotation(BuildRotation);
        }

        #region Controls

        void Update()
        {
            // Hover
            UpdateHoveredObjects();
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
            if(Input.GetKeyDown(KeyCode.G))
            {
                IsShowingGrid = !IsShowingGrid;
                UpdateGridOverlay();
            }

            // Visualize Pathfinding
            if(Input.GetKeyDown(KeyCode.P))
            {
                IsShowingPathfindingGraph = !IsShowingPathfindingGraph;
                if (IsShowingPathfindingGraph) PathfindingGraphVisualizer.Singleton.VisualizeGraph(World);
                else PathfindingGraphVisualizer.Singleton.ClearVisualization();
            }

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
            if(Input.GetKeyDown(KeyCode.Alpha2)) SelectTool(EditorTool.WorldExpand);
            if(Input.GetKeyDown(KeyCode.Alpha3)) SelectTool(EditorTool.SurfacePath);
            if(Input.GetKeyDown(KeyCode.Alpha4)) SelectTool(EditorTool.AirPath);
            if(Input.GetKeyDown(KeyCode.Alpha5)) SelectTool(EditorTool.Stairs);
            if(Input.GetKeyDown(KeyCode.Alpha6)) SelectTool(EditorTool.Building);
        }

        /// <summary>
        /// Updates all hovered that are currently hovered by the mouse in different layers.
        /// </summary>
        private void UpdateHoveredObjects()
        {
            RaycastHit hit;
            Ray ray = Camera.Camera.ScreenPointToRay(Input.mousePosition);

            SurfaceNode newHoveredSurfaceNode = null;

            if (Physics.Raycast(ray, out hit, 1000f, ~World.Layer_Terrain))
            {
                Transform objectHit = hit.transform;

                if (objectHit != null)
                {
                    Vector3 hitPosition = hit.point;
                    TileHoverMode = GetTileHoverMode(hitPosition);

                    HoveredWorldCoordinates = World.WorldPositionToWorldCoordinates(hitPosition);
                    newHoveredSurfaceNode = World.GetSurfaceNode(HoveredWorldCoordinates);
                    if (newHoveredSurfaceNode != null)
                    {
                        HoveredLocalCoordinates = newHoveredSurfaceNode.LocalCoordinates;
                        TileInfoText.text = HoveredWorldCoordinates.ToString() + " (" + newHoveredSurfaceNode.Shape + ") " + newHoveredSurfaceNode.Surface.Name;
                    }
                    else
                    {
                        TileInfoText.text = "";
                    }
                }
            }
            else
            {
                newHoveredSurfaceNode = null;
            }

            if(newHoveredSurfaceNode != HoveredSurfaceNode)
            {
                OnHoveredNodeChanged(HoveredSurfaceNode, newHoveredSurfaceNode);
                HoveredSurfaceNode = newHoveredSurfaceNode;
            }
        }

        private void OnHoveredNodeChanged(SurfaceNode oldNode, SurfaceNode newNode)
        {
            switch (CurrentTool)
            {
                case EditorTool.Terrain:
                case EditorTool.SurfacePath:
                case EditorTool.Building:
                    if (oldNode != null) oldNode.GetComponent<MeshRenderer>().material.SetFloat("_ShowTileOverlay", 0);
                    if (newNode != null) newNode.GetComponent<MeshRenderer>().material.SetFloat("_ShowTileOverlay", 1);
                    break;

                case EditorTool.WorldExpand:
                    if (oldNode != null) oldNode.GetComponent<MeshRenderer>().material.SetFloat("_ShowBlockOverlay", 0);
                    if (newNode != null)
                    {
                        newNode.GetComponent<MeshRenderer>().material.SetFloat("_ShowBlockOverlay", 1);
                        newNode.GetComponent<MeshRenderer>().material.SetColor("_BlockOverlayColor", newNode.Chunk.CanAquire() ? Color.white : Color.red);
                    }
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
                    if(HoveredSurfaceNode != null)
                    {
                        HoveredSurfaceNode.GetComponent<MeshRenderer>().material.SetTexture("_TileOverlayTex", GetTextureForHoverMode(TileHoverMode));
                        bool canIncrease = HoveredSurfaceNode.CanChangeHeight(HoveredSurfaceNode, increase: true, TileHoverMode);
                        bool canDecrease = HoveredSurfaceNode.CanChangeHeight(HoveredSurfaceNode, increase: false, TileHoverMode);
                        Color c = Color.white;
                        if (canIncrease && canDecrease) c = Color.white;
                        else if (canIncrease) c = Color.green;
                        else if (canDecrease) c = Color.yellow;
                        else c = Color.red;
                        HoveredSurfaceNode.GetComponent<MeshRenderer>().material.SetColor("_TileOverlayColor", c);
                    }
                    break;

                case EditorTool.SurfacePath:
                    if (HoveredSurfaceNode != null)
                    {
                        HoveredSurfaceNode.GetComponent<MeshRenderer>().material.SetTexture("_TileOverlayTex", GetTextureForHoverMode(Direction.None));
                        HoveredSurfaceNode.GetComponent<MeshRenderer>().material.SetColor("_TileOverlayColor", World.CanBuildSurfacePath(HoveredSurfaceNode) ? Color.white : Color.red);
                    }
                    break;

                case EditorTool.AirPath:
                    if(HoveredSurfaceNode != null)
                    {
                        Vector3 hoverPos = HoveredSurfaceNode.GetCenterWorldPosition();
                        PathPreview.transform.position = new Vector3(hoverPos.x, World.TILE_HEIGHT * BuildHeight + World.TILE_HEIGHT * 0.5f, hoverPos.z);
                        PathPreview.transform.rotation = Quaternion.Euler(0f, BuildRotation, 0f);
                        PathPreview.GetComponentInChildren<MeshRenderer>().material.color = World.CanBuildAirPath(HoveredWorldCoordinates, BuildHeight) ? Color.white : Color.red;
                    }
                    break;

                case EditorTool.Stairs:
                    if (HoveredSurfaceNode != null)
                    {
                        Vector3 hoverPos = HoveredSurfaceNode.GetCenterWorldPosition();
                        PathPreview.transform.position = new Vector3(hoverPos.x, World.TILE_HEIGHT * BuildHeight + World.TILE_HEIGHT * 0.5f, hoverPos.z);
                        PathPreview.transform.rotation = Quaternion.Euler(0f, BuildRotation, 0f);
                        PathPreview.GetComponentInChildren<MeshRenderer>().material.color = World.CanBuildAirSlope(HoveredWorldCoordinates, BuildHeight, BuildRotationDirection) ? Color.white : Color.red;
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
                    if(!isUiClick && HoveredSurfaceNode != null && HoveredSurfaceNode.CanChangeHeight(HoveredSurfaceNode, increase: true, TileHoverMode)) HoveredSurfaceNode.ChangeHeight(TileHoverMode, isIncrease: true);
                    OnHoveredNodeChanged(HoveredSurfaceNode, HoveredSurfaceNode);
                    UpdateGridOverlay();
                    break;

                case EditorTool.WorldExpand:
                    if (!isUiClick && HoveredSurfaceNode != null && HoveredSurfaceNode.Chunk.CanAquire())
                    {
                        HoveredSurfaceNode.Chunk.Aquire();
                        OnHoveredNodeChanged(HoveredSurfaceNode, HoveredSurfaceNode);
                    }
                    break;
                    
                case EditorTool.SurfacePath:
                    if (!isUiClick && HoveredSurfaceNode != null && World.CanBuildSurfacePath(HoveredSurfaceNode)) World.BuildSurfacePath(HoveredSurfaceNode);
                    break;

                case EditorTool.AirPath:
                    if (!isUiClick && HoveredSurfaceNode != null && World.CanBuildAirPath(HoveredWorldCoordinates, BuildHeight)) World.BuildAirPath(HoveredWorldCoordinates, BuildHeight);
                    break;

                case EditorTool.Stairs:
                    if (!isUiClick && HoveredSurfaceNode != null && World.CanBuildAirSlope(HoveredWorldCoordinates, BuildHeight, BuildRotationDirection)) World.BuildAirSlope(HoveredWorldCoordinates, BuildHeight, BuildRotationDirection);
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
                    if (HoveredSurfaceNode != null && !isUiClick && HoveredSurfaceNode.CanChangeHeight(HoveredSurfaceNode, increase: false, TileHoverMode)) HoveredSurfaceNode.ChangeHeight(TileHoverMode, isIncrease: false);
                    break;
            }
        }

        private Direction GetTileHoverMode(Vector3 worldPos)
        {
            Vector2 posOnTile = new Vector2(worldPos.x - (int)worldPos.x, worldPos.z - (int)worldPos.z);
            if (worldPos.x < 0) posOnTile.x++;
            if (worldPos.z < 0) posOnTile.y++;

            bool north = posOnTile.y > (1f - HoverEdgeSensitivity);
            bool south = posOnTile.y < HoverEdgeSensitivity;
            bool west = posOnTile.x < HoverEdgeSensitivity;
            bool east = posOnTile.x > (1f - HoverEdgeSensitivity);

            if (north && east) return Direction.NE;
            if (north && west) return Direction.NW;
            if (north) return Direction.N;
            if (south && east) return Direction.SE;
            if (south && west) return Direction.SW;
            if (south) return Direction.S;
            if (east) return Direction.E;
            if (west) return Direction.W;
            return Direction.None;
        }

        private Texture2D GetTextureForHoverMode(Direction mode)
        {
            if (mode == Direction.None) return BlockmapResourceManager.Singleton.TileSelector;
            if (mode == Direction.N) return BlockmapResourceManager.Singleton.TileSelectorN;
            if (mode == Direction.E) return BlockmapResourceManager.Singleton.TileSelectorE;
            if (mode == Direction.S) return BlockmapResourceManager.Singleton.TileSelectorS;
            if (mode == Direction.W) return BlockmapResourceManager.Singleton.TileSelectorW;
            if (mode == Direction.NE) return BlockmapResourceManager.Singleton.TileSelectorNE;
            if (mode == Direction.NW) return BlockmapResourceManager.Singleton.TileSelectorNW;
            if (mode == Direction.SW) return BlockmapResourceManager.Singleton.TileSelectorSW;
            if (mode == Direction.SE) return BlockmapResourceManager.Singleton.TileSelectorSE;
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

        private void UpdateGridOverlay()
        {
            foreach (Chunk chunk in World.Chunks.Values)
                foreach (SurfaceNode node in chunk.GetSurfaceNodes())
                    node.GetComponent<MeshRenderer>().material.SetFloat("_ShowGrid", IsShowingGrid ? 1 : 0);
        }

        #endregion

        #region Tools

        private void SetHeight(int value)
        {
            BuildHeight = value;
            BuildHeight = Mathf.Clamp(BuildHeight, World.MIN_HEIGHT, World.MAX_HEIGHT);
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
                    if (HoveredSurfaceNode != null) HoveredSurfaceNode.GetComponent<MeshRenderer>().material.SetFloat("_ShowTileOverlay", 0);
                    break;

                case EditorTool.AirPath:
                case EditorTool.Stairs:
                    Destroy(PathPreview);
                    PathPreview = null;
                    break;

                case EditorTool.WorldExpand:
                    if (HoveredSurfaceNode != null) HoveredSurfaceNode.GetComponent<MeshRenderer>().material.SetFloat("_ShowBlockOverlay", 0);
                    break;

                case EditorTool.Building:
                    Destroy(BuildingPreview);
                    BuildingPreview = null;
                    break;
            }

            // Handle selection of new tool
            switch(newTool)
            {
                case EditorTool.Terrain:
                case EditorTool.SurfacePath:
                case EditorTool.WorldExpand:
                    OnHoveredNodeChanged(HoveredSurfaceNode, HoveredSurfaceNode);
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

                case EditorTool.Building:
                    BuildingPreview = Instantiate(BuildingPrefabs[BuildingIndex]);
                    break;
            }

            
        }

        #endregion

    }


}
