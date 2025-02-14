using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public static class AbilityDefs
    {
        public static List<AbilityDef> GetDefs()
        {
            return new List<AbilityDef>()
            {
                new AbilityDef() { DefName="Bite", AbilityClass = typeof(Ability001_Bite) }
            };
        }
    }
}
