using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Class to store information about where and in what state an entity has last been seen by an actor.
    /// </summary>
    public class LastSeenInfo
    {
        /// <summary>
        /// Exact world position at which the entity has last been seen.
        /// </summary>
        /// 
        public Vector3 Position { get; private set; }
        /// <summary>
        /// Exact world rotation at which the entity has last been seen.
        /// </summary>
        public Quaternion Rotation { get; private set; }

        /// <summary>
        /// Origin node at which the entity has last been seen.
        /// </summary>
        public BlockmapNode OriginNode { get; private set; }

        /// <summary>
        /// All nodes that where occupied when the entity has last been seen.
        /// </summary>
        public HashSet<BlockmapNode> OccupiedNodes { get; private set; }

        public LastSeenInfo(Vector3 position, Quaternion rotation, BlockmapNode node, HashSet<BlockmapNode> occupiedNodes)
        {
            Position = position;
            Rotation = rotation;
            OriginNode = node;
            OccupiedNodes = new HashSet<BlockmapNode>(occupiedNodes);
        }
    }
}
