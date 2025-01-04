using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// A simple data structure that holds information of how a SkillDef impacts a StatDef.
    /// </summary>
    public class SkillImpact
    {
        public string SkillDefName { get; init; } = "";

        public SkillImpactType Type { get; init; } = SkillImpactType.Linear;
        public float LinearPerLevelValue { get; init; } = 0f;

        /// <summary>
        /// Dictionary containing values for certain skill levels.
        /// <br/>Not every single level needs to be defined if some levels share the same value. It will just search for the value with equal or next lower key.
        /// </summary>
        public Dictionary<int, float> PerLevelValues { get; init; } = new();

        public float GetValueFor(Entity e)
        {
            int skillLevel = e.GetSkillLevel(DefDatabase<SkillDef>.GetNamed(SkillDefName));

            if (Type == SkillImpactType.Linear)
            {
                return skillLevel * LinearPerLevelValue;
            }
            if (Type == SkillImpactType.ValuePerLevel)
            {
                // Get all keys less than or equal to the skill level
                var eligibleKeys = PerLevelValues.Keys.Where(k => k <= skillLevel);

                // Find the maximum key from eligible keys
                int closestKey = eligibleKeys.Any() ? eligibleKeys.Max() : throw new KeyNotFoundException("No suitable key found for the given skill level.");

                return PerLevelValues[closestKey];
            }
            throw new System.Exception($"SkillImpactType {Type} not handled.");
        }
    }

    public enum SkillImpactType
    {
        ValuePerLevel,
        Linear
    }
}