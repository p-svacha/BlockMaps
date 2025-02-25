using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class StatDefs
    {
        public static List<StatDef> GetDefs()
        {
            return new List<StatDef>()
            {
                new StatDef()
                {
                    DefName = "MovementSpeed",
                    Label = "movement speed",
                    Description = "The base amount tiles per second this character moves when running.\nThe actual is speed is also influenced by the running aptitude.",
                    BaseValue = 2f
                },

                new StatDef()
                {
                    DefName = "RunningAptitude",
                    Label = "running aptitude",
                    Description = "Modifier of how many action points running costs. The higher the aptitude the lower the cost.",
                    Type = StatType.Percent,
                    BaseValue = 0f,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Running,
                            LinearPerLevelValue = 0.1f,
                        }
                    },
                },

                new StatDef()
                {
                    DefName = "VisionRange",
                    Label = "vision range",
                    Description = "How many tiles around themselves this character can see things.",
                    Type = StatType.Int,
                    BaseValue = 0f,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Vision,
                            LinearPerLevelValue = 1f,
                        }
                    }
                },

                new StatDef()
                {
                    DefName = "MaxStamina",
                    Label = "max stamina",
                    Description = "How much stamina this character has when fully rested.",
                    Type = StatType.Int,
                    BaseValue = 18,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Endurance,
                            LinearPerLevelValue = 3,
                        }
                    },
                },

                new StatDef()
                {
                    DefName = "StaminaRegeneration",
                    Label = "stamina regeneration",
                    Description = "How much stamina this character regenerates at the start of each turn.",
                    BaseValue = 0f,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Regeneration,
                            LinearPerLevelValue = 0.5f,
                        }
                    }
                },

                new StatDef()
                {
                    DefName = "ClimbingSkill",
                    Label = "climbing",
                    Description = "Defines what kind of things this character can climb\n0 = nothing\n1 = ladders\n2 = ladders, fences\n3 = ladders, fences, walls.",
                    Type = StatType.Int,
                    BaseValue = 0,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Climbing,
                            Curve = SkillImpactCurve.ValuePerLevel,
                            PerLevelValues = new Dictionary<int, float>()
                            {
                                { 0, 0 },
                                { 1, 1 },
                                { 10, 2 },
                                { 20, 3 },
                            }
                        }
                    }
                },

                new StatDef()
                {
                    DefName = "ClimbingAptitude",
                    Label = "climbing aptitude",
                    Description = "Modifier of how many action points climbing costs.",
                    Type = StatType.Percent,
                    BaseValue = 0.25f,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Climbing,
                            LinearPerLevelValue = 0.1375f,
                        }
                    }
                },

                new StatDef()
                {
                    DefName = "CanSwim",
                    Label = "can swim",
                    Description = "If this character enter water.",
                    Type = StatType.Binary,
                    BaseValue = 1,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillRequirement()
                        {
                            RequiredSkill = SkillDefOf.Swimming
                        }
                    },
                },

                new StatDef()
                {
                    DefName = "SwimmingAptitude",
                    Label = "swimming aptitude",
                    Description = "Modifier of how many action points swimming costs.",
                    Type = StatType.Percent,
                    BaseValue = 0.25f,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Swimming,
                            LinearPerLevelValue = 0.1375f,
                        }
                    }
                },

                new StatDef()
                {
                    DefName = "HopUpDistance",
                    Label = "hopping",
                    Description = "How many cells upwards this character can vault over obstacles or onto adjacent tiles.",
                    Type = StatType.Int,
                    BaseValue = 1,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Vaulting,
                            LinearPerLevelValue = 0.2f,
                        }
                    }
                },

                new StatDef()
                {
                    DefName = "HopDownDistance",
                    Label = "dropping",
                    Description = "How many cells downwards this character can drop onto adjacent tiles.",
                    Type = StatType.Int,
                    BaseValue = 1,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Vaulting,
                            LinearPerLevelValue = 0.4f,
                        }
                    }
                },

                new StatDef()
                {
                    DefName = "HopAptitude",
                    Label = "jumping aptitude",
                    Description = "Modifier of how many action points hopping and dropping costs.",
                    Type = StatType.Percent,
                    BaseValue = 0.25f,
                    StatParts = new List<StatPart>()
                    {
                        new StatPart_SkillImpact()
                        {
                            SkillDef = SkillDefOf.Vaulting,
                            LinearPerLevelValue = 0.1375f,
                        }
                    }
                },
            };
        }
    }
}
