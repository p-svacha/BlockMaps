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

        public override void Initialize(CompProperties props, Entity entity)
        {
            base.Initialize(props, entity);

            Stats = new Dictionary<StatDef, Stat>();
            foreach (StatDef statDef in DefDatabase<StatDef>.AllDefs)
            {
                Stats.Add(statDef, new Stat(statDef, entity));
            }
        }

        public float GetStat(StatDef def) => Stats[def].GetValue();
        public List<Stat> GetAllStats() => Stats.Values.ToList();
    }
}
