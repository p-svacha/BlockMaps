using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WallData
    {
        public int Id { get; set; }
        public int CellX { get; set; }
        public int CellY { get; set; }
        public int CellZ { get; set; }
        public int Side { get; set; }
        public string WallShapeDef { get; set; }
        public string WallMaterialDef { get; set; }
        public bool IsMirrored { get; set; }
    }
}
