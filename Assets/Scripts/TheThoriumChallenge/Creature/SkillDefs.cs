using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class SkillDefs
    {
        // Constants to ensure proper DefName consistency
        public const string HEALTH = "Running";
        public const string MOVING = "Moving";
        public const string VISION = "Vision";
        public const string ENDURANCE = "Endurance";
        public const string BITING = "Biting";
        public const string PUNCHING = "Punching";
        public const string KICKING = "Kicking";

        public static List<SkillDef> GetDefs()
        {
            return new List<SkillDef>()
            {
                new SkillDef()
                {
                    DefName = HEALTH,
                    Label = "health",
                    Description = "How much damage the creature can absorb.",
                },

                new SkillDef()
                {
                    DefName = MOVING,
                    Label = "moving",
                    Description = "How fast the creature is at moving around the world.",
                },

                new SkillDef()
                {
                    DefName = VISION,
                    Label = "vision",
                    Description = "How far the creature can see.",
                },

                new SkillDef()
                {
                    DefName = ENDURANCE,
                    Label = "biting",
                    Description = "How good the creature is at biting.",
                },

                new SkillDef()
                {
                    DefName = BITING,
                    Label = "punching",
                    Description = "How good the creature is at punching with its arms.",
                },

                new SkillDef()
                {
                    DefName = PUNCHING,
                    Label = "kicking",
                    Description = "How good the creature is at kicking with its legs.",
                },
            };
        }
    }
}
