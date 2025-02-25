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
    public class Fence : WorldDatabaseObject, IClimbable, ISaveAndLoadable
    {
        private World World;

        private int id;
        public override int Id => id;

        /// <summary>
        /// Def containing the blueprint of the fence.
        /// </summary>
        public FenceDef Def;

        /// <summary>
        /// Node that this fence piece is placed on.
        /// </summary>
        public BlockmapNode Node;

        /// <summary>
        /// The chunk that the fence is on.
        /// </summary>
        public Chunk Chunk => Node.Chunk;

        /// <summary>
        /// On what side of the OriginNode this fence is placed on.
        /// </summary>
        public Direction Side;

        /// <summary>
        /// How many tiles high this fence is, starting at OriginNode.BaseHeight.
        /// </summary>
        public int Height;

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
        /// The local coordinates of the node within the chunk that this fence is on.
        /// </summary>
        public Vector2Int LocalCoordinates => Node.LocalCoordinates;

        public bool IsClimbable => ClimbSkillRequirement != ClimbingCategory.Unclimbable && !IsSloped;

        // IClimbable
        public ClimbingCategory ClimbSkillRequirement => Def.ClimbSkillRequirement;
        public float ClimbCostUp => Def.ClimbCostUp;
        public float ClimbCostDown => Def.ClimbCostDown;
        public float ClimbTransformOffset => Def.Width;
        public Direction ClimbSide => Side;

        #region Init

        public Fence() { }
        public Fence(World world, FenceDef def, int id, BlockmapNode node, Direction side, int height)
        {
            World = world;
            Def = def;
            this.id = id;
            Node = node;
            Side = side;
            Height = height;
        }

        public override void PostLoad()
        {
            World.RegisterFence(this, registerInWorld: false);
        }

        #endregion

        #region Getters
        public virtual string Label => Def.Label;
        public virtual string LabelCap => Label.CapitalizeFirst();
        public virtual string Description => Def.Description;
        public virtual bool BlocksVision => Def.BlocksVision;

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
            return Node.WorldCoordinates.ToString() + " H:" + Height + "(" + MinAltitude + "-" + MaxAltitude + ") " + Side.ToString() + " " + Label.ToString();
        }

        #endregion

        #region Save / Load

        public virtual void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadDef(ref Def, "def");
            SaveLoadManager.SaveOrLoadReference(ref Node, "node");
            SaveLoadManager.SaveOrLoadPrimitive(ref Side, "side");
            SaveLoadManager.SaveOrLoadPrimitive(ref Height, "height");
        }

        #endregion
    }
}
