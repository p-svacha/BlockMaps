using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class EntitySpawnPositionProperties
    {
        /// <summary>
        /// Returns a new target node within these spawn position constraints.
        /// </summary>
        public abstract BlockmapNode GetNewTargetNode(EntitySpawnProperties spawnProps);
    }
}
