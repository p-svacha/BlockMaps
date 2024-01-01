using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Object representing one world with its own node/pathfinding system. One world is a closed system.
    /// <br/> A world is made up of different chunks.
    /// </summary>
    public class World : MonoBehaviour
    {
        /// <summary>
        /// Maximum y coordiante a tile can have.
        /// </summary>
        public const int MAX_HEIGHT = 30;

        /// <summary>
        /// Physical height (y) of a tile.
        /// </summary>
        public const float TILE_HEIGHT = 0.5f;

        /// <summary>
        /// How much the colors/textures of adjacent surface tiles flow into each other (0 - 0.5).
        /// </summary>
        public const float SURFACE_TILE_BLENDING = 0.4f;

        /// <summary>
        /// How much height of a tile water covers.
        /// </summary>
        public const float WATER_HEIGHT = 0.9f;

        // Indices for node corners
        private const int SW = 0;
        private const int SE = 1;
        private const int NE = 2;
        private const int NW = 3;

        public string Name { get; private set; }
        private int InitializeStep; // Some initialization steps need to happen frames after others, this is to keep count
        private bool IsInitialized;
        public int ChunkSize { get; private set; }
        public EntityLibrary EntityLibrary { get; private set; }


        // Database
        private Dictionary<int, BlockmapNode> Nodes = new Dictionary<int, BlockmapNode>();
        public Dictionary<Vector2Int, Chunk> Chunks = new Dictionary<Vector2Int, Chunk>();
        public Dictionary<int, Player> Players = new Dictionary<int, Player>();
        public List<Entity> Entities = new List<Entity>();
        public Dictionary<int, WaterBody> WaterBodies = new Dictionary<int, WaterBody>();

        private int NodeIdCounter;
        private int EntityIdCounter;
        private int WaterBodyIdCounter;
        
        private BlockmapCamera Camera;
        /// <summary>
        /// Neutral passive player
        /// </summary>
        public Player Gaia { get; private set; }
        /// <summary>
        /// The player that the vision is drawn for currently.
        /// <br/> If null everything is drawn.
        /// </summary>
        public Player ActiveVisionPlayer { get; private set; }

        // Layers
        public int Layer_SurfaceNode;
        public int Layer_Entity;
        public int Layer_AirNode;
        public int Layer_Water;

        // Attributes regarding current cursor position
        public bool IsHoveringWorld { get; private set; }
        public Vector2Int HoveredWorldCoordinates { get; private set; }       
        public BlockmapNode HoveredNode { get; private set; }
        public SurfaceNode HoveredSurfaceNode { get; private set; }
        public Entity HoveredEntity { get; private set; }
        public Chunk HoveredChunk { get; private set; }
        public WaterBody HoveredWaterBody { get; private set; }

        /// <summary>
        /// What area of the node is currently being hovered.
        /// <br/> The returned value is the direction of the edge/corner that is being hovered.
        /// <br/> Hovering the center part of a node will return Direction.None.
        /// </summary>
        public Direction NodeHoverMode { get; private set; }
        private float HoverEdgeSensitivity = 0.3f; // sensitivity for NodeHoverMode

        public event System.Action<BlockmapNode, BlockmapNode> OnHoveredNodeChanged;
        public event System.Action<SurfaceNode, SurfaceNode> OnHoveredSurfaceNodeChanged;
        public event System.Action<Chunk, Chunk> OnHoveredChunkChanged;
        public event System.Action<Entity, Entity> OnHoveredEntityChanged;

        // Draw modes
        public bool IsAllVisible => ActiveVisionPlayer == null;
        private bool IsShowingGrid;
        private bool IsShowingPathfindingGraph;
        private bool IsShowingTextures;
        private bool IsShowingTileBlending;
        

        public void Init(WorldData data, EntityLibrary entityLibrary)
        {
            EntityLibrary = entityLibrary;

            Layer_SurfaceNode = LayerMask.NameToLayer("Terrain");
            Layer_Entity = LayerMask.NameToLayer("Entity");
            Layer_AirNode = LayerMask.NameToLayer("Path");
            Layer_Water = LayerMask.NameToLayer("Water");

            // Init nodes
            Name = data.Name;
            ChunkSize = data.ChunkSize;
            foreach (ChunkData chunkData in data.Chunks)
            {
                Chunk chunk = Chunk.Load(this, chunkData);
                Chunks.Add(new Vector2Int(chunkData.ChunkCoordinateX, chunkData.ChunkCoordinateY), chunk);
            }
            NodeIdCounter = data.MaxNodeId + 1;
            EntityIdCounter = data.MaxEntityId + 1;
            WaterBodyIdCounter = data.MaxWaterBodyId + 1;

            // Init players
            foreach (PlayerData playerData in data.Players) AddPlayer(Player.Load(this, playerData));

            if (!Players.ContainsKey(-1)) AddPlayer(new Player(this, -1, "Gaia"));
            Gaia = Players[-1];

            // Init pathfinder
            Pathfinder.Init(this);

            // Generate initial navmesh so we have node connections that we need for entity node occupation
            GenerateFullNavmesh();

            // Init entities
            foreach (EntityData entityData in data.Entities) Entities.Add(Entity.Load(this, entityData));

            // Init water bodies
            foreach (WaterBodyData waterData in data.WaterBodies)
            {
                WaterBody water = WaterBody.Load(this, waterData);
                WaterBodies.Add(waterData.Id, water);
            }

            // Init camera
            Camera = GameObject.Find("Main Camera").GetComponent<BlockmapCamera>();
            Camera.SetPosition(new Vector2(ChunkSize * 0.5f, ChunkSize * 0.5f));
            Camera.SetZoom(10f);
            Camera.SetAngle(225);

            InitializeStep = 1;
        }

        #region Update

        void Update()
        {
            // Check if world is done initializing
            UpdateInitialization();
            if (!IsInitialized) return;

            // Regular updates
            UpdateHoveredObjects();
            foreach (Entity e in Entities) e.UpdateEntity();
        }

        private void UpdateInitialization()
        {
            if (IsInitialized) return;

            // Frame 1 after initialization: Readjust entities based on drawn terrain.
            if(InitializeStep == 1)
            {
                foreach (Entity e in Entities) e.SetToCurrentWorldPosition();

                InitializeStep++;
                return;
            }

            // Frame 1 after initialization: Calculate visibility of all entities.
            if (InitializeStep == 2)
            {
                foreach (Entity e in Entities) e.UpdateVisibleNodes();

                InitializeStep++;
                return;
            }

            // When all post-initialization steps are done, regenerate navmesh and we're good to go
            if (InitializeStep == 3)
            {
                GenerateFullNavmesh();
                IsInitialized = true;
            }
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

            SurfaceNode oldHoveredSurfaceNode = HoveredSurfaceNode;
            SurfaceNode newHoveredSurfaceNode = null;

            BlockmapNode oldHoveredNode = HoveredNode;
            BlockmapNode newHoveredNode = null;

            Entity oldHoveredEntity = HoveredEntity;
            Entity newHoveredEntity = null;

            WaterBody oldHoveredWaterBody = HoveredWaterBody;
            WaterBody newHoveredWaterBody = null;

            // Shoot a raycast on surface and air layer to detect hovered nodes
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_SurfaceNode | 1 << Layer_AirNode))
            {
                Transform objectHit = hit.transform;

                Vector3 hitPosition = hit.point;
                IsHoveringWorld = true;

                NodeHoverMode = GetNodeHoverMode(hitPosition);
                HoveredWorldCoordinates = GetWorldCoordinates(hitPosition);

                // Update chunk
                newHoveredChunk = objectHit.GetComponentInParent<Chunk>();

                if (objectHit.gameObject.layer == Layer_SurfaceNode) // Hit a surface node
                {
                    newHoveredSurfaceNode = GetSurfaceNode(HoveredWorldCoordinates);
                    newHoveredNode = GetSurfaceNode(HoveredWorldCoordinates);
                }
                else if (objectHit.gameObject.layer == Layer_AirNode) // Hit an air node
                {
                    // Current bug: this only works from 2 sides when hovering the edge of an air node
                    newHoveredNode = GetAirNodes(HoveredWorldCoordinates).FirstOrDefault(x => x.BaseHeight == objectHit.GetComponent<AirNodeMesh>().HeightLevel);

                    if (newHoveredNode == null) // If we are exactly on a north or east edge we have to adjust the hit position slightly, else we are 1 coordinate off and don't find anything
                    {
                        Vector3 offsetHitPosition = hitPosition + new Vector3(-0.001f, 0f, -0.001f);
                        Vector2Int offsetCoordinates = GetWorldCoordinates(offsetHitPosition);
                        newHoveredNode = GetAirNodes(offsetCoordinates).FirstOrDefault(x => x.BaseHeight == objectHit.GetComponent<AirNodeMesh>().HeightLevel);
                    }
                }
                else if(objectHit.gameObject.layer == Layer_Entity)
                {
                    newHoveredEntity = objectHit.GetComponent<Entity>(); 
                }

            }

            // Ray to detect entity
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_SurfaceNode | 1 << Layer_AirNode | 1 << Layer_Entity))
            {
                if (hit.transform.gameObject.layer == Layer_Entity)
                {
                    Transform objectHit = hit.transform;
                    newHoveredEntity = hit.transform.GetComponent<Entity>();
                }
            }

            // Ray to detect water body
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_SurfaceNode | 1 << Layer_AirNode | 1 << Layer_Water))
            {
                if (hit.transform.gameObject.layer == Layer_Water)
                {
                    Vector3 hitPosition = hit.point;
                    Vector2Int hitWorldCoordinates = GetWorldCoordinates(hitPosition);
                    WaterNode hitWaterNode = GetWaterNode(hitWorldCoordinates);
                    if (hitWaterNode != null) newHoveredWaterBody = hitWaterNode.WaterBody;
                }
            }

            // Update currently hovered objects
            HoveredNode = newHoveredNode;
            HoveredSurfaceNode = newHoveredSurfaceNode;
            HoveredChunk = newHoveredChunk;
            HoveredEntity = newHoveredEntity;
            HoveredWaterBody = newHoveredWaterBody;

            // Fire update events
            if (newHoveredNode != oldHoveredNode) OnHoveredNodeChanged?.Invoke(oldHoveredNode, newHoveredNode);
            if (newHoveredSurfaceNode != oldHoveredSurfaceNode) OnHoveredSurfaceNodeChanged?.Invoke(oldHoveredSurfaceNode, newHoveredSurfaceNode);
            if (newHoveredChunk != oldHoveredChunk) OnHoveredChunkChanged?.Invoke(oldHoveredChunk, newHoveredChunk);
            if (newHoveredEntity != oldHoveredEntity) OnHoveredEntityChanged?.Invoke(oldHoveredEntity, newHoveredEntity);
        }
        private Direction GetNodeHoverMode(Vector3 worldPos)
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

        #endregion

        #region Actions

        public void RegisterNode(BlockmapNode node)
        {
            Nodes.Add(node.Id, node); // Global registry

            // Chunk registry
            node.Chunk.Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(node);
            if (node is SurfaceNode surfaceNode) node.Chunk.SurfaceNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = surfaceNode;
            else if (node is WaterNode waterNode) node.Chunk.WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = waterNode;
            else node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(node);
        }
        private void DeregisterNode(BlockmapNode node)
        {
            Nodes.Remove(node.Id); // Global registry

            // Chunk registry
            node.Chunk.Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(node);
            if (node is SurfaceNode surfaceNode) node.Chunk.SurfaceNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if (node is WaterNode waterNode) node.Chunk.WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(node);
        }

        public void AddPlayer(Player player) => Players.Add(player.Id, player);

        private void GenerateFullNavmesh()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.UpdatePathfindingGraphStraight();
            foreach (Chunk chunk in Chunks.Values) chunk.UpdatePathfindingGraphDiagonal();
            UpdatePathfindingVisualization();
        }
        public void UpdateNavmesh(Vector2Int worldCoordinates, int rangeX = 1, int rangeY = 1) // RangeX is in Direction E, RangeY in Direction N
        {
            int redrawRadius = 1;
            for (int y = worldCoordinates.y - redrawRadius; y <= worldCoordinates.y + redrawRadius + rangeY; y++)
            {
                for (int x = worldCoordinates.x - redrawRadius; x <= worldCoordinates.x + redrawRadius + rangeX; x++)
                {
                    Vector2Int coordinates = new Vector2Int(x, y);
                    if (!IsInWorld(coordinates)) continue;

                    List<BlockmapNode> nodes = GetNodes(coordinates);
                    foreach (BlockmapNode node in nodes) node.UpdateConnectedNodesStraight();
                }
            }

            for (int y = worldCoordinates.y - redrawRadius; y <= worldCoordinates.y + redrawRadius + rangeY; y++)
            {
                for (int x = worldCoordinates.x - redrawRadius; x <= worldCoordinates.x + redrawRadius + rangeX; x++)
                {
                    Vector2Int coordinates = new Vector2Int(x, y);
                    if (!IsInWorld(coordinates)) continue;
                    List<BlockmapNode> nodes = GetNodes(coordinates);
                    foreach (BlockmapNode node in nodes) node.UpdateConnectedNodesDiagonal();
                }
            }

            UpdatePathfindingVisualization();
        }

        public bool CanChangeHeight(SurfaceNode node, Direction mode, bool isIncrease)
        {
            return node.CanChangeHeight(mode, isIncrease);
        }
        public void ChangeHeight(SurfaceNode node, Direction mode, bool isIncrease)
        {
            node.ChangeHeight(mode, isIncrease);

            UpdateNavmesh(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntities(node.GetCenterWorldPosition()); // might not work because rays are shot before new mesh is drawn sometimes
        }

        public bool CanBuildSurfacePath(SurfaceNode node)
        {
            if (node == null) return false;
            if (!(node.Shape == "0000" || node.Shape == "1001" || node.Shape == "1100" || node.Shape == "0110" || node.Shape == "0011")) return false;
            if (node.HasPath) return false;

            BlockmapNode nodeAbovePath = Pathfinder.TryGetPathNode(node.WorldCoordinates, node.BaseHeight + 1);
            if (nodeAbovePath != null && !Pathfinder.CanNodesBeAboveEachOther(node.Shape, nodeAbovePath.Shape)) return false;

            return true;
        }
        public void BuildSurfacePath(SurfaceNode node, Surface pathSurface)
        {
            node.BuildPath(pathSurface);
            RedrawNodesAround(node.WorldCoordinates);
            UpdatePathfindingVisualization();
        }

        public bool CanBuildAirPath(Vector2Int worldCoordinates, int height)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            SurfaceNode surfaceNode = chunk.GetSurfaceNode(localCoordinates);

            // Check if an entity below is blocking this space
            List<BlockmapNode> belowNodes = GetNodes(worldCoordinates, 0, height);
            foreach (BlockmapNode node in belowNodes)
                foreach (Entity e in node.Entities)
                    if (node.MaxHeight + e.Dimensions.y > height)
                        return false;

            // Check if underwater
            WaterNode water = GetWaterNode(worldCoordinates);
            if (water != null && water.WaterBody.ShoreHeight > height) return false;


            if (Pathfinder.TryGetPathNode(worldCoordinates, height) != null) return false; // Can't build when path node on same level
            BlockmapNode pathNodeBelow = Pathfinder.TryGetPathNode(worldCoordinates, height - 1);
            if (pathNodeBelow != null && pathNodeBelow.Type == NodeType.AirPathSlope) return false; // Can't build with slope underneath

            if (surfaceNode.Shape == "0000") return height > surfaceNode.Height[0];
            if (surfaceNode.HasPath || surfaceNode.Shape == "1110" || surfaceNode.Shape == "1101" || surfaceNode.Shape == "1011" || surfaceNode.Shape == "0111") return height > surfaceNode.Height.Max(x => x);
            else return surfaceNode.Height.All(x => height >= x);
        }
        public void BuildAirPath(Vector2Int worldCoordinates, int height)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);

            AirPathNode newNode = new AirPathNode(this, chunk, NodeIdCounter++, localCoordinates, new int[] { height, height, height, height }, SurfaceId.Tarmac);
            RegisterNode(newNode);

            UpdateNavmesh(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
            UpdateVisionOfNearbyEntities(newNode.GetCenterWorldPosition());
        }

        public bool CanBuildAirSlope(Vector2Int worldCoordinates, int height, Direction dir)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            SurfaceNode surfaceNode = chunk.GetSurfaceNode(localCoordinates);

            // Check if an entity below is blocking this space
            List<BlockmapNode> belowNodes = GetNodes(worldCoordinates, 0, height);
            foreach (BlockmapNode node in belowNodes)
                foreach (Entity e in node.Entities)
                    if (node.MaxHeight + e.Dimensions.y >= height)
                        return false;

            // Check if underwater
            WaterNode water = GetWaterNode(worldCoordinates);
            if (water != null && water.WaterBody.ShoreHeight > height) return false;

            if (Pathfinder.TryGetPathNode(worldCoordinates, height) != null) return false; // Can't build when path node on same level
            BlockmapNode pathNodeBelow = Pathfinder.TryGetPathNode(worldCoordinates, height - 1);
            if (pathNodeBelow != null && !Pathfinder.CanNodesBeAboveEachOther(pathNodeBelow.Shape, AirPathSlopeNode.GetShapeFromDirection(dir))) return false;

            if (surfaceNode.HasPath) return height > surfaceNode.Height.Max(x => x);
            else return surfaceNode.Height.All(x => height >= x);
        }
        public void BuildAirSlope(Vector2Int worldCoordinates, int height, Direction dir)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);

            AirPathSlopeNode newNode = new AirPathSlopeNode(this, chunk, NodeIdCounter++, localCoordinates, AirPathSlopeNode.GetHeightsFromDirection(height, dir), SurfaceId.Tarmac);
            RegisterNode(newNode);

            UpdateNavmesh(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
            UpdateVisionOfNearbyEntities(newNode.GetCenterWorldPosition());
        }

        public void SetSurface(SurfaceNode node, SurfaceId surface)
        {
            node.SetSurface(surface);
            RedrawNodesAround(node.WorldCoordinates);
        }

        public bool CanPlaceEntity(StaticEntity entity, BlockmapNode node)
        {
            // Check if terrain below entity is fully connected (as in is the surface below big enough to support the whole footprint of the entity)
            if (!entity.CanBePlacedOn(node)) return false;

            // Check if underwater - todo: fix 
            if (node is SurfaceNode surfaceNode && surfaceNode.WaterNode != null && surfaceNode.GetCenterWorldPosition().y < surfaceNode.WaterNode.WaterBody.WaterSurfaceWorldHeight)
                return false;

            int minHeight = GetNodeHeight(entity.GetWorldPosition(this, node).y);
            List<BlockmapNode> occupiedNodes = entity.GetOccupiedNodes(node); // get nodes that would be occupied when placing the entity on the given node
            foreach (BlockmapNode occupiedNode in occupiedNodes)
            {
                // Check if any occupied nodes exist above entity that would block space
                List<BlockmapNode> aboveNodes = GetNodes(occupiedNode.WorldCoordinates, minHeight + 1, minHeight + entity.Dimensions.y);
                if (aboveNodes.Where(x => !occupiedNodes.Contains(x)).Count() > 0) return false;

                // Check if any occupied nodes already have an entity
                if (occupiedNode.Entities.Count > 0) return false;

                // Check if any occupied node is not flat while requiring flat terrain
                if (entity.RequiresFlatTerrain && !occupiedNode.IsFlat) return false;
            }

            return true;
        }
        public void SpawnEntity(Entity newEntity, BlockmapNode node, Player player)
        {
            newEntity.Init(EntityIdCounter++, this, node, player);

            // Update if the new entity is currently visible
            newEntity.UpdateVisiblity(ActiveVisionPlayer);

            // Update vision of all other entities near this new entity
            UpdateVisionOfNearbyEntities(newEntity.GetWorldCenter());

            // Register new entity
            Entities.Add(newEntity);

            // Update pathfinding navmesh
            UpdateNavmesh(node.WorldCoordinates, newEntity.Dimensions.x, newEntity.Dimensions.z);
        }
        public void RemoveEntity(Entity entity)
        {
            // De-register entity
            Entities.Remove(entity);

            // Remove seen by on all nodes
            foreach (BlockmapNode node in entity.VisibleNodes) node.RemoveVisionBy(entity);

            // Remove node & chunk occupation
            foreach (BlockmapNode node in entity.OccupiedNodes)
            {
                node.RemoveEntity(entity);
                node.Chunk.RemoveEntity(entity);
            }

            // Update pathfinding navmesh
            UpdateNavmesh(entity.OriginNode.WorldCoordinates, entity.Dimensions.x, entity.Dimensions.z);

            if (entity.Player == ActiveVisionPlayer) UpdateVisibility();

            // Destroy
            GameObject.Destroy(entity.gameObject);

            // Update vision of all other entities near the entity (doesn't work instantly bcuz destroying takes too long)
            UpdateVisionOfNearbyEntities(entity.GetWorldCenter());
        }

        public WaterBody CanAddWater(SurfaceNode node, int maxDepth) // returns null when cannot
        {
            int shoreHeight = node.BaseHeight + 1;
            WaterBody waterBody = new WaterBody(); // dummy water body to store data that can later be used to create a real water body
            waterBody.ShoreHeight = shoreHeight;
            waterBody.CoveredNodes = new List<SurfaceNode>();

            List<System.Tuple<SurfaceNode, Direction>> checkedNodes = new List<System.Tuple<SurfaceNode, Direction>>(); // nodes that were already checked for water expansion in one direction
            List<System.Tuple<SurfaceNode, Direction>> expansionNodes = new List<System.Tuple<SurfaceNode, Direction>>(); // nodes that need to be checked for water expansion and in what direction
            expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(node, Direction.None));

            // Check the following:
            // > If the node goes deeper than maxDepth, return false
            // > If the node is below shoreDepth (meaning also underwater), mark it to check
            // > If the node is above shoreDepth, do nothing
            while (expansionNodes.Count > 0)
            {
                System.Tuple<SurfaceNode, Direction> check = expansionNodes[0];
                SurfaceNode checkNode = check.Item1;
                Direction checkDir = check.Item2;
                expansionNodes.RemoveAt(0);

                if (checkNode == null) continue;
                if (waterBody.CoveredNodes.Contains(checkNode)) continue;
                if (checkedNodes.Contains(check)) continue;

                checkedNodes.Add(check);

                bool isTooDeep = checkNode.BaseHeight < shoreHeight - maxDepth;
                if (isTooDeep) return null; // too deep
                if (checkNode.WaterNode != null) return null; // already has water here

                bool isUnderwater = (
                    ((checkDir == Direction.None || checkDir == Direction.N) && (checkNode.Height[NW] < shoreHeight || checkNode.Height[NE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.E) && (checkNode.Height[NE] < shoreHeight || checkNode.Height[SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.S) && (checkNode.Height[SW] < shoreHeight || checkNode.Height[SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.W) && (checkNode.Height[SW] < shoreHeight || checkNode.Height[NW] < shoreHeight))
                    );
                if (isUnderwater) // underwater
                {
                    // Check if we're drowing entities
                    if (GetEntities(checkNode.WorldCoordinates, shoreHeight - maxDepth, shoreHeight - 1).Count > 0) return null;

                    // Check if we're drowing air nodes
                    if (GetAirNodes(checkNode.WorldCoordinates, shoreHeight - maxDepth, shoreHeight - 1).Count > 0) return null;

                    waterBody.CoveredNodes.Add(checkNode);

                    if (checkNode.Height[NW] < shoreHeight || checkNode.Height[NE] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.N), Direction.S));
                    if (checkNode.Height[NE] < shoreHeight || checkNode.Height[SE] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.E), Direction.W));
                    if (checkNode.Height[SW] < shoreHeight || checkNode.Height[SE] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.S), Direction.N));
                    if (checkNode.Height[NW] < shoreHeight || checkNode.Height[SW] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.W), Direction.E));
                }
                else { } // above water
            }

            return waterBody;
        }
        public void AddWaterBody(WaterBody data)
        {
            // Create a new water nodes for each covered surface node
            List<WaterNode> waterNodes = new List<WaterNode>();
            foreach (SurfaceNode node in data.CoveredNodes)
            {
                WaterNode waterNode = new WaterNode(this, node.Chunk, NodeIdCounter++, node.LocalCoordinates, new int[] { data.ShoreHeight, data.ShoreHeight, data.ShoreHeight, data.ShoreHeight }, SurfaceId.Water);
                waterNodes.Add(waterNode);
                RegisterNode(waterNode);
            }

            // Make a new water body instance with a unique id
            WaterBody newWaterBody = new WaterBody(WaterBodyIdCounter++, data.ShoreHeight, waterNodes, data.CoveredNodes);

            // Get chunks that will have nodes covered in new water body
            HashSet<Chunk> affectedChunks = new HashSet<Chunk>();
            foreach (BlockmapNode node in newWaterBody.CoveredNodes) affectedChunks.Add(node.Chunk);

            // Update navmesh
            UpdateNavmesh(new Vector2Int(newWaterBody.MinWorldX, newWaterBody.MinWorldY), newWaterBody.MaxWorldX - newWaterBody.MinWorldX, newWaterBody.MaxWorldY - newWaterBody.MinWorldY);

            // Redraw affected chunks
            foreach (Chunk c in affectedChunks) RedrawChunk(c);

            // Register water bofy
            WaterBodies.Add(newWaterBody.Id, newWaterBody);
        }
        public void RemoveWaterBody(WaterBody water)
        {
            // De-register
            WaterBodies.Remove(water.Id);

            // Get chunks that will had nodes covered in water body
            HashSet<Chunk> affectedChunks = new HashSet<Chunk>();
            foreach (SurfaceNode node in water.CoveredNodes) affectedChunks.Add(node.Chunk);

            // Remove water node reference from all covered surface nodes
            foreach (SurfaceNode node in water.CoveredNodes) node.SetWaterNode(null);

            // Deregister deleted water nodes
            foreach (WaterNode node in water.WaterNodes) DeregisterNode(node);

            // Update navmesh
            UpdateNavmesh(new Vector2Int(water.MinWorldX, water.MinWorldY), water.MaxWorldX - water.MinWorldX, water.MaxWorldY - water.MinWorldY);

            // Redraw affected chunks
            foreach (Chunk c in affectedChunks) RedrawChunk(c);
        }


        #endregion

        #region Draw

        /// <summary>
        /// Generates all meshes of the world.
        /// </summary>
        public void Draw()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.DrawMesh();

            SetActiveVisionPlayer(null);

            UpdateGridOverlay();
            UpdatePathfindingVisualization();
            UpdateTextureMode();
            UpdateTileBlending();
        }

        /// <summary>
        /// Redraws all chunks around the given coordinates.
        /// </summary>
        public void RedrawNodesAround(Vector2Int worldCoordinates)
        {
            List<Chunk> affectedChunks = new List<Chunk>();

            int range = 1;
            for (int y = worldCoordinates.y - range; y <= worldCoordinates.y + range; y++)
            {
                for (int x = worldCoordinates.x - range; x <= worldCoordinates.x + range; x++)
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
        private void RedrawChunk(Chunk chunk)
        {
            chunk.DrawMesh();
            chunk.SetVisibility(ActiveVisionPlayer);
            chunk.ShowGrid(IsShowingGrid);
            chunk.ShowTextures(IsShowingTextures);
            chunk.ShowTileBlending(IsShowingTileBlending);
        }

        /// <summary>
        /// Updates the visibility for the full map according to the current player vision.
        /// </summary>
        public void UpdateVisibility()
        {
            foreach (Chunk c in Chunks.Values) UpdateVisibility(c);
        }
        /// <summary>
        /// Updates the visibility display for one chunk according to the current player vision.
        /// </summary>
        public void UpdateVisibility(Chunk c)
        {
            c.SetVisibility(ActiveVisionPlayer);
        }

        /// <summary>
        /// Gets called when the visibility of a node changes on the specified chunk for the specified player.
        /// </summary>
        public void OnVisibilityChanged(Chunk c, Player player)
        {
            if (player == ActiveVisionPlayer) UpdateVisibility(c);
        }

        /// <summary>
        /// Recalculates the vision of all entities that have the given position within their vision range.
        /// </summary>
        private void UpdateVisionOfNearbyEntities(Vector3 position)
        {
            foreach (Entity e in Entities.Where(x => Vector3.Distance(x.GetWorldCenter(), position) <= x.VisionRange))
                e.UpdateVisibleNodes();
        }

        public void ToggleGridOverlay()
        {
            IsShowingGrid = !IsShowingGrid;
            UpdateGridOverlay();
        }
        private void UpdateGridOverlay()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowGrid(IsShowingGrid);
        }

        public void TogglePathfindingVisualization()
        {
            IsShowingPathfindingGraph = !IsShowingPathfindingGraph;
            UpdatePathfindingVisualization();
        }
        private void UpdatePathfindingVisualization()
        {
            if (IsShowingPathfindingGraph) NavmeshVisualizer.Singleton.Visualize(this);
            else NavmeshVisualizer.Singleton.ClearVisualization();
        }

        public void ToggleTextureMode()
        {
            IsShowingTextures = !IsShowingTextures;
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
        private void UpdateTileBlending()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowTileBlending(IsShowingTileBlending);
        }

        public void SetActiveVisionPlayer(Player player)
        {
            ActiveVisionPlayer = player;
            UpdateVisibility();
        }

        #endregion

        #region Getters

        /// <summary>
        /// Returns if the given world coordinates exist in this world.
        /// </summary>
        public bool IsInWorld(Vector2Int worldCoordinates)
        {
            return GetChunk(worldCoordinates) != null;
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

            if (Chunks.TryGetValue(chunkCoordinates, out Chunk value)) return value;
            else return null;
        }

        public BlockmapNode GetNode(int id) => Nodes[id];

        public List<BlockmapNode> GetNodes(Vector2Int worldCoordinates, int minHeight, int maxHeight)
        {
            return GetNodes(worldCoordinates).Where(x => x.BaseHeight >= minHeight && x.BaseHeight <= maxHeight).ToList();
        }
        public List<BlockmapNode> GetNodes(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return new List<BlockmapNode>();

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetNodes(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public SurfaceNode GetSurfaceNode(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetSurfaceNode(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public WaterNode GetWaterNode(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetWaterNode(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public List<BlockmapNode> GetAirNodes(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return new List<BlockmapNode>();

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetAirNodes(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public List<BlockmapNode> GetAirNodes(Vector2Int worldCoordinates, int minHeight, int maxHeight)
        {
            return GetAirNodes(worldCoordinates).Where(x => x.BaseHeight >= minHeight && x.BaseHeight <= maxHeight).ToList();
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
            Vector2Int targetCoordinates = GetWorldCoordinatesInDirection(worldCoordinates, dir);
            return GetNodes(targetCoordinates);
        }
        public SurfaceNode GetAdjacentSurfaceNode(Vector2Int worldCoordinates, Direction dir)
        {
            return GetSurfaceNode(GetWorldCoordinatesInDirection(worldCoordinates, dir));
        }
        public SurfaceNode GetAdjacentSurfaceNode(BlockmapNode node, Direction dir)
        {
            return GetAdjacentSurfaceNode(node.WorldCoordinates, dir);
        }
        public List<BlockmapNode> GetAdjacentPathNodes(Vector2Int worldCoordinates, Direction dir)
        {
            return GetAirNodes(GetWorldCoordinatesInDirection(worldCoordinates, dir));
        }

        public List<Entity> GetEntities(Vector2Int worldCoordinates, int minHeight, int maxHeight)
        {
            List<Entity> entities = new List<Entity>();
            foreach (BlockmapNode node in GetNodes(worldCoordinates))
                foreach (Entity e in node.Entities)
                    if (e.MinHeight <= maxHeight && e.MaxHeight >= minHeight)
                        entities.Add(e);
            return entities;
        }

        public Vector2Int GetWorldCoordinatesInDirection(Vector2Int worldCoordinates, Direction dir)
        {
            if (dir == Direction.N) return (worldCoordinates + new Vector2Int(0, 1));
            if (dir == Direction.E) return (worldCoordinates + new Vector2Int(1, 0));
            if (dir == Direction.S) return (worldCoordinates + new Vector2Int(0, -1));
            if (dir == Direction.W) return (worldCoordinates + new Vector2Int(-1, 0));
            if (dir == Direction.NE) return (worldCoordinates + new Vector2Int(1, 1));
            if (dir == Direction.NW) return (worldCoordinates + new Vector2Int(-1, 1));
            if (dir == Direction.SE) return (worldCoordinates + new Vector2Int(1, -1));
            if (dir == Direction.SW) return (worldCoordinates + new Vector2Int(-1, -1));
            return worldCoordinates;
        }

        public float GetWorldHeight(float heightValue)
        {
            return heightValue * TILE_HEIGHT;
        }
        public int GetNodeHeight(float worldHeight)
        {
            return (int)(worldHeight / TILE_HEIGHT);
        }

        /// <summary>
        /// Returns the exact world height (y-coordinate) at the given relative position.
        /// <br/> Only works when that node is visible.
        /// </summary>
        public float GetWorldHeightAt(Vector2 worldPosition2d, BlockmapNode node)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(worldPosition2d.x, 20f, worldPosition2d.y), -Vector3.up, 1000f);
            foreach (RaycastHit hit in hits)
            {
                Transform objectHit = hit.transform;

                // We hit the surface mesh we are looking for
                if(node.Type == NodeType.Surface && objectHit.gameObject.layer == Layer_SurfaceNode)
                {
                    Vector3 hitPosition = hit.point;
                    return hitPosition.y;
                }

                // We hit the air node mesh of the level we are looking for
                if ((node.Type == NodeType.AirPath || node.Type == NodeType.AirPathSlope) && objectHit.gameObject.layer == Layer_AirNode && objectHit.GetComponent<AirNodeMesh>().HeightLevel == node.BaseHeight)
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

            throw new System.Exception("World height not found for node of type " + node.Type.ToString());
        }

        public SurfaceNode GetRandomSurfaceNode()
        {
            List<Chunk> candidateChunks = Chunks.Values.ToList();
            Chunk chosenChunk = candidateChunks[Random.Range(0, candidateChunks.Count)];

            int x = Random.Range(0, ChunkSize);
            int y = Random.Range(0, ChunkSize);
            return chosenChunk.GetSurfaceNode(x, y);
        }
        public BlockmapNode GetRandomPassableNode(Entity entity) // very not performant
        {
            List<BlockmapNode> candidateNodes = new List<BlockmapNode>();
            foreach (Chunk c in Chunks.Values) candidateNodes.AddRange(c.GetAllNodes().Where(x => x.IsPassable(entity)).ToList());
            return candidateNodes[Random.Range(0, candidateNodes.Count)];
        }

        #endregion

        #region Save / Load

        public static World Load(WorldData data, EntityLibrary entityLibrary)
        {
            GameObject worldObject = new GameObject(data.Name);
            World world = worldObject.AddComponent<World>();
            world.Init(data, entityLibrary);
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
                Chunks = Chunks.Values.Select(x => x.Save()).ToList(),
                Players = Players.Values.Select(x => x.Save()).ToList(),
                Entities = Entities.Select(x => x.Save()).ToList(),
                WaterBodies = WaterBodies.Values.Select(x => x.Save()).ToList()
            };
        }

        #endregion
    }
}
