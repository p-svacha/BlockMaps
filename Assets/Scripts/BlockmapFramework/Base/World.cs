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
        /// Minimum y coordinate a tile can have.
        /// </summary>
        public const int MIN_HEIGHT = 0;
        /// <summary>
        /// Maximum y coordiante a tile can have.
        /// </summary>
        public const int MAX_HEIGHT = 100;

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

        // Layers
        public int Layer_Terrain;
        public int Layer_Entity;
        public int Layer_Path;

        public void Init(WorldData data)
        {
            Layer_Terrain = LayerMask.NameToLayer("Terrain");
            Layer_Entity = LayerMask.NameToLayer("Entity");
            Layer_Path = LayerMask.NameToLayer("Path");

            Name = data.Name;
            ChunkSize = data.ChunkSize;
            foreach(ChunkData chunkData in data.Chunks)
                Chunks.Add(new Vector2Int(chunkData.ChunkCoordinateX, chunkData.ChunkCoordinateY), Chunk.Load(this, chunkData));
            NodeIdCounter = data.MaxNodeId + 1;

            Pathfinder.Init(this);

            // Init connections
            foreach (Chunk chunk in Chunks.Values) chunk.UpdateFullPathfindingGraph();
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

        public void BuildSurfacePath(SurfaceNode node)
        {
            node.BuildPath();
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

        #endregion

        #region Draw

        /// <summary>
        /// Generates all meshes of the world
        /// </summary>
        public void Draw()
        {
            foreach (Chunk chunk in Chunks.Values) chunk.Draw();
        }

        /// <summary>
        /// Redraws all nodes around the given coordiates.
        /// </summary>
        public void RedrawNodesAround(Vector2Int worldCoordinates)
        {
            int range = 1;
            for (int y = worldCoordinates.y - range; y <= worldCoordinates.y + range; y++)
            {
                for (int x = worldCoordinates.x - range; x <= worldCoordinates.x + range; x++)
                {
                    foreach(BlockmapNode node in GetNodes(new Vector2Int(x, y)))
                    {
                        node.Draw();
                    }
                }
            }
        }

        public void ShowGrid(bool value)
        {

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
        public List<BlockmapNode> GetPathNodes(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetPathNodes(chunk.GetLocalCoordinates(worldCoordinates));
        }
        public List<AirPathSlopeNode> GetAirPathSlopeNodes(Vector2Int worldCoordinates)
        {
            if (!IsInWorld(worldCoordinates)) return null;

            Chunk chunk = GetChunk(worldCoordinates);
            return chunk.GetAirPathSlopeNodes(chunk.GetLocalCoordinates(worldCoordinates));
        }

        public Vector2Int WorldPositionToWorldCoordinates(Vector3 worldPosition)
        {
            Vector2Int worldCoords = new Vector2Int((int)worldPosition.x, (int)worldPosition.z);
            if (worldPosition.x < 0) worldCoords.x -= 1;
            if (worldPosition.z < 0) worldCoords.y -= 1;
            return worldCoords;
        }
        public Vector2Int WorldPositionToWorldCoordinates(Vector2 worldPosition2d)
        {
            return WorldPositionToWorldCoordinates(new Vector3(worldPosition2d.x, 0f, worldPosition2d.y));
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
            return GetPathNodes(GetWorldCoordinatesInDirection(worldCoordinates, dir));
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
            if (Physics.Raycast(new Vector3(worldPosition2d.x, 20f, worldPosition2d.y), -Vector3.up, out hit, 1000f, 1 << Layer_Terrain))
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
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(worldPosition2d.x, 20f, worldPosition2d.y), -Vector3.up, 1000f, 1 << Layer_Path);
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
            List<Chunk> candidateBlocks = Chunks.Values.ToList();
            Chunk chosenBlock = candidateBlocks[Random.Range(0, candidateBlocks.Count)];

            int x = Random.Range(0, ChunkSize);
            int y = Random.Range(0, ChunkSize);
            return chosenBlock.GetSurfaceNode(x, y);
        }

        #endregion

        #region Save / Load

        public static World Load(WorldData data)
        {
            GameObject worldObject = new GameObject("World");
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
