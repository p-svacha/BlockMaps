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
        private WorldData WorldData;

        /// <summary>
        /// Maximum y coordiante a tile can have.
        /// </summary>
        public const int MAX_HEIGHT = 30;

        /// <summary>
        /// Physical height (y) of a tile.
        /// </summary>
        public const float TILE_HEIGHT = 0.5f;

        public const float MAP_EDGE_HEIGHT = (-1 * TILE_HEIGHT);

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
        private bool IsInitialized;
        public int ChunkSize { get; private set; }
        public WorldEntityLibrary ContentLibrary { get; private set; }


        // Database
        private Dictionary<int, BlockmapNode> Nodes = new Dictionary<int, BlockmapNode>();
        public Dictionary<Vector2Int, Chunk> Chunks = new Dictionary<Vector2Int, Chunk>();
        public Dictionary<int, Player> Players = new Dictionary<int, Player>();
        public List<Entity> Entities = new List<Entity>();
        public Dictionary<int, WaterBody> WaterBodies = new Dictionary<int, WaterBody>();
        public List<Wall> Walls = new List<Wall>();

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
        public int Layer_Wall;

        // Attributes regarding current cursor position
        public bool IsHoveringWorld { get; private set; }
        public Vector2Int HoveredWorldCoordinates { get; private set; }       
        public BlockmapNode HoveredNode { get; private set; }
        public AirNode HoveredAirNode { get; private set; }
        public SurfaceNode HoveredSurfaceNode { get; private set; }
        public Entity HoveredEntity { get; private set; }
        public Chunk HoveredChunk { get; private set; }
        public WaterBody HoveredWaterBody { get; private set; }
        public Wall HoveredWall { get; private set; }

        /// <summary>
        /// What area of the node is currently being hovered.
        /// <br/> The returned value is the direction of the edge/corner that is being hovered.
        /// <br/> Hovering the center part of a node will return Direction.None.
        /// </summary>
        public Direction NodeHoverMode { get; private set; }
        public Direction NodeSideHoverMode { get; private set; }
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

        // Variables for delayed updated
        private bool DoUpdateVisionNextFrame;
        private Vector3 VisionUpdatePosition;
        private int VisionUpdateRangeEast;
        private int VisionUpdateRangeNorth;

        #region Init

        public void Init(WorldData data, WorldEntityLibrary entityLibrary)
        {
            // Init general
            Name = data.Name;
            ChunkSize = data.ChunkSize;
            ContentLibrary = entityLibrary;
            WorldData = data;

            Layer_SurfaceNode = LayerMask.NameToLayer("Terrain");
            Layer_Entity = LayerMask.NameToLayer("Entity");
            Layer_AirNode = LayerMask.NameToLayer("Path");
            Layer_Water = LayerMask.NameToLayer("Water");
            Layer_Wall = LayerMask.NameToLayer("Wall");

            // Init camera
            Camera = GameObject.Find("Main Camera").GetComponent<BlockmapCamera>();
            Camera.SetPosition(new Vector2(ChunkSize * 0.5f, ChunkSize * 0.5f));
            Camera.SetZoom(10f);
            Camera.SetAngle(225);

            // Init pathfinder
            Pathfinder.Init(this);

            // Init players
            foreach (PlayerData playerData in data.Players) AddPlayer(Player.Load(this, playerData));

            if (!Players.ContainsKey(-1)) AddPlayer(new Player(this, -1, "Gaia"));
            Gaia = Players[-1];

            // Init nodes
            foreach (ChunkData chunkData in data.Chunks)
            {
                Chunk chunk = Chunk.Load(this, chunkData);
                Chunks.Add(new Vector2Int(chunkData.ChunkCoordinateX, chunkData.ChunkCoordinateY), chunk);
            }
            NodeIdCounter = data.MaxNodeId + 1;
            EntityIdCounter = data.MaxEntityId + 1;
            WaterBodyIdCounter = data.MaxWaterBodyId + 1;

            // Init walls
            foreach(WallData wallData in data.Walls)
            {
                Wall wall = Wall.Load(this, wallData);
            }

            // Init water bodies
            foreach (WaterBodyData waterData in data.WaterBodies)
            {
                WaterBody water = WaterBody.Load(this, waterData);
                WaterBodies.Add(waterData.Id, water);
            }

            // Draw node meshes because we need to shoot rays to generate navmesh
            DrawNodes();

            InitializeStep = 1;
        }
        private void UpdateInitialization()
        {
            if (IsInitialized) return;

            // Frame 1 after initilaization: Do stuff that requires drawn node meshes.
            if (InitializeStep == 1)
            {
                // Generate initial navmesh so we have node connections that we need for entity node occupation
                GenerateFullNavmesh();

                // Init entities
                foreach (EntityData entityData in WorldData.Entities) Entities.Add(Entity.Load(this, entityData));
                foreach (Entity e in Entities) e.SetToCurrentWorldPosition();

                InitializeStep++;
                return;
            }

            // Frame 2 after initialization: Do stuff that requires entities to be at the correct world position
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

        #endregion

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

        private void LateUpdate()
        {
            if (DoUpdateVisionNextFrame) DoUpdateVisionOfNearbyEntities();
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

            SurfaceNode oldHoveredSurfaceNode = HoveredSurfaceNode;
            SurfaceNode newHoveredSurfaceNode = null;

            AirNode oldHoveredAirNode = HoveredAirNode;
            AirNode newHoveredAirNode = null;

            Entity oldHoveredEntity = HoveredEntity;
            Entity newHoveredEntity = null;

            WaterBody oldHoveredWaterBody = HoveredWaterBody;
            WaterBody newHoveredWaterBody = null;

            Wall oldHoveredWall = HoveredWall;
            Wall newHoveredWall = null;

            // Shoot a raycast on surface and air layer to detect hovered nodes
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_SurfaceNode | 1 << Layer_AirNode | 1 << Layer_Water | 1 << Layer_Wall))
            {
                Transform objectHit = hit.transform;

                Vector3 hitPosition = hit.point;
                IsHoveringWorld = true;

                NodeHoverMode = GetNodeHoverMode(hitPosition);
                NodeSideHoverMode = GetNodeSideHoverMode(hitPosition);
                HoveredWorldCoordinates = GetWorldCoordinates(hitPosition);

                // Update chunk
                newHoveredChunk = objectHit.GetComponentInParent<Chunk>();

                // Hit surface node
                if (objectHit.gameObject.layer == Layer_SurfaceNode)
                {
                    newHoveredSurfaceNode = GetSurfaceNode(HoveredWorldCoordinates);
                    newHoveredNode = newHoveredSurfaceNode;
                }

                // Hit air node
                else if (objectHit.gameObject.layer == Layer_AirNode)
                {
                    newHoveredAirNode = GetAirNodes(HoveredWorldCoordinates).FirstOrDefault(x => x.BaseHeight == objectHit.GetComponent<AirNodeMesh>().HeightLevel);

                    if (newHoveredAirNode == null) // If we are exactly on a north or east edge we have to adjust the hit position slightly, else we are 1 coordinate off and don't find anything
                    {
                        Vector3 offsetHitPosition = hitPosition + new Vector3(-0.001f, 0f, -0.001f);
                        Vector2Int offsetCoordinates = GetWorldCoordinates(offsetHitPosition);

                        newHoveredAirNode = GetAirNodes(offsetCoordinates).FirstOrDefault(x => x.BaseHeight == objectHit.GetComponent<AirNodeMesh>().HeightLevel);
                        
                    }

                    newHoveredNode = newHoveredAirNode;

                    if (newHoveredAirNode != null) HoveredWorldCoordinates = newHoveredAirNode.WorldCoordinates;
                }

                // Hit water node
                else if (objectHit.gameObject.layer == Layer_Water)
                {
                    WaterNode hitWaterNode = GetWaterNode(HoveredWorldCoordinates);

                    if (hitWaterNode != null)
                    {
                        if (hitWaterNode.SurfaceNode.IsCenterUnderWater)
                        {
                            newHoveredNode = hitWaterNode;
                            newHoveredWaterBody = hitWaterNode.WaterBody;
                        }
                        else newHoveredNode = hitWaterNode.SurfaceNode;

                        newHoveredSurfaceNode = hitWaterNode.SurfaceNode;
                    }
                }

                // Hit wall
                else if(objectHit.gameObject.layer == Layer_Wall)
                {
                    newHoveredWall = GetWallFromRaycastHit(hit);
                    if (newHoveredWall != null) HoveredWorldCoordinates = newHoveredWall.Node.WorldCoordinates;
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

            // Update currently hovered objects
            HoveredNode = newHoveredNode;
            HoveredSurfaceNode = newHoveredSurfaceNode;
            HoveredAirNode = newHoveredAirNode;
            HoveredChunk = newHoveredChunk;
            HoveredEntity = newHoveredEntity;
            HoveredWaterBody = newHoveredWaterBody;
            HoveredWall = newHoveredWall;

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
        private Direction GetNodeSideHoverMode(Vector3 worldPos)
        {
            Direction fullHoverMode = GetNodeHoverMode(worldPos);
            if (fullHoverMode == Direction.NW || fullHoverMode == Direction.NE || fullHoverMode == Direction.SW || fullHoverMode == Direction.SE)
                return fullHoverMode;

            Vector2 posOnTile = new Vector2(worldPos.x - (int)worldPos.x, worldPos.z - (int)worldPos.z);
            if (worldPos.x < 0) posOnTile.x++;
            if (worldPos.z < 0) posOnTile.y++;

            if(posOnTile.x > posOnTile.y)
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
        private List<Direction> GetNodeSideHoverModes(Vector3 worldPos)
        {
            List<Direction> sides = new List<Direction>();

            Direction fullHoverMode = GetNodeHoverMode(worldPos);
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

        public Wall GetWallFromRaycastHit(RaycastHit hit)
        {
            Vector2Int hitCoordinates = GetWorldCoordinates(hit.point);
            List<BlockmapNode> hitNodes = GetNodes(hitCoordinates, hit.transform.GetComponent<WallMesh>().HeightLevel).OrderByDescending(x => x.MaxHeight).ToList();
            Direction primaryHitSide = GetNodeSideHoverMode(hit.point);
            List<Direction> otherPossibleHitSides = GetNodeSideHoverModes(hit.point);

            foreach (BlockmapNode hitNode in hitNodes)
            {
                if (hitNode != null && hitNode.Walls.ContainsKey(primaryHitSide)) return hitNode.Walls[primaryHitSide];
                else
                {
                    foreach (Direction hitSide in otherPossibleHitSides)
                    {
                        if (hitNode != null && hitNode.Walls.ContainsKey(hitSide))
                        {
                            return hitNode.Walls[hitSide];
                        }
                    }
                }

                // If we are exactly on a north or east edge we have to adjust the hit position slightly, else we are 1 coordinate off and don't find anything
                // Do the same detection stuff again with the offset position
                Vector3 offsetHitPosition = hit.point + new Vector3(-0.001f, 0f, -0.001f);
                Vector2Int offsetCoordinates = GetWorldCoordinates(offsetHitPosition);
                List<BlockmapNode> offsetHitNodes = GetNodes(offsetCoordinates, hit.transform.GetComponent<WallMesh>().HeightLevel).OrderByDescending(x => x.MaxHeight).ToList();
                Direction primaryOffsetSide = GetNodeSideHoverMode(offsetHitPosition);
                List<Direction> otherPossibleOffsetSides = GetNodeSideHoverModes(offsetHitPosition);

                foreach (BlockmapNode offsetHitNode in offsetHitNodes)
                {
                    if (offsetHitNode != null && offsetHitNode.Walls.ContainsKey(primaryOffsetSide)) return offsetHitNode.Walls[primaryOffsetSide];
                    else
                    {
                        foreach (Direction hitSide in otherPossibleOffsetSides)
                        {
                            if (offsetHitNode != null && offsetHitNode.Walls.ContainsKey(hitSide))
                            {
                                return offsetHitNode.Walls[hitSide];
                            }
                        }
                    }
                }
            }

            return null;
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
            else if(node is AirNode airNode) node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(airNode);
        }
        public void DeregisterNode(BlockmapNode node)
        {
            // Walls on node
            while(node.Walls.Count > 0) DeregisterWall(node.Walls.Values.ToList()[0]);

            // Node
            Nodes.Remove(node.Id); // Global registry

            // Chunk registry
            node.Chunk.Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(node);
            if (node is SurfaceNode surfaceNode) node.Chunk.SurfaceNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if (node is WaterNode waterNode) node.Chunk.WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if(node is AirNode airNode) node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(airNode);
        }
        private void DeregisterWall(Wall wall)
        {
            BlockmapNode node = wall.Node;
            node.Walls.Remove(wall.Side);
            Walls.Remove(wall);
        }

        public void AddPlayer(Player player) => Players.Add(player.Id, player);
        public void ResetExploration(Player player)
        {
            foreach (BlockmapNode node in Nodes.Values) node.RemoveExploredBy(player);
            foreach (Entity entity in Entities.Where(x => x.Player == player)) entity.UpdateVisibleNodes();

            UpdateVisibility();
        }

        private void GenerateFullNavmesh()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ResetNavmeshConnections();
            foreach (Chunk chunk in Chunks.Values) chunk.UpdatePathfindingGraphStraight();
            foreach (Chunk chunk in Chunks.Values) chunk.UpdatePathfindingGraphDiagonal();
            UpdatePathfindingVisualization();
        }
        public void UpdateNavmesh(Vector2Int worldCoordinates, int rangeEast = 1, int rangeNorth = 1)
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

            // Update nodes
            foreach (BlockmapNode node in nodesToUpdate) node.ResetTransitions();
            foreach (BlockmapNode node in nodesToUpdate) node.SetStraightAdjacentTransitions();
            foreach (BlockmapNode node in nodesToUpdate) node.SetDiagonalAdjacentTransitions();

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
            UpdateVisionOfNearbyEntities(node.GetCenterWorldPosition());
        }

        public bool CanBuildSurfacePath(SurfaceNode node)
        {
            if (node == null) return false;
            if (!(node.IsFlat || node.IsSlope())) return false;
            if (node.HasPath) return false;
            if (node.GetFreeHeadSpace(Direction.None) <= 0) return false;

            return true;
        }
        public void BuildSurfacePath(SurfaceNode node, Surface pathSurface)
        {
            node.BuildPath(pathSurface);
            RedrawNodesAround(node.WorldCoordinates);
        }
        public void RemoveSurfacePath(SurfaceNode node)
        {
            node.RemovePath();
            RedrawNodesAround(node.WorldCoordinates);
        }

        public bool CanBuildAirPath(Vector2Int worldCoordinates, int height)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            SurfaceNode surfaceNode = chunk.GetSurfaceNode(localCoordinates);

            // Check if an entity or wall below is blocking this space
            List<BlockmapNode> belowNodes = GetNodes(worldCoordinates, 0, height);
            foreach (BlockmapNode node in belowNodes)
            {
                foreach (Entity e in node.Entities)
                    if (e.MaxHeight > height)
                        return false;

                foreach (Wall wall in node.Walls.Values)
                    if (wall.MaxHeight > height)
                        return false;
            }

            // Check if underwater
            WaterNode water = GetWaterNode(worldCoordinates);
            if (water != null && water.WaterBody.ShoreHeight > height) return false;

            // Check overlapping with existing node
            List<BlockmapNode> sameLevelNodes = GetNodes(worldCoordinates, height);
            if (sameLevelNodes.Any(x => !IsAbove(HelperFunctions.GetFlatHeights(height), x.Height))) return false;

            // Check if overlap with surface
            if (!IsAbove(HelperFunctions.GetFlatHeights(height), surfaceNode.Height)) return false;

            return true;
        }
        public void BuildAirPath(Vector2Int worldCoordinates, int height)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);

            AirNode newNode = new AirNode(this, chunk, NodeIdCounter++, localCoordinates, HelperFunctions.GetFlatHeights(height), SurfaceId.Tarmac);
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

            // Check if an entity or wall below is blocking this space
            List<BlockmapNode> belowNodes = GetNodes(worldCoordinates, 0, height);
            foreach (BlockmapNode node in belowNodes)
            {
                foreach (Entity e in node.Entities)
                    if (e.MaxHeight > height)
                        return false;

                foreach (Wall wall in node.Walls.Values)
                    if (wall.MaxHeight > height)
                        return false;
            }

            // Check if underwater
            WaterNode water = GetWaterNode(worldCoordinates);
            if (water != null && water.WaterBody.ShoreHeight > height) return false;

            // Check overlapping with existing node
            List<BlockmapNode> sameLevelNodes = GetNodes(worldCoordinates, height);
            if (sameLevelNodes.Any(x => !IsAbove(HelperFunctions.GetSlopeHeights(height, dir), x.Height))) return false;

            return true;
        }
        public void BuildAirSlope(Vector2Int worldCoordinates, int height, Direction dir)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);

            AirNode newNode = new AirNode(this, chunk, NodeIdCounter++, localCoordinates, HelperFunctions.GetSlopeHeights(height, dir), SurfaceId.Tarmac);
            RegisterNode(newNode);

            UpdateNavmesh(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
            UpdateVisionOfNearbyEntities(newNode.GetCenterWorldPosition());
        }

        public bool CanRemoveAirNode(AirNode node)
        {
            if (node.Entities.Count > 0) return false;

            return true;
        }
        public void RemoveAirNode(AirNode node)
        {
            DeregisterNode(node);

            UpdateNavmesh(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntities(node.GetCenterWorldPosition());
        }

        public void SetSurface(SurfaceNode node, SurfaceId surface)
        {
            if (node.Surface.Id == surface) return;

            node.SetSurface(surface);
            RedrawNodesAround(node.WorldCoordinates);
        }

        public bool CanPlaceEntity(StaticEntity entity, BlockmapNode node)
        {
            List<BlockmapNode> occupiedNodes = entity.GetOccupiedNodes(node); // get nodes that would be occupied when placing the entity on the given node

            if (occupiedNodes == null) return false; // Terrain below entity is not fully connected and therefore occupiedNodes is null

            Vector3 placePos = entity.GetWorldPosition(this, node);
            int minHeight = GetNodeHeight(placePos.y); // min y coordinate that this entity will occupy on all occupied tiles
            int maxHeight = minHeight + entity.Dimensions.y; // max y coordinate that this entity will occupy on all occupied tiles

            // Make some checks for all nodes that would be occupied when placing the entity on the given node
            foreach (BlockmapNode occupiedNode in occupiedNodes)
            {
                // Check if the place position is on water
                if (occupiedNode is WaterNode waterNode && placePos.y <= waterNode.WaterBody.WaterSurfaceWorldHeight) return false;
                if (occupiedNode is SurfaceNode surfaceNode && surfaceNode.WaterNode != null &&  placePos.y <= surfaceNode.WaterNode.WaterBody.WaterSurfaceWorldHeight) return false;

                // Check if anything above that blocks space
                int headSpace = occupiedNode.GetFreeHeadSpace(Direction.None);
                if (occupiedNode.BaseHeight + headSpace < maxHeight) return false;

                // Check if alredy has an entity
                if (occupiedNode.Entities.Count > 0) return false;

                // Check if flat
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
                    ((checkDir == Direction.None || checkDir == Direction.N) && (checkNode.Height[Direction.NW] < shoreHeight || checkNode.Height[Direction.NE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.E) && (checkNode.Height[Direction.NE] < shoreHeight || checkNode.Height[Direction.SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.S) && (checkNode.Height[Direction.SW] < shoreHeight || checkNode.Height[Direction.SE] < shoreHeight)) ||
                    ((checkDir == Direction.None || checkDir == Direction.W) && (checkNode.Height[Direction.SW] < shoreHeight || checkNode.Height[Direction.NW] < shoreHeight))
                    );
                if (isUnderwater) // underwater
                {
                    // Check if we're drowing entities
                    if (GetEntities(checkNode.WorldCoordinates, shoreHeight - maxDepth, shoreHeight - 1).Count > 0) return null;

                    // Check if we're drowing air nodes
                    if (GetAirNodes(checkNode.WorldCoordinates, shoreHeight - maxDepth, shoreHeight - 1).Count > 0) return null;

                    waterBody.CoveredNodes.Add(checkNode);

                    if (checkNode.Height[Direction.NW] < shoreHeight || checkNode.Height[Direction.NE] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.N), Direction.S));
                    if (checkNode.Height[Direction.NE] < shoreHeight || checkNode.Height[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.E), Direction.W));
                    if (checkNode.Height[Direction.SW] < shoreHeight || checkNode.Height[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.S), Direction.N));
                    if (checkNode.Height[Direction.NW] < shoreHeight || checkNode.Height[Direction.SW] < shoreHeight) expansionNodes.Add(new System.Tuple<SurfaceNode, Direction>(GetAdjacentSurfaceNode(checkNode, Direction.W), Direction.E));
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
                WaterNode waterNode = new WaterNode(this, node.Chunk, NodeIdCounter++, node.LocalCoordinates, HelperFunctions.GetFlatHeights(data.ShoreHeight), SurfaceId.Water);
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

        public bool CanBuildWall(WallType type, BlockmapNode node, Direction side, int height)
        {
            // Adjust height if it's higher than wall type allows
            if (height > type.MaxHeight) height = type.MaxHeight;

            // Check if wall already has a wall on that side
            foreach(Direction dir in HelperFunctions.GetAffectedDirections(side))
                if (node.Walls.ContainsKey(dir)) return false;

            // Check if enough space above node to place wall of that height
            int freeHeadSpace = node.GetFreeHeadSpace(side, Wall.GetWallStartY(node, side));
            if (freeHeadSpace < height) return false;

            return true;
        }
        public void PlaceWall(WallType type, BlockmapNode node, Direction side, int height)
        {
            // Adjust height if it's higher than wall type allows
            if (height > type.MaxHeight) height = type.MaxHeight;

            Wall wall = new Wall(type);
            wall.Init(node, side, height);

            UpdateNavmesh(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntities(node.GetCenterWorldPosition());
        }
        public void RemoveWall(Wall wall)
        {
            BlockmapNode node = wall.Node;
            DeregisterWall(wall);

            UpdateNavmesh(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntities(node.GetCenterWorldPosition());
        }

        /*
        public void CreateCliffClimbConnection(BlockmapNode from, BlockmapNode to, Direction dir)
        {
            Debug.Log("create climb " + from.Type.ToString() + from.WorldCoordinates.ToString() + " > " + to.Type.ToString() + to.WorldCoordinates.ToString());

            ClimbNode climbStart = new ClimbNode(this, from.Chunk, NodeIdCounter++, from.LocalCoordinates, HelperFunctions.GetFlatHeights(from.BaseHeight));
            ClimbNode climbEnd = new ClimbNode(this, from.Chunk, NodeIdCounter++, from.LocalCoordinates, HelperFunctions.GetFlatHeights(to.BaseHeight));

            climbStart.Init(dir, 0.02f, climbEnd);
            from.ConnectedNodes.Add(dir, climbStart);
            climbStart.ConnectedNodes[HelperFunctions.GetOppositeDirection(dir)] = from;
            climbStart.ConnectedNodes.Add(Direction.None, climbEnd);

            climbEnd.Init(dir, 0.02f, climbStart);
            climbEnd.ConnectedNodes.Add(dir, to);
            to.ConnectedNodes[HelperFunctions.GetOppositeDirection(dir)] = climbEnd;
            climbEnd.ConnectedNodes.Add(Direction.None, climbStart);

            from.ClimbingNodes.Add(dir, new List<ClimbNode>() { climbStart, climbEnd });
        }
        */

        #endregion

        #region Draw

        /// <summary>
        /// Generates all meshes of the world.
        /// </summary>
        public void DrawNodes()
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
        public void RedrawNodesAround(Vector2Int worldCoordinates, int rangeEast = 1, int rangeNorth = 1)
        {
            List<Chunk> affectedChunks = new List<Chunk>();

            for (int y = worldCoordinates.y - 1; y <= worldCoordinates.y + rangeEast ; y++)
            {
                for (int x = worldCoordinates.x - 1; x <= worldCoordinates.x + rangeNorth; x++)
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
        /// <br/> Is delayed by one frame so all draw calls can be completed before shooting the vision rays.
        /// </summary>
        public void UpdateVisionOfNearbyEntities(Vector3 position, int rangeEast = 1, int rangeNorth = 1)
        {
            DoUpdateVisionNextFrame = true;
            VisionUpdatePosition = position;
            VisionUpdateRangeEast = rangeEast;
            VisionUpdateRangeNorth = rangeNorth;
        }
        private void DoUpdateVisionOfNearbyEntities() // never call this directly
        {
            foreach (Entity e in Entities.Where(x => Vector3.Distance(x.GetWorldCenter(), VisionUpdatePosition) <= x.VisionRange + (VisionUpdateRangeEast - 1) + (VisionUpdateRangeNorth - 1)))
                e.UpdateVisibleNodes();

            DoUpdateVisionNextFrame = false;
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

        public BlockmapNode GetNode(int id) => Nodes[id];

        public List<BlockmapNode> GetNodes(Vector2Int worldCoordinates, int height)
        {
            return GetNodes(worldCoordinates, height, height);
        }
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
        public List<SurfaceNode> GetSurfaceNodes(Vector2Int worldCoordinates, int rangeEast, int rangeNorth)
        {
            List<SurfaceNode> nodes = new List<SurfaceNode>();
            for(int x = 0; x < rangeEast; x++)
            {
                for(int y = 0; y < rangeNorth; y++)
                {
                    Vector2Int coordinates = worldCoordinates + new Vector2Int(x, y);
                    SurfaceNode node = GetSurfaceNode(coordinates);
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
        public WaterNode GetAdjacentWaterNode(Vector2Int worldCoordinates, Direction dir)
        {
            return GetWaterNode(GetWorldCoordinatesInDirection(worldCoordinates, dir));
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
            return worldCoordinates + GetDirectionVector(dir);
        }
        public Vector2Int GetDirectionVector(Direction dir, int distance = 1)
        {
            if (dir == Direction.N) return new Vector2Int(0, distance);
            if (dir == Direction.E) return new Vector2Int(distance, 0);
            if (dir == Direction.S) return new Vector2Int(0, -distance);
            if (dir == Direction.W) return new Vector2Int(-distance, 0);
            if (dir == Direction.NE) return new Vector2Int(distance, distance);
            if (dir == Direction.NW) return new Vector2Int(-distance, distance);
            if (dir == Direction.SE) return new Vector2Int(distance, -distance);
            if (dir == Direction.SW) return new Vector2Int(-distance, -distance);
            return new Vector2Int(0, 0);
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
        /// Returns the exact world height (y-coordinate) of the given node at the given position.
        /// <br/> Only works when the node mesh is drawn.
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
                if (node.Type == NodeType.AirPath && objectHit.gameObject.layer == Layer_AirNode && objectHit.GetComponent<AirNodeMesh>().HeightLevel == node.BaseHeight)
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

        /// <summary>
        /// Checks and returns if two adjacent nodes match seamlessly in the given direction.
        /// <br/> 
        /// </summary>
        public bool DoAdjacentHeightsMatch(BlockmapNode fromNode, BlockmapNode toNode, Direction dir)
        {
            if (toNode.WorldCoordinates != GetWorldCoordinatesInDirection(fromNode.WorldCoordinates, dir))
                throw new System.Exception("toNode is not adjacent to fromNode in the given direction. fromNode = " + fromNode.WorldCoordinates.ToString() + ", toNode = " + toNode.WorldCoordinates.ToString() + ", direction = " + dir.ToString());

            switch (dir)
            {
                case Direction.N:
                    return (fromNode.Height[Direction.NE] == toNode.Height[Direction.SE]) && (fromNode.Height[Direction.NW] == toNode.Height[Direction.SW]);

                case Direction.S:
                    return (fromNode.Height[Direction.SE] == toNode.Height[Direction.NE]) && (fromNode.Height[Direction.SW] == toNode.Height[Direction.NW]);

                case Direction.E:
                    return (fromNode.Height[Direction.SE] == toNode.Height[Direction.SW]) && (fromNode.Height[Direction.NE] == toNode.Height[Direction.NW]);

                case Direction.W:
                    return (fromNode.Height[Direction.SW] == toNode.Height[Direction.SE]) && (fromNode.Height[Direction.NW] == toNode.Height[Direction.NE]);

                case Direction.NW:
                    return fromNode.Height[Direction.NW] == toNode.Height[Direction.SE];

                case Direction.NE:
                    return fromNode.Height[Direction.NE] == toNode.Height[Direction.SW];

                case Direction.SW:
                    return fromNode.Height[Direction.SW] == toNode.Height[Direction.NE];

                case Direction.SE:
                    return fromNode.Height[Direction.SE] == toNode.Height[Direction.NW];

                default:
                    return false;
            }
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
                if (node1.Height[dir] != node2.Height[dir]) return false;
            return true;
        }
        #endregion

        #region Save / Load

        public static World Load(WorldData data, WorldEntityLibrary entityLibrary)
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
                MaxWaterBodyId = WaterBodyIdCounter,
                Chunks = Chunks.Values.Select(x => x.Save()).ToList(),
                Players = Players.Values.Select(x => x.Save()).ToList(),
                Entities = Entities.Select(x => x.Save()).ToList(),
                WaterBodies = WaterBodies.Values.Select(x => x.Save()).ToList(),
                Walls = Walls.Select(x => x.Save()).ToList()
            };
        }

        #endregion
    }
}
