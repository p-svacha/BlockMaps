using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Singleton class to easily access all surface properties.
    /// </summary>
    public class SurfaceManager
    {
        private static SurfaceManager _Instance;
        private Dictionary<SurfacePropertyId, SurfaceProperties> SurfaceProperties;
        private Dictionary<SurfaceId, Surface> Surfaces;

        private SurfaceManager()
        {
            // Defines all surface properties
            SurfaceProperties = new Dictionary<SurfacePropertyId, SurfaceProperties>()
            {
                {SurfacePropertyId.Grass, new SP01_Grass() },
                {SurfacePropertyId.Sand, new SP02_Sand() },
                {SurfacePropertyId.Water, new SP03_Water() },
                {SurfacePropertyId.Default, new SP04_Default() },
                {SurfacePropertyId.DirtPath, new SP05_DirtPath() },
            };

            // Define all surfaces
            Surfaces = new Dictionary<SurfaceId, Surface>()
            {
                { SurfaceId.Grass, new S001_Grass(this) },
                { SurfaceId.Sand, new S002_Sand(this) },
                { SurfaceId.Concrete, new S003_Concrete(this) },
                { SurfaceId.Street, new S004_Street(this) },
                { SurfaceId.RoofingTiles, new S005_RoofingTiles(this) },
                { SurfaceId.WoodParquet, new S006_WoodParquet(this) },
                { SurfaceId.Tiles, new S007_Tiles(this) },
                { SurfaceId.DirtPath, new S008_DirtPath(this) },
            };

            // Pass terrain colors to surface material
            Color[] terrainColors = new Color[Surfaces.Count];
            int counter = 0;
            foreach(Surface s in Surfaces.Values) terrainColors[counter++] = s.PreviewColor;
            ResourceManager.Singleton.SurfaceMaterial.SetColorArray("_TerrainColors", terrainColors);
            
            // Pass terrain textures to surface material
            Texture2DArray terrainTexArray = new Texture2DArray(1024, 1024, Surfaces.Count, TextureFormat.RGBA32, true);
            counter = 0;
            foreach (Surface s in Surfaces.Values)
            {
                int index = counter++;
                if (s.BlendingTexture != null) terrainTexArray.SetPixels32(s.BlendingTexture.GetPixels32(), index);
            }
            terrainTexArray.Apply();
            ResourceManager.Singleton.SurfaceMaterial.SetTexture("_TerrainTextures", terrainTexArray);
        }

        public static SurfaceManager Instance
        {
            get
            {
                if (_Instance == null) _Instance = new SurfaceManager();
                return _Instance;
            }
        }

        public List<Surface> GetAllSurfaces() => Surfaces.Values.ToList();
        public Surface GetSurface(SurfaceId id)
        {
            return Surfaces[id];
        }
        public SurfaceProperties GetSurfaceProperties(SurfacePropertyId id) => SurfaceProperties[id];
    }
}
