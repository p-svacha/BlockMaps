using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class GrassSurface : Surface
    {
        public override SurfaceId Id => SurfaceId.Grass;
        public override string Name => "Grass";
        public override float SpeedModifier => 0.5f;
        public override Color Color => new Color(0.3f, 0.7f, 0.1f);
    }
}
