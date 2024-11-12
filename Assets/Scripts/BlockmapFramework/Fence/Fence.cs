using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Fences are a kind of barrier at the edge or corner of nodes. They follow the node topography and may be climbable.
    /// <br/>An instance represents one fence one one node in the world. All fence-specific attributes are stored in Type.
    /// </summary>
    public class Fence : IClimbable
    {
        /// <summary>
        /// Unique identifier of this specific fence.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Type containing general properties about the fence.
        /// </summary>
        public FenceType Type { get; private set; }

        /// <summary>
        /// Node that this fence piece is placed on.
        /// </summary>
        public BlockmapNode Node { get; private set; }

        /// <summary>
        /// On what side of the OriginNode this fence is placed on.
        /// </summary>
        public Direction Side { get; private set; }

        /// <summary>
        /// How many tiles high this fence is, starting at OriginNode.BaseHeight.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The minimum y coordinate that this fence is taking up.
        /// </summary>
        public int MinAltitude => Node.GetMinAltitude(Side);

        /// <summary>
        /// The maximum y coordinate that this fence is taking up.
        /// </summary>
        public int MaxAltitude => Node.GetMaxAltitude(Side) + Height - 1;

        /// <summary>
        /// Returns the height for this fence for all corners it covers as a y coordinate.
        /// </summary>
        public Dictionary<Direction, int> MaxHeights => GetMaxHeights(Node.Altitude);

        /// <summary>
        /// Returns if this fence follows a slope.
        /// </summary>
        public bool IsSloped => !Node.IsFlat(Side);

        /// <summary>
        /// The coordinates in 3d space of the base of the fence.
        /// </summary>
        public Vector3Int LocalCellCoordinates => new Vector3Int(Node.LocalCoordinates.x, MinAltitude, Node.LocalCoordinates.y);

        public bool IsClimbable => ClimbSkillRequirement != ClimbingCategory.Unclimbable && !IsSloped;

        // IClimbable
        public ClimbingCategory ClimbSkillRequirement => Type.ClimbSkillRequirement;
        public float ClimbCostUp => Type.ClimbCostUp;
        public float ClimbCostDown => Type.ClimbCostDown;
        public float ClimbSpeedUp => Type.ClimbSpeedUp;
        public float ClimbSpeedDown => Type.ClimbSpeedDown;
        public float ClimbTransformOffset => Type.Width;
        public Direction ClimbSide => Side;

        #region Init

        public Fence(FenceType type)
        {
            Type = type;
        }

        public void Init(int id, BlockmapNode node, Direction side, int height)
        {
            Id = id;
            Node = node;
            Side = side;
            Height = height;

            node.Fences.Add(side, this);
        }

        #endregion

        #region Getters

        /// <summary>
        /// Returns the y coordinate of where the fence would start when placing on the given node & side.
        /// </summary>
        public static int GetFenceStartY(BlockmapNode node, Direction side)
        {
            List<Direction> relevantCorners = HelperFunctions.GetAffectedCorners(side);
            return node.Altitude.Where(x => relevantCorners.Contains(x.Key)).Min(x => x.Value);
        }

        /// <summary>
        /// Returns a dictionary containing the maximum height of this fence for all corners it covers, given the node it is on would have the provided heights.
        /// </summary>
        public Dictionary<Direction, int> GetMaxHeights(Dictionary<Direction, int> nodeHeight)
        {
            Dictionary<Direction, int> fenceHeights = new Dictionary<Direction, int>();
            foreach(Direction dir in HelperFunctions.GetAffectedCorners(Side))
            {
                fenceHeights.Add(dir, nodeHeight[dir] + Height);
            }
            return fenceHeights;
        }

        public override string ToString()
        {
            return Node.WorldCoordinates.ToString() + " H:" + Height + "(" + MinAltitude + "-" + MaxAltitude + ") " + Side.ToString() + " " + Type.Name.ToString();
        }

        #endregion

        #region Save / Load

        public static Fence Load(World world, FenceData data)
        {
            FenceType type = FenceTypeManager.Instance.GetFenceType(data.TypeId);
            Fence fence = new Fence(type);
            fence.Init(data.Id, world.GetNode(data.NodeId), data.Side, data.Height);
            return fence;
        }

        public FenceData Save()
        {
            return new FenceData
            {
                Id = Id,
                TypeId = Type.Id,
                NodeId = Node.Id,
                Side = Side,
                Height = Height
            };
        }

        #endregion
    }
}
