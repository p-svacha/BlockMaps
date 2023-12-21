using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class Chunk : MonoBehaviour
    {
        public int Size { get; private set; }
        public World World { get; private set; }
        public Vector2Int Coordinates { get; private set; }

        /// <summary>
        /// Each tile on the chunk has a list of nodes, whereas the first element is always the surface node and all possible further nodes are PathNodes.
        /// </summary>
        public List<BlockmapNode>[,] Nodes { get; private set; }
        public bool IsOwned { get; private set; } // todo: remove from base class


        /// <summary>
        /// Initializes the block to get all relevant data. Only call this once.
        /// </summary>
        public void Init(World world, ChunkData data)
        {
            // Init general
            Size = world.ChunkSize;
            World = world;
            Coordinates = new Vector2Int(data.ChunkCoordinateX, data.ChunkCoordinateY);

            // Init  nodes
            Nodes = new List<BlockmapNode>[Size, Size];
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    Nodes[x, y] = new List<BlockmapNode>();

            foreach (NodeData nodeData in data.Nodes)
            {
                BlockmapNode node = BlockmapNode.Load(world, this, nodeData);
                Nodes[nodeData.LocalCoordinateX, nodeData.LocalCoordinateY].Add(node);
            }

            transform.position = new Vector3(Coordinates.x * Size, 0f, Coordinates.y * Size);
        }

        /// <summary>
        /// Updates the connected nodes in all nodes on this block
        /// </summary>
        public void UpdateFullPathfindingGraph()
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    foreach (BlockmapNode node in Nodes[x, y]) node.UpdateConnectedNodesStraight();

            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    foreach (BlockmapNode node in Nodes[x, y]) node.UpdateConnectedNodesDiagonal();
        }

        #region Draw

        public void Draw()
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    foreach(BlockmapNode node in Nodes[x,y])
                    {
                        node.Draw();
                    }
                }
            }
        }

        #endregion

        #region Ownership

        public bool CanAquire()
        {
            if (IsOwned) return false;
            Vector2Int north = Coordinates + new Vector2Int(0, 1);
            bool isNorthBlockOwned = (World.Chunks.ContainsKey(north) && World.Chunks[north].IsOwned);
            Vector2Int south = Coordinates + new Vector2Int(0, -1);
            bool isSouthBlockOwned = (World.Chunks.ContainsKey(south) && World.Chunks[south].IsOwned);
            Vector2Int east = Coordinates + new Vector2Int(1, 0);
            bool isEastBlockOwned = (World.Chunks.ContainsKey(east) && World.Chunks[east].IsOwned);
            Vector2Int west = Coordinates + new Vector2Int(-1, 0);
            bool isWestBlockOwned = (World.Chunks.ContainsKey(west) && World.Chunks[west].IsOwned);
            if (!isNorthBlockOwned && !isSouthBlockOwned && !isEastBlockOwned && !isWestBlockOwned) return false;

            return true;
        }

        public void Aquire()
        {
            IsOwned = true;
        }

        #endregion

        #region Getters

        public List<BlockmapNode> GetNodes(int x, int y)
        {
            return Nodes[x, y];
        }
        public List<BlockmapNode> GetNodes(Vector2Int localCoordinates)
        {
            return GetNodes(localCoordinates.x, localCoordinates.y);
        }
        public List<SurfaceNode> GetSurfaceNodes()
        {
            List<SurfaceNode> nodes = new List<SurfaceNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    nodes.Add(GetSurfaceNode(x,y));
            return nodes;
        }
        public SurfaceNode GetSurfaceNode(int x, int y)
        {
            return (SurfaceNode)Nodes[x, y][0];
        }
        public SurfaceNode GetSurfaceNode(Vector2Int localCoordinates)
        {
            return GetSurfaceNode(localCoordinates.x, localCoordinates.y);
        }
        public List<BlockmapNode> GetPathNodes(int x, int y)
        {
            return Nodes[x, y].Skip(1).ToList();
        }
        public List<BlockmapNode> GetPathNodes(Vector2Int localCoordinates)
        {
            return GetPathNodes(localCoordinates.x, localCoordinates.y);
        }
        public List<AirPathSlopeNode> GetAirPathSlopeNodes(int x, int y)
        {
            return Nodes[x, y].Where(x => x.Type == NodeType.AirPathSlope).Select(x => (AirPathSlopeNode)x).ToList();
        }
        public List<AirPathSlopeNode> GetAirPathSlopeNodes(Vector2Int localCoordinates)
        {
            return GetAirPathSlopeNodes(localCoordinates.x, localCoordinates.y);
        }

        public Vector2Int GetLocalCoordinates(Vector2Int worldCoordinates)
        {
            int localX = HelperFunctions.Mod(worldCoordinates.x, Size);
            int localY = HelperFunctions.Mod(worldCoordinates.y, Size);
            return new Vector2Int(localX, localY);
        }
        public Vector2Int GetWorldCoordinates(Vector2Int localCoordinates)
        {
            int worldX = Coordinates.x * Size + localCoordinates.x;
            int worldY = Coordinates.y * Size + localCoordinates.y;
            return new Vector2Int(worldX, worldY);
        }

        #endregion

        #region Save / Load

        public static Chunk Load(World world, ChunkData data)
        {
            GameObject chunkObject = new GameObject("Chunk");
            chunkObject.transform.SetParent(world.transform);
            Chunk chunk = chunkObject.AddComponent<Chunk>();

            chunk.Init(world, data);
            return chunk;
        }

        public ChunkData Save()
        {
            List<NodeData> nodeData = new List<NodeData>();
            for(int x = 0; x < Nodes.GetLength(0); x++)
            {
                for(int y = 0; y < Nodes.GetLength(1); x++)
                {
                    foreach (BlockmapNode node in Nodes[x, y]) nodeData.Add(node.Save());
                }
            }


            return new ChunkData
            {
                ChunkCoordinateX = Coordinates.x,
                ChunkCoordinateY = Coordinates.y,
                Nodes = nodeData
            };
        }

        #endregion
    }
}
