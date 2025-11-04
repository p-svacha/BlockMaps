using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Database containing all meshes that are used in GPU instancing.
    /// <br/>Meshes in here are created so that they fill a full 1x1x1 unit. 
    /// <br/>They are then transformed in the according renderer (i.e. ChunkWallInstancedRenderer) to fit the correct orientation, position and scale.
    /// </summary>
    public static class GPUInstancingMeshDatabase
    {
        public static Mesh CubeMesh { get; private set; }
        public static Mesh SlopeWallMesh { get; private set; }
        public static Mesh SlopeWallMeshMirrored { get; private set; }
        public static Mesh WindowGlassMesh { get; private set; }

        public static void CreateAllMeshes()
        {
            CreateCubeMesh();
            CreateSlopeMeshes();
            CreateWindowGlassMesh();
        }

        /// <summary>
        /// Creates a basic 1x1x1 cube mesh centered on (0,0.5,0) filling one full cell. This can be used for solid and corner wall pieces, since they are both just cubes that get transformed appropriately.
        /// </summary>
        private static void CreateCubeMesh()
        {
            MeshBuilder meshBuilder = new MeshBuilder();
            int submesh = meshBuilder.AddDummySubmesh();

            // Build 1x1x1 cube centered on (0,0.5,0). The block gets rotated, scaled and offset in ChunkWallInstancedRenderer.
            meshBuilder.BuildCube(submesh, startPos: new Vector3(-0.5f, 0f, -0.5f), dimensions: new Vector3(1f, 1f, 1f));

            CubeMesh = meshBuilder.BuildMesh();
        }

        /// <summary>
        /// Creates the meshes used for slope wall pieces, both normal and mirrored.
        /// </summary>
        private static void CreateSlopeMeshes()
        {
            // Normal
            MeshBuilder meshBuilder = new MeshBuilder();
            int submesh = meshBuilder.AddDummySubmesh();

            float start = -0.5f;
            float end = 0.5f;

            // Front triangle
            Vector3 ft1 = new Vector3(end, 0f, start);
            Vector3 ft2 = new Vector3(start, 1f, start);
            Vector3 ft3 = new Vector3(start, 0f, start);
            meshBuilder.BuildTriangle(submesh, ft1, ft2, ft3);

            // Back triangle
            Vector3 bt1 = new Vector3(end, 0f, end);
            Vector3 bt2 = new Vector3(start, 1f, end);
            Vector3 bt3 = new Vector3(start, 0f, end);
            meshBuilder.BuildTriangle(submesh, bt1, bt2, bt3, true);

            // Side plane
            Vector3 sp1 = new Vector3(start, 0f, start);
            Vector3 sp2 = new Vector3(start, 1f, start);
            Vector3 sp3 = new Vector3(start, 1f, end);
            Vector3 sp4 = new Vector3(start, 0f, end);
            meshBuilder.BuildPlane(submesh, sp1, sp2, sp3, sp4, Vector2.zero, Vector2.one);

            // Top sloped plane
            Vector3 tsp1 = new Vector3(end, 0f, start);
            Vector3 tsp2 = new Vector3(start, 1f, start);
            Vector3 tsp3 = new Vector3(start, 1f, end);
            Vector3 tsp4 = new Vector3(end, 0f, end);
            meshBuilder.BuildPlane(submesh, tsp1, tsp2, tsp3, tsp4, Vector2.zero, Vector2.one, true);

            SlopeWallMesh = meshBuilder.BuildMesh();

            // Mirrored
            MeshBuilder meshBuilderMirrored = new MeshBuilder();
            int submeshMirrored = meshBuilderMirrored.AddDummySubmesh();

            // Front triangle
            Vector3 ft1m = new Vector3(start, 0f, start);
            Vector3 ft2m = new Vector3(end, 1f, start);
            Vector3 ft3m = new Vector3(end, 0f, start);
            meshBuilderMirrored.BuildTriangle(submeshMirrored, ft1m, ft2m, ft3m, true);

            // Back triangle
            Vector3 bt1m = new Vector3(start, 0f, end);
            Vector3 bt2m = new Vector3(end, 1f, end);
            Vector3 bt3m = new Vector3(end, 0f, end);
            meshBuilderMirrored.BuildTriangle(submeshMirrored, bt1m, bt2m, bt3m);

            // Side plane
            Vector3 sp1m = new Vector3(end, 0f, start);
            Vector3 sp2m = new Vector3(end, 1f, start);
            Vector3 sp3m = new Vector3(end, 1f, end);
            Vector3 sp4m = new Vector3(end, 0f, end);
            meshBuilderMirrored.BuildPlane(submeshMirrored, sp1m, sp2m, sp3m, sp4m, Vector2.zero, Vector2.one, true);

            // Top sloped plane
            Vector3 tsp1m = new Vector3(start, 0f, start);
            Vector3 tsp2m = new Vector3(end, 1f, start);
            Vector3 tsp3m = new Vector3(end, 1f, end);
            Vector3 tsp4m = new Vector3(start, 0f, end);
            meshBuilderMirrored.BuildPlane(submeshMirrored, tsp1m, tsp2m, tsp3m, tsp4m, Vector2.zero, Vector2.one);

            SlopeWallMeshMirrored = meshBuilderMirrored.BuildMesh();
        }


        /// <summary>
        /// Creates the mesh used for the glass part of window walls, already at the correct scale since the glass meshes are always the same size.
        /// </summary>
        private static void CreateWindowGlassMesh()
        {
            float paneWidth = 0.05f;

            MeshBuilder meshBuilder = new MeshBuilder();
            int submesh = meshBuilder.AddDummySubmesh();

            Vector3 frontPane1 = new Vector3(-0.5f, 0f, -paneWidth / 2);
            Vector3 frontPane2 = new Vector3(0.5f, 0f, -paneWidth / 2);
            Vector3 frontPane3 = new Vector3(0.5f, World.NodeHeight, -paneWidth / 2);
            Vector3 frontPane4 = new Vector3(-0.5f, World.NodeHeight, -paneWidth / 2);
            meshBuilder.BuildPlane(submesh, frontPane1, frontPane2, frontPane3, frontPane4, Vector2.zero, Vector2.one);

            Vector3 backPane1 = new Vector3(-0.5f, 0f, paneWidth / 2);
            Vector3 backPane2 = new Vector3(0.5f, 0f, paneWidth / 2);
            Vector3 backPane3 = new Vector3(0.5f, World.NodeHeight, paneWidth / 2);
            Vector3 backPane4 = new Vector3(-0.5f, World.NodeHeight, paneWidth / 2);
            meshBuilder.BuildPlane(submesh, backPane1, backPane2, backPane3, backPane4, Vector2.zero, Vector2.one, mirror: true);

            WindowGlassMesh = meshBuilder.BuildMesh();
        }
    }
}
