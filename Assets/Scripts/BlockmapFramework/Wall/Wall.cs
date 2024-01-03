using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Wall
    {
        /// <summary>
        /// Type containing general properties about the wall.
        /// </summary>
        public WallType Properties { get; private set; }

        /// <summary>
        /// Node that this wall piece is placed on.
        /// </summary>
        public BlockmapNode Node { get; private set; }

        /// <summary>
        /// On what side of the OriginNode this wall is placed on.
        /// </summary>
        public Direction Side { get; private set; }

        /// <summary>
        /// How many tiles high this wall is, starting at OriginNode.BaseHeight.
        /// </summary>
        public int Height { get; protected set; }
        
        public Wall(WallType type, BlockmapNode originNode, Direction side, int height)
        {
            Properties = type;
            Node = originNode;
            Side = side;
            Height = height;
        }

        #region Save / Load

        public static Wall Load(World world, WallData data)
        {
            Wall wall = world.ContentLibrary.GetWallInstance(world, data);

            return wall;
        }

        public WallData Save()
        {
            return new WallData
            {
                TypeId = Properties.Id,
                NodeId = Node.Id,
                Side = (int)Side,
                Height = Height
            };
        }

        #endregion
    }
}
