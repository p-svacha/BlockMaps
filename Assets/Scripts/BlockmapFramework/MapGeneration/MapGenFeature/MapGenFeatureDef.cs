using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// The definition of a procedurally generated feature that can be placed on the map, that consists of various world objects.
    /// </summary>
    public class MapGenFeatureDef : Def
    {
        /// <summary>
        /// Function used to generate the map feature.
        /// </summary>
        public System.Action<Parcel, BlockmapNode> GenerateAction { get; init; } = (parcel, node) => throw new System.Exception("GenerateAction not defined");
    }
}