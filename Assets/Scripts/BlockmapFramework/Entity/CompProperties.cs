using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// EntityCompProperties hold the properties and configuration data for a specific EntityComp.
    /// </summary>
    public abstract class CompProperties
    {
        public Type CompClass { get; init; } = null;

        /// <summary>
        /// Creates a deep copy of an existing CompProperties
        /// </summary>
        public abstract CompProperties Clone();

        public virtual bool Validate(EntityDef parent)
        {
            return true;
        }

        /// <summary>
        /// Gets called after all Defs are loaded into the database. Used to resolve references to other Defs.
        /// </summary>
        public virtual void ResolveReferences() { }
    }
}
