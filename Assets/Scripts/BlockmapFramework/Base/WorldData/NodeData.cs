using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    [Serializable]
    public class NodeData
    {
        public int Id { get; set; }
        public int LocalCoordinateX { get; set; }
        public int LocalCoordinateY { get; set; }
        public int[] Height { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]

        public NodeType Type { get; set; }
        public int SubType { get; set; } // Different for every type (i.e. surface for surface nodes, path type for air nodes)

    }
}
