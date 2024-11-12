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
                // Get walls for altitude level
                List<Wall> wallsToDraw = chunk.GetWalls(altitude);
                if (wallsToDraw.Count == 0) continue;

                // Generate mesh
                GameObject meshObject = new GameObject("WallMesh_" + altitude);
                WallMesh mesh = meshObject.AddComponent<WallMesh>();
                mesh.Init(chunk, altitude);

                MeshBuilder meshBuilder = new MeshBuilder(meshObject);
                foreach(Wall wall in wallsToDraw)
                {
                    wall.Shape.GenerateMesh(chunk.World, meshBuilder, wall.GlobalCellCoordinates, wall.LocalCellCoordinates, wall.Side, wall.Material.Material, wall.IsMirrored);
                }
                meshBuilder.ApplyMesh();
                mesh.OnMeshApplied();

                meshes.Add(altitude, mesh);
            }

            return meshes;
        }

        /// <summary>
        /// Adds the mesh of a single wall piece to a MeshBuilder.
        /// </summary>
        public static void DrawWall(World world, MeshBuilder meshBuilder, Vector3Int globalCellPosition, Vector3Int localCellPosition, Direction side, WallShape shape, WallMaterial material, bool isMirrored, bool isPreview = false)
        {
            shape.GenerateMesh(world, meshBuilder, globalCellPosition, localCellPosition, side, GetMaterial(material.Material, isPreview), isMirrored);
        }

        private static Material GetMaterial(Material mat, bool isPreview)
        {
            if (isPreview) return MaterialManager.BuildPreviewMaterial;
            else return mat;
        }
    }
}
