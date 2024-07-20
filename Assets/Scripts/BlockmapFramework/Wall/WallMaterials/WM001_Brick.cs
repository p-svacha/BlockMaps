using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WM001_Brick : WallMaterial
    {
        public override WallMaterialId Id => WallMaterialId.Brick;
        public override string Name => "Brick";
        public override Material Material => ResourceManager.Singleton.Mat_Brick;
    }
}
