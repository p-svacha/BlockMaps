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
        /// Stores the exact world position at which the entity has last been seen.
        /// </summary>
        /// 
        public Vector3 Position { get; private set; }
        /// <summary>
        /// Stores the exact world rotation at which the entity has last been seen.
        /// </summary>
        public Quaternion Rotation { get; private set; }

        /// <summary>
        /// Stores the origin node at which the entity has last been seen.
        /// </summary>
        public BlockmapNode Node { get; private set; }

        public LastSeenInfo(Vector3 position, Quaternion rotation, BlockmapNode node)
        {
            Position = position;
            Rotation = rotation;
            Node = node;
        }
    }
}
