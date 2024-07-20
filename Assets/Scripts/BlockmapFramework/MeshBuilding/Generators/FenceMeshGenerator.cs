using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class FenceMeshGenerator
    {
        public static Dictionary<int, FenceMesh> GenerateMeshes(Chunk chunk)
        {
            Dictionary<int, FenceMesh> meshes = new Dictionary<int, FenceMesh>();

            for (int heightLevel = 0; heightLevel < World.MAX_HEIGHT; heightLevel++)
            {
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
            }

            return meshes;
        }

        public static void DrawFence(MeshBuilder meshBuilder, FenceType type, BlockmapNode node, Direction side, int height, bool isPreview = false)
        {
            if (HelperFunctions.IsSide(side)) type.GenerateSideMesh(meshBuilder, node, side, height, isPreview);
            else if (HelperFunctions.IsCorner(side)) type.GenerateCornerMesh(meshBuilder, node, side, height, isPreview);
            else throw new System.Exception("Fences can only be drawn when on side or corner of a node");
        }
    }
}
