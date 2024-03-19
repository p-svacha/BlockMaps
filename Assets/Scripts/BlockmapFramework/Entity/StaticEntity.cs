using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A kind of entity that is instantiated from a prefab and always looks the same.
    /// </summary>
    public class StaticEntity : Entity
    {
        public List<Vector2Int> HeightOverridePositions; // Set in inspector, acts as key for HeightOverrideHeights
        public List<int> HeightOverrideHeights; // Set in inspector, acts as value for HeightOverridePositions
        private Dictionary<Vector2Int, BoxCollider> VisionCollidersPerCoordinate; // If some heights are overriden, this lists includes all vision colliders

        protected override void CreateVisionCollider()
        {
            if (HeightOverridePositions.Count != HeightOverrideHeights.Count) throw new System.Exception("The 2 lists HeightOverridePositions and HeightOverrideHeights act as a dictionary and must have the same length.");

            if (HeightOverridePositions.Count == 0) // Create one single box collider for whole entity
            {
                base.CreateVisionCollider();
                return;
            }

            else // Create one box collider per coordinate
            {
                VisionCollidersPerCoordinate = new Dictionary<Vector2Int, BoxCollider>();

                for(int x = 0; x < Dimensions.x; x++)
                {
                    for (int y = 0; y < Dimensions.z; y++)
                    {
                        Vector2Int localCoords = new Vector2Int(x, y);

                        GameObject visionColliderObject = new GameObject("visionCollider_" + x + "_" + y);
                        visionColliderObject.transform.SetParent(Wrapper.transform);
                        visionColliderObject.transform.localScale = transform.localScale;
                        visionColliderObject.layer = World.Layer_EntityVisionCollider;
                        BoxCollider collider = visionColliderObject.AddComponent<BoxCollider>();

                        float height = Dimensions.y;
                        if(HeightOverridePositions.Contains(localCoords))
                        {
                            int index = HeightOverridePositions.IndexOf(localCoords);
                            height = HeightOverrideHeights[index];
                        }

                        collider.size = new Vector3(1f / transform.localScale.x, (height * World.TILE_HEIGHT) / transform.localScale.y, 1f / transform.localScale.z);
                        collider.center = new Vector3((Dimensions.x / 2f) - x - 0.5f, collider.size.y / 2, (Dimensions.z / 2f) - y - 0.5f);

                        VisionCollidersPerCoordinate.Add(localCoords, collider);
                    }
                }
            }
        }

        protected override void UpdateVisionColliderPosition()
        {
            if (HeightOverridePositions.Count == 0) // Set position of single box collider for whole entity
            {
                base.UpdateVisionColliderPosition();
                return;
            }

            else // Set position of box colliders per coordinate
            {
                for (int x = 0; x < Dimensions.x; x++)
                {
                    for (int y = 0; y < Dimensions.z; y++)
                    {
                        Vector2Int localCoords = new Vector2Int(x, y);
                        //Vector2Int translatedLocalCoords = GetLocalPosition(localCoords);

                        BoxCollider visionCollider = VisionCollidersPerCoordinate[localCoords];

                        visionCollider.transform.position = GetWorldPosition(World, OriginNode, Rotation);
                        visionCollider.transform.rotation = HelperFunctions.Get2dRotationByDirection(Rotation);
                    }
                }
            }
        }
    }
}
