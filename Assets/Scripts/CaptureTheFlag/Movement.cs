using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// An action for default moving from one node to another.
    /// </summary>
    public class Movement : CharacterAction
    {
        /// <summary>
        /// Exact order of nodes that are traversed for this movement, including the origin and target node.
        /// </summary>
        public List<BlockmapNode> Path { get; private set; }
        public BlockmapNode Target { get; private set; }

        public Movement(List<BlockmapNode> path, float cost)
        {
            Path = path;
            Target = path.Last();
            Cost = cost;
        }

        public override void Perform(Character c)
        {
            c.Entity.Move(Path);
        }
    }
}
