using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// A movement stores the path and cost for moving from one node to another with default movement for a specific character.
    /// </summary>
    public class Movement
    {
        /// <summary>
        /// Exact order of nodes that are traversed for this movement, excluding the origin node and including the target node.
        /// </summary>
        public List<BlockmapNode> Path { get; private set; }

        /// <summary>
        /// Amount of action points and stamina that gets reduced when taking this movement.
        /// </summary>
        public float Cost { get; private set; }

        public Movement(List<BlockmapNode> path, float cost)
        {
            Path = path;
            Cost = cost;
        }
    }
}
