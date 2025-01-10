using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public static class MeshGen_WoodenFence
    {
        private static float POLE_WIDTH = 0.1f;
        private static int NUM_POLES = 2;

        private static float CROSS_BRACE_START_Y = 0.2f;
        private static float CROSS_BRACE_HEIGHT = 0.1f;
        private static float CROSS_BRACE_WIDTH = 0.05f;

        public static void DrawMesh(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            if (HelperFunctions.IsSide(side)) DrawSide(meshBuilder, node, side, height, isPreview);
            else if (HelperFunctions.IsCorner(side)) DrawCorner(meshBuilder, node, side, height, isPreview);
            else throw new System.Exception("Invalid side " + side.ToString());
        }

        private static void DrawSide(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(isPreview ? MaterialManager.BuildPreviewMaterial : MaterialManager.LoadMaterial("Materials/NodeMaterials/Wood"));

            // Poles
            float poleStep = 1f / NUM_POLES;
            for (int i = 0; i < NUM_POLES; i++)
            {
                float startX = (poleStep / 2f) + (i * poleStep - (POLE_WIDTH / 2f));
                float dimX = POLE_WIDTH;
                float startY = 0f;
                float dimY = World.NodeHeight * height;
                float startZ = 0f;
                float dimZ = POLE_WIDTH;
                Vector3 polePos = new Vector3(startX, startY, startZ);
                Vector3 poleDims = new Vector3(dimX, dimY, dimZ);
                meshBuilder.BuildCube(node, side, submesh, polePos, poleDims, adjustToNodeSlope: true);
            }

            // Cross braces
            for (int i = 0; i < height; i++)
            {
                // Main cross brace for altitude
                float braceYPos = (World.NodeHeight * i) + CROSS_BRACE_START_Y;
                BuildCrossBrace(meshBuilder, node, side, submesh, braceYPos);

                // Cross brace in between two altitudes
                if (i > 0)
                {
                    float betweenBraceYPos = (World.NodeHeight * i) - (CROSS_BRACE_HEIGHT / 2f);
                    BuildCrossBrace(meshBuilder, node, side, submesh, betweenBraceYPos);
                }
            }
        }
        private static void BuildCrossBrace(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int submesh, float yPos)
        {
            float cb_x = 0f;
            float cb_dimX = 1f;
            float cb_y = yPos;
            float cb_dimY = CROSS_BRACE_HEIGHT;
            float cb_z = (POLE_WIDTH - CROSS_BRACE_WIDTH) / 2f;
            float cb_dimZ = CROSS_BRACE_WIDTH;
            Vector3 cbPos = new Vector3(cb_x, cb_y, cb_z);
            Vector3 cbDims = new Vector3(cb_dimX, cb_dimY, cb_dimZ);
            meshBuilder.BuildCube(node, side, submesh, cbPos, cbDims, adjustToNodeSlope: true);
        }

        private static void DrawCorner(MeshBuilder meshBuilder, BlockmapNode node, Direction side, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(isPreview ? MaterialManager.BuildPreviewMaterial : MaterialManager.LoadMaterial("Materials/NodeMaterials/Wood"));

            float startX = 0;
            float dimX = POLE_WIDTH;
            float startY = 0f;
            float dimY = World.NodeHeight * height;
            float startZ = 0f;
            float dimZ = POLE_WIDTH;
            Vector3 pos = new Vector3(startX, startY, startZ);
            Vector3 dim = new Vector3(dimX, dimY, dimZ);
            meshBuilder.BuildCube(node, side, submesh, pos, dim, adjustToNodeSlope: true);
        }
    }
}
