using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Contains all the skills for entities.
    /// </summary>
    public class Comp_Skills : EntityComp
    {
        private CompProperties_Skills Props => (CompProperties_Skills)props;

        private Dictionary<SkillDef, Skill> Skills;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            Skills = new Dictionary<SkillDef, Skill>();
            foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefs)
            {
                Skills.Add(skillDef, new Skill(skillDef, Entity, Props.InitialSkillLevels[skillDef.DefName]));
            }
        }

        public int GetSkillLevel(SkillDef def) => Skills[def].BaseLevel;
        public List<Skill> GetAllSkills() => Skills.Values.ToList();
    }
}
