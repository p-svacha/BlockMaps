using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The SurfaceProperties contain all rules of how a surface behaves gameplay-wise and interacts with other systems and mechanics.
    /// <br/> Multiple surfaces (different textures / models) can use the same SurfaceProperties.
    /// </summary>
    public abstract class SurfaceProperties
    {
        public abstract SurfacePropertyId Id { get; }
        public abstract string Name { get; }
        public abstract float SpeedModifier { get; }
    }
}
