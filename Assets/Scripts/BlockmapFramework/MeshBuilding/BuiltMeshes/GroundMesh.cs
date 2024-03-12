using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents the mesh that has all ground nodes of a single chunk.
    /// </summary>
    public class GroundMesh : ChunkMesh
    {
        public void Init(Chunk chunk)
        {
            OnInit(chunk);

            gameObject.layer = chunk.World.Layer_GroundNode;
        }

        public override void OnDraw()
        {
            // Create surface material submesh so it always has index 0
            MeshBuilder.GetSubmesh(ResourceManager.Singleton.SurfaceMaterial);

            // Draw each node on the mesh builder
            foreach (GroundNode node in Chunk.GetAllGroundNodes())
            {
                // Generate mesh
                node.Draw(MeshBuilder);
                node.SetMesh(this);
            }
        }

        public override void OnMeshApplied()
        {
            // Shader values
            List<float> surfaceArray = new List<float>();
            Dictionary<Direction, List<float>> surfaceBlendArrays = new Dictionary<Direction, List<float>>();
            foreach (Direction dir in HelperFunctions.GetAllDirections8()) surfaceBlendArrays.Add(dir, new List<float>());


            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    // Get node
                    GroundNode node = Chunk.GetGroundNode(x, y);

                    // Base surface
                    int surfaceId = (int)node.Surface.Id;
                    surfaceArray.Add(surfaceId);

                    // Blend for each direction
                    foreach (Direction dir in HelperFunctions.GetAllDirections8())
                    {
                        if (DoBlend(node, dir, out int blendSurfaceId)) surfaceBlendArrays[dir].Add(blendSurfaceId);
                        else surfaceBlendArrays[dir].Add(surfaceId);
                    }
                }
            }

            // Set blend values for surface material only
            Material surfaceMaterial = Renderer.materials[0];
            surfaceMaterial.SetFloatArray("_TileSurfaces", surfaceArray);
            surfaceMaterial.SetFloatArray("_TileBlend_W", surfaceBlendArrays[Direction.W]);
            surfaceMaterial.SetFloatArray("_TileBlend_E", surfaceBlendArrays[Direction.E]);
            surfaceMaterial.SetFloatArray("_TileBlend_N", surfaceBlendArrays[Direction.N]);
            surfaceMaterial.SetFloatArray("_TileBlend_S", surfaceBlendArrays[Direction.S]);
            surfaceMaterial.SetFloatArray("_TileBlend_NW", surfaceBlendArrays[Direction.NW]);
            surfaceMaterial.SetFloatArray("_TileBlend_NE", surfaceBlendArrays[Direction.NE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SE", surfaceBlendArrays[Direction.SE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SW", surfaceBlendArrays[Direction.SW]);
        }

        /// <summary>
        /// Returns if a source node should have the surface of an adjacent node in a given direction blended into it.
        /// </summary>
        private bool DoBlend(BlockmapNode sourceNode, Direction dir, out int blendSurfaceId)
        {
            blendSurfaceId = -1;

            if (sourceNode == null) return false;
            if (!sourceNode.GetSurface().DoBlend) return false; // No blend on this node

            List<BlockmapNode> adjacentNodes = World.GetAdjacentNodes(sourceNode.WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjacentNodes)
            {
                if (!World.DoAdjacentHeightsMatch(sourceNode, adjNode, dir)) continue;
                if (adjNode.GetSurface() == null) continue;
                if (!adjNode.GetSurface().DoBlend) continue;

                blendSurfaceId = (int)(adjNode.GetSurface().Id);
                return true;
            }
            return false;
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

            // Set visibility in all surface mesh materials
            for (int i = 0; i < Renderer.materials.Length; i++)
            {
                Renderer.materials[i].SetFloat("_FullVisibility", 0);
                Renderer.materials[i].SetFloatArray("_TileVisibility", surfaceVisibilityArray);
            }
        }
    }
}
