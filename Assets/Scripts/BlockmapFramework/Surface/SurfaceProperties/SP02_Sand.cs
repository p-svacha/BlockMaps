using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SP02_Sand : SurfaceProperties
    {
        public override SurfacePropertyId Id => SurfacePropertyId.Sand;
        public override float SpeedModifier => 0.35f;
    }
}
