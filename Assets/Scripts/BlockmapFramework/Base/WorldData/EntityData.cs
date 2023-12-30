using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class EntityData
    {
        public int Id { get; set; }
        public string TypeId { get; set; }
        public int OriginNodeId { get; set; }
        public int OriginNodeX { get; set; }
        public int OriginNodeY { get; set; }
        public int PlayerId { get; set; }
    }
}
