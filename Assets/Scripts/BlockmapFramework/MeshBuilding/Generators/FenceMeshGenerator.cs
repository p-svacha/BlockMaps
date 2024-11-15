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

            for (int altitude = 0; altitude < World.MAX_ALTITUDE; altitude++)
            {
                // Get fences for altitude level
                List<Fence> fencesToDraw = chunk.GetFences(altitude);
                if (fencesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("FenceMesh_" + altitude);
                FenceMesh mesh = meshObject.AddComponent<FenceMesh>();
                mesh.Init(chunk, altitude);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (Fence fence in fencesToDraw)
                {
                    DrawFence(meshBuilder, fence.Type, fence.Node, fence.Side, fence.Height);
                }
                meshBuilder.ApplyMesh();
                mesh.OnMeshApplied();

                meshes.Add(altitude, mesh);
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
