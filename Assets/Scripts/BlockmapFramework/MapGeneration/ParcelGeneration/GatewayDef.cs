using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// The definition of a gateway that can appear between 2 ParcelGenDef.
    /// <br/>Each ParcelGenDef can still be adjacent to any other PacelGenDef regardless of GatewayDefs, but only between ParcelGenDefs defined in a GatewayDef there will be an active gate created that ensures a connection.
    /// <br/>Unlike other Defs, GatewayDefs are not stored in a database but are created at runtime when starting a ParcelWorldGenerator in GetGatewayDefs().
    /// </summary>
    public class GatewayDef
    {
        /// <summary>
        /// DefName of ParcelGenDef that needs to be on one side of this GatewayDef.
        /// </summary>
        public string ParcelGenDef1 { get; init; }

        /// <summary>
        /// DefName of ParcelGenDef that needs to be on the other side of this GatewayDef.
        /// </summary>
        public string ParcelGenDef2 { get; init; }

        /// <summary>
        /// The gateway bet
        /// </summary>
        public int MinSize { get; init; } = 1;
    }
}
