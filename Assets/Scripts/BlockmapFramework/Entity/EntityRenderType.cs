using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public enum EntityRenderType
    {
        /// <summary>
        /// Entity is not rendered in the world.
        /// </summary>
        NoRender,

        /// <summary>
        /// Entity is rendered as a standalone object and mesh that is loaded from a premade model.
        /// </summary>
        StandaloneModel,

        /// <summary>
        /// Entity is rendered as a standalone object and mesh that is generated procedurally.
        /// </summary>
        StandaloneGenerated,

        /// <summary>
        /// Entity is rendered within one object and mesh that contains all of the things of this type for one chunk.
        /// </summary>
        Batch
    }
}


