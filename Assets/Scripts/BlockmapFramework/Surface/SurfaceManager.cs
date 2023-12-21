using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Singleton class to easily access all surface properties.
    /// </summary>
    public class SurfaceManager : MonoBehaviour
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
        }

        public static SurfaceManager Instance
        {
            get
            {
                if (_Instance == null) _Instance = new SurfaceManager();
                return _Instance;
            }
        }

        public Surface GetSurface(SurfaceId id)
        {
            return Surfaces[id];
        }
    }
}
