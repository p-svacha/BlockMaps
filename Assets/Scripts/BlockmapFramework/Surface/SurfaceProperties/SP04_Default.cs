using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SP04_Default : SurfaceProperties
    {
        public override SurfacePropertyId Id => SurfacePropertyId.Default;
        public override string Name => "Default";
        public override float SpeedModifier => 0.9f;
    }
}
