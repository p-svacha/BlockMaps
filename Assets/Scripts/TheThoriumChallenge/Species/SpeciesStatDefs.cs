using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class SpeciesStatDefs
    {
        public static List<SpeciesStatDef> Defs = new List<SpeciesStatDef>()
        {
            new SpeciesStatDef()
            {
                DefName = "MaxHP",
                Label = "max HP per level",
                Description = "How much additional maximum HP creatures of this species get each level.",
            },

            new SpeciesStatDef()
            {
                DefName = "VisionRange",
                Label = "vision range",
                Description = "How many tiles around them creatures of this species see. ",
            },

            new SpeciesStatDef()
            {
                DefName = "MovementSpeed",
                Label = "movement speed",
                Description = "How many tiles creatures of this species can move within 60s. Affects cost of movement abilities.",
            },

            new SpeciesStatDef()
            {
                DefName = "BiteStrength",
                Label = "bite strength per level",
                Description = "How much additional bite strength creatures of this species get each level.",
            },
        };
    }
}
