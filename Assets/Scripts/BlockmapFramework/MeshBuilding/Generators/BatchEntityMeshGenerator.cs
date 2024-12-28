using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class BatchEntityMeshGenerator
    {
        public static Dictionary<int, BatchEntityMesh> GenerateMeshes(Chunk chunk)
        {
            Dictionary<int, BatchEntityMesh> meshes = new Dictionary<int, BatchEntityMesh>();

            for (int altitude = 0; altitude < World.MAX_ALTITUDE; altitude++)
            {
                // Get procedural entities for altitude level
                List<Entity> entitiesToDraw = chunk.GetBatchEntities(altitude);
                if (entitiesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("ProceduralEntityMesh_" + altitude);
                BatchEntityMesh mesh = meshObject.AddComponent<BatchEntityMesh>();
                mesh.Init(chunk, altitude);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (Entity e in entitiesToDraw)
                {
                    e.Def.RenderProperties.BatchRenderFunction(meshBuilder, e.OriginNode, e.Height, false);
                    e.SetBatchEntityMesh(mesh);
                }
                meshBuilder.ApplyMesh();
                mesh.OnMeshApplied();

                meshes.Add(altitude, mesh);
            }

            return meshes;
        }
    }
}
