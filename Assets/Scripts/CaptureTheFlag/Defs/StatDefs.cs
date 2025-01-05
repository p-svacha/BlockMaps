using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class StatDefs
    {
        public static List<StatDef> Defs = new List<StatDef>()
        {
            new StatDef()
            {
                DefName = "MovementSpeed",
                Label = "movement speed",
                Description = "How many tiles per second this character moves when running.",
                BaseValue = 0f,
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Running",
                        LinearPerLevelValue = 0.2f,
                    }
                },
            },

            new StatDef()
            {
                DefName = "RunningAptitude",
                Label = "running aptitude",
                Description = "Modifier of how many action points running costs.",
                Type = StatType.Percent,
                BaseValue = 0f,
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Running",
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Vision",
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Stamina",
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Regeneration",
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Climbing",
                        Type = SkillImpactType.ValuePerLevel,
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Climbing",
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
                SkillRequirements = new List<string>()
                {
                    "Swimming",
                },
            },

            new StatDef()
            {
                DefName = "SwimmingAptitude",
                Label = "swimming aptitude",
                Description = "Modifier of how many action points swimming costs.",
                Type = StatType.Percent,
                BaseValue = 0.25f,
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Swimming",
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Vaulting",
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Vaulting",
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
                SkillOffsets = new List<SkillImpact>()
                {
                    new SkillImpact()
                    {
                        SkillDefName = "Vaulting",
                        LinearPerLevelValue = 0.1375f,
                    }
                }
            },
        };
    }
}
