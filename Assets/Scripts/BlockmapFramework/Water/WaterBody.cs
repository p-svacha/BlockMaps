using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public class WaterBody
    {
        public int Id { get; private set; }
        /// <summary>
        /// The first y coordinate where nodes are not covered anymore by this water body.
        /// </summary>
        public int ShoreHeight;
        public List<BlockmapNode> CoveredNodes;

        public WaterBody() { }
        public WaterBody(int id, WaterBody source)
        {
            Id = id;
            ShoreHeight = source.ShoreHeight;
            CoveredNodes = new List<BlockmapNode>(source.CoveredNodes);
        }
        public WaterBody(int id, int shoreHeight, List<BlockmapNode> coveredNodes)
        {
            Id = id;
            ShoreHeight = shoreHeight;
            CoveredNodes = coveredNodes;
        }

        #region Save / Load

        public static WaterBody Load(World world, WaterBodyData data)
        {
            return new WaterBody(data.Id, data.ShoreHeight, data.CoveredNodes.Select(x => world.GetNode(x)).ToList());
        }

        public WaterBodyData Save()
        {
            return new WaterBodyData
            {
                Id = Id,
                ShoreHeight = ShoreHeight,
                CoveredNodes = CoveredNodes.Select(x => x.Id).ToList()
            };
        }

        #endregion
    }
}
