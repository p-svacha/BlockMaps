using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class CompProperties_Creature : CompProperties
    {
        public CompProperties_Creature()
        {
            CompClass = typeof(Comp_Creature);
        }

        /// <summary>
        /// The abilities the creature will have learned when spawned.
        /// </summary>
        public List<AbilityDef> InternalizedAbilities { get; init; }

        /// <summary>
        /// The classes the creature has.
        /// </summary>
        public List<CreatureClassDef> Classes { get; init; }

        public override CompProperties Clone()
        {
            return new CompProperties_Creature()
            {
                CompClass = this.CompClass,
                InternalizedAbilities = new List<AbilityDef>(this.InternalizedAbilities)
            };
        }
    }
}
