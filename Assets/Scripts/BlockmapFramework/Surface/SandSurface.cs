using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SandSurface : Surface
    {
        public SandSurface() : base() { }

        public override SurfaceId Id => SurfaceId.Sand;
        public override string Name => "Sand";
        public override float SpeedModifier => 0.2f;
        public override Color Color => new Color(0.7f, 0.5f, 0f);
    }
}
