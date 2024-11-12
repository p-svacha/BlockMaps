using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class ProceduralEntityMeshGenerator
    {
        public static Dictionary<int, ProceduralEntityMesh> GenerateMeshes(Chunk chunk)
        {
            Dictionary<int, ProceduralEntityMesh> meshes = new Dictionary<int, ProceduralEntityMesh>();

            for (int altitude = 0; altitude < World.MAX_ALTITUDE; altitude++)
            {
                // Get procedural entities for altitude level
                List<ProceduralEntity> entitiesToDraw = chunk.GetProceduralEntities(altitude);
                if (entitiesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("ProceduralEntityMesh_" + altitude);
                ProceduralEntityMesh mesh = meshObject.AddComponent<ProceduralEntityMesh>();
                mesh.Init(chunk, altitude);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (ProceduralEntity e in entitiesToDraw)
                {
                    e.BuildMesh(meshBuilder);
                }
                meshBuilder.ApplyMesh();
                mesh.OnMeshApplied();

                meshes.Add(altitude, mesh);
            }

            return meshes;
        }
    }
}
