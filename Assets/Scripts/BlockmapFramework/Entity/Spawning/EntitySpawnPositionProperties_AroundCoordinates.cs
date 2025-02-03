using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class EntitySpawnPositionProperties_AroundCoordinates : EntitySpawnPositionProperties
    {
        private Vector2Int Center;
        private float StandardDeviation;

        public EntitySpawnPositionProperties_AroundCoordinates(Vector2Int center, float standardDeviation)
        {
            Center = center;
            StandardDeviation = standardDeviation;
        }

        public override BlockmapNode GetNewTargetNode(EntitySpawnProperties spawnProps)
        {
            Vector2Int targetCoordinates = HelperFunctions.GetRandomNearPosition(Center, StandardDeviation);

            List<BlockmapNode> nodes = spawnProps.World.GetNodes(targetCoordinates);
            if (nodes == null || nodes.Count == 0) return null;
            return nodes.RandomElement();
        }
    }
}
