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
        FlatBlendableSurface,

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
        /// If Type is set to FlatBlendableSurface, the color, main texture and texture scale of this material will be used for rendering and blending a node with this surface.
        /// </summary>
        public Material SurfaceReferenceMaterial { get; init; } = null;

        /// <summary>
        /// If true, triangles on edge shaped nodes (0001 or 1110) are always built in a way that the edge is long
        /// <br/>Useful for roofs for example.
        /// </summary>
        public bool UseLongEdges { get; init; } = false;
    }
}
