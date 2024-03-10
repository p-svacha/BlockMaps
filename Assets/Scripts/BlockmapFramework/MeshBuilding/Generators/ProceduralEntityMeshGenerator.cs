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

            for (int heightLevel = 0; heightLevel < World.MAX_HEIGHT; heightLevel++)
            {
                List<BlockmapNode> nodesToDraw = chunk.GetNodes(heightLevel).Where(x => x.Entities.Any(e => e is ProceduralEntity)).ToList();
                if (nodesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("ProceduralEntityMesh_" + heightLevel);
                ProceduralEntityMesh mesh = meshObject.AddComponent<ProceduralEntityMesh>();
                mesh.Init(chunk, heightLevel);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (BlockmapNode node in nodesToDraw)
                {
                    foreach (ProceduralEntity e in node.Entities.Where(x => x is ProceduralEntity)) e.BuildMesh(meshBuilder, node, isPreview: false);
                }
                meshBuilder.ApplyMesh();

                // Set chunk values for all materials
                mesh.Renderer = mesh.GetComponent<MeshRenderer>();
                for (int i = 0; i < mesh.Renderer.materials.Length; i++)
                {
                    mesh.Renderer.materials[i].SetFloat("_ChunkSize", chunk.Size);
                    mesh.Renderer.materials[i].SetFloat("_ChunkCoordinatesX", chunk.Coordinates.x);
                    mesh.Renderer.materials[i].SetFloat("_ChunkCoordinatesY", chunk.Coordinates.y);
                }

                meshes.Add(heightLevel, mesh);
            }

            return meshes;
        }
    }
}
