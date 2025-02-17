using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Comp_Creature : EntityComp
    {
        public CompProperties_Creature Props => (CompProperties_Creature)props;

        private Dictionary<AbilityDef, Ability> Abilities;

        public override void Initialize(CompProperties props, Entity entity)
        {
            base.Initialize(props, entity);

            Abilities = new Dictionary<AbilityDef, Ability>();
            foreach (AbilityDef abilityDef in Props.InternalizedAbilities)
            {
                Ability ability = (Ability)System.Activator.CreateInstance(abilityDef.AbilityClass);
                ability.Init(abilityDef, (Creature)entity);
                Abilities.Add(abilityDef, ability);
            }
        }

        public Ability GetAbility(AbilityDef def) => Abilities[def];
        public List<Ability> GetAllAbilities() => Abilities.Values.ToList();
    }
}
