using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Changes the value of the stat according to the value of a skill.
    /// </summary>
    public class StatPart_SkillImpact : StatPart
    {
        public SkillDef SkillDef { get; init; } = null;
        public SkillImpactType Type { get; init; } = SkillImpactType.Additive;
        public SkillImpactCurve Curve { get; init; } = SkillImpactCurve.Linear;
        public float LinearPerLevelValue { get; init; } = 0f;

        /// <summary>
        /// Dictionary containing values for certain skill levels.
        /// <br/>Not every single level needs to be defined if some levels share the same value. It will just search for the value with equal or next lower key.
        /// </summary>
        public Dictionary<int, float> PerLevelValues { get; init; } = new();

        public override void TransformValue(Entity entity, Stat stat, ref float value)
        {
            float transformationValue = GetTransformationValue(entity);

            if (Type == SkillImpactType.Additive) value += transformationValue;
            if (Type == SkillImpactType.Multiplicative) value *= transformationValue;
        }

        public override string ExplanationString(Entity entity, Stat stat)
        {
            float transformationValue = GetTransformationValue(entity);
            string sign = "";
            if (Type == SkillImpactType.Additive) sign = transformationValue > 0 ? "+" : "";
            if (Type == SkillImpactType.Multiplicative) sign = "x";
            return $"{SkillDef.LabelCap} Skill: {sign}{stat.GetValueText(transformationValue)}";
        }

        private float GetTransformationValue(Entity entity)
        {
            int skillLevel = entity.GetSkillLevel(SkillDef);

            if (Curve == SkillImpactCurve.Linear)
            {
                return skillLevel * LinearPerLevelValue;
            }
            if (Curve == SkillImpactCurve.ValuePerLevel)
            {
                // Get all keys less than or equal to the skill level
                var eligibleKeys = PerLevelValues.Keys.Where(k => k <= skillLevel);

                // Find the maximum key from eligible keys
                int closestKey = eligibleKeys.Any() ? eligibleKeys.Max() : throw new KeyNotFoundException("No suitable key found for the given skill level.");

                return PerLevelValues[closestKey];
            }
            throw new System.Exception($"SkillImpactType {Curve} not handled.");
        }
    }

    public enum SkillImpactType
    {
        Additive,
        Multiplicative,
    }

    public enum SkillImpactCurve
    {
        ValuePerLevel,
        Linear
    }
}