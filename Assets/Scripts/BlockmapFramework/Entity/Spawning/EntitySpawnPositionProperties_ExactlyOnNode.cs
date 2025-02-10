using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Tries spawning the entity exactly on the provided node.
    /// </summary>
    public class EntitySpawnPositionProperties_ExactlyOnNode : EntitySpawnPositionProperties
    {
        BlockmapNode Node;

        public EntitySpawnPositionProperties_ExactlyOnNode(BlockmapNode node)
        {
            Node = node;
        }

        public override BlockmapNode GetNewTargetNode(EntitySpawnProperties spawnProps)
        {
            return Node;
        }
    }
}
