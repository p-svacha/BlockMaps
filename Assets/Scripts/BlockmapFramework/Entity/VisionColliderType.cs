using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public enum VisionColliderType
    {
        /// <summary>
        /// A single box collider that spans the full dimensions of the entity.
        /// </summary>
        FullBox,

        /// <summary>
        /// One BoxCollider per node, where each have its own height.
        /// </summary>
        BlockPerNode,

        /// <summary>
        /// The VisionCollider uses the same mesh as the MeshObject of the entity.
        /// <br/>Only works on standalone rendered entities and should be avoided for entities with complicated meshes.
        /// </summary>
        MeshCollider,

        /// <summary>
        /// VisionCollider is created with custom logic in the entity subclass.
        /// </summary>
        CustomImplementation
    }
}
