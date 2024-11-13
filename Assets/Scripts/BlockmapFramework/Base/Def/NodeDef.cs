using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The blueprint of a node type that defines some main properties of that node.
    /// </summary>
    public class NodeDef : Def
    {
        /// <summary>
        /// Flag if the heights of this node can change. Undeformable nodes can only be flat.
        /// </summary>
        public bool Deformable { get; init; } = true;

        /// <summary>
        /// Flag if entities can be placed on top of nodes of this type.
        /// </summary>
        public bool SupportsEntities { get; init; } = true;
    }
}
