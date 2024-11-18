using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A script that needs to be attached to GameObjects with entity mesh/vision colliders to easily get the entity when hitting them.
    /// </summary>
    public class EntityCollider : MonoBehaviour
    {
        public Entity Entity;
    }
}
