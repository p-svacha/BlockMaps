using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A measurable attributes that defines a characters performance or efficiency in various activities, tasks, or states.
    /// <br/>Stats numerical values calculated purely at runtime based on the state of various other systems (i.e. skills).
    /// </summary>
    public class Stat
    {
        public StatDef Def { get; private set; }

        /// <summary>
        /// The entity this stat is attached to.
        /// </summary>
        public Entity Entity { get; private set; }

        public Stat(StatDef def, Entity entity)
        {
            Def = def;
            Entity = entity;
        }

        /// <summary>
        /// The current value of this stat.
        /// </summary>
        public float GetValue()
        {
            float value = Def.BaseValue;

            // Return 0 if any skill requirement is not met
            foreach(string skillRequirement in Def.SkillRequirements)
            {
                if (Entity.GetSkillLevel(DefDatabase<SkillDef>.GetNamed(skillRequirement)) == 0) return 0;
            }

            // Apply additive offsets
            foreach(SkillImpact skillOffset in Def.SkillOffsets)
            {
                value += skillOffset.GetValueFor(Entity);
            }

            // Apply multiplicative factors
            foreach(SkillImpact skillFactor in Def.SkillFactors)
            {
                value *= skillFactor.GetValueFor(Entity);
            }

            if (Def.Type == StatType.Int) value = (int)(value);
            if (Def.Type == StatType.Binary)
            {
                if (value != 0 && value != 1) throw new System.Exception($"Invalid value for stat with Def {Def.DefName}. Stat type is binary (0/1) but value is {value}.");
            }

            return value;
        }
    }
}
