using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Each different walkable texure/material is represented by one instance of a Surface.
    /// </summary>
    public abstract class Surface
    {
        public abstract SurfaceId Id { get; }
        public abstract string Name { get; }
        public abstract SurfaceProperties Properties { get; }
        public abstract Color Color { get; }
        public abstract Texture2D Texture { get; }
    }
}
