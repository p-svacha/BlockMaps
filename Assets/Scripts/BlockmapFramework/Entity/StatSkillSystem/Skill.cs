using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A skill represents an entity's proficiency in a specific activity, and determines their effectiveness and success rate in related tasks.
    /// </summary>
    public class Skill
    {
        public SkillDef Def { get; private set; }

        /// <summary>
        /// The entity this skill is attached to.
        /// </summary>
        public Entity Entity { get; private set; }

        /// <summary>
        /// The base value the entity has for this skill.
        /// </summary>
        public int BaseLevel { get; private set; }

        public Skill(SkillDef def, Entity entity, int baseLevel)
        {
            Def = def;
            Entity = entity;
            BaseLevel = baseLevel;
        }

        /// <summary>
        /// The actual value the entity has for this skill.
        /// </summary>
        public int GetSkillLevel()
        {
            return BaseLevel;
        }
    }
}
