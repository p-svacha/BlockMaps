using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class SkillDefs
    {
        // Constants to ensure proper DefName consistency
        public const string RUNNING = "Running";
        public const string VISION = "Vision";
        public const string ENDURANCE = "Endurance";
        public const string REGENERATION = "Regeneration";
        public const string CLIMBING = "Climbing";
        public const string SWIMMING = "Swimming";
        public const string VAULTING = "Vaulting";

        public static List<SkillDef> GetDefs()
        {
            return new List<SkillDef>()
            {
                new SkillDef()
                {
                    DefName = RUNNING,
                    Label = "running",
                    Description = "How far this character can move within a turn. Higher speed stat means moving needs less action points.",
                },

                new SkillDef()
                {
                    DefName = VISION,
                    Label = "vision",
                    Description = "How far away this character can see things.",
                },

                new SkillDef()
                {
                    DefName = ENDURANCE,
                    Label = "endurance",
                    Description = "Affects how much stamina this character has when fully rested.",
                },

                new SkillDef()
                {
                    DefName = REGENERATION,
                    Label = "regeneration",
                    Description = "How much stamina this character regenerates at the start of each turn.",
                },

                new SkillDef()
                {
                    DefName = CLIMBING,
                    Label = "climbing",
                    Description = "How good this character is at climbing things like ladders, fences and walls.",
                },

                new SkillDef()
                {
                    DefName = SWIMMING,
                    Label = "swimming",
                    Description = "If this character can swim and how many action points it costs to swim.",
                },

                new SkillDef()
                {
                    DefName = VAULTING,
                    Label = "vaulting",
                    Description = "How good this character is at vaulting over obstacles, and jumping and dropping onto adjacent tiles.",
                },
            };
        }
    }
}
