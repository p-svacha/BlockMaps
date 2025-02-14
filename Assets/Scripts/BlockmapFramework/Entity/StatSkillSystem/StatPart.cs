using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// StatParts are sub-components of a stat that modify its value based on conditions.
    /// </summary>
    public abstract class StatPart
    {
        public abstract void TransformValue(Entity entity, Stat stat, ref float value);
        public abstract string ExplanationString(Entity entity, Stat stat);
    }
}
