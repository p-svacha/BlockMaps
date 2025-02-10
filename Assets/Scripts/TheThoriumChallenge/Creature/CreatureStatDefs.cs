using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class CreatureStatDefs
    {
        public static List<CreatureStatDef> Defs = new List<CreatureStatDef>()
        {
            new CreatureStatDef()
            {
                DefName = "MaxHP",
                Label = "max HP",
                LabelShort = "Max HP",
                Description = "The maximum HP of this creature.",
                Type = StatType.Int,
                ScalesWithLevel = true,
            },

            new CreatureStatDef()
            {
                DefName = "VisionRange",
                Label = "vision range",
                LabelShort = "Vision",
                Description = "How many tiles around itself the creature sees.",
                Type = StatType.Float,
                ScalesWithLevel = false,
            },

            new CreatureStatDef()
            {
                DefName = "MovementSpeed",
                Label = "movement speed",
                LabelShort = "Move",
                Description = "How many tiles this creature can move within 60s. Affects cost of movement abilities..",
                Type = StatType.Float,
                ScalesWithLevel = false,
            },

            new CreatureStatDef()
            {
                DefName = "BiteStrength",
                Label = "bite strength",
                LabelShort = "Bite",
                Description = "How harsh this creature can bite. Affects the damage of bite-based abilities.",
                Type = StatType.Float,
                ScalesWithLevel = true,
            },
        };
    }
}
