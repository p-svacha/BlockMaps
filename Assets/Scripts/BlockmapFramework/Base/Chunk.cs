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

        // Meshes (each chunk consists of a surface mesh and one air node mesh per level.
        public SurfaceMesh SurfaceMesh;
        public AirNodeMesh[] AirNodeMeshes;


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

            // Init meshes
            GameObject surfaceMeshObject = new GameObject("SurfaceMesh");
            SurfaceMesh = surfaceMeshObject.AddComponent<SurfaceMesh>();
            SurfaceMesh.Init(this);

            AirNodeMeshes = new AirNodeMesh[World.MAX_HEIGHT];
            for(int i = 0; i < World.MAX_HEIGHT; i++)
            {
                GameObject obj = new GameObject("AirNodeMesh_" + i);
                AirNodeMeshes[i] = obj.AddComponent<AirNodeMesh>();
                AirNodeMeshes[i].Init(this, i);
            }
        }

        /// <summary>
        /// Updates the connected nodes in Directions W,E,S,N for all nodes in this chunk.
        /// </summary>
        public void UpdatePathfindingGraphStraight()
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    foreach (BlockmapNode node in Nodes[x, y]) node.UpdateConnectedNodesStraight();
        }

        /// <summary>
        /// Updates the connected nodes in Directions NW,NE,SW,SE for all nodes in this chunk.
        /// <br/> This function requires UpdatePathfindingGraphStraight() to be called on all chunks before. Else it won't work correctly
        /// </summary>
        public void UpdatePathfindingGraphDiagonal()
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    foreach (BlockmapNode node in Nodes[x, y]) node.UpdateConnectedNodesDiagonal();
        }

        #region Draw

        /// <summary>
        /// Generates a single mesh for this chunk
        /// </summary>
        public void Draw()
        {
            // Surface mesh
            SurfaceMesh.Draw();

            // Air node meshes
            foreach (AirNodeMesh mesh in AirNodeMeshes) mesh.Draw();

            // Chunk position
            transform.position = new Vector3(Coordinates.x * Size, 0f, Coordinates.y * Size);
        }

        /// <summary>
        /// Updates the visible nodes on this chunk according to the vision of the specified player.
        /// <br/> Does not change the vision values (SeenBy, ExploredBy) of the nodes, only accesses them.
        /// </summary>
        public void SetVisibility(Player player)
        {
            SurfaceMesh.SetVisibility(player);
            foreach (AirNodeMesh mesh in AirNodeMeshes) mesh.SetVisibility(player);
        }

        public void ShowGrid(bool show)
        {
            SurfaceMesh.ShowGrid(show);
            foreach (AirNodeMesh mesh in AirNodeMeshes) mesh.ShowGrid(show);
        }
        public void ShowTextures(bool show)
        {
            SurfaceMesh.ShowTextures(show);
            foreach (AirNodeMesh mesh in AirNodeMeshes) mesh.ShowTextures(show);
        }

        public void ShowTileBlending(bool show)
        {
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloat("_BlendThreshhold", show ? 0.4f : 0);
        }

        #endregion

        #region Getters

        public List<BlockmapNode> GetAllNodes()
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    nodes.AddRange(GetNodes(x, y));
            return nodes;
        }
        public List<BlockmapNode> GetNodes(int x, int y)
        {
            return Nodes[x, y];
        }
        public List<BlockmapNode> GetNodes(Vector2Int localCoordinates)
        {
            return GetNodes(localCoordinates.x, localCoordinates.y);
        }

        public List<SurfaceNode> GetAllSurfaceNodes()
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

        public List<BlockmapNode> GetAllAirNodes()
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    foreach (BlockmapNode node in GetAirNodes(x, y))
                        nodes.Add(node);
            return nodes;
        }
        public List<BlockmapNode> GetAllAirNodes(int heightLevel)
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    foreach (BlockmapNode node in GetAirNodes(x, y))
                        if (node.BaseHeight == heightLevel)
                            nodes.Add(node);
            return nodes;
        }
        public List<BlockmapNode> GetAirNodes(int x, int y)
        {
            return Nodes[x, y].Skip(1).ToList();
        }
        public List<BlockmapNode> GetAirNodes(Vector2Int localCoordinates)
        {
            return GetAirNodes(localCoordinates.x, localCoordinates.y);
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
            GameObject chunkObject = new GameObject("Chunk " + data.ChunkCoordinateX + "/" + data.ChunkCoordinateY);
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
