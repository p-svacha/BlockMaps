using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class CompProperties_Abilities : CompProperties
    {
        public CompProperties_Abilities()
        {
            CompClass = typeof(Comp_Abilities);
        }

        /// <summary>
        /// The abilities the creature will have learned when spawned.
        /// </summary>
        public List<AbilityDef> InternalizedAbilities { get; init; } = new();

        public override CompProperties Clone()
        {
            return new CompProperties_Abilities()
            {
                CompClass = this.CompClass,
                InternalizedAbilities = new List<AbilityDef>(this.InternalizedAbilities)
            };
        }
    }
}
