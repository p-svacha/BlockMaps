using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    /// <summary>
    /// Each creature has a Stat for all existing StatDefs in here.
    /// <br/>Stats are the attributes that are relevant for the simulation and combat, and can be affected by the species, traits, status effects, items, etc.
    /// </summary>
    public static class StatDefs
    {
        public static List<StatDef> GetDefs()
        {
            return new List<StatDef>()
            {
                new StatDef()
                {
                    DefName = "MaxHP",
                    Label = "max HP",
                    Description = "The maximum HP of this creature.",
                    Type = StatType.Int,
                    BaseValue = 3,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Health,
                            LinearPerLevelValue = 0.1f,
                        },
                        new StatPart_CreatureLevel()
                    }
                },

                new StatDef()
                {
                    DefName = "VisionRange",
                    Label = "vision range",
                    Description = "How many tiles around itself the creature sees.",
                    Type = StatType.Float,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Vision,
                            LinearPerLevelValue = 1f,
                        },
                    }
                },

                new StatDef()
                {
                    DefName = "MovementSpeed",
                    Label = "movement speed",
                    Description = "How many tiles this creature can move within 60s. Affects cost of movement abilities..",
                    Type = StatType.Float,
                    BaseValue = 1,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Moving,
                            Type = SkillImpactType.Multiplicative,
                            LinearPerLevelValue = 0.1f,
                        },
                    }
                },

                new StatDef()
                {
                    DefName = "BiteStrength",
                    Label = "bite strength",
                    Description = "How harsh this creature can bite. Affects the damage of bite-based abilities.",
                    Type = StatType.Float,
                    BaseValue = 1,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Biting,
                            Type = SkillImpactType.Multiplicative,
                            LinearPerLevelValue = 0.1f,
                        },
                        new StatPart_CreatureLevel()
                    }
                },

                new StatDef()
                {
                    DefName = "XpPerLevel",
                    Label = "XP / Level",
                    Description = "How many XP this creature gives for each level.",
                    Type = StatType.Int,
                },
            };
        }
    }
}
