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
        public int MaxLevel { get; init; } = 20;
    }
}
