using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class WallMeshGenerator
    {
        public static Dictionary<int, WallMesh> GenerateMeshes(Chunk chunk)
        {
            Dictionary<int, WallMesh> meshes = new Dictionary<int, WallMesh>();

            for (int heightLevel = 0; heightLevel < World.MAX_HEIGHT; heightLevel++)
            {
                List<BlockmapNode> nodesToDraw = chunk.GetNodes(heightLevel).Where(x => x.HasWall).ToList();
                if (nodesToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("WallMesh_" + heightLevel);
                WallMesh mesh = meshObject.AddComponent<WallMesh>();
                mesh.Init(chunk, heightLevel);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach (BlockmapNode node in nodesToDraw)
                {
                    foreach (Direction side in HelperFunctions.GetSides())
                    {
                        if (node.Walls.ContainsKey(side))
                        {
                            DrawWall(meshBuilder, node.Walls[side]);
                        }
                    }
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

        public static void DrawWall(MeshBuilder meshBuilder, Wall wall)
        {
            wall.Type.GenerateMesh(meshBuilder, wall);
        }
    }
}
