using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// An instance of a wall in the world. All wall-specific attributes are stored in Type.
    /// </summary>
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
        public int Height { get; private set; }

        /// <summary>
        /// The minimum y coordinate that this wall is taking up.
        /// </summary>
        public int MinHeight { get; private set; }

        /// <summary>
        /// The maximum y coordinate that this wall is taking up.
        /// </summary>
        public int MaxHeight => MinHeight + Height;

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
            MinHeight = node.GetMinHeight(side);

            node.Walls.Add(side, this);
            node.World.Walls.Add(this);
        }

        #endregion

        #region Getters

        /// <summary>
        /// Returns the y coordinate of where the wall would start when placing on the given node & side.
        /// </summary>
        public static int GetWallStartY(BlockmapNode node, Direction side)
        {
            List<Direction> relevantCorners = HelperFunctions.GetAffectedCorners(side);
            return node.Height.Where(x => relevantCorners.Contains(x.Key)).Min(x => x.Value);
        }

        #endregion

        #region Save / Load

        public static Wall Load(World world, WallData data)
        {
            WallType type = WallTypeManager.Instance.GetWallType(data.TypeId);
            Wall wall = new Wall(type);
            wall.Init(world.GetNode(data.NodeId), data.Side, data.Height);
            return wall;
        }

        public WallData Save()
        {
            return new WallData
            {
                TypeId = Type.Id,
                NodeId = Node.Id,
                Side = Side,
                Height = Height
            };
        }

        #endregion
    }
}
