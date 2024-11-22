using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Everything that is stored in a world database is a WorldDatabaseObject.
    /// </summary>
    public abstract class WorldDatabaseObject
    {
        /// <summary>
        /// Unique identifier of this object used for easy access in the world databases. (like Actors, Nodes, Entities, etc.)
        /// </summary>
        public abstract int Id { get; }

        /// <summary>
        /// Gets called when loading a world. After all WorldDatabaseObject have been loaded and before initialization of this object.
        /// </summary>
        public abstract void PostLoad();
    }
}
