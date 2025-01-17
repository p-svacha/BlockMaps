using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class Door : Entity
    {
        public const string DOOR_ENTITY_NAME = "door";

        /// <summary>
        /// How high the door is.
        /// </summary>
        public int DoorHeight { get; private set; }

        /// <summary>
        /// Lowest y coordinate of the door.
        /// </summary>
        public int DoorMinAltitude { get; private set; }

        /// <summary>
        /// Highest y coordinate of the door.
        /// </summary>
        public int DoorMaxAltitude { get; private set; }

        /// <summary>
        /// Flag if the door is open or closed.
        /// </summary>
        public bool IsOpen;

        /// <summary>
        /// Returns the direction on its OriginNode that this bloor is currently blocking, based on if it is open or closed.
        /// </summary>
        public Direction CurrentBlockingDirection => IsOpen ? (IsMirrored ? HelperFunctions.GetPreviousSideDirection(Rotation) : HelperFunctions.GetNextSideDirection(Rotation)) : Rotation;

        private Quaternion ClosedRotation => HelperFunctions.Get2dRotationByDirection(Rotation);
        private Quaternion OpenRotation => IsMirrored ? HelperFunctions.Get2dRotationByDirection(HelperFunctions.GetNextSideDirection(Rotation)) : HelperFunctions.Get2dRotationByDirection(HelperFunctions.GetPreviousSideDirection(Rotation));


        // Open/close animation
        private Quaternion targetRotation;
        private Quaternion startingRotation;
        private float elapsedTime;
        private float rotationDuration = 1f; // Duration for the full rotation in seconds
        private bool isRotating = false;
        private System.Action ToggleCallback;

        #region Init

        public override void OnRegister()
        {
            OriginNode.Doors.Add(Rotation, this);
        }
        public override void OnDeregister()
        {
            OriginNode.Doors.Remove(Rotation);
        }

        protected override void CreateVisionCollider()
        {
            // A door has 2 non-moving vision colliders in place for open and closed state. Because if you see one of those, you know the state for sure.

            // Parent objects of both colliders
            VisionColliderObject = new GameObject("visionCollider");
            VisionColliderObject.transform.SetParent(Wrapper.transform);

            // Vision collider for closed state
            GameObject closedVisionColliderObj = new GameObject("visionCollider_closed");
            closedVisionColliderObj.layer = World.Layer_EntityVisionCollider;
            closedVisionColliderObj.transform.SetParent(VisionColliderObject.transform);

            MeshCollider closedCollider = closedVisionColliderObj.AddComponent<MeshCollider>();
            closedCollider.sharedMesh = MeshCollider.sharedMesh;
            closedCollider.transform.rotation = Quaternion.Euler(0, 0, 0);

            WorldObjectCollider closedEvc = closedVisionColliderObj.AddComponent<WorldObjectCollider>();
            closedEvc.Object = this;
            closedEvc.State = 0;

            // Vision collider for open state
            GameObject openVisionColliderObj = new GameObject("visionCollider_closed");
            openVisionColliderObj.layer = World.Layer_EntityVisionCollider;
            openVisionColliderObj.transform.SetParent(VisionColliderObject.transform);

            MeshCollider openCollider = openVisionColliderObj.AddComponent<MeshCollider>();
            openCollider.sharedMesh = MeshCollider.sharedMesh;
            openCollider.transform.rotation = IsMirrored ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, -90, 0);

            WorldObjectCollider openEvc = openVisionColliderObj.AddComponent<WorldObjectCollider>();
            openEvc.Object = this;
            openEvc.State = 1;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Opens or closes the door based on its current state.
        /// </summary>
        public void Toggle(System.Action callback  = null)
        {
            if (isRotating) return; // Prevent toggling during rotation

            ToggleCallback = callback;

            startingRotation = WorldRotation;
            targetRotation = IsOpen ? ClosedRotation : OpenRotation;
            elapsedTime = 0f;
            isRotating = true;

            IsOpen = !IsOpen;
            World.UpdateNavmesh(new Parcel(OriginNode));
        }

        protected override void OnTick()
        {
            if (!isRotating) return;

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / rotationDuration);
            WorldRotation = Quaternion.Slerp(startingRotation, targetRotation, progress);

            UpdateVisibility();

            if (progress >= 1f)
            {
                isRotating = false; // Rotation is complete
                WorldRotation = targetRotation;
                UpdateVisibility();
                World.UpdateVisionOfNearbyEntitiesDelayed(OriginNode.MeshCenterWorldPosition, callback: ToggleCallback);
            }
        }


        #endregion

        public override void ResetWorldPositonAndRotation()
        {
            base.ResetWorldPositonAndRotation();
            WorldRotation = IsOpen ? OpenRotation : ClosedRotation;
        }

        #region Getters

        public override bool BlocksVision(WorldObjectCollider collider)
        {
            if (collider.State == 0 && !IsOpen) return true;
            if (collider.State == 1 && IsOpen) return true;
            return false;
        }

        public static Vector3 GetWorldPosition(EntityDef def, World world, BlockmapNode originNode, Direction rotation, int height, bool isMirrored)
        {
            if (def.Dimensions.x != 1 || def.Dimensions.z != 1) throw new System.Exception("GetWorldPosition not yet implemented for doors that are not 1x1.");

            Vector3 basePosition = originNode.MeshCenterWorldPosition;
            float worldY = World.NodeHeight * originNode.GetMinAltitude(rotation);

            Vector3 offsetPosition = basePosition + Door.GetWorldPositionOffset(rotation, isMirrored);
            return new Vector3(offsetPosition.x, worldY, offsetPosition.z);
        }

        public static Vector3 GetWorldPositionOffset(Direction rotation, bool isMirrored)
        {
            float offsetValue = 0.5f - (DOOR_WIDTH / 2f);

            if (isMirrored)
            {
                return rotation switch
                {
                    Direction.S => new Vector3(offsetValue, 0f, -offsetValue),
                    Direction.E => new Vector3(offsetValue, 0f, offsetValue),
                    Direction.N => new Vector3(-offsetValue, 0f, offsetValue),
                    Direction.W => new Vector3(-offsetValue, 0f, -offsetValue),
                    _ => throw new System.Exception("Direction not handled")
                };
            }

            else
            {
                return rotation switch
                {
                    Direction.S => new Vector3(-offsetValue, 0f, -offsetValue),
                    Direction.E => new Vector3(offsetValue, 0f, -offsetValue),
                    Direction.N => new Vector3(offsetValue, 0f, offsetValue),
                    Direction.W => new Vector3(-offsetValue, 0f, offsetValue),
                    _ => throw new System.Exception("Direction not handled")
                };
            }
        }

        #endregion

        #region Save / Load

        public override void ExposeDataForSaveAndLoad()
        {
            base.ExposeDataForSaveAndLoad();

            SaveLoadManager.SaveOrLoadPrimitive(ref IsOpen, "isOpen");
        }

        #endregion

        #region Mesh Generation

        private const float DOOR_WIDTH = 0.05f;

        private const float HANDLE_Y_ABSOLUTE = 1f;
        private const float MIN_HANDLE_MARGIN_TOP = 0.2f;
        private const float HANDLE_MARGIN_X = 0.1f;
        private const float HANDLE_SIZE = 0.1f;

        public static void GenerateDoorMesh(MeshBuilder meshBuilder, int height, bool isMirrored, bool isPreview)
        {
            int doorSubmesh = meshBuilder.GetSubmesh(isPreview ? MaterialManager.BuildPreviewMaterial : MaterialManager.LoadMaterial("Materials/NodeMaterials/WoodParquet"));
            int handleSubmesh = meshBuilder.GetSubmesh(MaterialManager.LoadMaterial("Materials/Special/LadderMaterial"));

            // Anchor point (needed for correct door rotation)
            float anchorPointOffsetX = DOOR_WIDTH / 2f;
            float anchorPointOffsetY = DOOR_WIDTH / 2f;
            Vector2 anchorPoint;
            if (isMirrored) anchorPoint = new Vector2(-(1f - anchorPointOffsetX), -anchorPointOffsetY);
            else anchorPoint = new Vector2(-anchorPointOffsetX, -anchorPointOffsetY);

            // Door
            float doorHeight = height * World.NodeHeight;
            Vector3 pos = new Vector3(anchorPoint.x, 0f, anchorPoint.y);
            Vector3 dim = new Vector3(1f, doorHeight, DOOR_WIDTH);
            meshBuilder.BuildCube(doorSubmesh, pos, dim);

            // Handle
            float handleX = isMirrored ? HANDLE_MARGIN_X : (1f - HANDLE_MARGIN_X - HANDLE_SIZE);
            float handleY = (doorHeight < (HANDLE_Y_ABSOLUTE + MIN_HANDLE_MARGIN_TOP) ? doorHeight - MIN_HANDLE_MARGIN_TOP : HANDLE_Y_ABSOLUTE);
            Vector3 handle_pos = new Vector3(handleX + anchorPoint.x, handleY, (-(HANDLE_SIZE - DOOR_WIDTH) / 2) + anchorPoint.y);
            Vector3 handle_dim = new Vector3(HANDLE_SIZE, HANDLE_SIZE, HANDLE_SIZE);
            meshBuilder.BuildCube(handleSubmesh, handle_pos, handle_dim);

            meshBuilder.ApplyMesh();
        }

        #endregion
    }
}
