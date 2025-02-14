using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Skills represents a characters proficiency in a specific activity, and determines their effectiveness and success rate in related tasks.
    /// </summary>
    public class SkillDef : Def
    {
        /// <summary>
        /// The maximum level the skill can have.
        /// </summary>
        public int MaxLevel { get; init; } = 20;
    }
}
