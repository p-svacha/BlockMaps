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
        /// The entity has no vision collider. Its visibility is fully based on its OriginNode.
        /// </summary>
        NodeBased,

        /// <summary>
        /// One BoxCollider per node, where the height of each considers the OverrideHeights of the EntityDef.
        /// </summary>
        EntityShape,

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
