using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents the mesh of a node layer of a single chunk. Can be the mesh of GroundNodes or any mesh of AirNodes.
    /// </summary>
    public abstract class NodeMesh : ChunkMesh
    {
        private Dictionary<SurfaceDef, int> SurfaceArrayIndices = new Dictionary<SurfaceDef, int>();

        // Performance Profilers
        static readonly ProfilerMarker pm_NodeIteration = new ProfilerMarker("NodeIteration");
        static readonly ProfilerMarker pm_GetBlendNode = new ProfilerMarker("GetBlendNode");
        static readonly ProfilerMarker pm_PassSurfaceDataArraysToShader = new ProfilerMarker("PassSurfaceDataArraysToShader");

        public override void OnMeshApplied()
        {
            base.OnMeshApplied();
            PassShaderValues();
        }

        private void PassShaderValues()
        {
            // Check if any node in this mesh uses the blending-capable SurfaceMaterial
            Material surfaceMaterial = GetComponent<MeshRenderer>().materials.FirstOrDefault(x => x.shader.name == "Custom/SurfaceShader");
            if (surfaceMaterial == null) return;

            // Clear dictionary that stores which surfaces correspond to which array indices in the surface shader arrays
            SurfaceArrayIndices.Clear();

            // Set surface values for surface materials (textures per node and blending)
            List<float> surfaceArray = new List<float>();
            Dictionary<Direction, List<float>> surfaceBlendArrays = new Dictionary<Direction, List<float>>();
            foreach (Direction dir in HelperFunctions.GetAllDirections8()) surfaceBlendArrays.Add(dir, new List<float>());

            pm_NodeIteration.Begin();
            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    // Get node
                    BlockmapNode node = GetNode(new Vector2Int(x, y));

                    // Base surface
                    int surfaceArrayIndex = node == null ? -1 : MaterialManager.GetBlendableSurfaceShaderIndexFor(node.SurfaceDef);
                    surfaceArray.Add(surfaceArrayIndex);

                    // Blend for each direction
                    foreach (Direction dir in HelperFunctions.GetAllDirections8())
                    {
                        pm_GetBlendNode.Begin();
                        if (node == null) surfaceBlendArrays[dir].Add(-1);
                        else
                        {
                            SurfaceDef blendSurface = GetBlendSurface(node, dir);
                            surfaceBlendArrays[dir].Add(MaterialManager.GetBlendableSurfaceShaderIndexFor(blendSurface));
                        }
                        pm_GetBlendNode.End();
                    }
                }
            }
            pm_NodeIteration.End();

            // Set blend values for surface material only
            pm_PassSurfaceDataArraysToShader.Begin();
            surfaceMaterial.SetFloatArray("_TileSurfaces", surfaceArray);
            surfaceMaterial.SetFloatArray("_TileBlend_W", surfaceBlendArrays[Direction.W]);
            surfaceMaterial.SetFloatArray("_TileBlend_E", surfaceBlendArrays[Direction.E]);
            surfaceMaterial.SetFloatArray("_TileBlend_N", surfaceBlendArrays[Direction.N]);
            surfaceMaterial.SetFloatArray("_TileBlend_S", surfaceBlendArrays[Direction.S]);
            surfaceMaterial.SetFloatArray("_TileBlend_NW", surfaceBlendArrays[Direction.NW]);
            surfaceMaterial.SetFloatArray("_TileBlend_NE", surfaceBlendArrays[Direction.NE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SE", surfaceBlendArrays[Direction.SE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SW", surfaceBlendArrays[Direction.SW]);
            pm_PassSurfaceDataArraysToShader.End();
        }

        /// <summary>
        /// Returns the surface that should be blended into the given source node from the adjacent node in the given direction.
        /// <br/>Returns the source nodes own surface if no blending should be done.
        /// </summary>
        private SurfaceDef GetBlendSurface(BlockmapNode sourceNode, Direction dir)
        {
            if (sourceNode == null) return sourceNode.SurfaceDef;
            if (sourceNode.SurfaceDef.RenderProperties.Type != SurfaceRenderType.Default_Blend) return sourceNode.SurfaceDef; // No blend on this node

            List<BlockmapNode> adjacentNodes = World.GetAdjacentNodes(sourceNode.WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjacentNodes)
            {
                if (!World.DoAdjacentHeightsMatch(sourceNode, adjNode, dir)) continue; // Nodes are not seamlessly adjacent
                if (adjNode.SurfaceDef.RenderProperties.Type != SurfaceRenderType.Default_Blend) continue; // No blend on adjacent node

                return adjNode.SurfaceDef;
            }
            return sourceNode.SurfaceDef;
        }

        protected abstract BlockmapNode GetNode(Vector2Int localCoordinates);
    }
}
