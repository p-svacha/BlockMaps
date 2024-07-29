using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Each different walkable texure/material is represented by one instance of a Surface that contains all information and logic of how it is drawn in the world.
    /// </summary>
    public abstract class Surface
    {
        public abstract SurfaceId Id { get; }
        public abstract string Name { get; }
        public abstract SurfacePropertyId PropertiesId { get; }

        /// <summary>
        /// Flag if this surface should blend into adjacent nodes of other surfaces
        /// </summary>
        public abstract bool DoBlend { get; }
        /// <summary>
        /// If true, triangles on edge shaped nodes (0001 or 1110) are always built in a way that the edge is long
        /// <br/>Useful for roofs for example.
        /// </summary>
        public abstract bool UseLongEdges { get; }
        public abstract Color PreviewColor { get; }

        /// <summary>
        /// Used in surface material for blending
        /// </summary>
        public virtual Texture2D BlendingTexture => ResourceManager.Singleton.GrassTexture;

        public SurfaceProperties Properties { get; private set; }

        public Surface(SurfaceManager surfaceManager)
        {
            Properties = surfaceManager.GetSurfaceProperties(PropertiesId);
        }

        /// <summary>
        /// Draw the top side of a node with this surface.
        /// </summary>
        public abstract void DrawNode(World world, BlockmapNode node, MeshBuilder meshBuilder);
    }
}
