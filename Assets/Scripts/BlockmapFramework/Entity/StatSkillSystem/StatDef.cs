using BlockmapFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Stats are measurable attributes that defines a characters performance or efficiency in various activities, tasks, or states.
    /// <br/>They are represented as a numerical value and can be influenced by various other systems.
    /// </summary>
    public class StatDef : Def
    {
        /// <summary>
        /// What kind of values this stat can have and how it is displayed.
        /// </summary>
        public StatType Type { get; init; } = StatType.Float;

        public float BaseValue { get; init; } = 0f;

        /// <summary>
        /// The maxiumum value the stat can have. If -1, there is no upper bound.
        /// </summary>
        public float MaxValue { get; init; } = -1;

        /// <summary>
        /// List containing all sub-parts / modifiers that affect the stat calculation.
        /// <br/>The stat parts will be applied in the order they are defined in this list.
        /// </summary>
        public List<StatPart> StatParts { get; init; } = new();
    }

    public enum StatType
    {
        /// <summary>
        /// The stat value is represented as a float greater than 0 up to a defined max value.
        /// </summary>
        Float,

        /// <summary>
        /// The stat value is either true (1) or false (0)
        /// </summary>
        Binary,

        /// <summary>
        /// The stat value is represented as an int, ranging from 0 to a defined max value.
        /// <br/>Values are rounded DOWN to the next lower full integer when returning the stat value.
        /// </summary>
        Int,

        /// <summary>
        /// The stat represents a percentage or a modifier of another value.
        /// <br/>Acts the same as Float but gets displayed differently.
        /// </summary>
        Percent
    }
}
