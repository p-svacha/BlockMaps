using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A mesh that contains all geometry for all nodes within one height level in a chunk.
    /// </summary>
    public class AirNodeMesh : ChunkMesh
    {
        public int HeightLevel { get; private set; }

        public void Init(Chunk chunk, int level)
        {
            OnInit(chunk);

            HeightLevel = level;
            gameObject.layer = chunk.World.Layer_AirNode;
        }

        public override void OnMeshApplied()
        {
            Material surfaceMaterial = GetComponent<MeshRenderer>().materials.FirstOrDefault(x => x.shader.name == "Custom/SurfaceShader");
            if (surfaceMaterial == null) return;

            // Set surface values for surface materials (textures per node and blending)
            List<float> surfaceArray = new List<float>();
            Dictionary<Direction, List<float>> surfaceBlendArrays = new Dictionary<Direction, List<float>>();
            foreach (Direction dir in HelperFunctions.GetAllDirections8()) surfaceBlendArrays.Add(dir, new List<float>());

            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    // Get node
                    AirNode node = World.GetAirNodes(Chunk.GetWorldCoordinates(new Vector2Int(x, y)), HeightLevel).OrderBy(x => x.MaxHeight).FirstOrDefault();

                    // Base surface
                    int surfaceId = node == null ? -1 : (int)node.Surface.Id;
                    surfaceArray.Add(surfaceId);

                    // Blend for each direction
                    foreach (Direction dir in HelperFunctions.GetAllDirections8())
                    {
                        if (DoBlend(node, dir, out int blendSurfaceId)) surfaceBlendArrays[dir].Add(blendSurfaceId);
                        else surfaceBlendArrays[dir].Add(surfaceId);
                    }
                }
            }

            // Set blend values for surface material only
            surfaceMaterial.SetFloatArray("_TileSurfaces", surfaceArray);
            surfaceMaterial.SetFloatArray("_TileBlend_W", surfaceBlendArrays[Direction.W]);
            surfaceMaterial.SetFloatArray("_TileBlend_E", surfaceBlendArrays[Direction.E]);
            surfaceMaterial.SetFloatArray("_TileBlend_N", surfaceBlendArrays[Direction.N]);
            surfaceMaterial.SetFloatArray("_TileBlend_S", surfaceBlendArrays[Direction.S]);
            surfaceMaterial.SetFloatArray("_TileBlend_NW", surfaceBlendArrays[Direction.NW]);
            surfaceMaterial.SetFloatArray("_TileBlend_NE", surfaceBlendArrays[Direction.NE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SE", surfaceBlendArrays[Direction.SE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SW", surfaceBlendArrays[Direction.SW]);
        }

        /// <summary>
        /// Returns if a source node should have the surface of an adjacent node in a given direction blended into it.
        /// </summary>
        private bool DoBlend(BlockmapNode sourceNode, Direction dir, out int blendSurfaceId)
        {
            blendSurfaceId = -1;

            if (sourceNode == null) return false;
            if (!sourceNode.GetSurface().DoBlend) return false; // No blend on this node

            List<BlockmapNode> adjacentNodes = World.GetAdjacentNodes(sourceNode.WorldCoordinates, dir);
            foreach(BlockmapNode adjNode in adjacentNodes)
            {
                if (!World.DoAdjacentHeightsMatch(sourceNode, adjNode, dir)) continue;
                if (adjNode.GetSurface() == null) continue;
                if (!adjNode.GetSurface().DoBlend) continue;

                blendSurfaceId = (int)(adjNode.GetSurface().Id);
                return true;
            }
            return false;
        }


        public override void SetVisibility(Actor player)
        {
            // Set renderer
            if(Renderer == null) Renderer = GetComponent<MeshRenderer>();

            // Define visibility array
            List<float> visibilityArray = new List<float>();
            for (int x = -1; x <= Chunk.Size; x++)
            {
                for (int y = -1; y <= Chunk.Size; y++)
                {
                    int visibility = 0; // 0 = unexplored

                    Vector2Int localCoordinates = new Vector2Int(x, y);
                    Vector2Int worldCoordiantes = Chunk.GetWorldCoordinates(localCoordinates);

                    List<AirNode> nodes = Chunk.World.GetAirNodes(worldCoordiantes, HeightLevel).ToList();

                    if (nodes.Any(x => x.IsVisibleBy(player))) visibility = 2; // 2 = visible
                    else if (nodes.Any(x => x.IsExploredBy(player))) visibility = 1; // 1 = fog of war

                    visibilityArray.Add(visibility);
                }
            }


            // Pass visibility array to shader
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloat("_FullVisibility", 0);
                Renderer.materials[i].SetFloatArray("_TileVisibility", visibilityArray);
            }
        }

        #region Generator

        /// <summary>
        /// Generates all AirNodeMeshes for a single chunk. (one per height level)
        /// </summary>
        public static Dictionary<int, AirNodeMesh> GenerateAirNodeMeshes(Chunk chunk)
        {
            Dictionary<int, AirNodeMesh> meshes = new Dictionary<int, AirNodeMesh>();

            for (int heightLevel = 0; heightLevel < World.MAX_HEIGHT; heightLevel++)
            {
                List<AirNode> nodesToDraw = chunk.GetAirNodes(heightLevel);
                if (nodesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("AirNodeMesh_" + heightLevel);
                AirNodeMesh mesh = meshObject.AddComponent<AirNodeMesh>();
                mesh.Init(chunk, heightLevel);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (AirNode airNode in nodesToDraw)
                {
                    airNode.Draw(meshBuilder);
                    airNode.SetMesh(mesh);
                }
                meshBuilder.ApplyMesh();

                // Set chunk values for all materials
                MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    renderer.materials[i].SetFloat("_ChunkSize", chunk.Size);
                    renderer.materials[i].SetFloat("_ChunkCoordinatesX", chunk.Coordinates.x);
                    renderer.materials[i].SetFloat("_ChunkCoordinatesY", chunk.Coordinates.y);
                }

                meshes.Add(heightLevel, mesh);

                mesh.OnMeshApplied();
            }

            return meshes;
        }

        #endregion
    }
}
