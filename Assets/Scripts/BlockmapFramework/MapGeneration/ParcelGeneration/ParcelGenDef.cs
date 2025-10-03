using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.WorldGeneration
{
    /// <summary>
    /// The definition of a Parcel Generator.
    /// <br/>Contains all constraints and information of how this parcel generator can be used and also the actual generation logic.
    /// <br/>Unlike other Defs, ParcelGenDefs are not stored in a database but are created at runtime when starting a ParcelWorldGenerator in GetParcelDefs().
    /// </summary>
    public class ParcelGenDef : Def
    {
        /// <summary>
        /// Generator class for generating the world within an actual parcel.
        /// </summary>
        public System.Type GeneratorClass { get; init; } = null;

        /// <summary>
        /// How likely it is for this parcel type to appear.
        /// </summary>
        public float Commonness { get; init; } = 1f;

        /// <summary>
        /// If defined (!= -1), the smaller side of the parcel need to be at least this length.
        /// </summary>
        public int MinSizeShortSide { get; init; } = -1;

        /// <summary>
        /// If defined (!= -1), the longer side of the parcel need to be at least this length.
        /// </summary>
        public int MinSizeLongSide { get; init; } = -1;

        /// <summary>
        /// If defined (!= -1), the smaller side of the parcel cannot have a length exceeding this value.
        /// </summary>
        public int MaxSizeShortSide { get; init; } = -1;

        /// <summary>
        /// If defined (!= -1), the longer side of the parcel cannot have a length exceeding this value.
        /// </summary>
        public int MaxSizeLongSide { get; init; } = -1;

        public bool DoesFulfillConstraints(Vector2Int dimensions)
        {
            int shortSide = Mathf.Min(dimensions.x, dimensions.y);
            int longSide = Mathf.Max(dimensions.x, dimensions.y);

            if (MinSizeShortSide > 0 && shortSide < MinSizeShortSide) return false;
            if (MinSizeLongSide > 0 && longSide < MinSizeLongSide) return false;

            if (MaxSizeShortSide > 0 && shortSide > MaxSizeShortSide) return false;
            if (MaxSizeLongSide > 0 && longSide > MaxSizeLongSide) return false;

            return true;
        }
    }
}
