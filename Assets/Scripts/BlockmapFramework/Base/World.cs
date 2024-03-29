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
        public bool IsInitialized { get; private set; }
        public int ChunkSize { get; private set; }
        public int MinX, MaxX, MinY, MaxY;
        public Vector2Int Dimensions { get; private set; }
        public WorldEntityLibrary ContentLibrary { get; private set; }


        // Database
        private Dictionary<int, BlockmapNode> Nodes = new Dictionary<int, BlockmapNode>();
        public Dictionary<Vector2Int, Chunk> Chunks = new Dictionary<Vector2Int, Chunk>();
        public Dictionary<int, Actor> Actors = new Dictionary<int, Actor>();
        public List<Entity> Entities = new List<Entity>();
        public Dictionary<int, WaterBody> WaterBodies = new Dictionary<int, WaterBody>();
        public List<Wall> Walls = new List<Wall>();
        public Dictionary<int, Zone> Zones = new Dictionary<int, Zone>();

        private int NodeIdCounter;
        private int EntityIdCounter;
        private int WaterBodyIdCounter;
        private int ActorIdCounter;
        private int ZoneIdCounter;

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
        public int Layer_Wall;
        public int Layer_ProceduralEntityMesh;

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
            Layer_Wall = LayerMask.NameToLayer("Wall");
            Layer_ProceduralEntityMesh = LayerMask.NameToLayer("ProceduralEntityMesh");

            // Init pathfinder
            Pathfinder.Init(this);

            // Init database id's
            NodeIdCounter = data.MaxNodeId + 1;
            EntityIdCounter = data.MaxEntityId + 1;
            WaterBodyIdCounter = data.MaxWaterBodyId + 1;
            ActorIdCounter = data.MaxActorId + 1;
            ZoneIdCounter = data.MaxZoneId + 1;

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
            Camera.SetPosition(new Vector3(initialCameraFocusNode.WorldCoordinates.x, initialCameraFocusNode.BaseHeight * TILE_HEIGHT, initialCameraFocusNode.WorldCoordinates.y));
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

            ContentLibrary = entityLibrary;

            // Init walls
            foreach (WallData wallData in data.Walls)
            {
                Wall wall = Wall.Load(this, wallData);
            }

            // Init water bodies
            foreach (WaterBodyData waterData in data.WaterBodies)
            {
                WaterBody water = WaterBody.Load(this, waterData);
                WaterBodies.Add(waterData.Id, water);
            }

            // Init zones
            foreach(ZoneData zoneData in data.Zones)
            {
                Zone zone = Zone.Load(this, zoneData);
                Zones.Add(zoneData.Id, zone);
            }

            IsInitialized = false;
            InitializeStep = 1;
        }
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

                    // Register entity (null-check because some special entities are already registered in Entity.Load (i.e. ladders))
                    if (e != null) RegisterEntity(e);
                }

                // Draw node meshes because we need to shoot rays to generate navmesh
                DrawNodes();

                InitializeStep++;
                return;
            }

            // Frame 2 after initialization: Do stuff that requires entities to be at the correct world position
            if (InitializeStep == 2)
            {
                foreach (Entity e in Entities) e.UpdateVision();

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
            UpdateInitialization();
            if (!IsInitialized) return;

            // Invoke delayed updates from last frame

            // Regular updates
            UpdateHoveredObjects();
            foreach (Entity e in Entities) e.UpdateEntity();
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

            Wall oldHoveredWall = HoveredWall;
            Wall newHoveredWall = null;

            // Shoot a raycast on ground and air layers to detect hovered nodes
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_GroundNode | 1 << Layer_AirNode | 1 << Layer_Water | 1 << Layer_Wall))
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
                    newHoveredAirNode = GetAirNodes(HoveredWorldCoordinates).FirstOrDefault(x => x.BaseHeight == objectHit.GetComponent<AirNodeMesh>().HeightLevel);

                    if (newHoveredAirNode == null) // If we are exactly on a north or east edge we have to adjust the hit position slightly, else we are 1 coordinate off and don't find anything
                    {
                        Vector3 offsetHitPosition = hitPosition + new Vector3(-0.001f, 0f, -0.001f);
                        Vector2Int offsetCoordinates = GetWorldCoordinates(offsetHitPosition);

                        newHoveredAirNode = GetAirNodes(offsetCoordinates).FirstOrDefault(x => x.BaseHeight == objectHit.GetComponent<AirNodeMesh>().HeightLevel);
                        
                    }

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

                // Hit wall
                else if(objectHit.gameObject.layer == Layer_Wall)
                {
                    newHoveredWall = GetWallFromRaycastHit(hit);
                    if (newHoveredWall != null) HoveredWorldCoordinates = newHoveredWall.Node.WorldCoordinates;
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
                else if(hit.transform.gameObject.layer == Layer_ProceduralEntityMesh)
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

        public Wall GetWallFromRaycastHit(RaycastHit hit)
        {
            Vector2Int hitCoordinates = GetWorldCoordinates(hit.point);
            List<BlockmapNode> hitNodes = GetNodes(hitCoordinates, hit.transform.GetComponent<WallMesh>().HeightLevel).OrderByDescending(x => x.MaxHeight).ToList();
            Direction primaryHitSide = GetNodeHoverMode8(hit.point);
            List<Direction> otherPossibleHitSides = GetNodeHoverModes8(hit.point);

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
                Direction primaryOffsetSide = GetNodeHoverMode8(offsetHitPosition);
                List<Direction> otherPossibleOffsetSides = GetNodeHoverModes8(offsetHitPosition);

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
        private ProceduralEntity GetProceduralEntityFromRaycastHit(RaycastHit hit)
        {
            ProceduralEntityMesh hitMesh = hit.transform.GetComponent<ProceduralEntityMesh>();
            Vector2Int hitCoordinates = GetWorldCoordinates(hit.point);
            List<BlockmapNode> hitNodes = GetNodes(hitCoordinates, hitMesh.HeightLevel);

            // If the exact node we hit has a procedural entity, return that
            BlockmapNode targetNode = hitNodes.FirstOrDefault(x => x.Entities.Any(e => e is ProceduralEntity));
            if (targetNode != null) return targetNode.Entities.First(x => x is ProceduralEntity) as ProceduralEntity;

            return null;
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
            else if(node is AirNode airNode) node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(airNode);
        }
        public void DeregisterNode(BlockmapNode node)
        {
            // Destroy walls on node
            while(node.Walls.Count > 0) DeregisterWall(node.Walls.Values.ToList()[0]);

            // Destroy ladders from and to node
            while (node.SourceLadders.Count > 0) RemoveLadder(node.SourceLadders.Values.ToList()[0]);
            while (node.TargetLadders.Count > 0) RemoveLadder(node.TargetLadders.Values.ToList()[0]);

            // Node
            Nodes.Remove(node.Id); // Global registry

            // Chunk registry
            node.Chunk.Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(node);
            if (node is GroundNode groundNode) node.Chunk.GroundNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if (node is WaterNode waterNode) node.Chunk.WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = null;
            else if(node is AirNode airNode) node.Chunk.AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Remove(airNode);
        }
        private void DeregisterWall(Wall wall)
        {
            BlockmapNode node = wall.Node;
            node.Walls.Remove(wall.Side);
            Walls.Remove(wall);
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
            foreach (Entity entity in Entities) entity.ResetLastKnownPositionFor(actor);

            foreach (Entity entity in Entities.Where(x => x.Owner == actor)) entity.UpdateVision();

            UpdateVisibility();
        }
        public void ExploreEverything(Actor actor)
        {
            foreach (BlockmapNode node in Nodes.Values) node.AddExploredBy(actor);
            foreach (Entity entity in Entities) entity.UpdateLastKnownPositionFor(actor);

            foreach (Entity entity in Entities.Where(x => x.Owner == actor)) entity.UpdateVision();

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

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            foreach (BlockmapNode node in nodes) node.ResetTransitions();
            foreach (BlockmapNode node in nodes) node.SetStraightAdjacentTransitions();
            foreach (BlockmapNode node in nodes) node.SetDiagonalAdjacentTransitions();
            foreach (BlockmapNode node in nodes) node.SetClimbTransitions();

            if(isInitialization) IsInitialized = true;

            sw.Stop();
            Debug.Log("Updating navmesh took " + sw.ElapsedMilliseconds + " ms for " + nodes.Count + " nodes.");

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

        public bool CanChangeHeight(DynamicNode node, Direction mode, bool isIncrease)
        {
            return node.CanChangeHeight(mode, isIncrease);
        }
        public void ChangeHeight(DynamicNode node, Direction mode, bool isIncrease)
        {
            node.ChangeHeight(mode, isIncrease);

            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(node.GetCenterWorldPosition());
        }

        public void SetSurface(DynamicNode node, SurfaceId surfaceId)
        {
            node.SetSurface(surfaceId);

            RedrawNodesAround(node.WorldCoordinates);
            UpdateNavmeshDisplayDelayed();
        }
        
        public bool CanBuildAirPath(Vector2Int worldCoordinates, int height)
        {
            if (!IsInWorld(worldCoordinates)) return false;

            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            GroundNode groundNode = chunk.GetGroundNode(localCoordinates);

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

            // Check if overlap with ground
            if (!IsAbove(HelperFunctions.GetFlatHeights(height), groundNode.Height)) return false;

            return true;
        }
        public void BuildAirPath(Vector2Int worldCoordinates, int height, SurfaceId surface)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);

            AirNode newNode = new AirNode(this, chunk, NodeIdCounter++, localCoordinates, HelperFunctions.GetFlatHeights(height), surface);
            RegisterNode(newNode);

            UpdateNavmeshAround(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(newNode.GetCenterWorldPosition());
        }
        public bool CanBuildAirSlope(Vector2Int worldCoordinates, int height, Direction dir)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            GroundNode groundNode = chunk.GetGroundNode(localCoordinates);

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

            AirNode newNode = new AirNode(this, chunk, NodeIdCounter++, localCoordinates, HelperFunctions.GetSlopeHeights(height, dir), SurfaceId.Concrete);
            RegisterNode(newNode);

            UpdateNavmeshAround(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(newNode.GetCenterWorldPosition());
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
            UpdateVisionOfNearbyEntitiesDelayed(node.GetCenterWorldPosition());
        }

        public bool CanSpawnEntity(Entity entityPrefab, BlockmapNode node, Direction rotation)
        {
            HashSet<BlockmapNode> occupiedNodes = entityPrefab.GetOccupiedNodes(this, node, rotation); // get nodes that would be occupied when placing the entity on the given node

            if (occupiedNodes == null) return false; // Terrain below entity is not fully connected and therefore occupiedNodes is null
            
            Vector3 placePos = entityPrefab.GetWorldPosition(this, node, rotation);
            int minHeight = GetNodeHeight(placePos.y); // min y coordinate that this entity will occupy on all occupied tiles
            int maxHeight = minHeight + entityPrefab.Height; // max y coordinate that this entity will occupy on all occupied tiles

            // Make some checks for all nodes that would be occupied when placing the entity on the given node
            foreach (BlockmapNode occupiedNode in occupiedNodes)
            {
                // Check if the place position is on water
                if (occupiedNode is WaterNode waterNode && placePos.y <= waterNode.WaterBody.WaterSurfaceWorldHeight) return false;
                if (occupiedNode is GroundNode groundNode && groundNode.WaterNode != null &&  placePos.y <= groundNode.WaterNode.WaterBody.WaterSurfaceWorldHeight) return false;

                // Check if anything above that blocks space
                int headSpace = occupiedNode.GetFreeHeadSpace(Direction.None);
                if (occupiedNode.BaseHeight + headSpace < maxHeight) return false;

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
            if(instance is ProceduralEntity) RedrawNodesAround(node.WorldCoordinates);

            // Update vision around new entity
            if (updateWorld) UpdateVisionOfNearbyEntitiesDelayed(instance.OriginNode.GetCenterWorldPosition());

            // Update if the new entity is currently visible
            if (updateWorld) instance.UpdateVisiblity(ActiveVisionActor);

            // Update pathfinding navmesh
            if (updateWorld) UpdateNavmeshAround(node.WorldCoordinates, instance.GetDimensions().x, instance.GetDimensions().z);

            // Return new instance
            return instance;
        }
        public void RemoveEntity(Entity entity)
        {
            // De-register entity
            Entities.Remove(entity);
            entity.Owner.Entities.Remove(entity);

            // Remove seen by on all nodes
            HashSet<Chunk> chunksAffectedByVision = new HashSet<Chunk>();
            foreach (BlockmapNode node in entity.VisibleNodes)
            {
                node.RemoveVisionBy(entity);
                chunksAffectedByVision.Add(node.Chunk);
            }

            // Remove node & chunk occupation
            foreach (BlockmapNode node in entity.OccupiedNodes)
            {
                node.RemoveEntity(entity);
                node.Chunk.RemoveEntity(entity);
            }

            // Redraw chunk meshes if it is a procedural entity
            if (entity is ProceduralEntity) RedrawNodesAround(entity.OriginNode.WorldCoordinates);

            // Update pathfinding navmesh
            UpdateNavmeshAround(entity.OriginNode.WorldCoordinates, entity.GetDimensions().x, entity.GetDimensions().z);

            // Update visibility of all chunks affected by the entity vision if the entity belongs to the active vision actor
            if (entity.Owner == ActiveVisionActor)
                foreach (Chunk c in chunksAffectedByVision)
                    UpdateVisibility(c);

            // Destroy
            entity.DestroySelf();

            // Update vision of all other entities near the entity (doesn't work instantly bcuz destroying takes too long)
            UpdateVisionOfNearbyEntitiesDelayed(entity.GetWorldCenter());
        }
        private void RegisterEntity(Entity entity)
        {
            Entities.Add(entity);
            entity.Owner.Entities.Add(entity);
        }

        public WaterBody CanAddWater(GroundNode node, int maxDepth) // returns null when cannot
        {
            int shoreHeight = node.BaseHeight + 1;
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

                    if (checkNode.Height[Direction.NW] < shoreHeight || checkNode.Height[Direction.NE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.N), Direction.S));
                    if (checkNode.Height[Direction.NE] < shoreHeight || checkNode.Height[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.E), Direction.W));
                    if (checkNode.Height[Direction.SW] < shoreHeight || checkNode.Height[Direction.SE] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.S), Direction.N));
                    if (checkNode.Height[Direction.NW] < shoreHeight || checkNode.Height[Direction.SW] < shoreHeight) expansionNodes.Add(new System.Tuple<GroundNode, Direction>(GetAdjacentGroundNode(checkNode, Direction.W), Direction.E));
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
                WaterNode waterNode = new WaterNode(this, node.Chunk, NodeIdCounter++, node.LocalCoordinates, HelperFunctions.GetFlatHeights(data.ShoreHeight));
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
            if(updateNavmesh)
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

        public bool CanBuildWall(WallType type, BlockmapNode node, Direction side, int height)
        {
            // Check if disallowed corner
            if (HelperFunctions.IsCorner(side) && !type.CanBuildOnCorners) return false;

            // Adjust height if it's higher than wall type allows
            if (height > type.MaxHeight) height = type.MaxHeight;

            // Check if node already has a wall on that side
            foreach(Direction dir in HelperFunctions.GetAffectedDirections(side))
                if (node.Walls.ContainsKey(dir)) return false;

            // Check if a ladder is already there
            if (node.SourceLadders.ContainsKey(side)) return false;

            // Check if enough space above node to place wall of that height
            int freeHeadSpace = node.GetFreeHeadSpace(side, node.GetMinHeight(side));
            if (freeHeadSpace < height) return false;

            return true;
        }
        public void PlaceWall(WallType type, BlockmapNode node, Direction side, int height)
        {
            // Adjust height if it's higher than wall type allows
            if (height > type.MaxHeight) height = type.MaxHeight;

            Wall wall = new Wall(type);
            wall.Init(node, side, height);

            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(node.GetCenterWorldPosition());
        }
        public void RemoveWall(Wall wall)
        {
            BlockmapNode node = wall.Node;
            DeregisterWall(wall);

            UpdateNavmeshAround(node.WorldCoordinates);
            RedrawNodesAround(node.WorldCoordinates);
            UpdateVisionOfNearbyEntitiesDelayed(node.GetCenterWorldPosition());
        }

        public List<BlockmapNode> GetPossibleLadderTargetNodes(BlockmapNode source, Direction side)
        {
            List<BlockmapNode> possibleTargetNodes = new List<BlockmapNode>();

            // Check if source node is viable for a ladder
            if (!source.IsFlat(side)) return possibleTargetNodes;
            if (source.Walls.ContainsKey(side)) return possibleTargetNodes;
            if (source.SourceLadders.ContainsKey(side)) return possibleTargetNodes;

            // Get target node (need to be adjacent, higher up and flat on the target direction)
            int sourceHeight = source.GetMinHeight(side);
            Direction targetSide = HelperFunctions.GetOppositeDirection(side);
            Vector2Int targetCoordinates = GetWorldCoordinatesInDirection(source.WorldCoordinates, side);

            List<BlockmapNode> adjNodes = GetNodes(targetCoordinates).OrderBy(x => x.GetMaxHeight(targetSide)).ToList();
            foreach (BlockmapNode adjNode in adjNodes)
            {
                if (adjNode.GetMaxHeight(targetSide) <= sourceHeight) continue; // not higher than starting point
                if (!adjNode.IsFlat(HelperFunctions.GetOppositeDirection(side))) continue; // not flat in target direction

                // Check headspace
                int targetHeight = adjNode.GetMaxHeight(HelperFunctions.GetOppositeDirection(side));
                int ladderHeight = targetHeight - sourceHeight;
                int headspace = source.GetFreeHeadSpace(side);
                if (headspace <= ladderHeight) continue; // a node is blocking the climb

                // Add as possible target
                possibleTargetNodes.Add(adjNode);
            }

            return possibleTargetNodes;
        }
        public bool CanBuildLadder(BlockmapNode from, BlockmapNode to, Direction side)
        {
            return GetPossibleLadderTargetNodes(from, side).Contains(to);
        }
        public void BuildLadder(BlockmapNode from, BlockmapNode to, Direction side)
        {
            Ladder newLadder = new Ladder(from, to, side);
            newLadder.Init();
        }
        public void RemoveLadder(Ladder ladder)
        {
            ladder.Bottom.SourceLadders.Remove(ladder.Side);
            ladder.Top.TargetLadders.Remove(HelperFunctions.GetOppositeDirection(ladder.Side));
            RemoveEntity(ladder.Entity);
        }

        public Zone AddZone(HashSet<Vector2Int> coordinates, Actor actor, bool providesVision, bool showBorders)
        {
            int id = ZoneIdCounter++;
            Zone newZone = new Zone(this, id, actor, coordinates, providesVision, showBorders);
            Zones.Add(id, newZone);
            return newZone;
        }

        #endregion

        #region Draw

        /// <summary>
        /// Generates all meshes of the world.
        /// </summary>
        public void DrawNodes()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.DrawMesh();

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
            chunk.DrawMesh();
            chunk.SetVisibility(ActiveVisionActor);
            chunk.ShowGrid(IsShowingGrid);
            chunk.ShowTextures(IsShowingTextures);
            chunk.ShowTileBlending(IsShowingTileBlending);
            chunk.DrawZoneBorders();
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
        /// <br/> Is delayed by one frame so all draw calls can be completed before shooting the vision rays.
        /// </summary>
        public void UpdateVisionOfNearbyEntitiesDelayed(Vector3 position, int rangeEast = 1, int rangeNorth = 1)
        {
            StartCoroutine(DoUpdateVisionOfNearbyEntities(position, rangeEast, rangeNorth));
        }
        private IEnumerator DoUpdateVisionOfNearbyEntities(Vector3 position, int rangeEast = 1, int rangeNorth = 1)
        {
            yield return new WaitForFixedUpdate();

            List<Entity> entitiesToUpdate = Entities.Where(x => Vector3.Distance(x.GetWorldCenter(), position) <= x.VisionRange + (rangeEast) + (rangeNorth)).ToList();
            Debug.Log("Updating vision of " + entitiesToUpdate.Count + " entities.");
            foreach (Entity e in entitiesToUpdate) e.UpdateVision();
        }

        public void ToggleGridOverlay()
        {
            IsShowingGrid = !IsShowingGrid;
            UpdateGridOverlay();
        }
        public void ShowGridOverlay(bool value)
        {
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
            IsShowingTileBlending = value;
            UpdateTileBlending();
        }
        private void UpdateTileBlending()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.ShowTileBlending(IsShowingTileBlending);
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
        public void CameraPanToFocusEntity(Entity e, float duration, bool followAfterPan)
        {
            Camera.PanTo(duration, e.WorldPosition, followAfterPan ? e : null);
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

        public bool IsValidNodeHeight(Dictionary<Direction, int> height)
        {
            if (height.Values.Any(x => x < 0)) return false;
            if (height.Values.Any(x => x > World.MAX_HEIGHT)) return false;

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

        public HashSet<BlockmapNode> GetAllNodes() => Nodes.Values.ToHashSet();
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

        public List<GroundNode> GetAllGroundNodes()
        {
            List<GroundNode> nodes = new List<GroundNode>();
            foreach (Chunk c in Chunks.Values) nodes.AddRange(c.GetAllGroundNodes());
            return nodes;
        }
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
        public GroundNode GetAdjacentGroundNode(Vector2Int worldCoordinates, Direction dir)
        {
            return GetGroundNode(GetWorldCoordinatesInDirection(worldCoordinates, dir));
        }
        public GroundNode GetAdjacentGroundNode(BlockmapNode node, Direction dir)
        {
            return GetAdjacentGroundNode(node.WorldCoordinates, dir);
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

                // We hit the ground mesh we are looking for
                if(node.Type == NodeType.Ground && objectHit.gameObject.layer == Layer_GroundNode)
                {
                    Vector3 hitPosition = hit.point;
                    return Mathf.Max(hitPosition.y, node.BaseWorldHeight);
                }

                // We hit the air node mesh of the level we are looking for
                if (node.Type == NodeType.Air && objectHit.gameObject.layer == Layer_AirNode && objectHit.GetComponent<AirNodeMesh>().HeightLevel == node.BaseHeight)
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

        /// <summary>
        /// Checks and returns if two node heights intersect by either fully overlapping or going through each other.
        /// </summary>
        public bool DoNodesIntersect(Dictionary<Direction, int> n1, Dictionary<Direction, int> n2)
        {
            if (!IsAbove(n1, n2) && !IsAbove(n2, n1)) return true; // Neither node is fully above the other one => they intersect
            return false;
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
                Chunks = Chunks.Values.Select(x => x.Save()).ToList(),
                Actors = Actors.Values.Select(x => x.Save()).ToList(),
                Entities = Entities.Select(x => x.Save()).ToList(),
                WaterBodies = WaterBodies.Values.Select(x => x.Save()).ToList(),
                Walls = Walls.Select(x => x.Save()).ToList(),
                Zones = Zones.Values.Select(x => x.Save()).ToList()
            };
        }

        #endregion
    }
}
