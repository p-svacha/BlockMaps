using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public static class MaterialManager
    {
        private static Dictionary<string, Material> CachedMaterials = new Dictionary<string, Material>();
        private static Dictionary<SurfaceDef, int> CachedSurfaceArrayIndices = new Dictionary<SurfaceDef, int>();

        public static Material LoadMaterial(string resourcePath)
        {
            // cached
            if (CachedMaterials.TryGetValue(resourcePath, out Material mat)) return mat;

            // not yet cached
            Material newMat = Resources.Load<Material>(resourcePath);
            if (newMat == null) throw new System.Exception($"Failed to load material {resourcePath}.");
            CachedMaterials.Add(resourcePath, newMat);
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
            List<SurfaceDef> blendableSurfaces = DefDatabase<SurfaceDef>.AllDefs.Where(x => x.RenderProperties.Type == SurfaceRenderType.Default_Blend).ToList();

            // Set up caching
            CachedSurfaceArrayIndices.Clear();
            for (int i = 0; i < blendableSurfaces.Count; i++) CachedSurfaceArrayIndices.Add(blendableSurfaces[i], i);

            // Load materials
            List<Material> blendableMaterials = blendableSurfaces.Select(x => LoadMaterial(x.GetFullMaterialResourcePath())).ToList();

            // Pass terrain colors to surface material
            Color[] terrainColors = new Color[blendableSurfaces.Count];
            for(int i = 0; i < blendableSurfaces.Count; i++)
                terrainColors[i] = blendableMaterials[i].color;
            BlendbaleSurfaceMaterial.SetColorArray("_TerrainColors", terrainColors);

            // Pass terrain textures to surface material
            Texture2DArray terrainTexArray = new Texture2DArray(1024, 1024, blendableSurfaces.Count, TextureFormat.RGBA32, true);
            for (int i = 0; i < blendableSurfaces.Count; i++)
                terrainTexArray.SetPixels32(((Texture2D)blendableMaterials[i].mainTexture).GetPixels32(), i);
            terrainTexArray.Apply();
            BlendbaleSurfaceMaterial.SetTexture("_TerrainTextures", terrainTexArray);

            // Pass texture scaling values to surface materials
            float[] textureScalingValues = new float[blendableSurfaces.Count];
            for (int i = 0; i < blendableSurfaces.Count; i++)
                textureScalingValues[i] = blendableMaterials[i].GetFloat("_TextureScale");
            BlendbaleSurfaceMaterial.SetFloatArray("_TerrainTextureScale", textureScalingValues);
        }

        // Quick accessors for special materials
        public static Material BlendbaleSurfaceMaterial => LoadMaterial("Materials/Special/SurfaceMaterial");
        public static Material BuildPreviewMaterial => LoadMaterial("Materials/Special/BuildPreviewMaterial");

        #region Textures

        private static Dictionary<string, Texture2D> CachedTextures = new Dictionary<string, Texture2D>();

        public static Texture2D LoadTexture(string fullPath)
        {
            // cached
            if (CachedTextures.TryGetValue(fullPath, out Texture2D tex)) return tex;

            // not yet cached
            Texture2D newTex = Resources.Load<Texture2D>(fullPath);
            if (newTex == null) throw new System.Exception($"Failed to load texture {fullPath}.");
            CachedTextures.Add(fullPath, newTex);
            return newTex;
        }

        #endregion
    }
}
