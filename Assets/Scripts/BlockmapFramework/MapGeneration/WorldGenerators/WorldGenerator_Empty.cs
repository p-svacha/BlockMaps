using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class WorldGenerator_Empty : WorldGenerator
    {
        public override string Label => "Empty";
        public override string Description => "Completely empty, flag, grass map.";

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>();
        }
    }
}
