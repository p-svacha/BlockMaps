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
                new AbilityDef() { DefName="Move", AbilityClass = typeof(Ability_Move) },
                new AbilityDef() { DefName="Bite", AbilityClass = typeof(Ability_Bite) }
            };
        }
    }
}
