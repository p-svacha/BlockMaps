using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A collection of all entities that can be used inside a world, including procedural ones.
    /// <br/> Maps entity id's to entity objects.
    /// </summary>
    public abstract class WorldEntityLibrary
    {
        /// <summary>
        /// Returns an uninitialized entity instance according to the given id.
        /// </summary>
        public abstract Entity GetEntityInstance(World world, string id);
    }
}
