using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Changes the value of the stat to 0 if the level of the given skill is at 0.
    /// </summary>
    public class StatPart_SkillRequirement : StatPart
    {
        /// <summary>
        /// If the level the skill is 0, then the value of this stat is also 0.
        /// </summary>
        public SkillDef RequiredSkill { get; init; }

        public override void TransformValue(Entity entity, Stat stat, ref float value)
        {
            if (entity.GetSkillLevel(RequiredSkill) == 0) value = 0;
        }

        public override string ExplanationString(Entity entity, Stat stat)
        {
            return "Multiplier for not meeting skill requirements: x0%"; 
        }
    }
}
