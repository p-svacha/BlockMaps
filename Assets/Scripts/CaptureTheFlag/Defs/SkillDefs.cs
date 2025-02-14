using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class SkillDefs
    {
        public static List<SkillDef> GetDefs()
        {
            return new List<SkillDef>()
            {
                // Most important ones
                new SkillDef()
                {
                    DefName = "Running",
                    Label = "running",
                    Description = "How far this character can move within a turn. Higher speed stat means moving needs less action points.",
                },

                new SkillDef()
                {
                    DefName = "Vision",
                    Label = "vision",
                    Description = "How far away this character can see things.",
                },

                new SkillDef()
                {
                    DefName = "Endurance",
                    Label = "endurance",
                    Description = "Affects how much stamina this character has when fully rested.",
                },

                new SkillDef()
                {
                    DefName = "Regeneration",
                    Label = "regeneration",
                    Description = "How much stamina this character regenerates at the start of each turn.",
                },

                new SkillDef()
                {
                    DefName = "Climbing",
                    Label = "climbing",
                    Description = "How good this character is at climbing things like ladders, fences and walls.",
                },

                new SkillDef()
                {
                    DefName = "Swimming",
                    Label = "swimming",
                    Description = "If this character can swim and how many action points it costs to swim.",
                },

                new SkillDef()
                {
                    DefName = "Vaulting",
                    Label = "vaulting",
                    Description = "How good this character is at vaulting over obstacles, and jumping and dropping onto adjacent tiles.",
                },
            };
        }
    }
}
