using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Ladder : Entity, IClimbable
    {
        public const string LADDER_ENTITY_NAME = "ladder";

        /// <summary>
        /// Node that the bottom of the ladder is standing on
        /// </summary>
        public BlockmapNode Bottom { get; private set; }
        /// <summary>
        /// Node that the top of the ladder leads to. Always adjacent to Source.
        /// </summary>
        public BlockmapNode Top { get; private set; }

        /// <summary>
        /// The side that the ladder stands on on Source. [N/E/S/W]
        /// </summary>
        public Direction Side { get; private set; }

        /// <summary>
        /// Height at which the ladder starts.
        /// </summary>
        public int LadderStartAltitude { get; private set; }

        /// <summary>
        /// Altitude above where the ladder ends.
        /// </summary>
        public int LadderEndAltitude { get; private set; }

        /// <summary>
        /// How hight the ladder is
        /// </summary>
        public int LadderHeight { get; private set; }


        // IClimbable
        public ClimbingCategory ClimbSkillRequirement => ClimbingCategory.Basic;
        public float ClimbCostUp => 1.6f;
        public float ClimbCostDown => 1.3f;
        public float ClimbSpeedUp => 0.65f;
        public float ClimbSpeedDown => 0.75f;
        public float ClimbTransformOffset => LadderMeshGenerator.LADDER_POLE_SIZE;
        public Direction ClimbSide => Side;
        public bool IsClimbable => true;

        #region Init

        public void InitLadder(BlockmapNode source, BlockmapNode target, Direction side)
        {
            // Ladder specific
            Bottom = source;
            Top = target;
            Side = side;
            LadderEndAltitude = target.GetMaxAltitude(HelperFunctions.GetOppositeDirection(side));
            LadderStartAltitude = source.GetMinAltitude(side);
            LadderHeight = LadderEndAltitude - LadderStartAltitude;

            // Entity general
            Name = "Ladder";
            TypeId = LADDER_ENTITY_NAME + "_" + Top.Id;
            Dimensions = new Vector3Int(1, LadderHeight, 1);
            BlocksVision = false;
            IsPassable = true;
        }

        public override void OnRegister()
        {
            Bottom.SourceLadders.Add(Side, this);
            Top.TargetLadders.Add(HelperFunctions.GetOppositeDirection(Side), this);
        }
        public override void OnDeregister()
        {
            Bottom.SourceLadders.Remove(Side);
            Top.TargetLadders.Remove(HelperFunctions.GetOppositeDirection(Side));
        }

        public static Ladder GetInstance(World world, EntityData data)
        {
            string[] attributes = data.TypeId.Split('_');
            int targetNodeId = int.Parse(attributes[1]);
            BlockmapNode sourceNode = world.GetNode(data.OriginNodeId);
            BlockmapNode targetNode = world.GetNode(targetNodeId);

            return GetInstance(sourceNode, targetNode, data.Rotation);
        }
        public static Ladder GetInstance(BlockmapNode source, BlockmapNode target, Direction side)
        {
            Ladder instance = LadderMeshGenerator.GenerateLadderObject(source, target, side);
            instance.InitLadder(source, target, side);
            return instance;
        }

        #endregion

        public override Vector3 GetWorldPosition(World world, BlockmapNode originNode, Direction rotation)
        {
            Vector3 nodeCenter = originNode.CenterWorldPosition;
            float worldHeight = LadderStartAltitude * World.TILE_HEIGHT;
            return new Vector3(nodeCenter.x, worldHeight, nodeCenter.z);
        }
    }
}
