using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    [Serializable]
    public class ZoneData
    {
        public int Id { get; set; }
        public int ActorId { get; set; }
        public bool ShowBorders { get; set; }
        public bool ProvidesVision { get; set; }
        public List<Tuple<int, int>> Coordinates { get; set; }
    }
}
