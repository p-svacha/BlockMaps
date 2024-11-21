using BlockmapFramework;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldEditor
{
    /// <summary>
    /// A dynamic entity for the editor where all movement attributes can be set to custom values.
    /// </summary>
    public class EditorMovingEntity : Entity
    {
        public GameObject TargetFlag;
        private float TargetFlagScale = 0.1f;

        private float customSpeed;
        private float customVisionRange;
        private bool customCanSwin;
        private ClimbingCategory customClimbSkill;
        private int customMaxHopUpDistance;
        private int customMaxHopDownDistance;

        private Comp_Movement MovementComp;

        public override float VisionRange => customVisionRange;

        protected override void OnInitialized()
        {
            MeshObject.transform.localScale = new Vector3(1f, 1f * Dimensions.y, 1f);

            TargetFlag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            TargetFlag.transform.SetParent(MeshObject.transform.parent);
            TargetFlag.gameObject.SetActive(false);

            MovementComp = GetComponent<Comp_Movement>();
            MovementComp.OnNewPath += OnNewPath;
            MovementComp.OnStopMoving += OnStopMoving;

            MovementComp.EnableOverrideMovementSpeed(customSpeed);
            MovementComp.EnableOverrideCanSwim(customCanSwin);
            MovementComp.EnableOverrideClimbSkill(customClimbSkill);
        }

        public void PreInit(float speed, float visionRange, bool canSwim, ClimbingCategory climbSkill, int maxHopDistance, int maxDropDistance)
        {
            customSpeed = speed;
            customVisionRange = visionRange;
            customCanSwin = canSwim;
            customClimbSkill = climbSkill;
            customMaxHopUpDistance = maxHopDistance;
            customMaxHopDownDistance = maxDropDistance;
        }

        private void GoToRandomNode()
        {
            BlockmapNode targetNode = World.GetRandomPassableNode(this);
            while (Vector2.Distance(targetNode.WorldCoordinates, OriginNode.WorldCoordinates) > 100 || targetNode == OriginNode)
            {
                targetNode = World.GetRandomPassableNode(this);
            }
            MovementComp.MoveTo(targetNode);
        }

        private void OnNewPath()
        {
            TargetFlag.transform.position = MovementComp.Target.CenterWorldPosition;
            TargetFlag.transform.localScale = new Vector3(TargetFlagScale, 1f, TargetFlagScale);
            TargetFlag.GetComponent<MeshRenderer>().material.color = Color.red;
            TargetFlag.gameObject.SetActive(true);
        }

        protected void OnStopMoving()
        {
            TargetFlag.gameObject.SetActive(false);
        }

        public override void ExposeDataForSaveAndLoad()
        {
            base.ExposeDataForSaveAndLoad();

            SaveLoadManager.SaveOrLoadPrimitive(ref customSpeed, "customSpeed");
            SaveLoadManager.SaveOrLoadPrimitive(ref customVisionRange, "customVisionRange");
            SaveLoadManager.SaveOrLoadPrimitive(ref customCanSwin, "customCanSwim");
            SaveLoadManager.SaveOrLoadPrimitive(ref customClimbSkill, "customClimbSkill");
            SaveLoadManager.SaveOrLoadPrimitive(ref customMaxHopUpDistance, "customMaxHopUpDistance");
            SaveLoadManager.SaveOrLoadPrimitive(ref customMaxHopDownDistance, "customMaxHopDownDistance");
        }
    }
}
