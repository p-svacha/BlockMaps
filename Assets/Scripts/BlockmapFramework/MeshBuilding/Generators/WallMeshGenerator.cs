using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class WallMeshGenerator
    {
        /// <summary>
        /// Generates the meshes for all altitude levels of a chunk
        /// </summary>
        public static Dictionary<int, WallMesh> GenerateMeshes(Chunk chunk)
        {
            Dictionary<int, WallMesh> meshes = new Dictionary<int, WallMesh>();

            for (int altitude = 0; altitude < World.MAX_ALTITUDE; altitude++)
            {
                List<Wall> wallsToDraw = chunk.GetWalls(altitude);
                if (wallsToDraw.Count == 0) continue;

                /*
                List<BlockmapNode> nodesToDraw = chunk.GetNodes(heightLevel).Where(x => x.HasFence).ToList();
                if (nodesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("FenceMesh_" + heightLevel);
                FenceMesh mesh = meshObject.AddComponent<FenceMesh>();
                mesh.Init(chunk, heightLevel);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (BlockmapNode node in nodesToDraw)
                {
                    foreach (Fence fence in node.Fences.Values) DrawFence(meshBuilder, fence.Type, fence.Node, fence.Side, fence.Height);
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
                */
            }

            return meshes;
        }

        /// <summary>
        /// Adds the mesh of a single wall piece to a MeshBuilder.
        /// </summary>
        public static void DrawWall(MeshBuilder meshBuilder, Vector3Int localCellPosition, Direction side, WallShape shape, WallMaterial material, bool isPreview = false)
        {
            shape.GenerateMesh(meshBuilder, localCellPosition, side, GetMaterial(material.Material, isPreview));
        }

        private static Material GetMaterial(Material mat, bool isPreview)
        {
            if (isPreview) return ResourceManager.Singleton.BuildPreviewMaterial;
            else return mat;
        }
    }
}
