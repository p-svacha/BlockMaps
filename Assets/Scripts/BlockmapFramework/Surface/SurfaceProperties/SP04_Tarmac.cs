using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class SP04_Tarmac : SurfaceProperties
    {
        public override SurfacePropertyId Id => SurfacePropertyId.Tarmac;
        public override string Name => "Tarmac";
        public override float SpeedModifier => 0.9f;
    }
}
