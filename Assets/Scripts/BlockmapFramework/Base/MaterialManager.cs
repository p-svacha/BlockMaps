using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class MaterialManager
    {
        private static string MaterialsBasePath = "BlockmapFramework/Materials/";
        private static Dictionary<string, Material> CachedMaterials = new Dictionary<string, Material>();
        private static Dictionary<SurfaceDef, int> CachedSurfaceArrayIndices = new Dictionary<SurfaceDef, int>();

        public static Material LoadMaterial(string materialSubpath)
        {
            // cached
            if (CachedMaterials.TryGetValue(materialSubpath, out Material mat)) return mat;

            // not yet cached
            string fullPath = MaterialsBasePath + materialSubpath;
            Material newMat = Resources.Load<Material>(fullPath);
            if (newMat == null) throw new System.Exception($"Failed to load material {fullPath}.");
            CachedMaterials.Add(materialSubpath, newMat);
            return newMat;
        }

        public static int GetBlendableSurfaceShaderIndexFor(SurfaceDef def)
        {
            if (CachedSurfaceArrayIndices.TryGetValue(def, out int value)) return value;
            return -1;
        }

        /// <summary>
        /// Sets the meta arrays for colors, textures and texture scalings for all blendable SurfaceDefs
        /// </summary>
        public static void InitializeBlendableSurfaceMaterial()
        {
            List<SurfaceDef> blendableSurfaces = DefDatabase<SurfaceDef>.AllDefs.Where(x => x.RenderProperties.Type == SurfaceRenderType.FlatBlendableSurface).ToList();

            // Set up caching
            CachedSurfaceArrayIndices.Clear();
            for (int i = 0; i < 3; i++) CachedSurfaceArrayIndices.Add(blendableSurfaces[i], i);

            // Pass terrain colors to surface material
            Color[] terrainColors = new Color[blendableSurfaces.Count];
            for(int i = 0; i < blendableSurfaces.Count; i++)
                terrainColors[i] = blendableSurfaces[i].RenderProperties.SurfaceReferenceMaterial.color;
            BlendbaleSurfaceMaterial.SetColorArray("_TerrainColors", terrainColors);

            // Pass terrain textures to surface material
            Texture2DArray terrainTexArray = new Texture2DArray(1024, 1024, blendableSurfaces.Count, TextureFormat.RGBA32, true);
            for (int i = 0; i < blendableSurfaces.Count; i++)
                terrainTexArray.SetPixels32(((Texture2D)blendableSurfaces[i].RenderProperties.SurfaceReferenceMaterial.mainTexture).GetPixels32(), i);
            terrainTexArray.Apply();
            BlendbaleSurfaceMaterial.SetTexture("_TerrainTextures", terrainTexArray);

            // Pass texture scaling values to surface materials
            float[] textureScalingValues = new float[blendableSurfaces.Count];
            for (int i = 0; i < blendableSurfaces.Count; i++)
                textureScalingValues[i] = blendableSurfaces[i].RenderProperties.SurfaceReferenceMaterial.GetFloat("_TextureScale");
            BlendbaleSurfaceMaterial.SetFloatArray("_TerrainTextureScale", textureScalingValues);
        }

        // Quick accessors for special materials
        public static Material BlendbaleSurfaceMaterial => LoadMaterial("Special/SurfaceMaterial");
        public static Material BuildPreviewMaterial => LoadMaterial("Special/BuildPreviewMaterial");
    }
}
