using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SP03_Water : SurfaceProperties
    {
        public override SurfacePropertyId Id => SurfacePropertyId.Water;
        public override float SpeedModifier => 0.2f;
    }
}
