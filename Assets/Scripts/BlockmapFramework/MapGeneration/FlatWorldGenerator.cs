using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class FlatWorldGenerator : WorldGenerator
    {
        public override string Label => "Flat";

        protected override void OnGenerationStart() { }
        protected override void OnUpdate()
        {
            FinalizeGeneration();
        }
    }
}
