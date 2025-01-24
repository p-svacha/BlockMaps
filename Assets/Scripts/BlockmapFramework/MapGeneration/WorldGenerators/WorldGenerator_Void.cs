using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class WorldGenerator_Void : WorldGenerator
    {
        public override string Label => "Void";
        public override string Description => "Nothing.";
        public override bool StartAsVoid => true;

        protected override List<System.Action> GetGenerationSteps()
        {
            return new List<System.Action>();
        }
    }
}
