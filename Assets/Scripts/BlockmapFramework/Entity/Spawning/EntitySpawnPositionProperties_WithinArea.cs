using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class EntitySpawnPositionProperties_WithinArea : EntitySpawnPositionProperties
    {
        private int MinX;
        private int MaxX;
        private int MinY;
        private int MaxY;

        public EntitySpawnPositionProperties_WithinArea(int minX, int maxX, int minY, int maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public override BlockmapNode GetNewTargetNode(EntitySpawnProperties spawnProps)
        {
            Vector2Int targetCoordinates = new Vector2Int(Random.Range(MinX, MaxX + 1), Random.Range(MinY, MaxY + 1));

            List<BlockmapNode> nodes = spawnProps.World.GetNodes(targetCoordinates);
            if (nodes == null || nodes.Count == 0) return null;
            return nodes.RandomElement();
        }
    }
}
