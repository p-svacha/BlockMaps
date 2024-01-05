using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class WallMeshGenerator
    {
        private const float WALL_WIDTH = 0.1f;

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
            switch (wall.Type.Id)
            {
                case "brickWall":
                    int submesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.BrickWallMaterial);
                    meshBuilder.BuildCube(submesh, GetWallStartPos(wall.Node, wall.Side), GetWallDimensions(wall.Node, wall.Side, wall.Height));
                    break;
            }
        }

        private static Vector3 GetWallStartPos(BlockmapNode node, Direction side)
        {
            int startHeightCoordinate = Wall.GetWallStartY(node, side);
            float worldHeight = node.World.GetWorldHeight(startHeightCoordinate);

            return side switch
            {
                Direction.N => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y + (1f - WALL_WIDTH)),
                Direction.E => new Vector3(node.LocalCoordinates.x + (1f - WALL_WIDTH), worldHeight, node.LocalCoordinates.y),
                Direction.S => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y),
                Direction.W => new Vector3(node.LocalCoordinates.x, worldHeight, node.LocalCoordinates.y),
                _ => throw new System.Exception("Direction " + side.ToString() + " not handled.")
            };
        }

        private static Vector3 GetWallDimensions(BlockmapNode node, Direction side, int height)
        {
            float worldHeight = height * World.TILE_HEIGHT;

            return side switch
            {
                Direction.N => new Vector3(1f, worldHeight, WALL_WIDTH),
                Direction.E => new Vector3(WALL_WIDTH, worldHeight, 1f),
                Direction.S => new Vector3(1f, worldHeight, WALL_WIDTH),
                Direction.W => new Vector3(WALL_WIDTH, worldHeight, 1f),
                _ => throw new System.Exception("Direction " + side.ToString() + " not handled.")
            };
        }
    }
}
