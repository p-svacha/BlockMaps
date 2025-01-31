using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    public class Chunk
    {

        /// <summary>
        /// The Unity GameObject of this world.
        /// </summary>
        public GameObject ChunkObject;

        public Vector2Int Coordinates { get; private set; } // Acts as the unique primary key for each chunk
        public int Size => World.ChunkSize;
        public World World { get; private set; }

        public List<BlockmapNode>[,] Nodes { get; private set; } // all nodes per local coordinate

        public GroundNode[,] GroundNodes { get; private set; }
        public List<AirNode>[,] AirNodes { get; private set; }
        public WaterNode[,] WaterNodes { get; private set; }

        /// <summary>
        /// All fences present in this chunk, grouped by local coordinates
        /// </summary>
        public Dictionary<Vector2Int, List<Fence>> Fences = new Dictionary<Vector2Int, List<Fence>>();

        /// <summary>
        /// All walls present in this chunk, grouped by local cell coordinates
        /// </summary>
        public Dictionary<Vector3Int, List<Wall>> Walls = new Dictionary<Vector3Int, List<Wall>>();

        /// <summary>
        /// All batch-rendered entities present in this chunk, grouped by local coordinates
        /// </summary>
        public Dictionary<Vector2Int, List<Entity>> BatchEntities = new Dictionary<Vector2Int, List<Entity>>();

        /// <summary>
        /// All entities that currently occupy at least one node on this chunk.
        /// </summary>
        private HashSet<Entity> Entities = new HashSet<Entity>();
        /// <summary>
        /// All zones that include at least one position in this chunk.
        /// </summary>
        private List<Zone> Zones = new List<Zone>();

        // Meshes (many types of things like nodes and fences are combined into one mesh per chunk to increase performance)
        public GroundMesh GroundMesh;
        public Dictionary<int, AirNodeMesh> AirNodeMeshes;
        public WaterMesh WaterMesh;
        public Dictionary<int, FenceMesh> FenceMeshes;
        public Dictionary<int, Dictionary<Direction, WallMesh>> WallMeshes; // 8 wall meshes per altitude level (1 per direction)
        public Dictionary<int, BatchEntityMesh> ProceduralEntityMeshes;

        // Performance Profilers
        static readonly ProfilerMarker pm_GenerateGroundNodeMesh = new ProfilerMarker("GenerateGroundNodeMesh");
        static readonly ProfilerMarker pm_GenerateWaterMesh = new ProfilerMarker("GenerateWaterMesh");
        static readonly ProfilerMarker pm_GenerateAirNodeMeshes = new ProfilerMarker("GenerateAirNodeMeshes");
        static readonly ProfilerMarker pm_GenerateFenceMeshes = new ProfilerMarker("GenerateFenceMeshes");
        static readonly ProfilerMarker pm_GenerateWallMeshes = new ProfilerMarker("GenerateWallMeshes");
        static readonly ProfilerMarker pm_GenerateProcEntityMeshes = new ProfilerMarker("GenerateProcEntityMeshes");

        public Chunk(World world, Vector2Int coordinates)
        {
            // Init general
            World = world;
            Coordinates = coordinates;

            // Init  nodes
            Nodes = new List<BlockmapNode>[Size, Size];
            AirNodes = new List<AirNode>[Size, Size];
            GroundNodes = new GroundNode[Size, Size];
            WaterNodes = new WaterNode[Size, Size];

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Nodes[x, y] = new List<BlockmapNode>();
                    AirNodes[x, y] = new List<AirNode>();
                }
            }
        }

        public void InitGameObjects()
        {
            ChunkObject = new GameObject("Chunk " + Coordinates.x + "/" + Coordinates.y);
            ChunkObject.transform.SetParent(World.WorldObject.transform);

            GameObject groundMeshObject = new GameObject("GroundMesh");
            GroundMesh = groundMeshObject.AddComponent<GroundMesh>();
            GroundMesh.Init(this);

            GameObject waterMeshObject = new GameObject("WaterMesh");
            WaterMesh = waterMeshObject.AddComponent<WaterMesh>();
            WaterMesh.Init(this);

            AirNodeMeshes = new Dictionary<int, AirNodeMesh>();
            FenceMeshes = new Dictionary<int, FenceMesh>();
            WallMeshes = new Dictionary<int, Dictionary<Direction, WallMesh>>();
            ProceduralEntityMeshes = new Dictionary<int, BatchEntityMesh>();
        }

        #region Actions

        public void RegisterNode(BlockmapNode node)
        {
            Nodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(node);
            if (node is GroundNode groundNode) GroundNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = groundNode;
            else if (node is WaterNode waterNode) WaterNodes[node.LocalCoordinates.x, node.LocalCoordinates.y] = waterNode;
            else if (node is AirNode airNode) AirNodes[node.LocalCoordinates.x, node.LocalCoordinates.y].Add(airNode);
        }

        public void AddEntity(Entity e)
        {
            Entities.Add(e);
        }
        public void RemoveEntity(Entity e)
        {
            Entities.Remove(e);
        }

        public void AddZone(Zone z)
        {
            Zones.Add(z);
        }
        public void RemoveZone(Zone z)
        {
            Zones.Remove(z);
        }

        public void RegisterFence(Fence f)
        {
            if (Fences.ContainsKey(f.LocalCoordinates)) Fences[f.LocalCoordinates].Add(f);
            else Fences.Add(f.LocalCoordinates, new List<Fence>() { f });
        }
        public void DeregisterFence(Fence f)
        {
            Fences[f.LocalCoordinates].Remove(f);
        }
        public void RegisterWall(Wall w)
        {
            Vector3Int localCoords = w.LocalCellCoordinates;
            if (Walls.ContainsKey(localCoords)) Walls[localCoords].Add(w);
            else Walls.Add(localCoords, new List<Wall>() { w });
        }
        public void DeregisterWall(Wall w)
        {
            Walls[w.LocalCellCoordinates].Remove(w);
        }
        public void RegisterBatchEntity(Entity e)
        {
            if (BatchEntities.ContainsKey(e.OriginNode.LocalCoordinates)) BatchEntities[e.OriginNode.LocalCoordinates].Add(e);
            else BatchEntities.Add(e.OriginNode.LocalCoordinates, new List<Entity>() { e });
        }
        public void DeregisterBatchEntity(Entity e)
        {
            BatchEntities[e.OriginNode.LocalCoordinates].Remove(e);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Generates all meshes for this chunk
        /// </summary>
        public void DrawMeshes()
        {
            // Ground mesh
            pm_GenerateGroundNodeMesh.Begin();
            GroundMesh.Draw();
            pm_GenerateGroundNodeMesh.End();

            // Water mesh
            pm_GenerateWaterMesh.Begin();
            WaterMesh.Draw();
            pm_GenerateWaterMesh.End();

            // Meshes per height level
            pm_GenerateAirNodeMeshes.Begin();
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) GameObject.Destroy(mesh.gameObject);
            AirNodeMeshes = AirNodeMesh.GenerateAirNodeMeshes(this);
            pm_GenerateAirNodeMeshes.End();

            pm_GenerateFenceMeshes.Begin();
            foreach (FenceMesh mesh in FenceMeshes.Values) GameObject.Destroy(mesh.gameObject);
            FenceMeshes = FenceMeshGenerator.GenerateMeshes(this);
            pm_GenerateFenceMeshes.End();

            pm_GenerateWallMeshes.Begin();
            foreach (var altitudeLevel in WallMeshes.Values)
            {
                foreach (WallMesh mesh in altitudeLevel.Values)
                {
                    GameObject.Destroy(mesh.gameObject);
                }
            }
            WallMeshes = WallMeshGenerator.GenerateMeshes(this);
            pm_GenerateWallMeshes.End();

            pm_GenerateProcEntityMeshes.Begin();
            foreach (BatchEntityMesh mesh in ProceduralEntityMeshes.Values) GameObject.Destroy(mesh.gameObject);
            ProceduralEntityMeshes = BatchEntityMeshGenerator.GenerateMeshes(this);
            pm_GenerateProcEntityMeshes.End();

            // Chunk position
            ChunkObject.transform.position = new Vector3(Coordinates.x * Size, 0f, Coordinates.y * Size);
        }

        /// <summary>
        /// Updates the visible nodes on this chunk according to the vision of the specified player.
        /// <br/> Does not change the vision values (SeenBy, ExploredBy) of the nodes, only accesses them.
        /// </summary>
        public void SetVisibility(Actor actor)
        {
            // Node visibility
            GroundMesh.SetVisibility(actor);
            WaterMesh.SetVisibility(actor);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.SetVisibility(actor);
            foreach (FenceMesh mesh in FenceMeshes.Values) mesh.SetVisibility(actor);
            foreach (BatchEntityMesh mesh in ProceduralEntityMeshes.Values) mesh.SetVisibility(actor);
            foreach (var altitudeLevel in WallMeshes.Values)
                foreach (WallMesh mesh in altitudeLevel.Values)
                    mesh.SetVisibility(actor);

            // Entity visibility
            foreach (Entity e in Entities) e.UpdateVisibility(actor);
        }

        public void ShowGrid(bool show)
        {
            GroundMesh.ShowGrid(show);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.ShowGrid(show);
            WaterMesh.ShowGrid(show);
        }
        public void ShowTextures(bool show)
        {
            GroundMesh.ShowTextures(show);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.ShowTextures(show);
            foreach (FenceMesh mesh in FenceMeshes.Values) mesh.ShowTextures(show);
            foreach (BatchEntityMesh mesh in ProceduralEntityMeshes.Values) mesh.ShowTextures(show);
            foreach (Entity entity in Entities.Where(x => x.IsStandaloneEntity)) entity.ShowTextures(show);
            WaterMesh.ShowTextures(show);
            foreach (var altitudeLevel in WallMeshes.Values)
                foreach (WallMesh mesh in altitudeLevel.Values)
                    mesh.ShowTextures(show);
        }
        public void ShowTileBlending(bool show)
        {
            GroundMesh.ShowTileBlending(show);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.ShowTileBlending(show);
        }

        public void DrawZoneBorders(Actor actor)
        {
            // Combine all visible zones on this chunk into 1 combines zone border list that contains a bool[4] (1 for each direction) for each node that state if a border should be drawn there.
            bool[][] combinedBorders = new bool[256][];
            int[] combinedBorderColors = new int[256];

            for (int i = 0; i < 256; i++)
            {
                combinedBorders[i] = new bool[4];
            }

            List<Zone> visibleZones = Zones.Where(x => x.CanBeSeenBy(actor)).ToList();
            foreach (Zone z in visibleZones)
            {
                List<bool[]> nodeBorders = z.GetChunkZoneBorders(this);
                for (int i = 0; i < 256; i++) // for each 2d position on chunk
                {
                    for (int j = 0; j < 4; j++) // for each border direction
                    {
                        if (nodeBorders[i][j] == true)
                        {
                            combinedBorders[i][j] = true;
                            combinedBorderColors[i] = z.Actor.Id;
                        }
                    }
                }

            }

            // Translate the bool[4] list into a float array that the shaders can use
            float[] zoneBorderArray = new float[256];
            for(int i = 0; i < 256; i++)
            {
                int shaderArrayValue = 0;
                if (combinedBorders[i][0]) shaderArrayValue += 1000;
                if (combinedBorders[i][1]) shaderArrayValue += 100;
                if (combinedBorders[i][2]) shaderArrayValue += 10;
                if (combinedBorders[i][3]) shaderArrayValue += 1;
                zoneBorderArray[i] = shaderArrayValue;
            }

            // Translate color array with actor id's into float array that the shaders can use
            float[] zoneBorderColors = combinedBorderColors.Select(x => (float)x).ToArray();

            // Pass array to all meshes
            GroundMesh.ShowZoneBorders(zoneBorderArray, zoneBorderColors);
            foreach (AirNodeMesh mesh in AirNodeMeshes.Values) mesh.ShowZoneBorders(zoneBorderArray, zoneBorderColors);
            WaterMesh.ShowZoneBorders(zoneBorderArray, zoneBorderColors);
        }

        #endregion

        #region Getters

        public List<BlockmapNode> GetAllNodes()
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    List<BlockmapNode> coordinateNodes = GetNodes(x, y);
                    foreach(BlockmapNode node in coordinateNodes)
                    {
                        nodes.Add(node);
                    }
                }
            }
            return nodes;
        }
        public List<BlockmapNode> GetNodes(int altitude)
        {
            List<BlockmapNode> nodes = new List<BlockmapNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    foreach (BlockmapNode node in GetNodes(x, y))
                        if (node.BaseAltitude == altitude)
                            nodes.Add(node);
            return nodes;
        }
        public List<BlockmapNode> GetNodes(int x, int y)
        {
            return new List<BlockmapNode>(Nodes[x, y]);
        }
        public List<BlockmapNode> GetNodes(Vector2Int localCoordinates)
        {
            return GetNodes(localCoordinates.x, localCoordinates.y);
        }

        public List<GroundNode> GetAllGroundNodes()
        {
            List<GroundNode> nodes = new List<GroundNode>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    nodes.Add(GroundNodes[x, y]);
            return nodes;
        }
        public GroundNode GetGroundNode(int x, int y)
        {
            return GroundNodes[x, y];
        }
        public GroundNode GetGroundNode(Vector2Int localCoordinates)
        {
            return GetGroundNode(localCoordinates.x, localCoordinates.y);
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
                        if (node.BaseAltitude == heightLevel)
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


        public Wall GetWall(Vector3Int localCellCoordinates, Direction side)
        {
            if (Walls.TryGetValue(localCellCoordinates, out List<Wall> walls))
                return walls.FirstOrDefault(x => x.Side == side);
            return null;
        }
        public List<Wall> GetWalls(Vector3Int localCellCoordinates)
        {
            if (!Walls.ContainsKey(localCellCoordinates)) return new List<Wall>();
            return Walls[localCellCoordinates];
        }
        public List<Wall> GetWalls(int altitude)
        {
            return Walls.Where(x => x.Key.y == altitude).Select(x => x.Value).SelectMany(x => x).ToList();
        }
        public List<Wall> GetWalls(int altitude, Direction side)
        {
            return Walls.Where(x => x.Key.y == altitude).Select(x => x.Value.Where(w => w.Side == side)).SelectMany(x => x).ToList();
        }

        public List<Fence> GetFences(int altitude)
        {
            return Fences.Select(x => x.Value).SelectMany(x => x).Where(x => x.Node.BaseAltitude == altitude).ToList();
        }
        public HashSet<Entity> GetAllEntities() => Entities;
        public List<Entity> GetBatchEntities(int altitude)
        {
            return BatchEntities.Select(x => x.Value).SelectMany(x => x).Where(x => x.OriginNode.BaseAltitude == altitude).ToList();
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

        public Vector3 WorldPosition => new Vector3(Coordinates.x * Size, 0f, Coordinates.y * Size);

        #endregion

        #region Save / Load
        /*
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
        */

        #endregion
    }
}
