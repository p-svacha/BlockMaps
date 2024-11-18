using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The blueprint of a fence.
    /// </summary>
    public class FenceDef : Def
    {
        /// <summary>
        /// The maximum height a fence with this def can have.
        /// </summary>
        public int MaxHeight { get; init; } = World.MAX_ALTITUDE;

        /// <summary>
        /// If true, this fence supports being built on node corners (in addition to sides which is always possible).
        /// </summary>
        public bool CanBuildOnCorners { get; init; } = true;

        /// <summary>
        /// If true, the fence will block incoming vision rays and stuff behind will not be visible.
        /// <br/>If false, the fence will be see-through.
        /// </summary>
        public bool BlocksVision { get; init; } = false;

        /// <summary>
        /// The minimun climbing skill a character needs to be able to climb a wall with this material.
        /// </summary>
        public ClimbingCategory ClimbSkillRequirement { get; init; } = ClimbingCategory.Advanced;

        /// <summary>
        /// The cost of climbing up one wall piece with this material. Must be at least 1.
        /// </summary>
        public float ClimbCostUp { get; init; } = 2.5f;

        /// <summary>
        /// The cost of climbing down one wall piece with this material. Must be at least 1.
        /// </summary>
        public float ClimbCostDown { get; init; } = 1.5f;

        /// <summary>
        /// How much space of the node the wall takes up on the side (both in meters and %).
        /// </summary>
        public float Width { get; init; } = 0.1f;

        /// <summary>
        /// The function that adds the mesh of this fence to a MeshBuilder.
        /// </summary>
        public System.Action<MeshBuilder, BlockmapNode, Direction, int, bool> GenerateMeshFunction = null;
    }
}
