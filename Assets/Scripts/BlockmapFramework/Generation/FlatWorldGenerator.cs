using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class FlatWorldGenerator : WorldGenerator
    {
        public override string Name => "Flat";

        protected override void OnGenerationStart() { }
        public override void UpdateGeneration()
        {
            WorldData data = CreateEmptyWorldData();
            FinishGeneration(data);
        }
    }
}
