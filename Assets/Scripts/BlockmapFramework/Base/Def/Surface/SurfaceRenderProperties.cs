using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public enum SurfaceRenderType
    {
        /// <summary>
        /// Nodes with this RenderType will be invisible.
        /// </summary>
        NoRender,

        /// <summary>
        /// Nodes with this RenderType will be rendered flat (according to node shape) and can blend with other nodes of this type.
        /// </summary>
        Default_Blend,

        /// <summary>
        /// Nodes with this RenderType will be rendered flat (according to node shape) and won't blend with other nodes.
        /// <br/>They also support stairs.
        /// </summary>
        Default_NoBlend,

        /// <summary>
        /// Nodes with this RenderType will have a custom render logic defined in CustomRenderFunction.
        /// </summary>
        CustomMeshGeneration
    }

    /// <summary>
    /// Property within a SurfaceDef that contains all rules of how nodes with that surface should be rendered in the world.
    /// </summary>
    public class SurfaceRenderProperties
    {
        /// <summary>
        /// The fundamental way of how the mesh for a node of that surface is generated.
        /// </summary>
        public SurfaceRenderType Type { get; init; } = SurfaceRenderType.NoRender;

        /// <summary>
        /// If Type is set to CustomMeshGeneration, this function will render the node.
        /// </summary>
        public Action<BlockmapNode, MeshBuilder> CustomRenderFunction { get; init; } = null;

        /// <summary>
        /// Name of the material that is used for default rendered nodes.
        /// <br/>If Type is set to FlatSurface_Blend, the color, main texture and texture scale of the blendable surface reference material with this name will be used for rendering and blending a node with this surface.
        /// <br/>If Type is set to FlatSurface_NoBlend, the material with this name will be used.
        /// </summary>
        public string MaterialName { get; init; } = null;

        /// <summary>
        /// If Type is set to FlatSurface_NoBlend, nodes will have this height.
        /// </summary>
        public float Height { get; init; } = 0f;

        /// <summary>
        /// If true, triangles on edge shaped nodes (0001 or 1110) are always built in a way that the edge is long
        /// <br/>Useful for roofs for example.
        /// </summary>
        public bool UseLongEdges { get; init; } = false;
    }
}
