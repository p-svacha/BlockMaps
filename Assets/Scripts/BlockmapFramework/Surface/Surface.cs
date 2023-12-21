using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public abstract class Surface
    {
        public abstract SurfaceId Id { get; }
        public abstract string Name { get; }
        public abstract float SpeedModifier { get; }
        public abstract Color Color { get; }
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
    }
}
