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
        public BlockmapNode Bottom => OriginNode;
        /// <summary>
        /// Node that the top of the ladder leads to. Always adjacent to OriginNode.
        /// </summary>
        public BlockmapNode Target;

        /// <summary>
        /// The side that the ladder stands on on Source. [N/E/S/W]
        /// </summary>
        public Direction Side => Rotation;

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
        public float ClimbTransformOffset => LadderMeshGenerator.LADDER_POLE_SIZE;
        public Direction ClimbSide => Side;
        public bool IsClimbable => true;

        #region Init
        
        public void PreInit(BlockmapNode target)
        {
            // Ladder specific
            Target = target;
            LadderEndAltitude = target.GetMaxAltitude(HelperFunctions.GetOppositeDirection(Side));
            LadderStartAltitude = Bottom.GetMinAltitude(Side);
            LadderHeight = LadderEndAltitude - LadderStartAltitude;

            // Entity general
            overrideHeight = LadderHeight;
        }

        protected override void OnPostLoad()
        {
            LadderEndAltitude = Target.GetMaxAltitude(HelperFunctions.GetOppositeDirection(Side));
            LadderStartAltitude = Bottom.GetMinAltitude(Side);
            LadderHeight = LadderEndAltitude - LadderStartAltitude;
        }


        public override void OnRegister()
        {
            Bottom.SourceLadders.Add(Side, this);
            Target.TargetLadders.Add(HelperFunctions.GetOppositeDirection(Side), this);
        }
        public override void OnDeregister()
        {
            Bottom.SourceLadders.Remove(Side);
            Target.TargetLadders.Remove(HelperFunctions.GetOppositeDirection(Side));
        }

        #endregion

        public static Vector3 GetLadderWorldPosition(EntityDef def, World world, BlockmapNode originNode, Direction rotation, bool isMirrored)
        {
            Vector3 nodeCenter = originNode.MeshCenterWorldPosition;
            float worldHeight = originNode.GetMinAltitude(rotation) * World.NodeHeight;
            return new Vector3(nodeCenter.x, worldHeight, nodeCenter.z);
        }

        public override void ExposeDataForSaveAndLoad()
        {
            base.ExposeDataForSaveAndLoad();

            SaveLoadManager.SaveOrLoadReference(ref Target, "topNode");
        }
    }
}
