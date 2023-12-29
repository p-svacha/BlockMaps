using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class EditorMovingEntity : MovingEntity
    {
        public GameObject TargetFlag;
        private float TargetFlagScale = 0.1f;

        public void PreInit(float speed, float visionRange)
        {
            IsPassable = true;
            MovementSpeed = speed;
            VisionRange = visionRange;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (MovementSpeed > 0)
            {
                TargetFlag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                TargetFlag.transform.SetParent(transform.parent);
                GoToRandomNode();
            }
        }

        public override void UpdateEntity()
        {
            base.UpdateEntity();

            if (TargetPath == null && MovementSpeed > 0) GoToRandomNode();
        }

        private void GoToRandomNode()
        {
            BlockmapNode targetNode = World.GetRandomPassableNode();
            while (Vector2.Distance(targetNode.WorldCoordinates, OriginNode.WorldCoordinates) > 100 || targetNode == OriginNode)
            {
                targetNode = World.GetRandomPassableNode();
            }
            GoTo(targetNode);
        }

        protected override void OnNewTarget()
        {
            TargetFlag.transform.position = Target.GetCenterWorldPosition();
            TargetFlag.transform.localScale = new Vector3(TargetFlagScale, 1f, TargetFlagScale);
            TargetFlag.GetComponent<MeshRenderer>().material.color = Color.red;
        }

        protected override void OnTargetReached()
        {
            GoToRandomNode();
        }
    }
}
