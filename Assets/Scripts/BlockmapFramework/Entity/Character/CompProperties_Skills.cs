using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Add this to an EntityDef to give it a Comp_Skills that contains all Skills defined in the SkillDef database.
    /// </summary>
    public class CompProperties_Skills : CompProperties
    {
        public CompProperties_Skills()
        {
            CompClass = typeof(Comp_Skills);
        }

        /// <summary>
        /// The initial skill levels the entity has when created.
        /// </summary>
        public Dictionary<string, int> InitialSkillLevels { get; init; } = new();

        public override CompProperties Clone()
        {
            return new CompProperties_Skills()
            {
                CompClass = this.CompClass,
                InitialSkillLevels = this.InitialSkillLevels.ToDictionary(x => x.Key, x => x.Value),
            };
        }
    }
}
