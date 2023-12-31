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
        public int MaxEntityId { get; set; }
        public int MaxWaterBodyId { get; set; }
        public List<ChunkData> Chunks { get; set; }
        public List<PlayerData> Players { get; set; }
        public List<EntityData> Entities { get; set; }
        public List<WaterBodyData> WaterBodies { get; set; }
        public List<WallData> Walls { get; set; }
    }
}
