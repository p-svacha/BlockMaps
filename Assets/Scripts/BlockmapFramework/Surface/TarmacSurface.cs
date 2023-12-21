using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class TarmacSurface : Surface
    {
        public override SurfaceId Id => SurfaceId.Tarmac;
        public override string Name => "Tarmac";
        public override float SpeedModifier => 0.9f;
        public override Color Color => new Color(0.3f, 0.3f, 0f);
    }
}
