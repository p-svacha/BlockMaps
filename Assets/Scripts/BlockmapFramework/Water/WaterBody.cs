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
        public List<WaterNode> WaterNodes;
        public List<SurfaceNode> CoveredNodes;

        public int MinX { get; private set; }
        public int MaxX { get; private set; }
        public int MinY { get; private set; }
        public int MaxY { get; private set; }

        public WaterBody() { }
        public WaterBody(int id, int shoreHeight, List<WaterNode> waterNodes, List<SurfaceNode> coveredNodes)
        {
            Id = id;
            ShoreHeight = shoreHeight;
            WaterNodes = new List<WaterNode>(waterNodes);
            CoveredNodes = new List<SurfaceNode>(coveredNodes);

            // Init references
            for (int i = 0; i < waterNodes.Count; i++) WaterNodes[i].Init(this, coveredNodes[i]);
            CoveredNodes = WaterNodes.Select(x => x.SurfaceNode).ToList();

            MinX = WaterNodes.Min(x => x.WorldCoordinates.x);
            MaxX = WaterNodes.Max(x => x.WorldCoordinates.x);
            MinY = WaterNodes.Min(x => x.WorldCoordinates.y);
            MaxY = WaterNodes.Max(x => x.WorldCoordinates.y);
    }

        #region Getters

        public float WaterSurfaceWorldHeight => ((ShoreHeight - 1) * World.TILE_HEIGHT) + (World.WATER_HEIGHT * World.TILE_HEIGHT);

        #endregion

        #region Save / Load

        public static WaterBody Load(World world, WaterBodyData data)
        {
            return new WaterBody(data.Id, data.ShoreHeight, data.WaterNodes.Select(x => world.GetNode(x) as WaterNode).ToList(), data.CoveredNodes.Select(x => world.GetNode(x) as SurfaceNode).ToList());
        }

        public WaterBodyData Save()
        {
            return new WaterBodyData
            {
                Id = Id,
                ShoreHeight = ShoreHeight,
                WaterNodes = WaterNodes.Select(x => x.Id).ToList(),
                CoveredNodes = WaterNodes.Select(x => x.SurfaceNode.Id).ToList()
            };
        }

        #endregion
    }
}
