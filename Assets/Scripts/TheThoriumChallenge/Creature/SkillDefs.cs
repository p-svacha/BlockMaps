using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class SkillDefs
    {
        public static List<SkillDef> GetDefs()
        {
            return new List<SkillDef>()
            {
                new SkillDef()
                {
                    DefName = "Health",
                    Label = "health",
                    Description = "How much damage the creature can absorb.",
                },

                new SkillDef()
                {
                    DefName = "Moving",
                    Label = "moving",
                    Description = "How fast the creature is at moving around the world.",
                },

                new SkillDef()
                {
                    DefName = "Vision",
                    Label = "vision",
                    Description = "How far the creature can see.",
                },

                new SkillDef()
                {
                    DefName = "Biting",
                    Label = "biting",
                    Description = "How good the creature is at biting.",
                },

                new SkillDef()
                {
                    DefName = "Punching",
                    Label = "punching",
                    Description = "How good the creature is at punching with its arms.",
                },

                new SkillDef()
                {
                    DefName = "Kicking",
                    Label = "kicking",
                    Description = "How good the creature is at kicking with its legs.",
                },
            };
        }
    }
}
