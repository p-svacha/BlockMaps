using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Object representing one world with its own node/pathfinding system. One world is a closed system.
    /// <br/> A world is made up of different chunks.
    /// </summary>
    public class World : MonoBehaviour
    {
        private WorldData WorldData;

        /// <summary>
        /// Maximum y coordiante a tile can have.
        /// </summary>
        public const int MAX_ALTITUDE = 30;

        /// <summary>
        /// Physical height (y) of a tile.
        /// </summary>
        public const float TILE_HEIGHT = 0.5f;

        public const int MAP_EDGE_ALTITUDE = -1;

        public const float MAP_EDGE_HEIGHT = (MAP_EDGE_ALTITUDE * TILE_HEIGHT);

        /// <summary>
        /// How much the colors/textures of adjacent surface tiles flow into each other (0 - 0.5).
        /// </summary>
        public const float SURFACE_TILE_BLENDING = 0.4f;

        /// <summary>
        /// How much height of a tile water covers.
        /// </summary>
        public const float WATER_HEIGHT = 0.9f;

        public string Name { get; private set; }
        private int InitializeStep; // Some initialization steps need to happen frames after others, this is to keep count
        public bool IsInitialized { get; private set; }
        public int ChunkSize { get; private set; }
        public int MinX, MaxX, MinY, MaxY;
        public Vector2Int Dimensions { get; private set; }
        public WorldEntityLibrary EntityLibrary { get; private set; }


        // Database
        private Dictionary<int, BlockmapNode> Nodes = new Dictionary<int, BlockmapNode>();
        private Dictionary<Vector2Int, Chunk> Chunks = new Dictionary<Vector2Int, Chunk>();
        private Dictionary<int, Actor> Actors = new Dictionary<int, Actor>();
        private Dictionary<int, Entity> Entities = new Dictionary<int, Entity>();
        private Dictionary<int, WaterBody> WaterBodies = new Dictionary<int, WaterBody>();
        private Dictionary<int, Fence> Fences = new Dictionary<int, Fence>();
        private Dictionary<int, Wall> Walls = new Dictionary<int, Wall>();
        private Dictionary<int, Zone> Zones = new Dictionary<int, Zone>();

        private int NodeIdCounter;
        private int EntityIdCounter;
        private int WaterBodyIdCounter;
        private int ActorIdCounter;
        private int ZoneIdCounter;
        private int FenceIdCounter;
        private int WallIdCounter;

        // Database indexes
        private Dictionary<Vector2Int, List<Wall>> WallsByWorldCoordinates2D = new Dictionary<Vector2Int, List<Wall>>();

        // Camera
        public BlockmapCamera Camera { get; private set; }

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
        public int Layer_GroundNode;
        public int Layer_EntityMesh;
        public int Layer_EntityVisionCollider;
        public int Layer_AirNode;
        public int Layer_Water;
        public int Layer_Fence;
        public int Layer_ProceduralEntityMesh;
        public int Layer_Wall;

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

        public event System.Action<BlockmapNode, BlockmapNode> OnHoveredNodeChanged;
        public event System.Action<GroundNode, GroundNode> OnHoveredGroundNodeChanged;
        public event System.Action<Chunk, Chunk> OnHoveredChunkChanged;
        public event System.Action<Entity, Entity> OnHoveredEntityChanged;

        // Draw modes
        public bool IsShowingGrid { get; private set; }
        public bool IsShowingNavmesh { get; private set; }
        public MovingEntity NavmeshEntity { get; private set; }
        public bool IsShowingTextures { get; private set; }
        public bool IsShowingTileBlending { get; private set; }
        public bool IsVisionCutoffEnabled { get; private set; }
        public int VisionCutoffAltitude { get; private set; }

        // Cache
        private static readonly List<BlockmapNode> EmptyNodeList = new List<BlockmapNode>();

        // Performance Profilers
        static readonly ProfilerMarker pm_RedrawChunk = new ProfilerMarker("RedrawChunk");


        #region Init

        /// <summary>
        /// Initializes the world by only drawing the nodes and creating the actors from the given WorldData.
        /// <br/> Everything else in the data is discarded. Navmesh will not be generated either.
        /// </summary>
        public void SimpleInit(WorldData data)
        {
            // Init general
            Name = data.Name;
            ChunkSize = data.ChunkSize;
            WorldData = data;

            Layer_GroundNode = LayerMask.NameToLayer("Terrain");
            Layer_EntityMesh = LayerMask.NameToLayer("EntityMesh");
            Layer_EntityVisionCollider = LayerMask.NameToLayer("EntityVisionCollider");
            Layer_AirNode = LayerMask.NameToLayer("Path");
            Layer_Water = LayerMask.NameToLayer("Water");
            Layer_Fence = LayerMask.NameToLayer("Fence");
            Layer_ProceduralEntityMesh = LayerMask.NameToLayer("ProceduralEntityMesh");
            Layer_Wall = LayerMask.NameToLayer("Wall");

            // Init pathfinder
            Pathfinder.Init(this);

            // Init database id's
            NodeIdCounter = data.MaxNodeId + 1;
            EntityIdCounter = data.MaxEntityId + 1;
            WaterBodyIdCounter = data.MaxWaterBodyId + 1;
            ActorIdCounter = data.MaxActorId + 1;
            ZoneIdCounter = data.MaxZoneId + 1;
            FenceIdCounter = data.MaxFenceId + 1;
            WallIdCounter = data.MaxWallId + 1;

            // Init actors
            foreach (ActorData actorData in data.Actors) Actors.Add(actorData.Id, Actor.Load(this, actorData));
            Gaia = Actors[0];

            // Init nodes
            foreach (ChunkData chunkData in data.Chunks)
            {
                Chunk chunk = Chunk.Load(this, chunkData);
                Chunks.Add(new Vector2Int(chunkData.ChunkCoordinateX, chunkData.ChunkCoordinateY), chunk);
            }

            // Calculate world bounds
            MinX = Chunks.Values.Min(x => x.Coordinates.x) * ChunkSize;
            MaxX = Chunks.Values.Max(x => x.Coordinates.x) * ChunkSize + (ChunkSize - 1);
            MinY = Chunks.Values.Min(x => x.Coordinates.y) * ChunkSize;
            MaxY = Chunks.Values.Max(x => x.Coordinates.y) * ChunkSize + (ChunkSize - 1);
            Dimensions = new Vector2Int(MaxX - MinX, MaxY - MinY);

            // Init camera
            Camera = GameObject.Find("Main Camera").GetComponent<BlockmapCamera>();
            BlockmapNode initialCameraFocusNode = GetGroundNode(new Vector2Int(0, 0));
            Camera.SetPosition(new Vector3(initialCameraFocusNode.WorldCoordinates.x, initialCameraFocusNode.BaseAltitude * TILE_HEIGHT, initialCameraFocusNode.WorldCoordinates.y));
            Camera.SetZoom(10f);
            Camera.SetAngle(225);

            IsInitialized = true;
        }

        /// <summary>
        /// Fully initializes the given world data including all objects and generating the full navmesh.
        /// <br/> Will take a while until finished, wait for IsInitialized = true;
        /// </summary>
        public void FullInit(WorldData data, WorldEntityLibrary entityLibrary)
        {
            SimpleInit(data);

            EntityLibrary = entityLibrary;

            // Init fences
            foreach (FenceData fenceData in data.Fences)
            {
                Fence fence = Fence.Load(this, fenceData);
                RegisterFence(fence);
            }

            // Init walls
            foreach (WallData wallData in data.Walls)
            {
                Wall wall = Wall.Load(this, wallData);
                RegisterWall(wall);
            }

            // Init water bodies
            foreach (WaterBodyData waterData in data.WaterBodies)
            {
                WaterBody water = WaterBody.Load(this, waterData);
                WaterBodies.Add(waterData.Id, water);
            }

            // Init zones
            foreach (ZoneData zoneData in data.Zones)
            {
                Zone zone = Zone.Load(this, zoneData);
                Zones.Add(zoneData.Id, zone);
            }

            IsInitialized = false;
            InitializeStep = 1;
        }

        /// <summary>
        /// Gets executed each frame while the world is being initialized
        /// </summary>
        private void UpdateInitialization()
        {
            if (IsInitialized) return;

            // Frame 1 after initilaization: Do stuff that requires drawn node meshes.
            if (InitializeStep == 1)
            {
                // Init entities
                foreach (EntityData entityData in WorldData.Entities)
                {
                    Entity e = Entity.Load(this, entityData);

                    // Register entity
                    RegisterEntity(e);
                }

                // Draw node meshes because we need to shoot rays to generate navmesh
                DrawNodes();

                InitializeStep++;
                return;
            }

            // Frame 2 after initialization: Do stuff that requires entities to be at the correct world position
            if (InitializeStep == 2)
            {
                foreach (Entity e in Entities.Values) e.UpdateVision();

                InitializeStep++;
                return;
            }

            // When all post-initialization steps are done, regenerate navmesh and we're good to go
            if (InitializeStep == 3)
            {
                GenerateFullNavmesh();
                InitializeStep++;
                return;
            }
        }

        #endregion

        #region Update

        void Update()
        {
            // Check if world is done initializing
            if (!IsInitialized)
            {
                UpdateInitialization();
                return;
            }

            // Regular updates
            UpdateHoveredObjects();
            foreach (Entity e in Entities.Values) e.UpdateEntity();
        }

        /// <summary>
        /// Updates all hovered objects and fires events if anything changed.
        /// </summary>
        private void UpdateHoveredObjects()
        {
            RaycastHit hit;
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

            // Shoot a raycast on ground and air layers to detect hovered nodes
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_GroundNode | 1 << Layer_AirNode | 1 << Layer_Water | 1 << Layer_Fence | 1 << Layer_Wall))
            {
                Transform objectHit = hit.transform;

                Vector3 hitPosition = hit.point;
                IsHoveringWorld = true;

                HoveredWorldPosition = hitPosition;
                NodeHoverMode9 = GetNodeHoverMode9(hitPosition);
                NodeHoverMode8 = GetNodeHoverMode8(hitPosition);
                NodeHoverModeSides = GetNodeHoverModeSides(hitPosition);
                HoveredWorldCoordinates = GetWorldCoordinates(hitPosition);

                // Update chunk
                newHoveredChunk = objectHit.GetComponentInParent<Chunk>();

                // Hit ground node
                if (objectHit.gameObject.layer == Layer_GroundNode)
                {
                    newHoveredGroundNode = GetGroundNode(HoveredWorldCoordinates);
                    newHoveredNode = newHoveredGroundNode;
                    newHoveredDynamicNode = newHoveredGroundNode;
                }

                // Hit air node
                else if (objectHit.gameObject.layer == Layer_AirNode)
                {
                    newHoveredAirNode = GetAirNodeFromRaycastHit(hit);

                    newHoveredNode = newHoveredAirNode;
                    newHoveredDynamicNode = newHoveredAirNode;

                    if (newHoveredAirNode != null) HoveredWorldCoordinates = newHoveredAirNode.WorldCoordinates;
                }

                // Hit water node
                else if (objectHit.gameObject.layer == Layer_Water)
                {
                    WaterNode hitWaterNode = GetWaterNode(HoveredWorldCoordinates);

                    if (hitWaterNode != null)
                    {
                        if (hitWaterNode.GroundNode.IsCenterUnderWater)
                        {
                            newHoveredNode = hitWaterNode;
                            newHoveredWaterBody = hitWaterNode.WaterBody;
                        }
                        else newHoveredNode = hitWaterNode.GroundNode;

                        newHoveredGroundNode = hitWaterNode.GroundNode;
                        newHoveredDynamicNode = hitWaterNode.GroundNode;
                    }
                }

                // Hit fence
                else if (objectHit.gameObject.layer == Layer_Fence)
                {
                    newHoveredFence = GetFenceFromRaycastHit(hit);
                    if (newHoveredFence != null) HoveredWorldCoordinates = newHoveredFence.Node.WorldCoordinates;
                }

                // Hit wall
                else if (objectHit.gameObject.layer == Layer_Wall)
                {
                    newHoveredWall = GetWallFromRaycastHit(hit);
                    if (newHoveredWall != null) HoveredWorldCoordinates = newHoveredWall.WorldCoordinates;
                }
            }

            // Ray to detect entity
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_GroundNode | 1 << Layer_AirNode | 1 << Layer_EntityMesh | 1 << Layer_ProceduralEntityMesh))
            {
                if (hit.transform.gameObject.layer == Layer_EntityMesh)
                {
                    Transform objectHit = hit.transform;
                    newHoveredEntity = hit.transform.parent.GetComponentInChildren<Entity>();
                }
                else if (hit.transform.gameObject.layer == Layer_ProceduralEntityMesh)
                {
                    newHoveredEntity = GetProceduralEntityFromRaycastHit(hit);
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
            Vector2Int hitCoordinates = GetWorldCoordinates(hit.point);
            int altitude = hit.transform.GetComponent<AirNodeMesh>().Altitude;

            List<AirNode> hitAirNodes = GetAirNodes(hitCoordinates).Where(x => x.BaseAltitude == altitude).ToList();
            if(hitAirNodes.Count == 1) return hitAirNodes[0];
            if(hitAirNodes.Count > 1) // Get node that closest matches the hit height if multiple are on that base base height
            {
                Vector2 relativePos = new Vector2(hit.point.x % 1f, hit.point.z % 1f);
                AirNode closestNode = null;
                float closestDistance = float.MaxValue;
                foreach(AirNode node in hitAirNodes)
                {
                    float distance = Mathf.Abs((hit.point.y % 1f) - node.GetExactLocalAltitudeAt(relativePos));
                    if(distance < closestDistance)
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
                    float distance = Mathf.Abs((hit.point.y % 1f) - node.GetExactLocalAltitudeAt(relativePos));
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

            Vector2Int hitCoordinates = GetWorldCoordinates(hit.point);

            List<BlockmapNode> hitNodes = GetNodes(hitCoordinates, altitude).OrderByDescending(x => x.MaxAltitude).ToList();
            Direction primaryHitSide = GetNodeHoverMode8(hit.point);
            List<Direction> otherPossibleHitSides = GetNodeHoverModes8(hit.point);

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

            Debug.LogWarning("GetFenceFromRaycastHit failed to find a fence at world position: " + hit.point.ToString());
            return null;
        }
        private ProceduralEntity GetProceduralEntityFromRaycastHit(RaycastHit hit)
        {
            ProceduralEntityMesh hitMesh = hit.transform.GetComponent<ProceduralEntityMesh>();
            List<BlockmapNode> hitNodes = GetNodes(HoveredWorldCoordinates, hitMesh.Altitude);

            // If the exact node we hit has a procedural entity, return that
            BlockmapNode targetNode = hitNodes.FirstOrDefault(x => x.Entities.Any(e => e is ProceduralEntity));
            if (targetNode != null) return targetNode.Entities.First(x => x is ProceduralEntity) as ProceduralEntity;

            return null;
        }
        public Wall GetWallFromRaycastHit(RaycastHit hit)
        {
            // Check if there is a wall on the exact HoveredWorldCoordinates in the HovereModeSide direction
            Vector2Int hitWorldCoordinate = GetWorldCoordinates(hit.point);
            WallMesh hitMesh = hit.transform.GetComponent<WallMesh>();
            int altitude = hitMesh.Altitude;
            Vector3Int globalCellCoordinates = new Vector3Int(hitWorldCoordinate.x, altitude, hitWorldCoordinate.y);
            Direction primaryHitSide = NodeHoverModeSides;
            List<Direction> otherPossibleHitSides = GetNodeHoverModes8(hit.point);

            List<Wall> cellWalls = GetWalls(globalCellCoordinates);
            if (cellWalls != null)
            {
                // Check primary hit side
                Wall hitWall = cellWalls.FirstOrDefault(x => x.Side == NodeHoverModeSides);
                if (hitWall != null) return hitWall;

                // Check other possible hit sides
                foreach (Direction dir in otherPossibleHitSides)
                {
                    Wall otherHitWall = cellWalls.FirstOrDefault(x => x.Side == dir);
                    if (otherHitWall != null) return otherHitWall;
                }
            }

            // If we are exactly on a north or east edge we have to adjust the hit position slightly, else we are 1 coordinate off and don't find anything
            // Do the same detection stuff again with the offset position
            Vector3 offsetHitPosition = hit.point + new Vector3(-0.01f, 0f, -0.01f);
            Vector2Int offsetCoordinates = GetWorldCoordinates(offsetHitPosition);
            Vector3Int offsetCellCoordinates = new Vector3Int(offsetCoordinates.x, altitude, offsetCoordinates.y);
            Direction primaryOffsetSide = GetNodeHoverMode8(offsetHitPosition);
            List<Direction> otherPossibleOffsetSides = GetNodeHoverModes8(offsetHitPosition);

            List<Wall> offsetCellWalls = GetWalls(offsetCellCoordinates);
            if (offsetCellWalls != null)
            {
                // Check primary hit side
                Wall hitWall = offsetCellWalls.FirstOrDefault(x => x.Side == primaryOffsetSide);
                if (hitWall != null) return hitWall;

                // Check other possible hit sides
                foreach (Direction dir in otherPossibleOffsetSides)
                {
                    Wall otherHitWall = offsetCellWalls.FirstOrDefault(x => x.Side == dir);
                    if (otherHitWall != null) return otherHitWall;
                }
            }

            // Didn't find anything
            Debug.LogWarning("GetWallFromRaycastHit failed to find a wall at world position: " + hit.point.ToString() + "/" + hitWorldCoordinate.ToString() + " (offset was " + offsetHitPosition.ToString() + "/" + offsetCoordinates.ToString() + ")");
            return null;
        }


        /// <summary>
        /// Returns the exact world position of where the cursor is currently hovering on a specific altitude level.
        /// <br/>Calculation is based on intersecting mouse hover with an invisible plane on the given altitude.
        /// </summary>
        private Vector3 GetAltitudeHitPoint(int altitude)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, altitude * World.TILE_HEIGHT, 0f));
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

        #region Actions

        public void RegisterNode(BlockmapNode node)
        {
            Nodes.Add(node.Id, node); // Global registry

            // Chunk registry
            node.Chunk.Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(node);
            if (node is GroundNode groundNode) node.Chunk.GroundNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = groundNode;
            else if (node is WaterNode waterNode) node.Chunk.WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = waterNode;
            else if (node is AirNode airNode) node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(airNode);
        }
        public void DeregisterNode(BlockmapNode node)
        {
            // Destroy fences on node
            while (node.Fences.Count > 0) DeregisterFence(node.Fences.Values.ToList()[0]);

            // Destroy ladders from and to node
            while (node.SourceLadders.Count > 0) RemoveEntity(node.SourceLadders.Values.ToList()[0]);
            while (node.TargetLadders.Count > 0) RemoveEntity(node.TargetLadders.Values.ToList()[0]);

            // Node
            Nodes.Remove(node.Id); // Global registry

            // Chunk registry
            node.Chunk.Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(node);
            if (node is GroundNode groundNode) node.Chunk.GroundNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if (node is WaterNode waterNode) node.Chunk.WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if (node is AirNode airNode) node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(airNode);
        }

        public Actor AddActor(string name, Color color)
        {
            int id = ActorIdCounter++;
            Actor newActor = new Actor(this, id, name, color);
            Actors.Add(id, newActor);

            return newActor;
        }
        public void ResetExploration(Actor actor)
        {
            foreach (BlockmapNode node in Nodes.Values) node.RemoveExploredBy(actor);
            foreach (Entity entity in Entities.Values) entity.ResetLastKnownPositionFor(actor);
            foreach (Wall wall in Walls.Values) wall.RemoveExploredBy(actor);

            foreach (Entity entity in Entities.Values.Where(x => x.Owner == actor)) entity.UpdateVision();

            UpdateVisibility();
        }
        public void ExploreEverything(Actor actor)
        {
            foreach (BlockmapNode node in Nodes.Values) node.AddExploredBy(actor);
            foreach (Entity entity in Entities.Values) entity.UpdateLastKnownPositionFor(actor);
            foreach (Wall wall in Walls.Values) wall.AddExploredBy(actor);

            foreach (Entity entity in Entities.Values.Where(x => x.Owner == actor)) entity.UpdateVision();

            UpdateVisibility();
        }

        private void UpdateNavmeshDelayed(List<BlockmapNode> nodes, bool isInitialization = false)
        {
            StartCoroutine(DoUpdateNavmesh(nodes, isInitialization));
        }
        /// <summary>
        /// Updates the navmesh for the given nodes by recalculating all transitions originating from them.
        /// <br/> All navmesh calculations need to be done through this function.
        /// </summary>
        private IEnumerator DoUpdateNavmesh(List<BlockmapNode> nodes, bool isInitialization)
        {
            yield return new WaitForFixedUpdate();

            System.Diagnostics.Stopwatch fullNavmeshTimer = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch recalcPassabilityTimer = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch straightTransitionsTimer = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch diagonalTransitionsTimer = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch climbTransitionsTimer = new System.Diagnostics.Stopwatch();
            fullNavmeshTimer.Start();

            recalcPassabilityTimer.Start();
            foreach (BlockmapNode node in nodes) node.RecalcuatePassability();
            recalcPassabilityTimer.Stop();
            Debug.Log("Recalculating passability took " + recalcPassabilityTimer.ElapsedMilliseconds + " ms for " + nodes.Count + " nodes.");

            foreach (BlockmapNode node in nodes) node.ResetTransitions();

            straightTransitionsTimer.Start();
            foreach (BlockmapNode node in nodes) node.SetStraightAdjacentTransitions();
            straightTransitionsTimer.Stop();
            Debug.Log("Updating straight transitions took " + straightTransitionsTimer.ElapsedMilliseconds + " ms for " + nodes.Count + " nodes.");

            diagonalTransitionsTimer.Start();
            foreach (BlockmapNode node in nodes) node.SetDiagonalAdjacentTransitions();
            diagonalTransitionsTimer.Stop();
            Debug.Log("Updating diagonal transitions took " + diagonalTransitionsTimer.ElapsedMilliseconds + " ms for " + nodes.Count + " nodes.");

            
            climbTransitionsTimer.Start();
            foreach (BlockmapNode node in nodes) node.SetClimbTransitions();
            climbTransitionsTimer.Stop();
            Debug.Log("Updating climb transitions took " + climbTransitionsTimer.ElapsedMilliseconds + " ms for " + nodes.Count + " nodes.");
            
            if (isInitialization) IsInitialized = true;

            fullNavmeshTimer.Stop();
            Debug.Log("Updating ALL transitions took " + fullNavmeshTimer.ElapsedMilliseconds + " ms for " + nodes.Count + " nodes.");

            UpdateNavmeshDisplayDelayed();
        }
        public void GenerateFullNavmesh()
        {
            List<BlockmapNode> nodesToUpdate = new List<BlockmapNode>();
            foreach (Chunk chunk in Chunks.Values) nodesToUpdate.AddRange(chunk.GetAllNodes());

            UpdateNavmeshDelayed(nodesToUpdate, isInitialization: true);
        }
        public void UpdateNavmeshAround(Vector2Int worldCoordinates, int rangeEast = 1, int rangeNorth = 1)
        {
            // Get nodes that need update
            List<BlockmapNode> nodesToUpdate = new List<BlockmapNode>();
            for (int y = worldCoordinates.y - 1; y <= worldCoordinates.y + rangeNorth; y++)
            {
                for (int x = worldCoordinates.x - 1; x <= worldCoordinates.x + rangeEast; x++)
                {
                    Vector2Int coordinates = new Vector2Int(x, y);
                    if (!IsInWorld(coordinates)) continue;

                    nodesToUpdate.AddRange(GetNodes(coordinates));
                }
            }

            UpdateNavmeshDelayed(nodesToUpdate);
        }

        public bool CanChangeShape(DynamicNode node, Direction mode, bool isIncrease)
        {
            return node.CanChangeShape(mode, isIncrease);
        }
        public void ChangeShape(DynamicNode node, Direction mode, bool isIncrease)
        {
            node.ChangeShape(mode, isIncrease);

            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(node.CenterWorldPosition);
        }

        public void SetSurface(DynamicNode node, SurfaceDef surface, bool updateWorld = true)
        {
            node.SetSurface(surface);

            if (!updateWorld) return;
            RedrawNodesAround(node.WorldCoordinates);
            UpdateNavmeshDisplayDelayed();
        }
        public void SetGroundNodeAsVoid(GroundNode node, bool updateWorld = true)
        {
            node.SetAsVoid();

            if (!updateWorld) return;
            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateNavmeshDisplayDelayed();
        }
        public void UnsetGroundNodeAsVoid(GroundNode node, int altitude, bool updateWorld = true)
        {
            node.UnsetAsVoid(altitude);

            if (!updateWorld) return;
            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateNavmeshDisplayDelayed();
        }

        public bool CanBuildAirNode(Vector2Int worldCoordinates, int height)
        {
            if (!IsInWorld(worldCoordinates)) return false;

            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            GroundNode groundNode = chunk.GetGroundNode(localCoordinates);
            Dictionary<Direction, int> newNodeAltitude = HelperFunctions.GetFlatHeights(height);

            // Check if an entity or fence below is blocking this space
            List<BlockmapNode> belowNodes = GetNodes(worldCoordinates, 0, height);
            foreach (BlockmapNode node in belowNodes)
            {
                foreach (Entity e in node.Entities)
                    if (e.MaxAltitude >= height)
                        return false;

                foreach (Fence fence in node.Fences.Values)
                    if (fence.MaxAltitude >= height)
                        return false;
            }

            // Check if underwater
            WaterNode water = GetWaterNode(worldCoordinates);
            if (water != null && water.WaterBody.ShoreHeight > height) return false;
            
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
        public void BuildAirPath(Vector2Int worldCoordinates, int height, SurfaceDef surfaceDef)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);

            AirNode newNode = new AirNode(this, chunk, NodeIdCounter++, localCoordinates, HelperFunctions.GetFlatHeights(height), surfaceDef);
            RegisterNode(newNode);

            UpdateNavmeshAround(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(newNode.CenterWorldPosition);
        }
        public bool CanRemoveAirNode(AirNode node)
        {
            if (node.Entities.Count > 0) return false;

            return true;
        }
        public void RemoveAirNode(AirNode node)
        {
            DeregisterNode(node);

            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(node.CenterWorldPosition);
        }

        public bool CanSpawnEntity(Entity entityPrefab, BlockmapNode node, Direction rotation, bool forceHeadspaceRecalc = false)
        {
            HashSet<BlockmapNode> occupiedNodes = entityPrefab.GetOccupiedNodes(this, node, rotation); // get nodes that would be occupied when placing the entity on the given node

            // Terrain below entity is not fully connected and therefore occupiedNodes is null
            if (occupiedNodes == null) return false;

            Vector3 placePos = entityPrefab.GetWorldPosition(this, node, rotation);
            int minAltitude = Mathf.FloorToInt(placePos.y); // min y coordinate that this entity will occupy on all occupied tiles
            int maxAltitude = minAltitude + entityPrefab.Height - 1; // max y coordinate that this entity will occupy on all occupied tiles

            // Make some checks for all nodes that would be occupied when placing the entity on the given node
            foreach (BlockmapNode occupiedNode in occupiedNodes)
            {
                // Check if the place position is on water
                if (occupiedNode is WaterNode waterNode && placePos.y <= waterNode.WaterBody.WaterSurfaceWorldHeight) return false;
                if (occupiedNode is GroundNode groundNode && groundNode.WaterNode != null && placePos.y <= groundNode.WaterNode.WaterBody.WaterSurfaceWorldHeight) return false;

                // Check if entity can stand here
                if (forceHeadspaceRecalc) occupiedNode.RecalcuatePassability();
                int headSpace = occupiedNode.MaxPassableHeight[Direction.None];
                if (minAltitude + headSpace <= maxAltitude) return false;

                // Check if alredy has an entity
                if (occupiedNode.Entities.Count > 0) return false;

                // Check if flat
                if (entityPrefab.RequiresFlatTerrain && !occupiedNode.IsFlat()) return false;
            }

            return true;
        }
        public Entity SpawnEntity(Entity prefab, BlockmapNode node, Direction rotation, Actor actor, bool isInstance = false, bool updateWorld = true)
        {
            // Create entity object
            Entity instance = isInstance ? prefab : GameObject.Instantiate(prefab, transform);

            // Init
            instance.Init(EntityIdCounter++, this, node, rotation, actor);

            // Register new entity
            RegisterEntity(instance);

            // Redraw chunk meshes if it is a procedural entity
            if (updateWorld && instance is ProceduralEntity) RedrawNodesAround(node.WorldCoordinates);

            // Update vision around new entity (and then update it's visibility when done)
            if (updateWorld) UpdateVisionOfNearbyEntitiesDelayed(instance.OriginNode.CenterWorldPosition, callback: instance.UpdateVisibility);

            // Update pathfinding navmesh
            if (updateWorld) UpdateNavmeshAround(node.WorldCoordinates, instance.GetDimensions().x, instance.GetDimensions().z);

            // Return new instance
            return instance;
        }
        public void RemoveEntity(Entity entityToRemove, bool updateWorld = true)
        {
            // De-register entity
            DeregisterEntity(entityToRemove);

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
            foreach (BlockmapNode node in entityToRemove.OccupiedNodes)
            {
                node.RemoveEntity(entityToRemove);
                node.Chunk.RemoveEntity(entityToRemove);
            }

            // Redraw chunk meshes if it is a procedural entity
            if (updateWorld && entityToRemove is ProceduralEntity) RedrawNodesAround(entityToRemove.OriginNode.WorldCoordinates);

            // Update pathfinding navmesh
            if(updateWorld) UpdateNavmeshAround(entityToRemove.OriginNode.WorldCoordinates, entityToRemove.GetDimensions().x, entityToRemove.GetDimensions().z);

            // Update visibility of all chunks affected by the entity vision if the entity belongs to the active vision actor
            if (updateWorld && entityToRemove.Owner == ActiveVisionActor)
                foreach (Chunk c in chunksAffectedByVision)
                    UpdateVisibility(c);

            // Destroy
            entityToRemove.DestroySelf();

            // Update vision of all other entities near the entity (doesn't work instantly bcuz destroying takes too long)
            if(updateWorld) UpdateVisionOfNearbyEntitiesDelayed(entityToRemove.GetWorldCenter());
        }
        public void RemoveEntities(BlockmapNode node, bool updateWorld = true)
        {
            while (node.Entities.Count > 0) RemoveEntity(node.Entities.First(), updateWorld);
        }
        public void RegisterEntity(Entity entity)
        {
            Entities.Add(entity.Id, entity);
            entity.Owner.Entities.Add(entity);

            entity.OnRegister();
        }
        public void DeregisterEntity(Entity entity)
        {
            Entities.Remove(entity.Id);
            entity.Owner.Entities.Remove(entity);

            entity.OnDeregister();
        }

        public WaterBody CanAddWater(GroundNode node, int maxDepth) // returns null when cannot
        {
            int shoreHeight = node.BaseAltitude + 1;
            WaterBody waterBody = new WaterBody(); // dummy water body to store data that can later be used to create a real water body
            waterBody.ShoreHeight = shoreHeight;
            waterBody.CoveredNodes = new List<GroundNode>();

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
                if (waterBody.CoveredNodes.Contains(checkNode)) continue;
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

                    waterBody.CoveredNodes.Add(checkNode);

                    if (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.NE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.N), Direction.S));
                    if (checkNode.Altitude[Direction.NE] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.E), Direction.W));
                    if (checkNode.Altitude[Direction.SW] < shoreHeight || checkNode.Altitude[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.S), Direction.N));
                    if (checkNode.Altitude[Direction.NW] < shoreHeight || checkNode.Altitude[Direction.SW] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.W), Direction.E));
                }
                else { } // above water
            }

            return waterBody;
        }
        public void AddWaterBody(WaterBody data, bool updateNavmesh)
        {
            // Create a new water nodes for each covered surface node
            List<WaterNode> waterNodes = new List<WaterNode>();
            foreach (GroundNode node in data.CoveredNodes)
            {
                WaterNode waterNode = new WaterNode(this, node.Chunk, NodeIdCounter++, node.LocalCoordinates, data.ShoreHeight);
                waterNodes.Add(waterNode);
                RegisterNode(waterNode);
            }

            // Make a new water body instance with a unique id
            WaterBody newWaterBody = new WaterBody(WaterBodyIdCounter++, data.ShoreHeight, waterNodes, data.CoveredNodes);

            // Get chunks that will have nodes covered in new water body
            HashSet<Chunk> affectedChunks = new HashSet<Chunk>();
            foreach (BlockmapNode node in newWaterBody.CoveredNodes) affectedChunks.Add(node.Chunk);

            // Redraw affected chunks
            foreach (Chunk c in affectedChunks) RedrawChunk(c);

            // Update navmesh
            if (updateNavmesh)
                UpdateNavmeshAround(new Vector2Int(newWaterBody.MinX, newWaterBody.MinY), newWaterBody.MaxX - newWaterBody.MinX + 1, newWaterBody.MaxY - newWaterBody.MinY + 1);

            // Register water body
            WaterBodies.Add(newWaterBody.Id, newWaterBody);
        }
        public void RemoveWaterBody(WaterBody water)
        {
            // De-register
            WaterBodies.Remove(water.Id);

            // Get chunks that will had nodes covered in water body
            HashSet<Chunk> affectedChunks = new HashSet<Chunk>();
            foreach (GroundNode node in water.CoveredNodes) affectedChunks.Add(node.Chunk);

            // Remove water node reference from all covered surface nodes
            foreach (GroundNode node in water.CoveredNodes) node.SetWaterNode(null);

            // Deregister deleted water nodes
            foreach (WaterNode node in water.WaterNodes) DeregisterNode(node);

            // Update navmesh
            UpdateNavmeshAround(new Vector2Int(water.MinX, water.MinY), water.MaxX - water.MinX + 1, water.MaxY - water.MinY + 1);

            // Redraw affected chunks
            foreach (Chunk c in affectedChunks) RedrawChunk(c);
        }

        public bool CanBuildFence(FenceType type, BlockmapNode node, Direction side, int height)
        {
            List<Direction> affectedSides = HelperFunctions.GetAffectedDirections(side);

            // Check if disallowed corner
            if (HelperFunctions.IsCorner(side) && !type.CanBuildOnCorners) return false;

            // Adjust height if it's higher than fence type allows
            if (height > type.MaxHeight) height = type.MaxHeight;

            return CanBuildOnNodeSide(node, side, height, allowSlopes: true);
        }
        public void BuildFence(FenceTypeId type, BlockmapNode node, Direction side, int height, bool updateWorld = true) => BuildFence(FenceTypeManager.Instance.GetFenceType(type), node, side, height, updateWorld);
        public void BuildFence(FenceType type, BlockmapNode node, Direction side, int height, bool updateWorld = true)
        {
            // Adjust height if it's higher than fence type allows
            if (height > type.MaxHeight) height = type.MaxHeight;

            // Create and register new fence
            Fence fence = new Fence(type);
            fence.Init(FenceIdCounter++, node, side, height);
            RegisterFence(fence);

            if (updateWorld)
            {
                UpdateNavmeshAround(node.WorldCoordinates);
                RedrawNodesAround(node.WorldCoordinates);
                UpdateVisionOfNearbyEntitiesDelayed(node.CenterWorldPosition);
            }
        }
        public void RegisterFence(Fence fence)
        {
            // In world
            Fences.Add(fence.Id, fence);

            // In chunk
            fence.Node.Chunk.RegisterFence(fence);
        }
        public void RemoveFence(Fence fence)
        {
            // Deregister
            BlockmapNode node = fence.Node;
            DeregisterFence(fence);

            // Update world
            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(node.CenterWorldPosition);
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
                foreach(Door door in nodeOnCoordinate.Doors.Values)
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
        public void BuildWall(Vector3Int globalCellCoordinates, Direction side, WallShapeDef shape, WallMaterialDef material, bool mirrored)
        {
            // Create and register new wall
            Wall newWall = new Wall(this, WallIdCounter++, globalCellCoordinates, side, shape, material, mirrored);
            RegisterWall(newWall);

            // Update systems around cell
            UpdateNavmeshAround(newWall.WorldCoordinates);
            RedrawNodesAround(newWall.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(newWall.CellCenterWorldPosition);
        }
        private void RegisterWall(Wall wall) // add to database and add all references in different objects
        {
            // In world
            Walls.Add(wall.Id, wall);

            // Database index by world coordinate 2D
            if (WallsByWorldCoordinates2D.ContainsKey(wall.WorldCoordinates)) WallsByWorldCoordinates2D[wall.WorldCoordinates].Add(wall);
            else WallsByWorldCoordinates2D.Add(wall.WorldCoordinates, new List<Wall>() { wall });

            // In chunk
            wall.Chunk.RegisterWall(wall);
        }
        public void RemoveWall(Wall wall)
        {
            // Deregister
            DeregisterWall(wall);

            // Update systems around cell
            UpdateNavmeshAround(wall.WorldCoordinates);
            RedrawNodesAround(wall.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(wall.CellCenterWorldPosition);
        }
        public void DeregisterWall(Wall wall)
        {
            Walls.Remove(wall.Id);
            wall.Chunk.DeregisterWall(wall);
            WallsByWorldCoordinates2D[wall.WorldCoordinates].Remove(wall);
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
            Vector2Int targetCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(source.WorldCoordinates, side);

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
        public void BuildLadder(BlockmapNode from, BlockmapNode to, Direction side)
        {
            Ladder instance = Ladder.GetInstance(from, to, side);
            SpawnEntity(instance, from, side, Gaia, isInstance: true);
        }

        public bool CanBuildDoor(BlockmapNode node, Direction side, int height)
        {
            return CanBuildOnNodeSide(node, side, height, allowSlopes: false);
        }
        public void BuildDoor(BlockmapNode node, Direction side, int height, bool isMirrored)
        {
            Door instance = Door.GetInstance(node, side, height, isMirrored);
            SpawnEntity(instance, node, side, Gaia, isInstance: true);
        }

        public Zone AddZone(HashSet<Vector2Int> coordinates, Actor actor, bool providesVision, bool showBorders)
        {
            int id = ZoneIdCounter++;
            Zone newZone = new Zone(this, id, actor, coordinates, providesVision, showBorders);
            Zones.Add(id, newZone);
            return newZone;
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
        public void DrawNodes()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.DrawMeshes();

            SetActiveVisionActor(null);

            UpdateGridOverlay();
            UpdateNavmeshDisplayDelayed();
            UpdateTextureMode();
            UpdateTileBlending();
        }

        /// <summary>
        /// Redraws all chunks around the given coordinates.
        /// </summary>
        public void RedrawNodesAround(Vector2Int worldCoordinates, int rangeEast = 1, int rangeNorth = 1)
        {
            List<Chunk> affectedChunks = new List<Chunk>();

            for (int y = worldCoordinates.y - 1; y <= worldCoordinates.y + rangeNorth; y++)
            {
                for (int x = worldCoordinates.x - 1; x <= worldCoordinates.x + rangeEast; x++)
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
            chunk.ShowGrid(IsShowingGrid);
            chunk.ShowTextures(IsShowingTextures);
            chunk.ShowTileBlending(IsShowingTileBlending);
            chunk.DrawZoneBorders();
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
        }

        /// <summary>
        /// Gets called when the visibility of a node changes on the specified chunk for the specified actor.
        /// </summary>
        public void OnVisibilityChanged(Chunk c, Actor actor)
        {
            if (actor == ActiveVisionActor) UpdateVisibility(c);
        }

        /// <summary>
        /// Recalculates the vision of all entities that have the given position within their vision range.
        /// <br/> Is delayed by one frame so all draw calls and vision collider movements can be completed before shooting the vision rays.
        /// <br/> Each entity is additionally delayed by 1 frame to not cause lag spikes.
        /// </summary>
        public void UpdateVisionOfNearbyEntitiesDelayed(Vector3 position, int rangeEast = 1, int rangeNorth = 1, System.Action callback = null, Actor excludeActor = null)
        {
            List<Entity> entitiesToUpdate = GetNearbyEntities(position, rangeEast, rangeNorth);
            if (excludeActor != null) entitiesToUpdate = entitiesToUpdate.Where(x => x.Owner != excludeActor).ToList();
            StartCoroutine(DoUpdateVisionOfEntities(entitiesToUpdate, callback));
        }
        /// <summary>
        /// Updates the vision of all given entities over the next few frames and calls callback when done.
        /// </summary>
        public void UpdateVisionDelayed(List<Entity> entities, System.Action callback)
        {
            StartCoroutine(DoUpdateVisionOfEntities(entities, callback));
        }
        private IEnumerator DoUpdateVisionOfEntities(List<Entity> entities, System.Action callback)
        {
            yield return new WaitForFixedUpdate();

            //Debug.Log("Updating vision of " + entitiesToUpdate.Count + " entities.");
            foreach (Entity e in entities)
            {
                e.UpdateVision();
                yield return 0;
            }

            if (callback != null) callback.Invoke();
        }

        public void ToggleGridOverlay()
        {
            IsShowingGrid = !IsShowingGrid;
            UpdateGridOverlay();
        }
        public void ShowGridOverlay(bool value)
        {
            if (!IsInitialized) return;
            IsShowingGrid = value;
            UpdateGridOverlay();
        }
        private void UpdateGridOverlay()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowGrid(IsShowingGrid);
        }

        public void ToggleNavmesh()
        {
            IsShowingNavmesh = !IsShowingNavmesh;
            UpdateNavmeshDisplayDelayed();
        }
        public void SetNavmeshEntity(MovingEntity entity)
        {
            NavmeshEntity = entity;
            UpdateNavmeshDisplayDelayed();
        }
        public void ShowNavmesh(bool value)
        {
            if (!IsInitialized) return;
            IsShowingNavmesh = value;
            UpdateNavmeshDisplayDelayed();
        }
        public void UpdateNavmeshDisplayDelayed()
        {
            StartCoroutine(DoUpdateNavmeshDisplay());
        }
        private IEnumerator DoUpdateNavmeshDisplay()
        {
            if (NavmeshVisualizer.Singleton == null) yield break;
            yield return new WaitForFixedUpdate();
            
            if (IsShowingNavmesh) NavmeshVisualizer.Singleton.Visualize(this, NavmeshEntity);
            else NavmeshVisualizer.Singleton.ClearVisualization();
        }

        public void ToggleTextureMode()
        {
            IsShowingTextures = !IsShowingTextures;
            UpdateTextureMode();
        }
        public void ShowTextures(bool value)
        {
            if (!IsInitialized) return;
            IsShowingTextures = value;
            UpdateTextureMode();
        }
        private void UpdateTextureMode()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowTextures(IsShowingTextures);
        }

        public void ToggleTileBlending()
        {
            IsShowingTileBlending = !IsShowingTileBlending;
            UpdateTileBlending();
        }
        public void ShowTileBlending(bool value)
        {
            if (!IsInitialized) return;
            IsShowingTileBlending = value;
            UpdateTileBlending();
        }
        private void UpdateTileBlending()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowTileBlending(IsShowingTileBlending);
        }
        public void ToggleVisionCutoff()
        {
            IsVisionCutoffEnabled = !IsVisionCutoffEnabled;
            UpdateVisionCutoff();
        }
        public void EnableVisionCutoff(bool value)
        {
            IsVisionCutoffEnabled = value;
            UpdateVisionCutoff();
        }
        public void SetVisionCutoffAltitude(int value)
        {
            VisionCutoffAltitude = value;
            if (VisionCutoffAltitude < 0) VisionCutoffAltitude = 0;
            if (VisionCutoffAltitude > MAX_ALTITUDE) VisionCutoffAltitude = MAX_ALTITUDE;
            
            if (IsVisionCutoffEnabled) UpdateVisionCutoff();
        }
        private void UpdateVisionCutoff()
        {
            foreach (Chunk c in Chunks.Values) c.SetVisionCutoffAltitude(IsVisionCutoffEnabled ? VisionCutoffAltitude : -1);
        }

        public void SetActiveVisionActor(Actor actor)
        {
            ActiveVisionActor = actor;
            UpdateVisibility();
        }

        public void CameraJumpToFocusEntity(Entity e)
        {
            Camera.SetPosition(e.WorldPosition);
        }
        public void CameraPanToFocusEntity(Entity e, float duration, bool followAfterPan, bool unbreakableFollow = false)
        {
            Camera.PanTo(duration, e.WorldPosition, followAfterPan ? e : null, unbreakableFollow);
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

        public BlockmapNode GetNode(int id) => Nodes[id];
        public Actor GetActor(int id) => Actors[id];
        public Entity GetEntity(int id) => Entities[id];
        public WaterBody GetWaterBody(int id) => WaterBodies[id];
        public Fence GetFence(int id) => Fences[id];
        public Zone GetZone(int id) => Zones[id];
        public Wall GetWall(int id) => Walls[id];


        /// <summary>
        /// Returns if the given world coordinates exist in this world.
        /// </summary>
        public bool IsInWorld(Vector2Int worldCoordinates)
        {
            return GetChunk(worldCoordinates) != null;
        }
        public bool IsValidNodeHeight(Dictionary<Direction, int> height)
        {
            if (height.Values.Any(x => x < World.MAP_EDGE_ALTITUDE)) return false;
            if (height.Values.Any(x => x > World.MAX_ALTITUDE)) return false;

            return !(Mathf.Abs(height[Direction.SE] - height[Direction.SW]) > 1 ||
            Mathf.Abs(height[Direction.SW] - height[Direction.NW]) > 1 ||
            Mathf.Abs(height[Direction.NW] - height[Direction.NE]) > 1 ||
            Mathf.Abs(height[Direction.NE] - height[Direction.SE]) > 1);
        }

        
        /// <summary>
        /// Returns the chunk that the given world coordinates are on.
        /// </summary>
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
            for(int x = 0; x <= rangeEast; x++)
            {
                for(int y = 0; y <= rangeNorth; y++)
                {
                    Vector2Int coordinates = chunkCoordinates + new Vector2Int(x, y);
                    if(Chunks.ContainsKey(coordinates)) chunks.Add(Chunks[coordinates]);
                }
            }
            return chunks;
        }

        public Actor GetActor(string name) => Actors.Values.First(x => x.Name == name);
        
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
            if(chunk == null) return EmptyNodeList; // not in world

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
            for(int x = 0; x < rangeEast; x++)
            {
                for(int y = 0; y < rangeNorth; y++)
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
            Vector2Int targetCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(worldCoordinates, dir);
            return GetNodes(targetCoordinates);
        }
        public GroundNode GetAdjacentGroundNode(Vector2Int worldCoordinates, Direction dir)
        {
            return GetGroundNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoordinates, dir));
        }
        public GroundNode GetAdjacentGroundNode(BlockmapNode node, Direction dir)
        {
            return GetAdjacentGroundNode(node.WorldCoordinates, dir);
        }
        public WaterNode GetAdjacentWaterNode(Vector2Int worldCoordinates, Direction dir)
        {
            return GetWaterNode(HelperFunctions.GetWorldCoordinatesInDirection(worldCoordinates, dir));
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
        /// If present, returns the climbable (cliff, ladder, fence, wall) on the given side of a specific cell.
        /// </summary>
        public IClimbable GetClimbable(Vector3Int globalCellCoordinates, Direction side)
        {
            Vector2Int worldCoordinates = new Vector2Int(globalCellCoordinates.x, globalCellCoordinates.z);
            int altitude = globalCellCoordinates.y;
            Vector2Int adjacentCoordinates = HelperFunctions.GetWorldCoordinatesInDirection(worldCoordinates, side);
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

        public float GetWorldHeight(float heightValue)
        {
            return heightValue * TILE_HEIGHT;
        }

        /// <summary>
        /// Returns the exact world height (y-coordinate) of the given node at the given position.
        /// <br/> Only works when the node mesh is drawn.
        /// </summary>
        public float GetWorldHeightAt(Vector2 worldPosition2d, BlockmapNode node)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(worldPosition2d.x, 20f, worldPosition2d.y), -Vector3.up, 1000f);
            foreach (RaycastHit hit in hits)
            {
                Transform objectHit = hit.transform;

                // We hit the ground mesh we are looking for
                if(node.Type == NodeType.Ground && objectHit.gameObject.layer == Layer_GroundNode)
                {
                    Vector3 hitPosition = hit.point;
                    return Mathf.Max(hitPosition.y, node.BaseWorldHeight);
                }

                // We hit the air node mesh of the level we are looking for
                if (node.Type == NodeType.Air && objectHit.gameObject.layer == Layer_AirNode && objectHit.GetComponent<AirNodeMesh>().Altitude == node.BaseAltitude)
                {
                    Vector3 hitPosition = hit.point;
                    return hitPosition.y;
                }

                // We hit the water mesh we are looking for
                if(node.Type == NodeType.Water && objectHit.gameObject.layer == Layer_Water)
                {
                    Vector3 hitPosition = hit.point;
                    return hitPosition.y;
                }
            }

            return node.BaseWorldHeight; // fallback

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
        public bool IsAbove(Dictionary<Direction, int> topHeight, Dictionary<Direction, int> botHeight) // returns true if at least one corner of topHeight is above botHeight and none of them are below botHeight
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

        /// <summary>
        /// Loads all data and fully initializes a new world from the data. Generates a full navmesh.
        /// <br/> Wait for IsInitialized = true before doing stuff.
        /// </summary>
        public static World Load(WorldData data, WorldEntityLibrary entityLibrary)
        {
            GameObject worldObject = new GameObject(data.Name);
            World world = worldObject.AddComponent<World>();
            world.FullInit(data, entityLibrary);
            return world;
        }
        /// <summary>
        /// Only loads and draws nodes and actors. Does not generate navmesh.
        /// </summary>
        public static World SimpleLoad(WorldData data)
        {
            GameObject worldObject = new GameObject(data.Name);
            World world = worldObject.AddComponent<World>();
            world.SimpleInit(data);
            return world;
        }

        public WorldData Save()
        {
            return new WorldData
            {
                Name = Name,
                ChunkSize = ChunkSize,
                MaxNodeId = NodeIdCounter,
                MaxEntityId = EntityIdCounter,
                MaxWaterBodyId = WaterBodyIdCounter,
                MaxActorId = ActorIdCounter,
                MaxZoneId = ZoneIdCounter,
                MaxFenceId = FenceIdCounter,
                MaxWallId = WallIdCounter,
                Chunks = Chunks.Values.Select(x => x.Save()).ToList(),
                Actors = Actors.Values.Select(x => x.Save()).ToList(),
                Entities = Entities.Values.Select(x => x.Save()).ToList(),
                WaterBodies = WaterBodies.Values.Select(x => x.Save()).ToList(),
                Fences = Fences.Values.Select(x => x.Save()).ToList(),
                Zones = Zones.Values.Select(x => x.Save()).ToList(),
                Walls = Walls.Values.Select(x => x.Save()).ToList(),
            };
        }

        #endregion
    }
}
