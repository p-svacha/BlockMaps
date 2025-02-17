using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class DamageTypeDefs
    {
        public static List<DamageTypeDef> GetDefs()
        {
            return new List<DamageTypeDef>()
            {
                new DamageTypeDef()
                {
                    DefName = "Pierce",
                    Label = "pierce",
                    Description = "Damage that punctures the target.",
                    Effectivess = new Dictionary<CreatureClassDef, Effectiveness>()
                    {
                        { CreatureClassDefOf.Squishy, Effectiveness.Normal },
                        { CreatureClassDefOf.Insect, Effectiveness.Normal },
                    }
                }
            };
        }
    }
}
