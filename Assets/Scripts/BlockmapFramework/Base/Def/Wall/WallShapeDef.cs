using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The definition of a wall shape. Each wall consists of a combination of shape + material.
    /// </summary>
    public class WallShapeDef : Def
    {
        /// <summary>
        /// Flag if walls with this shape block vision.
        /// </summary>
        public bool BlocksVision { get; init; } = true;

        /// <summary>
        /// Flag if walls with this can be climbed.
        /// </summary>
        public bool IsClimbable { get; init; } = true;

        /// <summary>
        /// How much space of the node the wall takes up on the side (both in meters and %).
        /// </summary>
        public float Width { get; init; } = 0.1f;

        /// <summary>
        /// Flag if this shape is applicable in node corners instead of node sides.
        /// </summary>
        public bool IsCornerShape { get; init; } = false;

        /// <summary>
        /// The function that is used to draw a wall piece with this shape.
        /// </summary>
        public Action<World, MeshBuilder, Vector3Int, Vector3Int, Direction, Material, bool> RenderFunction { get; init; } = null;
    }
}
