using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class FenceData
    {
        public int Id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FenceTypeId TypeId { get; set; }
        
        public int NodeId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Direction Side { get; set; }
        public int Height { get; set; }
    }
}
