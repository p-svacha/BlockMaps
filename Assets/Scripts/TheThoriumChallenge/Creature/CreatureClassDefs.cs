using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class CreatureClassDefs
    {
        public static List<CreatureClassDef> GetDefs()
        {
            return new List<CreatureClassDef>()
            {
                new CreatureClassDef()
                {
                    DefName = "Armored",
                    Label = "armored",
                    Description = "Creatures with protective features like shells.",
                    Color = HelperFunctions.GetColorFromRgb255(128, 128, 128),
                },

                new CreatureClassDef()
                {
                    DefName = "Squishy",
                    Label = "squishy",
                    Description = "Gooey creatures without a vertebrae.",
                    Color = HelperFunctions.GetColorFromRgb255(204, 102, 255),
                },

                new CreatureClassDef()
                {
                    DefName = "Insect",
                    Label = "insect",
                    Description = "Insect- and arthropod-like creatures.",
                    Color = HelperFunctions.GetColorFromRgb255(153, 102, 51),
                },
            };
        }
    }
}
