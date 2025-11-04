using BlockmapFramework.Profiling;
using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Object representing one world with its own node/pathfinding system. One world is a closed system.
    /// <br/> A world is made up of different chunks.
    /// </summary>
    public class World : IWorld, ISaveAndLoadable
    {
        /// <summary>
        /// The Unity GameObject of this world.
        /// </summary>
        public GameObject WorldObject;

        private int numChunksPerSide;
        /// <summary>
        /// The amount of chunks on each side of this world.
        /// </summary>
        public int NumChunksPerSide => numChunksPerSide;

        /// <summary>
        /// The amount of nodes on each side of this world.
        /// </summary>
        public int NumNodesPerSide => ChunkSize * NumChunksPerSide;

        /// <summary>
        /// Amount of ground nodes on the map. (A world with 2x2 chunks has WorldArea = 32 * 32 = 1024).
        /// </summary>
        public int WorldArea => NumNodesPerSide * NumNodesPerSide;

        /// <summary>
        /// The total amount of chunks in the world.
        /// </summary>
        public int TotalNumChunks => NumChunksPerSide * NumChunksPerSide;

        /// <summary>
        /// Maximum y coordiante a tile can have.
        /// </summary>
        public const int MAX_ALTITUDE = 60;
        public const int MAX_NODE_STEEPNESS = 4;

        /// <summary>
        /// Physical height (y) of a tile.
        /// </summary>
        public const float NodeHeight = 0.5f;

        public const int MAP_EDGE_ALTITUDE = -1;

        public const float MAP_EDGE_WORLD_ALTITUDE = (MAP_EDGE_ALTITUDE * NodeHeight);

        /// <summary>
        /// How much the colors/textures of adjacent surface tiles flow into each other (0 - 0.5).
        /// </summary>
        public const float SURFACE_TILE_BLENDING = 0.4f;

        /// <summary>
        /// How much height of a tile water covers.
        /// </summary>
        public const float WATER_HEIGHT = 0.9f;

        public string Name;

        public string CliffMaterialPath;
        public Material CliffMaterial => MaterialManager.LoadMaterial(CliffMaterialPath);

        public bool IsInitialized { get; private set; }
        public int CurrentTick { get; private set; }
        private System.Action OnInitializationDoneCallback;

        /// <summary>
        /// The amount of nodes on each side on a chunk.
        /// </summary>
        public const int ChunkSize = 16;
        public int MinX, MaxX, MinY, MaxY;

        // World updates
        private WorldSystemUpdateInfo CurrentWorldSystemUpdate;
        private Queue<WorldSystemUpdateInfo> PendingWorldUpdates = new Queue<WorldSystemUpdateInfo>();
        private int WorldUpdateFrame;

        private int UpdateEntityVisionIn;
        private List<Entity> VisionUpdateEntities;
        private System.Action EntityVisionUpdateCallback;

        private List<Entity> EntitiesToRemoveThisTick = new();

        // Database
        private Dictionary<int, BlockmapNode> Nodes = new Dictionary<int, BlockmapNode>();
        private Dictionary<Vector2Int, Chunk> Chunks = new Dictionary<Vector2Int, Chunk>(); // created dynamically - not part of save file
        private Dictionary<int, Actor> Actors = new Dictionary<int, Actor>();
        private Dictionary<int, Entity> Entities = new Dictionary<int, Entity>();
        private Dictionary<int, WaterBody> WaterBodies = new Dictionary<int, WaterBody>();
        private Dictionary<int, Fence> Fences = new Dictionary<int, Fence>();
        private Dictionary<int, Wall> Walls = new Dictionary<int, Wall>();
        private Dictionary<int, Zone> Zones = new Dictionary<int, Zone>();
        private Dictionary<int, Room> Rooms = new Dictionary<int, Room>();

        private int NodeIdCounter;
        private int EntityIdCounter;
        private int WaterBodyIdCounter;
        private int ActorIdCounter;
        private int ZoneIdCounter;
        private int FenceIdCounter;
        private int WallIdCounter;
        private int RoomIdCounter;

        // Database indexes
        private Dictionary<Vector2Int, List<Wall>> WallsByWorldCoordinates2D = new Dictionary<Vector2Int, List<Wall>>();

        // Camera
        public BlockmapCamera Camera { get; private set; }
        public WorldDisplaySettings DisplaySettings { get; private set; }
        public MovingEntity NavmeshEntity { get; private set; }

        // Actors
        /// <summary>
        /// Neutral passive actor
        /// </summary>
        public Actor Gaia { get; private set; }
        /// <summary>
        /// The actor that the vision is drawn for currently.
        /// <br/> If null everything is drawn.
        /// </summary>
        public Actor ActiveVisionActor { get; private set; }

        // Layers
        public int Layer_GroundNodeMesh;
        public int Layer_EntityMesh;
        public int Layer_EntityVisionCollider;
        public int Layer_AirNodeMesh;
        public int Layer_WaterMesh;
        public int Layer_FenceMesh;
        public int Layer_ProceduralEntityMesh;
        public int Layer_WallMesh;
        public int Layer_WallVisionCollider;

        public int Layer_AllMouseHoverLayers;

        // Attributes regarding current cursor position
        public bool IsHoveringWorld { get; private set; }
        public Vector3 HoveredWorldPosition { get; private set; }
        public Vector2Int HoveredWorldCoordinates { get; private set; }
        public BlockmapNode HoveredNode { get; private set; }
        public AirNode HoveredAirNode { get; private set; }
        public GroundNode HoveredGroundNode { get; private set; }
        public DynamicNode HoveredDynamicNode { get; private set; }
        public Entity HoveredEntity { get; private set; }
        public Chunk HoveredChunk { get; private set; }
        public WaterBody HoveredWaterBody { get; private set; }
        public Fence HoveredFence { get; private set; }
        public Wall HoveredWall { get; private set; }

        /// <summary>
        /// What area of the node is currently being hovered.
        /// <br/> The returned value is the direction of the edge/corner that is being hovered.
        /// <br/> Hovering the center part of a node will return Direction.None.
        /// </summary>
        public Direction NodeHoverMode9 { get; private set; }
        public Direction NodeHoverMode8 { get; private set; }
        public Direction NodeHoverModeSides { get; private set; }
        private float HoverEdgeSensitivity = 0.3f; // sensitivity for NodeHoverMode

        // Events
        public event System.Action<BlockmapNode, BlockmapNode> OnHoveredNodeChanged;
        public event System.Action<GroundNode, GroundNode> OnHoveredGroundNodeChanged;
        public event System.Action<Chunk, Chunk> OnHoveredChunkChanged;
        public event System.Action<Entity, Entity> OnHoveredEntityChanged;

        public event System.Action OnWorldContentChanged; // Fired whenever something gets added to or removed from the world. Changes to existing content doesn't fire event.

        // Cache
        private static readonly List<BlockmapNode> EmptyNodeList = new List<BlockmapNode>();

        // Performance Profilers
        static readonly ProfilerMarker pm_RedrawChunk = new ProfilerMarker("RedrawChunk");


        #region Init

        /// <summary>
        /// Empty constructor needed for loading world.
        /// </summary>
        public World() { }

        /// <summary>
        /// Creates a new flat and empty world with 3 actors (gaia, player 1, player 2) and a flat ground made out of grass (or nothing if isVoid = true).
        /// </summary>
        public World(int numChunksPerSide, bool isVoid)
        {
            this.numChunksPerSide = numChunksPerSide;

            OnCreate();

            // Create initial actors
            Gaia = AddActor("Gaia", Color.white);
            AddActor("Player 1", Color.blue);
            AddActor("Player 2", Color.red);

            Init();

            // Create initial nodes
            foreach (Chunk chunk in GetAllChunks())
            {
                for (int localX = 0; localX < ChunkSize; localX++)
                {
                    for (int localY = 0; localY < ChunkSize; localY++)
                    {
                        GroundNode node;
                        if(isVoid) node = new GroundNode(this, chunk, NodeIdCounter++, new Vector2Int(localX, localY), HelperFunctions.GetFlatHeights(0), SurfaceDefOf.Void);
                        else node = new GroundNode(this, chunk, NodeIdCounter++, new Vector2Int(localX, localY), HelperFunctions.GetFlatHeights(5), SurfaceDefOf.Grass);
                        RegisterNode(node);
                    }
                }
            }
        }

        /// <summary>
        /// Gets called right when loading or creating a world.
        /// </summary>
        private void OnCreate()
        {
            CliffMaterialPath = "Materials/NodeMaterials/Cliff";
        }

        /// <summary>
        /// Command to destroy this world.
        /// </summary>
        public void Destroy()
        {
            BlockmapCamera.Instance.OnFacingDirectionChanged -= OnCameraFacingDirectionChanged;
            GameObject.Destroy(WorldObject);
        }

        /// <summary>
        /// Gets executed once when or loading a world creating a new world (before nodes are created).
        /// </summary>
        private void Init()
        {
            // Create display settings
            DisplaySettings = new WorldDisplaySettings();

            // Set layers
            Layer_GroundNodeMesh = LayerMask.NameToLayer("Terrain");
            Layer_EntityMesh = LayerMask.NameToLayer("EntityMesh");
            Layer_EntityVisionCollider = LayerMask.NameToLayer("EntityVisionCollider");
            Layer_AirNodeMesh = LayerMask.NameToLayer("Path");
            Layer_WaterMesh = LayerMask.NameToLayer("Water");
            Layer_FenceMesh = LayerMask.NameToLayer("Fence");
            Layer_ProceduralEntityMesh = LayerMask.NameToLayer("ProceduralEntityMesh");
            Layer_WallMesh = LayerMask.NameToLayer("WallMesh");
            Layer_WallVisionCollider = LayerMask.NameToLayer("WallVisionCollider");

            Layer_AllMouseHoverLayers = (1 << Layer_GroundNodeMesh | 1 << Layer_AirNodeMesh | 1 << Layer_WaterMesh | 1 << Layer_FenceMesh | 1 << Layer_WallVisionCollider | 1 << Layer_EntityMesh | 1 << Layer_ProceduralEntityMesh);

            // Create chunks
            for (int chunkX = 0; chunkX < NumChunksPerSide; chunkX++)
            {
                for (int chunkY = 0; chunkY < NumChunksPerSide; chunkY++)
                {
                    Vector2Int chunkCoordinates = new Vector2Int(chunkX, chunkY);
                    Chunk chunk = new Chunk(this, chunkCoordinates);
                    RegisterChunk(chunk);
                }
            }

            // Create game objects for world and each chunk
            WorldObject = new GameObject(string.IsNullOrEmpty(Name) ? "World" : Name);
            foreach (Chunk chunk in GetAllChunks()) chunk.InitGameObjects();

            // Attach hooks
            BlockmapCamera.Instance.OnFacingDirectionChanged += OnCameraFacingDirectionChanged;
        }

        /// <summary>
        /// Starts world initialization which redraws the full world, updates the vision of all entities and generates the full navmesh.
        /// <br/>Gets executed once after creating or loading a world.
        /// </summary>
        public void Initialize(System.Action callback)
        {
            IsInitialized = false;
            OnInitializationDoneCallback = callback;

            Profiler.Begin("Initialize World");
            UpdateFullWorld(callback: OnInitializationDone);

            // Gaia
            Gaia = Actors[0];

            // Do general initialization that can be done instantly
            Pathfinder.Init(this);

            // Camera
            Camera = GameObject.Find("Main Camera").GetComponent<BlockmapCamera>();
            BlockmapNode initialCameraFocusNode = GetGroundNode(new Vector2Int(NumNodesPerSide / 4, NumNodesPerSide / 4));
            Camera.SetPosition(new Vector3(initialCameraFocusNode.WorldCoordinates.x, initialCameraFocusNode.BaseAltitude * NodeHeight, initialCameraFocusNode.WorldCoordinates.y));
            Camera.SetZoom(20f);
            Camera.SetAngle(225);

            // Calculate world bounds
            MinX = Chunks.Values.Min(x => x.Coordinates.x) * ChunkSize;
            MaxX = Chunks.Values.Max(x => x.Coordinates.x) * ChunkSize + (ChunkSize - 1);
            MinY = Chunks.Values.Min(x => x.Coordinates.y) * ChunkSize;
            MaxY = Chunks.Values.Max(x => x.Coordinates.y) * ChunkSize + (ChunkSize - 1);

            // Init database id's
            NodeIdCounter = Nodes.Count == 0 ? 0 : Nodes.Max(x => x.Key) + 1;
            EntityIdCounter = Entities.Count == 0 ? 0 : Entities.Max(x => x.Key) + 1;
            WaterBodyIdCounter = WaterBodies.Count == 0 ? 0 : WaterBodies.Max(x => x.Key) + 1;
            ActorIdCounter = Actors.Count == 0 ? 0 : Actors.Max(x => x.Key) + 1;
            ZoneIdCounter = Zones.Count == 0 ? 0 : Zones.Max(x => x.Key) + 1;
            FenceIdCounter = Fences.Count == 0 ? 0 : Fences.Max(x => x.Key) + 1;
            WallIdCounter = Walls.Count == 0 ? 0 : Walls.Max(x => x.Key) + 1;
            RoomIdCounter = Rooms.Count == 0 ? 0 : Rooms.Max(x => x.Key) + 1;
        }

        private void OnInitializationDone()
        {
            IsInitialized = true;
            Profiler.End("Initialize World");
            Profiler.LogAndClearResults();

            CurrentTick = 0;
            OnInitializationDoneCallback?.Invoke();
        }

        #endregion

        #region Update

        public void Render(float alpha)
        {
            if (!IsInitialized) return;

            // Hovered objects
            UpdateHoveredObjects();

            // Entities
            foreach (Entity e in Entities.Values) e.Render(alpha);
            foreach(Actor a in Actors.Values)
                foreach (Entity e in a.GhostMarkers) e.RenderGhostMarker();
        }

        public void Tick()
        {
            CurrentTick++;

            // Entities
            foreach (Entity e in Entities.Values) e.Tick();

            // Remove entities that are marked to be removed
            foreach (Entity e in EntitiesToRemoveThisTick) RemoveEntity(e, updateWorld: true);
            EntitiesToRemoveThisTick.Clear();
        }

        /// <summary>
        /// World system updates need to be performed in fixed update to ensure the physics enginge updates come through between the different update steps.
        /// </summary>
        public void FixedUpdate()
        {
            // Update world systems (DO NOT REMOVE THIS FROM FixedUpdate - I've tried it multiple times and it needs to be here)
            WorldSystemUpdate();

            if (UpdateEntityVisionIn > 0)
            {
                UpdateEntityVisionIn--;
                if (UpdateEntityVisionIn == 0)
                {
                    UpdateEntityVision(VisionUpdateEntities);
                    EntityVisionUpdateCallback?.Invoke();
                }
            }
        }

        /// <summary>
        /// Updates all hovered objects and fires events if anything changed.
        /// </summary>
        private void UpdateHoveredObjects()
        {
            Ray ray = Camera.Camera.ScreenPointToRay(Input.mousePosition);

            IsHoveringWorld = false;

            Chunk oldHoveredChunk = HoveredChunk;
            Chunk newHoveredChunk = null;

            BlockmapNode oldHoveredNode = HoveredNode;
            BlockmapNode newHoveredNode = null;

            GroundNode oldHoveredGroundNode = HoveredGroundNode;
            GroundNode newHoveredGroundNode = null;

            AirNode oldHoveredAirNode = HoveredAirNode;
            AirNode newHoveredAirNode = null;

            DynamicNode oldHoveredDynamicNode = HoveredDynamicNode;
            DynamicNode newHoveredDynamicNode = null;

            Entity oldHoveredEntity = HoveredEntity;
            Entity newHoveredEntity = null;

            WaterBody oldHoveredWaterBody = HoveredWaterBody;
            WaterBody newHoveredWaterBody = null;

            Fence oldHoveredFence = HoveredFence;
            Fence newHoveredFence = null;

            Wall oldHoveredWall = HoveredWall;
            Wall newHoveredWall = null;

            // Shoot a raycast on all layers that are relevant for detecting hovered objects
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, Layer_AllMouseHoverLayers);
            HelperFunctions.OrderRaycastHitsByDistance(hits);

            foreach (RaycastHit hit in hits)
            {
                Transform objectHit = hit.transform;

                Vector3 hitPosition = hit.point;
                IsHoveringWorld = true;

                HoveredWorldPosition = hitPosition;
                NodeHoverMode9 = GetNodeHoverMode9(hitPosition);
                NodeHoverMode8 = GetNodeHoverMode8(hitPosition);
                NodeHoverModeSides = GetNodeHoverModeSides(hitPosition);
                HoveredWorldCoordinates = GetWorldCoordinates(hitPosition);

                // Hit ground node
                if (objectHit.gameObject.layer == Layer_GroundNodeMesh)
                {
                    GroundNode hitNode = GetGroundNode(HoveredWorldCoordinates);

                    if (hitNode != null && hitNode.IsExploredBy(ActiveVisionActor) && hitNode.Mesh.Renderer.enabled)
                    {
                        newHoveredGroundNode = hitNode;
                        newHoveredNode = hitNode;
                        newHoveredDynamicNode = hitNode;
                        newHoveredChunk = hitNode.Chunk;
                        break;
                    }
                }

                // Hit air node
                else if (objectHit.gameObject.layer == Layer_AirNodeMesh)
                {
                    AirNode hitNode = GetAirNodeFromRaycastHit(hit);

                    if (hitNode != null && hitNode.GetVisibility(ActiveVisionActor) != VisibilityType.Unrevealed)
                    {
                        newHoveredAirNode = hitNode;
                        newHoveredNode = hitNode;
                        newHoveredDynamicNode = hitNode;
                        HoveredWorldCoordinates = hitNode.WorldCoordinates;
                        newHoveredChunk = hitNode.Chunk;
                        break;
                    }
                }

                // Hit water node
                else if (objectHit.gameObject.layer == Layer_WaterMesh)
                {
                    WaterNode hitNode = GetWaterNode(HoveredWorldCoordinates);

                    if (hitNode != null && hitNode.IsExploredBy(ActiveVisionActor))
                    {
                        if (hitNode.GroundNode.IsCenterUnderWater)
                        {
                            newHoveredNode = hitNode;
                            newHoveredWaterBody = hitNode.WaterBody;
                        }
                        else newHoveredNode = hitNode.GroundNode;

                        newHoveredGroundNode = hitNode.GroundNode;
                        newHoveredDynamicNode = hitNode.GroundNode;
                        newHoveredChunk = hitNode.Chunk;
                        break;
                    }
                }

                // Hit fence
                else if (objectHit.gameObject.layer == Layer_FenceMesh)
                {
                    Fence hitFence = GetFenceFromRaycastHit(hit);
                    
                    if (hitFence != null && hitFence.Node.GetVisibility(ActiveVisionActor) != VisibilityType.Unrevealed)
                    {
                        newHoveredFence = hitFence;
                        HoveredWorldCoordinates = hitFence.Node.WorldCoordinates;
                        newHoveredChunk = hitFence.Chunk;
                        break;
                    }
                }

                // Hit wall
                else if (objectHit.gameObject.layer == Layer_WallVisionCollider)
                {
                    Wall hitWall = (Wall)objectHit.GetComponent<WorldObjectCollider>().Object;
                    
                    if (hitWall != null && hitWall.GetVisibility(ActiveVisionActor) != VisibilityType.Unrevealed)
                    {
                        newHoveredWall = hitWall;
                        HoveredWorldCoordinates = hitWall.WorldCoordinates;
                        newHoveredChunk = hitWall.Chunk;
                        break;
                    }
                }

                // Hit standalone entity
                else if (hit.transform.gameObject.layer == Layer_EntityMesh)
                {
                    Entity hitEntity = (Entity)objectHit.GetComponent<WorldObjectCollider>().Object;

                    if (hitEntity != null && hitEntity.GetVisibility(ActiveVisionActor) != VisibilityType.Unrevealed)
                    {
                        newHoveredEntity = hitEntity;
                        newHoveredNode = hitEntity.OccupiedNodes.FirstOrDefault(n => n.WorldCoordinates == HoveredWorldCoordinates);
                        newHoveredChunk = newHoveredNode != null ? newHoveredNode.Chunk : hitEntity.Chunk;
                        break;
                    }
                }

                // Hit batch entity
                else if (hit.transform.gameObject.layer == Layer_ProceduralEntityMesh)
                {
                    Entity hitEntity = GetBatchEntityFromRaycastHit(hit);

                    if (hitEntity != null && hitEntity.GetVisibility(ActiveVisionActor) != VisibilityType.Unrevealed)
                    {
                        newHoveredEntity = hitEntity;
                        newHoveredNode = hitEntity.OccupiedNodes.FirstOrDefault(n => n.WorldCoordinates == HoveredWorldCoordinates);
                        newHoveredChunk = newHoveredNode != null ? newHoveredNode.Chunk : hitEntity.Chunk;
                        break;
                    }
                }
            }

            // Update currently hovered objects
            HoveredNode = newHoveredNode;
            HoveredGroundNode = newHoveredGroundNode;
            HoveredAirNode = newHoveredAirNode;
            HoveredDynamicNode = newHoveredDynamicNode;
            HoveredChunk = newHoveredChunk;
            HoveredEntity = newHoveredEntity;
            HoveredWaterBody = newHoveredWaterBody;
            HoveredFence = newHoveredFence;
            HoveredWall = newHoveredWall;

            // Fire update events
            if (newHoveredNode != oldHoveredNode) OnHoveredNodeChanged?.Invoke(oldHoveredNode, newHoveredNode);
            if (newHoveredGroundNode != oldHoveredGroundNode) OnHoveredGroundNodeChanged?.Invoke(oldHoveredGroundNode, newHoveredGroundNode);
            if (newHoveredChunk != oldHoveredChunk) OnHoveredChunkChanged?.Invoke(oldHoveredChunk, newHoveredChunk);
            if (newHoveredEntity != oldHoveredEntity) OnHoveredEntityChanged?.Invoke(oldHoveredEntity, newHoveredEntity);
        }
        private Direction GetNodeHoverMode9(Vector3 worldPos)
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
        private Direction GetNodeHoverMode8(Vector3 worldPos)
        {
            Direction fullHoverMode = GetNodeHoverMode9(worldPos);
            if (fullHoverMode == Direction.NW || fullHoverMode == Direction.NE || fullHoverMode == Direction.SW || fullHoverMode == Direction.SE)
                return fullHoverMode;

            return GetNodeHoverModeSides(worldPos);
        }

        /// <summary>
        /// Returns all possible directions a fence/wall can have when hovering a position.
        /// </summary>
        private List<Direction> GetNodeHoverModes8(Vector3 worldPos)
        {
            List<Direction> sides = new List<Direction>();

            Direction fullHoverMode = GetNodeHoverMode9(worldPos);
            if (fullHoverMode == Direction.NW || fullHoverMode == Direction.NE || fullHoverMode == Direction.SW || fullHoverMode == Direction.SE)
                sides.Add(fullHoverMode);

            Vector2 posOnTile = new Vector2(worldPos.x - (int)worldPos.x, worldPos.z - (int)worldPos.z);
            if (worldPos.x < 0) posOnTile.x++;
            if (worldPos.z < 0) posOnTile.y++;

            float epsilon = 0.2f;

            if (posOnTile.x < epsilon) sides.Add(Direction.W);
            if (posOnTile.x > 1f - epsilon) sides.Add(Direction.E);
            if (posOnTile.y < epsilon) sides.Add(Direction.S);
            if (posOnTile.y > 1f - epsilon) sides.Add(Direction.N);

            return sides;
        }
        private Direction GetNodeHoverModeSides(Vector3 worldPos)
        {
            Vector2 posOnTile = new Vector2(worldPos.x - (int)worldPos.x, worldPos.z - (int)worldPos.z);
            if (worldPos.x < 0) posOnTile.x++;
            if (worldPos.z < 0) posOnTile.y++;

            if (posOnTile.x > posOnTile.y)
            {
                if (1f - posOnTile.x > posOnTile.y) return Direction.S;
                else return Direction.E;
            }
            else
            {
                if (1f - posOnTile.x > posOnTile.y) return Direction.W;
                else return Direction.N;
            }
        }
        private Direction GetNodeHoverModeCorners(Vector3 worldPos)
        {
            Vector2 posOnTile = new Vector2(worldPos.x - (int)worldPos.x, worldPos.z - (int)worldPos.z);

            if (posOnTile.x >= 0.5f && posOnTile.y >= 0.5f) return Direction.NE;
            if (posOnTile.x < 0.5f && posOnTile.y >= 0.5f) return Direction.NW;
            if (posOnTile.x < 0.5f && posOnTile.y < 0.5f) return Direction.SW;
            if (posOnTile.x >= 0.5f && posOnTile.y < 0.5f) return Direction.SE;

            throw new System.Exception("posOnTile " + posOnTile.ToString() + " is invalid");
        }


        public AirNode GetAirNodeFromRaycastHit(RaycastHit hit)
        {
            Vector3 adjustedHitPosition = hit.point + new Vector3(0.001f, 0f, 0.001f); // This fixes the bug where hitting a mesh on a coordinate border sometimes returns 12.9999 instead of 13.
            Vector2Int hitCoordinates = GetWorldCoordinates(adjustedHitPosition);
            int altitude = hit.transform.GetComponent<AirNodeMesh>().Altitude;

            List<AirNode> hitAirNodes = GetAirNodes(hitCoordinates).Where(x => x.BaseAltitude == altitude).ToList();
            if (hitAirNodes.Count == 1) return hitAirNodes[0];
            if (hitAirNodes.Count > 1) // Get node that closest matches the hit height if multiple are on that base base height
            {
                Vector2 relativePos = new Vector2(adjustedHitPosition.x % 1f, adjustedHitPosition.z % 1f);
                AirNode closestNode = null;
                float closestDistance = float.MaxValue;
                foreach (AirNode node in hitAirNodes)
                {
                    float distance = Mathf.Abs((adjustedHitPosition.y % 1f) - node.GetWorldMeshAltitude(relativePos));
                    if (distance < closestDistance)
                    {
                        closestNode = node;
                        closestDistance = distance;
                    }
                }
                return closestNode;
            }

            // If we are exactly on a north or east edge we have to adjust the hit position slightly, else we are 1 coordinate off and don't find anything
            Vector3 offsetHitPosition = hit.point + new Vector3(-0.001f, 0f, -0.001f);
            Vector2Int offsetCoordinates = GetWorldCoordinates(offsetHitPosition);

            List<AirNode> hitOffsetAirNodes = GetAirNodes(offsetCoordinates).Where(x => x.BaseAltitude == altitude).ToList();
            if (hitOffsetAirNodes.Count == 1) return hitOffsetAirNodes[0];
            if (hitOffsetAirNodes.Count > 1) // Get node that closest matches the hit height if multiple are on that base base height
            {
                Vector2 relativePos = new Vector2(offsetHitPosition.x % 1f, offsetHitPosition.z % 1f);
                AirNode closestNode = null;
                float closestDistance = float.MaxValue;
                foreach (AirNode node in hitOffsetAirNodes)
                {
                    float distance = Mathf.Abs((offsetHitPosition.y % 1f) - node.GetWorldMeshAltitude(relativePos));
                    if (distance < closestDistance)
                    {
                        closestNode = node;
                        closestDistance = distance;
                    }
                }
                return closestNode;
            }

            // Didnt' find anything
            Debug.LogWarning("GetAirNodeFromRaycastHit failed to find an air node at world position: " + hit.point.ToString());
            return null;
        }
        public Fence GetFenceFromRaycastHit(RaycastHit hit)
        {
            FenceMesh hitMesh = hit.transform.GetComponent<FenceMesh>();
            int altitude = hitMesh.Altitude;

            Vector3 adjustedHitPosition = hit.point + new Vector3(0.001f, 0f, 0.001f); // This fixes the bug where hitting a mesh on a coordinate border sometimes returns 12.9999 instead of 13.
            Vector2Int hitCoordinates = GetWorldCoordinates(adjustedHitPosition);

            List<BlockmapNode> hitNodes = GetNodes(hitCoordinates, altitude).OrderByDescending(x => x.MaxAltitude).ToList();
            Direction primaryHitSide = GetNodeHoverMode8(adjustedHitPosition);
            List<Direction> otherPossibleHitSides = GetNodeHoverModes8(adjustedHitPosition);

            foreach (BlockmapNode hitNode in hitNodes)
            {
                if (hitNode != null && hitNode.Fences.ContainsKey(primaryHitSide)) return hitNode.Fences[primaryHitSide];
                else
                {
                    foreach (Direction hitSide in otherPossibleHitSides)
                    {
                        if (hitNode != null && hitNode.Fences.ContainsKey(hitSide))
                        {
                            return hitNode.Fences[hitSide];
                        }
                    }
                }
            }

            // If we are exactly on a north or east edge we have to adjust the hit position slightly, else we are 1 coordinate off and don't find anything
            // Do the same detection stuff again with the offset position
            Vector3 offsetHitPosition = hit.point + new Vector3(-0.01f, 0f, -0.01f);
            Vector2Int offsetCoordinates = GetWorldCoordinates(offsetHitPosition);
            List<BlockmapNode> offsetHitNodes = GetNodes(offsetCoordinates, altitude).OrderByDescending(x => x.MaxAltitude).ToList();
            Direction primaryOffsetSide = GetNodeHoverMode8(offsetHitPosition);
            List<Direction> otherPossibleOffsetSides = GetNodeHoverModes8(offsetHitPosition);

            foreach (BlockmapNode offsetHitNode in offsetHitNodes)
            {
                if (offsetHitNode != null && offsetHitNode.Fences.ContainsKey(primaryOffsetSide)) return offsetHitNode.Fences[primaryOffsetSide];
                else
                {
                    foreach (Direction hitSide in otherPossibleOffsetSides)
                    {
                        if (offsetHitNode != null && offsetHitNode.Fences.ContainsKey(hitSide))
                        {
                            return offsetHitNode.Fences[hitSide];
                        }
                    }
                }
            }

            Debug.LogWarning($"GetFenceFromRaycastHit failed to find a fence at world position: {hit.point}. Initial hit coordinates were {hitCoordinates} and offset coordinates were {offsetCoordinates}.");
            return null;
        }
        private Entity GetBatchEntityFromRaycastHit(RaycastHit hit)
        {
            BatchEntityMesh hitMesh = hit.transform.GetComponent<BatchEntityMesh>();
            List<BlockmapNode> hitNodes = GetNodes(HoveredWorldCoordinates, hitMesh.Altitude);

            // If the exact node we hit has a procedural entity, return that
            BlockmapNode targetNode = hitNodes.FirstOrDefault(x => x.Entities.Any(e => e.Def.RenderProperties.RenderType == EntityRenderType.Batch));
            if (targetNode != null) return targetNode.Entities.First(x => x.Def.RenderProperties.RenderType == EntityRenderType.Batch);

            return null;
        }

        /// <summary>
        /// Returns the exact world position of where the cursor is currently hovering on a specific altitude level.
        /// <br/>Calculation is based on intersecting mouse hover with an invisible plane on the given altitude.
        /// </summary>
        private Vector3 GetAltitudeHitPoint(int altitude)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, altitude * World.NodeHeight, 0f));
            var ray = Camera.Camera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float distance)) return ray.GetPoint(distance);

            // Below should only happen when cursor is out of game window.
            return Vector3.zero;
            throw new System.Exception("Altitude World Position Retrieval Error");
        }

        /// <summary>
        /// Returns the world coordinates of the currently hovered position on a specific altitude level.
        /// </summary>
        public Vector2Int GetHoveredCoordinates(int altitude)
        {
            Vector3 altitudeHitPoint = GetAltitudeHitPoint(altitude);
            return GetWorldCoordinates(altitudeHitPoint);
        }

        /// <summary>
        /// Returns the side hover mode of the currently hovered position on a specific altitude level.
        /// </summary>
        public Direction GetNodeHoverModeSides(int altitude)
        {
            Vector3 altitudeHitPoint = GetAltitudeHitPoint(altitude);
            return GetNodeHoverModeSides(altitudeHitPoint);
        }

        /// <summary>
        /// Returns the corner hover mode of the currently hovered position on a specific altitude level.
        /// </summary>
        public Direction GetNodeHoverModeCorners(int altitude)
        {
            Vector3 altitudeHitPoint = GetAltitudeHitPoint(altitude);
            return GetNodeHoverModeCorners(altitudeHitPoint);
        }

        /// <summary>
        /// Returns the amount of headspace for a cell in all directions by checking for nodes and walls (only on side/corner) ABOVE the cell.
        /// </summary>
        public Dictionary<Direction, int> GetFreeHeadspace(Vector3Int cellCoordinates)
        {
            Dictionary<Direction, int> headspace = new Dictionary<Direction, int>();
            foreach (Direction dir in HelperFunctions.GetAllDirections9()) headspace[dir] = World.MAX_ALTITUDE;

            Vector2Int worldCoordinates = new Vector2Int(cellCoordinates.x, cellCoordinates.z);
            int altitude = cellCoordinates.y;
            List<BlockmapNode> nodesAbove = GetNodes(worldCoordinates, altitude, World.MAX_ALTITUDE);
            List<Wall> wallsOnCoordinate = GetWalls(worldCoordinates);

            // 1. Check corners from nodes above (these block corners, side and center)
            foreach (BlockmapNode node in nodesAbove)
            {
                foreach (Direction corner in HelperFunctions.GetCorners())
                {
                    int diff = node.Altitude[corner] - altitude;
                    headspace[corner] = diff;
                    headspace[HelperFunctions.GetPreviousDirection8(corner)] = diff;
                    headspace[HelperFunctions.GetNextDirection8(corner)] = diff;
                }
            }

            // 1b. Center headspace is only affected by nodes above
            headspace[Direction.None] = Mathf.Min(headspace[Direction.NW], headspace[Direction.NE], headspace[Direction.SW], headspace[Direction.SE]);

            // 2. Check side walls above (these block sides and corners)
            foreach (Direction side in HelperFunctions.GetSides())
            {
                foreach (Wall wall in wallsOnCoordinate)
                {
                    if (wall.Side != side) continue; // Wall is not on this side
                    if (wall.MaxAltitude <= altitude) continue; // Wall is below node

                    int diff = wall.MaxAltitude - altitude;

                    if (diff < headspace[side]) headspace[side] = diff;

                    // Also blocks corners
                    Direction prevDir = HelperFunctions.GetPreviousDirection8(side);
                    if (diff < headspace[prevDir]) headspace[prevDir] = diff;

                    Direction nextDir = HelperFunctions.GetNextDirection8(side);
                    if (diff < headspace[nextDir]) headspace[nextDir] = diff;
                }
            }

            // 3. Check corner walls (these only block corners)
            foreach (Direction corner in HelperFunctions.GetCorners())
            {
                foreach (Wall wall in wallsOnCoordinate)
                {
                    if (wall.Side != corner) continue; // Wall is not on this corner
                    if (wall.MaxAltitude < altitude) continue; // Wall is below node

                    int diff = wall.MaxAltitude - altitude;

                    if (diff < headspace[corner]) headspace[corner] = diff;
                }
            }

            return headspace;
        }

        #endregion

        #region Systems Update (Navmesh / Draw / Vision)

        public void UpdateFullWorld(System.Action callback = null) => UpdateWorldSystems(null, callback);
        public void UpdateWorldSystems(Vector2Int worldCoordinates) => UpdateWorldSystems(new Parcel(worldCoordinates, Vector2Int.one));

        /// <summary>
        /// Updates all systems of the world for the given area over the course of the next few frames.
        /// <br/>If area is null, the full world will be updated.
        /// </summary>
        public void UpdateWorldSystems(Parcel area, System.Action callback = null)
        {
            PendingWorldUpdates.Enqueue(new WorldSystemUpdateInfo(area, callback));
        }

        /// <summary>
        /// Gets executed each fixed update while the world is being updated
        /// </summary>
        private void WorldSystemUpdate()
        {
            if (CurrentWorldSystemUpdate == null && PendingWorldUpdates.Count > 0) // Moving to next pending update
            {
                CurrentWorldSystemUpdate = PendingWorldUpdates.Dequeue();
                WorldUpdateFrame = 0;
            }

            if (CurrentWorldSystemUpdate == null) return; // nothing to update

            WorldUpdateFrame++;

            // Step 1: Draw meshes
            if (WorldUpdateFrame == 1)
            {
                Profiler.Begin("Draw Meshes");
                if (CurrentWorldSystemUpdate.Area == null) RedrawFullWorld();
                else RedrawNodes(CurrentWorldSystemUpdate.Area);
                Profiler.End("Draw Meshes");
            }

            // Step 2: Calculate node center world positions
            if (WorldUpdateFrame == 2)
            {
                Profiler.Begin("Recalculate Node Mesh Center");
                if (CurrentWorldSystemUpdate.Area == null)
                {
                    foreach (BlockmapNode n in GetAllNodes()) n.RecalculateMeshCenter();
                }
                else
                {
                    for (int x = CurrentWorldSystemUpdate.Area.Position.x - 1; x < CurrentWorldSystemUpdate.Area.Position.x + CurrentWorldSystemUpdate.Area.Dimensions.x + 1; x++)
                    {
                        for (int y = CurrentWorldSystemUpdate.Area.Position.y - 1; y < CurrentWorldSystemUpdate.Area.Position.y + CurrentWorldSystemUpdate.Area.Dimensions.y + 1; y++)
                        {
                            if (!IsInWorld(new Vector2Int(x, y))) continue;
                            foreach (BlockmapNode n in GetNodes(new Vector2Int(x, y))) n.RecalculateMeshCenter();
                        }
                    }
                }
                Profiler.End("Recalculate Node Mesh Center");

                // Step 3: Generate navmesh
                Profiler.Begin("Generate Navmesh");
                if (CurrentWorldSystemUpdate.Area == null) GenerateFullNavmesh();
                else UpdateNavmesh(CurrentWorldSystemUpdate.Area);
                Profiler.End("Generate Navmesh");

                // Step 4: Reposition standalone entities
                Profiler.Begin("Reposition Entities");
                HashSet<Entity> entitiesToUpdate = new HashSet<Entity>();

                if (CurrentWorldSystemUpdate.Area == null) entitiesToUpdate = Entities.Values.ToHashSet();
                else
                {
                    for (int x = CurrentWorldSystemUpdate.Area.Position.x - 1; x < CurrentWorldSystemUpdate.Area.Position.x + CurrentWorldSystemUpdate.Area.Dimensions.x + 1; x++)
                    {
                        for (int y = CurrentWorldSystemUpdate.Area.Position.y - 1; y < CurrentWorldSystemUpdate.Area.Position.y + CurrentWorldSystemUpdate.Area.Dimensions.y + 1; y++)
                        {
                            if (!IsInWorld(new Vector2Int(x, y))) continue;
                            foreach (BlockmapNode n in GetNodes(new Vector2Int(x, y)))
                            {
                                foreach (Entity e in n.Entities) entitiesToUpdate.Add(e);
                            }
                        }
                    }
                }

                foreach (Entity e in entitiesToUpdate)
                {
                    e.ResetWorldPositonAndRotation();
                    e.UpdateVisionColliderPosition();
                    e.UpdateVisibility();
                }
                Profiler.End("Reposition Entities");
            }

            if (WorldUpdateFrame == 3)
            {
                // Step 5: Update entity vision
                Profiler.Begin("Update Entity Vision");
                if (CurrentWorldSystemUpdate.Area == null) UpdateVisionOfAllEntities();
                else UpdateVisionOfNearbyEntities(CurrentWorldSystemUpdate.Area);
                Profiler.End("Update Entity Vision");

                // Step 6: Done and callback
                CurrentWorldSystemUpdate.Callback?.Invoke();
                CurrentWorldSystemUpdate = null;
            }
        }


        public void UpdateVisionOfNearbyEntitiesDelayed(Vector3 position, int rangeEast = 1, int rangeNorth = 1, Actor excludeActor = null, System.Action callback = null)
        {
            List<Entity> entities = GetNearbyEntities(position, rangeEast, rangeNorth);
            if (excludeActor != null) entities = entities.Where(x => x.Actor != excludeActor).ToList();
            UpdateEntityVisionDelayed(entities, callback);
        }

        public void UpdateEntityVisionDelayed(List<Entity> entities, System.Action callback)
        {
            UpdateEntityVisionIn = 2;
            VisionUpdateEntities = entities;
            EntityVisionUpdateCallback = callback;
        }

        #endregion

        #region Navmesh

        /// <summary>
        /// Updates the navmesh for the given area by recalculating all transitions originating from them.
        /// </summary>
        public void UpdateNavmesh(Parcel area)
        {
            // Get nodes to update
            List<BlockmapNode> navmeshUpdateNodes = new List<BlockmapNode>();
            if (area == null) navmeshUpdateNodes = GetAllNodes();
            else
            {
                for (int x = area.Position.x - 1; x < area.Position.x + area.Dimensions.x + 1; x++)
                {
                    for (int y = area.Position.y - 1; y < area.Position.y + area.Dimensions.y + 1; y++)
                    {
                        if (!IsInWorld(new Vector2Int(x, y))) continue;
                        navmeshUpdateNodes.AddRange(GetNodes(new Vector2Int(x, y)));
                    }
                }
            }

            Profiler.Begin("Recalculate Passability");
            foreach (BlockmapNode node in navmeshUpdateNodes) node.RecalculatePassability();
            Profiler.End("Recalculate Passability");

            foreach (BlockmapNode node in navmeshUpdateNodes) node.ResetTransitions();

            Profiler.Begin("Straight Transitions");
            foreach (BlockmapNode node in navmeshUpdateNodes) node.SetStraightAdjacentTransitions();
            Profiler.End("Straight Transitions");

            Profiler.Begin("Diagonal Transitions");
            foreach (BlockmapNode node in navmeshUpdateNodes) node.SetDiagonalAdjacentTransitions();
            Profiler.End("Diagonal Transitions");

            Profiler.Begin("Hop Transitions");
            foreach (BlockmapNode node in navmeshUpdateNodes) node.SetHopTransitions();
            Profiler.End("Hop Transitions");

            Profiler.Begin("Climb Transitions");
            foreach (BlockmapNode node in navmeshUpdateNodes) node.SetClimbTransitions();
            Profiler.End("Climb Transitions");

            UpdateNavmeshDisplay();
        }
        public void GenerateFullNavmesh() => UpdateNavmesh(area: null);

        #endregion

        #region Actions
        // All documentation for functions here are in IWorld.

        public void RegisterNode(BlockmapNode node, bool registerInWorld = true)
        {
            if (registerInWorld) Nodes.Add(node.Id, node); // Global registry

            // Chunk registry
            node.Chunk.RegisterNode(node);
        }
        public void DeregisterNode(BlockmapNode node)
        {
            // Destroy fences on node
            while (node.Fences.Count > 0) DeregisterFence(node.Fences.Values.ToList()[0]);

            // Destroy ladders from and to node
            while (node.SourceLadders.Count > 0) RemoveEntity(node.SourceLadders.Values.ToList()[0], updateWorld: false);
            while (node.TargetLadders.Count > 0) RemoveEntity(node.TargetLadders.Values.ToList()[0], updateWorld: false);

            // Node
            Nodes.Remove(node.Id); // Global registry

            // Chunk registry
            node.Chunk.Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(node);
            if (node is GroundNode groundNode) node.Chunk.GroundNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if (node is WaterNode waterNode) node.Chunk.WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if (node is AirNode airNode) node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(airNode);
        }
        public void RegisterChunk(Chunk chunk)
        {
            Chunks.Add(chunk.Coordinates, chunk);
        }

        public Actor AddActor(string name, Color color)
        {
            int id = ActorIdCounter++;
            Actor newActor = new Actor(this, id, name, color);
            Actors.Add(id, newActor);

            OnWorldContentChanged?.Invoke();

            return newActor;
        }
        public void ResetExploration(Actor actor)
        {
            foreach (BlockmapNode node in Nodes.Values) node.RemoveExploredBy(actor);
            foreach (Entity entity in Entities.Values) entity.RemoveLastSeenInformationFor(actor);
            foreach (Wall wall in Walls.Values) wall.RemoveExploredBy(actor);

            foreach (Entity entity in Entities.Values.Where(x => x.Actor == actor)) entity.UpdateVision();

            UpdateVisibility();
        }
        public void ExploreEverything(Actor actor)
        {
            foreach (BlockmapNode node in Nodes.Values) node.AddExploredBy(actor);
            foreach (Entity entity in Entities.Values) entity.UpdateLastSeenInformationFor(actor);
            foreach (Wall wall in Walls.Values) wall.AddExploredBy(actor);

            foreach (Entity entity in Entities.Values.Where(x => x.Actor == actor)) entity.UpdateVision();

            UpdateVisibility();
        }

        public bool CanChangeShape(DynamicNode node, Direction mode, bool isIncrease)
        {
            return node.CanChangeShape(mode, isIncrease);
        }
        public void ChangeShape(DynamicNode node, Direction mode, bool isIncrease, bool updateWorld)
        {
            node.ChangeShape(mode, isIncrease);

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }

        public void SetSurface(DynamicNode node, SurfaceDef surface, bool updateWorld)
        {
            if (node == null) return;
            node.SetSurface(surface);

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }
        public void SetGroundNodeAsVoid(GroundNode node, bool updateWorld)
        {
            node.SetAsVoid();

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }
        public void UnsetGroundNodeAsVoid(GroundNode node, int altitude, bool updateWorld)
        {
            node.UnsetAsVoid(null, altitude);

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }

        public bool CanBuildAirNode(Vector2Int worldCoordinates, int altitude)
        {
            if (!IsInWorld(worldCoordinates)) return false;

            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            GroundNode groundNode = chunk.GetGroundNode(localCoordinates);
            Dictionary<Direction, int> newNodeAltitude = HelperFunctions.GetFlatHeights(altitude);

            // Check if an entity or fence below is blocking this space
            List<BlockmapNode> belowNodes = GetNodes(worldCoordinates, 0, altitude);
            foreach (BlockmapNode node in belowNodes)
            {
                foreach (Entity e in node.Entities)
                    if (e.GetMaxAltitudeAt(node) > altitude)
                        return false;

                foreach (Fence fence in node.Fences.Values)
                    if (fence.MaxAltitude >= altitude)
                        return false;
            }

            // Check if underwater
            WaterNode water = GetWaterNode(worldCoordinates);
            if (water != null && water.WaterBody.ShoreHeight > altitude) return false;

            // Check if no more than 2 corners would overlap with another node
            List<BlockmapNode> nodesOnCoordinate = GetNodes(worldCoordinates);
            foreach (BlockmapNode otherNode in nodesOnCoordinate)
            {
                if (GetNumOverlappingCorners(newNodeAltitude, otherNode.Altitude) > 2) return false;
            }

            // Check if overlap with ground
            if (!IsAbove(newNodeAltitude, groundNode.Altitude)) return false;

            return true;
        }
        public AirNode BuildAirNode(Vector2Int worldCoordinates, int altitude, SurfaceDef surfaceDef, bool updateWorld)
        {
            if (!CanBuildAirNode(worldCoordinates, altitude)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);

            AirNode newNode = new AirNode(this, chunk, NodeIdCounter++, localCoordinates, HelperFunctions.GetFlatHeights(altitude), surfaceDef);
            RegisterNode(newNode);

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(newNode.WorldCoordinates);

            OnWorldContentChanged?.Invoke();

            return newNode;
        }
        public bool CanRemoveAirNode(AirNode node)
        {
            if (node.Entities.Count > 0) return false;

            return true;
        }
        public void RemoveAirNode(AirNode node, bool updateWorld)
        {
            DeregisterNode(node);

            OnWorldContentChanged?.Invoke();

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }

        public Entity SpawnEntity(EntitySpawnProperties spawnProps) => SpawnEntity(spawnProps.ResolvedDef, spawnProps.ResolvedTargetNode, spawnProps.ResolvedRotation, spawnProps.ResolvedMirrored, spawnProps.ResolvedActor, updateWorld: false, height: spawnProps.CustomHeight, variantIndex: spawnProps.ResolvedVariantIndex);
        public Entity SpawnEntity(EntityDef def, BlockmapNode node, Direction rotation, bool isMirrored, Actor actor, bool updateWorld, int height = -1, System.Action<Entity> preInit = null, int variantIndex = 0)
        {
            if (actor == null) throw new System.Exception("Cannot spawn an entity without an actor");
            if (node == null) throw new System.Exception("The node on which to spawn an entity must not be null.");

            // Create entity
            Entity newEntity = (Entity)System.Activator.CreateInstance(def.EntityClass);

            // Call OnCreate to initialize all basic info that are passed to SpawnEntity
            newEntity.OnCreate(def, EntityIdCounter++, this, node, height, rotation, actor, isMirrored, variantIndex);

            // Call entity-specific PreInit to maybe set more entity-specific values
            preInit?.Invoke(newEntity);

            // Initialize entity
            newEntity.Init();

            // Register new entity
            RegisterEntity(newEntity, updateWorld);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Return new instance
            return newEntity;
        }
        public void MarkEntityToBeRemovedThisTick(Entity e) => EntitiesToRemoveThisTick.Add(e);
        public void RemoveEntity(Entity entityToRemove, bool updateWorld)
        {
            // De-register entity
            DeregisterEntity(entityToRemove);

            // Create ghost markers for actors that have previously explored the entity, but don't currently see it at their last known position.
            // The ghost markers will disappear once actors see an affected node again.
            if (entityToRemove.ExploredBehaviour != ExploredBehaviour.None)
            {
                foreach (Actor actor in GetAllActors())
                {
                    if (actor != entityToRemove.Actor)
                    {
                        if (!entityToRemove.IsVisibleBy(actor) && entityToRemove.IsExploredBy(actor))
                        {
                            CreateGhostMarker(entityToRemove, actor, entityToRemove.GetLastSeenInfo(actor));
                        }
                    }
                }
            }

            // Remove entity vision reference on all nodes, entities and walls
            HashSet<Chunk> chunksAffectedByVision = new HashSet<Chunk>();
            foreach (BlockmapNode node in entityToRemove.CurrentVision.VisibleNodes)
            {
                node.RemoveVisionBy(entityToRemove);
                chunksAffectedByVision.Add(node.Chunk);
            }
            foreach (Entity e in entityToRemove.CurrentVision.VisibleEntities)
            {
                e.RemoveVisionBy(entityToRemove);
                foreach (BlockmapNode eNode in e.OccupiedNodes) chunksAffectedByVision.Add(eNode.Chunk);
            }
            foreach (Wall w in entityToRemove.CurrentVision.VisibleWalls)
            {
                w.RemoveVisionBy(entityToRemove);
                chunksAffectedByVision.Add(w.Chunk);
            }

            // Remove node & chunk occupation
            if (entityToRemove.PlacementType == EntityPlacementType.AttachedToNode)
            {
                foreach (BlockmapNode node in entityToRemove.OccupiedNodes)
                {
                    node.RemoveEntity(entityToRemove);
                    node.Chunk.RemoveEntity(entityToRemove);
                }
            }

            // Remove from inventory
            if (entityToRemove.IsInInventory)
            {
                entityToRemove.Holder.RemoveFromInventory(entityToRemove);
            }

            // Update world around coordinates
            if (!entityToRemove.IsInInventory)
            {
                if (updateWorld) UpdateWorldSystems(new Parcel(entityToRemove.OriginNode.WorldCoordinates, new Vector2Int(entityToRemove.GetTranslatedDimensions().x, entityToRemove.GetTranslatedDimensions().z)));
            }

            // Update visibility of all chunks affected by the entity vision if the entity belongs to the active vision actor
            if (updateWorld && entityToRemove.Actor == ActiveVisionActor)
                foreach (Chunk c in chunksAffectedByVision)
                    UpdateVisibility(c);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Destroy
            entityToRemove.DestroyGameObject();
        }
        public void RemoveEntities(BlockmapNode node, bool updateWorld)
        {
            while (node.Entities.Count > 0) RemoveEntity(node.Entities.First(), updateWorld);
        }
        public void RegisterEntity(Entity entity, bool updateWorld, bool registerInWorld = true)
        {
            // Register in various registries
            if (registerInWorld) Entities.Add(entity.Id, entity);
            entity.Actor.Entities.Add(entity);
            if (entity.Def.RenderProperties.RenderType == EntityRenderType.Batch) entity.Chunk.RegisterBatchEntity(entity);

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(new Parcel(entity.OriginNode.WorldCoordinates, new Vector2Int(entity.GetTranslatedDimensions().x, entity.GetTranslatedDimensions().z)));

            entity.OnRegister();
        }
        public void DeregisterEntity(Entity entity)
        {
            Entities.Remove(entity.Id);
            entity.Actor.Entities.Remove(entity);
            if (entity.Def.RenderProperties.RenderType == EntityRenderType.Batch) entity.Chunk.DeregisterBatchEntity(entity);

            entity.OnDeregister();
        }

        public void CreateGhostMarker(Entity removedEntity, Actor actor, LastSeenInfo lastSeenInfo)
        {
            // Create entity as ghost marker
            Entity newGhostMarker = (Entity)System.Activator.CreateInstance(removedEntity.Def.EntityClass);
            newGhostMarker.InitAsGhostMarker(this, removedEntity.Def, removedEntity.Actor, lastSeenInfo);
            actor.GhostMarkers.Add(newGhostMarker);
            newGhostMarker.SetGhostMarkerVisibility(ActiveVisionActor == actor);
        }
        public void RemoveGhostMarkers(Actor actor, HashSet<BlockmapNode> nodes) // Removes all ghost markers for the given actor that touch one of the given nodes
        {
            foreach (BlockmapNode n in nodes)
            {
                List<Entity> removedGhostMarkers = new List<Entity>();
                foreach (Entity ghostMarker in actor.GhostMarkers)
                {
                    if (ghostMarker.OccupiedNodes.Contains(n))
                    {
                        ghostMarker.DestroyGameObject();
                        removedGhostMarkers.Add(ghostMarker);
                    }
                }
                foreach (Entity removedGhostMarker in removedGhostMarkers) actor.GhostMarkers.Remove(removedGhostMarker);
            }
        }

        public WaterBody CanAddWater(GroundNode node, int maxDepth) // returns null when cannot
        {
            int shoreHeight = node.BaseAltitude + 1;
            WaterBody waterBody = new WaterBody(); // dummy water body to store data that can later be used to create a real water body
            waterBody.ShoreHeight = shoreHeight;
            waterBody.CoveredGroundNodes = new List<GroundNode>();

            List<System.Tuple<GroundNode, Direction>> checkedNodes = new List<System.Tuple<GroundNode, Direction>>(); // nodes that were already checked for water expansion in one direction
            List<System.Tuple<GroundNode, Direction>> expansionNodes = new List<System.Tuple<GroundNode, Direction>>(); // nodes that need to be checked for water expansion and in what direction
            expansionNodes.Add(new System.Tuple<GroundNode, Direction>(node, Direction.None));

            // Check the following:
            // > If the node goes deeper than maxDepth, return false
            // > If the node is below shoreDepth (meaning also underwater), mark it to check
            // > If the node is above shoreDepth, do nothing
            while (expansionNodes.Count > 0)
            {
                System.Tuple<GroundNode, Direction> check = expansionNodes[0];
                GroundNode checkNode = check.Item1;
                Direction checkDir = check.Item2;
                expansionNodes.RemoveAt(0);

                if (checkNode == null) continue;
                if (waterBody.CoveredGroundNodes.Contains(checkNode)) continue;
                if (checkedNodes.Contains(check)) continue;

                checkedNodes.Add(check);

                bool isTooDeep = checkNode.BaseAltitude < shoreHeight - maxDepth;
                if (isTooDeep) return null; // too deep
                if (checkNode.WaterNode != null) return null; // already has water here

                bool isUnderwater = (
                    ((checkDir == Direction.None || checkDir == Direction.N) && (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.NE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.E) && (checkNode.Altitude[Direction.NE] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.S) && (checkNode.Altitude[Direction.SW] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.W) && (checkNode.Altitude[Direction.SW] < shoreHeight || checkNode.Altitude[Direction.NW] < shoreHeight))
                    );
                if (isUnderwater) // underwater
                {
                    // Check if we're drowing entities
                    if (GetEntities(checkNode.WorldCoordinates, shoreHeight - maxDepth, shoreHeight - 1).Count > 0) return null;

                    // Check if we're drowing air nodes
                    if (GetAirNodes(checkNode.WorldCoordinates, shoreHeight - maxDepth, shoreHeight - 1).Count > 0) return null;

                    waterBody.CoveredGroundNodes.Add(checkNode);

                    if (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.NE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.N), Direction.S));
                    if (checkNode.Altitude[Direction.NE] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.E), Direction.W));
                    if (checkNode.Altitude[Direction.SW] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.S), Direction.N));
                    if (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.SW] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.W), Direction.E));
                }
                else { } // above water
            }

            return waterBody;
        }
        public void AddWaterBody(WaterBody data, bool updateWorld)
        {
            // Create a new water nodes for each covered surface node
            List<WaterNode> waterNodes = new List<WaterNode>();
            foreach (GroundNode node in data.CoveredGroundNodes)
            {
                WaterNode waterNode = new WaterNode(this, node.Chunk, NodeIdCounter++, node.LocalCoordinates, data.ShoreHeight);
                waterNodes.Add(waterNode);
                RegisterNode(waterNode);
            }

            // Make a new water body instance with a unique id
            WaterBody newWaterBody = new WaterBody(WaterBodyIdCounter++, data.ShoreHeight, waterNodes, data.CoveredGroundNodes);

            // Register water body
            WaterBodies.Add(newWaterBody.Id, newWaterBody);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Update world around water body
            if (updateWorld) UpdateWorldSystems(new Parcel(new Vector2Int(newWaterBody.MinX, newWaterBody.MinY), new Vector2Int(newWaterBody.MaxX - newWaterBody.MinX + 1, newWaterBody.MaxY - newWaterBody.MinY + 1)));
        }
        public void AddWaterBody(GroundNode sourceNode, int shoreHeight, bool updateWorld)
        {
            if (shoreHeight >= World.MAX_ALTITUDE) return;

            WaterBody waterBody = new WaterBody(); // dummy water body to store data that can later be used to create a real water body
            waterBody.ShoreHeight = shoreHeight;
            waterBody.CoveredGroundNodes = new List<GroundNode>();

            List<System.Tuple<GroundNode, Direction>> checkedNodes = new List<System.Tuple<GroundNode, Direction>>(); // nodes that were already checked for water expansion in one direction
            List<System.Tuple<GroundNode, Direction>> expansionNodes = new List<System.Tuple<GroundNode, Direction>>(); // nodes that need to be checked for water expansion and in what direction
            expansionNodes.Add(new System.Tuple<GroundNode, Direction>(sourceNode, Direction.None));

            // Check the following:
            // > If the node goes deeper than maxDepth, return false
            // > If the node is below shoreDepth (meaning also underwater), mark it to check
            // > If the node is above shoreDepth, do nothing
            while (expansionNodes.Count > 0)
            {
                System.Tuple<GroundNode, Direction> check = expansionNodes[0];
                GroundNode checkNode = check.Item1;
                Direction checkDir = check.Item2;
                expansionNodes.RemoveAt(0);

                if (checkNode == null) continue;
                if (waterBody.CoveredGroundNodes.Contains(checkNode)) continue;
                if (checkedNodes.Contains(check)) continue;

                checkedNodes.Add(check);

                if (checkNode.WaterNode != null) return; // already has water here

                bool isUnderwater = (
                    ((checkDir == Direction.None || checkDir == Direction.N) && (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.NE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.E) && (checkNode.Altitude[Direction.NE] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.S) && (checkNode.Altitude[Direction.SW] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.W) && (checkNode.Altitude[Direction.SW] < shoreHeight || checkNode.Altitude[Direction.NW] < shoreHeight))
                    );
                if (isUnderwater) // underwater
                {
                    // Remove drowned entities
                    List<Entity> drownedEntities = GetEntities(checkNode.WorldCoordinates, 0, shoreHeight - 1);
                    foreach (Entity drownedEntity in drownedEntities) RemoveEntity(drownedEntity, updateWorld: false);

                    // Remove drowned air nodes
                    List<AirNode> drownedAirNodes = GetAirNodes(checkNode.WorldCoordinates, 0, shoreHeight - 1);
                    foreach (AirNode drownedAirNode in drownedAirNodes) RemoveAirNode(drownedAirNode, updateWorld: false);

                    waterBody.CoveredGroundNodes.Add(checkNode);

                    if (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.NE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.N), Direction.S));
                    if (checkNode.Altitude[Direction.NE] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.E), Direction.W));
                    if (checkNode.Altitude[Direction.SW] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.S), Direction.N));
                    if (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.SW] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.W), Direction.E));
                }
                else { } // above water
            }

            if (waterBody.CoveredGroundNodes.Count > 0) AddWaterBody(waterBody, updateWorld);

        }
        public void RemoveWaterBody(WaterBody water, bool updateWorld)
        {
            // De-register
            WaterBodies.Remove(water.Id);

            // Remove water node reference from all covered surface nodes
            foreach (GroundNode node in water.CoveredGroundNodes) node.SetWaterNode(null);

            // Deregister deleted water nodes
            foreach (WaterNode node in water.WaterNodes) DeregisterNode(node);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Update world around water body
            if (updateWorld) UpdateWorldSystems(new Parcel(new Vector2Int(water.MinX, water.MinY), new Vector2Int(water.MaxX - water.MinX + 1, water.MaxY - water.MinY + 1)));
        }

        public bool CanBuildFence(FenceDef def, BlockmapNode node, Direction side, int height)
        {
            List<Direction> affectedSides = HelperFunctions.GetAffectedDirections(side);

            // Check if disallowed corner
            if (HelperFunctions.IsCorner(side) && !def.CanBuildOnCorners) return false;

            // Adjust height if it's higher than fence type allows
            if (height > def.MaxHeight) height = def.MaxHeight;

            return CanBuildOnNodeSide(node, side, height, allowSlopes: true);
        }
        public void BuildFence(FenceDef def, BlockmapNode node, Direction side, int height, bool updateWorld)
        {
            // Adjust height if it's higher than fence type allows
            if (height > def.MaxHeight) height = def.MaxHeight;

            // Create and register new fence
            Fence fence = new Fence(this, def, FenceIdCounter++, node, side, height);
            RegisterFence(fence);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }
        public void RegisterFence(Fence fence, bool registerInWorld = true)
        {
            // In world
            if (registerInWorld) Fences.Add(fence.Id, fence);

            // In chunk
            fence.Node.Chunk.RegisterFence(fence);

            // On node
            fence.Node.Fences.Add(fence.Side, fence);
        }
        public void RemoveFence(Fence fence, bool updateWorld)
        {
            // Deregister
            BlockmapNode node = fence.Node;
            DeregisterFence(fence);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Update world around coordinates
            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }
        public void DeregisterFence(Fence fence)
        {
            BlockmapNode node = fence.Node;
            Fences.Remove(fence.Id);
            node.Fences.Remove(fence.Side);
            node.Chunk.DeregisterFence(fence);
        }

        public bool CanBuildWall(Vector3Int globalCellCoordinates, Direction side)
        {
            if (!IsInWorld(new Vector2Int(globalCellCoordinates.x, globalCellCoordinates.z))) return false;

            int altitude = globalCellCoordinates.y;
            Vector2Int worldCoordinates = new Vector2Int(globalCellCoordinates.x, globalCellCoordinates.z);
            List<BlockmapNode> nodesOnCoordinate = GetNodes(worldCoordinates);
            List<Direction> affectedSides = HelperFunctions.GetAffectedDirections(side);


            foreach (BlockmapNode nodeOnCoordinate in nodesOnCoordinate)
            {
                // Check if we would overlap a fence
                foreach (Fence fence in nodeOnCoordinate.Fences.Values)
                {
                    foreach (Direction affectedSide in affectedSides)
                    {
                        if (fence.Side == affectedSide)
                        {
                            if (fence.MinAltitude <= altitude && fence.MaxAltitude >= altitude) return false;
                        }
                    }
                }

                // Check if we would overlap a ladder
                foreach (Ladder ladder in nodeOnCoordinate.SourceLadders.Values)
                {
                    if (HelperFunctions.DoAffectedCornersOverlap(side, ladder.Side))
                    {
                        if (ladder.LadderStartAltitude <= altitude && ladder.LadderEndAltitude >= altitude) return false;
                    }
                }

                // Check if we would overlap a door
                foreach (Door door in nodeOnCoordinate.Doors.Values)
                {
                    if (HelperFunctions.DoAffectedCornersOverlap(side, door.Rotation))
                    {
                        if (door.DoorMinAltitude <= altitude && door.DoorMaxAltitude >= altitude) return false;
                    }
                }
            }

            // Check if we would overlap another wall
            foreach (Wall w in GetWalls(globalCellCoordinates))
            {
                foreach (Direction affectedSide in affectedSides)
                    if (w.Side == affectedSide)
                        return false;
            }

            return true;
        }
        public List<Wall> BuildWalls(Vector3Int globalSourceCellCoordinates, Direction side, WallShapeDef shape, WallMaterialDef material, bool mirrored, int height, bool updateWorld)
        {
            List<Wall> builtWalls = new List<Wall>();
            for (int y = 0; y < height; y++)
            {
                Vector3Int coords = new Vector3Int(globalSourceCellCoordinates.x, globalSourceCellCoordinates.y + y, globalSourceCellCoordinates.z);
                if (!CanBuildWall(coords, side)) continue;
                Wall builtWall = BuildWall(coords, side, shape, material, updateWorld: false, mirrored);
                builtWalls.Add(builtWall);
            }

            if (updateWorld) UpdateWorldSystems(new Vector2Int(globalSourceCellCoordinates.x, globalSourceCellCoordinates.z));

            return builtWalls;
        }
        public Wall BuildWall(Vector3Int globalCellCoordinates, Direction side, WallShapeDef shape, WallMaterialDef material, bool updateWorld, bool mirrored = false)
        {
            // Create and register new wall
            Wall newWall = new Wall(this, WallIdCounter++, globalCellCoordinates, side, shape, material, mirrored);
            RegisterWall(newWall);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Update world around cell
            if (updateWorld) UpdateWorldSystems(newWall.WorldCoordinates);

            return newWall;
        }
        public void RegisterWall(Wall wall, bool registerInWorld = true) // add to database and add all references in different objects
        {
            // In world
            if (registerInWorld) Walls.Add(wall.Id, wall);

            // Database index by world coordinate 2D
            if (WallsByWorldCoordinates2D.ContainsKey(wall.WorldCoordinates)) WallsByWorldCoordinates2D[wall.WorldCoordinates].Add(wall);
            else WallsByWorldCoordinates2D.Add(wall.WorldCoordinates, new List<Wall>() { wall });

            // In chunk
            wall.Chunk.RegisterWall(wall);

            // Opposite wall references
            Wall oppositeWall = GetWall(HelperFunctions.GetAdjacentCellCoordinates(wall.GlobalCellCoordinates, wall.Side), HelperFunctions.GetOppositeDirection(wall.Side));
            if(oppositeWall != null)
            {
                wall.SetOppositeWall(oppositeWall);
                oppositeWall.SetOppositeWall(wall);
            }
        }
        public void RemoveWall(Wall wall, bool updateWorld)
        {
            // Destroy vision collider
            GameObject.Destroy(wall.VisionColliderObject);

            // Deregister
            DeregisterWall(wall);

            // Fire content change event
            OnWorldContentChanged?.Invoke();

            // Update world around cell
            if (updateWorld) UpdateWorldSystems(wall.WorldCoordinates);
        }
        public void DeregisterWall(Wall wall)
        {
            Walls.Remove(wall.Id);
            wall.Chunk.DeregisterWall(wall);
            WallsByWorldCoordinates2D[wall.WorldCoordinates].Remove(wall);
            if(wall.OppositeWall != null)
            {
                wall.OppositeWall.SetOppositeWall(null);
                wall.SetOppositeWall(null);
            }
        }

        public List<BlockmapNode> GetPossibleLadderTargetNodes(BlockmapNode source, Direction side)
        {
            List<BlockmapNode> possibleTargetNodes = new List<BlockmapNode>();

            // Check if source node is viable for a ladder
            if (!source.IsFlat(side)) return possibleTargetNodes;
            if (source.Fences.ContainsKey(side)) return possibleTargetNodes;
            if (source.SourceLadders.ContainsKey(side)) return possibleTargetNodes;
            if (source.Doors.ContainsKey(side)) return possibleTargetNodes;

            // Get target node (need to be adjacent, higher up and flat on the target direction)
            int sourceAltitude = source.GetMinAltitude(side);
            Direction targetSide = HelperFunctions.GetOppositeDirection(side);
            Vector2Int targetCoordinates = HelperFunctions.GetCoordinatesInDirection(source.WorldCoordinates, side);

            List<BlockmapNode> adjNodes = GetNodes(targetCoordinates).OrderBy(x => x.GetMaxAltitude(targetSide)).ToList();
            foreach (BlockmapNode adjNode in adjNodes)
            {
                if (adjNode.GetMaxAltitude(targetSide) <= sourceAltitude) continue; // not higher than starting point
                if (!adjNode.IsFlat(HelperFunctions.GetOppositeDirection(side))) continue; // not flat in target direction
                //if (!adjNode.IsPassable(HelperFunctions.GetOppositeDirection(side))) continue; // blocked in the direction we would come from when reaching top of ladder

                // Check headspace
                int targetAltitude = adjNode.GetMaxAltitude(HelperFunctions.GetOppositeDirection(side));
                int ladderHeight = targetAltitude - sourceAltitude;
                int headspace = source.FreeHeadSpace[side];
                if (headspace <= ladderHeight) continue; // a node is blocking the climb

                // Check if a wall would block the ladder
                bool isBlockedByWall = false;
                for (int i = sourceAltitude; i <= targetAltitude; i++)
                {
                    Vector3Int globalCellCoordinates = new Vector3Int(source.WorldCoordinates.x, i, source.WorldCoordinates.y);
                    List<Wall> walls = GetWalls(globalCellCoordinates);
                    foreach (Wall w in walls)
                    {
                        if (HelperFunctions.DoAffectedCornersOverlap(side, w.Side)) isBlockedByWall = true;
                    }
                }
                if (isBlockedByWall) continue;

                // Add as possible target
                possibleTargetNodes.Add(adjNode);
            }

            return possibleTargetNodes;
        }
        public bool CanBuildLadder(BlockmapNode from, Direction side, BlockmapNode to) => GetPossibleLadderTargetNodes(from, side).Contains(to);
        public void BuildLadder(BlockmapNode from, BlockmapNode to, Direction side, bool updateWorld)
        {
            SpawnEntity(EntityDefOf.Ladder, from, side, isMirrored: false, Gaia, preInit: e => ((Ladder)e).PreInit(to), updateWorld: updateWorld);
        }

        public bool CanBuildDoor(BlockmapNode node, Direction side, int height)
        {
            return CanBuildOnNodeSide(node, side, height, allowSlopes: false);
        }
        public Door BuildDoor(BlockmapNode node, Direction side, int height, bool isMirrored, bool updateWorld)
        {
            return (Door)(SpawnEntity(EntityDefOf.Door, node, side, isMirrored, Gaia, updateWorld, height));
        }

        public Zone AddZone(HashSet<Vector2Int> coordinates, Actor actor, bool providesVision, ZoneVisibility visibility)
        {
            int id = ZoneIdCounter++;
            Zone newZone = new Zone(this, id, actor, coordinates, providesVision, visibility);
            Zones.Add(id, newZone);
            return newZone;
        }

        public Room AddRoom(string label, List<BlockmapNode> nodes, List<Wall> interiorWalls)
        {
            int id = RoomIdCounter++;
            Room newRoom = new Room(id, label, nodes, interiorWalls);
            Rooms.Add(id, newRoom);
            return newRoom;
        }

        public void AddToInventory(Entity item, Entity holder, bool updateWorld)
        {
            if (!item.CanBeHeldByOtherEntities) throw new System.Exception($"Can't add {item.LabelCap} to the inventory of {holder.LabelCap} because it can't be held by other entities.");

            BlockmapNode node = item.OriginNode;
            item.AddToInventory(holder);

            if (updateWorld) UpdateWorldSystems(node.WorldCoordinates);
        }
        public void DropFromInventory(Entity item, BlockmapNode newOriginNode, bool updateWorld)
        {
            item.DropFromInventory(newOriginNode);

            if (updateWorld) UpdateWorldSystems(newOriginNode.WorldCoordinates);
        }

        // Helpers
        private bool CanBuildOnNodeSide(BlockmapNode node, Direction side, int height, bool allowSlopes)
        {
            List<Direction> affectedSides = HelperFunctions.GetAffectedDirections(side);

            // Check if node already has a fence on that side
            foreach (Direction dir in affectedSides)
                if (node.Fences.ContainsKey(dir)) return false;

            // Check if a ladder is already there
            if (node.SourceLadders.ContainsKey(side)) return false;

            // Check if a door is already there
            if (node.Doors.ContainsKey(side)) return false;

            // Check if we are on a slope without being allowed
            if (!allowSlopes && !node.IsFlat(side)) return false;

            // Check for each affected corner if enough space is above node
            foreach (Direction corner in HelperFunctions.GetAffectedCorners(side))
            {
                int freeHeadSpace = node.FreeHeadSpace[corner];
                if (freeHeadSpace < height) return false;
            }

            // Check if we would overlap a wall
            int minAltitude = node.GetMinAltitude(side);
            int maxAltitude = node.GetMaxAltitude(side) + height - 1;
            for (int i = minAltitude; i <= maxAltitude; i++)
            {
                Vector3Int globalCellCoordinates = new Vector3Int(node.WorldCoordinates.x, i, node.WorldCoordinates.y);
                List<Wall> walls = GetWalls(globalCellCoordinates);
                foreach (Wall w in walls)
                {
                    foreach (Direction dir in affectedSides)
                    {
                        if (w.Side == dir) return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Generates all meshes of the world.
        /// </summary>
        public void RedrawFullWorld()
        {
            foreach (Chunk chunk in Chunks.Values) RedrawChunk(chunk);
        }

        /// <summary>
        /// Redraws all chunks in the given area.
        /// </summary>
        public void RedrawNodes(Parcel area)
        {
            List<Chunk> affectedChunks = new List<Chunk>();

            for (int x = area.Position.x - 1; x < area.Position.x + area.Dimensions.x + 1; x++)
            {
                for (int y = area.Position.y - 1; y < area.Position.y + area.Dimensions.y + 1; y++)
                {
                    Chunk chunk = GetChunk((new Vector2Int(x, y)));
                    if (chunk != null && !affectedChunks.Contains(chunk)) affectedChunks.Add(chunk);
                }
            }

            foreach (Chunk chunk in affectedChunks) RedrawChunk(chunk);
        }

        /// <summary>
        /// Fully redraws a single chunk.
        /// </summary>
        public void RedrawChunk(Chunk chunk)
        {
            pm_RedrawChunk.Begin();
            chunk.DrawMeshes();
            chunk.SetVisibility(ActiveVisionActor);
            chunk.ShowGrid(DisplaySettings.IsShowingGrid);
            chunk.ShowTextures(DisplaySettings.IsShowingTextures);
            chunk.ShowTileBlending(DisplaySettings.IsShowingTileBlending);
            chunk.DrawZoneBorders(ActiveVisionActor);
            pm_RedrawChunk.End();
        }

        /// <summary>
        /// Updates the visibility for the full map according to the current actor vision.
        /// </summary>
        public void UpdateVisibility()
        {
            foreach (Chunk c in Chunks.Values) UpdateVisibility(c);
        }
        /// <summary>
        /// Updates the visibility display for one chunk according to the current actor vision.
        /// </summary>
        public void UpdateVisibility(Chunk c)
        {
            c.SetVisibility(ActiveVisionActor);
            c.DrawZoneBorders(ActiveVisionActor);
        }

        private void OnCameraFacingDirectionChanged()
        {
            if(DisplaySettings.VisionCutoffMode == VisionCutoffMode.RoomPerspectiveCutoff && DisplaySettings.PerspectiveVisionCutoffTarget != null)
            {
                UpdateVisibility();
            }
        }

        /// <summary>
        /// Gets called when the visibility of a node changes on the specified chunk for the specified actor.
        /// </summary>
        public void OnVisibilityChanged(Chunk c, Actor actor)
        {
            if (actor == ActiveVisionActor) UpdateVisibility(c);
        }

        /// <summary>
        /// Updates the vision of all entities.
        /// </summary>
        public void UpdateVisionOfAllEntities()
        {
            UpdateEntityVision(Entities.Values.ToList());
        }

        /// <summary>
        /// Updates the vision of all entities that have the given area within their vision range.
        /// </summary>
        public void UpdateVisionOfNearbyEntities(Parcel area, Actor excludeActor = null)
        {
            List<Entity> entities = GetEntitiesThatCanSeeParcel(area);
            if (excludeActor != null) entities = entities.Where(x => x.Actor != excludeActor).ToList();
            UpdateEntityVision(entities);
        }

        private void UpdateEntityVision(List<Entity> visionUpdateEntities)
        {
            foreach (Entity e in visionUpdateEntities) e.UpdateVision();
        }

        public void ToggleGridOverlay()
        {
            DisplaySettings.ToggleGrid();
            UpdateGridOverlay();
        }
        public void ShowGridOverlay(bool value)
        {
            if (!IsInitialized) return;
            DisplaySettings.ShowGrid(value);
            UpdateGridOverlay();
        }
        private void UpdateGridOverlay()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowGrid(DisplaySettings.IsShowingGrid);
        }
        private void UpdateZoneBorders()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.DrawZoneBorders(ActiveVisionActor);
        }

        public void ToggleNavmesh()
        {
            DisplaySettings.ToggleNavmesh();
            UpdateNavmeshDisplay();
        }
        public void SetNavmeshEntity(MovingEntity entity)
        {
            NavmeshEntity = entity;
            UpdateNavmeshDisplay();
        }
        public void ShowNavmesh(bool value)
        {
            if (!IsInitialized) return;
            DisplaySettings.ShowNavmesh(value);
            UpdateNavmeshDisplay();
        }

        private void UpdateNavmeshDisplay()
        {
            if (NavmeshVisualizer.Singleton == null) return;

            if (DisplaySettings.IsShowingNavmesh) NavmeshVisualizer.Singleton.Visualize(this, NavmeshEntity);
            else NavmeshVisualizer.Singleton.ClearVisualization();
        }

        public void ToggleTextureMode()
        {
            DisplaySettings.ToggleTextures();
            UpdateTextureMode();
        }
        public void ShowTextures(bool value)
        {
            if (!IsInitialized) return;
            DisplaySettings.ShowTextures(value);
            UpdateTextureMode();
        }
        private void UpdateTextureMode()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowTextures(DisplaySettings.IsShowingTextures);
        }

        public void ToggleTileBlending()
        {
            DisplaySettings.ToggleTileBlending();
            UpdateTileBlending();
        }
        public void ShowTileBlending(bool value)
        {
            if (!IsInitialized) return;
            DisplaySettings.ShowTileBlending(value);
            UpdateTileBlending();
        }
        private void UpdateTileBlending()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowTileBlending(DisplaySettings.IsShowingTileBlending);
        }
        public void SetVisionCutoffMode(VisionCutoffMode mode)
        {
            DisplaySettings.SetVisionCutoffMode(mode);
            UpdateVisibility();
        }
        public void SetVisionCutoffAltitude(int altitude)
        {
            DisplaySettings.SetVisionCutoffAltitude(Mathf.Clamp(altitude, 0, MAX_ALTITUDE));
            UpdateVisibility();
        }
        public void SetVisionCutoffPerspectiveHeight(int height)
        {
            DisplaySettings.SetVisionCutoffPerspectiveHeight(Mathf.Clamp(height, 0, MAX_ALTITUDE));
            UpdateVisibility();
        }

        public void EnableAbsoluteVisionCutoffAt(int altitude)
        {
            DisplaySettings.SetVisionCutoffMode(VisionCutoffMode.AbsoluteCutoff);
            DisplaySettings.SetVisionCutoffAltitude(altitude);
            UpdateVisibility();
        }
        public void EnablePerspectiveVisionCutoff(Entity target)
        {
            DisplaySettings.SetVisionCutoffPerspectiveTarget(target);
            DisplaySettings.SetVisionCutoffMode(VisionCutoffMode.RoomPerspectiveCutoff);
            DisplaySettings.SetVisionCutoffAltitude(target.MinAltitude);
            UpdateVisibility();
        }

        public void SetActiveVisionActor(Actor actor)
        {
            ActiveVisionActor = actor;
            UpdateVisibility();

            // Ghost markers
            foreach(Actor a in GetAllActors())
            {
                foreach (Entity ghostMaker in a.GhostMarkers)
                {
                    ghostMaker.SetGhostMarkerVisibility(actor == a);
                }
            }
        }

        public void CameraJumpToFocusEntity(Entity e)
        {
            Camera.SetPosition(e.WorldPosition);
        }
        public void CameraJumpToFocusNode(BlockmapNode n)
        {
            Camera.SetPosition(n.ShapeCenterWorldPosition);
        }
        public void CameraPanToFocusEntity(Entity e, float duration, bool followAfterPan, bool unbreakableFollow = false, System.Action callback = null)
        {
            Camera.PanTo(duration, e.WorldPosition, followAfterPan ? e : null, unbreakableFollow, callback);
        }

        #endregion

        #region Getters

        public List<BlockmapNode> GetAllNodes() => Nodes.Values.ToList();
        public List<Chunk> GetAllChunks() => Chunks.Values.ToList();
        public List<Actor> GetAllActors() => Actors.Values.ToList();
        public List<Entity> GetAllEntities() => Entities.Values.ToList();
        public List<WaterBody> GetAllWaterBodies() => WaterBodies.Values.ToList();
        public List<Fence> GetAllFences() => Fences.Values.ToList();
        public List<Zone> GetAllZones() => Zones.Values.ToList();
        public List<Wall> GetAllWalls() => Walls.Values.ToList();

        public BlockmapNode GetNode(int id) => Nodes.ContainsKey(id) ? Nodes[id] : null;
        public Actor GetActor(int id) => Actors[id];
        public Entity GetEntity(int id) => Entities[id];
        public WaterBody GetWaterBody(int id) => WaterBodies[id];
        public Fence GetFence(int id) => Fences[id];
        public Zone GetZone(int id) => Zones[id];
        public Wall GetWall(int id) => Walls[id];


        public bool IsInWorld(Vector2Int worldCoordinates)
        {
            return GetChunk(worldCoordinates) != null;
        }

        public Chunk GetChunk(Vector2Int worldCoordinates)
        {
            int chunkCoordinateX = worldCoordinates.x / ChunkSize;
            if (worldCoordinates.x < 0) chunkCoordinateX--;

            int chunkCoordinateY = worldCoordinates.y / ChunkSize;
            if (worldCoordinates.y < 0) chunkCoordinateY--;

            Vector2Int chunkCoordinates = new Vector2Int(chunkCoordinateX, chunkCoordinateY);

            Chunks.TryGetValue(chunkCoordinates, out Chunk value);

            return value;
        }
        public Chunk GetChunk(int worldX, int worldY) => GetChunk(new Vector2Int(worldX, worldY));
        public List<Chunk> GetChunks(Vector2Int chunkCoordinates, int rangeEast, int rangeNorth)
        {
            List<Chunk> chunks = new List<Chunk>();
            for (int x = 0; x <= rangeEast; x++)
            {
                for (int y = 0; y <= rangeNorth; y++)
                {
                    Vector2Int coordinates = chunkCoordinates + new Vector2Int(x, y);
                    if (Chunks.ContainsKey(coordinates)) chunks.Add(Chunks[coordinates]);
                }
            }
            return chunks;
        }

        public Actor GetActor(string name) => Actors.Values.First(x => x.Label == name);

        public List<BlockmapNode> GetNodes(Vector2Int worldCoordinates, int altitude)
        {
            return GetNodes(worldCoordinates, altitude, altitude);
        }
        public List<BlockmapNode> GetNodes(Vector2Int worldCoordinates, int minAltitude, int maxAltitude)
        {
            return GetNodes(worldCoordinates).Where(x => x.BaseAltitude >= minAltitude && x.BaseAltitude <= maxAltitude).ToList();
        }
        public List<BlockmapNode> GetNodes(Vector2Int worldCoordinates)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            if (chunk == null) return EmptyNodeList; // not in world

            Vector2Int localCoordinates = GetLocalCoordinates(worldCoordinates);
            return chunk.GetNodes(localCoordinates);
        }
        public Vector2Int GetLocalCoordinates(Vector2Int worldCoordinates)
        {
            int localX = HelperFunctions.Mod(worldCoordinates.x, ChunkSize);
            int localY = HelperFunctions.Mod(worldCoordinates.y, ChunkSize);
            return new Vector2Int(localX, localY);
        }

        public List<GroundNode> GetAllGroundNodes()
        {
            List<GroundNode> nodes = new List<GroundNode>();
            foreach (Chunk c in Chunks.Values) nodes.AddRange(c.GetAllGroundNodes());
            return nodes;
        }
        public GroundNode GetGroundNode(int x, int y) => GetGroundNode(new Vector2Int(x, y));
        public GroundNode GetGroundNode(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetGroundNode(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public List<GroundNode> GetGroundNodes(Vector2Int worldCoordinates, int rangeEast, int rangeNorth)
        {
            List<GroundNode> nodes = new List<GroundNode>();
            for (int x = 0; x < rangeEast; x++)
            {
                for (int y = 0; y < rangeNorth; y++)
                {
                    Vector2Int coordinates = worldCoordinates + new Vector2Int(x, y);
                    GroundNode node = GetGroundNode(coordinates);
                    if (node != null) nodes.Add(node);
                }
            }
            return nodes;
        }

        public WaterNode GetWaterNode(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetWaterNode(chunk.GetLocalCoordinates(worldCoordinates));
        }

        public List<AirNode> GetAllAirNodes()
        {
            List<AirNode> nodes = new List<AirNode>();
            foreach (Chunk c in Chunks.Values) nodes.AddRange(c.GetAllAirNodes());
            return nodes;
        }
        public List<AirNode> GetAirNodes(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return new List<AirNode>();

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetAirNodes(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public List<AirNode> GetAirNodes(Vector2Int worldCoordinates, int height)
        {
            return GetAirNodes(worldCoordinates, height, height);
        }
        public List<AirNode> GetAirNodes(Vector2Int worldCoordinates, int minHeight, int maxHeight)
        {
            return GetAirNodes(worldCoordinates).Where(x => x.BaseAltitude >= minHeight && x.BaseAltitude <= maxHeight).ToList();
        }

        public Vector2Int GetWorldCoordinates(Vector3 worldPosition)
        {
            Vector2Int worldCoords = new Vector2Int((int)worldPosition.x, (int)worldPosition.z);
            if (worldPosition.x < 0) worldCoords.x -= 1;
            if (worldPosition.z < 0) worldCoords.y -= 1;
            return worldCoords;
        }
        public Vector2Int GetWorldCoordinates(Vector2 worldPosition2d)
        {
            return GetWorldCoordinates(new Vector3(worldPosition2d.x, 0f, worldPosition2d.y));
        }

        public List<BlockmapNode> GetAdjacentNodes(Vector2Int worldCoordinates, Direction dir)
        {
            Vector2Int targetCoordinates = HelperFunctions.GetCoordinatesInDirection(worldCoordinates, dir);
            return GetNodes(targetCoordinates);
        }
        public GroundNode GetAdjacentGroundNode(Vector2Int worldCoordinates, Direction dir)
        {
            return GetGroundNode(HelperFunctions.GetCoordinatesInDirection(worldCoordinates, dir));
        }
        public GroundNode GetAdjacentGroundNode(BlockmapNode node, Direction dir)
        {
            return GetAdjacentGroundNode(node.WorldCoordinates, dir);
        }
        public WaterNode GetAdjacentWaterNode(Vector2Int worldCoordinates, Direction dir)
        {
            return GetWaterNode(HelperFunctions.GetCoordinatesInDirection(worldCoordinates, dir));
        }

        public List<Entity> GetEntities(Vector2Int worldCoordinates, int minHeight, int maxHeight)
        {
            List<Entity> entities = new List<Entity>();
            foreach (BlockmapNode node in GetNodes(worldCoordinates))
                foreach (Entity e in node.Entities)
                    if (e.MinAltitude <= maxHeight && e.MaxAltitude >= minHeight)
                        entities.Add(e);
            return entities;
        }
        public List<Entity> GetNearbyEntities(Vector3 position, int rangeEast = 1, int rangeNorth = 1)
        {
            return Entities.Values.Where(x => x.CanSee && Vector3.Distance(x.GetWorldCenter(), position) <= x.VisionRange + (rangeEast) + (rangeNorth)).ToList();
        }
        public HashSet<Entity> GetNearbyEntities(Vector3 worldPosition, float maxDistance)
        {
            HashSet<Entity> nearbyEntities = new HashSet<Entity>();
            float maxDistanceSquared = maxDistance * maxDistance;

            // Determine the chunk radius to search
            int chunkRadius = Mathf.CeilToInt(maxDistance / ChunkSize);

            Vector2Int targetChunkCoords = GetChunk(new Vector2Int((int)worldPosition.x, (int)worldPosition.z)).Coordinates;

            // Only check entities in chunks where there's a chance that they're close enough
            for (int x = -chunkRadius; x <= chunkRadius; x++)
            {
                for (int y = -chunkRadius; y <= chunkRadius; y++)
                {
                    Vector2Int neighborChunkCoords = targetChunkCoords + new Vector2Int(x, y);
                    Chunks.TryGetValue(neighborChunkCoords, out Chunk neighbourChunk);
                    if (neighbourChunk != null)
                    {
                        foreach (var entity in neighbourChunk.GetAllEntities())
                        {
                            if ((entity.WorldPosition - worldPosition).sqrMagnitude <= maxDistanceSquared)
                            {
                                nearbyEntities.Add(entity);
                            }
                        }
                    }
                }
            }

            return nearbyEntities;
        }
        public List<Entity> GetEntitiesThatCanSeeParcel(Parcel parcel)
        {
            return Entities.Values
                .Where(e => e.CanSee && IsParcelWithinVision(e, parcel))
                .ToList();
        }

        private bool IsParcelWithinVision(Entity entity, Parcel parcel)
        {
            Vector3 center = entity.GetWorldCenter();

            // Clamp the entity's position to the parcel's bounding rectangle to find the closest point
            float clampedX = Mathf.Clamp(center.x, parcel.MinX, parcel.MaxX);
            float clampedZ = Mathf.Clamp(center.z, parcel.MinY, parcel.MaxY);

            // Calculate the squared distance from entity center to that closest point
            float dx = center.x - clampedX;
            float dz = center.z - clampedZ;
            float distanceSquared = dx * dx + dz * dz;

            // Compare with squared vision range for efficiency (no need for sqrt)
            float visionRangeSquared = entity.VisionRange * entity.VisionRange;

            return distanceSquared <= visionRangeSquared;
        }

        public List<Wall> GetWalls(Vector2Int worldCoordinates)
        {
            if (WallsByWorldCoordinates2D.TryGetValue(worldCoordinates, out List<Wall> walls)) return walls;
            else return new List<Wall>();
        }
        public List<Wall> GetWalls(Vector3Int globalCellCoordinates)
        {
            Vector2Int globalCoordinates2D = new Vector2Int(globalCellCoordinates.x, globalCellCoordinates.z);
            Chunk chunk = GetChunk(globalCoordinates2D);
            if (chunk == null) return new List<Wall>();
            Vector2Int localWorldCoordinates = chunk.GetLocalCoordinates(globalCoordinates2D);
            Vector3Int localCellCoordinates = new Vector3Int(localWorldCoordinates.x, globalCellCoordinates.y, localWorldCoordinates.y);
            return chunk.GetWalls(localCellCoordinates);
        }
        public Wall GetWall(Vector3Int globalCellCoordinates, Direction side)
        {
            return GetWalls(globalCellCoordinates).FirstOrDefault(x => x.Side == side);
        }

        /// <summary>
        /// Returns if the given side of the given cell is blocked by something. (Fence / Wall / Ladder / Door)
        /// </summary>
        public bool IsBlocked(Vector3Int globalCellCoordinates, Direction side)
        {
            // Check if a wall is there
            if (GetWall(globalCellCoordinates, side) != null) return true;

            // Check if a fence/ladder/door is there
            int altitude = globalCellCoordinates.y;
            List<BlockmapNode> nodesOnCoordinate = GetNodes(new Vector2Int(globalCellCoordinates.x, globalCellCoordinates.z));
            foreach(BlockmapNode node in nodesOnCoordinate)
            {
                if (node.Fences.TryGetValue(side, out Fence fence) && fence.MinAltitude <= altitude && fence.MaxAltitude >= altitude) return true;
                if (node.Doors.Any(x => x.Value.CurrentBlockingDirection == side && x.Value.MinAltitude <= altitude && x.Value.MaxAltitude >= altitude)) return true;
                if (node.SourceLadders.TryGetValue(side, out Ladder ladder) && ladder.MinAltitude <= altitude && ladder.MaxAltitude >= altitude) return true;
            }

            return false;
        }

        /// <summary>
        /// If present, returns the climbable (cliff, ladder, fence, wall) on the given side of a specific cell.
        /// </summary>
        public IClimbable GetClimbable(Vector3Int globalCellCoordinates, Direction side)
        {
            Vector2Int worldCoordinates = new Vector2Int(globalCellCoordinates.x, globalCellCoordinates.z);
            int altitude = globalCellCoordinates.y;
            Vector2Int adjacentCoordinates = HelperFunctions.GetCoordinatesInDirection(worldCoordinates, side);
            Vector3Int adjacentCellCoordinates = new Vector3Int(adjacentCoordinates.x, altitude, adjacentCoordinates.y);
            Direction oppositeSide = HelperFunctions.GetOppositeDirection(side);

            // Nodes on source side
            foreach (BlockmapNode node in GetNodes(worldCoordinates))
            {
                // Look for fence on source side
                if (node.Fences.TryGetValue(side, out Fence fence))
                    if (fence.MinAltitude <= altitude && fence.MaxAltitude >= altitude)
                        return fence;

                // Look for ladder on source side
                if (node.SourceLadders.TryGetValue(side, out Ladder ladder))
                    if (ladder.LadderStartAltitude <= altitude && ladder.LadderEndAltitude > altitude)
                        return ladder;
            }

            // Look for wall piece on source side
            Wall wall = GetWall(globalCellCoordinates, side);
            if (wall != null) return wall;

            // Nodes on adjacent opposite side
            foreach (BlockmapNode adjNode in GetNodes(adjacentCoordinates))
            {
                // Look for fence on adjacent opposite side
                if (adjNode.Fences.TryGetValue(oppositeSide, out Fence fence))
                    if (fence.MinAltitude <= altitude && fence.MaxAltitude >= altitude)
                        return fence;
            }

            // Look for wall piece on adjacent opposite side
            Wall oppositeWall = GetWall(adjacentCellCoordinates, oppositeSide);
            if (oppositeWall != null) return oppositeWall;

            // Look for cliif on opposite side
            GroundNode adjGroundNode = GetGroundNode(adjacentCoordinates);
            if (adjGroundNode.GetMinAltitude(oppositeSide) > altitude) return Cliff.Instance;

            // No climbable found
            return null;
        }

        public Vector3Int GetLocalCellCoordinates(Vector3Int globalCellCoordinates)
        {
            Vector2Int globalWorldCoordinates = new Vector2Int(globalCellCoordinates.x, globalCellCoordinates.z);
            Chunk chunk = GetChunk(globalWorldCoordinates);
            if (chunk == null) throw new System.Exception("The given global cell position " + globalCellCoordinates.ToString() + " is outside the world.");

            Vector2Int localWorldCoordinates = chunk.GetLocalCoordinates(globalWorldCoordinates);
            return new Vector3Int(localWorldCoordinates.x, globalCellCoordinates.y, localWorldCoordinates.y);
        }

        public float GetWorldY(int altitude)
        {
            return altitude * NodeHeight;
        }

        /// <summary>
        /// Returns the exact world height (y-coordinate) of the given node at the given position by shooting a ray at it from above.
        /// <br/> Only works when the node mesh is drawn.
        /// </summary>
        public float GetWorldAltitudeAt(Vector2 worldPosition2d, BlockmapNode node)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(worldPosition2d.x, 20f, worldPosition2d.y), -Vector3.up, 1000f);
            foreach (RaycastHit hit in hits)
            {
                Transform objectHit = hit.transform;

                // We hit the ground mesh we are looking for
                if (node.Type == NodeType.Ground && objectHit.gameObject.layer == Layer_GroundNodeMesh)
                {
                    Vector3 hitPosition = hit.point;
                    return Mathf.Max(hitPosition.y, node.BaseWorldAltitude);
                }

                // We hit the air node mesh of the level we are looking for
                if (node.Type == NodeType.Air && objectHit.gameObject.layer == Layer_AirNodeMesh && objectHit.GetComponent<AirNodeMesh>().Altitude == node.BaseAltitude)
                {
                    Vector3 hitPosition = hit.point;
                    return hitPosition.y;
                }

                // We hit the water mesh we are looking for
                if (node.Type == NodeType.Water && objectHit.gameObject.layer == Layer_WaterMesh)
                {
                    Vector3 hitPosition = hit.point;
                    return hitPosition.y;
                }
            }

            Debug.Log($"Couldn't detect world altitude at {worldPosition2d} for {node}. Applying fallback: {node.BaseWorldAltitude}");
            return node.BaseWorldAltitude; // fallback

            throw new System.Exception("World height not found for node of type " + node.Type.ToString());
        }

        public bool IsOnNode(Vector2 worldPosition2d, BlockmapNode node)
        {
            return GetWorldCoordinates(worldPosition2d) == node.WorldCoordinates;
        }

        public GroundNode GetRandomGroundNode()
        {
            List<Chunk> candidateChunks = Chunks.Values.ToList();
            Chunk chosenChunk = candidateChunks[Random.Range(0, candidateChunks.Count)];

            int x = Random.Range(0, ChunkSize);
            int y = Random.Range(0, ChunkSize);
            return chosenChunk.GetGroundNode(x, y);
        }
        public BlockmapNode GetRandomPassableNode(Entity entity) // very not performant
        {
            List<BlockmapNode> candidateNodes = EmptyNodeList;
            foreach (Chunk c in Chunks.Values) candidateNodes.AddRange(c.GetAllNodes().Where(x => x.IsPassable(entity)).ToList());
            return candidateNodes[Random.Range(0, candidateNodes.Count)];
        }

        /// <summary>
        /// Checks and returns if two adjacent nodes match seamlessly in the given direction.
        /// <br/> 
        /// </summary>
        public bool DoAdjacentHeightsMatch(BlockmapNode fromNode, BlockmapNode toNode, Direction dir)
        {
            switch (dir)
            {
                case Direction.N:
                    return (fromNode.Altitude[Direction.NE] == toNode.Altitude[Direction.SE]) && (fromNode.Altitude[Direction.NW] == toNode.Altitude[Direction.SW]);

                case Direction.S:
                    return (fromNode.Altitude[Direction.SE] == toNode.Altitude[Direction.NE]) && (fromNode.Altitude[Direction.SW] == toNode.Altitude[Direction.NW]);

                case Direction.E:
                    return (fromNode.Altitude[Direction.SE] == toNode.Altitude[Direction.SW]) && (fromNode.Altitude[Direction.NE] == toNode.Altitude[Direction.NW]);

                case Direction.W:
                    return (fromNode.Altitude[Direction.SW] == toNode.Altitude[Direction.SE]) && (fromNode.Altitude[Direction.NW] == toNode.Altitude[Direction.NE]);

                case Direction.NW:
                    return fromNode.Altitude[Direction.NW] == toNode.Altitude[Direction.SE];

                case Direction.NE:
                    return fromNode.Altitude[Direction.NE] == toNode.Altitude[Direction.SW];

                case Direction.SW:
                    return fromNode.Altitude[Direction.SW] == toNode.Altitude[Direction.NE];

                case Direction.SE:
                    return fromNode.Altitude[Direction.SE] == toNode.Altitude[Direction.NW];

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the amount of overlapping corners between two node altitudes
        /// </summary>
        public int GetNumOverlappingCorners(Dictionary<Direction, int> n1, Dictionary<Direction, int> n2)
        {
            int numOverlaps = 0;
            foreach (var dir in n1.Keys)
                if (n1[dir] == n2[dir]) numOverlaps++;
            return numOverlaps;
        }

        /// <summary>
        /// Returns true if at least one corner of topHeight is above botHeight and none of them are below botHeight
        /// </summary>
        public bool IsAbove(Dictionary<Direction, int> topHeight, Dictionary<Direction, int> botHeight)
        {
            bool isAbove = false;
            foreach (Direction dir in HelperFunctions.GetCorners())
            {
                if (topHeight[dir] > botHeight[dir]) isAbove = true;
                if (topHeight[dir] < botHeight[dir]) return false;
            }
            return isAbove;
        }

        public bool DoFullyOverlap(BlockmapNode node1, BlockmapNode node2)
        {
            foreach (Direction dir in HelperFunctions.GetCorners())
                if (node1.Altitude[dir] != node2.Altitude[dir]) return false;
            return true;
        }

        #endregion

        #region Save / Load

        public void ExposeDataForSaveAndLoad()
        {
            SaveLoadManager.SaveOrLoadPrimitive(ref Name, "name");
            SaveLoadManager.SaveOrLoadPrimitive(ref numChunksPerSide, "numChunksPerSide");
            SaveLoadManager.SaveOrLoadPrimitive(ref CliffMaterialPath, "cliffMaterial");

            SaveLoadManager.SaveOrLoadDeepDictionary(ref Actors, "actors");
            SaveLoadManager.SaveOrLoadDeepDictionary(ref Nodes, "nodes");
            SaveLoadManager.SaveOrLoadDeepDictionary(ref Entities, "entities");
            SaveLoadManager.SaveOrLoadDeepDictionary(ref Fences, "fences");
            SaveLoadManager.SaveOrLoadDeepDictionary(ref Walls, "walls");
            SaveLoadManager.SaveOrLoadDeepDictionary(ref WaterBodies, "waterBodies");
            SaveLoadManager.SaveOrLoadDeepDictionary(ref Zones, "zones");
            SaveLoadManager.SaveOrLoadDeepDictionary(ref Rooms, "rooms");

            if (SaveLoadManager.IsLoading)
            {
                Init();

                foreach (Actor actor in Actors.Values) actor.PostLoad();
                foreach (BlockmapNode node in Nodes.Values) node.PostLoad();
                foreach (Entity e in Entities.Values) e.PostLoad();
                foreach (Fence f in Fences.Values) f.PostLoad();
                foreach (Wall w in Walls.Values) w.PostLoad();
                foreach (WaterBody wb in WaterBodies.Values) wb.PostLoad();
                foreach (Zone z in Zones.Values) z.PostLoad();
                foreach (Room r in Rooms.Values) r.PostLoad();
            }
        }

        public int Id => 0; // Unused since World is the root object of save data and there is always only one.

        #endregion
    }
}
