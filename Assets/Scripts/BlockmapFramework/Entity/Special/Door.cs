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
        /// Returns the direction on its OriginNode that this bloor is currently blocking, based on if it is open or closed.
        /// </summary>
        public Direction CurrentBlockingDirection => IsOpen ? HelperFunctions.GetNextSideDirection(Rotation) : Rotation;

        private Quaternion ClosedRotation => HelperFunctions.Get2dRotationByDirection(Rotation);
        private Quaternion OpenRotation => HelperFunctions.Get2dRotationByDirection(HelperFunctions.GetPreviousSideDirection(Rotation));

        #region Init

        public void InitDoor(BlockmapNode node, Direction side, int height)
        {
            // Door specific
            DoorHeight = height;
            DoorMinAltitude = node.GetMinAltitude(side);
            DoorMaxAltitude = DoorMinAltitude + height - 1;

            // Entity general
            Name = "Door";
            TypeId = DOOR_ENTITY_NAME + "_" + height;
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

            return GetInstance(node, data.Rotation, height);
        }
        public static Door GetInstance(BlockmapNode node, Direction side, int height)
        {
            Door instance = GenerateDoorObject(node, side, height);
            instance.InitDoor(node, side, height);
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

            UpdateVisionColliderPosition();
            World.UpdateVisionOfNearbyEntitiesDelayed(OriginNode.GetCenterWorldPosition());
        }

        #endregion

        #region Getters

        public static Vector3 GetWorldPositionOffset(Direction rotation)
        {
            float offsetValue = 0.5f - (DOOR_WIDTH / 2f);

            return rotation switch
            {
                Direction.S => new Vector3(-offsetValue, 0f, -offsetValue),
                Direction.E => new Vector3(offsetValue, 0f, -offsetValue),
                Direction.N => new Vector3(offsetValue, 0f, offsetValue),
                Direction.W => new Vector3(-offsetValue, 0f, offsetValue),
                _ => throw new System.Exception("Direction not handled")
            };
        }

        public override Vector3 GetWorldPosition(World world, BlockmapNode originNode, Direction rotation)
        {
            Vector3 basePosition = base.GetWorldPosition(world, originNode, rotation);
            float worldY = World.TILE_HEIGHT * originNode.GetMinAltitude(Rotation);

            Vector3 offsetPosition = basePosition + Door.GetWorldPositionOffset(rotation);
            return new Vector3(offsetPosition.x, worldY, offsetPosition.z);
        }

        #endregion

        #region Mesh Generation

        private const float DOOR_WIDTH = 0.05f;

        private static Door GenerateDoorObject(BlockmapNode node, Direction side, int height)
        {
            GameObject doorObject = new GameObject(DOOR_ENTITY_NAME);

            MeshBuilder meshBuilder = new MeshBuilder(doorObject);
            GenerateDoorMesh(meshBuilder, height, isPreview: false);

            Door door = doorObject.AddComponent<Door>();
            return door;
        } 

        public static void GenerateDoorMesh(MeshBuilder meshBuilder, int height, bool isPreview)
        {
            int submesh = meshBuilder.GetSubmesh(GetMaterial(isPreview));

            // Door
            Vector3 pos = new Vector3(-(DOOR_WIDTH / 2f), 0f, -(DOOR_WIDTH / 2f));
            Vector3 dim = new Vector3(1f, height * World.TILE_HEIGHT, DOOR_WIDTH);
            meshBuilder.BuildCube(submesh, pos, dim);

            meshBuilder.ApplyMesh();
        }

        private static Material GetMaterial(bool isPreview)
        {
            if (isPreview) return ResourceManager.Singleton.BuildPreviewMaterial;
            else return ResourceManager.Singleton.Mat_Wood;
        }

        #endregion
    }
}
