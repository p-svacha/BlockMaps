using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    [Serializable]
    public class ChunkData
    {
        public int ChunkCoordinateX { get; set; }
        public int ChunkCoordinateY { get; set; }
        public List<NodeData> Nodes { get; set; }
    }
}
