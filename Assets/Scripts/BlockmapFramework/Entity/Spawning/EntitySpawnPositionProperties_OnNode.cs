using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class EntitySpawnPositionProperties_OnNode : EntitySpawnPositionProperties
    {
        BlockmapNode Node;

        public EntitySpawnPositionProperties_OnNode(BlockmapNode node)
        {
            Node = node;
        }

        public override BlockmapNode GetNewTargetNode(EntitySpawnProperties spawnProps)
        {
            return Node;
        }
    }
}
