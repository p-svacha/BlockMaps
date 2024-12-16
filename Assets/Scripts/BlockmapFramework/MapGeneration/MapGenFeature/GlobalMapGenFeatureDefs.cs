using BlockmapFramework.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.Defs
{
    /// <summary>
    /// The collection of all MapGenFeatureDefs that are useful globally for all projects.
    /// </summary>
    public static class GlobalMapGenFeatureDefs
    {
        public static List<MapGenFeatureDef> Defs = new List<MapGenFeatureDef>()
        {
            new MapGenFeatureDef()
            {
                DefName = "Shack",
                Label = "shack",
                Description = "A one-story shack.",
                GenerateAction = ShackGenerator.GenerateShack
            },
        };
    }
}