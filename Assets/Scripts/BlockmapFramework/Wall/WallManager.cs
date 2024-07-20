using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Singleton class to easily access all wall shapes and wall materials.
    /// </summary>
    public class WallManager
    {
        private static WallManager _Instance;
        private Dictionary<WallShapeId, WallShape> WallShapes;
        private Dictionary<WallMaterialId, WallMaterial> WallMaterials;

        private WallManager()
        {
            WallShapes = new Dictionary<WallShapeId, WallShape>()
            { 
                { WallShapeId.Solid, new WS001_Solid() }
            };
            WallMaterials = new Dictionary<WallMaterialId, WallMaterial>()
            {
                { WallMaterialId.Brick, new WM001_Brick() }
            };
        }

        public static WallManager Instance
        {
            get
            {
                if (_Instance == null) _Instance = new WallManager();
                return _Instance;
            }
        }

        public List<WallShape> GetAllWallShapes() => WallShapes.Values.ToList();
        public List<WallMaterial> GetAllWallMaterials() => WallMaterials.Values.ToList();

        public WallShape GetWallShape(WallShapeId id) => WallShapes[id];
        public WallMaterial GetWallMaterial(WallMaterialId id) => WallMaterials[id];
    }
}
