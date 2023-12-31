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

        public List<BlockmapNode>[,] Nodes { get; private set; } // all nodes per local coordinate

        public SurfaceNode[,] SurfaceNodes { get; private set; }
        public List<AirNode>[,] AirNodes { get; private set; }
        public WaterNode[,] WaterNodes { get; private set; }

        /// <summary>
        /// All entities that currently occupy at least one node on this chunk.
        /// </summary>
        private HashSet<Entity> Entities = new HashSet<Entity>();

        // Meshes (many types of things like nodes and walls are combined into one mesh per chunk to increase performance)
        public SurfaceMesh SurfaceMesh;
        public Dictionary<int, AirNodeMesh> AirNodeMeshes;
        public WaterMesh WaterMesh;
        public Dictionary<int, WallMesh> WallMeshes;

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
            AirNodes = new List<AirNode>[Size, Size];
            SurfaceNodes = new SurfaceNode[Size, Size];
            WaterNodes = new WaterNode[Size, Size];

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Nodes[x, y] = new List<BlockmapNode>();
                    AirNodes[x, y] = new List<AirNode>();
                }
            }

            foreach (NodeData nodeData in data.Nodes)
            {
                BlockmapNode node = BlockmapNode.Load(world, this, nodeData);
                World.RegisterNode(node);
            }

            // Init meshes
            GameObject surfaceMeshObject = new GameObject("SurfaceMesh");
            SurfaceMesh = surfaceMeshObject.AddComponent<SurfaceMesh>();
            SurfaceMesh.Init(this);

            GameObject waterMeshObject = new GameObject("WaterMesh");
            WaterMesh = waterMeshObject.AddComponent<WaterMesh>();
            WaterMesh.Init(this);

            AirNodeMeshes = new Dictionary<int, AirNodeMesh>();
            WallMeshes = new Dictionary<int, WallMesh>();
        }

        #region Actions

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

        public void AddEntity(Entity e)
        {
            Entities.Add(e);
        }
        public void RemoveEntity(Entity e)
        {
            Entities.Remove(e);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Generates a single mesh for this chunk
        /// </summary>
        public void DrawMesh()
        {
            // Surface mesh
            SurfaceMesh.Draw();

            // Water mesh
            WaterMesh.Draw();

            // Meshes per height level
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) Destroy(mesh.gameObject);
            AirNodeMeshes = AirNodeMeshGenerator.GenerateMeshes(this);

            foreach (WallMesh mesh in WallMeshes.Values) Destroy(mesh.gameObject);
            WallMeshes = WallMeshGenerator.GenerateMeshes(this);

            // Chunk position
            transform.position = new Vector3(Coordinates.x * Size, 0f, Coordinates.y * Size);
        }

        /// <summary>
        /// Updates the visible nodes on this chunk according to the vision of the specified player.
        /// <br/> Does not change the vision values (SeenBy, ExploredBy) of the nodes, only accesses them.
        /// </summary>
        public void SetVisibility(Player player)
        {
            // Node visibility
            SurfaceMesh.SetVisibility(player);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.SetVisibility(player);
            foreach (WallMesh mesh in WallMeshes.Values) mesh.SetVisibility(player);
            WaterMesh.SetVisibility(player);

            // Entity visibility
            foreach(Entity e in Entities)
            {
                e.UpdateVisiblity(player);
            }
        }

        public void ShowGrid(bool show)
        {
            SurfaceMesh.ShowGrid(show);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.ShowGrid(show);
            WaterMesh.ShowGrid(show);
        }
        public void ShowTextures(bool show)
        {
            SurfaceMesh.ShowTextures(show);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.ShowTextures(show);
            foreach (WallMesh mesh in WallMeshes.Values) mesh.ShowTextures(show);
            WaterMesh.ShowTextures(show);
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
        public List<BlockmapNode> GetNodes(int heightLevel)
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    foreach (BlockmapNode node in GetNodes(x, y))
                        if (node.BaseHeight == heightLevel)
                            nodes.Add(node);
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
            return SurfaceNodes[x, y];
        }
        public SurfaceNode GetSurfaceNode(Vector2Int localCoordinates)
        {
            return GetSurfaceNode(localCoordinates.x, localCoordinates.y);
        }

        public List<WaterNode> GetAllWaterNodes()
        {
            List<WaterNode> nodes = new List<WaterNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if(WaterNodes[x,y] != null)
                        nodes.Add(WaterNodes[x, y]);
            return nodes;
        }
        public WaterNode GetWaterNode(int x, int y)
        {
            return WaterNodes[x, y];
        }
        public WaterNode GetWaterNode(Vector2Int localCoordinates)
        {
            return GetWaterNode(localCoordinates.x, localCoordinates.y);
        }

        public List<AirNode> GetAllAirNodes()
        {
            List<AirNode> nodes = new List<AirNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    foreach (AirNode node in GetAirNodes(x, y))
                        nodes.Add(node);
            return nodes;
        }
        public List<AirNode> GetAirNodes(int heightLevel)
        {
            List<AirNode> nodes = new List<AirNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    foreach (AirNode node in GetAirNodes(x, y))
                        if (node.BaseHeight == heightLevel)
                            nodes.Add(node);
            return nodes;
        }
        public List<AirNode> GetAirNodes(int x, int y)
        {
            return AirNodes[x, y];
        }
        public List<AirNode> GetAirNodes(Vector2Int localCoordinates)
        {
            return GetAirNodes(localCoordinates.x, localCoordinates.y);
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
                for(int y = 0; y < Nodes.GetLength(1); y++)
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
