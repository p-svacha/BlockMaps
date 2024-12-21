using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A component that can be attached to entities that adds custom behaviour to it.
    /// <br/>Acts as a modular plugin to extend entity functionality without modifying the base class.
    /// </summary>
    public abstract class EntityComp
    {
        /// <summary>
        /// The parent this component is attached to.
        /// </summary>
        public Entity Entity;
        public World World => Entity.World;

        /// <summary>
        /// The properties defining the behaviour rules of this comp.
        /// <br/>Should not be used directly but rather overwritten with 'Props' of the subclass.
        /// </summary>
        protected CompProperties props;

        public virtual void Initialize(CompProperties props)
        {
            this.props = props;
        }

        /// <summary>
        /// Gets called every tick.
        /// </summary>
        public virtual void Tick() { }

        /// <summary>
        /// Checks if this comp is valid on the parent entity and returns an exception if not.
        /// </summary>
        public virtual void Validate() { }

        #region Save / Load

        public virtual void ExposeDataForSaveAndLoad() { }

        #endregion
    }
}
