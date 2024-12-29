using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public static class StatDefs
    {
        public static List<StatDef> Defs = new List<StatDef>()
        {
            // Most important ones
            new StatDef()
            {
                DefName = "Speed",
                Label = "speed",
                Description = "How far this character can move within a turn. Higher speed stat means moving needs less action points."
            },

            new StatDef()
            {
                DefName = "Vision",
                Label = "vision",
                Description = "How many cells in every direction this character can see things."
            },

            new StatDef()
            {
                DefName = "MaxStamina",
                Label = "stamina",
                Description = "How much stamina this character has when fully rested."
            },

            new StatDef()
            {
                DefName = "StaminaRegeneration",
                Label = "stamina regeneration",
                Description = "How much stamina this character regenerates at the start of each turn."
            },

            // Less important ones
            new StatDef()
            {
                DefName = "Climbing",
                Label = "climbing",
                Description = "If this character can climb and how many action points it costs."
            },

            new StatDef()
            {
                DefName = "Swimming",
                Label = "swimming",
                Description = "If this character can swim and how many action points it costs to swim."
            },

            new StatDef()
            {
                DefName = "Jumping",
                Label = "jumping",
                Description = "How many cells upwards this character can jump onto adjacent tiles.",
                Type = StatType.Int
            },

            new StatDef()
            {
                DefName = "Dropping",
                Label = "dropping",
                Description = "How many cells downwards this character can drop onto adjacent tiles.",
                Type = StatType.Int
            },

            new StatDef()
            {
                DefName = "Height",
                Label = "height",
                Description = "How many cells tall this character is. Taller characters can see better over things, but can't fit through tight spaces.",
                Type = StatType.Int
            },

            new StatDef()
            {
                DefName = "CanUseDoors",
                Label = "can use doors",
                Description = "If this character can interact with doors.",
                Type = StatType.Binary
            },
        };
    }
}
