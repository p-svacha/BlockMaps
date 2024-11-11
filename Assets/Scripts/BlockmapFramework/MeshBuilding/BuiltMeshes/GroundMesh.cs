using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents the mesh that has all ground nodes of a single chunk.
    /// </summary>
    public class GroundMesh : NodeMesh
    {
        public void Init(Chunk chunk)
        {
            OnInit(chunk);

            gameObject.layer = chunk.World.Layer_GroundNode;
        }

        public void Draw()
        {
            MeshBuilder meshBuilder = new MeshBuilder(gameObject);

            // Create surface material submesh so it always has index 0
            meshBuilder.GetSubmesh(ResourceManager.Singleton.SurfaceMaterial);

            // Draw each node on the mesh builder
            foreach (GroundNode node in Chunk.GetAllGroundNodes())
            {
                // Generate mesh
                node.DrawSurface(meshBuilder);
                node.DrawSides(meshBuilder);
                node.SetMesh(this);
            }

            meshBuilder.ApplyMesh();
            OnMeshApplied();
        }

        protected override BlockmapNode GetNode(Vector2Int localCoordinates)
        {
            return Chunk.GetGroundNode(localCoordinates);
        }

        public override void SetVisibility(Actor player)
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
