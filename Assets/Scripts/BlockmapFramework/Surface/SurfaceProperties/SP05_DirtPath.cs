using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SP05_DirtPath : SurfaceProperties
    {
        public override SurfacePropertyId Id => SurfacePropertyId.DirtPath;
        public override string Name => "Dirt Path";
        public override float SpeedModifier => 0.8f;
    }
}

