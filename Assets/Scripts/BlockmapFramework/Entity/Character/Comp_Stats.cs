using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Contains all the stats for entities.
    /// </summary>
    public class Comp_Stats : EntityComp
    {
        private CompProperties_Stats Props => (CompProperties_Stats)props;

        private Dictionary<StatDef, Stat> Stats;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            Stats = new Dictionary<StatDef, Stat>();
            foreach (StatDef statDef in DefDatabase<StatDef>.AllDefs)
            {
                Stats.Add(statDef, new Stat(statDef, Entity));
            }
        }

        public float GetStat(StatDef def) => Stats[def].GetValue();
        public List<Stat> GetAllStats() => Stats.Values.ToList();
    }
}
