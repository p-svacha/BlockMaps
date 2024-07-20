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

        [JsonConverter(typeof(StringEnumConverter))]
        public WallTypeId TypeId { get; set; }
        
        public int NodeId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Direction Side { get; set; }
        public int Height { get; set; }
    }
}
