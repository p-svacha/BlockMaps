using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Interface that should be implemented by world objects that are part of the vision system, meaning that can be explored and/or seen by entities/actors.
    /// </summary>
    public interface IVisionTarget
    {      
        /// <summary>
        /// Returns if this object is currently visible for the specified actor.
        /// </summary>
        public bool IsVisibleBy(Actor actor);

        /// <summary>
        /// Returns if the object has been explored by the specified actor.
        /// </summary>
        public bool IsExploredBy(Actor actor);

        /// <summary>
        /// Adds an entity to the list of entities that currently see this object.
        /// </summary>
        public void AddVisionBy(Entity e);

        /// <summary>
        /// Removes an entity from the list of entities that currently see this object.
        /// </summary>
        public void RemoveVisionBy(Entity e);
    }
}
