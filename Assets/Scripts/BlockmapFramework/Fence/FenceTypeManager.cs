using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Singleton class to easily access all fence types.
    /// </summary>
    public class FenceTypeManager
    {
        private static FenceTypeManager _Instance;
        private Dictionary<FenceTypeId, FenceType> FenceTypes;

        private FenceTypeManager()
        {
            FenceTypes = new Dictionary<FenceTypeId, FenceType>()
            {
                //{ FenceTypeId.BrickWall, new FT01_BrickWall() },
                { FenceTypeId.WoodenFence, new FT02_WoodenFence() },
                //{ FenceTypeId.Cliff, new FT03_Cliff() },
            };
        }

        public static FenceTypeManager Instance
        {
            get
            {
                if (_Instance == null) _Instance = new FenceTypeManager();
                return _Instance;
            }
        }

        public List<FenceType> GetAllFenceTypes() => FenceTypes.Values.ToList();
        public FenceType GetFenceType(FenceTypeId id)
        {
            return FenceTypes[id];
        }
    }
}
