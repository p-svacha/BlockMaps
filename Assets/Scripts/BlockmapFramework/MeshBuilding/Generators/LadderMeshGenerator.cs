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
        private const float LADDER_POLE_SIZE = 0.05f; // x and z dimension of the poles on the sides
        private const float STEP_INTERVAL = 0.1f;
        private const float STEP_HEIGHT = 0.05f;
        private const float STEP_WIDTH = 0.02f;

        public static LadderReference GenerateLadderObject(World world, Ladder ladder)
        {
            GameObject ladderObject = new GameObject("Ladder");
            ladderObject.layer = world.Layer_Ladder;
            ladderObject.transform.SetParent(ladder.Bottom.Chunk.transform);
            ladderObject.transform.localPosition = Vector3.zero;
            LadderReference ladderRef = ladderObject.AddComponent<LadderReference>();
            ladderRef.Init(ladder);

            MeshBuilder meshBuilder = new MeshBuilder(ladderObject);

            int ladderSubmesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.LadderMaterial);

            // Left pole
            Vector3 lpl_pos = new Vector3(0.5f - LADDER_STEP_LENGTH / 2f - LADDER_POLE_SIZE, 0f, 0f);
            Vector3 lpl_dim = new Vector3(LADDER_POLE_SIZE, ladder.Height * World.TILE_HEIGHT, LADDER_POLE_SIZE);
            meshBuilder.BuildCube(ladderSubmesh, lpl_pos, lpl_dim, ladder.Bottom, ladder.Side);

            // Right pole
            Vector3 lpr_pos = new Vector3(0.5f + LADDER_STEP_LENGTH / 2f, 0f, 0f);
            Vector3 lpr_dim = new Vector3(LADDER_POLE_SIZE, ladder.Height * World.TILE_HEIGHT, LADDER_POLE_SIZE);
            meshBuilder.BuildCube(ladderSubmesh, lpr_pos, lpr_dim, ladder.Bottom, ladder.Side);

            // Steps
            float currentY = STEP_INTERVAL;
            while(currentY < ladder.Height * World.TILE_HEIGHT)
            {
                Vector3 step_pos = new Vector3(0.5f - LADDER_STEP_LENGTH / 2f, currentY, STEP_WIDTH / 2f);
                Vector3 step_dim = new Vector3(LADDER_STEP_LENGTH, STEP_HEIGHT, STEP_WIDTH);
                meshBuilder.BuildCube(ladderSubmesh, step_pos, step_dim, ladder.Bottom, ladder.Side);
                currentY += (STEP_INTERVAL + STEP_HEIGHT);
            }

            meshBuilder.ApplyMesh();
            return ladderRef;
        }
    }
}
