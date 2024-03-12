using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SP01_Grass : SurfaceProperties
    {
        public override SurfacePropertyId Id => SurfacePropertyId.Grass;
        public override string Name => "Grass";
        public override float SpeedModifier => 0.6f;
    }
}
