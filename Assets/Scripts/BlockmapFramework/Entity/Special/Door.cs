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
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Flag if the door opens to the left instead of to the right.
        /// </summary>
        public bool IsMirrored { get; private set; }

        /// <summary>
        /// Returns the direction on its OriginNode that this bloor is currently blocking, based on if it is open or closed.
        /// </summary>
        public Direction CurrentBlockingDirection => IsOpen ? (IsMirrored ? HelperFunctions.GetPreviousSideDirection(Rotation) : HelperFunctions.GetNextSideDirection(Rotation)) : Rotation;

        private Quaternion ClosedRotation => HelperFunctions.Get2dRotationByDirection(Rotation);
        private Quaternion OpenRotation => IsMirrored ? HelperFunctions.Get2dRotationByDirection(HelperFunctions.GetNextSideDirection(Rotation)) : HelperFunctions.Get2dRotationByDirection(HelperFunctions.GetPreviousSideDirection(Rotation));

        #region Init

        public void InitDoor(BlockmapNode node, Direction side, int height, bool isMirrored)
        {
            // Door specific
            DoorHeight = height;
            DoorMinAltitude = node.GetMinAltitude(side);
            DoorMaxAltitude = DoorMinAltitude + height - 1;
            IsMirrored = isMirrored;

            // Entity general
            Name = "Door";
            TypeId = DOOR_ENTITY_NAME + "_" + height + "_" + IsMirrored;
            Dimensions = new Vector3Int(1, height, 1);
            BlocksVision = true;
            IsPassable = true;
        }

        public override void RegisterInWorld()
        {
            base.RegisterInWorld();
            OriginNode.Doors.Add(Rotation, this);
        }

        protected override void CreateVisionCollider()
        {
            GameObject visionColliderObject = new GameObject("visionCollider");
            visionColliderObject.transform.SetParent(Wrapper.transform);
            visionColliderObject.transform.localScale = transform.localScale;
            visionColliderObject.layer = World.Layer_EntityVisionCollider;

            MeshCollider collider = visionColliderObject.AddComponent<MeshCollider>();
            collider.sharedMesh = MeshCollider.sharedMesh;
            VisionCollider = collider;
        }

        public static Door GetInstance(World world, EntityData data)
        {
            string[] attributes = data.TypeId.Split('_');
            BlockmapNode node = world.GetNode(data.OriginNodeId);
            int height = int.Parse(attributes[1]);
            bool isMirrored = attributes.Length > 2 ? bool.Parse(attributes[2]) : false;

            return GetInstance(node, data.Rotation, height, isMirrored);
        }
        public static Door GetInstance(BlockmapNode node, Direction side, int height, bool isMirrored)
        {
            Door instance = GenerateDoorObject(node, side, height, isMirrored);
            instance.InitDoor(node, side, height, isMirrored);
            return instance;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Opens or closes the door based on its current state.
        /// </summary>
        public void Toggle()
        {
            if (!IsOpen)
            {
                StartCoroutine(RotateDoor(ClosedRotation, OpenRotation));
            }
            else
            {
                StartCoroutine(RotateDoor(OpenRotation, ClosedRotation));
            }

            IsOpen = !IsOpen;

            World.UpdateNavmeshAround(OriginNode.WorldCoordinates);
        }

        private IEnumerator RotateDoor(Quaternion startingRotation, Quaternion targetRotation)
        {
            float elapsedTime = 0f;
            float rotationDuration = 1f;

            while (elapsedTime < rotationDuration)
            {
                WorldRotation = Quaternion.Slerp(startingRotation, targetRotation, elapsedTime / rotationDuration);
                elapsedTime += Time.deltaTime;
                UpdateVisiblity(World.ActiveVisionActor);
                yield return null;
            }

            WorldRotation = targetRotation;
            UpdateVisiblity(World.ActiveVisionActor);

            UpdateVisionColliderPosition();
            World.UpdateVisionOfNearbyEntitiesDelayed(OriginNode.GetCenterWorldPosition());
        }

        #endregion

        #region Getters

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

        public override Vector3 GetWorldPosition(World world, BlockmapNode originNode, Direction rotation)
        {
            Vector3 basePosition = base.GetWorldPosition(world, originNode, rotation);
            float worldY = World.TILE_HEIGHT * originNode.GetMinAltitude(Rotation);

            Vector3 offsetPosition = basePosition + Door.GetWorldPositionOffset(rotation, IsMirrored);
            return new Vector3(offsetPosition.x, worldY, offsetPosition.z);
        }

        #endregion

        #region Mesh Generation

        private const float DOOR_WIDTH = 0.05f;

        private const float HANDLE_Y_ABSOLUTE = 1f;
        private const float MIN_HANDLE_MARGIN_TOP = 0.2f;
        private const float HANDLE_MARGIN_X = 0.1f;
        private const float HANDLE_SIZE = 0.1f;

        private static Door GenerateDoorObject(BlockmapNode node, Direction side, int height, bool isMirrored)
        {
            GameObject doorObject = new GameObject(DOOR_ENTITY_NAME);

            MeshBuilder meshBuilder = new MeshBuilder(doorObject);
            GenerateDoorMesh(meshBuilder, height, isMirrored, isPreview: false);

            Door door = doorObject.AddComponent<Door>();
            return door;
        } 

        public static void GenerateDoorMesh(MeshBuilder meshBuilder, int height, bool isMirrored, bool isPreview)
        {
            int doorSubmesh = meshBuilder.GetSubmesh(isPreview ? ResourceManager.Singleton.BuildPreviewMaterial : ResourceManager.Singleton.Mat_WoodParquet);
            int handleSubmesh = meshBuilder.GetSubmesh(ResourceManager.Singleton.LadderMaterial);

            // Anchor point (needed for correct door rotation)
            float anchorPointOffset = DOOR_WIDTH / 2f;
            Vector2 anchorPoint;
            if (isMirrored) anchorPoint = new Vector2(-(1f - anchorPointOffset), -anchorPointOffset);
            else anchorPoint = new Vector2(-anchorPointOffset, -anchorPointOffset);

            // Door
            float doorHeight = height * World.TILE_HEIGHT;
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
