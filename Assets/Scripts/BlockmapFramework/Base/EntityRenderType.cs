using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public enum EntityRenderType
    {
        /// <summary>
        /// Thing is not rendered in the world.
        /// </summary>
        NoRender,

        /// <summary>
        /// Thing is rendered as a standalone object and mesh.
        /// </summary>
        Standalone,

        /// <summary>
        /// Thing is rendered within one object and mesh that contains all of the things of this type for one chunk.
        /// </summary>
        Batch
    }
}


