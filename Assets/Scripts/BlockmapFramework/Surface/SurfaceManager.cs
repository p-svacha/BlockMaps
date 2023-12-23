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
        private Dictionary<SurfaceId, Surface> Surfaces;

        private SurfaceManager()
        {
            Surfaces = new Dictionary<SurfaceId, Surface>()
            {
                { SurfaceId.Grass, new GrassSurface() },
                { SurfaceId.Sand, new SandSurface() },
                { SurfaceId.Tarmac, new TarmacSurface() }
            };

            ResourceManager.Singleton.SurfaceMaterial.SetColor("_GrassColor", Surfaces[SurfaceId.Grass].Color);
            ResourceManager.Singleton.SurfaceMaterial.SetColor("_SandColor", Surfaces[SurfaceId.Sand].Color);
            ResourceManager.Singleton.SurfaceMaterial.SetColor("_TarmacColor", Surfaces[SurfaceId.Tarmac].Color);
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
    }
}
