using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Add this to an EntityDef to give it a Comp_Stats that contains all Stats defined in the StatDef database.
    /// </summary>
    public class CompProperties_Stats : CompProperties
    {
        public CompProperties_Stats()
        {
            CompClass = typeof(Comp_Stats);
        }

        public override CompProperties Clone()
        {
            return new CompProperties_Stats()
            {
                CompClass = this.CompClass,
            };
        }
    }
}

