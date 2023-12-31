using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WaterBody
    {
        /// <summary>
        /// The first y coordinate where nodes are not covered anymore by this water body.
        /// </summary>
        public int ShoreHeight;
        public List<BlockmapNode> CoveredNodes;

        public WaterBody() { }
        public WaterBody(WaterBody source)
        {
            ShoreHeight = source.ShoreHeight;
            CoveredNodes = new List<BlockmapNode>(source.CoveredNodes);
        }
        public WaterBody(int shoreHeight, List<BlockmapNode> coveredNodes)
        {
            ShoreHeight = shoreHeight;
            CoveredNodes = coveredNodes;
        }
    }
}
