using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class FlatWorldGenerator : WorldGenerator
    {
        public override string Label => "Flat";

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>();
        }
    }
}
