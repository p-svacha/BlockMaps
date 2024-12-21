using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents all water body parts that are on one chunk.
    /// </summary>
    public class WaterMesh : ChunkMesh
    {
        public void Init(Chunk chunk)
        {
            OnInit(chunk);

            gameObject.layer = chunk.World.Layer_WaterMesh;
        }

        public void Draw()
        {
            MeshBuilder meshBuilder = new MeshBuilder(gameObject);

            foreach (WaterNode node in Chunk.GetAllWaterNodes())
            {
                WaterMeshGenerator.BuildWaterMeshForSingleNode(meshBuilder, node);
                node.SetMesh(this);
            }

            meshBuilder.ApplyMesh();
            OnMeshApplied();
        }


        public override void SetVisibility(Actor player) // same as SurfaceNode
        {
            // Define surface visibility array based on node visibility
            List<float> surfaceVisibilityArray = new List<float>();
            for (int x = -1; x <= Chunk.Size; x++)
            {
                for (int y = -1; y <= Chunk.Size; y++)
                {
                    GroundNode targetNode = Chunk.World.GetGroundNode(Chunk.GetWorldCoordinates(new Vector2Int(x, y)));

                    int visibility;
                    if (targetNode != null && targetNode.IsVisibleBy(player)) visibility = 2; // 2 = visible
                    else if (targetNode != null && targetNode.IsExploredBy(player)) visibility = 1; // 1 = fog of war
                    else visibility = 0; // 0 = unexplored
                    surfaceVisibilityArray.Add(visibility);
                }
            }

            // Pass to shader
            SetShaderVisibilityData(surfaceVisibilityArray);
        }
    }
}
