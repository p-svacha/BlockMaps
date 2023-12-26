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
        public GameObject SurfaceMesh;
        public AirNodeMesh[] AirNodeMesh;


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
            SurfaceMesh = new GameObject("SurfaceMesh");
            SurfaceMesh.layer = World.Layer_SurfaceNode;
            SurfaceMesh.transform.SetParent(transform);

            AirNodeMesh = new AirNodeMesh[World.MAX_HEIGHT];
            for(int i = 0; i < World.MAX_HEIGHT; i++)
            {
                GameObject obj = new GameObject("AirNodeMesh_" + i);
                AirNodeMesh[i] = obj.AddComponent<AirNodeMesh>();
                AirNodeMesh[i].Init(this, i);
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
            MeshBuilder surfaceMeshBuilder = new MeshBuilder(SurfaceMesh);
            surfaceMeshBuilder.AddNewSubmesh(ResourceManager.Singleton.SurfaceMaterial); // Submesh 0: surface
            surfaceMeshBuilder.AddNewSubmesh(ResourceManager.Singleton.CliffMaterial); // Submesh 1: cliffs
            List<float> surfaceArray = new List<float>(); // for shader
            List<float> surfaceBlend_W = new List<float>(); 
            List<float> surfaceBlend_N = new List<float>(); 
            List<float> surfaceBlend_S = new List<float>();
            List<float> surfaceBlend_E = new List<float>();
            List<float> surfaceBlend_NW = new List<float>();
            List<float> surfaceBlend_NE = new List<float>();
            List<float> surfaceBlend_SW = new List<float>();
            List<float> surfaceBlend_SE = new List<float>();

            foreach (SurfaceNode node in GetAllSurfaceNodes())
            {
                // Generate mesh
                node.Draw(surfaceMeshBuilder);

                // Get surface value for shader
                int surfaceId = (int)node.Surface.Id;
                surfaceArray.Add(surfaceId);

                // Blend west
                if (node.ConnectedNodes.TryGetValue(Direction.W, out BlockmapNode westNode)) surfaceBlend_W.Add((int)westNode.Surface.Id);
                else surfaceBlend_W.Add(surfaceId);
                // Blend north
                if (node.ConnectedNodes.TryGetValue(Direction.N, out BlockmapNode northNode)) surfaceBlend_N.Add((int)northNode.Surface.Id);
                else surfaceBlend_N.Add(surfaceId);
                // Blend south
                if (node.ConnectedNodes.TryGetValue(Direction.S, out BlockmapNode southNode)) surfaceBlend_S.Add((int)southNode.Surface.Id);
                else surfaceBlend_S.Add(surfaceId);
                // Blend east
                if (node.ConnectedNodes.TryGetValue(Direction.E, out BlockmapNode eastNode)) surfaceBlend_E.Add((int)eastNode.Surface.Id);
                else surfaceBlend_E.Add(surfaceId);

                // Blend nw
                if (node.ConnectedNodes.TryGetValue(Direction.NW, out BlockmapNode nwNode)) surfaceBlend_NW.Add((int)nwNode.Surface.Id);
                else surfaceBlend_NW.Add(surfaceId);
                // Blend ne
                if (node.ConnectedNodes.TryGetValue(Direction.NE, out BlockmapNode neNode)) surfaceBlend_NE.Add((int)neNode.Surface.Id);
                else surfaceBlend_NE.Add(surfaceId);
                // Blend se
                if (node.ConnectedNodes.TryGetValue(Direction.SE, out BlockmapNode seNode)) surfaceBlend_SE.Add((int)seNode.Surface.Id);
                else surfaceBlend_SE.Add(surfaceId);
                // Blend sw
                if (node.ConnectedNodes.TryGetValue(Direction.SW, out BlockmapNode swNode)) surfaceBlend_SW.Add((int)swNode.Surface.Id);
                else surfaceBlend_SW.Add(surfaceId);
            }
            surfaceMeshBuilder.ApplyMesh();

            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloat("_ChunkSize", Size);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileSurfaces", surfaceArray);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_W", surfaceBlend_W);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_E", surfaceBlend_E);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_N", surfaceBlend_N);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_S", surfaceBlend_S);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_NW", surfaceBlend_NW);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_NE", surfaceBlend_NE);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_SE", surfaceBlend_SE);
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloatArray("_TileBlend_SW", surfaceBlend_SW);

            // Air node meshes
            foreach (AirNodeMesh mesh in AirNodeMesh) mesh.Draw();

            // Chunk position
            transform.position = new Vector3(Coordinates.x * Size, 0f, Coordinates.y * Size);
        }

        public void ShowGrid(bool show)
        {
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloat("_ShowGrid", show ? 1 : 0);
        }
        public void ShowTextures(bool show)
        {
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloat("_UseTextures", show ? 1 : 0);
        }
        public void ShowTileBlending(bool show)
        {
            SurfaceMesh.GetComponent<MeshRenderer>().material.SetFloat("_BlendThreshhold", show ? 0.4f : 0);
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
