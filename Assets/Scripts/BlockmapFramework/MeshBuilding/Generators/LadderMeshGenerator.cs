using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Generates a ladder object as a GameObject (NOT part of a meshBuilder - each ladder is 1 object)
    /// </summary>
    public class LadderMeshGenerator
    {
        private const float LADDER_STEP_LENGTH = 0.4f;
        public const float LADDER_POLE_SIZE = 0.05f; // x and z dimension of the poles on the sides
        private const float STEP_INTERVAL = 0.1f;
        private const float STEP_HEIGHT = 0.05f;
        private const float STEP_WIDTH = 0.02f;

        public static void GenerateLadderMesh(MeshBuilder meshBuilder, int height, bool isPreview)
        {
            int ladderSubmesh = meshBuilder.GetSubmesh(GetMaterial(isPreview));

            // Left pole
            Vector3 lpl_pos = new Vector3(-LADDER_STEP_LENGTH / 2f - LADDER_POLE_SIZE, 0f, -0.5f);
            Vector3 lpl_dim = new Vector3(LADDER_POLE_SIZE, height * World.NodeHeight, LADDER_POLE_SIZE);
            meshBuilder.BuildCube(ladderSubmesh, lpl_pos, lpl_dim);

            // Right pole
            Vector3 lpr_pos = new Vector3(LADDER_STEP_LENGTH / 2f, 0f, -0.5f);
            Vector3 lpr_dim = new Vector3(LADDER_POLE_SIZE, height * World.NodeHeight, LADDER_POLE_SIZE);
            meshBuilder.BuildCube(ladderSubmesh, lpr_pos, lpr_dim);

            // Steps
            float currentY = STEP_INTERVAL;
            while (currentY < height * World.NodeHeight)
            {
                Vector3 step_pos = new Vector3(-LADDER_STEP_LENGTH / 2f, currentY, -0.5f + STEP_WIDTH / 2f);
                Vector3 step_dim = new Vector3(LADDER_STEP_LENGTH, STEP_HEIGHT, STEP_WIDTH);
                meshBuilder.BuildCube(ladderSubmesh, step_pos, step_dim);
                currentY += (STEP_INTERVAL + STEP_HEIGHT);
            }

            meshBuilder.ApplyMesh();
        }

        private static Material GetMaterial(bool isPreview)
        {
            if (isPreview) return MaterialManager.BuildPreviewMaterial;
            else return MaterialManager.LoadMaterial("Special/LadderMaterial");
        }
    }
}
