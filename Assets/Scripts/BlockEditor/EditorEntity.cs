using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    public class EditorEntity : MovingEntity
    {
        public GameObject TargetFlag;
        private float TargetFlagScale = 0.1f;

        public void Init(World world, BlockmapNode position, bool[,,] shape, Player player, float speed, float visionRange)
        {
            MovementSpeed = speed;
            VisionRange = visionRange;
            base.Init(world, position, shape, player);

            TargetFlag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            TargetFlag.transform.SetParent(transform.parent);

            GoToRandomNode();
        }

        private void GoToRandomNode()
        {
            BlockmapNode targetNode = World.GetRandomNode();
            while (Vector2.Distance(targetNode.WorldCoordinates, OriginNode.WorldCoordinates) > 100 || targetNode == OriginNode)
            {
                targetNode = World.GetRandomNode();
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
