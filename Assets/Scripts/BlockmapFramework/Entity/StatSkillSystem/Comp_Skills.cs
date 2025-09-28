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

        public override void Initialize(CompProperties props, Entity entity)
        {
            base.Initialize(props, entity);

            Skills = new Dictionary<SkillDef, Skill>();
            foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefs)
            {
                // if (!Props.InitialSkillLevels.ContainsKey(skillDef)) throw new System.Exception($"InitialSkillLevels does not contain a key for {skillDef.DefName}.");
                Skills.Add(skillDef, new Skill(skillDef, entity, Props.GetInitialSkillLevel(skillDef)));
            }
        }

        public override void Validate()
        {
            if (Props.InitialSkillLevels.Keys.Any(x => x == null)) throw new System.Exception("A SkillDef in CompProperties_Skills.InitialSkillLevels is null.");
        }

        public int GetSkillLevel(SkillDef def) => Skills[def].BaseLevel;
        public List<Skill> GetAllSkills() => Skills.Values.ToList();
    }
}
