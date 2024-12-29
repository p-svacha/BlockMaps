using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// A stat that characters have a value for.
    /// </summary>
    public class StatDef : Def
    {
        /// <summary>
        /// What kind of values this stat can have.
        /// </summary>
        public StatType Type { get; init; } = StatType.Scalar;

        /// <summary>
        /// The maxiumum value the stat can have. For star stats this gets rounded to an int.
        /// <br/>If -1, there is no upper bound.
        /// </summary>
        public float MaxValue { get; init; } = -1;
    }

    public enum StatType
    {
        /// <summary>
        /// The stat value is represented as a float greater than 0 up to a defined max value.
        /// </summary>
        Scalar,

        /// <summary>
        /// The stat value is either true (1) or false (0)
        /// </summary>
        Binary,

        /// <summary>
        /// The stat value is represented as an int, ranging from 0 to a defined max value.
        /// </summary>
        Int
    }
}
