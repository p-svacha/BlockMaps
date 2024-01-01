using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    [Serializable]
    public class WaterBodyData
    {
        public int Id { get; set; }
        public int ShoreHeight { get; set; }
        public List<int> WaterNodes { get; set; }
        public List<int> CoveredNodes { get; set; } // index 0 here refers to the SurfaceNode that WaterNodes[0] covers, etc.
    }
}
