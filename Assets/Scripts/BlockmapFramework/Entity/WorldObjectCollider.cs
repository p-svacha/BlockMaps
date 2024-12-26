using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A script that needs to be attached to GameObjects with object mesh/vision colliders to easily get the WorldDatabaseObject when hitting them.
    /// </summary>
    public class WorldObjectCollider : MonoBehaviour
    {
        public WorldDatabaseObject Object;

        public int State; // Flag is used for entities with vision colliders for individual states (i.e. door)
    }
}
