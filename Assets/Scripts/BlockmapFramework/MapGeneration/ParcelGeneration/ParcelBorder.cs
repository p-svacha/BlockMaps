using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// Contains information about the adjacency of 2 parcels during world generation in a ParcelWorldGenerator.
    /// </summary>
    public class ParcelBorder
    {
        public Parcel A, B;
        public Direction SideOnA, SideOnB; // opposing sides
        public int SharedStartA; // offset along A's side
        public int SharedStartB; // offset along B's side
        public int SharedLength; // length of common border
    }
}
