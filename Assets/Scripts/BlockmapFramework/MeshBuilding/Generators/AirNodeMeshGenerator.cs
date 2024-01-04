using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public static class AirNodeMeshGenerator
    {
        public static Dictionary<int, AirNodeMesh> GenerateMeshes(Chunk chunk)
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
            }

            return meshes;
        }
    }
}
