using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Represents the mesh of a node layer of a single chunk. Can be the mesh of GroundNodes or any mesh of AirNodes.
    /// </summary>
    public abstract class NodeMesh : ChunkMesh
    {
        private Dictionary<SurfaceDef, int> SurfaceArrayIndices = new Dictionary<SurfaceDef, int>();

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


            for (int x = 0; x < Chunk.Size; x++)
            {
                for (int y = 0; y < Chunk.Size; y++)
                {
                    // Get node
                    BlockmapNode node = GetNode(new Vector2Int(x, y));

                    // Base surface
                    int surfaceArrayIndex = GetOrSetArrayIndexFor(node);
                    surfaceArray.Add(surfaceArrayIndex);

                    // Blend for each direction
                    foreach (Direction dir in HelperFunctions.GetAllDirections8())
                    {
                        BlockmapNode blendNode = GetBlendNode(node, dir);
                        if(blendNode == null) surfaceBlendArrays[dir].Add(surfaceArrayIndex); // Just take own texture for blending when no blending should be done
                        else surfaceBlendArrays[dir].Add(GetOrSetArrayIndexFor(blendNode)); // Take index of blending texture
                    }
                }
            }
            

            // Set blend values for surface material only
            surfaceMaterial.SetFloatArray("_TileSurfaces", surfaceArray);
            surfaceMaterial.SetFloatArray("_TileBlend_W", surfaceBlendArrays[Direction.W]);
            surfaceMaterial.SetFloatArray("_TileBlend_E", surfaceBlendArrays[Direction.E]);
            surfaceMaterial.SetFloatArray("_TileBlend_N", surfaceBlendArrays[Direction.N]);
            surfaceMaterial.SetFloatArray("_TileBlend_S", surfaceBlendArrays[Direction.S]);
            surfaceMaterial.SetFloatArray("_TileBlend_NW", surfaceBlendArrays[Direction.NW]);
            surfaceMaterial.SetFloatArray("_TileBlend_NE", surfaceBlendArrays[Direction.NE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SE", surfaceBlendArrays[Direction.SE]);
            surfaceMaterial.SetFloatArray("_TileBlend_SW", surfaceBlendArrays[Direction.SW]);

            // Set arrays in shader that stores values for each surface
            SetSurfaceShaderArrays(surfaceMaterial);
        }

        private int GetOrSetArrayIndexFor(BlockmapNode node)
        {
            int index = -1;
            if (node != null && node.SurfaceDef.RenderProperties.SurfaceTexture != null)
            {
                if (SurfaceArrayIndices.ContainsKey(node.SurfaceDef)) index = SurfaceArrayIndices[node.SurfaceDef];
                else
                {
                    int newIndex = SurfaceArrayIndices.Count;
                    SurfaceArrayIndices.Add(node.SurfaceDef, newIndex);
                    index = newIndex;
                }
            }
            return index;
        }

        /// <summary>
        /// Sets the arrays for colors, textures and texture scaling in the shader of the surface material of this chunk, where each element represents the values for one surface that is used in this mesh.
        /// <br/>The values or set according to SurfaceArrayIndices.
        /// </summary>
        private void SetSurfaceShaderArrays(Material surfaceMaterialInstance)
        {
            // Pass terrain colors to shader of surface material of this chunk
            Color[] terrainColors = new Color[SurfaceArrayIndices.Count];
            foreach (KeyValuePair<SurfaceDef, int> kvp in SurfaceArrayIndices) terrainColors[kvp.Value] = kvp.Key.RenderProperties.SurfaceColor;
            surfaceMaterialInstance.SetColorArray("_TerrainColors", terrainColors);

            // Pass terrain textures to shader of surface material of this chunk
            Texture2DArray terrainTexArray = new Texture2DArray(1024, 1024, SurfaceArrayIndices.Count, TextureFormat.RGBA32, true);
            foreach (KeyValuePair<SurfaceDef, int> kvp in SurfaceArrayIndices)
            {
                terrainTexArray.SetPixels32(kvp.Key.RenderProperties.SurfaceTexture.GetPixels32(), kvp.Value);
            }
            terrainTexArray.Apply();
            surfaceMaterialInstance.SetTexture("_TerrainTextures", terrainTexArray);

            // Pass texture scaling values to shader of surface material of this chunk
            float[] textureScalingValues = new float[SurfaceArrayIndices.Count];
            foreach (KeyValuePair<SurfaceDef, int> kvp in SurfaceArrayIndices) textureScalingValues[kvp.Value] = kvp.Key.RenderProperties.SurfaceTextureScale;
            surfaceMaterialInstance.SetFloatArray("_TerrainTextureScale", textureScalingValues);
        }

        /// <summary>
        /// Returns the Node whose surface that should be blended into the given source node from the adjacent node in the given direction.
        /// <br/>Returns null if no blending should be done.
        /// </summary>
        private BlockmapNode GetBlendNode(BlockmapNode sourceNode, Direction dir)
        {
            if (sourceNode == null) return null;
            if (!sourceNode.SurfaceDef.RenderProperties.DoBlend) return null; // No blend on this node

            List<BlockmapNode> adjacentNodes = World.GetAdjacentNodes(sourceNode.WorldCoordinates, dir);
            foreach (BlockmapNode adjNode in adjacentNodes)
            {
                if (!World.DoAdjacentHeightsMatch(sourceNode, adjNode, dir)) continue; // Nodes are not seamlessly adjacent
                if (!adjNode.SurfaceDef.RenderProperties.DoBlend) continue; // No blend on adjacent node

                return adjNode;
            }
            return null;
        }

        protected abstract BlockmapNode GetNode(Vector2Int localCoordinates);
    }
}
