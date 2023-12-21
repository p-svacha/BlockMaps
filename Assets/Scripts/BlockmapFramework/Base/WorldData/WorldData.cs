using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    [Serializable]
    public class WorldData
    {
        public string Name;
        public int ChunkSize { get; set; }
        public int MaxNodeId { get; set; }
        public List<ChunkData> Chunks { get; set; }
    }
}
