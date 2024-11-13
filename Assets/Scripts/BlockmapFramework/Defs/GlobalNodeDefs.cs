using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    public static class GlobalNodeDefs
    {
        public static List<NodeDef> Defs = new List<NodeDef>()
        {
            new NodeDef()
            {
                DefName = "GroundNode",
                Label = "ground node",
                Description = "Ground nodes make up the terrain and are the bottom most layer of nodes.",
            },

            new NodeDef()
            {
                DefName = "AirNode",
                Label = "air node",
                Description = "Air nodes make up elevated paths and floors above ground level.",
            },

            new NodeDef()
            {
                DefName = "WaterNode",
                Label = "water node",
                Description = "Water nodes make up navigable water vodies.",
                Deformable = false,
                SupportsEntities = false,
            },

            new NodeDef()
            {
                DefName = "VoidNode",
                Label = "void node",
                Description = "A node to represent terrain outside of the playable world.",
                Deformable = false,
                SupportsEntities = false,
            },
        };
    }
}
