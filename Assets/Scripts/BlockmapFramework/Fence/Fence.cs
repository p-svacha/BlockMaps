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
        public int MinHeight => Node.GetMinHeight(Side);

        /// <summary>
        /// The maximum y coordinate that this fence is taking up.
        /// </summary>
        public int MaxHeight => Node.GetMaxHeight(Side) + Height;

        /// <summary>
        /// Returns the height for this fence for all corners it covers as a y coordinate.
        /// </summary>
        public Dictionary<Direction, int> MaxHeights => GetMaxHeights(Node.Height);

        /// <summary>
        /// Returns if this fence follows a slope.
        /// </summary>
        public bool IsSloped => !Node.IsFlat(Side);

        public bool IsClimbable => SkillRequirement != ClimbingCategory.Unclimbable && !IsSloped;

        // IClimbable
        public ClimbingCategory SkillRequirement => Type.ClimbSkillRequirement;
        public float CostUp => Type.ClimbCostUp;
        public float CostDown => Type.ClimbCostDown;
        public float SpeedUp => Type.ClimbSpeedUp;
        public float SpeedDown => Type.ClimbSpeedDown;
        public float TransformOffset => Type.Width;
        public Direction ClimbSide => Side;
        public int MaxClimbHeight(ClimbingCategory skill)
        {
            return skill switch
            {
                ClimbingCategory.None => 0,
                ClimbingCategory.Basic => MovingEntity.MAX_BASIC_CLIMB_HEIGHT,
                ClimbingCategory.Intermediate => MovingEntity.MAX_INTERMEDIATE_CLIMB_HEIGHT,
                ClimbingCategory.Advanced => MovingEntity.MAX_ADVANCED_CLIMB_HEIGHT,
                ClimbingCategory.Unclimbable => 0,
                _ => throw new System.Exception("category " + skill.ToString() + " not handled.")
            };
        }

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
            return node.Height.Where(x => relevantCorners.Contains(x.Key)).Min(x => x.Value);
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
            return Node.WorldCoordinates.ToString() + " " + Node.BaseHeight + " " + Side.ToString() + " " + Type.Name.ToString();
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