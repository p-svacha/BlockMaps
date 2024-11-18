using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public enum EntityVisionImpact
    {
        /// <summary>
        /// The entity doesn't block vision of other entities. They see right through it.
        /// </summary>
        SeeThrough,

        /// <summary>
        /// The entity blocks the vision of other entities on all cells it occupies with a single BoxCollider.
        /// </summary>
        FullBlock,

        /// <summary>
        /// The entity blocks the vision of other entities. It has a BoxCollider per node, where each have its own height.
        /// </summary>
        BlockPerNode,

        /// <summary>
        /// The entity blocks the vision of other entities, but only exactly in the location of its MeshCollider.
        /// <br/>Only works on standalone rendered entities and should be avoided for entities with complicated meshes.
        /// </summary>
        MeshCollider,
    }
}
