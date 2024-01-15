using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    [Serializable]
    public class EntityData
    {
        public int Id { get; set; }
        public string TypeId { get; set; }
        public int OriginNodeId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Direction Rotation { get; set; }
        public int PlayerId { get; set; }
    }
}
