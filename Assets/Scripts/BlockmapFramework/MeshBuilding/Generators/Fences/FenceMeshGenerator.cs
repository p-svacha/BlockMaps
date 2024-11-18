using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class FenceMeshGenerator
    {
        /// <summary>
        /// ChunkMesh
        /// </summary>
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
                    DrawFence(meshBuilder, fence.Def, fence.Node, fence.Side, fence.Height);
                }
                meshBuilder.ApplyMesh();
                mesh.OnMeshApplied();

                meshes.Add(altitude, mesh);
            }

            return meshes;
        }

        /// <summary>
        /// Single fence piece
        /// </summary>
        public static void DrawFence(MeshBuilder meshBuilder, FenceDef def, BlockmapNode node, Direction side, int height, bool isPreview = false)
        {
            if(!HelperFunctions.IsSide(side) && !HelperFunctions.IsCorner(side)) throw new System.Exception("Fences can only be drawn when on side or corner of a node");
            def.GenerateMeshFunction(meshBuilder, node, side, height, isPreview);
        }
    }
}
