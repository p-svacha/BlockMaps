using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Singleton class to easily access all wall types.
    /// </summary>
    public class WallTypeManager
    {
        private static WallTypeManager _Instance;
        private Dictionary<WallTypeId, WallType> WallTypes;

        private WallTypeManager()
        {
            WallTypes = new Dictionary<WallTypeId, WallType>()
            {
                { WallTypeId.BrickWall, new BrickWall() },
                { WallTypeId.WoodenFence, new WoodenFence() },
            };
        }

        public static WallTypeManager Instance
        {
            get
            {
                if (_Instance == null) _Instance = new WallTypeManager();
                return _Instance;
            }
        }

        public List<WallType> GetAllWallTypes() => WallTypes.Values.ToList();
        public WallType GetWallType(WallTypeId id)
        {
            return WallTypes[id];
        }
    }
}
