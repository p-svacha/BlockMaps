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
        public WallType Type { get; private set; }

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

        #region Init

        public Wall(WallType type)
        {
            Type = type;
        }

        public void Init(BlockmapNode node, Direction side, int height)
        {
            Node = node;
            Side = side;
            Height = height;

            node.Walls.Add(side, this);
            node.World.Walls.Add(this);
        }

        #endregion

        #region Save / Load

        public static Wall Load(World world, WallData data)
        {
            Wall wall = world.ContentLibrary.GetWallInstance(world, data.TypeId);
            wall.Init(world.GetNode(data.NodeId), (Direction)data.Side, data.Height);
            return wall;
        }

        public WallData Save()
        {
            return new WallData
            {
                TypeId = Type.Id,
                NodeId = Node.Id,
                Side = (int)Side,
                Height = Height
            };
        }

        #endregion
    }
}
