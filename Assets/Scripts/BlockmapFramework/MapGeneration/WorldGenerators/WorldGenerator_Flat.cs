using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class WorldGenerator_Flat : WorldGenerator
    {
        public override string Label => "Flat";
        public override string Description => "Completely empty, flag, grass map.";
        public override bool StartAsVoid => false;

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>();
        }
    }
}
