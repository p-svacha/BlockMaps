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
        /// Physical width and length (x & z) of a tile.
        /// </summary>
        public const float TILE_SIZE = 1f;


        // Base
        public string Name { get; private set; }
        public int ChunkSize { get; private set; }
        public Dictionary<Vector2Int, Chunk> Chunks = new Dictionary<Vector2Int, Chunk>();
        private int NodeIdCounter;
        private BlockmapCamera Camera;

        // Layers
        public int Layer_SurfaceNode;
        public int Layer_Entity;
        public int Layer_AirNode;

        /// <summary>
        /// Flag if the cursor is currently inside the world.
        /// </summary>
        public bool IsHoveringWorld { get; private set; }

        /// <summary>
        /// World coordinates that are currently being hovered.
        /// </summary>
        public Vector2Int HoveredWorldCoordinates { get; private set; }

        /// <summary>
        /// Node that is currently being hovered.
        /// </summary>
        public BlockmapNode HoveredNode { get; private set; }

        /// <summary>
        /// SurfaceNode that is currently being hovered.
        /// </summary>
        public SurfaceNode HoveredSurfaceNode { get; private set; }

        /// <summary>
        /// Chunk that is currently being hovered.
        /// </summary>
        public Chunk HoveredChunk { get; private set; }

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

        private bool IsShowingGrid;
        private bool IsShowingPathfindingGraph;


        public void Init(WorldData data)
        {
            Layer_SurfaceNode = LayerMask.NameToLayer("Terrain");
            Layer_Entity = LayerMask.NameToLayer("Entity");
            Layer_AirNode = LayerMask.NameToLayer("Path");

            Name = data.Name;
            ChunkSize = data.ChunkSize;
            foreach(ChunkData chunkData in data.Chunks)
                Chunks.Add(new Vector2Int(chunkData.ChunkCoordinateX, chunkData.ChunkCoordinateY), Chunk.Load(this, chunkData));
            NodeIdCounter = data.MaxNodeId + 1;

            Pathfinder.Init(this);

            // Init connections
            foreach (Chunk chunk in Chunks.Values) chunk.UpdateFullPathfindingGraph();

            // Init camera
            Camera = GameObject.Find("Main Camera").GetComponent<BlockmapCamera>();
            Camera.SetPosition(new Vector2(ChunkSize * 0.5f, ChunkSize * 0.5f));
            Camera.SetZoom(10f);
            Camera.SetAngle(225);
        }

        void Update()
        {
            UpdateHoveredObjects();
        }

        #region Actions

        public void UpdatePathfindingGraphAround(Vector2Int worldCoordinates)
        {
            int redrawRadius = 1;
            for (int y = worldCoordinates.y - redrawRadius; y <= worldCoordinates.y + redrawRadius; y++)
            {
                for (int x = worldCoordinates.x - redrawRadius; x <= worldCoordinates.x + redrawRadius; x++)
                {
                    Vector2Int coordinates = new Vector2Int(x, y);
                    if (!IsInWorld(coordinates)) continue;

                    List<BlockmapNode> nodes = GetNodes(coordinates);
                    foreach (BlockmapNode node in nodes) node.UpdateConnectedNodesStraight();
                }
            }

            for (int y = worldCoordinates.y - redrawRadius; y <= worldCoordinates.y + redrawRadius; y++)
            {
                for (int x = worldCoordinates.x - redrawRadius; x <= worldCoordinates.x + redrawRadius; x++)
                {
                    Vector2Int coordinates = new Vector2Int(x, y);
                    if (!IsInWorld(coordinates)) continue;
                    List<BlockmapNode> nodes = GetNodes(coordinates);
                    foreach (BlockmapNode node in nodes) node.UpdateConnectedNodesDiagonal();
                }
            }
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
        }

        public bool CanBuildAirPath(Vector2Int worldCoordinates, int height)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            SurfaceNode surfaceNode = chunk.GetSurfaceNode(localCoordinates);

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

            NodeData newNodeData = new NodeData()
            {
                Id = NodeIdCounter++,
                LocalCoordinateX = localCoordinates.x,
                LocalCoordinateY = localCoordinates.y,
                Height = new int[] { height, height, height, height },
                Surface = SurfaceId.Tarmac,
                Type = NodeType.AirPath
            };
            BlockmapNode newNode = BlockmapNode.Load(this, chunk, newNodeData);
            chunk.Nodes[localCoordinates.x, localCoordinates.y].Add(newNode);

            UpdatePathfindingGraphAround(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
        }

        public bool CanBuildAirSlope(Vector2Int worldCoordinates, int height, Direction dir)
        {
            Chunk chunk = GetChunk(worldCoordinates);
            Vector2Int localCoordinates = chunk.GetLocalCoordinates(worldCoordinates);
            SurfaceNode surfaceNode = chunk.GetSurfaceNode(localCoordinates);

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

            NodeData newNodeData = new NodeData()
            {
                Id = NodeIdCounter++,
                LocalCoordinateX = localCoordinates.x,
                LocalCoordinateY = localCoordinates.y,
                Height = AirPathSlopeNode.GetHeightsFromDirection(height, dir),
                Surface = SurfaceId.Tarmac,
                Type = NodeType.AirPathSlope
            };
            BlockmapNode newNode = BlockmapNode.Load(this, chunk, newNodeData);
            chunk.Nodes[localCoordinates.x, localCoordinates.y].Add(newNode);

            UpdatePathfindingGraphAround(newNode.WorldCoordinates);
            RedrawNodesAround(newNode.WorldCoordinates);
        }

        public void SetSurface(SurfaceNode node, SurfaceId surface)
        {
            node.SetSurface(surface);
            RedrawNodesAround(node.WorldCoordinates);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Generates all meshes of the world
        /// </summary>
        public void Draw()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.Draw();

            UpdateGridOverlay();
            UpdatePathfindingVisualization();
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

            foreach (Chunk chunk in affectedChunks) chunk.Draw();

            UpdateGridOverlay();
            UpdatePathfindingVisualization();
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
            if (IsShowingPathfindingGraph) PathfindingGraphVisualizer.Singleton.VisualizeGraph(this);
            else PathfindingGraphVisualizer.Singleton.ClearVisualization();
        }

        #endregion

        #region Input Handling

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

            // Shoot a raycast on surface and air layer to detect hovered nodes
            if (Physics.Raycast(ray, out hit, 1000f, 1 << Layer_SurfaceNode | 1 << Layer_AirNode))
            {
                Transform objectHit = hit.transform;

                if (objectHit != null)
                {
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
                    else if(objectHit.gameObject.layer == Layer_AirNode) // Hit an air node
                    {
                        newHoveredNode = GetAirNodes(HoveredWorldCoordinates).First(x => x.BaseHeight == objectHit.GetComponent<AirNodeMesh>().HeightLevel);
                    }
                }
            }

            // Update currently hovered objects
            HoveredNode = newHoveredNode;
            HoveredSurfaceNode = newHoveredSurfaceNode;
            HoveredChunk = newHoveredChunk;

            // Fire update events
            if (newHoveredNode != oldHoveredNode) OnHoveredNodeChanged?.Invoke(oldHoveredNode, newHoveredNode);
            if (newHoveredSurfaceNode != oldHoveredSurfaceNode) OnHoveredSurfaceNodeChanged?.Invoke(oldHoveredSurfaceNode, newHoveredSurfaceNode);
            if (newHoveredChunk != oldHoveredChunk) OnHoveredChunkChanged?.Invoke(oldHoveredChunk, newHoveredChunk);
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

        public List<BlockmapNode> GetNodes(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetNodes(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public SurfaceNode GetSurfaceNode(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetSurfaceNode(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public List<BlockmapNode> GetAirNodes(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetAirNodes(chunk.GetLocalCoordinates(worldCoordinates));
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

        public float GetTerrainHeightAt(Vector2 worldPosition2d)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(worldPosition2d.x, 20f, worldPosition2d.y), -Vector3.up, out hit, 1000f, 1 << Layer_SurfaceNode))
            {
                Transform objectHit = hit.transform;

                if (objectHit != null)
                {
                    Vector3 hitPosition = hit.point;
                    return hitPosition.y;
                }
            }
            throw new System.Exception("Couldn't get height");
        }
        public float GetPathHeightAt(Vector2 worldPosition2d, int baseHeight)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(worldPosition2d.x, 20f, worldPosition2d.y), -Vector3.up, 1000f, 1 << Layer_AirNode);
            foreach (RaycastHit hit in hits)
            {
                Transform objectHit = hit.transform;

                if (objectHit != null)
                {
                    Vector3 hitPosition = hit.point;
                    if (hitPosition.y >= GetWorldHeight(baseHeight) && hitPosition.y <= GetWorldHeight(baseHeight + 1)) return hitPosition.y;
                }
            }

            return GetTerrainHeightAt(worldPosition2d);
        }

        public SurfaceNode GetRandomOwnedTerrainNode()
        {
            List<Chunk> candidateChunks = Chunks.Values.ToList();
            Chunk chosenChunk = candidateChunks[Random.Range(0, candidateChunks.Count)];

            int x = Random.Range(0, ChunkSize);
            int y = Random.Range(0, ChunkSize);
            return chosenChunk.GetSurfaceNode(x, y);
        }

        #endregion

        #region Save / Load

        public static World Load(WorldData data)
        {
            GameObject worldObject = new GameObject(data.Name);
            World world = worldObject.AddComponent<World>();
            world.Init(data);
            return world;
        }

        public WorldData Save()
        {
            return new WorldData
            {
                Name = Name,
                ChunkSize = ChunkSize,
                MaxNodeId = NodeIdCounter,
                Chunks = Chunks.Values.Select(x => x.Save()).ToList()
            };
        }

        #endregion
    }
}
