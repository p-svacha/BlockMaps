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
        public abstract bool DoBlend { get; } // flag if this surface should blend into adjacent nodes of other surfaces
        public abstract Color Color { get; }
        public abstract Texture2D Texture { get; }

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
