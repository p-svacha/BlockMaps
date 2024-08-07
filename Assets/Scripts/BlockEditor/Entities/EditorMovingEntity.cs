using BlockmapFramework;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class EditorMovingEntity : MovingEntity
    {
        public GameObject TargetFlag;
        private float TargetFlagScale = 0.1f;

        public void PreInit(float speed, float visionRange, int height, bool canSwim, ClimbingCategory climbSkill)
        {
            if (height < 0) throw new System.Exception("Height can't be smaller than 1");

            IsPassable = true;
            MovementSpeed = speed;
            VisionRange = visionRange;
            Dimensions = new Vector3Int(1, height, 1);
            CanSwim = canSwim;
            ClimbingSkill = climbSkill;

            TypeId = Name + "_" + MovementSpeed + "_" + VisionRange + "_" + Dimensions.y + "_" + CanSwim + "_" + (int)ClimbingSkill;

            transform.localScale = new Vector3(1f, 1f * Dimensions.y, 1f);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            TargetFlag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            TargetFlag.transform.SetParent(transform.parent);
            TargetFlag.gameObject.SetActive(false);
        }

        private void GoToRandomNode()
        {
            BlockmapNode targetNode = World.GetRandomPassableNode(this);
            while (Vector2.Distance(targetNode.WorldCoordinates, OriginNode.WorldCoordinates) > 100 || targetNode == OriginNode)
            {
                targetNode = World.GetRandomPassableNode(this);
            }
            MoveTo(targetNode);
        }

        protected override void OnNewPath()
        {
            TargetFlag.transform.position = Target.CenterWorldPosition;
            TargetFlag.transform.localScale = new Vector3(TargetFlagScale, 1f, TargetFlagScale);
            TargetFlag.GetComponent<MeshRenderer>().material.color = Color.red;
            TargetFlag.gameObject.SetActive(true);
        }

        protected override void OnStopMoving()
        {
            TargetFlag.gameObject.SetActive(false);
        }
    }
}
