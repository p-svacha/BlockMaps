using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    public class WorldGenerator_Desert : WorldGenerator
    {
        public override string Label => "Desert";
        public override string Description => "Very sparse, flat and empty map with lots of sand."; 

        protected override List<Action> GetGenerationSteps()
        {
            throw new NotImplementedException();
        }
    }
}
