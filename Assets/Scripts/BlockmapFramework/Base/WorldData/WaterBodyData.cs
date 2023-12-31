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
        public List<int> CoveredNodes { get; set; }
    }
}
